//Copyright 2011 Cloud Sidekick
// 
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Data;
using System.Text;
using System.IO;

using System.Net;
using System.Globalization;

using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Serialization;

using Globals;

namespace Web.pages
{
    /// <summary>
    /// Summary description for awsMethods
    /// </summary>
    [WebService(Namespace = "ACAWSMethods")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]

    //class used to create our canonical list.
    class ParamComparer : IComparer<string>
    {
        public int Compare(string p1, string p2)
        {
            return string.CompareOrdinal(p1, p2);
        }
    }
    public class awsMethods : System.Web.Services.WebService
    {

		[WebMethod(EnableSession = true)]
		public string wmTestCloudConnection(string sAccountID, string sCloudID)
		{
			acUI.acUI ui = new acUI.acUI();
			string sErr = "";
			
			Cloud c = new Cloud(sCloudID);
			if (c.ID == null) {
				return "{'result':'fail','error':'Failed to get Cloud details for Cloud ID [" + sCloudID + "].'}";
			}
			
			CloudAccount ca = new CloudAccount(sAccountID);
			if (ca.ID == null) {
				return "{'result':'fail','error':'Failed to get Cloud Account details for Cloud Account ID [" + sAccountID + "].'}";
			}

			CloudProviders cp = ui.GetCloudProviders();
			if (cp == null) 
			{
				return "{'result':'fail','error':'Failed to get Cloud Providers definition from session.'}";
			}
			else 
			{
				//get the test cloud object type for this provider
				CloudObjectType cot = ui.GetCloudObjectType(c.Provider, c.Provider.TestObject);
				if (cot != null) {
					if (string.IsNullOrEmpty(cot.ID)) {
						return "{'result':'fail','error':'Cannot find definition for requested object type [" + c.Provider.TestObject + "].'}";
					}
				} else {
					return "{'result':'fail','error':'GetCloudObjectType failed for [" + c.Provider.TestObject + "].'}";
				}
				
				string sURL = GetURL(ca, c, cot, null, ref sErr);			
				if (!string.IsNullOrEmpty(sErr))
					return "{'result':'fail','error':'" + ui.packJSON(sErr) +"'}";
				
				string sResult = ui.HTTPGet(sURL, ref sErr);
				if (!string.IsNullOrEmpty(sErr))
					return "{'result':'fail','error':'" + ui.packJSON(sErr) + "'}";

				return "{'result':'success','response':'" + ui.packJSON(sResult) + "'}";
			}
		}

        #region "Request Building Methods"
        //this method looks up a cloud object in our database, and executes a call based on CloudObjectType parameters.
        //the columns created as part of the object are defined as CloudObjectTypeProperty.
        public DataTable GetCloudObjectsAsDataTable(string sCloudID, string sObjectType, ref string sErr)
        {
            acUI.acUI ui = new acUI.acUI();

            try
            {
                //build the DataTable
                DataTable dt = new DataTable();

                //get the cloud object type from the session
				Provider p = ui.GetSelectedCloudProvider();
				CloudObjectType cot = ui.GetCloudObjectType(p, sObjectType);
                if (cot != null)
                {
                    if (string.IsNullOrEmpty(cot.ID))
                    { sErr = "Cannot find definition for requested object type [" + sObjectType + "]"; return null; }
                }
                else
                {
                    sErr = "GetCloudObjectType failed for [" + sObjectType + "]";
                    return null;
                }

                string sXML = GetCloudObjectsAsXML(sCloudID, cot, ref sErr, null);
                if (sErr != "") return null;
				
				if (string.IsNullOrEmpty(sXML))
				{
					sErr = "GetCloudObjectsAsXML returned an empty document.";
					return null;
				}

                //OK look, all this namespace nonsense is annoying.  Every AWS result I've witnessed HAS a namespace
                // (which messes up all our xpaths)
                // but I've yet to see a result that actually has two namespaces 
                // which is the only scenario I know of where you'd need them at all.

                //So... to eliminate all namespace madness
                //brute force... parse this text and remove anything that looks like [ xmlns="<crud>"] and it's contents.
                sXML = ui.RemoveNamespacesFromXML(sXML);

                XElement xDoc = XElement.Parse(sXML);
                if (xDoc == null) { sErr = "AWS Response XML document is invalid."; return null; }


                //what columns go in the DataTable?
                if (cot.Properties.Count > 0)
                {
                    foreach (CloudObjectTypeProperty prop in cot.Properties)
                    {
                        //the column on the data table *becomes* the property.
                        //we'll load it up with all the goodness we need anywhere else
                        DataColumn dc = new DataColumn();

                        dc.ColumnName = prop.Name;

                        //This is important!  Places in the GUI expect the first column to be the ID column.
                        //hoping to stop doing that in favor of this property.
                        if (prop.IsID) dc.ExtendedProperties.Add("IsID", true);
                        //will we try to draw an icon?
                        if (prop.HasIcon) dc.ExtendedProperties.Add("HasIcon", true);

						//what was the xpath for this property?
                        dc.ExtendedProperties.Add("XPath", prop.XPath);
                        //a "short list" property is one that will always show up... it's a shortcut in some places.
                        dc.ExtendedProperties.Add("ShortList", prop.ShortList);
                        //it might have a custom caption
                        if (!string.IsNullOrEmpty(prop.Label)) dc.Caption = prop.Label;

                        //add the column
                        dt.Columns.Add(dc);
                    }
                }
                else
                {
                    sErr = "No properties defined for type [" + sObjectType + "]";
                    //if this is a power user, write out the XML of the response as a debugging aid.
                    if (ui.UserIsInRole("Developer") || ui.UserIsInRole("Administrator"))
                    {
                        sErr += "<br />RESPONSE:<br /><pre>" + ui.SafeHTML(sXML) + "</pre>";
                    }
                    return null;
                }

                //ok, columns are added.  Parse the XML and add rows.
                foreach (XElement xeRecord in xDoc.XPathSelectElements(cot.XMLRecordXPath))
                {
                    DataRow drNewRow = dt.NewRow();

                    //we could just loop the Cloud Type Properties again, but doing the DataColumn collection
                    //ensures all the info we need got added
                    foreach (DataColumn dc in dt.Columns)
                    {
                        XElement xeProp = xeRecord.XPathSelectElement(dc.ExtendedProperties["XPath"].ToString());
                        if (xeProp != null) drNewRow[dc.ColumnName] = xeProp.Value;
                    }

                    //build the row
                    dt.Rows.Add(drNewRow);
                }

                //all done
                return dt;
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                return null;
            }
        }
        public string GetCloudObjectsAsXML(string sCloudID, CloudObjectType cot, ref string sErr, Dictionary<string, string> AdditionalArguments)
        {
            acUI.acUI ui = new acUI.acUI();

            string sXML = "";

			string sAccountID = ui.GetSelectedCloudAccountID();
			CloudAccount ca = new CloudAccount(sAccountID);
			if (ca.ID == null) {
				sErr = "Failed to get Cloud Account details for Cloud Account ID [" + sAccountID + "].";
				return null;
			}

            if (cot != null)
            {
                //many reasons why we'd bail here.  Rather than a bunch of testing below, let's just crash
                //if a key field is missing.
                if (string.IsNullOrEmpty(cot.ID))
                { sErr = "Cannot find definition for requested object type [" + cot.ID + "]"; return null; }
//                if (string.IsNullOrEmpty(prod.APIUrlPrefix))
//                { sErr = "APIUrlPrefix not defined for requested object type [" + cot.ID + "]"; return null; }
//                if (string.IsNullOrEmpty(cot.APICall))
//                { sErr = "APICall not defined for requested object type [" + cot.ID + "]"; return null; }
            }
            else
            {
                sErr = "GetCloudObjectType failed for [" + cot.ID + "]";
                return null;
            }
			
			//get the cloud object
			Cloud c = new Cloud(sCloudID);
			if (c.ID == null) {
                sErr = "Failed to get Cloud details for Cloud ID [" + sCloudID + "].";
                return null;
			}
			
//			//HOST URL
//			//we have to use the provided cloud and object type to construct an endpoint
//			//if either of these values is missing, we will attempt to use the other one standalone.
//			string sHostName = "";
//			
//			//if both are there, concatenate them
//			if (!string.IsNullOrEmpty(prod.APIUrlPrefix) && !string.IsNullOrEmpty(c.APIUrl))
//				sHostName = prod.APIUrlPrefix + "." + c.APIUrl;
//			else if (string.IsNullOrEmpty(prod.APIUrlPrefix) && !string.IsNullOrEmpty(c.APIUrl))
//				sHostName = c.APIUrl;
//			else if (!string.IsNullOrEmpty(prod.APIUrlPrefix) && string.IsNullOrEmpty(c.APIUrl))
//				sHostName = prod.APIUrlPrefix;
//			
//			if (string.IsNullOrEmpty(sHostName)) {
//                sErr = "Unable to reconcile an endpoint from the Cloud [" + c.Name + "] or Cloud Object [" + cot.ID + "] definitions." + sErr;
//                return null;
//			}
//			
//			
//			//HOST URI
//			//what's the URI... (if any)
//			string sResourceURI = "";
//			if (!string.IsNullOrEmpty(prod.APIUri))
//				sResourceURI = prod.APIUri;
//
//			
//
//			//PARAMETERS
//            //first, this is an explicit list of parameters in a dictionary. 
//            //in the real world, we'll probably pull these params from a table
//            //or have to parse a querystring
//            ParamComparer pc = new ParamComparer();
//            SortedDictionary<string, string> sortedRequestParams = new SortedDictionary<string, string>(pc);
//
//            //call specific parameters (this is AWS specific!!!)
//            sortedRequestParams.Add("Action", cot.APICall);
//
//            //do we need to apply a group filter?  If it's defined on the table then YES!
//            if (!string.IsNullOrEmpty(cot.APIRequestGroupFilter))
//            {
//                string[] sTmp = cot.APIRequestGroupFilter.Split('=');
//                sortedRequestParams.Add(sTmp[0], sTmp[1]);
//            }
//
//			//ADDITIONAL ARGUMENTS
//            if (AdditionalArguments != null)
//            {
//                //we have custom arguments... use them
//                //for each... add to sortedRequestParams
//                //if the same key from the group filter is defined as sAdditionalArguments it overrides the table!
//            }
//
//
//            //AWS auth parameters
//            string sDate = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss", DateTimeFormatInfo.InvariantInfo);
//
//            sortedRequestParams.Add("AWSAccessKeyId", sAccessKeyID);
//            sortedRequestParams.Add("Version", prod.APIVersion);
//            
//			//some products use the older Expires method
//			if (prod.Name == "s3")
//				sortedRequestParams.Add("Expires", "2020202020"); // a point waaaay in the distant future.
//			else
//			sortedRequestParams.Add("Timestamp", sDate);
//			
//            sortedRequestParams.Add("SignatureMethod", "HmacSHA256");
//            sortedRequestParams.Add("SignatureVersion", "2");
//			
//			
//		
//			//now we have all the parameters in a list, build a sorted, encoded querystring string
//            string sQueryString = GetSortedParamsAsString(sortedRequestParams, true);
//            
//			
//			//use the URL/URI plus the querystring to build the full request to be signed
//            string sStringToSign = awsComposeStringToSign("GET", sHostName, sResourceURI, sQueryString);
//            
//			//and sign it
//            //string sSignature = GetAWS3_SHA1AuthorizationValue(sSecretAccessKeyID, sStringToSign);
//            string sSignature = awsGetSHA256AuthorizationValue(sSecretAccessKeyID, sStringToSign);
//
//            //finally, urlencode the signature
//            sSignature = PercentEncodeRfc3986(sSignature);
//
//
//            string sHostURL = prod.APIProtocol.ToLower() + "://" + sHostName + sResourceURI;
//            string sURL = sHostURL + "?" + sQueryString + "&Signature=" + sSignature;
			string sURL = GetURL(ca, c, cot, AdditionalArguments, ref sErr);			
			if (!string.IsNullOrEmpty(sErr))
				return null;
			
			sXML = ui.HTTPGet(sURL, ref sErr);
			if (!string.IsNullOrEmpty(sErr))
				return null;

            return sXML;
        }
		private string GetURL(CloudAccount ca, Cloud c, CloudObjectType cot, Dictionary<string, string> AdditionalArguments, ref string sErr)
		{
			////test output
			//string sOut = "STRING:<br />" + sStringToSign + "<br /><br />";
			//sOut += "QueryString:<br />" + sQueryString + "<br /><br />";
			//sOut += "SIGNED:<br />" + sSignature + "<br /><br />";
			//sOut += "URL:<br /><a href='" + sURL + "' target='_blank'>" + sURL + "</a><br /><br />";
	
			////sOut += "DECRYPTED:" + Convert.FromBase64String(sSignature) + "<br />";
	
			//char[] values = sStringToSign.ToCharArray();
			//string sHex = "";
			//foreach (char letter in values)
			//{
			//    // Get the integral value of the character.
			//    int value = Convert.ToInt32(letter);
			//    // Convert the decimal value to a hexadecimal value in string form.
			//    sHex += String.Format("{0:X}", value);
			//}
			//sOut += "HEX STRING TO SIGN:" + sHex + "<br />";
	
			
			
			//we have to use the provided cloud and object type to construct an endpoint
			//if either of these values is missing, we will attempt to use the other one standalone.
			string sHostName = "";
		
			Product prod = cot.ParentProduct;
		
			//if both are there, concatenate them
			if (!string.IsNullOrEmpty(prod.APIUrlPrefix) && !string.IsNullOrEmpty(c.APIUrl))
				sHostName = prod.APIUrlPrefix + "." + c.APIUrl;
			else if (string.IsNullOrEmpty(prod.APIUrlPrefix) && !string.IsNullOrEmpty(c.APIUrl))
				sHostName = c.APIUrl;
			else if (!string.IsNullOrEmpty(prod.APIUrlPrefix) && string.IsNullOrEmpty(c.APIUrl))
				sHostName = prod.APIUrlPrefix;
		
			if (string.IsNullOrEmpty(sHostName)) {
				sErr = "Unable to reconcile an endpoint from the Cloud [" + c.Name + "] or Cloud Object [" + cot.ID + "] definitions." + sErr;
				return null;
			}
		
		
			//HOST URI
			//what's the URI... (if any)
			string sResourceURI = "";
			if (!string.IsNullOrEmpty(prod.APIUri))
				sResourceURI = prod.APIUri;
	
		
	
			//PARAMETERS
			//first, this is an explicit list of parameters in a dictionary. 
			//in the real world, we'll probably pull these params from a table
			//or have to parse a querystring
			ParamComparer pc = new ParamComparer();
			SortedDictionary<string, string> sortedRequestParams = new SortedDictionary<string, string>(pc);
	
			//call specific parameters (this is AWS specific!!!)
			sortedRequestParams.Add("Action", cot.APICall);
	
			//do we need to apply a group filter?  If it's defined on the table then YES!
			if (!string.IsNullOrEmpty(cot.APIRequestGroupFilter)) {
				string[] sTmp = cot.APIRequestGroupFilter.Split('=');
				sortedRequestParams.Add(sTmp[0], sTmp[1]);
			}
	
			//ADDITIONAL ARGUMENTS
			if (AdditionalArguments != null) {
				//we have custom arguments... use them
				//for each... add to sortedRequestParams
				//if the same key from the group filter is defined as sAdditionalArguments it overrides the table!
			}
	
	
			//AWS auth parameters
			string sAccessKeyID = ca.LoginID;
			string sSecretAccessKeyID = ca.LoginPassword;
			
			string sDate = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss", DateTimeFormatInfo.InvariantInfo);
	
			sortedRequestParams.Add("AWSAccessKeyId", sAccessKeyID);
			sortedRequestParams.Add("Version", prod.APIVersion);
	
			//some products use the older Expires method
			if (prod.Name == "s3")
				sortedRequestParams.Add("Expires", "2020202020"); // a point waaaay in the distant future.
			else
				sortedRequestParams.Add("Timestamp", sDate);
		
			sortedRequestParams.Add("SignatureMethod", "HmacSHA256");
			sortedRequestParams.Add("SignatureVersion", "2");
		
		
	
			//now we have all the parameters in a list, build a sorted, encoded querystring string
			string sQueryString = GetSortedParamsAsString(sortedRequestParams, true);
	
		
			//use the URL/URI plus the querystring to build the full request to be signed
			string sStringToSign = awsComposeStringToSign("GET", sHostName, sResourceURI, sQueryString);
	
			//and sign it
			//string sSignature = GetAWS3_SHA1AuthorizationValue(sSecretAccessKeyID, sStringToSign);
			string sSignature = awsGetSHA256AuthorizationValue(sSecretAccessKeyID, sStringToSign);
	
			//finally, urlencode the signature
			sSignature = PercentEncodeRfc3986(sSignature);
	
	
			string sHostURL = prod.APIProtocol.ToLower() + "://" + sHostName + sResourceURI;
		
			return sHostURL + "?" + sQueryString + "&Signature=" + sSignature;
		}

        public string PercentEncodeRfc3986(string s)
        {
            s = HttpUtility.UrlEncode(s, System.Text.Encoding.UTF8);
            s = s.Replace("'", "%27").Replace("(", "%28").Replace(")", "%29").Replace("*", "%2A").Replace("!", "%21").Replace("%7e", "~").Replace("+", "%20");
            StringBuilder sb = new StringBuilder(s);
            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] == '%')
                {
                    if (Char.IsLetter(sb[i + 1]) || Char.IsLetter(sb[i + 2]))
                    {
                        sb[i + 1] = Char.ToUpper(sb[i + 1]); sb[i + 2] = Char.ToUpper(sb[i + 2]);
                    }
                }
            }
            return sb.ToString();
        }
        //After the parameters are sorted in natural byte order and URL encoded, the next step is to concatenate them into a single text string.  
        //The following method uses the PercentEncodeRfc3986 method, shown previously, to create the parameter string.
        public string GetSortedParamsAsString(SortedDictionary<String, String> paras, bool isCanonical)
        {
            String sParams = "";
            String sKey = null;
            String sValue = null;
            String separator = "";
            foreach (KeyValuePair<string, String> entry in paras)
            {
                sKey = PercentEncodeRfc3986(entry.Key);
                sValue = PercentEncodeRfc3986(entry.Value);
                //if (isCanonical)
                //{
                //    sKey = PercentEncodeRfc3986(sKey);
                //    sValue = PercentEncodeRfc3986(sValue);
                //}
                sParams += separator + sKey + "=" + sValue;
                separator = "&";
            }

            return sParams;
        }

        //The following method creates the final string to sign. 
        public string awsComposeStringToSign(string sHTTPVerb, string sHost, string sResourceURI, string sQueryString)
        {
            String stringToSign = null;

            stringToSign = sHTTPVerb + "\n";
            stringToSign += sHost + "\n";
            stringToSign += (string.IsNullOrEmpty(sResourceURI) ? "/" : sResourceURI) + "\n";
            stringToSign += sQueryString;  //don't worry about encoding... was already encoded.

            return stringToSign;
        }
        public string awsGetSHA1AuthorizationValue(string sAWSSecretAccessKey, string sStringToSign)
        {
            System.Security.Cryptography.HMACSHA1 MySigner =
                new System.Security.Cryptography.HMACSHA1(System.Text.Encoding.UTF8.GetBytes(sAWSSecretAccessKey));
            string SignatureValue =
                Convert.ToBase64String(MySigner.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sStringToSign)));
            return SignatureValue;
        }
        public string awsGetSHA256AuthorizationValue(string sAWSSecretAccessKey, string sStringToSign)
        {
            System.Security.Cryptography.HMACSHA256 MySigner =
                new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(sAWSSecretAccessKey));
            string SignatureValue =
                Convert.ToBase64String(MySigner.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sStringToSign)));
            return SignatureValue;
        }

        #endregion
    }
}
