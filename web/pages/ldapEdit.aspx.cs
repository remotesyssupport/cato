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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;
using System.Data;
using System.Text;

namespace Web.pages
{
    public partial class ldapEdit : System.Web.UI.Page
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
                sWhereString = " and (ld.ldap_domain like '%" + txtSearch.Text + "%' " +
                                    "or ld.address like '%" + txtSearch.Text + "%') ";
            }


            // bugzilla 1345 join the users table
            // this only joins where the user is set to ldap, if they
            // are set to local then the domain is not really in use??
                sSQL = "WITH domainsNames( domain ) AS " +
                "( " +
                "SELECT SUBSTRING(u.username, 0,CHARINDEX('\\', u.username)) as domain_name " +
                "FROM users u " +
                "where u.authentication_type = 'ldap' " +
                "and SUBSTRING(u.username, 0,CHARINDEX('\\', u.username)) <> '' " +
                ") " +
                "select ld.ldap_domain, ld.address, " +
                "(select count(*) from domainsNames where domain = ld.ldap_domain) as [in_use]  " +
                "from ldap_domain ld where (1=1) " + sWhereString;
                        
            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }

            ui.SetSessionObject("ldapDomains", dt, "SelectorListTables");

            //now, actually get the data from the session table and display it
            GetDomains();
        }
        private void GetDomains()
        {
            //here's how the paging works
            //you can get at the data by explicit ranges, or by pages
            //where pages are defined by properties

            //could come from a field on the page
            int iStart = 0;
            int iEnd = 0;

            //this is the page number you want
            int iPageNum = (string.IsNullOrEmpty(hidPage.Value) ? 1 : Convert.ToInt32(hidPage.Value));
            DataTable dtTotal = (DataTable)ui.GetSessionObject("ldapDomains", "SelectorListTables");
            dtTotal.TableName = "CredentialList";
            DataTable dt = ui.GetPageFromSessionTable(dtTotal, iPageSize, iPageNum, iStart, iEnd, hidSortColumn.Value,"");

            rptDomains.DataSource = dt;
            rptDomains.DataBind();

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
        public static string DeleteDomains(string sDeleteArray)
        {
            acUI.acUI ui = new acUI.acUI();
            string sSql = null;
            string sErr = "";

            if (sDeleteArray.Length < 36)
                return "";

            sDeleteArray = ui.QuoteUp(sDeleteArray);
                        
            try
            {

                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                //delete domains
                sSql = "delete from ldap_domain where ldap_domain in (" + sDeleteArray.ToString() + ")";
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
            ui.WriteObjectDeleteLog(Globals.acObjectTypes.Domain, sDeleteArray.ToString(), sDeleteArray.ToString(), "Domain(s) Deleted"); 
            
            return sErr;
        }
       
        [WebMethod(EnableSession = true)]
        public static string SaveDomain(object[] oAsset)
        {

            // we are passing in 4 elements, if we have 16 go
            if (oAsset.Length != 4) return "Incorrect list of attributes:" + oAsset.Length.ToString();

            string sEditDomain = oAsset[0].ToString();
            string sDomain = oAsset[1].ToString().Replace("'", "''");
            string sAddress = oAsset[2].ToString().Replace("'", "''");
            string sMode = oAsset[3].ToString();
            
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSql = null;
            string sErr = null;

            // before updating or adding make sure the domain name is available
            if (sEditDomain != sDomain)
            {
                try
                {
                    sSql = "select ldap_domain from ldap_domain where ldap_domain = '" + sDomain + "'";
                    string sDomainExists = "";
                    if (!dc.sqlGetSingleString(ref sDomainExists, sSql, ref sErr))
                    {
                        throw new Exception(sErr);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(sDomainExists))
                        {
                            return "Domain name exists, choose another name.";
                        }

                    }
                }
                catch (Exception ex)
                {

                    throw new Exception(ex.Message);
                }
            }
            
            
            try
            {
                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                // update the user fields.
                if (sMode == "edit")
                {
                    // if the domain name changed update all of the asset_credential's using this domain
                    if (sDomain != sEditDomain){
                        sSql = "update asset_credential set domain = '" + sDomain + "' where domain = '" + sEditDomain + "'";
                        oTrans.Command.CommandText = sSql;
                        if (!oTrans.ExecUpdate(ref sErr))
                        {
                            throw new Exception(sErr);
                        }  
                    }


                    sSql = "update ldap_domain set ldap_domain = '" + sDomain + "'," + "address = '" + sAddress + "' where ldap_domain = '" + sEditDomain + "'";
                    oTrans.Command.CommandText = sSql;
                    if (!oTrans.ExecUpdate(ref sErr))
                    {
                        throw new Exception(sErr);
                    }                
                
                }
                else
                {
                    sSql = "insert into ldap_domain (ldap_domain,address)" +
                    " values ('" + sDomain + "'," +
                    "'" + sAddress + "')";

                    oTrans.Command.CommandText = sSql;
                    if (!oTrans.ExecUpdate(ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                }
                

                oTrans.Commit();
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }

            // add security log
            if (sMode == "edit")
            {
                ui.WriteObjectChangeLog(Globals.acObjectTypes.Domain, sEditDomain, sEditDomain, sEditDomain, sDomain);
            }
            else
            {
                ui.WriteObjectAddLog(Globals.acObjectTypes.Domain, sDomain, sDomain, "Domain Created");
            }

            // no errors to here, so return an empty string
            return "";
        }

        [WebMethod(EnableSession = true)]
        public static string LoadDomain(string sDomain)
        {

            dataAccess dc = new dataAccess();
            string sSql = null;
            string sErr = null;
            string sAddress = "";

            sSql = "select address " +
                    "from ldap_domain " +
                    "where ldap_domain = '" + sDomain + "'";
            
            StringBuilder sbAssetValues = new StringBuilder();
            if (!dc.sqlGetSingleString(ref sAddress, sSql, ref sErr))
                throw new Exception(sErr);
            else
            {
                if (sAddress != "")
                {
                    // Return the asset object as a JSON 
                    sbAssetValues.Append("{");
                    sbAssetValues.AppendFormat("\"{0}\" : \"{1}\",", "sDomain", sDomain);
                    sbAssetValues.AppendFormat("\"{0}\" : \"{1}\"", "sAddress", sAddress);
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
            GetDomains();
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
