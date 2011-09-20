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
using Globals;
using System.Data;

namespace Web.pages
{
    public partial class loginDefaultsEdit : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                string sErr = "";
                string sSQL = "select pass_max_age, pass_max_attempts, pass_max_length, pass_min_length, pass_age_warn_days," +
                    " auto_lock_reset, login_message, auth_error_message, pass_complexity, pass_require_initial_change, pass_history," +
                    " page_view_logging, report_view_logging, allow_login, new_user_email_message" +
                    " from login_security_settings" +
                    " where id = 1";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, "Unable to continue. " + sErr, true, "");
                    return;
                }
                if (dt.Rows.Count > 0)
                {
                    DataRow dr = dt.Rows[0];
                    txtPassMaxAge.Text = dr["pass_max_age"].ToString();
                    ViewState["txtPassMaxAge"] = txtPassMaxAge.Text;
                    txtPassMaxAttempts.Text = dr["pass_max_attempts"].ToString();
                    ViewState["txtPassMaxAttempts"] = txtPassMaxAttempts.Text;
                    txtPassMaxLength.Text = dr["pass_max_length"].ToString();
                    ViewState["txtPassMaxLength"] = txtPassMaxLength.Text;
                    txtPassMinLength.Text = dr["pass_min_length"].ToString();
                    ViewState["txtPassMinLength"] = txtPassMinLength.Text;
                    txtPassAgeWarn.Text = dr["pass_age_warn_days"].ToString();
                    ViewState["txtPassAgeWarn"] = txtPassAgeWarn.Text;
                    txtPasswordHistory.Text = dr["pass_history"].ToString();
                    ViewState["txtPasswordHistory"] = txtPasswordHistory.Text;
                    txtAutoLockReset.Text = dr["auto_lock_reset"].ToString();
                    ViewState["txtAutoLockReset"] = txtAutoLockReset.Text;
                    txtLoginMessage.Text = dr["login_message"].ToString();
                    ViewState["txtLoginMessage"] = txtLoginMessage.Text;
                    txtAuthErrorMessage.Text = dr["auth_error_message"].ToString();
                    ViewState["txtAuthErrorMessage"] = txtAuthErrorMessage.Text;
                    txtNewUserMessage.Text = dr["new_user_email_message"].ToString();
                    ViewState["txtNewUserMessage"] = txtNewUserMessage.Text;


                    //I don't understand what this is doing?
                    cbPassComplexity.Checked = ((int)dr["pass_complexity"] == 1 ? true : false);
                    ViewState["cbPassComplexity"] = dr["pass_complexity"].ToString();
                    cbPassRequireInitialChange.Checked = ((int)dr["pass_require_initial_change"] == 1 ? true : false);
                    ViewState["cbPassRequireInitialChange"] = dr["pass_require_initial_change"];

                    cbPageViewLogging.Checked = ((int)dr["page_view_logging"] == 1 ? true : false);
                    ViewState["cbPageViewLogging"] = dr["page_view_logging"];
                    cbReportViewLogging.Checked = ((int)dr["report_view_logging"] == 1 ? true : false);
                    ViewState["cbReportViewLogging"] = dr["report_view_logging"];

                    cbAllowLogin.Checked = ((int)dr["allow_login"] == 1 ? true : false);
                    ViewState["cbAllowLogin"] = dr["allow_login"];
                }
                else
                {
                    //what to do when it's empty?
                    ui.RaiseError(Page, "Unable to continue.  No data found. " + sErr, true, "");
                }
            }
        }

        public void btnSave_Click(object sender, System.EventArgs e)
        {
            var sSQL = "update login_security_settings set" +
                " pass_max_age='" + txtPassMaxAge.Text + "'," +
                " pass_max_attempts='" + txtPassMaxAttempts.Text + "'," +
                " pass_max_length='" + txtPassMaxLength.Text + "'," +
                " pass_min_length='" + txtPassMinLength.Text + "'," +
                " pass_complexity='" + (cbPassComplexity.Checked ? "1" : "0") + "'," +
                " pass_age_warn_days='" + txtPassAgeWarn.Text + "'," +
                " pass_history = '" + txtPasswordHistory.Text + "'," +
                " pass_require_initial_change='" + (cbPassRequireInitialChange.Checked ? "1" : "0") + "'," +
                " auto_lock_reset='" + txtAutoLockReset.Text + "'," +
                " login_message='" + txtLoginMessage.Text.Replace("'", "''") + "'," +
                " auth_error_message='" + txtAuthErrorMessage.Text.Replace("'", "''").Replace(";", "") + "'," +
                " new_user_email_message='" + txtNewUserMessage.Text.Replace("'", "''").Replace(";", "") + "'," +
                " page_view_logging='" + (cbPageViewLogging.Checked ? "1" : "0") + "'," +
                " report_view_logging='" + (cbReportViewLogging.Checked ? "1" : "0") + "'," +
                " allow_login='" + (cbAllowLogin.Checked ? "1" : "0") + "'" +
                " where id = 1";
            string sErr = "";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                ui.RaiseError(Page, "Update failed: " + sErr, true, "");
            }

            //logging
            var sLogObject = "Login Defaults";
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Password Max Age", ViewState["txtPassMaxAge"].ToString(), txtPassMaxAge.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Password Max Login Attempts", ViewState["txtPassMaxAttempts"].ToString(), txtPassMaxAttempts.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Password Max Length", ViewState["txtPassMaxLength"].ToString(), txtPassMaxLength.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Password Min Length", ViewState["txtPassMinLength"].ToString(), txtPassMinLength.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Password Age Warn Days", ViewState["txtPassAgeWarn"].ToString(), txtPassAgeWarn.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Password History", ViewState["txtPasswordHistory"].ToString(), txtPasswordHistory.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Password Auto Lock Reset", ViewState["txtAutoLockReset"].ToString(), txtAutoLockReset.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Password Login Message", ViewState["txtLoginMessage"].ToString(), txtLoginMessage.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Password Auth Error Message", ViewState["txtAuthErrorMessage"].ToString(), txtAuthErrorMessage.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Password New User Message", ViewState["txtNewUserMessage"].ToString(), txtNewUserMessage.Text);

            if (ViewState["cbPassComplexity"].ToString() != cbPassComplexity.Checked.ToString())
            {
                string sLog = "Set [" + cbPassComplexity.Checked.ToString() + "]";
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Complex Password?", sLog);
            }
            if (ViewState["cbPassRequireInitialChange"].ToString() != cbPassRequireInitialChange.Checked.ToString())
            {
                string sLog = "Set [" + cbPassRequireInitialChange.Checked.ToString() + "]";
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Require Password Initial Change?", sLog);
            }
            if (ViewState["cbPageViewLogging"].ToString() != cbPageViewLogging.Checked.ToString())
            {
                string sLog = "Set [" + cbPageViewLogging.Checked.ToString() + "]";
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Page View Logging?", sLog);
            }
            if (ViewState["cbReportViewLogging"].ToString() != cbReportViewLogging.Checked.ToString())
            {
                string sLog = "Set [" + cbReportViewLogging.Checked.ToString() + "]";
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Report View Logging?", sLog);
            }
            if (ViewState["cbAllowLogin"].ToString() != cbAllowLogin.Checked.ToString())
            {
                string sLog = "Set [" + cbAllowLogin.Checked.ToString() + "]";
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Allow Login?", sLog);
            }
            ui.RaiseInfo(Page, "Login Defaults Updated.", "");
        }
    }
}
