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
using System.Data;
using System.Web.UI;

namespace Web.pages
{
    public partial class taskStatusView : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        string sSQL = "";
        string sErr = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                FillAutomatedTaskStatus();
            }
        }
        private void FillAutomatedTaskStatus()
        {

            sSQL = "select Processing, Staged, Pending, Submitted, Aborting, Queued, AllStatuses," +
                    "(Processing + Pending + Submitted + Aborting + Queued + Staged) as TotalActive," +
                    "Cancelled, Completed, Errored, (Cancelled + Completed + Errored) as TotalComplete " +
                    "from (select count(case when task_status = 'Processing' then 1 end) as Processing," +
                    "count(case when task_status = 'Pending' then 1 end) as Pending," +
                    "count(case when task_status = 'Submitted' then 1 end) as Submitted," +
                    "count(case when task_status = 'Aborting' then 1 end) as Aborting," +
                    "count(case when task_status = 'Queued' then 1 end) as Queued," +
                    "count(case when task_status = 'Cancelled' then 1 end) as Cancelled," +
                    "count(case when task_status = 'Completed' then 1 end) as Completed," +
                    "count(case when task_status = 'Staged' then 1 end) as Staged," +
                    "count(case when task_status = 'Error' then 1 end) as Errored, " +
                    "count(*) as AllStatuses " +
                    "from tv_task_instance) foo";

            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }
            else
            {
                if (dr != null)
                {
                    lblAutomatedTaskProcessing.Text = dr["Processing"].ToString();
                    lblAutomatedTaskSubmitted.Text = dr["Submitted"].ToString();
                    lblAutomatedTaskStaged.Text = dr["Staged"].ToString();
                    lblAutomatedTaskAborting.Text = dr["Aborting"].ToString();
                    lblAutomatedTaskPending.Text = dr["Pending"].ToString();
                    lblAutomatedTaskQueued.Text = dr["Queued"].ToString();
                    lblAutomatedTaskActive.Text = dr["TotalActive"].ToString();
                    lblAutomatedTaskCancelled.Text = dr["Cancelled"].ToString();
                    lblAutomatedTaskCompleted.Text = dr["Completed"].ToString();
                    lblAutomatedTaskErrored.Text = dr["Errored"].ToString();
                    lblAutomatedTaskTotalCompleted.Text = dr["TotalComplete"].ToString();
                    lblAutomatedAllStatuses.Text = dr["AllStatuses"].ToString();

                }
            }

        }
        }
}
