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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;
using System.Data;
using System.Text;
using Globals;

namespace Web.pages
{
    public partial class cloudEdit : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
				ltClouds.Text = GetClouds("");
 				
				//one time get of the Provider list.
				ltProviders.Text = GetProviders();
           }
        }
		
		public string GetProviders() {
			string sOptionHTML = "";
			
			CloudProviders cp = ui.GetCloudProviders();
			if (cp != null)
			{
				foreach (Provider p in cp.Values) {
					sOptionHTML += "<option value=\"" + p.Name + "\">" + p.Name + "</option>";
				}
			}
		
			return sOptionHTML;
		}
		public string GetClouds(string sSearch) {
            string sSQL = "";
            string sErr = "";
			string sWhereString = "";

            if (sSearch.Length > 0)
            {
                //split on spaces
                int i = 0;
                string[] aSearchTerms = sSearch.Split(' ');
                for (i = 0; i <= aSearchTerms.Length - 1; i++)
                {
                    //if the value is a guid, it's an existing task.
                    //otherwise it's a new task.
                    if (aSearchTerms[i].Length > 0)
                    {
                        sWhereString = " and (cloud_name like '%" + aSearchTerms[i] + "%' " +
                            "or provider like '%" + aSearchTerms[i] + "%' " +
                            "or api_url like '%" + aSearchTerms[i] + "%') ";
                    }
                }
            }

            sSQL = "select cloud_id, cloud_name, provider, api_url" +
                " from clouds" +
                " where 1=1 " + sWhereString +
                " order by provider, cloud_name";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }
			
            string sHTML = "";

            //the first DataColumn is the "id"
            // string sIDColumnName = sDataColumns[0];

            //buld the table
            sHTML += "<table class=\"jtable\" cellspacing=\"1\" cellpadding=\"1\" width=\"99%\">";
            sHTML += "<tr>";
            sHTML += "<th class=\"chkboxcolumn\">";
            sHTML += "<input type=\"checkbox\" class=\"chkbox\" id=\"chkAll\" />";
            sHTML += "</th>";

            sHTML += "<th sortcolumn=\"cloud_name\">Cloud Name</th>";
            sHTML += "<th sortcolumn=\"provider\">Type</th>";
            sHTML += "<th sortcolumn=\"api_url\">URL</th>";

            sHTML += "</tr>";

            //loop rows
            foreach (DataRow dr in dt.Rows)
            {
                sHTML += "<tr account_id=\"" + dr["cloud_id"].ToString() + "\">";
                sHTML += "<td class=\"chkboxcolumn\">";
                sHTML += "<input type=\"checkbox\" class=\"chkbox\"" +
                    " id=\"chk_" + dr[0].ToString() + "\"" +
                    " object_id=\"" + dr[0].ToString() + "\"" +
                    " tag=\"chk\" />";
                sHTML += "</td>";

                sHTML += "<td tag=\"selectable\">" + dr["cloud_name"].ToString() +  "</td>";
                sHTML += "<td tag=\"selectable\">" + dr["provider"].ToString() +  "</td>";
                sHTML += "<td tag=\"selectable\">" + dr["api_url"].ToString() +  "</td>";

                sHTML += "</tr>";
            }

            sHTML += "</table>";
			
			return sHTML;
		}

        #region "Web Methods"
        [WebMethod(EnableSession = true)]
        public static string wmGetClouds(string sSearch)
        {
			cloudEdit ce = new cloudEdit();
            return ce.GetClouds(sSearch);
        }
	
        [WebMethod(EnableSession = true)]
        public static string DeleteClouds(string sDeleteArray)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSql = null;
            string sErr = "";

            if (sDeleteArray.Length < 36)
                return "";

            sDeleteArray = ui.QuoteUp(sDeleteArray);

            DataTable dt = new DataTable();
            // get a list of ids that will be deleted for the log
            sSql = "select cloud_id, cloud_name, provider from clouds where cloud_id in (" + sDeleteArray + ")";
            if (!dc.sqlGetDataTable(ref dt, sSql, ref sErr))
                throw new Exception(sErr);

            try
            {

                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                sSql = "delete from clouds where cloud_id in (" + sDeleteArray + ")";
                oTrans.Command.CommandText = sSql;
                if (!oTrans.ExecUpdate(ref sErr))
                    throw new Exception(sErr);

				//refresh the cloud account list in the session
                if (!ui.PutCloudAccountsInSession(ref sErr))
					throw new Exception(sErr);

                oTrans.Commit();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // if we made it here, so save the logs
            foreach (DataRow dr in dt.Rows)
            {
                ui.WriteObjectDeleteLog(Globals.acObjectTypes.Cloud, dr["cloud_id"].ToString(), dr["cloud_name"].ToString(), dr["provider"].ToString() + " Cloud Deleted.");
            }

            return sErr;
        }

        [WebMethod(EnableSession = true)]
        public static string SaveCloud(string sMode, string sCloudID, string sCloudName, string sProvider, string sAPIUrl)
        {
            // for logging
            string sOriginalName = null;

            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSql = null;
            string sErr = null;


            //if we are editing get the original values
            if (sMode == "edit")
            {
            }

            try
            {
                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                // update the user fields.
                if (sMode == "edit")
                {
	                sSql = "select cloud_name from clouds " +
	                       "where cloud_id = '" + sCloudID + "'";
	                if (!dc.sqlGetSingleString(ref sOriginalName, sSql, ref sErr))
	                    throw new Exception("Error getting original cloud name:" + sErr);
	
                    sSql = "update clouds set" +
                        " cloud_name = '" + sCloudName + "'," +
                        " provider = '" + sProvider + "'," +
                        " api_url = '" + sAPIUrl + "'" +
                        " where cloud_id = '" + sCloudID + "'";

	                oTrans.Command.CommandText = sSql;
	                if (!oTrans.ExecUpdate(ref sErr))
	                    throw new Exception("Error updating cloud: " + sErr);
                	                
					ui.WriteObjectChangeLog(Globals.acObjectTypes.Cloud, sCloudID, sCloudName, sOriginalName, sCloudName);}
                else
                {
					sCloudID = ui.NewGUID();
                    sSql = "insert into clouds (cloud_id, cloud_name, provider, api_url)" +
                    " values ('" + sCloudID + "'," +
                    "'" + sCloudName + "'," +
                    "'" + sProvider + "'," +
                    "'" + sAPIUrl + "')";
	
	                oTrans.Command.CommandText = sSql;
	                if (!oTrans.ExecUpdate(ref sErr))
	                    throw new Exception("Error creating cloud: " + sErr);
	                
					ui.WriteObjectAddLog(Globals.acObjectTypes.Cloud, sCloudID, sCloudName, "Cloud Created");                
				}

                oTrans.Commit();
           }
            catch (Exception ex)
            {
                throw new Exception("Error: General Exception: " + ex.Message);
            }
			
            // no errors to here, so return an empty string
            return "{'cloud_id':'" + sCloudID + "'}";
        }

        [WebMethod(EnableSession = true)]
        public static string LoadCloud(string sID)
        {

            dataAccess dc = new dataAccess();
            string sSql = null;
            string sErr = null;

            string sCloudName = null;
            string sProvider = null;
            string sAPIUrl = null;


            sSql = "select cloud_id, cloud_name, provider, api_url" +
                " from clouds where cloud_id = '" + sID + "'";

            StringBuilder sb = new StringBuilder();
            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSql, ref sErr))
            {
                throw new Exception(sErr);
            }
            else
            {
                if (dr != null)
                {
                    sCloudName = (object.ReferenceEquals(dr["cloud_name"], DBNull.Value) ? "" : dr["cloud_name"].ToString());
                    sProvider = (object.ReferenceEquals(dr["provider"], DBNull.Value) ? "" : dr["provider"].ToString());
                    sAPIUrl = (object.ReferenceEquals(dr["api_url"], DBNull.Value) ? "" : dr["api_url"].ToString());

                    // Return the object as a JSON 

                    sb.Append("{");
                    sb.AppendFormat("\"{0}\" : \"{1}\",", "sCloudName", sCloudName);
                    sb.AppendFormat("\"{0}\" : \"{1}\",", "sProvider", sProvider);
                    sb.AppendFormat("\"{0}\" : \"{1}\",", "sAPIUrl", sAPIUrl);
                    sb.Append("}");

                }
                else
                {
                    sb.Append("{}");
                }

            }

            return sb.ToString();
        }
        #endregion
    }
}
