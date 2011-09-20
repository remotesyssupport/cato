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
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Web.pages
{
    public partial class securityLogView : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();
        int iPageSize;
        string sNumRecords;
        string sSQL = "";
        string sErr = "";
        string sObjectType = "";
        string sObjectID = "";
        string sLogin = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            /*OK this is gonna suck
             * This page is useful to several different roles.  Develoeprs can see the task "change" log.
             * User Managers can see the "Security Log"
             * 
             * So, I can't just block the whole page by role.  It has to be limited in functionality.
             * 
             * Or maybe not.  We shall see.
             */
            ui.IsPageAllowed("");


            //page size should be a system setting
            iPageSize = 50;

            //so should this default value
            sNumRecords = "1000";

            //if the field is empty or < 0, use the default otherwise use the user provided value.
            if (string.IsNullOrEmpty(txtResultCount.Text) || Convert.ToInt32(txtResultCount.Text) < 0)
                txtResultCount.Text = sNumRecords;
            else
                sNumRecords = txtResultCount.Text;

            sObjectID = (string)ui.GetQuerystringValue("id", typeof(string));
            sObjectType = (string)ui.GetQuerystringValue("type", typeof(string));

            // bugzilla 1119 special case for the login security log
            sLogin = (string)ui.GetQuerystringValue("login", typeof(string));

            if (!Page.IsPostBack)
            {

                BindList();
            }

        }
        private void BindList()
        {
            string sWhereString = "(1=1)";

            if (sObjectID.Length > 0)
            {
                sWhereString += " and usl.object_id = '" + sObjectID + "'";
            }

            if (sObjectType.Length > 0)
            {
                sWhereString += " and usl.object_type = '" + sObjectType + "'";
            }

            // bugzilla 1119 special case for the login security log
            // no objectid or type is passed
            if (sLogin.Length > 0)
            {
                sWhereString += " and usl.action like ('userlog%')";
            }

            string sDateSearchString = "";
            string sTextSearch = "";
            if (txtSearch.Text.Length > 0)
            {
                sTextSearch += " and (usl.log_dt like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or u.full_name like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or usl.log_msg like '%" + txtSearch.Text.Replace("'", "''") + "%') ";
            }

            if (txtStartDate.Text.Length == 0)
                txtStartDate.Text = DateTime.Now.AddDays(-30).ToShortDateString();

            sDateSearchString += " and usl.log_dt >= str_to_date('" + txtStartDate.Text + "', '%m/%d/%Y')";

            if (txtStopDate.Text.Length > 0)
            {
                sDateSearchString += " and usl.log_dt <= str_to_date('" + txtStopDate.Text + "', '%m/%d/%Y')";
            }

            sSQL = "select FormatTextToHTML(usl.log_msg) as log_msg," +
                " convert(usl.log_dt, CHAR(20)) as log_dt, u.full_name" +
                    " from user_security_log usl" +
                    " join users u on u.user_id = usl.user_id" +
                    " where " + sWhereString + sDateSearchString + sTextSearch +
                    " order by usl.log_id desc" +
                    " limit " + sNumRecords;

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }

            ui.SetSessionObject("SecurityLog", dt, "SecurityLog");

            //now, actually get the data from the session table and display it
            GetLog();
        }
        private void GetLog()
        {
            //here's how the paging works
            //you can get at the data by explicit ranges, or by pages
            //where pages are defined by properties

            //could come from a field on the page
            int iStart = 0;
            int iEnd = 0;

            //this is the page number you want
            int iPageNum = (string.IsNullOrEmpty(hidPage.Value) ? 1 : Convert.ToInt32(hidPage.Value));
            DataTable dtTotal = (DataTable)ui.GetSessionObject("SecurityLog", "SecurityLog");
            dtTotal.TableName = "AssetList";
            DataTable dt = ui.GetPageFromSessionTable(dtTotal, iPageSize, iPageNum, iStart, iEnd, hidSortColumn.Value, "");

            rpTaskInstances.DataSource = dt;
            rpTaskInstances.DataBind();

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
        protected void btnGetPage_Click(object sender, System.EventArgs e)
        {
            GetLog();
        }
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            // we are searching so clear out the page value
            hidPage.Value = "1";
            BindList();
        }
    }
}
