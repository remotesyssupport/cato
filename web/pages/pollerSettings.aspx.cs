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
using Globals;
using System.Data;

namespace Web.pages
{
    public partial class pollerSettings : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                string sErr = "";
                string sSQL = "select mode_off_on, loop_delay_sec, max_processes" +
                    " from poller_settings" +
                    " where id = 1";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, "Unable to continue. " + sErr, true, "");
                    return;
                }
                if (dt.Rows.Count > 0)
                {
                    DataRow dr = dt.Rows[0];
                    ddlPollerOnOff.Text = dr["mode_off_on"].ToString();
                    ViewState["ddlPollerOnOff"] = ddlPollerOnOff.Text;

                    txtLoopDelay.Text = dr["loop_delay_sec"].ToString();
                    ViewState["txtLoopDelay"] = txtLoopDelay.Text;

                    txtPollerMaxProcesses.Text = dr["max_processes"].ToString();
                    ViewState["txtPollerMaxProcesses"] = txtPollerMaxProcesses.Text;

                }
                else
                {
                    //what to do when it's empty?
                    ui.RaiseError(Page, "Unable to continue.  No data found. " + sErr, true, "");
                }
            }
        }

        public void btnSave_Click(object sender, System.EventArgs e)
        {

            var sSQL = "update poller_settings set mode_off_on='" + ddlPollerOnOff.Text + "'," +
                "loop_delay_sec='" + txtLoopDelay.Text + "', max_processes='" + txtPollerMaxProcesses.Text +"'";
            
            string sErr = "";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                ui.RaiseError(Page, "Update failed: " + sErr, true, "");
            }

            //logging
            var sLogObject = "Poller Settings";

            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Poller On / Off", ViewState["ddlPollerOnOff"].ToString(), ddlPollerOnOff.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Poll Loop", ViewState["txtLoopDelay"].ToString(), txtLoopDelay.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Max Processes", ViewState["txtPollerMaxProcesses"].ToString(), txtPollerMaxProcesses.Text);
           
            // all good, notify user
            ui.RaiseInfo(Page, "Poller settings updated.", "");

        }
    }
}
