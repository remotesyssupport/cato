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

namespace Web.pages
{
    public partial class awsDiscovery : System.Web.UI.Page
    {
        acUI.acUI ui = new acUI.acUI();
        dataAccess dc = new dataAccess();

        string sSQL = "";
        string sErr = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            //Set Ecosystems Dropdown
            BindEcosystem();
			
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

        private static void GetAssociatedEcosystems(ref DataTable dt, string sObjectType)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sErr = "";

            if (dt.Rows.Count > 0)
            {
                //we'll add a column to the output data table
                dt.Columns.Add("Ecosystems");

                //ok, we have some results from AWS.  Let's see if any of them are tied to a ecosystem and if so... note it...
                //spin the AWS results

                //what's in the ecosystem_object table already?
                //get all the ecosystem objects into a table so we can merge it as needed...
                //get the actual rows
                //but only from the selected cloud account
                DataTable dtEcosystemObjects = new DataTable();
                string sSQL = "select do.ecosystem_object_id, d.ecosystem_id, d.ecosystem_name" +
                    " from ecosystem_object do" +
                    " join ecosystem d on do.ecosystem_id = d.ecosystem_id" +
                    " where d.account_id = '" + ui.GetSelectedCloudAccountID() + "'" +
                    " and do.ecosystem_object_type = '" + sObjectType + "'" +
                    " order by do.ecosystem_object_id";

                if (!dc.sqlGetDataTable(ref dtEcosystemObjects, sSQL, ref sErr))
                    throw new Exception(sErr);


                foreach (DataRow dr in dt.Rows)
                {
                    if (!string.IsNullOrEmpty(dr[0].ToString()))
                    {
                        string sResultList = "";

						//aggregate all the id column values into one string
		                string sObjectID = ""; //Possibly a composite of several properties.
						foreach (DataColumn col in dt.Columns)
		                {
							if (col.ExtendedProperties["IsID"] != null) 
								sObjectID += dr[col.ColumnName].ToString();
						}
						
                        //are there any ecosystem objects?
                        if (dtEcosystemObjects != null)
                        {
                            if (dtEcosystemObjects.Rows.Count > 0)
                            {
                                //make an array of any that match
                                DataRow[] drMatches;
                                drMatches = dtEcosystemObjects.Select("ecosystem_object_id = '" + sObjectID + "'");

                                //spin that array and add the names to a string
                                foreach (DataRow drMatch in drMatches)
                                {
                                    string sLink = " <span class=\"ecosystem_link pointer\" ecosystem_id=\"" + drMatch["ecosystem_id"].ToString() + "\">" + drMatch["ecosystem_name"].ToString() + "</span>";
                                    sResultList += (sResultList == "" ? sLink : "," + sLink);
                                }
                            }
                        }

                        //HARDCODED RULE ALERT!  we expect the list of ecosystems to go in the column called "Ecosystems"!
                        dr["Ecosystems"] = sResultList;
                    }
                }
            }
        }

        private void BindEcosystem()
        {
            sSQL = "select ecosystem_id, ecosystem_name" +
				" from ecosystem where account_id = '" + ui.GetSelectedCloudAccountID() + "'";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                ui.RaiseError(Page, "Unable to get Ecosystems for selected Cloud Account.", false, sErr);

            ddlEcosystems.DataSource = dt;
            ddlEcosystems.DataValueField = "ecosystem_id";
            ddlEcosystems.DataTextField = "ecosystem_name";
            ddlEcosystems.DataBind();
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
        public static string wmGetAWSObjectList(string sCloudID, string sObjectType)
        {
            awsMethods acAWS = new awsMethods();

            string sHTML = "";
            string sErr = "";

            DataTable dt = acAWS.GetCloudObjectsAsDataTable(sCloudID, sObjectType, ref sErr);
            if (dt != null)
            {
                if (dt.Rows.Count > 0)
                {
                    GetAssociatedEcosystems(ref dt, sObjectType);
                    sHTML = DrawTableForType(sObjectType, dt);
                }
                else
                    sHTML = "No data returned from the Cloud Provider.";
            }
            else
			{
				sHTML += "<div class=\"ui-widget\" style=\"margin-top: 10px;\">";
				sHTML += "<div style=\"padding: 10px;\" class=\"ui-state-highlight ui-corner-all\">";
				sHTML += "<span style=\"float: left; margin-right: .3em;\" class=\"ui-icon ui-icon-info\"></span>";
				sHTML += "<p>" + sErr + "</p>";
				sHTML += "</div>";
				sHTML += "</div>";
			}

			return sHTML;
        }

        private static string DrawTableForType(string sObjectType, DataTable dt)
        {
            string sHTML = "";

            //the first DataColumn is the "id"
            // string sIDColumnName = sDataColumns[0];

            //buld the table
            sHTML += "<table class=\"jtable\" cellspacing=\"1\" cellpadding=\"1\" width=\"99%\">";
            sHTML += "<tr>";
            sHTML += "<th class=\"chkboxcolumn\">";
            sHTML += "<input type=\"checkbox\" class=\"chkbox\" id=\"chkAll\" />";
            sHTML += "</th>";

            //loop column headers
            foreach (DataColumn dc in dt.Columns)
            {
                sHTML += "<th>";
                sHTML += dc.ColumnName;
                sHTML += "</th>";
            }

            sHTML += "</tr>";

            //loop rows
            foreach (DataRow dr in dt.Rows)
            {
				//aggregate all the id column values into one string
                string sObjectID = ""; //Possibly a composite of several properties.
				foreach (DataColumn dc in dr.Table.Columns)
                {
					if (dc.ExtendedProperties["IsID"] != null) 
						sObjectID += dr[dc.ColumnName].ToString();
				}
				
				//crush the spaces... a javascript ID can't have spaces
				string sJSID = sObjectID.Trim().Replace(" ","");
				
                sHTML += "<tr>";
                sHTML += "<td class=\"chkboxcolumn\">";
                
				//not drawing the checkbox if there's no ID defined, we can't add it to an ecosystem without an id
				if (!string.IsNullOrEmpty(sObjectID))
					sHTML += "<input type=\"checkbox\" class=\"chkbox\"" +
	                    " id=\"chk_" + sJSID + "\"" +
	                    " object_id=\"" + sObjectID + "\"" +
	                    " tag=\"chk\" />";
                
					sHTML += "</td>";

                
					//loop data columns
                foreach (DataColumn dc in dr.Table.Columns)
                {
                    string sValue = dr[dc.ColumnName].ToString();  //yeah, the row item index by the column name... awesome

                    sHTML += "<td>";

                    //should we try to show an icon?
                    if (dc.ExtendedProperties["HasIcon"] != null)
                        sHTML += "<img class=\"custom_icon\" src=\"../images/custom/" + dc.ColumnName.Replace(" ", "").ToLower() + "_" + sValue.Replace(" ", "").ToLower() + ".png\" alt=\"\" />";

                    sHTML += sValue;
                    sHTML += "</td>";
                }

                sHTML += "</tr>";
            }

            sHTML += "</table>";

            return sHTML;
        }
    }


}
