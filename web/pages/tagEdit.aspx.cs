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
    public partial class tagEdit : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        int iPageSize;

        string sSQL = "";
        string sErr = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            iPageSize = 1000;

            if (!Page.IsPostBack)
            {
                BindList();
            }
        }

        private void BindList()
        {

            string sWhereString = "";

            if (txtSearch.Text.Length > 0)
            {
                sWhereString = " and (t.tag_name like '%" + txtSearch.Text + "%' " +
                                    "or t.tag_desc like '%" + txtSearch.Text + "%') ";
            }

            sSQL = "select newid() as id, t.tag_name, t.tag_desc, count(ot.tag_name) as in_use" +
                " from lu_tags t" +
                " left outer join object_tags ot on t.tag_name = ot.tag_name" +
                " where (1=1) " + sWhereString +
                " group by t.tag_name, t.tag_desc" +
                " order by t.tag_name";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }

            ui.SetSessionObject("tagNames", dt, "SelectorListTables");

            //now, actually get the data from the session table and display it
            GetTags();
        }
        private void GetTags()
        {
            //here's how the paging works
            //you can get at the data by explicit ranges, or by pages
            //where pages are defined by properties

            //could come from a field on the page
            int iStart = 0;
            int iEnd = 0;

            //this is the page number you want
            int iPageNum = (string.IsNullOrEmpty(hidPage.Value) ? 1 : Convert.ToInt32(hidPage.Value));
            DataTable dtTotal = (DataTable)ui.GetSessionObject("tagNames", "SelectorListTables");
            dtTotal.TableName = "TagList";
            DataTable dt = ui.GetPageFromSessionTable(dtTotal, iPageSize, iPageNum, iStart, iEnd, hidSortColumn.Value, "");

            rptTags.DataSource = dt;
            rptTags.DataBind();

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
            GetTags();
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
