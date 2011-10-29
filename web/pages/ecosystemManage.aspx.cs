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
using System.Xml.Linq;
using System.Xml.XPath;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;
using System.Data;
using System.Text;

namespace Web.pages
{
    public partial class ecosystemManage : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        int iPageSize;

        string sSQL = "";
        string sErr = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            //could be repository settings for default values, and will be options on the page as well
            iPageSize = 50;

            if (!Page.IsPostBack)
            {
                // first time on the page, get the sortcolumn last used if one exists.
                string sSettingXML = "";
                sSQL = "select settings_xml from users where user_id = '" + ui.GetSessionUserID() + "'";

                if (!dc.sqlGetSingleString(ref sSettingXML, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, "Unable to get settings for user.", false, sErr);
                }

                //we don't care to do anything if there were no settings
                if (sSettingXML != "")
                {
                    XDocument xDoc = XDocument.Parse(sSettingXML);
                    if (xDoc == null) ui.RaiseError(Page, "XML settings data for user is invalid.", false, "");

                    XElement xSortSettings = xDoc.Descendants("sort").Where(x => (string)x.Attribute("screen") == "ecosystem").LastOrDefault();
                    if (xSortSettings != null)
                    {
                        if (xSortSettings.Attribute("sort_column") != null)
                        {
                            hidSortColumn.Value = xSortSettings.Attribute("sort_column").Value.ToString();
                        }
                        if (xSortSettings.Attribute("sort_direction") != null)
                        {
                            hidSortDirection.Value = xSortSettings.Attribute("sort_direction").Value.ToString();
                        }

                    }

                }

                //build the list of eco templates for the create dialog
                sSQL = "select ecotemplate_id, ecotemplate_name from ecotemplate order by ecotemplate_name";
                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, true, "");
                }
                ddlEcotemplates.DataTextField = "ecotemplate_name";
                ddlEcotemplates.DataValueField = "ecotemplate_id";
                ddlEcotemplates.DataSource = dt;
                ddlEcotemplates.DataBind();


                BindList();
            }
        }
        private void BindList()
        {

            string sWhereString = "";

            if (txtSearch.Text.Length > 0)
            {
                //split on spaces
                int i = 0;
                string[] aSearchTerms = txtSearch.Text.Split(' ');
                for (i = 0; i <= aSearchTerms.Length - 1; i++)
                {

                    //if the value is a guid, it's an existing task.
                    //otherwise it's a new task.
                    if (aSearchTerms[i].Length > 0)
                    {
                        sWhereString = " and (a.ecosystem_name like '%" + aSearchTerms[i] +
                           "%' or a.ecosystem_desc like '%" + aSearchTerms[i] + "%' or et.ecotemplate_name like '%" + aSearchTerms[i] + "%' ) ";
                    }
                } 
            }


            sSQL = "select e.ecosystem_id, e.ecosystem_name, e.ecosystem_desc, e.account_id, et.ecotemplate_name" +
                   " from ecosystem e" +
                   " join ecotemplate et on e.ecotemplate_id = et.ecotemplate_id" +
                    " where e.account_id = '" + ui.GetSelectedCloudAccountID() + "'" +
                   sWhereString +
                   " order by e.ecosystem_name";


            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }

            ui.SetSessionObject("EcosystemList", dt, "SelectorListTables");

            //now, actually get the data from the session table and display it
            GetRows();
        }
        private void GetRows()
        {
            //here's how the paging works
            //you can get at the data by explicit ranges, or by pages
            //where pages are defined by properties

            //could come from a field on the page
            int iStart = 0;
            int iEnd = 0;

            //this is the page number you want
            int iPageNum = (string.IsNullOrEmpty(hidPage.Value) ? 1 : Convert.ToInt32(hidPage.Value));
            DataTable dtTotal = (DataTable)ui.GetSessionObject("EcosystemList", "SelectorListTables");
            dtTotal.TableName = "EcosystemList";
            DataTable dt = ui.GetPageFromSessionTable(dtTotal, iPageSize, iPageNum, iStart, iEnd, hidSortColumn.Value, hidSortDirection.Value);

            rpEcosystems.DataSource = dt;
            rpEcosystems.DataBind();


            // save the last sort used
            ui.SaveUsersSort("ecosystem", hidSortColumn.Value, hidSortDirection.Value, ref sErr);


            if ((dt != null))
            {
                if ((dtTotal.Rows.Count > iPageSize))
                {
                    Literal lt = new Literal();
                    lt.Text = ui.DrawPager(dtTotal.Rows.Count, iPageSize, iPageNum);
                    phPager.Controls.Add(lt);
                }
            }
        }

        #region "Buttons"

        protected void btnGetPage_Click(object sender, System.EventArgs e)
        {
            GetRows();
        }
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            // we are searching so clear out the page value
            hidPage.Value = "1";
            BindList();
        }

        #endregion

    }
}
