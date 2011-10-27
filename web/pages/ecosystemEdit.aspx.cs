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
using System.Web.UI.HtmlControls;
using System.Web.Services;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;
using ACWebMethods;

namespace Web.pages
{
    public partial class ecosystemEdit : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        string sEcosystemID;
        string sEcoTemplateID;

        protected void Page_Load(object sender, EventArgs e)
        {
            string sErr = "";

            sEcosystemID = (string)ui.GetQuerystringValue("ecosystem_id", typeof(string));

            if (!Page.IsPostBack)
            {
                hidEcosystemID.Value = sEcosystemID;

                if (!GetDetails(ref sErr))
                {
                    ui.RaiseError(Page, "Record not found for ecosystem_id [" + sEcosystemID + "]. " + sErr, true, "");
                    return;
                }
            }
        }

        private bool GetDetails(ref string sErr)
        {
            try
            {
                string sSQL = "select e.ecosystem_id, e.ecosystem_name, e.ecosystem_desc," +
                    " e.account_id, e.ecotemplate_id, et.ecotemplate_name" +
                    " from ecosystem e" +
                    " join ecotemplate et on e.ecotemplate_id = et.ecotemplate_id" +
                    " where e.ecosystem_id = '" + sEcosystemID + "'" +
                    " and e.account_id = '" + ui.GetSelectedCloudAccountID() + "'";

                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr)) return false;

                if (dr != null)
                {
                    sEcoTemplateID = dr["ecotemplate_id"].ToString();
                    hidEcoTemplateID.Value = sEcoTemplateID;
                    lblEcotemplateName.Text = dr["ecotemplate_name"].ToString();

                    txtEcosystemName.Text = dr["ecosystem_name"].ToString();
                    ViewState["txtEcosystemName"] = dr["ecosystem_name"].ToString();
                    txtDescription.Text = dr["ecosystem_desc"].ToString();
                    ViewState["txtDescription"] = dr["ecosystem_desc"].ToString();

                    //the header
                    lblEcosystemNameHeader.Text = dr["ecosystem_name"].ToString();

                    //schedule, only if we are the default.
                    //if (!GetSchedule(ref sErr)) return false;

                    return true;
                }
                else
                {
                    return false;

                }
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                return false;
            }
        }

        private static string DrawAllProperties(DataTable dt, string sObjectID)
        {
            string sHTML = "";

            //what is the name of the first column?
            //all over the place with AWS we hardcode and assume the first column is the 'ID'.
            //BUT WE SHOULD spin the columns looking for the one with the Extended Propoerty that says it's the id
            string sIDColumnName = dt.Columns[0].ColumnName;

            DataRow[] drFound;
            drFound = dt.Select(sIDColumnName + " = '" + sObjectID + "'");

            if (drFound.Count() > 0)
            {
                foreach (DataColumn dcAPIResultsColumn in dt.Columns)
                {
                    //there should be only one row - that's why I'm using the explicit index of 0

                    //AND WE WILL ONLY draw the short list properties here!!!

                    sHTML += DrawProperty(drFound[0], dcAPIResultsColumn.ColumnName);
                }
            }
            else { sHTML += "No data found for " + sObjectID; }
            return sHTML;
        }
        private static string DrawProperty(DataRow dr, string sPropertyName)
        {
            string sHTML = "";
            string sValue = (string.IsNullOrEmpty(dr[sPropertyName].ToString()) ? "" : dr[sPropertyName].ToString());
            string sIcon = "";

            //some fields have a status or other icon.
            //it's noted in the extended properties of the column in the AWS results.
            //we are simply assuming to have an image file for every propery that might have an icon.
            //the images will be named "property_value.png" 
            //NOTE: collapsed for spaces of course... just to be safe
            if (dr.Table.Columns[sPropertyName].ExtendedProperties["HasIcon"] != null)
            {
                if (sValue != "")
                    sIcon = "<img class=\"custom_icon\" src=\"../images/custom/" + sPropertyName.Replace(" ", "") + "_" + sValue.Replace(" ", "") + ".png\" alt=\"\" />";
            }

            sHTML += (sValue == "" ? "" : "<span class=\"ecosystem_item_property\">" + sPropertyName + ": " + sIcon + sValue + "</span>");

            return sHTML;
        }

        [WebMethod(EnableSession = true)]
        public static string wmGetEcosystemObjects(string sEcosystemID)
        {
            dataAccess dc = new dataAccess();

            try
            {
                string sHTML = "";
                string sErr = "";

                string sSQL = "select eo.ecosystem_object_type, cot.label, count(*) as num_objects" +
                    " from ecosystem_object eo" +
                    " join cloud_object_type cot on eo.ecosystem_object_type = cot.cloud_object_type" +
                    " where eo.ecosystem_id ='" + sEcosystemID + "'" +
                    " group by eo.ecosystem_object_type, cot.label" +
                    " order by cot.label";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                    return sErr;


                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {

                        //something here can look up the icon for each type if we wanna do that.
                        string sIcon = "aws_16.png";

                        sIcon = "<img src=\"../images/icons/" + sIcon + "\" alt=\"\" style=\"width: 16px; height: 16px;\" />&nbsp;&nbsp;&nbsp;";

                        string sLabel = sIcon + dr["label"].ToString() + " (" + dr["num_objects"].ToString() + ")";

                        sHTML += "<div class=\"ui-widget-content ui-corner-all ecosystem_type\" id=\"" + dr["ecosystem_object_type"].ToString() + "\" label=\"" + dr["label"].ToString() + "\">";
                        sHTML += "    <div class=\"ecosystem_type_header\">";
                        sHTML += "        <div class=\"ecosystem_item_header_title\">";
                        sHTML += "            <span>" + sLabel + "</span>";
                        sHTML += "        </div>";
                        sHTML += "        <div class=\"ecosystem_item_header_icons\">";
                        //might eventually enable whacking the whole group
                        sHTML += "        </div>";
                        sHTML += "    </div>";
                        sHTML += "    <div class=\"ecosystem_type_detail\" >";
                        //might eventually show some detail
                        sHTML += "    </div>";


                        sHTML += "</div>";

                    }
                }
                else
                {
                    sHTML += "<span>This ecosystem does not contain any Cloud Objects.</span>";
                }

                return sHTML;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public static string wmGetEcosystemObjectByType(string sEcosystemID, string sType)
        {
            dataAccess dc = new dataAccess();
            awsMethods acAWS = new awsMethods();
			acUI.acUI ui = new acUI.acUI();
			
            try
            {
                string sHTML = "";
                string sErr = "";

				//So, we'll first get a distinct list of all clouds represented in this set
				//then for each cloud we'll get the objects.
                string sSQL = "select eo.cloud_id, c.cloud_name" +
                    " from ecosystem_object eo" +
                    " join clouds c on eo.cloud_id = c.cloud_id" +
                    " where eo.ecosystem_id ='" + sEcosystemID + "'" +
                    " and eo.ecosystem_object_type = '" + sType + "'" +
					" group by eo.cloud_id, c.cloud_name" +
                    " order by c.cloud_name";

                DataTable dtClouds = new DataTable();
                if (!dc.sqlGetDataTable(ref dtClouds, sSQL, ref sErr))
                    return sErr;


                if (dtClouds.Rows.Count > 0)
                {
                    foreach (DataRow drCloud in dtClouds.Rows)
                    {
						string sCloudID = drCloud["cloud_id"].ToString();
						string sCloudName = drCloud["cloud_name"].ToString();

		                //get the cloud object rows
		                sSQL = "select eo.ecosystem_object_id, eo.ecosystem_object_type, cot.label" +
		                    " from ecosystem_object eo" +
		                    " join cloud_object_type cot on eo.ecosystem_object_type = cot.cloud_object_type" +
		                    " where eo.ecosystem_id ='" + sEcosystemID + "'" +
		                    " and eo.ecosystem_object_type = '" + sType + "'" +
		                    " and eo.cloud_id = '" + sCloudID + "'" +
							" order by cot.label";
		
		                DataTable dtObjects = new DataTable();
		                if (!dc.sqlGetDataTable(ref dtObjects, sSQL, ref sErr))
		                    return sErr;
		
		
		                if (dtObjects.Rows.Count > 0)
		                {
							//we only need to hit the API once... this result will contain all the objects
		                    //and our DrawProperties will filter the DataTable on the ID.
		                    DataTable dtAPIResults = acAWS.GetCloudObjectsAsDataTable(sCloudID, sType, ref sErr);
		
		                    foreach (DataRow drObject in dtObjects.Rows)
		                    {
		                        //giving each section a guid so we can delete it on the client side after the ajax call.
		                        //not 100% the ecosystem_object_id will always be suitable as a javascript ID.
		                        string sGroupID = ui.NewGUID();
		
		                        sHTML += "<div class=\"ui-widget-content ui-corner-all ecosystem_item\" id=\"" + sGroupID + "\">";
		
		
		                        string sObjectID = drObject["ecosystem_object_id"].ToString();
		
		                        string sLabel = "Cloud: " + sCloudName + " - " + sObjectID;
		
		                        sHTML += "<div class=\"ui-widget-header ecosystem_item_header\">";
		                        sHTML += "<div class=\"ecosystem_item_header_title\"><span>" + sLabel + "</span></div>";
		
		                        sHTML += "<div class=\"ecosystem_item_header_icons\">";
		
		                        sHTML += "<span class=\"ui-icon ui-icon-close ecosystem_item_remove_btn pointer\"" +
									" id_to_delete=\"" + drObject["ecosystem_object_id"].ToString() + "\"" +
									" id_to_remove=\"" + sGroupID + "\">";
		                        sHTML += "</span>";
		
		                        sHTML += "</div>";
		
		                        sHTML += "</div>";
		
		                        //the details section
		                        sHTML += "<div class=\"ecosystem_item_detail\">";
		
		                        if (dtAPIResults != null)
		                        {
		                            if (dtAPIResults.Rows.Count > 0)
		                                sHTML += DrawAllProperties(dtAPIResults, sObjectID);
		                        }
		
		
		                        //end detail section
		                        sHTML += "</div>";
		                        //end block
		                        sHTML += "</div>";
		                    }
		                }
		                else
		                {
		                    sHTML += "<span>This ecosystem does not contain any Cloud Objects.</span>";
		                }

					}
				}
				
				


                return sHTML;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }


    }

}
