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
    public partial class systemStatusView : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        string sSQL = "";
        string sErr = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                LoadSystemComponents();
                LoadCurrentUsers();

            }

        }
        private void LoadSystemComponents()
        {

            DataTable dt = new DataTable();
            sSQL = "select app_instance," +
                    " app_name as Component," +
                    " heartbeat as Heartbeat," +
                    " case master when 1 then 'Yes' else 'No' end as Master," +
                    " timestampdiff(MINUTE, heartbeat, now()) as mslr," +
                    " ifnull(logfile_name,'') as logfile_name, platform, hostname" +
                    " from application_registry " +
                    " order by component, master desc";
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }
            else
            {
                rpSystemComponents.DataSource = dt;
                rpSystemComponents.DataBind();
            }


        }
        private void LoadCurrentUsers()
        {

            DataTable dt = new DataTable();
            sSQL = "select u.user_id,u.full_name, us.address, us.login_dt,us.heartbeat as last_update," +
                "case when us.kick = 0 then 'Active' when us.kick = 1 then 'Warning' " +
                "when us.kick = 2 then 'Kicking' when us.kick = 3 then 'Inactive' end as 'kick' " +
                "from user_session us join users u on u.user_id = us.user_id " +
                "where timestampdiff(MINUTE,us.heartbeat, now()) < 10 order by us.heartbeat desc";
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }
            else
            {
                rptUsers.DataSource = dt;
                rptUsers.DataBind();
            }
        }

        protected void rpSystemComponents_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if ((e.Item.ItemType == ListItemType.Item) || (e.Item.ItemType == ListItemType.AlternatingItem))
            {
                DataRowView drv = ((System.Data.DataRowView)(e.Item.DataItem));

                if (drv.Row.ItemArray[5].ToString() != "")
                {
                    string sEncID = dc.EnCrypt(ui.GetSessionUserID());

                    //THIS IS TERRIBLE TO PUT THIS HERE...
                    //but it's temporary.  A ticket exists to give each logserver it's own settings, 
                    //at which time the main query above should join logserver_settings on the "app_name".

                    //can't build the link until we know what port we need.
                    string sSQL = "select port from logserver_settings where id = 1";
                    string sPort = "";
                    dc.sqlGetSingleString(ref sPort, sSQL, ref sErr);

                    if (string.IsNullOrEmpty(sPort))
                        sPort = "4000";

                    string sURL = "http://" +
                        drv.Row.ItemArray[7].ToString() +
                        ":" + sPort+ "/getlog?logtype=service&q=" + sEncID + "&logfile=" +
                        drv.Row.ItemArray[5].ToString();

                    ((HyperLink)e.Item.FindControl("lnkCELogFile")).NavigateUrl = sURL;
                }
                else
                {
                    ((HyperLink)e.Item.FindControl("lnkCELogFile")).Visible = false;
                }

            }

        }
    }
}
