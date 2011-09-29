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
using System.DirectoryServices;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Globals;
using System.Web.Security;
using System.Data;
using System.IO;

namespace Web
{
    public partial class login : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();
        acUI.AppGlobals ag = new acUI.AppGlobals();

        string sSQL = "";
        string sErr = "";
        string sLoginDefaultError = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            /*
             * 9/8/2011 NSC
             * 
             * This used to be in Globax.asax... so the db settings were only loaded once per startup of IIS.
             * 
             * That was/will cause potential issues with Mono/Apache, so for now we'll just load it here.
             * 
             * This leaves the possibility the environment.txt file could change and leave two different users in different databases. 
             * 
             * at the very least, we won't load it if it's already in Globals.
             * 
             */

            string sName = Globals.DatabaseSettings.DatabaseName;
            if (string.IsNullOrEmpty(sName))
            {
                // This may be the first load, or we may have lost the connection string
                // try to load, and show any error here.
                if (!dc.InitDatabaseSettings(Server.MapPath("conf/cato.conf"), ref sErr))
                {
               		lblErrorMessage.Text = sErr.Replace("\\r\\n", "");
					return;
                }
            }

            //If we are here because of a session error, there will be a querystring message.  Display it.
            lblErrorMessage.Text = ui.GetQuerystringValue("msg", typeof(string)).ToString();

			
			//continue
            ltTitle.Text = ag.APP_NAME;


            //NSC 8/19/2011 commenting out the postback catch.
            //Decided we need to fill these values and reset the session, whether it's a post back or not.
            //visiting the login page resets everything, period.

            //(there was a random error where the login page would be loaded and sitting in a browser,
            // and the server reset while it's sitting there.  Somehow this ended up with IsPostBack being true, but the session 
            // values were empty.  This should fix that.

            //if (!Page.IsPostBack)
            //{
            //FIRST THINGS FIRST... hitting the login page resets the session.
            HttpContext.Current.Session.Clear();


            string sSQL = "select login_message, page_view_logging, report_view_logging, log_days" +
				" from login_security_settings where id = 1";
            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }
            if (dr != null)
            {
                //login message
                lblMessage.Text = dr["login_message"].ToString();
				//put it in the global
                DatabaseSettings.AppInstance = dr["login_message"].ToString();
				
                //page view settings
                //global_view_logging
                if (dc.IsTrue(dr["page_view_logging"].ToString()))
                    ui.SetSessionObject("page_view_logging", "true", "Security");
                else
                    ui.SetSessionObject("page_view_logging", "false", "Security");

                //user page_view logging
                if (dc.IsTrue(dr["report_view_logging"].ToString()))
                    ui.SetSessionObject("report_view_logging", "true", "Security");
                else
                    ui.SetSessionObject("report_view_logging", "false", "Security");

                //log depth
                if (!string.IsNullOrEmpty(dr["log_days"].ToString()))
                    ui.SetSessionObject("log_days", dr["log_days"].ToString(), "Security");
                else
                    ui.SetSessionObject("log_days", "90", "Security");

            }
            //}
        }

        private bool LdapAuthenticate(string sUserID, string sDomain, string sUsername, string sPassword, ref string sErr)
        {

            // using the sDomain string get the server address from the ldap_server setting
            string sLdapServerAddress = "";
            string sSQL = "";
            sSQL = "select address from ldap_domain where ldap_domain = '" + sDomain + "'";
            if (!dc.sqlGetSingleString(ref sLdapServerAddress, sSQL, ref sErr))
            {
                sErr = "Error:" + sErr;
                return false;
            }
            if (string.IsNullOrEmpty(sLdapServerAddress))
            {
                sErr = "No server address for domain:" + sDomain;
                return false;
            }

            // verify the user using ldap
            DirectoryEntry entry = new DirectoryEntry("LDAP://" + sLdapServerAddress, sDomain + "\\" + sUsername, sPassword);

            try
            {

                //object obj = entry.NativeObject;
                DirectorySearcher search = new DirectorySearcher(entry);

                search.Filter = "(SAMAccountName=" + sUsername + ")";
                search.PropertiesToLoad.Add("cn");
                SearchResult result = search.FindOne();

                if ((result == null))
                {
                    sErr = "User " + sUsername + " not found in domain " + sDomain;
                    dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Ldap Error:LDAP://" + sLdapServerAddress + "//" + sDomain + "//" + sUsername + " user not found.", ref sErr);
                    return false;

                }
            }
            catch (Exception ex)
            {

                dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Ldap Error:LDAP://" + sLdapServerAddress + "//" + sDomain + "//" + sUsername + " " + ex.Message.ToString(), ref sErr);

                // use the default message instead of the one returned from the ad inquiry.
                sErr = sLoginDefaultError;
                return false;
            }

            return true;
        }
        private void LoginComplete(string sUserID, string sUserName, ref string sErr)
        {
            string sClientIPAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(sClientIPAddress))
                sClientIPAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];

            sSQL = "update users set failed_login_attempts=0, last_login_dt=now() where user_id='" + sUserID + "'";
            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }
            dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLogin, acObjectTypes.None, "", "Login from [" + sClientIPAddress + "] granted.", ref sErr);


            //put a crap load of static stuff in the session so we can use the values later without hitting the database a lot.

            //***SECURITY COLLECTION
            sSQL = "select full_name, email, last_login_dt from users where user_id = '" + sUserID + "'";
            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }
            if (dr != null)
            {
                //like the user_id
                ui.SetSessionObject("user_id", sUserID, "Security");
                //when we're ready to stop using the session, a global has proven to work.
                //CurrentUser.UserID = sUserID;

                //the full name
                ui.SetSessionObject("user_full_name", dr["full_name"], "Security");

                //the user name
                ui.SetSessionObject("user_name", sUserName, "Security");

                //the IP address
                ui.SetSessionObject("ip_address", sClientIPAddress, "Security");

                //last login
                ui.SetSessionObject("last_login", dr["last_login_dt"], "Security");

                //email
                ui.SetSessionObject("user_email", dr["email"], "Security");

                // user roles
                sSQL = "select role_name from users_roles where user_id = '" + sUserID + "'";
                string sUserRoles = null;
                if (!dc.GetDelimitedData(ref sUserRoles, sSQL, ",", ref sErr, false, false))
                {
                    lblErrorMessage.Text = sErr;
                    return;
                }
                else
                {
                    ui.SetSessionObject("user_roles", sUserRoles, "Security");
                }

                // user groups
                sSQL = "select tag_name from object_tags where object_type = 1 and object_id = '" + sUserID + "'";
                string sUserTags = null;
                if (!dc.GetDelimitedData(ref sUserTags, sSQL, ",", ref sErr, false, false))
                {
                    lblErrorMessage.Text = sErr;
                    return;
                }
                else
                {
                    ui.SetSessionObject("user_tags", sUserTags, "Security");
                }

                // admin emails for error reporting
                sSQL = "select admin_email from messenger_settings where id = 1";
                string sAdminEmail = null;
                if (!dc.sqlGetSingleString(ref sUserRoles, sSQL, ref sErr))
                {
                    lblErrorMessage.Text = sErr;
                    return;
                }
                else
                {
                    ui.SetSessionObject("admin_email", sAdminEmail, "Security");
                }

                // CLOUD ACCOUNTS (data table)
                if (!ui.PutCloudAccountsInSession(ref sErr))
                {
                    lblErrorMessage.Text = sErr;
                    return;
                }


                //CLOUD OBJECT TYPES (custom type)
                if (!ui.PutCloudObjectTypesInSession(ref sErr))
                {
                    lblErrorMessage.Text = sErr;
                    return;
                }


            }
            else
            {
                lblErrorMessage.Text = "Error: Could not determine Name for this login.   Unable to continue.";
                return;
            }
            //***END SECURITY COLLECTION


            //***GENERAL COLLECTION
            //these are settings we use in the gui
            //sSql = "select count(*) from resource_crit_group"
            //SetSessionObject("crit_group_count", dc.sqlGetSingleInteger(sSql, sErr), "General")
            //***END GENERAL COLLECTION

            // last step before authenticationg, update the user_session table
            if (!dc.sqlExecuteUpdate("delete from user_session where user_id = '" + sUserID + "';", ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }
            if (!dc.sqlExecuteUpdate("insert into user_session" +
                " (user_id, address, login_dt, heartbeat, kick)" +
                " values ('" + sUserID + "','" + sClientIPAddress + "', now(), now(), 0)", ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }


            //The GUI does it's own security log table maintenance
            string sLogDays = (string)ui.GetSessionObject("log_days", "Security");
            if (string.IsNullOrEmpty(sLogDays)) sLogDays = "90";

            if (!dc.sqlExecuteUpdate("delete from user_security_log" +
                " where log_dt < date_sub(now(), INTERVAL " + sLogDays + " DAY)", ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }


            // setup the forms authentication
            FormsAuthenticationTicket oTicket = new FormsAuthenticationTicket(1, sUserName, DateTime.Now, DateTime.Now.AddMinutes(30), false, "member");
            HttpContext.Current.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(oTicket)));


            HttpContext.Current.Response.Redirect("~/pages/home.aspx", false);
        }
        private void LoadPasswordRecovery()
        {
            string sAuthenticationType = "";
            //string sEmail = "";
            string sQuestion = "";
            //string sAnswer = "";

            pnlForgotPassword.Visible = true;

            //lblSecurityQuestion
            //

            // 1) make sure the user is valid
            sSQL = "select authentication_type,user_id, security_question,security_answer,email " +
                    "from users where username = '" + txtLoginUser.Text.Replace("'", "''") + "'";


            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }
            else
            {
                if (dr != null)
                {
                    sAuthenticationType = (object.ReferenceEquals(dr["authentication_type"], DBNull.Value) ? "" : dr["authentication_type"].ToString());
                    //sEmail = (object.ReferenceEquals(dr["email"], DBNull.Value) ? "" : dr["email"].ToString());
                    sQuestion = (object.ReferenceEquals(dr["security_question"], DBNull.Value) ? "" : dc.DeCrypt(dr["security_question"].ToString()));
                    //sAnswer = (object.ReferenceEquals(dr["security_answer"], DBNull.Value) ? "" : dr["security_answer"].ToString());
                }
                else
                {
                    // username invalid

                    HttpContext.Current.Response.Redirect("login.aspx", false);
                    return;
                }

            }

            // if we made it here we should have valid user info

            // 2) if the user is authentication_type = ldap
            // then give a message that the user can't change their password
            // in the system, contact the admin
            if (sAuthenticationType == "ldap")
            {
                HttpContext.Current.Response.Redirect("login.aspx?msg=LDAP/Active+Directory+users+can+not+change+their+passwords+in+this+system,+Please+contact+your+system+administrator.", false);
                return;
            }

            // 3) if the user is local and has a secruity q/a then prompt for that
            // and the new password.
            if (sQuestion != "")
            {

                lblSecurityQuestion.Text = sQuestion;
                lblRecoverMessage.Text = "To reset your password, enter the correct security answer and a new password.";
                pnlPasswordReset.Visible = true;
                return;

            }


            // no saved question
            pnlPasswordReset.Visible = false;
            lblRecoverMessage.Text = "No Security Question on file. Please contact your System Administrator to change your password.";
            pnlNoQaOnFile.Visible = true;


            // foregoing option 4 for now, after looking in 3.5.1 it did not exist there, (emailing a new password)



        }
        protected void ForgotPassword_Click(object sender, EventArgs e)
        {

            lblErrorMessage.Text = "";

            // make sure we have a username to query with
            if (txtLoginUser.Text.Length == 0)
            {
                lblErrorMessage.Text = "Enter a valid user for password recovery.";
            }
            else
            {
                lblRecoverUserName.Text = txtLoginUser.Text;

                //hide and show the necessary panels
                pnlLogin.Visible = false;
                LoadPasswordRecovery();

            }

        }
        protected void AttemptLogin()
        {
            string sUserID = null;
            string sUsername = txtLoginUser.Text.Replace("'", "''");
            sUsername = sUsername.Replace(";", "");

            lblErrorMessage.Text = "";

            //first before anything, if the system is flagged as 'down', only "administrators" can log in.
            string sRole = "";
            sSQL = "select role_name from users_roles where user_id = (select user_id from users where username = '" + sUsername + "')";

            if (!dc.sqlGetSingleString(ref sRole, sSQL, ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }

            if (sRole != "Administrator")
            {
                int iAllow = 0;
                sSQL = "select allow_login from login_security_settings limit 1";

                if (!dc.sqlGetSingleInteger(ref iAllow, sSQL, ref sErr))
                {
                    lblErrorMessage.Text = sErr;
                    return;
                }

                if (iAllow == 0)
                {
                    //not an administrator and the system isn't allowing login... too bad :-(
                    lblErrorMessage.Text = "The system is in maintenance mode and not accepting users at this time.";
                    return;
                }
            }


            // get the default message for login errors
            sSQL = "select auth_error_message from login_security_settings";
            sLoginDefaultError = "Invalid User or Password.";
            if (!dc.sqlGetSingleString(ref sLoginDefaultError, sSQL, ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }

            // see if this user is an ldap or local login type
            string sUserAuthType = "";
            sSQL = "select authentication_type from users where username = '" + sUsername + "'";

            if (!dc.sqlGetSingleString(ref sUserAuthType, sSQL, ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }

            if (sUserAuthType == "local")
            {

                //get all of the values from the login_security_settings table
                DataTable dtLoginSecurity = new DataTable();
                sSQL = "Select * from login_security_settings where id = 1";
                if (!dc.sqlGetDataTable(ref dtLoginSecurity, sSQL, ref sErr))
                {
                    lblErrorMessage.Text = sErr;
                    return;
                }
                else
                {
                    // the security settings are required, if no row exists give an error
                    if (dtLoginSecurity.Rows.Count == 0)
                    {
                        dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "System Setup error:no security setting row exists in the login_security_settings table.", ref sErr);
                        lblErrorMessage.Text = "System setup error - no security settings. Contact a System Adminstrator.";
                        return;
                    }
                }


                //string sDecryptedPassword = null;
                string sEncryptedPassword = null;
                string sForceChange = null;
                sSQL = "select user_id, status, failed_login_attempts, user_password, expiration_dt,force_change" +
                    " from users where username='" + sUsername + "'";
                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                {
                    lblErrorMessage.Text = sErr;
                    return;
                }

                if (dr != null)
                {
                    sUserID = dr["user_id"].ToString();
                    int iStatus = 0;
                    int iFailedLogins = 0;
                    iStatus = (int)dr["status"];
                    sForceChange = (string.IsNullOrEmpty(dr["force_change"].ToString()) ? "0" : dr["force_change"].ToString());
                    if ((iStatus != 1))
                    {
                        lblErrorMessage.Text = "Your user account has been locked.";
                        dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Login denied - locked user account.", ref sErr);

                        return;
                    }
                    if (!string.IsNullOrEmpty(dr["failed_login_attempts"].ToString()))
                    {
                        iFailedLogins = (int)dr["failed_login_attempts"];
                    }
                    if (string.IsNullOrEmpty(dr["user_password"].ToString()))
                    {
                        int iHistoryCount = 0;

                        if (!dc.sqlGetSingleInteger(ref iHistoryCount, "select count(*) from user_password_history where user_id = '" + sUserID + "'", ref sErr))
                        {
                            lblErrorMessage.Text = sErr;
                            return;
                        }
                        if (iHistoryCount > 0)
                        {
                            // Intentionally cryptic message because if the users password is NULL and the user has previous passwords 
                            // then the account has probably been hacked and the user is not allowed to login

                            dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Login denied - password is null but history shows previous passwords.  The password should never be NULL.", ref sErr);
                            lblErrorMessage.Text = sLoginDefaultError;
                        }
                    }
                    else
                    {

                        //sDecryptedPassword = dc.DeCrypt(dr["user_password"].ToString());
                        sEncryptedPassword = dr["user_password"].ToString();
                    }
                    if (!string.IsNullOrEmpty(dr["expiration_dt"].ToString()))
                    {
                        System.DateTime dtExpiration = (System.DateTime)dr["expiration_dt"];
                        if (dtExpiration < DateTime.Now)
                        {
                            sSQL = "update users set last_login_dt=now(), status=0 where user_id = '" + sUserID + "'";
                            dc.sqlExecuteUpdate(sSQL, ref sErr);
                            dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Login denied - account is expired.", ref sErr);
                            lblErrorMessage.Text = "Your user account has expired.";
                            return;
                        }
                    }

                    // get the failed_login_attempts value from the centivia parameters
                    string sMaxLoginAttemps = dtLoginSecurity.Rows[0]["pass_max_attempts"].ToString();
                    int iMaxLoginAttempts = Convert.ToInt32(sMaxLoginAttemps);

                    if (iMaxLoginAttempts == 0)
                    {
                        lblErrorMessage.Text = "Error: System setting for max password attemtps is not set. Contact a System Administrator.";
                        return;
                    }

                    if (iFailedLogins > iMaxLoginAttempts)
                    {
                        // Based on the security parameter "Password Policy automatic_lock_reset" (# of minutes)
                        // reset the failed_login_attempts for this user, else lock them 
                        string sAutoResetLock = dtLoginSecurity.Rows[0]["auto_lock_reset"].ToString();
                        int iAutoResetLock = 0;
                        if (int.TryParse(sAutoResetLock, out iAutoResetLock))
                        {

                            sSQL = "select ((DATEDIFF(log_dt, now()) * 24) * 60) AS minutes " +
                                "from user_security_log " +
                                "where user_id = '" + sUserID + "' " +
                                "and log_msg = 'Login denied - auto reset start message.' " +
                                "order by log_id desc limit 1";
                            int iLastFail = 0;
                            if (!dc.sqlGetSingleInteger(ref iLastFail, sSQL, ref sErr))
                            {
                                lblErrorMessage.Text = sErr;
                                return;
                            }
                            if (iLastFail > iAutoResetLock)
                            {

                                sSQL = "update users set failed_login_attempts = 0" +
                                    " where user_id = '" + sUserID + "'";
                                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                                {
                                    lblErrorMessage.Text = sErr;
                                    return;
                                }


                                iFailedLogins = 0;

                                dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Login attempt - failed attempts counter automatically reset per timeout.", ref sErr);

                                // have to remove the start message, so the whole thing starts over the next time
                                sSQL = "delete from user_security_log where user_id = '" + sUserID +
                                    "' and log_msg = 'Login denied - auto reset start message.'";
                                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                                {
                                    lblErrorMessage.Text = sErr;
                                    return;

                                }
                            }
                            else
                            {
                                int iMinutesRemaining = iAutoResetLock - iLastFail;
                                dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Login denied - account is disabled.  Minutes remaining till auto reset:" + iMinutesRemaining, ref sErr);

                                // check to see if a counter message exists for this user,
                                // if not create one.
                                string sExists = null;
                                sSQL = "select log_msg " +
                                    "from user_security_log " +
                                    "where user_id = '" + sUserID + "' " +
                                    "and log_msg = 'Login denied - auto reset start message.' " +
                                    "order by log_id desc";
                                if (!dc.sqlGetSingleString(ref sExists, sSQL, ref sErr))
                                {
                                    lblErrorMessage.Text = sErr;
                                    return;
                                }
                                if (string.IsNullOrEmpty(sExists))
                                {
                                    dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Login denied - auto reset start message.", ref sErr);
                                }

                                lblErrorMessage.Text = "Your user account has been disabled due to numerous failed login attempts.";
                                return;

                            }
                        }
                        else
                        {

                            dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Login denied - account is disabled due to numerous failed login attempts.", ref sErr);
                            lblErrorMessage.Text = "Your user account has been disabled due to numerous failed login attempts.";

                            return;

                        }
                    }
                    else
                    {
                        //what happens if it wasn't a numberic?
                    }

                    string sTestPasswordString = null;
                    if (hidUpdateLater.Value == "later")
                    {
                        sTestPasswordString = hidID.Value;
                    }
                    else
                    {
                        sTestPasswordString = dc.EnCrypt(txtLoginPassword.Text);
                    }


                    if (sEncryptedPassword != sTestPasswordString)
                    {
                        sSQL = "update users set failed_login_attempts=" + (iFailedLogins + 1).ToString() +
                            ", last_login_dt=now() where user_id='" + sUserID + "'";
                        if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        {
                            lblErrorMessage.Text = sErr;
                            return;
                        }
                        lblErrorMessage.Text = sLoginDefaultError;
                        dc.addSecurityLog(sUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Login denied - incorrect password.", ref sErr);
                        return;
                    }

                    // bugzilla 1194 check the password aging.
                    // compare the last password update to the value pass_max_age 
                    // in login_security_settings if the user needs to change their password prompt here. 
                    // this requires to checks, first if the password has expired give a message that
                    // the password must be changed.
                    // if the password expires within the pass_age_warn_days
                    // then give an option to change the password or to change it later.
                    string sMaxPassMaxAge = dtLoginSecurity.Rows[0]["pass_max_age"].ToString();
                    string sPassWarnDays = dtLoginSecurity.Rows[0]["pass_age_warn_days"].ToString();
                    // get the users last password update date. 
                    string sLastPasswordUpdate = null;
                    sSQL = "select change_time from user_password_history where user_id = '" + sUserID + "' order by change_time desc limit 1";
                    if (!dc.sqlGetSingleString(ref sLastPasswordUpdate, sSQL, ref sErr))
                    {
                        lblErrorMessage.Text = sErr;
                        return;
                    }
                    if (string.IsNullOrEmpty(sLastPasswordUpdate))
                    {
                        // no history exists for this user, so what should we do?
						//right now we just allow login, since a new user wouldn't have history anyway.
						//there was discussion about setting sForceChange = "1", but since 
						//creating a new user emails out a random password and set the force_change flag anyway
                    }
                    else
                    {
                        // how many days old is the password
                        System.DateTime dtPasswordAge = Convert.ToDateTime(sLastPasswordUpdate);
                        //string sWarn = DateTime.Now.AddDays(Convert.ToDouble("-" + sMaxPassMaxAge)).ToString();
                        DateTime dtPasswordAgePlus = dtPasswordAge.AddDays(Convert.ToDouble(sMaxPassMaxAge));

                        // if password has expired and must be changed
                        if (dtPasswordAgePlus < DateTime.Now)
                        {
                            lblUserName.Text = sUsername;
                            pnlLogin.Visible = false;
                            pnlForgotPassword.Visible = false;
                            pnlResetPassword.Visible = true;
                            lblResetMessage.Text = "You password has expired and must be changed.";
                            return;
                        }
                        else
                        {
                            // if password expiration date is within the warn days.
                            if (dtPasswordAgePlus < DateTime.Now.AddDays(Convert.ToDouble(sPassWarnDays)))
                            {
                                // if the user selected update later then continue
                                if (hidUpdateLater.Value != "later")
                                {
                                    TimeSpan ts = DateTime.Now.AddDays(Convert.ToDouble(sPassWarnDays)) - dtPasswordAgePlus;

                                    lblWarnUsername.Text = sUsername;
                                    pnlLogin.Visible = false;
                                    pnlForgotPassword.Visible = false;
                                    pnlResetPassword.Visible = false;
                                    pnlExpireWarning.Visible = true;
                                    hidID.Value = sTestPasswordString;

                                    lblExpireWarning.Text = "Your Password expires in " + ts.Days.ToString() + " days.";
                                    return;

                                }
                                else
                                {
                                    txtLoginPassword.Text = dc.DeCrypt(hidID.Value);
                                }
                            }
                        }
                    }




                    // Bugzilla 830, once all validation is done, do a check for password complexity
                    // to make sure the users password meets the rules in the current setting.
                    string sMessage = "";
                    try
                    {
                        bool bComplex = dc.PasswordIsComplex(txtLoginPassword.Text.Replace("'", "''"), ref sMessage);

                        if (!bComplex)
                        {
                            lblUserName.Text = sUsername;
                            pnlLogin.Visible = false;
                            pnlForgotPassword.Visible = false;
                            pnlResetPassword.Visible = true;
                            lblResetMessage.Text = "You must change your password." + sMessage;
                            return;

                        }
                    }
                    catch (Exception ex)
                    {
                        lblErrorMessage.Text = ex.Message;
                    }

                    // if the force_change is set, show the password change screen

                    try
                    {
                        if (sForceChange == "1")
                        {
                            lblUserName.Text = sUsername;
                            pnlLogin.Visible = false;
                            pnlForgotPassword.Visible = false;
                            pnlResetPassword.Visible = true;
                            lblResetMessage.Text = "You must change your password.<br />" + sMessage;
                            return;

                        }
                    }
                    catch (Exception ex)
                    {
                        lblErrorMessage.Text = ex.Message;
                    }



                    //if we got here then we are successful.  Yaaayyyy!
                    LoginComplete(sUserID, sUsername, ref sErr);
                }
                else
                {
                    //cryptic message so they won't know the attempted name is not a valid username.
                    lblErrorMessage.Text = sLoginDefaultError;
                    //addSecurityLog(iUserID, SecurityLogTypes.Security, SecurityLogActions.UserLoginAttempt, acObjectTypes.None, "", "Login attempt - username [" & sUserName & "] not found.")
                    return;

                }
            }
            else if (sUserAuthType == "ldap")
            {

                string[] aUser = sUsername.Split('\\');

                if (aUser.Length == 2)
                {
                    sSQL = "select user_id from users where username='" + sUsername + "'";

                    if (!dc.sqlGetSingleString(ref sUserID, sSQL, ref sErr))
                    {
                        lblErrorMessage.Text = sErr;
                        return;
                    }

                    if (!LdapAuthenticate(sUserID, aUser[0].ToString(), aUser[1].ToString(), txtLoginPassword.Text, ref sErr))
                    {
                        lblErrorMessage.Text = sErr;
                        return;
                    }
                    else
                    {
                        LoginComplete(sUserID, aUser[1].ToString(), ref sErr);
                    }

                }
                else
                {
                    lblErrorMessage.Text = "Error: Unable to locate domain";
                    return;
                }

            }
            else
            {
                lblErrorMessage.Text = "Authentication method not defined.";
                return;
            }
        }
        protected void UpdatePassword(string sCurrentPassword, string sNewPassword, string sNewPasswordConfirm)
        {

            // make sure the new password is actually new.
            // make sure the user entered their existing password.
            if (sCurrentPassword == sNewPassword)
            {
                lblErrorMessage.Text = "New password must be different than the current password.";
                return;
            }

            // make sure the user entered their existing password.
            if (sCurrentPassword.Length == 0)
            {
                lblMessage.Text = "Enter the current password.";
                return;
            }

            // do the new passwords match
            if (sNewPassword != sNewPasswordConfirm)
            {
                lblErrorMessage.Text = "New Passwords do not match.";
                return;
            }

            // is the new password complex enough
            try
            {
                string sMessage = "";
                bool bComplex = dc.PasswordIsComplex(sNewPassword, ref sMessage);

                if (!bComplex)
                {
                    lblErrorMessage.Text = sMessage;
                    return;
                }
            }
            catch (Exception ex)
            {
                lblErrorMessage.Text = ex.Message;
            }




            // make sure the username and password are correct, then update the users password and log them in.
            sSQL = "select user_id, status, failed_login_attempts, user_password, expiration_dt" +
                    " from users where username='" + txtLoginUser.Text.Replace("'", "''").Replace(";", "") + "'";
            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
            {
                lblErrorMessage.Text = sErr;
                return;
            }

            if (dr != null)
            {
                // bugzilla 1347
                // check the user password history setting, and make sure the password was not used in the past x passwords
                if (dc.PasswordInHistory(dc.EnCrypt(sNewPassword), dr["user_id"].ToString(), ref sErr))
                {
                    lblErrorMessage.Text = "Passwords can not be reused. Please choose another password.";
                    return;
                };
                if (sErr != "")
                {
                    lblErrorMessage.Text = sErr;
                    return;
                };



                string sDecryptedPassword = dc.DeCrypt(dr["user_password"].ToString());
                if (sCurrentPassword == sDecryptedPassword)
                {
                    sSQL = "update users set user_password = '" + dc.EnCrypt(sNewPassword) + "', force_change = 0 where user_id = '" + dr["user_id"].ToString() + "'";
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        lblErrorMessage.Text = sErr;
                        return;
                    }
                    else
                    {
                        // add Password history if it changed
                        sSQL = "insert user_password_history (user_id, change_time,password) values ('" + dr["user_id"].ToString() + "',now(),'" + dc.EnCrypt(sNewPassword) + "')";
                        if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        {
                            lblErrorMessage.Text = sErr;
                            return;
                        }
                        else
                        {
                            HttpContext.Current.Response.Redirect("login.aspx?msg=Password+updated,+you+may+now+log+in+with+the+new+password.", false);
                            return;
                        }

                    }

                }
                else
                {
                    lblErrorMessage.Text = "Invalid current password.";
                    return;

                }


            }

        }

        #region "Buttons"
        protected void btnLogin_Click(object sender, System.EventArgs e)
        {
            AttemptLogin();
        }
        protected void btnUpdatePassword_Click(object sender, EventArgs e)
        {

            UpdatePassword(txtCurrentPassword.Text, txtNewPassword.Text, txtNewPasswordConfirm.Text);

        }
        protected void btnResetPassword_Click(object sender, EventArgs e)
        {
            // this is the function that runs if a user has a q/a saved.
            // and they hit save password.

            lblRecoverMessage.Text = "";

            // make sure the new password exists
            if (txtResetPassword.Text.Length == 0)
            {
                lblRecoverMessage.Text = "Please enter a password.";
                return;
            }

            // make sure the new passwords match
            if (txtResetPassword.Text != txtResetPasswordConfirm.Text)
            {
                lblRecoverMessage.Text = "Passwords do not match.";
                return;
            }

            // make sure the answer given matches the answer saved
            string sSecurityAnswer = "";
            sSQL = "Select security_answer from users where username = '" + txtLoginUser.Text.Replace("'", "''").Replace(";", "") + "'";
            if (!dc.sqlGetSingleString(ref sSecurityAnswer, sSQL, ref sErr))
            {
                lblRecoverMessage.Text = sErr;
            }
            else
            {
                sSecurityAnswer = dc.DeCrypt(sSecurityAnswer);
                try
                {
                    if (sSecurityAnswer == txtSecurityAnswer.Text)
                    {
                        // all good so far, check the password complexity, and save, and redirect to the login.
                        if (!dc.PasswordIsComplex(txtResetPassword.Text, ref sErr))
                        {
                            lblRecoverMessage.Text = sErr;
                            return;
                        }
                        else
                        {
                            // update the users password and redirect to the login page.
                            sSQL = "update users set user_password = '" + dc.EnCrypt(txtResetPassword.Text) + "' where username = '" + txtLoginUser.Text.Replace("'", "''").Replace(";", "") + "'";
                            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                            {
                                lblRecoverMessage.Text = sErr;
                                return;
                            }
                            else
                            {
                                // we have to have the user_id to log this, if its not a valid user_id then we can't log it, seems like we need to log
                                // all attempts, but there is a foreign key on user_id in the user_security_log table
                                string sAttemptUserID = null;
                                dc.sqlGetSingleString(ref sAttemptUserID, "Select user_id from users where username = '" + txtLoginUser.Text.Replace("'", "''").Replace(";", "") + "'", ref sErr);
                                if (!string.IsNullOrEmpty(sAttemptUserID))
                                {
                                    dc.addSecurityLog(sAttemptUserID, SecurityLogTypes.Security, SecurityLogActions.UserPasswordChange, acObjectTypes.User, txtLoginUser.Text.Replace("'", "''").Replace(";", ""), "user changed password using the forgot password function.", ref sErr);
                                }
                                HttpContext.Current.Response.Redirect("login.aspx?msg=Password+updated,+you+may+now+log+in+with+the+new+password.", false);
                                return;
                            }
                        }
                    }
                    else
                    {
                        // we have to have the user_id to log this, if its not a valid user_id then we can't log it, seems like we need to log
                        // all attempts, but there is a foreign key on user_id in the user_security_log table
                        string sAttemptUserID = null;
                        dc.sqlGetSingleString(ref sAttemptUserID, "Select user_id from users where username = '" + txtLoginUser.Text.Replace("'", "''").Replace(";", "") + "'", ref sErr);
                        if (!string.IsNullOrEmpty(sAttemptUserID))
                        {
                            dc.addSecurityLog(sAttemptUserID, SecurityLogTypes.Security, SecurityLogActions.UserPasswordChange, acObjectTypes.User, txtLoginUser.Text.Replace("'", "''").Replace(";", ""), "Forgot password attempt, incorrect answer given.", ref sErr);
                        }
                        lblRecoverMessage.Text = "Incorrect Answer.";

                    }
                }
                catch (Exception ex)
                {
                    lblErrorMessage.Text = ex.Message;
                }

            }
        }
        protected void btnCancelPasswordReset_Click(object sender, EventArgs e)
        {
            HttpContext.Current.Response.Redirect("login.aspx", false);
        }
        protected void btnExpiredPassword_Click(object sender, EventArgs e)
        {

            UpdatePassword(txtWarningCurrentPassword.Text, txtWarningNewPassword.Text, txtWarningNewPasswordConfirm.Text);

        }
        protected void btnUpdateLater_Click(object sender, EventArgs e)
        {
            hidUpdateLater.Value = "later";
            txtLoginPassword.Text = txtCurrentPassword.Text;
            //string sTest = txtWarningCurrentPassword.Text;
            AttemptLogin();
        }
        #endregion

    }
}
