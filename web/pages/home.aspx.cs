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
			
			//for starters, just write out the hardcoded html
			pnlGettingStarted.Visible = true;
			
			Literal lt = new Literal();
			ArrayList aItems = new ArrayList();
			
			//administrator account
			sSQL = "select security_question, security_answer, email from users where username = 'administrator'";
			DataRow dr = null;
			if(!dc.sqlGetDataRow(ref dr, sSQL, ref sErr)) {
				ui.RaiseError(Page, "Unable to read Administrator account", false, sErr);
			}
            if (dr != null)
            {
				aItems.Add("Set an email account to receive system notifications.");
				aItems.Add("Select a security challenge question and response.");
				lt.Text += DrawGettingStartedItem("Administrator Account", aItems, "<a href=\"../pages/userPreferenceEdit.aspx\">Click here</a> to update Administrator account settings.");
			}
				

		
			//messenger settings
			aItems.Clear();
			aItems.Add("Sfds");
			aItems.Add("111");
			lt.Text += DrawGettingStartedItem("Messenger Settings", aItems, "<a href=\"../pages/userPreferenceEdit.aspx\">Click here</a> to update Messenger settings.");

		
			phGettingStartedItems.Controls.Add(lt);
			
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
