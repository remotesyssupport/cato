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
using System.Web.UI.WebControls;

namespace Web.pages
{

    public partial class taskRunLog : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();
        


        private void Page_Load(object sender, System.EventArgs e)
        {
            string sTaskInstance;
            string sTaskID;
            string sAssetID;
            string sRows;
            string sErr = "";
            sTaskInstance = ui.GetQuerystringValue("task_instance", typeof(string)).ToString();
            sTaskID = ui.GetQuerystringValue("task_id", typeof(string)).ToString();
            sAssetID = ui.GetQuerystringValue("asset_id", typeof(string)).ToString();
            sRows = ui.GetQuerystringValue("rows", typeof(string)).ToString();

            if (!Page.IsPostBack)
            {

                string sBack = ui.GetQuerystringValue("back_label", typeof(string)).ToString();

                if (sBack.Length != 0)
                {
                    lblBackLink.Text = "<a onClick=\"history.back();\">" + sBack + "</a> >";
                }


                //different things happen depending on the page args

                //if an instance was provided... it overrides all other args 
                //otherwise we need to figure out which instance we want
                if (sTaskInstance == "")
                {
                    if (ui.IsGUID(sTaskID))
                    {
                        string sSQL = "select max(task_instance)" +
                            " from tv_task_instance where task_id = '" + sTaskID + "'";

                        if (ui.IsGUID(sAssetID))
                        {
                            sSQL += " and asset_id = '" + sAssetID + "'";
                        }

                        dc.sqlGetSingleString(ref sTaskInstance, sSQL, ref sErr);
                    }
                }

                //now we are sure we have the instance... put it on the page...
                hidInstanceID.Value = sTaskInstance;

                //PERMISSION CHECK
                //now... kick out if the user isn't allowed to see this page

                //it's a little backwards... IsPageAllowed will kick out this page for users...
                //so don't bother to check that if we can determine the user has permission by 
                //a group association to this task
                string sOTID = "";
                string stiSQL = "select t.original_task_id" +
                   " from tv_task_instance ti join task t on ti.task_id = t.task_id" +
                   " where ti.task_instance = '" + sTaskInstance + "'";

                dc.sqlGetSingleString(ref sOTID, stiSQL, ref sErr);

                //now we know the ID, see if we are grouped with it
                //this will kick out if they DONT match tags and they AREN'T in a role with sufficient privileges
                if (!ui.UserAndObjectTagsMatch(sOTID, 3)) ui.IsPageAllowed("You do not have permission to view this Task.");
                //END PERMISSION CHECK


                //all good... continue...
                if (!GetDetails(sTaskInstance, ref sErr))
                {
                    ui.RaiseError(Page, "Unable to continue.  Task Instance record not found for instance_id [" + sTaskInstance + "]. " + sErr, true, "");
                    return;
                }

                GetLog(sTaskInstance, sRows);
            }
        }
        private bool GetDetails(string sTaskInstance, ref string sErr)
        {
            try
            {
                string sSQL = "select ti.task_instance, ti.task_id, '' as asset_id, ti.task_status, ti.submitted_by_instance, " +
                    " ti.submitted_dt, ti.started_dt, ti.completed_dt, ti.ce_node, ti.pid, ti.debug_level," +
                    " t.task_name, t.version, '' as asset_name, u.full_name, si.schedule_instance_name," +
                    " ar.app_instance, ar.platform, ar.hostname," +
                    " t.concurrent_instances, t.queue_depth," +
                    " ti.ecosystem_id, d.ecosystem_name, ti.account_id, ca.account_name" +
                    " from tv_task_instance ti" +
                    " join task t on ti.task_id = t.task_id" +
                    " left outer join tv_schedule_instance si on ti.schedule_instance = si.schedule_instance" +
                    " left outer join users u on ti.submitted_by = u.user_id" +
                    " left outer join tv_application_registry ar on ti.ce_node = ar.id" +
                    " left outer join cloud_account ca on ti.account_id = ca.account_id" +
                    " left outer join ecosystem d on ti.ecosystem_id = d.ecosystem_id" +
                    " where task_instance = " + sTaskInstance;

                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr)) return false;

                if (dr != null)
                {
                    int iConcurrent = 0;
                    int.TryParse(dr["concurrent_instances"].ToString(), out iConcurrent);
                    int iQueueDepth = 0;
                    int.TryParse(dr["queue_depth"].ToString(), out iQueueDepth);


                    hidTaskID.Value = dr["task_id"].ToString();
                    hidAssetID.Value = dr["asset_id"].Equals(System.DBNull.Value) ? "" : dr["asset_id"].ToString();

                    lblTaskInstance.Text = dr["task_instance"].ToString();
                    lblTaskName.Text = dr["task_name"].ToString() + " - Version " + dr["version"].ToString();
                    lblStatus.Text = dr["task_status"].ToString();
                    lblAssetName.Text = (dr["asset_name"].Equals(System.DBNull.Value) ? "N/A" : dr["asset_name"].ToString());
                    lblSubmittedDT.Text = (dr["submitted_dt"].Equals(System.DBNull.Value) ? "" : dr["submitted_dt"].ToString());
                    lblStartedDT.Text = (dr["started_dt"].Equals(System.DBNull.Value) ? "" : dr["started_dt"].ToString());
                    lblCompletedDT.Text = (dr["completed_dt"].Equals(System.DBNull.Value) ? "" : dr["completed_dt"].ToString());
                    lblCENode.Text = (dr["ce_node"].Equals(System.DBNull.Value) ? "" : dr["app_instance"].ToString() + " (" + dr["platform"].ToString() + ")");
                    lblPID.Text = (dr["pid"].Equals(System.DBNull.Value) ? "" : dr["pid"].ToString());
                    if (lblPID.Text != "")
                    {
                        string sEncID = dc.EnCrypt(ui.GetSessionUserID());

                        //can't build the link until we know what port we need.
                        sSQL = "select port from logserver_settings where id = 1";
                        string sPort = "";
                        dc.sqlGetSingleString(ref sPort, sSQL, ref sErr);

                        if (string.IsNullOrEmpty(sPort))
                            sPort = "4000";

                        hidCELogFile.Value = "http://" + dr["hostname"].ToString() + ":" + sPort + "/getlog?logtype=ce&q=" + sEncID + "&logfile=" + sTaskInstance + ".log";
                    }

                    hidSubmittedByInstance.Value = dr["submitted_by_instance"].Equals(System.DBNull.Value) ? "" : dr["submitted_by_instance"].ToString();
                    lblSubmittedByInstance.Text = (dr["submitted_by_instance"].Equals(System.DBNull.Value) ? "N/A" : dr["submitted_by_instance"].ToString());

                    if (hidSubmittedByInstance.Value != "")
                        lblSubmittedByInstance.CssClass = "link";

                    hidEcosystemID.Value = dr["ecosystem_id"].Equals(System.DBNull.Value) ? "" : dr["ecosystem_id"].ToString();
                    lblEcosystemName.Text = dr["ecosystem_name"].Equals(System.DBNull.Value) ? "" : dr["ecosystem_name"].ToString();
                    hidAccountID.Value = dr["account_id"].Equals(System.DBNull.Value) ? "" : dr["account_id"].ToString();
                    lblAccountName.Text = dr["account_name"].Equals(System.DBNull.Value) ? "" : dr["account_name"].ToString();


                    if (!dr["full_name"].Equals(System.DBNull.Value))
                    {
                        //launched by a user
                        lblSubmittedBy.Text = dr["full_name"].ToString();
                    }
                    else if (!dr["schedule_instance_name"].Equals(System.DBNull.Value))
                    {
                        //launched by scheduler
                        lblSubmittedBy.Text = " Schedule (" + dr["schedule_instance_name"].ToString() + ")";
                    }

                    //superusers AND those tagged with this Task can see the stop and resubmit button
                    if (ui.UserIsInRole("Developer") || ui.UserIsInRole("Administrator") || ui.UserAndObjectTagsMatch(dr["original_task_id"].ToString(), 3))
                    {
                        phResubmit.Visible = true;
                        phCancel.Visible = true;
                    }
                    else
                    {
                        phResubmit.Visible = false;
                        phCancel.Visible = false;
                    }


                    //if THIS instance is 'active', show additional warning info on the resubmit confirmation.
                    //and if it's not, don't show the "cancel" button
                    if ("processing,queued,submitted,pending,aborting,queued,staged".IndexOf(dr["task_status"].ToString().ToLower()) > -1)
                        lblResubmitMessage.Text = "This Task is currently active.  You have requested to start another instance.<br /><br />";
                    else
                        phCancel.Visible = false;


                    //check for OTHER active instances
                    int iActiveCount = 0;
                    sSQL = "select count(*) from tv_task_instance where task_id = '" + dr["task_id"].ToString() + "'" +
                        " and task_instance <> '" + sTaskInstance + "'" +
                        " and task_status in ('processing','submitted','pending','aborting','queued','staged')";
                    if (!dc.sqlGetSingleInteger(ref iActiveCount, sSQL, ref sErr))
                    {
                        ui.RaiseError(Page, sErr, true, "");
                        return false;
                    }


                    //and hide the resubmit button if we're over the limit
                    //if active < concurrent do nothing
                    //if active >= concurrent but there's room in the queue, change the message
                    //if this one would pop the queue, hide the button
                    if (iActiveCount > 0)
                    {
                        if (iConcurrent + iQueueDepth > 0)
                        {
                            if (iActiveCount >= iConcurrent && (iActiveCount + 1) <= iQueueDepth)
                                lblResubmitMessage.Text = "The maximum concurrent instances for this Task are running.  This request will be queued.<br /><br />";
                            else
                                phResubmit.Visible = false;
                        }

                        //neato... show the user a list of all the other instances!
                        sSQL = "select task_instance, task_status from tv_task_instance" +
                            " where task_id = '" + dr["task_id"].ToString() + "'" +
                            " and task_instance <> '" + sTaskInstance + "'" +
                            " and task_status in ('processing','submitted','pending','aborting','queued','staged')" +
                            " order by task_status";
                        DataTable dt = new DataTable();

                        if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                        {
                            ui.RaiseError(Page, sErr, true, "");
                            return false;
                        }

                        rpOtherInstances.DataSource = dt;
                        rpOtherInstances.DataBind();

                        pnlOtherInstances.Visible = true;
                    }
                    return true;
                }
                else
                {
                    return false;

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void GetLog(string sTaskInstance, string sRows)
        {
            try
            {
                string sErr = "";
                string sLimitClause = " limit 100";

                if (sRows != "")
                {
                    if (sRows == "all")
                        sLimitClause = "";
                    else
                        sLimitClause = " limit " + sRows;
                }

                string sSQL = "select til.task_instance, til.entered_dt, til.connection_name, FormatTextToHTML(til.log) as log," +
                    " til.step_id, s.step_order, s.function_name, f.function_label, s.codeblock_name, " +
                    " FormatTextToHTML(til.command_text) as command_text," +
                    " '' as variable_name,  '' as variable_value, " +
                    " case when length(til.log) > 256 then 1 else 0 end as large_text" +
                    " from task_instance_log til" +
                    " left outer join task_step s on til.step_id = s.step_id" +
                    " left outer join lu_task_step_function f on s.function_name = f.function_name" +
                    " where til.task_instance = " + sTaskInstance +
                    " order by til.id" + sLimitClause;

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                    ui.RaiseError(Page, sErr, false);

                Literal lt = new Literal();

                if (dt.Rows.Count > 0)
                {
                    lt.Text += "<ul class=\"log\">" + Environment.NewLine;
                    string sThisStepID = "";
                    string sPrevStepID = "";

                    foreach (DataRow dr in dt.Rows)
                    {
                        sThisStepID = dr["step_id"].ToString();




                        //start a new list item and header only if we are moving on to a different step.
                        if (sPrevStepID != sThisStepID)
                        {
                            //this is backwards, we are closing the previous loops list item
                            //only if this loop is a different step_id than the last.
                            //but not on the first loop (sPrevStepID = "")
                            if (sPrevStepID != "")
                                lt.Text += "</li>" + Environment.NewLine;

                            //new item
                            lt.Text += "<li class=\"ui-widget-content ui-corner-bottom log_item\">" + Environment.NewLine;
                            lt.Text += "    <div class=\"log_header ui-state-default ui-widget-header\">" + Environment.NewLine;
                            if (!dr["function_label"].Equals(System.DBNull.Value))
                            {
                                if (!dr["function_label"].Equals(""))
                                {
                                    if (Convert.ToInt32(dr["step_order"]) > 0)
                                        lt.Text += "[" + dr["codeblock_name"].ToString() + " - " + dr["function_label"].ToString() + " - Step " + dr["step_order"].ToString() + "]" + Environment.NewLine;
                                    else
                                        lt.Text += "[Action - " + dr["function_label"].ToString() + Environment.NewLine;
                                }
                            }

                            lt.Text += "At: [" + dr["entered_dt"].ToString() + "]" + Environment.NewLine;
                            if (!dr["connection_name"].Equals(System.DBNull.Value))
                            {

                                if (!dr["connection_name"].Equals(""))
                                {
                                    lt.Text += "On Connection: [" + dr["connection_name"].ToString() + "]" + Environment.NewLine;
                                }
                            }


                            lt.Text += "    </div>" + Environment.NewLine;
                        }


                        lt.Text += "<div class=\"log_detail\">" + Environment.NewLine;

                        //it might have a command
                        if (!dr["command_text"].Equals(System.DBNull.Value))
                        {
                            if (!string.IsNullOrEmpty(dr["command_text"].ToString().Trim()))
                            {
                                lt.Text += "<div class=\"log_command ui-widget-content ui-corner-all hidden\">" + Environment.NewLine;

                                //the command text might hold special information we want to display differently
                                string sCommandText = ParseCommandForFlags(dr["command_text"].ToString());
                                lt.Text += sCommandText + Environment.NewLine;
                                lt.Text += "</div>" + Environment.NewLine;
                            }
                        }


                        //it might be a log entry
                        if (!dr["log"].Equals(System.DBNull.Value))
                        {
                            if (!string.IsNullOrEmpty(dr["log"].ToString().Trim()))
                            {
                                //commented out to save space
                                //lt.Text += "Results:" + Environment.NewLine;
                                lt.Text += "    <div class=\"log_results ui-widget-content ui-corner-all\">" + Environment.NewLine;
                                lt.Text += dr["log"].ToString() + Environment.NewLine;
                                lt.Text += "    </div>" + Environment.NewLine;
                            }
                        }



                        //it could be a variable:value entry
                        if (!dr["variable_name"].Equals(System.DBNull.Value))
                        {
                            if (!string.IsNullOrEmpty(dr["variable_name"].ToString().Trim()))
                            {
                                lt.Text += "Variable:" + Environment.NewLine;
                                lt.Text += "<div class=\"log_variable_name ui-widget-content ui-corner-all\">" + Environment.NewLine;
                                lt.Text += dr["variable_name"].ToString() + Environment.NewLine;
                                lt.Text += "</div>" + Environment.NewLine;
                                lt.Text += "Set To:" + Environment.NewLine;
                                lt.Text += "<div class=\"log_variable_value ui-widget-content ui-corner-all\">" + Environment.NewLine;
                                lt.Text += dr["variable_value"].ToString() + Environment.NewLine;
                                lt.Text += "</div>" + Environment.NewLine;
                            }
                        }
                        //end detail
                        lt.Text += "</div>" + Environment.NewLine;

                        sPrevStepID = sThisStepID;

                    }
                    //the last one get's closed no matter what
                    lt.Text += "</li>" + Environment.NewLine;
                    lt.Text += "</ul>" + Environment.NewLine;

                    phLog.Controls.Add(lt);

                    return;
                }
                else
                {
                    return;

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        private string ParseCommandForFlags(string sCommand)
        {
            string sRetVal = "";

            if (sCommand.IndexOf("run_task") > -1)
            {
                string sInstance = sCommand.Replace("run_task ", "");
                sRetVal = "<span class=\"link\" onclick=\"location.href='taskRunLog.aspx?task_instance=" + sInstance + "';\">Jump to Task</span>";
            }
            else
            {
                sRetVal = sCommand;
            }

            return sRetVal;
        }

    }
}
