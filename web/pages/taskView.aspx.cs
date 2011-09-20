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
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;

namespace Web.pages
{
    public partial class taskView : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();

        acUI.acUI ui = new acUI.acUI();
        FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
        ACWebMethods.taskMethods tm = new ACWebMethods.taskMethods();

        string sTaskID;
        string sOriginalTaskID;

        //use a checkbox on the page to set this
        bool bShowNotes = true;

        protected void Page_Load(object sender, EventArgs e)
        {
            sTaskID = ui.GetQuerystringValue("task_id", typeof(string)).ToString();

            string sErr = "";

            if (!Page.IsPostBack)
            {
                hidTaskID.Value = sTaskID;

                //details
                if (!GetDetails(ref sErr))
                {
                    ui.RaiseError(Page, "Unable to continue.  Task record not found for task_id [" + sTaskID + "].<br />" + sErr, true, "");
                    return;
                }

                //steps
                if (!GetSteps(ref sErr))
                {
                    ui.RaiseError(Page, "Unable to continue.<br />" + sErr, true, "");
                    return;
                }

            }

        }
        private bool GetDetails(ref string sErr)
        {
            try
            {
                string sSQL = "select task_id, original_task_id, task_name, task_code, task_status, version, default_version," +
                              " task_desc, use_connector_system, concurrent_instances, queue_depth" +
                              " from task" +
                              " where task_id = '" + sTaskID + "'";

                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr)) return false;

                if (dr != null)
                {
                    sOriginalTaskID = dr["original_task_id"].ToString();
                    hidOriginalTaskID.Value = sOriginalTaskID;

                    hidDefault.Value = dr["default_version"].ToString();

                    if (dr["default_version"].ToString() == "1")
                        btnSetDefault.Visible = false;

                    lblTaskCode.Text = ui.SafeHTML(dr["task_code"].ToString());
                    lblVersion.Text = dr["version"].ToString();
                    lblCurrentVersion.Text = dr["version"].ToString();

                    lblDescription.Text = ui.SafeHTML(((!object.ReferenceEquals(dr["task_desc"], DBNull.Value)) ? dr["task_desc"].ToString() : ""));

                    //lblDirect.Text = ((int)dr["use_connector_system"] == 1 ? "Yes" : "No");
                    lblConcurrentInstances.Text = ((!object.ReferenceEquals(dr["concurrent_instances"], DBNull.Value)) ? dr["concurrent_instances"].ToString() : "");
                    lblQueueDepth.Text = ((!object.ReferenceEquals(dr["queue_depth"], DBNull.Value)) ? dr["queue_depth"].ToString() : "");

                    lblStatus.Text = dr["task_status"].ToString();
                    lblStatus2.Text = dr["task_status"].ToString();

                    //the header
                    lblTaskNameHeader.Text = ui.SafeHTML(dr["task_name"].ToString());
                    lblVersionHeader.Text = dr["version"].ToString() + ((int)dr["default_version"] == 1 ? " (default)" : "");

                    if (!GetMaxVersion(dr["task_id"].ToString(), ref sErr)) return false;

                    //schedules (only if this is default)
                    if (hidDefault.Value == "1")
                    {
                        if (!GetSchedule(dr["original_task_id"].ToString(), ref sErr)) return false;
                        tab_schedules.Visible = true;
                    }
                    else
                        tab_schedules.Visible = false;

                    return true;
                }
                else
                {
                    return false;

                }
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                return false;
            }
        }
        private bool GetSchedule(string sTaskID, ref string sErr)
        {
            return true;
            //try
            //{
            //    string sSQL = "select s.schedule_id, s.schedule_name, s.status from schedule s, schedule_object so " +
            //                  " where so.object_id = '" + sTaskID + "'" +
            //                  " and so.schedule_id = s.schedule_id";


            //    DataTable dt = new DataTable();
            //    if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            //    {
            //        return false;
            //    }

            //    if (dt.Rows.Count > 0)
            //    {
            //        rpSchedules.DataSource = dt;
            //        rpSchedules.DataBind();
            //    }
            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    sErr = ex.Message;
            //    return false;
            //}
        }
        #region "Steps"

        private bool GetSteps(ref string sErr)
        {
            try
            {
                Literal lt = new Literal();

                //MAIN codeblock first
                lt.Text += "<div class=\"ui-state-default te_header\">";

                lt.Text += "<div class=\"step_section_title\">";
                lt.Text += "<span class=\"step_title\">";
                lt.Text += "MAIN";
                lt.Text += "</span>";
                lt.Text += "</div>";
                lt.Text += "<div class=\"step_section_icons\">";
                lt.Text += "<span id=\"print_link\" class=\"pointer\">";
                lt.Text += "<img class=\"task_print_link\" alt=\"\" src=\"../images/icons/printer.png\" />";
                lt.Text += "</span>";
                lt.Text += "</div>";
                lt.Text += "</div>";

                lt.Text += "<div class=\"codeblock_box\">";
                lt.Text += BuildSteps("MAIN");
                lt.Text += "</div>";



                //now get the codeblocks and loop thru them
                string sSQL = "select codeblock_name" +
                      " from task_codeblock" +
                      " where task_id = '" + sTaskID + "'" +
                      " and codeblock_name <> 'MAIN'" +
                      " order by codeblock_name";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, "Database Error", true, sErr);
                    return false;
                }

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        lt.Text += "<div class=\"ui-state-default te_header\">";

                        lt.Text += "<div class=\"step_section_title\" id=\"cbt_" + dr["codeblock_name"].ToString() + "\">";
                        lt.Text += "<span class=\"step_title\">";
                        lt.Text += dr["codeblock_name"].ToString();
                        lt.Text += "</span>";
                        lt.Text += "</div>";
                        lt.Text += "</div>";

                        lt.Text += "<div class=\"codeblock_box\">";
                        lt.Text += BuildSteps(dr["codeblock_name"].ToString());
                        lt.Text += "</div>";
                    }
                }

                phSteps.Controls.Add(lt);
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                return false;
            }

            return true;
        }
        private string BuildSteps(string sCodeblockName)
        {
            string sErr = "";
            string sSQL = "select s.step_id, s.step_order, s.step_desc, s.function_name, s.function_xml, s.commented, s.locked," +
                " s.output_parse_type, s.output_row_delimiter, s.output_column_delimiter, s.variable_xml," +
                " f.function_label, f.category_name, c.category_label, f.description, f.help," +
                " us.visible, us.breakpoint, us.skip" +
                " from task_step s" +
                " join lu_task_step_function f on s.function_name = f.function_name" +
                " join lu_task_step_function_category c on f.category_name = c.category_name" +
                " left outer join task_step_user_settings us on us.user_id = '" + ui.GetSessionUserID() + "' and s.step_id = us.step_id" +
                " where s.task_id = '" + sTaskID + "'" +
                " and s.codeblock_name = '" + sCodeblockName + "' and s.commented = 0" +
                " order by s.step_order";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, "Database Error", true, sErr);
                return "";
            }

            string sHTML = "";
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    sHTML += ft.DrawReadOnlyStep(dr, bShowNotes);
                }
            }
            else
            {
                sHTML = "<li class=\"no_step\">No Commands defined for this Codeblock.</li>";
            }

            return sHTML;
        }
        #endregion

        private bool GetDebug(ref string sErr)
        {
            //valid task_instance statuses
            //Submitted, Processing, Queued, Completed, Error and Cancelled
            try
            {
                //string sAssetID = txtTestAsset.Attributes["asset_id"].ToString();

                //get the last 'done' instance.
                string sSQL = "select ti.task_status, ti.submitted_dt, ti.started_dt, ti.completed_dt, ti.ce_node, ti.pid," +
                    " t.task_name, a.asset_name, u.full_name, si.schedule_instance_name" +
                    " from tv_task_instance ti" +
                    " join task t on ti.task_id = t.task_id" +
                    " left outer join tv_schedule_instance si on ti.schedule_instance = si.schedule_instance" +
                    " left outer join users u on ti.submitted_by = u.user_id" +
                    " left outer join asset a on ti.asset_id = a.asset_id" +
                    " where ti.task_instance = (" +
                        "select max(task_instance)" +
                        " from tv_task_instance" +
                        " where task_status in ('Cancelled','Completed','Error')" +
                        " and task_id = '" + sTaskID + "'";
                //if (ui.IsGUID(sAssetID))
                //{
                //    sSQL += " and asset_id = '" + sAssetID + "'";
                //}
                sSQL += ")";

                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr)) return false;

                if (dr != null)
                {
                    lblLastRunStatus.Text = dr["task_status"].ToString();
                    lblLastRunDT.Text = (dr["completed_dt"].Equals(System.DBNull.Value) ? "" : dr["completed_dt"].ToString());
                }
                else
                {
                    // bugzilla 1139, hide the link to the runlog if the task has never ran.
                    debug_view_latest_log.Visible = false;
                }

                //ok, is it currently running?
                //this is a different check than the one above
                sSQL = "select task_instance, task_status" +
                        " from tv_task_instance" +
                        " where task_instance = (" +
                            "select max(task_instance)" +
                            " from tv_task_instance" +
                            " where task_status in ('Submitted','Queued','Processing','Aborting','Staged')" +
                            " and task_id = '" + sTaskID + "'";
                //if (ui.IsGUID(sAssetID))
                //{
                //    sSQL += " and asset_id = '" + sAssetID + "'";
                //}
                sSQL += ")";

                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr)) return false;

                if (dr != null)
                {
                    hidDebugActiveInstance.Value = dr["task_instance"].ToString();
                    lblCurrentStatus.Text = dr["task_status"].ToString();
                }
                else
                {
                    lblCurrentStatus.Text = "Inactive";
                }


                //see if a debug asset was saved for this user
                string sSettingXML = "";
                sSQL = "select settings_xml from users where user_id = '" + ui.GetSessionUserID() + "'";

                if (!dc.sqlGetSingleString(ref sSettingXML, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, "Unable to get settings for user.<br />", false, sErr);
                }

                //we don't care to do anything if there were no settings
                if (sSettingXML != "")
                {
                    XDocument xDoc = XDocument.Parse(sSettingXML);
                    if (xDoc == null) ui.RaiseError(Page, "XML settings data for user is invalid.", false, "");

                    XElement xTask = xDoc.Descendants("task").Where(
                        x => (string)x.Attribute("task_id") == sTaskID).LastOrDefault();
                    if (xTask != null)
                    {
                        string sDebugAssetID = "";
                        if (xTask.Attribute("asset_id") != null)
                        {
                            sDebugAssetID = xTask.Attribute("asset_id").Value.ToString();

                            if (ui.IsGUID(sDebugAssetID))
                            {
                                string sDebugAssetName = "";
                                sSQL = "select asset_name from asset where asset_id = '" + sDebugAssetID + "'";
                                if (!dc.sqlGetSingleString(ref sDebugAssetName, sSQL, ref sErr))
                                {
                                    throw new Exception("Unable to get asset name for asset [" + sDebugAssetID + "].<br />" + sErr);
                                }

                                //txtTestAsset.Text = sDebugAssetName;
                                //txtTestAsset.Attributes["asset_id"] = sDebugAssetID;
                            }
                        }
                    }

                }


                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        private void SetDefaultVersion()
        {
            try
            {
                string sErr = "";
                string sSQL = "";

                sSQL = "update task set default_version = 0" +
                    " where original_task_id = " +
                    " (select original_task_id from task where task_id = '" + sTaskID + "');" +
                    "update task set default_version = 1 where task_id = '" + sTaskID + "';";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                {
                    ui.RaiseError(Page, "Unable to set Default Version. " + sErr, true, "");
                    return;
                }

                //now that it's set, hide the button
                btnSetDefault.Visible = false;

                btnSetDefault.Visible = false;
            }
            catch (Exception ex)
            {
                ui.RaiseError(Page, "Unable to set Default Version. " + ex.Message, true, "");

            }
        }
        private bool GetMaxVersion(string sTaskID, ref string sErr)
        {

            try
            {

                string sMaxVer = tm.wmGetTaskMaxVersion(sTaskID, ref sErr);

                if (!string.IsNullOrEmpty(sMaxVer))
                {
                    //lblNewMinor.Text = "New Minor Version (" + String.Format("{0:0.000}", (Convert.ToDouble(sMaxVer) + .001)) + ")";
                    //lblNewMajor.Text = "New Major Version (" + String.Format("{0:0.000}", Math.Round((Convert.ToDouble(sMaxVer) + .5), MidpointRounding.AwayFromZero)) + ")";

                    return true;
                };
                return false;
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                return false;
            }
        }
        protected void btnSetDefault_Click(object sender, System.EventArgs e)
        {
            SetDefaultVersion();
        }

        #region "DataBound"
        protected void rpAttributeGroups_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            RepeaterItem item = e.Item;
            if ((item.ItemType == ListItemType.Item) ||
                (item.ItemType == ListItemType.AlternatingItem))
            {

                Repeater rp = (Repeater)item.FindControl("rpGroupAttributes");
                DataRowView drv = (DataRowView)item.DataItem;

                rp.DataSource = drv.CreateChildView("GroupAttributes");
                rp.DataBind();

            }
        }
        #endregion


    }
}
