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
using System.Web.Services;

namespace Web.pages
{
    public partial class notificationEdit : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                string sErr = "";
                string sSQL = "select mode_off_on, loop_delay_sec, retry_delay_min, retry_max_attempts," +
                    " smtp_server_addr, smtp_server_user, smtp_server_password, smtp_server_port, from_email, from_name, admin_email" +
                    " from messenger_settings" +
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
                    ddlMessengerOnOff.Text = dr["mode_off_on"].ToString();

                    txtPollLoop.Text = dr["loop_delay_sec"].ToString();

                    txtRetryDelay.Text = dr["retry_delay_min"].ToString();

                    txtRetryMaxAttempts.Text = dr["retry_max_attempts"].ToString();

                    txtSMTPServerAddress.Text = dr["smtp_server_addr"].ToString();

                    txtSMTPUserAccount.Text = dr["smtp_server_user"].ToString();

                    txtSMTPServerPort.Text = dr["smtp_server_port"].ToString();

                    txtFromEmail.Text = dr["from_email"].ToString();

                    txtFromName.Text = dr["from_name"].ToString();

                    txtAdminEmail.Text = dr["admin_email"].ToString();

                }
                else
                {
                    //what to do when it's empty?
                    ui.RaiseError(Page, "Unable to continue.  No data found. " + sErr, true, "");
                }
            }
        }

        [WebMethod(EnableSession = true)]
        public static string SaveNotifications(object[] oAsset)
        {

            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            acUI.AppGlobals ag = new acUI.AppGlobals();

            string sErr = "";
            string sMessengerOnOff = oAsset[0].ToString();
            string sPollLoop = oAsset[1].ToString();
            string sRetryDelay = oAsset[2].ToString();
            string sRetryMaxAttempts = oAsset[3].ToString();
            string sSMTPServerAddress = oAsset[4].ToString().Replace("'", "''");
            string sSMTPUserAccount = oAsset[5].ToString().Replace("'", "''");
            string sSMTPUserPassword = oAsset[6].ToString();
            string sSMTPServerPort = oAsset[7].ToString();
            string sFromEmail = oAsset[8].ToString().Replace("'", "''");
            string sFromName = oAsset[9].ToString().Replace("'", "''");
            string sAdminEmail = oAsset[10].ToString().Replace("'", "''");

            // get the current settings for the logging
            string sOrigMessengerOnOff = "";
            string sOrigPollLoop = "";
            string sOrigRetryDelay = "";
            string sOrigRetryMaxAttempts = "";
            string sOrigSMTPServerAddress = "";
            string sOrigSMTPUserAccount = "";
            string sOrigSMTPServerPort = "";
            string sOrigFromEmail = "";
            string sOrigFromName = "";
            string sOrigAdminEmail = "";


            string sSQL = "select mode_off_on, loop_delay_sec, retry_delay_min, retry_max_attempts," +
                    " smtp_server_addr, smtp_server_user, smtp_server_password, smtp_server_port, from_email, from_name, admin_email" +
                    " from messenger_settings" +
                    " where id = 1";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                return "Unable to continue. " + sErr;
            }
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                sOrigMessengerOnOff = dr["mode_off_on"].ToString();
                sOrigPollLoop = dr["loop_delay_sec"].ToString();
                sOrigRetryDelay = dr["retry_delay_min"].ToString();
                sOrigRetryMaxAttempts = dr["retry_max_attempts"].ToString();
                sOrigSMTPServerAddress = dr["smtp_server_addr"].ToString();
                sOrigSMTPUserAccount = dr["smtp_server_user"].ToString();
                sOrigSMTPServerPort = dr["smtp_server_port"].ToString();
                sOrigFromEmail = dr["from_email"].ToString();
                sOrigFromName = dr["from_name"].ToString();
                sOrigAdminEmail = dr["admin_email"].ToString();
            }

            sSQL = "update messenger_settings set mode_off_on='{0}', loop_delay_sec={1}, retry_delay_min={2}, retry_max_attempts={3}, smtp_server_addr='{4}', smtp_server_user='{5}', smtp_server_port={6}, from_email='{7}', from_name='{8}', admin_email='{9}'";
            //only update password if it has been changed.
            string sPasswordFiller = "($%#d@x!&";
            if (sSMTPUserPassword != sPasswordFiller)
            {
                sSQL += ",smtp_server_password='{10}'";
            }
            sSQL = string.Format(sSQL, sMessengerOnOff, sPollLoop, sRetryDelay, sRetryMaxAttempts, sSMTPServerAddress, sSMTPUserAccount, sSMTPServerPort, sFromEmail, sFromName, sAdminEmail, dc.EnCrypt(sSMTPUserPassword));

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                return "Update failed: " + sErr;
            }
            else
            {
                //logging
                var sLogObject = "Manage Notifications";
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Messenger On / Off", sOrigMessengerOnOff, sMessengerOnOff);
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Poll Loop", sOrigPollLoop, sPollLoop);
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Retry Delay", sOrigRetryDelay, sRetryDelay);
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Retry Max Attempts", sOrigRetryMaxAttempts, sRetryMaxAttempts);
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "SMTP Server Address", sOrigSMTPServerAddress, sSMTPServerAddress);
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "SMTP User Account", sOrigSMTPUserAccount, sSMTPUserAccount);
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "SMTP Server Port", sOrigSMTPServerPort, sSMTPServerPort);
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "From Email", sOrigFromEmail, sFromEmail);
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "From Name", sOrigFromName, sFromName);
                ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "From Name", sOrigAdminEmail, sAdminEmail);

                // send a notification to the user that made the change
                if (sMessengerOnOff == "on")
                {
                    // get the users email, if they do not have an email tell them no message was created.
                    string sUsersEmail = null;
                    string sUserID = ui.GetSessionUserID();
                    sSQL = "select email from users where user_id = '" + sUserID + "'";

                    if (!dc.sqlGetSingleString(ref sUsersEmail, sSQL, ref sErr))
                    {
                        return "Unable to create test email: " + sErr;
                    }
                    string sUserName = "";
                    sUserName = ui.GetSessionUserFullName();

                    if (string.IsNullOrEmpty(sUsersEmail) || sUsersEmail.Length < 5)
                    {
                        // all good, no email so notify user
                        return "Notification settings updated.\n\nNo email on file for user " + sUserName + " - unable to send a test message";
                    }
                    else
                    {
                        // create a test email
                        ui.SendEmailMessage(sUsersEmail, 
                            ag.APP_COMPANYNAME + " Account Management",
                            ag.APP_COMPANYNAME + " Messenger configuration change.",
                            "<html><head></head><body><p>" + sUserName + ",</p><p>This is a test mail to confirm the smtp server that you have configured.</p><p>Do not reply to this message, and feel free to delete it.</p><p>Regards,\n\n" + ag.APP_COMPANYNAME + " Administration team.</p></body></html>", ref sErr);

                        if (sErr != "")
                        {
                            return "Update completed.  Unable to create test message: " + sErr;
                        }
                    }
                    return "Notification settings updated.  A test email will be sent to " + sUsersEmail + ".";
                }
                else
                {
                    return "Notification settings updated.";
                }
            }
        }
    }
}
