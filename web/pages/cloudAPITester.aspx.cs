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
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Configuration;
using System.IO;
using Globals;
using System.Collections;
using System.Data;
using System.Web.Services;
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Serialization;

namespace Web.pages
{
    public partial class cloudAPITester : System.Web.UI.Page
    {
        acUI.acUI ui = new acUI.acUI();
        dataAccess dc = new dataAccess();

        string sSQL = "";
        string sErr = "";

        protected void Page_Load(object sender, EventArgs e)
        {
			//get the clouds
			BindClouds();
			
            //draw the tabs
			Provider oProvider = ui.GetSelectedCloudProvider();
	        if (oProvider == null)
                ui.RaiseError(Page, "Unable to get Cloud Provider.", false, sErr);

            if (oProvider.Products.Count > 0)
            {
                foreach (Product oProduct in oProvider.Products.Values)
                {
                    ltTabs.Text += "<li class=\"group_header\">" + oProduct.Label + "</li>";
                	
	                foreach (CloudObjectType oCOT in oProduct.CloudObjectTypes.Values)
	                {
	                    ltTabs.Text += "<li class=\"group_tab\" object_type=\"" + oCOT.ID + "\">" + 
	                        oCOT.Label + "</li>";
	                }
                }
            }
        }

        private void BindClouds()
        {
            sSQL = "select cloud_id, cloud_name" +
				" from clouds" +
				" where provider = '" + ui.GetSelectedCloudProviderName() + "'";


            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                ui.RaiseError(Page, "Unable to get Clouds for selected Cloud Account.", false, sErr);

            ddlClouds.DataSource = dt;
            ddlClouds.DataValueField = "cloud_id";
            ddlClouds.DataTextField = "cloud_name";
            ddlClouds.DataBind();
        }

        [WebMethod(EnableSession = true)]
        public static string wmGetCloudObjectList(string sCloudID, string sObjectType)
        {
            acUI.acUI ui = new acUI.acUI();
            awsMethods acAWS = new awsMethods();

            string sXML = "";
            string sErr = "";
            string sHTML = "";
			
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
			
			
            sXML = acAWS.GetCloudObjectsAsXML(sCloudID, cot, ref sErr, null);
			if (!string.IsNullOrEmpty(sErr))
			{
				return "GetCloudObjectsAsXML failed with error: " + sErr;
			}
			if (string.IsNullOrEmpty(sXML))
			{
				return "Cloud connection was successful, but the query returned no data.";
			}



            //try a few debugging things:
            //Peek at our object type definition
            sHTML += "<div class=\"ui-state-default\">Cloud Object Type Definition</div>";
			sHTML += "<div class=\"ui-widget-content\">";
            if (cot != null)
            {
	            string sReq = "<span class=\"ui-widget-content ui-state-error\">required</span>";
				
				//product stuff
	            sHTML += "<span class=\"property\">Product:</span> <span class=\"code\">" + (string.IsNullOrEmpty(cot.ParentProduct.Name) ? sReq : cot.ParentProduct.Name).ToString() + "</span><br />";
	            sHTML += "<span class=\"property\">APIVersion:</span> <span class=\"code\">" + (string.IsNullOrEmpty(cot.ParentProduct.APIVersion) ? sReq : cot.ParentProduct.APIVersion).ToString() + "</span><br />";
				
				//type stuff
	            sHTML += "<span class=\"property\">Name:</span> <span class=\"code\">" + (string.IsNullOrEmpty(cot.ID) ? sReq : cot.ID).ToString() + "</span>";
	            sHTML += "<span class=\"property\">Label:</span> <span class=\"code\">" + cot.Label + "</span><br />";
	            sHTML += "<span class=\"property\">API:</span> <span class=\"code\">" + cot.APICall + "</span>";
	            sHTML += "<span class=\"property\">APIUrlPrefix:</span> <span class=\"code\">" + cot.ParentProduct.APIUrlPrefix.ToString() + "</span>";
	            sHTML += "<span class=\"property\">APICall:</span> <span class=\"code\">" + cot.APICall.ToString() + "</span><br />";
	            sHTML += "<span class=\"property\">APIRequestGroupFilter:</span> <span class=\"code\">" + (string.IsNullOrEmpty(cot.APIRequestGroupFilter) ? "N/A" : cot.APIRequestGroupFilter) + "</span><br />";
	            sHTML += "<span class=\"property\">APIRequestRecordFilter:</span> <span class=\"code\">" + (string.IsNullOrEmpty(cot.APIRequestRecordFilter) ? "N/A" : cot.APIRequestRecordFilter) + "</span><br />";
	            sHTML += "<span class=\"property\">XMLRecordXPath:</span> <span class=\"code\">" + (string.IsNullOrEmpty(cot.XMLRecordXPath) ? sReq : cot.XMLRecordXPath).ToString() + "</span><br />";
	
				sHTML += "<div class=\"properties\">";
	            if (cot.Properties.Count > 0)
	            {
	                foreach (CloudObjectTypeProperty cop in cot.Properties)
	                {
	                    sHTML += "<div class=\"ui-state-default\">" + cop.Name + "</div>";
	                    sHTML += "<div class=\"ui-widget-content ui-corner-bottom\">";
						sHTML += "<span class=\"property\">Label: <span class=\"code\">" + (string.IsNullOrEmpty(cop.Label) ? "N/A" : cop.Label) + "</span></span>";
						sHTML += "<span class=\"property\">XPath: <span class=\"code\">" + cop.XPath + "</span></span>";
						sHTML += "<span class=\"property\">HasIcon: <span class=\"code\">" + cop.HasIcon + "</span></span>";
	                    sHTML += "<span class=\"property\">IsID: <span class=\"code\">" + cop.IsID + "</span></span>";
	                    sHTML += "<span class=\"property\">ShortList: <span class=\"code\">" + cop.ShortList + "</span></span>";
	                    sHTML += "</div>";
	                }
	            }
	            else
	            {
	                sHTML += "<span class=\"ui-widget-content ui-state-error\">At least one Property is required.</span>";
	            }
				sHTML += "</div>";
				
            }
            else
                sHTML = "<span class=\"ui-widget-content ui-state-error\">GetCloudObjectType failed for [" + sObjectType + "].</span>";
			
			//end object type definition box
			sHTML += "</div>";
			
			sHTML += "<hr />";

			
			//API RESULTS
			sHTML += "<div class=\"ui-state-default\">API Results</div>";
			sHTML += "<div class=\"ui-widget-content\">";
			
			//this will return false if the object doesn't have enough information to form a call
            if (cot.IsValidForCalls())
            {
                //we have a complete enough object type to make a call.
                //can it be parsed?

                sXML = ui.RemoveNamespacesFromXML(sXML);
                XElement xDoc = XElement.Parse(sXML);
                if (xDoc == null)
                    sHTML += "<span class=\"ui-widget-content ui-state-error\">Cloud Response XML document is invalid.</span>.";
                else
                    sHTML += "Result is valid XML.";





                //test the record xpath
                sHTML += "<div>Checking Record Xpath [" + cot.XMLRecordXPath + "]... ";
                if (cot.XMLRecordXPath != "")
                {
                    XElement xe = xDoc.XPathSelectElement(cot.XMLRecordXPath);
                    if (xe == null) {
                        sHTML += "<span class=\"ui-state-info\">Record XPath [" + cot.XMLRecordXPath + "] was not found.</span><br />";
                        sHTML += "<span class=\"ui-state-info\">(This may be a normal condition if the Cloud doesn't contain any objects of this type.)</span>";
					}
					else
                        sHTML += "Record XPath matched [" + xe.Nodes().Count() + "] items.";
                }
                else
                    sHTML += "Record XPath is not defined.";
                sHTML += "</div>";



                sHTML += "<div class=\"ui-state-default\"><span id=\"api_results_toggler\" class=\"ui-icon-circle-triangle-e ui-icon floatleft\"></span>Result XML</div>";
				sHTML += "<div id=\"api_results_div\" class=\"hidden\">";
				sHTML += "<pre><code>";
                sHTML += ui.FixBreaks(ui.SafeHTML(sXML));
                sHTML += "</code></pre>";
                sHTML += "</div>";
			}
			else 
			{
                sHTML = "<span class=\"ui-widget-content ui-state-error\">Cloud Object Type definition for [" + sObjectType + "] is incomplete.</span>";				
			}
			
			//end API RESULTS
			sHTML += "</div>";


            return sHTML;
        }

    }


}
