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

namespace Web.pages
{
    public partial class about : System.Web.UI.Page
    {
        acUI.AppGlobals ag = new acUI.AppGlobals();

        protected void Page_Load(object sender, EventArgs e)
        {
            lblAbout.Text = "About " + ag.APP_COMPANYNAME;

            lnkWeb.Text = ag.APP_WEB;
            lnkWeb.NavigateUrl = ag.APP_WEB;
            lnkEmail.Text = ag.APP_SUPPORT_EMAIL;
            lnkEmail.NavigateUrl = "mailto:" + ag.APP_SUPPORT_EMAIL;

            lblEnv.Text = Server.MachineName.ToString();

            lblVersion.Text = ag.APP_VERSION;
            lblBuild.Text = ag.APP_REVISION;

            lblDatabaseAddress.Text = DatabaseSettings.DatabaseAddress.ToString();
            lblDatabaseName.Text = DatabaseSettings.DatabaseName.ToString();

            lblInstance.Text = DatabaseSettings.AppInstance.ToString();
        }

    }
}
