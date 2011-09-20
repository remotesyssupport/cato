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
    public partial class schedulerSettings : System.Web.UI.Page
    {
       dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                string sErr = "";
                string sSQL = "select mode_off_on, loop_delay_sec, schedule_min_depth, schedule_max_days, clean_app_registry" +
                    " from scheduler_settings" +
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
                    ddlSchedulerOnOff.Text = dr["mode_off_on"].ToString();
                    ViewState["ddlSchedulerOnOff"] = ddlSchedulerOnOff.Text;

                    txtLoopDelay.Text = dr["loop_delay_sec"].ToString();
                    ViewState["txtLoopDelay"] = txtLoopDelay.Text;

                    txtScheduleMinDepth.Text = dr["schedule_min_depth"].ToString();
                    ViewState["txtScheduleMinDepth"] = txtScheduleMinDepth.Text;

                    txtScheduleMaxDays.Text = dr["schedule_max_days"].ToString();
                    ViewState["txtRetryMaxAttempts"] = txtScheduleMaxDays.Text;

                    txtCleanAppRegistry.Text = dr["clean_app_registry"].ToString();
                    ViewState["txtCleanAppRegistry"] = txtCleanAppRegistry.Text;
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

            var sSQL = "update scheduler_settings set" +
                " mode_off_on='" + ddlSchedulerOnOff.Text + "'," +
                " loop_delay_sec='" + txtLoopDelay.Text + "'," +
                " schedule_min_depth='" + txtScheduleMinDepth.Text +"'," +
                " schedule_max_days='" + txtScheduleMaxDays.Text +"'," +
                " clean_app_registry = '" + txtCleanAppRegistry.Text + "'";
            
            string sErr = "";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                ui.RaiseError(Page, "Update failed: " + sErr, true, "");
            }

            //logging
            var sLogObject = "Scheduler Settings";

            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Scheduler On / Off", ViewState["ddlSchedulerOnOff"].ToString(), ddlSchedulerOnOff.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Poll Loop", ViewState["txtLoopDelay"].ToString(), txtLoopDelay.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Min Depth", ViewState["txtScheduleMinDepth"].ToString(), txtScheduleMinDepth.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Max Days", ViewState["txtScheduleMaxDays"].ToString(), txtScheduleMaxDays.Text);
            ui.WriteObjectChangeLog(acObjectTypes.None, sLogObject, "Clean Registry", ViewState["txtCleanAppRegistry"].ToString(), txtCleanAppRegistry.Text);
           
            // all good, notify user
            ui.RaiseInfo(Page, "Scheduler settings updated.", "");

        }
    }
}
