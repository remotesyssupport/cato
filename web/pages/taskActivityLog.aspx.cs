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
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Text;

namespace Web.pages
{
    public partial class taskActivityLog : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();
        int iPageSize;
        string sNumRecords;
        string sSQL = "";
        string sErr = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            //page size should be a system setting
            iPageSize = 50;

            //so should this default value
            sNumRecords = "1000";

            //if the field is empty or < 0, use the default otherwise use the user provided value.
            if (string.IsNullOrEmpty(txtResultCount.Text) || Convert.ToInt32(txtResultCount.Text) < 0)
                txtResultCount.Text = sNumRecords;
            else
                sNumRecords = txtResultCount.Text;

            if (!Page.IsPostBack)
            {
                //if it's not a postback, use any passed in search string
                string sSearchQS = (string)ui.GetQuerystringValue("search", typeof(string));
                if (txtSearch.Text.Length == 0 && sSearchQS.Length > 0)
                    txtSearch.Text = sSearchQS;

                BindList();
            }

        }
        private void BindList()
        {
            string sWhereString = "";

            // was there a search where passed in?
            sWhereString = (string)ui.GetQuerystringValue("status", typeof(string));
            sWhereString.Replace(";", "");

            Hashtable htValidValues = new Hashtable();
            htValidValues.Add("value1", "processing");
            htValidValues.Add("value2", "submitted");
            htValidValues.Add("value3", "pending");
            htValidValues.Add("value4", "aborting");
            htValidValues.Add("value5", "queued");
            htValidValues.Add("value7", "completed");
            htValidValues.Add("value8", "cancelled");
            htValidValues.Add("value9", "error");
            htValidValues.Add("value10", "staged");


            string sSearchString = " where (1=1) ";
            if (txtSearch.Text.Length > 0)
            {
                sSearchString += " and (ti.task_instance like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or ti.task_id like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or ti.asset_id like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or ti.pid like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or ti.task_status like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or ar.hostname like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or a.asset_name like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or t.task_name like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or t.version like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or u.username like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or u.full_name like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or d.ecosystem_name like '%" + txtSearch.Text.Replace("'", "''") + "%' " +
                                "or si.schedule_instance_name like '%" + txtSearch.Text.Replace("'", "''") + "%') ";
            }

            if (txtStartDate.Text.Length == 0)
                txtStartDate.Text = DateTime.Now.AddDays(-30).ToShortDateString();
            //str_to_date('4/9/2011', '%m/%d/%Y')
            sSearchString += " and (submitted_dt >= str_to_date('" + txtStartDate.Text + "', '%m/%d/%Y')" +
                " or started_dt >= str_to_date('" + txtStartDate.Text + "', '%m/%d/%Y')" +
                " or completed_dt >= str_to_date('" + txtStartDate.Text + "', '%m/%d/%Y')) ";

            if (txtStopDate.Text.Length > 0)
            {
                sSearchString += " and (submitted_dt <= str_to_date('" + txtStopDate.Text + "', '%m/%d/%Y'" +
                " or started_dt <= str_to_date('" + txtStopDate.Text + "', '%m/%d/%Y'" +
                " or completed_dt <= str_to_date('" + txtStopDate.Text + "', '%m/%d/%Y') ";
            }



            if (sWhereString.Length > 0)
            {
                // to prevent sql injection dont take the where string as is, dice it up
                // and only accept valid values
                StringBuilder sb = new StringBuilder();
                string[] sWhereArray = sWhereString.Split(',');
                foreach (string part in sWhereArray)
                {
                    if (htValidValues.ContainsValue(part))
                    {
                        sb.Append("'" + part + "',");
                    }
                }

                if (sb.Length > 1) sb.Remove(sb.Length - 1, 1);

                // if after all of this, there is now string then sWhere will = " where ti.task_status in ('')
                if (sb.Length == 0)
                {
                    sWhereString = " and ti.task_status in ('')";
                }
                else
                {
                    sWhereString = " and ti.task_status in (" + sb.ToString() + ")";
                }

            }

            string sTagString = "";
            if (!ui.UserIsInRole("Developer") && !ui.UserIsInRole("Administrator"))
            {
                sTagString+= " join object_tags tt on t.original_task_id = tt.object_id" +
                    " join object_tags ut on ut.tag_name = tt.tag_name" +
                    " and ut.object_type = 1 and tt.object_type = 3" +
                    " and ut.object_id = '" + ui.GetSessionUserID() + "'";
            }

            sSQL = "select ti.task_instance, t.task_id, t.task_code, a.asset_name," +
                    " ti.pid as process_id, ti.task_status, t.task_name," +
                    " ifnull(u.full_name, s.schedule_name) as started_by," +
                    " t.version, u.full_name, ar.hostname as ce_name, ar.platform as ce_type," +
                    " d.ecosystem_name, d.ecosystem_id," +
                    " convert(ti.submitted_dt, CHAR(20)) as submitted_dt," +
                    " convert(ti.started_dt, CHAR(20)) as started_dt," +
                    " convert(ti.completed_dt, CHAR(20)) as completed_dt" +
                    " from tv_task_instance ti" +
                    " left join task t on t.task_id = ti.task_id" +
                    sTagString +
                    " left outer join application_registry ar on ti.ce_node = ar.id" +
                    " left outer join ecosystem d on ti.ecosystem_id = d.ecosystem_id" +
                    " left join users u on u.user_id = ti.submitted_by" +
                    " left join schedule_instance si on ti.schedule_instance = si.schedule_instance" +
                    " left join asset a on a.asset_id = ti.asset_id" +
                    " left join schedule s on si.schedule_id = s.schedule_id" +
                    sSearchString.Replace(";", "") + sWhereString + 
                    " order by ti.task_instance desc" +
                    " limit " + sNumRecords;




            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }

            ui.SetSessionObject("TaskInstanceList", dt, "TaskInstanceTables");

            //now, actually get the data from the session table and display it
            GetTasks();
        }
        private void GetTasks()
        {
            //here's how the paging works
            //you can get at the data by explicit ranges, or by pages
            //where pages are defined by properties

            //could come from a field on the page
            int iStart = 0;
            int iEnd = 0;

            //this is the page number you want
            int iPageNum = (string.IsNullOrEmpty(hidPage.Value) ? 1 : Convert.ToInt32(hidPage.Value));
            DataTable dtTotal = (DataTable)ui.GetSessionObject("TaskInstanceList", "TaskInstanceTables");
            dtTotal.TableName = "TaskInstanceList";
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
            GetTasks();
        }
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            // we are searching so clear out the page value
            hidPage.Value = "1";
            BindList();
        }
        protected void rpTaskInstances_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if ((e.Item.ItemType == ListItemType.Item) || (e.Item.ItemType == ListItemType.AlternatingItem))
            {
                DataRowView drv = ((System.Data.DataRowView)(e.Item.DataItem));

                //if we are a developer or administrator, show a link to jump to the task edit page.
                if (ui.UserIsInRole("Developer") || ui.UserIsInRole("Administrator"))
                {
                    ((HyperLink)e.Item.FindControl("lnkEdit")).NavigateUrl = "taskEdit.aspx?task_id=" + drv.Row.ItemArray[1].ToString();
                }
                else
                {
                    ((HyperLink)e.Item.FindControl("lnkEdit")).Visible = false;
                }
            }

        }

    }
}
