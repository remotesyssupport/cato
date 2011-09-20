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
using System.Web.UI.HtmlControls;
using System.Web.Services;
using System.Data;
using System.IO;
using System.Collections.Specialized;
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;

namespace Web.pages
{
    public partial class ecoTemplateEdit : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();
        ACWebMethods.uiMethods wm = new ACWebMethods.uiMethods();

        string sEcoTemplateID;

        protected void Page_Load(object sender, EventArgs e)
        {
            string sErr = "";

            sEcoTemplateID = ui.GetQuerystringValue("ecotemplate_id", typeof(string)).ToString();

            if (!Page.IsPostBack)
            {
                hidEcoTemplateID.Value = sEcoTemplateID;

                if (!GetDetails(ref sErr))
                {
                    ui.RaiseError(Page, "Unable to continue.  Eco Template not found for ID [" + sEcoTemplateID + "].<br />" + sErr, true, "");
                    return;
                }


                //Get the actions on initial load from the web method
                Literal ltActions = new Literal();
                ltActions.Text = wm.wmGetEcotemplateActions(sEcoTemplateID);
                phActions.Controls.Add(ltActions);


            }
        }

        #region "Tabs"
        private bool GetDetails(ref string sErr)
        {
            try
            {
                string sSQL = "select ecotemplate_id, ecotemplate_name, ecotemplate_desc" +
                              " from ecotemplate" +
                              " where ecotemplate_id = '" + sEcoTemplateID + "'";

                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr)) return false;

                if (dr != null)
                {
                    txtEcoTemplateName.Text = dr["ecotemplate_name"].ToString();
                    txtDescription.Text = ((!object.ReferenceEquals(dr["ecotemplate_desc"], DBNull.Value)) ? dr["ecotemplate_desc"].ToString() : "");

                    //the header
                    lblEcoTemplateHeader.Text = dr["ecotemplate_name"].ToString();

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                return false;
            }
        }
        #endregion
    }
}
