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
using System.Web.Security;
using System.Data;


namespace Web
{
    public partial class sitemaster : System.Web.UI.MasterPage
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();
        public acUI.AppGlobals ag = new acUI.AppGlobals();

        public string sUserID = "";
        protected void Page_Init(object sender, System.EventArgs e)
        {
            //check if the user is logged in.
            sUserID = ui.GetSessionUserID();
            if (string.IsNullOrEmpty(sUserID)) ui.Logout("");
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                // Role based menus
                //hide everything, then turn on as roles define
                pnlInformation.Visible = false;
                pnlSecurity.Visible = false;
                pnlProcedures.Visible = false;
                //pnlAssets.Visible = false;
                pnlConfig.Visible = false;

                if (ui.UserIsInRole("User"))
                {
                    //nothing special for users yet
                }
                if (ui.UserIsInRole("Security Manager"))
                {
                    pnlSecurity.Visible = true;
                }
                /*if (ui.UserIsInRole("Application Manager"))
                {
                    pnlApplications.Visible = true;
                }*/
                if (ui.UserIsInRole("Developer"))
                {
                    pnlInformation.Visible = true;
                    pnlProcedures.Visible = true;
                    //pnlAssets.Visible = true;
                }
                if (ui.UserIsInRole("Administrator"))
                {
                    pnlInformation.Visible = true;
                    pnlSecurity.Visible = true;
                    pnlProcedures.Visible = true;
                    //pnlAssets.Visible = true;
                    pnlConfig.Visible = true;
                }

                /*
                 * NOW! this app has a lot of pages, and each one needs to check whether or not the current user
                 * has the 'privilege' of viewing this page.
                 * 
                 * Rather than put code on each page to check the role, we'll just put it here and look 
                 * in a master list.
                 * */

                ui.IsPageAllowed("");

                //update the session on each page load
                string sMsg = "";
                ui.UpdateUserSession(ref sMsg);

                if (!string.IsNullOrEmpty(sMsg))
                {
                    sMsg = "showInfo('" + sMsg.Replace("'", "\\'") + "');";
                    System.Web.UI.ScriptManager.RegisterStartupScript(Page, this.GetType(), "Alert", sMsg, true);
                }

                //log page views if logging is enabled
                ui.addPageViewLog();


                //fill the clould account drop down
                DataTable dtCloudAccounts = (DataTable)ui.GetSessionObject("cloud_accounts_dt", "Security");
                if (dtCloudAccounts != null)
                {
                    if (dtCloudAccounts.Rows.Count > 0)
                    {
                        ddlCloudAccounts.DataTextField = "account_label";
                        ddlCloudAccounts.DataValueField = "account_id";
                        ddlCloudAccounts.DataSource = dtCloudAccounts;
                        ddlCloudAccounts.DataBind();

                        ddlCloudAccounts.SelectedValue = ui.GetSessionObject("cloud_account_id", "Security").ToString();
                    }
                }
            }
        }
        protected void Logout_Click(object sender, EventArgs e)
        {
            string sUserName = "";
            string sErr = "";
            dc.addSecurityLog(sUserID, Globals.SecurityLogTypes.Security, Globals.SecurityLogActions.UserLogout, Globals.acObjectTypes.None, sUserName, "Manual Log Out", ref sErr);
            ui.SetCookie("user_id", "", "");
            FormsAuthentication.SignOut();
            FormsAuthentication.RedirectToLoginPage();
        }

    }
}
