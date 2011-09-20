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
            //draw the tabs
            sSQL = "select cloud_object_type, label from cloud_object_type order by vendor, api";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, "Unable to get Cloud Object Types.", false, sErr);
            }

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    ltTabs.Text += "<li class=\"group_tab\" object_type=\"" + dr["cloud_object_type"].ToString() + "\">" +
                        dr["label"].ToString() + "</li>";
                }
            }

        }

        [WebMethod(EnableSession = true)]
        public static string wmGetCloudObjectList(string sObjectType)
        {
            acUI.acUI ui = new acUI.acUI();
            awsMethods acAWS = new awsMethods();

            string sXML = "";
            string sErr = "";
            string sHTML = "";

            sXML = acAWS.GetCloudObjectsAsXML(sObjectType, ref sErr, null);
            if (sXML == "")
                sXML = "<span class='ui-state-error'>No data returned from AWS.</span>" + sErr;


            //try a few debugging things:
            //Peek at our object type definition
            sHTML += "<h3>Getting Cloud Object Type definition...</h3><div>";
            CloudObjectType cot = ui.GetCloudObjectType(sObjectType);
            if (cot != null)
            {
                sHTML += cot.AsString();
            }
            else
                sHTML = "<span class='ui-state-error'>GetCloudObjectType failed for [" + sObjectType + "]</span>";

            //this will return false if the object doesn't have enough information to form a call
            if (cot.IsValidForCalls())
            {
                //we have a complete enough object type to make a call.
                //can it be parsed?
                sHTML += "<h3>Parsing XML...</h3><div>";
                sXML = ui.RemoveNamespacesFromXML(sXML);
                XElement xDoc = XElement.Parse(sXML);
                if (xDoc == null)
                    sHTML += "<span class='ui-state-error'>AWS Response XML document is invalid</span>.";
                else
                    sHTML += "Result is valid XML.";
                sHTML += "</div>";




                //test the record xpath
                sHTML += "<h3>Checking RecordXpath [" + cot.XMLRecordXPath + "]...</h3><div>";
                if (cot.XMLRecordXPath != "")
                {
                    XElement xe = xDoc.XPathSelectElement(cot.XMLRecordXPath);
                    if (xe == null)
                        sHTML += "<span class='ui-state-error'>Record XPath [" + cot.XMLRecordXPath + "] was not found.</span>";
                    else
                        sHTML += "Record XPath found a node with [" + xe.Nodes().Count() + "] elements.";
                }
                else
                    sHTML += "Record XPath is not defined.";
                sHTML += "</div>";



                sHTML += "<h3>Result XML</h3><pre><code>";
                sHTML += ui.FixBreaks(ui.SafeHTML(sXML));
                sHTML += "</code></pre>";
            }



            return sHTML;
        }

    }


}
