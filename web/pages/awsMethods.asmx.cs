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


        #region "Request Building Methods"
        //this method looks up a cloud object in our database, and executes a call based on parameters stored in the cloud_object_type table.
        //the columns created as part of the object are defined in cloud_object_type_detail table
        public DataTable GetCloudObjectsAsDataTable(string sObjectType, ref string sErr)
        {
            acUI.acUI ui = new acUI.acUI();

            try
            {
                //build the DataTable
                DataTable dt = new DataTable();

                //get the cloud object type from the session
                CloudObjectType cot = ui.GetCloudObjectType(sObjectType);
                if (cot != null)
                {
                    if (string.IsNullOrEmpty(cot.ObjectType))
                    { sErr = "Cannot find definition for requested object type [" + sObjectType + "]"; return null; }
                }
                else
                {
                    sErr = "GetCloudObjectType failed for [" + sObjectType + "]";
                    return null;
                }

                string sXML = GetCloudObjectsAsXML(sObjectType, ref sErr, null);
                if (sErr != "") return null;

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
                    foreach (CloudObjectTypeProperty p in cot.Properties)
                    {
                        //the column on the data table *becomes* the property.
                        //we'll load it up with all the goodness we need anywhere else
                        DataColumn dc = new DataColumn();

                        dc.ColumnName = p.PropertyName;

                        //This is important!  Places in the GUI expect the first column to be the ID column.
                        //hoping to stop doing that in favor of this property.
                        dc.ExtendedProperties.Add("IsID", p.IsID);
                        //what was the xpath for this property?
                        dc.ExtendedProperties.Add("XPath", p.PropertyXPath);
                        //a "short list" property is one that will always show up... it's a shortcut in some places.
                        dc.ExtendedProperties.Add("ShortList", p.ShortList);
                        //it might have a custom caption
                        if (!string.IsNullOrEmpty(p.PropertyLabel)) dc.Caption = p.PropertyLabel;
                        //will we try to draw an icon?
                        if (p.PropertyHasIcon) dc.ExtendedProperties.Add("HasIcon", true);

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
        public string GetCloudObjectsAsXML(string sObjectType, ref string sErr, Dictionary<string, string> sAdditionalArguments)
        {
            acUI.acUI ui = new acUI.acUI();

            string sXML = "";

            string sAccessKeyID = ui.GetCloudLoginID();
            string sSecretAccessKeyID = ui.GetCloudLoginPassword();

            //get the cloud object type from the session
            Globals.CloudObjectType cot = ui.GetCloudObjectType(sObjectType);
            if (cot != null)
            {
                //many reasons why we'd bail here.  Rather than a bunch of testing below, let's just crash
                //if a key field is missing.
                if (string.IsNullOrEmpty(cot.ObjectType))
                { sErr = "Cannot find definition for requested object type [" + sObjectType + "]"; return null; }
                if (string.IsNullOrEmpty(cot.APIHostName))
                { sErr = "APIHostName not defined for requested object type [" + sObjectType + "]"; return null; }
                if (string.IsNullOrEmpty(cot.APICall))
                { sErr = "APICall not defined for requested object type [" + sObjectType + "]"; return null; }
                if (string.IsNullOrEmpty(cot.APIRequestMethod))
                { sErr = "APIRequestMethod not defined for requested object type [" + sObjectType + "]"; return null; }
            }
            else
            {
                sErr = "GetCloudObjectType failed for [" + sObjectType + "]";
                return null;
            }



            //first, this is an explicit list of parameters in a dictionary. 
            //in the real world, we'll probably pull these params from a table
            //or have to parse a querystring
            ParamComparer pc = new ParamComparer();
            SortedDictionary<string, string> sortedRequestParams = new SortedDictionary<string, string>(pc);

            //call specific parameters (this is AWS specific!!!)
            sortedRequestParams.Add("Action", cot.APICall);

            //do we need to apply a group filter?  If it's defined on the table then YES!
            if (!string.IsNullOrEmpty(cot.APIRequestGroupFilter))
            {
                string[] sTmp = cot.APIRequestGroupFilter.Split('=');
                sortedRequestParams.Add(sTmp[0], sTmp[1]);
            }


            if (sAdditionalArguments != null)
            {
                //we have custom arguments... use them
                //for each... add to sortedRequestParams
                //if the same key from the group filter is defined as sAdditionalArguments it overrides the table!
            }


            //AWS auth parameters
            string sDate = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss", DateTimeFormatInfo.InvariantInfo);

            sortedRequestParams.Add("AWSAccessKeyId", sAccessKeyID);
            sortedRequestParams.Add("Version", cot.APIVersion);
            sortedRequestParams.Add("Timestamp", sDate);
            //sortedRequestParams.Add("Expires", date);
            sortedRequestParams.Add("SignatureMethod", "HmacSHA256");
            sortedRequestParams.Add("SignatureVersion", "2");


            string sHostName = cot.APIHostName;

            //now we have all the parameters in a list, build a sorted, encoded querystring string
            string sQueryString = GetSortedParamsAsString(sortedRequestParams, true);
            //then build the full request to be signed
            string sStringToSign = awsComposeStringToSign(cot.APIRequestMethod, sHostName, "", sQueryString);
            //and sign it
            //string sSignature = GetAWS3_SHA1AuthorizationValue(sSecretAccessKeyID, sStringToSign);
            string sSignature = awsGetSHA256AuthorizationValue(sSecretAccessKeyID, sStringToSign);

            //finally, urlencode the signature
            sSignature = PercentEncodeRfc3986(sSignature);


            string sHostURL = "https://" + sHostName + "/";
            string sURL = sHostURL + "?" + sQueryString + "&Signature=" + sSignature;

            try
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



                // Create a request for the URL. 
                //WebRequest request = WebRequest.Create(sAPICall);
                HttpWebRequest request = WebRequest.Create(sURL) as HttpWebRequest;
                request.Method = cot.APIRequestMethod;

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader sr = new StreamReader(dataStream);
                // Read the content.
                sXML = sr.ReadToEnd();

                // Clean up the streams and the response.
                sr.Close();
                response.Close();


            }
            catch (Exception ex)
            {
                throw ex;
            }


            return sXML;
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
