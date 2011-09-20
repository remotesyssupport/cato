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
    public partial class notAllowed : System.Web.UI.Page
    {
        acUI.acUI ui = new acUI.acUI();
        
        protected void Page_Load(object sender, EventArgs e)
        {
//the page where the decision was made to redirect here might have put additional detail messages in the session

    //if so, display that message

            if (!string.IsNullOrEmpty((string)ui.GetSessionObject("NotAllowedMessage", "Security")))
            {
                lblMessage.Text = ui.GetSessionObject("NotAllowedMessage", "Security").ToString();
                ui.DeleteSessionObject("NotAllowedMessage", "Security");
            }
        }

    }
}
