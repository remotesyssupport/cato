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
using System.Data;

namespace Web.pages
{
    /// <summary>
    /// Description of taskStepVarsEdit
    /// </summary>
    public partial class taskCommandHelp : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();

        private void Page_Load(object sender, System.EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                GetHelp();
            }
        }

        private void GetHelp()
        {

            string sSQL = "select help, icon, upper(left(category_name,1)) + substring(Lower(category_name), 2," + 
                " length(category_name)) as category" +
                " from lu_task_step_function" +
                " order by category_name, function_label";
            string sErr = "";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                throw new Exception("Unable to get command help.<br />" + sErr);
            }

            rpCommandHelp.DataSource = dt;
            rpCommandHelp.DataBind();


            return;
        }


      

        
    }
}
