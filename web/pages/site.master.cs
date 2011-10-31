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
using System.Xml.Linq;
using System.Xml.XPath;


namespace Web
{
    public partial class sitemaster : System.Web.UI.MasterPage
    {
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
				//new menu from xml
				string sRole = ui.GetSessionUserRole().ToLower();
				
                XDocument xSiteMaster = (XDocument)ui.GetSessionObject("site_master_xml", "Security");

                if (xSiteMaster == null)
                {
                    ui.RaiseError(Page, "Error: Site master XML is not in the session.", false, "");
                } 
				else 
				{
					Literal lt = new Literal();
					
					foreach (XElement xMenu in xSiteMaster.XPathSelectElements("//mainmenu/menu"))
                    {
						string sMenuRoles = (xMenu.Attribute("roles") == null ? "" : xMenu.Attribute("roles").Value);
						
						//so, you get to see this menu item IF:
						// *) you're an "administrator"
						// *) the "roles" attribute is "all"
						// *) the  matches your role
						if (sMenuRoles.ToLower().IndexOf("all") > -1 || sMenuRoles.ToLower().IndexOf(sRole) > -1 || sRole == "administrator") {

							string sLabel = (xMenu.Attribute("label") == null ? "No Label Defined" : xMenu.Attribute("label").Value);
							string sHref = (xMenu.Attribute("href") == null ? "" : " href=\"" + xMenu.Attribute("href").Value + "\"");
							string sOnClick = (xMenu.Attribute("onclick") == null ? "" : " onclick=\"" + xMenu.Attribute("onclick").Value + "\"");
							string sIcon = (xMenu.Attribute("icon") == null ? "" : "<img src=\"" + xMenu.Attribute("icon").Value + "\" alt=\"\" />");
							string sClass = (xMenu.Attribute("class") == null ? "" : xMenu.Attribute("class").Value);
							
							//this is the top level menu... may or may not have onclicks or hrefs
							//but should always have an image
	                        lt.Text += "<li class=\"" + sClass + "\" style=\"cursor: pointer;\">";
	                        lt.Text += "<a";
							lt.Text += sOnClick;
							lt.Text += sHref;
							lt.Text += ">";
	                        lt.Text += sIcon;
	                        lt.Text += sLabel;
							lt.Text += "</a>";
							
							//may or may not have a submenu (we don't support many levels, just one.)
							if (xMenu.XPathSelectElements("item").Count() > 0) {						
								lt.Text += "<ul>";
								
								foreach (XElement xSubMenu in xMenu.XPathSelectElements("item"))
			                    {
				                    sMenuRoles = (xSubMenu.Attribute("roles") == null ? "" : xSubMenu.Attribute("roles").Value);
							
									//so, you get to see this menu item IF:
									// *) you're an "administrator"
									// *) the "roles" attribute is "all"
									// *) the  matches your role
									if (sMenuRoles.ToLower().IndexOf("all") > -1 || sMenuRoles.ToLower().IndexOf(sRole) > -1 || sRole == "administrator") {
										
										sLabel = (xSubMenu.Attribute("label") == null ? "No Label Defined" : xSubMenu.Attribute("label").Value);
										sHref = (xSubMenu.Attribute("href") == null ? "" : " href=\"" + xSubMenu.Attribute("href").Value + "\"");
										sOnClick = (xSubMenu.Attribute("onclick") == null ? "" : " onclick=\"" + xSubMenu.Attribute("onclick").Value + "\"");
										sIcon = (xSubMenu.Attribute("icon") == null ? "" : "<img src=\"" + xSubMenu.Attribute("icon").Value + "\" alt=\"\" />");
										sClass = (xSubMenu.Attribute("class") == null ? "" : xSubMenu.Attribute("class").Value);
										
		                        		lt.Text += "<li class=\"ui-widget-header " + sClass + "\" style=\"cursor: pointer;\">";
				                        lt.Text += "<a";
										lt.Text += sOnClick ;
										lt.Text += sHref ;
										lt.Text += ">";
				                        lt.Text += sIcon;
				                        lt.Text += sLabel;
										lt.Text += "</a>";
				                        lt.Text += "</li>";
									}
			                    }
								
								lt.Text += "</ul>";
							}
							
	                        lt.Text += "</li>";
	                    }
						
						phMenu.Controls.Add(lt);
					
					}
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
                    ui.RaiseInfo(Page, sMsg, "");

				
                //log page views if logging is enabled
                ui.addPageViewLog();


                //fill the cloud account drop down
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
    }
}
