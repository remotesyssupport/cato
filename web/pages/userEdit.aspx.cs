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
using System.Data;
using System.Linq;
using System.Text;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;

namespace Web.pages
{
    public partial class userEdit : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        int iPageSize;

        string sSQL = "";
        string sErr = "";
        public static System.Web.UI.WebControls.Image imgSortDirection;

        protected void Page_Load(object sender, EventArgs e)
        {
            //could be repository settings for default values, and will be options on the page as well
            iPageSize = 50;

            if (!Page.IsPostBack)
            {
                // set the hidden flag that indicates if newly created users must change their password.
                sSQL = "select pass_require_initial_change" +
                    " from login_security_settings" +
                    " where id = 1";
                string sRequire = "0";
                if (!dc.sqlGetSingleString(ref sRequire, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, true, "");
                }
                else
                {
                    if (sRequire == "1")
                    {
                        hidRequirePasswordChange.Value = "yes";
                    }
                }

                // first time on the page, get the sortcolumn last used if one exists.
                string sSettingXML = "";
                sSQL = "select settings_xml from users where user_id = '" + ui.GetSessionUserID() + "'";

                if (!dc.sqlGetSingleString(ref sSettingXML, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, "Unable to get settings for user.<br />", false, sErr);
                }

                //we don't care to do anything if there were no settings
                if (sSettingXML != "")
                {
                    XDocument xDoc = XDocument.Parse(sSettingXML);
                    if (xDoc == null) ui.RaiseError(Page, "XML settings data for user is invalid.", false, "");

                    XElement xSortSettings = xDoc.Descendants("sort").Where(x => (string)x.Attribute("screen") == "user").LastOrDefault();
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

                BindList();
                FillUserRoles();
            }
        }
        private void FillUserRoles()
        {
            /* 4-18-2011 NSC
            The "Security Manager" role cannot create any type of user.  They can on create "Users" and other "Security Managers".
             * 
             * Only "Administrators" can create any type of user.
             */
			
			//temporarily commenting out the notion of an "security manager"
//            if (ui.UserIsInRole("Administrator"))
//            {
            ddlUserRole.Items.Add("Administrator");
            ddlUserRole.Items.Add("Developer");
//            ddlUserRole.Items.Add("Application Manager");
//            ddlUserRole.Items.Add("Security Manager");
            ddlUserRole.Items.Add("User");
//            }
//            else
//            {
//                ddlUserRole.Items.Add("Security Manager");
//                ddlUserRole.Items.Add("User");
//            }
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
                        sWhereString += " and (u.full_name like '%" + aSearchTerms[i] +
                            "%' or u.user_role like '%" + aSearchTerms[i] +
                            "%' or u.username like '%" + aSearchTerms[i] +
                            "%' or u.status like '%" + aSearchTerms[i] +
                            "%' or u.last_login_dt like '%" + aSearchTerms[i] + "%' ) ";
                    }
                }
            }


            sSQL = "select u.user_id,u.username,u.full_name,u.last_login_dt,u.email," +
                "case when u.status = '1' then 'Enabled' when u.status = '-1' then 'Locked'" +
                    " when u.status = '0' then 'Disabled' end as status," +
                " u.authentication_type," +
                " u.user_role as role " +
                " from users u " +
                " where u.status <> 86" +
                sWhereString +
                " order by full_name";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }

            ui.SetSessionObject("UserList", dt, "SelectorListTables");

            //now, actually get the data from the session table and display it
            GetUsers();
        }
        private void GetUsers()
        {
            //here's how the paging works
            //you can get at the data by explicit ranges, or by pages
            //where pages are defined by properties

            //could come from a field on the page
            int iStart = 0;
            int iEnd = 0;

            //this is the page number you want
            int iPageNum = (string.IsNullOrEmpty(hidPage.Value) ? 1 : Convert.ToInt32(hidPage.Value));
            DataTable dtTotal = (DataTable)ui.GetSessionObject("UserList", "SelectorListTables");
            dtTotal.TableName = "UserList";
            DataTable dt = ui.GetPageFromSessionTable(dtTotal, iPageSize, iPageNum, iStart, iEnd, hidSortColumn.Value, hidSortDirection.Value);

            rpUsers.DataSource = dt;
            rpUsers.DataBind();

            // save the users settings for sorting on this page.
            // the same general logic exists in 3 places,
            // based on if the last column sorted was the same, then reverse the order etc.
            ui.SaveUsersSort("user", hidSortColumn.Value, hidSortDirection.Value, ref sErr);
            if (sErr != "")
            {
                ui.RaiseError(Page, sErr, true, "");
            }

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

        #region "Add User Functions"

        [WebMethod(EnableSession = true)]
        public static string SaveNewUser(object[] oUser)
        {

            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            acUI.AppGlobals ag = new acUI.AppGlobals();
            string sSql = null;
            string sErr = null;


            // check the number of properties
            if (oUser.Length != 10) return "Incorrect list of user properties";

            string sLoginID = oUser[0].ToString();
            string sFullName = oUser[1].ToString();
            string sAuthType = oUser[2].ToString();
            string sUserPassword = oUser[3].ToString();
            string sGeneratePW = oUser[4].ToString();
            string sForcePasswordChange = oUser[5].ToString();
            string sUserRole = oUser[6].ToString();
            string sEmail = oUser[7].ToString();
            string sStatus = oUser[8].ToString();
            string sGroupArray = oUser[9].ToString();


            // checks that cant be done on the client side
            // is the name unique?
            string sInuse = "";
            if (!dc.sqlGetSingleString(ref sInuse, "select user_id from users where username = '" + sLoginID.Trim() + "' limit 1", ref sErr))
            {
                return "sErr";
            }
            else
            {
                if (!string.IsNullOrEmpty(sInuse))
                {
                    return "Login ID '" + sLoginID + "' is unavailable, please choose another.";
                }
            }

            // password
            string sPassword = null;
            if (sAuthType == "local")
            {
                if (sGeneratePW == "1") //generate an initial strong password
                	sUserPassword = dc.GenerateNewPassword();

                sPassword = "'" + dc.EnCrypt(sUserPassword) + "'";
            }
            else if (sAuthType == "ldap")
            {
                sPassword = "NULL";
            }
            else
            {
                return "Unknown Authentication Type.";
            }

            // passed client and server validations, create the user
            string sNewUserID = ui.NewGUID();


            try
            {
                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);


                // all good, save the new user and redirect to the user edit page.
                sSql = "insert users" +
                    " (user_id,username,full_name,authentication_type,user_password,force_change,email,status,user_role)" +
                    " values " +
                    "('" + sNewUserID + "','" +
                    sLoginID.Trim().Replace("'", "''") + "'," +
                    "'" + sFullName.Trim().Replace("'", "''") + "'," +
                    "'" + sAuthType + "'," + sPassword + "," +
                    "'" + sForcePasswordChange + "'," +
                    "'" + sEmail.Trim() + "'," +
                    "'" + sStatus + "'" +
                    "'" + sUserRole + "'" +
                    ")";
                oTrans.Command.CommandText = sSql;
                if (!oTrans.ExecUpdate(ref sErr))
                {
                    throw new Exception(sErr);
                }


                #region "groups"
                // add user groups, if there are any
                if (sGroupArray.Length > 0)
                {
                    ArrayList aGroups = new ArrayList(sGroupArray.Split(','));
                    foreach (string sGroupName in aGroups)
                    {
                        sSql = "insert object_tags (object_id, object_type, tag_name)" +
                            " values ('" + sNewUserID + "', 1, '" + sGroupName + "')";
                        oTrans.Command.CommandText = sSql;
                        if (!oTrans.ExecUpdate(ref sErr))
                        {
                            throw new Exception(sErr);
                        }
                    }
                }
                #endregion

                oTrans.Commit();
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }



            // add security log
            ui.WriteObjectAddLog(Globals.acObjectTypes.User, sNewUserID, sFullName.Trim().Replace("'", "''"), "");

            //email out the password
            string sBody = "";
            if (!dc.sqlGetSingleString(ref sBody, "select new_user_email_message from login_security_settings where id = 1", ref sErr))
                throw new Exception(sErr);

            //default message if undefined in the table
            if (string.IsNullOrEmpty(sBody))
                sBody = sFullName + " - an account has been created for you in " + ag.APP_NAME + "." + Environment.NewLine + Environment.NewLine +
                "Your User Name is: " + sLoginID + "." + Environment.NewLine +
                "Your temporary password is: " + sUserPassword + "." + Environment.NewLine;

            //replace our special tokens with the values
            sBody = sBody.Replace("##FULLNAME##", sFullName).Replace("##USERNAME##", sLoginID).Replace("##PASSWORD##", sUserPassword);

            if (!ui.SendEmailMessage(sEmail.Trim(), ag.APP_COMPANYNAME + " Account Management", "Welcome to " + ag.APP_COMPANYNAME, sBody, ref sErr))
                throw new Exception(sErr);

            // no errors to here, so return an empty string

            return "";
        }
        #endregion


        #region "Delete Functions"

        [WebMethod(EnableSession = true)]
        public static string DeleteUsers(string sDeleteArray)
        {
            acUI.acUI ui = new acUI.acUI();

            string sSql = null;
            string sErr = "";

            string WhoAmI = ui.GetSessionUserID();
            try
            {
                ArrayList arrList = new ArrayList();
                arrList.AddRange(sDeleteArray.Split(','));

                if (sDeleteArray.Length < 36)
                    return "";


                StringBuilder sbDeleteNow = new StringBuilder();
                StringBuilder sbDeleteLater = new StringBuilder();
                StringBuilder sbAll = new StringBuilder();
                foreach (string sUserID in arrList)
                {
                    if (sUserID.Length == 36)
                    {
                        //you cannot delete yourself!!!
                        if (sUserID != WhoAmI)
                        {
                            sbAll.Append("'" + sUserID + "',");

                            //this will flag a user for later deletion by the system
                            //it returns the user_id back if it's safe to delete now
                            if (UserHasHistory(sUserID))
                                sbDeleteLater.Append("'" + sUserID + "',");
                            else
                                sbDeleteNow.Append("'" + sUserID + "',");
                        }
                    }
                }

                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                // stuff to delete no matter what...
                if (sbAll.Length != 0)
                {
                    sbAll.Remove(sbAll.Length - 1, 1);

                    ////delete any attributes for these users
                    //sSql = "delete from user_assign_defaults where user_id in (" + sbAll.ToString() + ")";
                    //oTrans.Command.CommandText = sSql;
                    //if (!oTrans.ExecUpdate(ref sErr))
                    //    throw new Exception(sErr);

                    //delete roles
                    sSql = "delete from users_roles where user_id in (" + sbAll.ToString() + ")";
                    oTrans.Command.CommandText = sSql;
                    if (!oTrans.ExecUpdate(ref sErr))
                        throw new Exception(sErr);
                }

                // delete some users...
                if (sbDeleteNow.Length != 0)
                {
                    sbDeleteNow.Remove(sbDeleteNow.Length - 1, 1);

                    sSql = "delete from users where user_id in (" + sbDeleteNow.ToString() + ")";
                    oTrans.Command.CommandText = sSql;
                    if (!oTrans.ExecUpdate(ref sErr))
                        throw new Exception(sErr);
                }

                // flag the others...
                if (sbDeleteLater.Length != 0)
                {
                    sbDeleteLater.Remove(sbDeleteLater.Length - 1, 1);

                    sSql = "update users set status = 86 where user_id in (" + sbDeleteLater.ToString() + ")";
                    oTrans.Command.CommandText = sSql;
                    if (!oTrans.ExecUpdate(ref sErr))
                        throw new Exception(sErr);
                }

                oTrans.Commit();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return "User(s) deleted.";
        }

        public static bool UserHasHistory(string sUserID)
        {
            dataAccess dc = new dataAccess();
            string sSql = "";
            string sErr = "";
            int iResults = 0;

            // history in user_session.
            sSql = "select count(*) from user_session where user_id = '" + sUserID + "'";
            if (!dc.sqlGetSingleInteger(ref iResults, sSql, ref sErr))
                throw new Exception(sErr);

            if (iResults > 0)
                return true;

            // history in user_security_log
            sSql = "select count(*) from user_security_log where user_id = '" + sUserID + "'";
            if (!dc.sqlGetSingleInteger(ref iResults, sSql, ref sErr))
                throw new Exception(sErr);

            if (iResults > 0)
                return true;

            return false;
        }

        #endregion

        #region "User Modify Functions"

        [WebMethod(EnableSession = true)]
        public static string SaveUserEdits(object[] oUser)
        {
            string sChangeDetail = "User Details updated.";

            // verify the right number of properties
            if (oUser.Length != 10) return "Incorrect number of User Properties.";

            string sEditUserID = oUser[0].ToString();
            string sLoginID = oUser[1].ToString();
            string sFullName = oUser[2].ToString();
            string sAuthType = oUser[3].ToString();
            string sUserPassword = oUser[4].ToString();
            string sForcePasswordChange = oUser[5].ToString();
            string sUserRole = oUser[6].ToString();
            string sEmail = oUser[7].ToString();
            string sStatus = oUser[8].ToString();
            //string sGroupArray = oUser[9].ToString();

            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSql = null;
            string sErr = null;

            // checks that cant be done on the client side
            // is the name unique?
            string sInuse = "";
            if (!dc.sqlGetSingleString(ref sInuse, "select user_id from users where username = '" + sLoginID.Trim() + "' and user_id <> '" + sEditUserID + "' limit 1", ref sErr))
            {
                throw new Exception(sErr);
            }
            else
            {
                if (!string.IsNullOrEmpty(sInuse))
                {
                    return "Login ID '" + sLoginID + "' is unavailable, please choose another.";
                }
            }

            // CHANGE Per conference call 5-11-09 we are using a random 9 char mask
            // if the password has not changed this will be the same 9 chars
            string sPasswordUpdate = null;
            bool boolPasswordChanged = false;

            if (sUserPassword == "($%#d@x!&")
            {
                // password has not been touched
                sPasswordUpdate = ",";
                boolPasswordChanged = false;
            }
            else
            {
                // password changed
                sChangeDetail += "  Password changed.";
                if (sAuthType == "local")
                {

                    // bugzilla 1347
                    // check the user password history setting, and make sure the password was not used in the past x passwords
                    if (dc.PasswordInHistory(dc.EnCrypt(sUserPassword.Trim()), sEditUserID, ref sErr))
                    {
                        return "Passwords can not be reused, please choose another password";
                    };
                    if (sErr != null)
                    {
                        return sErr;
                    };

                    if (!dc.PasswordIsComplex(sUserPassword.Trim(), ref sErr))
                    {
                        return sErr;
                    }
                    else
                    {
                        sPasswordUpdate = ",user_password = '" + dc.EnCrypt(sUserPassword.Trim()) + "',";
                        boolPasswordChanged = true;

                    }
                }
                else if (sAuthType == "ldap")
                {
                    sPasswordUpdate = ",user_password = NULL,";
                }
                else
                {
                    return "Unknown Authentication type.";
                }
            }

            try
            {
                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                // update the user fields.
                sSql = "update users set" +
                    " full_name = '" + sFullName + "'," +
                    " username = '" + sLoginID + "'" + sPasswordUpdate +
                    " force_change = '" + sForcePasswordChange + "'," +
                    " authentication_type = '" + sAuthType + "'," +
                    " email = '" + sEmail + "'," +
                    " failed_login_attempts = '0'," +
                    " status = '" + sStatus + "' " +
                    " user_role = '" + sUserRole + "' " +
                    " where user_id = '" + sEditUserID + "'";
                oTrans.Command.CommandText = sSql;
                if (!oTrans.ExecUpdate(ref sErr))
                {
                    throw new Exception(sErr);
                }

                if (boolPasswordChanged)
                {
                    // add Password history if it changed
                    sSql = "insert user_password_history (user_id, change_time,password) values ('" + sEditUserID + "',now(),'" + dc.EnCrypt(sUserPassword.Trim()) + "')";
                    oTrans.Command.CommandText = sSql;
                    if (!oTrans.ExecUpdate(ref sErr))
                    {
                        throw new Exception(sErr);
                    }
                }


                /*#region "tags"
                // remove the existing tags
                sSql = "delete from object_tags where object_id = '" + sEditUserID + "'";
                oTrans.Command.CommandText = sSql;
                if (!oTrans.ExecUpdate(ref sErr))
                {
                    throw new Exception(sErr);
                }

                // add user groups, if there are any
                if (sGroupArray.Length > 0)
                {
                    ArrayList aGroups = new ArrayList(sGroupArray.Split(','));
                    foreach (string sGroupName in aGroups)
                    {
                        sSql = "insert object_tags (object_id, object_type, tag_name)" +
                            " values ('" + sEditUserID + "', 1, '" + sGroupName + "')";
                        oTrans.Command.CommandText = sSql;
                        if (!oTrans.ExecUpdate(ref sErr))
                        {
                            throw new Exception(sErr);
                        }
                    }
                }
                #endregion*/



                oTrans.Commit();
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }

            // add security log
            ui.WriteObjectChangeLog(Globals.acObjectTypes.User, sEditUserID, sFullName.Trim().Replace("'", "''"), sChangeDetail);

            // no errors to here, so return an empty string

            return "";
        }

        [WebMethod(EnableSession = true)]
        public static string ClearFailedAttempts(string sUserID)
        {
            dataAccess dc = new dataAccess();
            string sSQL = null;
            string sErr = null;

            sSQL += "update users set failed_login_attempts = 0 where user_id = '" + sUserID + "'";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                return sErr;
            else
                return "";

        }

        [WebMethod(EnableSession = true)]
        public static string ResetPassword(string sUserID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            acUI.AppGlobals ag = new acUI.AppGlobals();

            string sSQL = null;
            string sErr = null;

            //get the details of this user
            sSQL = "select u.username, u.full_name, u.email, u.authentication_type" +
                " from users u " +
                " where u.user_id = '" + sUserID + "'";
            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                throw new Exception(sErr);

            if (dr != null)
            {
                if (!string.IsNullOrEmpty(dr["email"].ToString()))
                {
                    string sEmail = dr["email"].ToString();
                    string sNewPassword = dc.GenerateNewPassword();

                    sSQL = "update users set user_password = '" + dc.EnCrypt(sNewPassword) + "' where user_id = '" + sUserID + "'";

                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception(sErr);

                    // add security log
                    ui.WriteObjectAddLog(Globals.acObjectTypes.User, sUserID, sUserID, "Password Reset");

                    //email out the password
                    string sBody = "";
                    if (!dc.sqlGetSingleString(ref sBody, "select new_user_email_message from login_security_settings where id = 1", ref sErr))
                        throw new Exception(sErr);

                    //default message if undefined in the table
                    if (string.IsNullOrEmpty(sBody))
                        sBody = dr["full_name"].ToString() + " - your password has been reset by an Administrator." + Environment.NewLine + Environment.NewLine +
                        "Your temporary password is: " + sNewPassword + "." + Environment.NewLine;

                    //replace our special tokens with the values
                    sBody = sBody.Replace("##FULLNAME##", dr["full_name"].ToString()).Replace("##USERNAME##", dr["username"].ToString()).Replace("##PASSWORD##", sNewPassword);

                    if (!ui.SendEmailMessage(sEmail.Trim(), ag.APP_COMPANYNAME + " Account Management", "Account Action in " + ag.APP_NAME, sBody, ref sErr))
                        throw new Exception(sErr);

                }
                else
                {
                    return "Unable to reset - user does not have an email address defined.";
                }
            }

            return "";
        }

        [WebMethod(EnableSession = true)]
        public static string LoadUserFormData(string sUserID)
        {

            dataAccess dc = new dataAccess();
            string sSQL = null;
            string sErr = null;

            StringBuilder sbUserValues = new StringBuilder();

            sSQL = "select u.user_id, u.username, u.full_name, u.status, u.last_login_dt," +
                " u.failed_login_attempts, u.email ,u.authentication_type," +
                " ifnull(u.user_password,'') as user_password," +
                " u.user_role from users u " +
                " where u.user_id = '" + sUserID + "'";
            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }
            else
            {
                if (dr != null)
                {
                    sbUserValues.Append("{");
                    sbUserValues.AppendFormat("\"{0}\" : \"{1}\",", "sEditUserID", dr["username"].ToString());
                    sbUserValues.AppendFormat("\"{0}\" : \"{1}\",", "sLoginID", dr["username"].ToString());
                    sbUserValues.AppendFormat("\"{0}\" : \"{1}\",", "sUserFullName", dr["full_name"].ToString());
                    sbUserValues.AppendFormat("\"{0}\" : \"{1}\",", "sEmail", (object.ReferenceEquals(dr["email"], DBNull.Value) ? "" : dr["email"].ToString()));
                    sbUserValues.AppendFormat("\"{0}\" : \"{1}\",", "sAuthenticationType", (object.ReferenceEquals(dr["authentication_type"], DBNull.Value) ? "" : dr["authentication_type"].ToString()));
                    sbUserValues.AppendFormat("\"{0}\" : \"{1}\",", "sStatus", (object.ReferenceEquals(dr["status"], DBNull.Value) ? "" : dr["status"].ToString()));
                    sbUserValues.AppendFormat("\"{0}\" : \"{1}\",", "sRole", (object.ReferenceEquals(dr["user_role"], DBNull.Value) ? "" : dr["user_role"].ToString()));
                    sbUserValues.AppendFormat("\"{0}\" : \"{1}\",", "sPasswordMasked", "($%#d@x!&");
                    sbUserValues.AppendFormat("\"{0}\" : \"{1}\",", "sFailedLogins", (object.ReferenceEquals(dr["failed_login_attempts"], DBNull.Value) ? "0" : dr["failed_login_attempts"].ToString()));
                    sbUserValues.AppendFormat("\"{0}\" : \"{1}\",", "sUserStatus", (object.ReferenceEquals(dr["status"], DBNull.Value) ? "0" : dr["status"].ToString()));
                    sbUserValues.Append("}");

                }
            }

            return sbUserValues.ToString();

        }
        #endregion

        #region "Buttons"

        protected void btnGetPage_Click(object sender, System.EventArgs e)
        {
            GetUsers();
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
