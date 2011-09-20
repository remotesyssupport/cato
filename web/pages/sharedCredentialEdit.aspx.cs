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
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;
using System.Data;
using System.Text;

namespace Web.pages
{
    public partial class sharedCredentialEdit : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        int iPageSize;

        string sSQL = "";
        string sErr = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            iPageSize = 50;

            if (!Page.IsPostBack)
            {
                BindList();
                //FillAttributeDDL();
                //FillOwnersDDL();
            }
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
                        sWhereString = " and (credential_name like '%" + aSearchTerms[i] + "%' " +
                            "or username like '%" + aSearchTerms[i] + "%' " +
                            "or domain like '%" + aSearchTerms[i] + "%' " +
                            "or shared_cred_desc like '%" + aSearchTerms[i] + "%') ";
                    }
                }
            }

            sSQL = "select credential_id, credential_name, username, domain, shared_cred_desc" +
                " from asset_credential" +
                " where shared_or_local = 0 " + sWhereString +
                " order by credential_name";



            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }

            ui.SetSessionObject("CredentialList", dt, "SelectorListTables");

            //now, actually get the data from the session table and display it
            GetCredentials();
        }
        private void GetCredentials()
        {
            //here's how the paging works
            //you can get at the data by explicit ranges, or by pages
            //where pages are defined by properties

            //could come from a field on the page
            int iStart = 0;
            int iEnd = 0;

            //this is the page number you want
            int iPageNum = (string.IsNullOrEmpty(hidPage.Value) ? 1 : Convert.ToInt32(hidPage.Value));
            DataTable dtTotal = (DataTable)ui.GetSessionObject("CredentialList", "SelectorListTables");
            dtTotal.TableName = "CredentialList";
            DataTable dt = ui.GetPageFromSessionTable(dtTotal, iPageSize, iPageNum, iStart, iEnd, hidSortColumn.Value, "");

            rptCredentials.DataSource = dt;
            rptCredentials.DataBind();

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


        #region "Web Methods"

        [WebMethod(EnableSession = true)]
        public static string DeleteCredentials(string sDeleteArray)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSql = null;
            string sErr = "";

            if (sDeleteArray.Length < 36)
                return "";

            sDeleteArray = ui.QuoteUp(sDeleteArray);

            DataTable dt = new DataTable();
            // get a list of credential_ids that will be deleted for the log
            sSql = "select credential_name,credential_id from asset_credential where credential_id in (" + sDeleteArray.ToString() + ") " +
                    "and credential_id not in (select distinct credential_id from asset where credential_id is not null)";
            if (!dc.sqlGetDataTable(ref dt, sSql, ref sErr))
            {
                throw new Exception(sErr);
            }


            try
            {

                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                //delete asset_credential
                sSql = "delete from asset_credential where credential_id in (" + sDeleteArray.ToString() + ") " +
                        "and credential_id not in (select distinct credential_id from asset where credential_id is not null)";
                oTrans.Command.CommandText = sSql;
                if (!oTrans.ExecUpdate(ref sErr))
                {
                    throw new Exception(sErr);
                }

                oTrans.Commit();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // if we made it here, so save the logs
            foreach (DataRow dr in dt.Rows)
            {
                ui.WriteObjectDeleteLog(Globals.acObjectTypes.Credential, dr["credential_id"].ToString(), dr["credential_name"].ToString(), "Credential Deleted");
            }


            return sErr;
        }




        [WebMethod(EnableSession = true)]
        public static string SaveCredential(object[] oAsset)
        {

            // we are passing in 16 elements, if we have 16 go
            if (oAsset.Length != 8) return "Incorrect list of attributes:" + oAsset.Length.ToString();

            string sCredentialID = oAsset[0].ToString();
            string sCredentialName = oAsset[1].ToString().Replace("'", "''");
            string sUserName = oAsset[2].ToString().Replace("'", "''");
            string sCredentialDesc = oAsset[3].ToString().Replace("'", "''");
            string sPassword = oAsset[4].ToString();
            string sDomain = oAsset[5].ToString();
            string sMode = oAsset[6].ToString();
            string sPrivilegedPassword = oAsset[7].ToString();

            // for logging
            string sOriginalUserName = null;

            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSql = null;
            string sErr = null;


            //if we are editing get the original values
            if (sMode == "edit")
            {
                sSql = "select username from asset_credential " +
                       "where credential_id = '" + sCredentialID + "'";

                if (!dc.sqlGetSingleString(ref sOriginalUserName, sSql, ref sErr))
                {
                    throw new Exception(sErr);
                }
            }

            try
            {
                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                // update the user fields.
                if (sMode == "edit")
                {
                    // only update the passwword if it has changed
                    string sNewPassword = "";
                    if (sPassword != "($%#d@x!&")
                    {
                        sNewPassword = ",password = '" + dc.EnCrypt(sPassword) + "'";
                    }

                    // bugzilla 1260
                    // same for privileged_password
                    string sPriviledgedPasswordUpdate = null;
                    if (sPrivilegedPassword == "($%#d@x!&")
                    {
                        // password has not been touched
                        sPriviledgedPasswordUpdate = "";
                    }
                    else
                    {
                        // updated password
                        sPriviledgedPasswordUpdate = ",privileged_password = '" + dc.EnCrypt(sPrivilegedPassword) + "'";

                    }


                    sSql = "update asset_credential set" +
                        " credential_name = '" + sCredentialName.Replace("'", "''") + "'," +
                        " username = '" + sUserName.Replace("'", "''") + "'," +
                        " domain = '" + sDomain.Replace("'", "''") + "'," +
                        " shared_cred_desc = '" + sCredentialDesc.Replace("'", "''") + "'" +
                        sNewPassword +
                        sPriviledgedPasswordUpdate +
                        " where credential_id = '" + sCredentialID + "'";
                }
                else
                {
                    // if the priviledged password is empty just set it to null
                    string sPrivilegedPasswordUpdate = "NULL";
                    if (sPrivilegedPassword.Length != 0)
                    {
                        sPrivilegedPasswordUpdate = "'" + dc.EnCrypt(sPrivilegedPassword) + "'";
                    };


                    sSql = "insert into asset_credential (credential_id, credential_name, username, password, domain, shared_cred_desc, shared_or_local, privileged_password)" +
                    " values (" + "'" + System.Guid.NewGuid().ToString() + "'," +
                    "'" + sCredentialName.Replace("'", "''") + "'," +
                    "'" + sUserName.Replace("'", "''") + "'," +
                    "'" + dc.EnCrypt(sPassword) + "'," +
                    "'" + sDomain.Replace("'", "''") + "'," +
                    "'" + sCredentialDesc.Replace("'", "''") + "'," +
                    "'0'," + sPrivilegedPasswordUpdate + ")";
                }
                oTrans.Command.CommandText = sSql;
                if (!oTrans.ExecUpdate(ref sErr))
                {
                    throw new Exception(sErr);
                }

                oTrans.Commit();
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }




            // add security log
            // since this is not handled as a page postback, theres no "Viewstate" settings
            // so 2 options either we keep an original setting for each value in hid values, or just get them from the db as part of the 
            // update above, since we are already passing in 15 or so fields, lets just get the values at the start and reference them here
            if (sMode == "edit")
            {
                ui.WriteObjectChangeLog(Globals.acObjectTypes.Credential, sCredentialID, sUserName.Replace("'", "''"), sOriginalUserName, sUserName.Replace("'", "''"));
            }
            else
            {
                ui.WriteObjectAddLog(Globals.acObjectTypes.Credential, sCredentialID, sUserName.Replace("'", "''"), "Credential Created");
            }


            // no errors to here, so return an empty string
            return "";
        }

        [WebMethod(EnableSession = true)]
        public static string LoadCredential(string sCredentialID)
        {

            dataAccess dc = new dataAccess();
            string sSQL = null;
            string sErr = null;

            string sCredName = null;
            string sCredUsername = null;
            string sCredDomain = null;
            string sCredPassword = null;
            string sPriviledgedPassword = null;
            string sCredDesc = null;


            sSQL = "select credential_id, credential_name, username, domain, shared_cred_desc, privileged_password" +
                " from asset_credential" +
                " where credential_id = '" + sCredentialID + "'";

            StringBuilder sbAssetValues = new StringBuilder();
            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }
            else
            {
                if (dr != null)
                {
                    sCredName = (object.ReferenceEquals(dr["credential_name"], DBNull.Value) ? "" : dr["credential_name"].ToString());
                    sCredUsername = (object.ReferenceEquals(dr["username"], DBNull.Value) ? "" : dr["username"].ToString());
                    sCredDomain = (object.ReferenceEquals(dr["domain"], DBNull.Value) ? "" : dr["domain"].ToString());
                    sCredDesc = (object.ReferenceEquals(dr["shared_cred_desc"], DBNull.Value) ? "" : dr["shared_cred_desc"].ToString());
                    sCredPassword = "($%#d@x!&";
                    sPriviledgedPassword = (object.ReferenceEquals(dr["privileged_password"], DBNull.Value) ? "" : dr["privileged_password"].ToString());
                    // if the password is blank, its been cleared at some point
                    if (sPriviledgedPassword.Length > 0)
                    {
                        sPriviledgedPassword = "($%#d@x!&";
                    }
                    else
                    {
                        sPriviledgedPassword = "";
                    }



                    // Return the asset object as a JSON 

                    sbAssetValues.Append("{");
                    sbAssetValues.AppendFormat("\"{0}\" : \"{1}\",", "sCredName", sCredName);
                    sbAssetValues.AppendFormat("\"{0}\" : \"{1}\",", "sCredUsername", sCredUsername);
                    sbAssetValues.AppendFormat("\"{0}\" : \"{1}\",", "sCredDomain", sCredDomain);
                    sbAssetValues.AppendFormat("\"{0}\" : \"{1}\",", "sCredPassword", sCredPassword);
                    sbAssetValues.AppendFormat("\"{0}\" : \"{1}\",", "sPriviledgedPassword", sPriviledgedPassword);
                    sbAssetValues.AppendFormat("\"{0}\" : \"{1}\"", "sCredDesc", sCredDesc);
                    sbAssetValues.Append("}");

                }
                else
                {
                    sbAssetValues.Append("{}");
                }

            }

            return sbAssetValues.ToString();
        }

        #endregion

        #region "Buttons"

        protected void btnGetPage_Click(object sender, System.EventArgs e)
        {
            GetCredentials();
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
