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
using System.Web.Configuration;
using System.IO;
using Globals;
using System.Collections;
using System.Data;

namespace Web.pages
{
    public partial class home : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();
		
		protected void Page_Load(object sender, EventArgs e)
        {
			string sSQL = "";
			string sErr = "";
			
			//this will only write out if you're an Administrator
			//will check each item to make sure the proper config is done.
			if (ui.UserIsInRole("Administrator")) {
				ArrayList aItems = new ArrayList();
				
				//administrator account
				sSQL = "select security_question, security_answer, email from users where username = 'administrator'";
				DataRow dr = null;
				if(!dc.sqlGetDataRow(ref dr, sSQL, ref sErr)) {
					ui.RaiseError(Page, "Unable to read Administrator account.", false, sErr);
				}
	            if (dr != null)
	            {
					if (string.IsNullOrEmpty(dr["email"].ToString()))
						aItems.Add("Set an email account to receive system notifications.");
	
					if (string.IsNullOrEmpty(dr["security_question"].ToString()) || string.IsNullOrEmpty(dr["security_answer"].ToString()))
						aItems.Add("Select a security challenge question and response.");
		
					if (aItems.Count > 0)
					{
						if (ui.GetSessionUsername().ToLower() == "administrator")
							ltGettingStartedItems.Text += DrawGettingStartedItem("Administrator Account", aItems, "<a href=\"../pages/userPreferenceEdit.aspx\">Click here</a> to update Administrator account settings.");
						else
							ltGettingStartedItems.Text += DrawGettingStartedItem("Administrator Account", aItems, "You must be logged in as 'Administrator' to change these settings.");					
					}
				}
					
	
			
				//messenger settings
				aItems.Clear();
				sSQL = "select smtp_server_addr from messenger_settings where id = 1";
				dr = null;
				if(!dc.sqlGetDataRow(ref dr, sSQL, ref sErr)) {
					ui.RaiseError(Page, "Unable to read Messenger Settings.", false, sErr);
				}
	            if (dr != null)
	            {
					if (string.IsNullOrEmpty(dr["smtp_server_addr"].ToString()))
						aItems.Add("Define an SMTP server.");
	
					if (aItems.Count > 0)
						ltGettingStartedItems.Text += DrawGettingStartedItem("Messenger Settings", aItems, "<a href=\"../pages/notificationEdit.aspx\">Click here</a> to update Messenger settings.");
				}
					
	
			
				//cloud account settings
				//a little different - this one can be an empty table
				aItems.Clear();
				sSQL = "select login_id, login_password from cloud_account";
				dr = null;
				if(!dc.sqlGetDataRow(ref dr, sSQL, ref sErr)) {
					ui.RaiseError(Page, "Unable to read Cloud Accounts.", false, sErr);
				}
	            if (dr != null)
	            {
					if (string.IsNullOrEmpty(dr["login_id"].ToString()) || string.IsNullOrEmpty(dr["login_password"].ToString()))
						aItems.Add("Provide an Account Login ID and Password.");
				}
				else 
				{
					aItems.Add("There are no Cloud Accounts defined.");
				}
				if (aItems.Count > 0)
					ltGettingStartedItems.Text += DrawGettingStartedItem("Cloud Accounts", aItems, "<a href=\"../pages/cloudAccountEdit.aspx\">Click here</a> to manage Cloud Accounts.");
			}

			//if the phGettingStarted has anything in it, show the getting started panel
			if (string.IsNullOrEmpty(ltGettingStartedItems.Text))
				pnlGettingStarted.Visible = false;
			else
				pnlGettingStarted.Visible = true;
			
        }
    
		protected string DrawGettingStartedItem(string sTitle, ArrayList aItems, string sActionLine)
		{
			string sHTML = "";

			sHTML += "<div class=\"ui-widget\" style=\"margin-top: 10px;\">";
			sHTML += "<div style=\"padding: 10px;\" class=\"ui-state-highlight ui-corner-all\">";
			sHTML += "<span style=\"float: left; margin-right: .3em;\" class=\"ui-icon ui-icon-info\"></span>";
			sHTML += "<strong>" + sTitle + "</strong>";

			
			//each item
			foreach (string sItem in aItems) 
			{
				sHTML += "<p style=\"margin-left: 10px;\">" + sItem + "</p>";				
			}

			
			sHTML += "<br />";
			sHTML += "<p>" + sActionLine + "</p>";
			sHTML += "</div>";
			sHTML += "</div>";
			
			return sHTML;
		}
	}
}
