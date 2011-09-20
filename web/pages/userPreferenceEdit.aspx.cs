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
using Globals;

namespace Web.pages
{
    public partial class userPreferenceEdit : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        
        acUI.acUI ui = new acUI.acUI();
        string sErr = "";
        string sSQL = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                FillUserPreferences();
            }
        }
        private void FillUserPreferences()
        {

            sSQL = "select username,full_name,authentication_type,email,security_question,security_answer from users where user_id = '" + ui.GetSessionUserID() + "'";

            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }
            else
            {
                if (dr != null)
                {
                    lblName.Text = (object.ReferenceEquals(dr["full_name"], DBNull.Value) ? "" : dr["full_name"].ToString());
                    lblUserName.Text = (object.ReferenceEquals(dr["username"], DBNull.Value) ? "" : dr["username"].ToString());
                    lblAuthenticationType.Text = (object.ReferenceEquals(dr["authentication_type"], DBNull.Value) ? "" : dr["authentication_type"].ToString());
                    txtEmail.Text = (object.ReferenceEquals(dr["email"], DBNull.Value) ? "" : dr["email"].ToString());
                    hidEmail.Value = txtEmail.Text;
                    txtSecurityQuestion.Text = (object.ReferenceEquals(dr["security_question"], DBNull.Value) ? "" : dc.DeCrypt(dr["security_question"].ToString()));
                    string sSecurityAnswer = (object.ReferenceEquals(dr["security_answer"], DBNull.Value) ? "" : dc.DeCrypt(dr["security_answer"].ToString()));
                    if (sSecurityAnswer.Length > 0) { 
                        txtSecurityAnswer.Attributes.Add("value", sSecurityAnswer);
                        hidSecurityAnswer.Value = sSecurityAnswer;
                    };
                    
                    // we know the user has a password, or they would not be on the screen
                    string sPasswordFiller = "($%#d@x!&";
                    txtPassword.Attributes.Add("value", sPasswordFiller);
                    txtPasswordConfirm.Attributes.Add("value", sPasswordFiller);
                    
                    if (lblAuthenticationType.Text == "ldap")
                    {
                        pnlLocalSettings.Visible = false;
                    }


                }
            }


        }
        public void btnSave_Click(object sender, System.EventArgs e)
        {

            // decide what We are updating, its ok to update email everytime, but the password and security answer may not have changed.

            // validation for password match
            if (txtPassword.Text != txtPasswordConfirm.Text)
            {
                ui.RaiseError(Page, "Passwords do not match", true, "");
                return;

            }
            
            sSQL = "update users set email = '" + txtEmail.Text.Replace("'","''") + "'";
            string sPasswordFiller = "($%#d@x!&";
            if (lblAuthenticationType.Text == "local")
            {
                //-------------------------------------------------------------------------------------------------------
                // these settings are only applicable if the user is local
                //only update password if it has been changed.
                sSQL += ",security_question = '" + dc.EnCrypt(txtSecurityQuestion.Text.Replace("'", "''")) + "'";

                
                if (txtPassword.Text != sPasswordFiller)
                {

                    // bugzilla 1347
                    // check the user password history setting, and make sure the password was not used in the past x passwords
                    if (dc.PasswordInHistory(dc.EnCrypt(txtPassword.Text), ui.GetSessionUserID(), ref sErr))
                    {
                        ui.RaiseError(Page, "Passwords can not be reused, choose another password", true, "");
                        return;
                    };
                    if (sErr != "")
                    {
                        ui.RaiseError(Page, sErr, true, "");
                        return;
                    };
                    
                    
                    // make sure the password is valid
                    if (!dc.PasswordIsComplex(txtPassword.Text, ref sErr))
                    {
                        ui.RaiseError(Page, sErr, true, "");
                        return;
                    }
                    sSQL += ",user_password='" + dc.EnCrypt(txtPassword.Text) + "'";
                    
                }

                // only update the security answer if it has changed
                if (txtSecurityAnswer.Text != hidSecurityAnswer.Value)
                {
                    sSQL += ",security_answer='" + dc.EnCrypt(txtSecurityAnswer.Text) + "'";
                }
                //-------------------------------------------------------------------------------------------------------
            }


            sSQL += " where user_id = '" + ui.GetSessionUserID() + "'";

            try
            {
                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                {
                    ui.RaiseError(Page, "Update failed: " + sErr, true, "");
                }

                

                //logging, what else should we log? I guess the fact that the user changed the password would be enough?
                ui.WriteObjectChangeLog(acObjectTypes.User, "User Preferences", "Email", hidEmail.Value, txtEmail.Text);
                // what else should we log? I guess the fact that the user changed the password would be enough?
                if (txtPassword.Text != sPasswordFiller)
                {
                    ui.WriteObjectChangeLog(acObjectTypes.User, ui.GetSessionUserID(), "Password", "User updated password via User Preferences");

                    // add the password update to the history
                    sSQL = "insert user_password_history (user_id, change_time,password) values ('" + ui.GetSessionUserID() + "',now(),'" + dc.EnCrypt(txtPassword.Text) + "')";
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        ui.RaiseError(Page, "User updated, could not add password history: " + sErr, true, "");
                    }
                }
            }
            catch
            {
                ui.RaiseError(Page, "Update failed: " + sErr, true, "");
            }


            txtSecurityAnswer.Attributes.Add("value", txtSecurityAnswer.Text);
            ui.RaiseInfo(Page, "Preferences updated.","");

            // to make everything look right redirect to raw
            //Response.Redirect(Request.RawUrl);


        }
    }
}
