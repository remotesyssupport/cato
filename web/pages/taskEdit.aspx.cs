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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;

namespace Web.pages
{
    public partial class taskEdit : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();
        FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
        ACWebMethods.taskMethods tm = new ACWebMethods.taskMethods();

        string sTaskID;
        string sOriginalTaskID;
        string sQSCodeblock;
        string sUserID;
        protected void Page_Load(object sender, EventArgs e)
        {
            sUserID = ui.GetSessionUserID();

            string sErr = "";

            sTaskID = ui.GetQuerystringValue("task_id", typeof(string)).ToString();
            sQSCodeblock = ui.GetQuerystringValue("codeblock_name", typeof(string)).ToString();

            if (!Page.IsPostBack)
            {
                hidTaskID.Value = sTaskID;
                //the initial value of the Codeblock is always "MAIN"
                hidCodeblockName.Value = "MAIN";

                if (!GetDetails(ref sErr))
                {
                    ui.RaiseError(Page, "Unable to continue.  Task record not found for task_id [" + sTaskID + "].<br />" + sErr, true, "");
                    return;
                }

                //categories and functions
                if (!GetCategories(ref sErr))
                {
                    ui.RaiseError(Page, "Unable to continue.  No categories defined.<br />" + sErr, true, "");
                    return;
                }

                //get debug information
                if (!GetDebug(ref sErr))
                {
                    sErr = "Error getting Debugging information.<br />" + sErr;
                    return;
                }

                //codeblocks
                if (!GetCodeblocks(ref sErr))
                {
                    sErr = "Unable to continue.  Could not retrieve Codeblocks. " + sErr;
                    return;
                }

                //if we have a codeblock on the querystring (that means some other page want's us to go there)
                //if not, take the one in the hidden field (that means the user clicked one from the list)
                //if the hidden field is empty, show 'MAIN'
                string sCodeblockName = "MAIN";
                if (!string.IsNullOrEmpty(sQSCodeblock)) sCodeblockName = sQSCodeblock;
                if (!string.IsNullOrEmpty(hidCodeblockName.Value)) sCodeblockName = hidCodeblockName.Value;

                if (!GetSteps(sCodeblockName, ref sErr))
                {
                    return;
                }
            }
        }

        #region "Tabs"
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
                    //ATTENTION!
                    //Approved tasks CANNOT be edited.  So, if the status is approved... we redirect to the
                    //task 'view' page.
                    //this is to prevent any sort of attempts on the client to load an approved or otherwise 'locked' 
                    // version into the edit page.
                    if (dr["task_status"].ToString() == "Approved")
                    {
                        Response.Redirect("taskView.aspx?task_id=" + sTaskID, false);
                        return true;
                    }
                    //so, we are here, meaning we have an editable task.
                    txtTaskName.Text = dr["task_name"].ToString();
                    txtTaskCode.Text = dr["task_code"].ToString();
                    lblVersion.Text = dr["version"].ToString();
                    lblCurrentVersion.Text = dr["version"].ToString();



                    //chkDirect.Checked = ((int)dr["use_connector_system"] == 1 ? true : false);

                    txtDescription.Text = ((!object.ReferenceEquals(dr["task_desc"], DBNull.Value)) ? dr["task_desc"].ToString() : "");
                    txtConcurrentInstances.Text = ((!object.ReferenceEquals(dr["concurrent_instances"], DBNull.Value)) ? dr["concurrent_instances"].ToString() : "");
                    txtQueueDepth.Text = ((!object.ReferenceEquals(dr["queue_depth"], DBNull.Value)) ? dr["queue_depth"].ToString() : "");

                    hidDefault.Value = dr["default_version"].ToString();

                    hidOriginalTaskID.Value = dr["original_task_id"].ToString();
                    sOriginalTaskID = dr["original_task_id"].ToString();

                    //lblStatus.Text = dr["task_status"].ToString();
                    lblStatus2.Text = dr["task_status"].ToString();
                    hidOriginalStatus.Value = dr["task_status"].ToString();

                    /*                    
                     * ok, this is important.
                     * there are some rules for the process of 'Approving' a task.
                     * specifically:
                     * -- if there are no other approved tasks in this family, this one will become the default.
                     * -- if there is another approved task in this family, we show the checkbox
                     * -- allowing the user to decide whether or not to make this one the default
                     */
                    sSQL = "select count(*) from task" +
                        " where original_task_id = '" + sOriginalTaskID + "'" +
                        " and task_status = 'Approved'";

                    int iCount = 0;
                    if (!dc.sqlGetSingleInteger(ref iCount, sSQL, ref sErr))
                    {
                        ui.RaiseError(Page, "Unable to count Tasks in this family.", true, sErr);
                        return false;
                    }

                    if (iCount > 0)
                        chkMakeDefault.Visible = true;
                    else
                        chkMakeDefault.Visible = false;



                    //the header
                    lblTaskNameHeader.Text = dr["task_name"].ToString();
                    lblVersionHeader.Text = dr["version"].ToString() + ((int)dr["default_version"] == 1 ? " (default)" : "");

                    if (!GetMaxVersion(dr["task_id"].ToString(), ref sErr)) return false;

                    //schedules (only if this is default)
                    if (hidDefault.Value == "1")
                    {
                        //if (!GetSchedule(dr["original_task_id"].ToString(), ref sErr)) return false;
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


                //see if any debug settings were saved for this user
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
                        //yay! we have user settings...

                        //test asset?
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
                                    ui.RaiseError(Page, "Unable to get asset name for asset [" + sDebugAssetID + "].<br />", false, sErr);
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
        private bool GetCodeblocks(ref string sErr)
        {
            try
            {
                string sSQL = "select codeblock_name" +
                    " from task_codeblock" +
                    " where task_id = '" + sTaskID + "'" +
                    " order by codeblock_name";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    return false;
                }

                if (dt.Rows.Count > 0)
                {
                    rpCodeblocks.DataSource = dt;
                    rpCodeblocks.DataBind();

                    return true;
                }
                else
                {
                    //uh oh... there are no codeblocks!
                    //since all tasks require a MAIN codeblock... if it's missing,
                    //we can just repair it right here.
                    sSQL = "insert task_codeblock (task_id, codeblock_name) values ('" + sTaskID + "', 'MAIN')";
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        ui.RaiseError(Page, "No codeblocks found, and unable to add MAIN." + sErr, true, "");
                        return false;
                    }

                    //go again
                    if (!GetCodeblocks(ref sErr))
                    {
                        sErr += "Unable to continue.  Could not retrieve Codeblocks (17). " + sErr;
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                return false;
            }
        }
        private bool GetCategories(ref string sErr)
        {
            try
            {
                string sSQL = "";

                sSQL = "select category_name, category_label, description, icon" +
                     " from lu_task_step_function_category" +
                     " order by sort_order;";

                //create the data set with the first table
                DataSet ds = new DataSet();
                if (!dc.sqlGetDataSet(ref ds, sSQL, ref sErr, "dt")) return false;

                //append it with the second table
                sSQL = " select category_name, function_name, function_label, description, help, icon" +
                    " from lu_task_step_function" +
                    " order by sort_order";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr)) return false;

                //put the second table in the dataset
                ds.Tables.Add(dt);

                DataRelation rel = new DataRelation("CatFuncs",
                    ds.Tables[0].Columns["category_name"], ds.Tables[1].Columns["category_name"]);
                ds.Relations.Add(rel);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    rpCategories.DataSource = ds.Tables[0].DefaultView;
                    rpCategories.DataBind();

                    rpCategoryWidgets.DataSource = ds.Tables[0].DefaultView;
                    rpCategoryWidgets.DataBind();

                    return true;
                }
                else
                {
                    sErr = "Error: No categories defined.";
                    return false;

                }
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                return false;

            }
        }
        #endregion
 
        #region "Steps"
        private bool GetSteps(string sCodeblockName, ref string sErr)
        {
            try
            {
                //if it's passed in as an arg, change nothing.
                //if the arg is empty, die.
                if (string.IsNullOrEmpty(sCodeblockName))
                {
                    sErr += "Error: No Codeblock specified for GetSteps.";
                    return false;
                }

                //set the label and the hidden field
                lblStepSectionTitle.Text = sCodeblockName;

                if (!BuildSteps(sCodeblockName, ref sErr))
                {
                    sErr += "Error building steps.<br />" + sErr;
                    return false;
                }

            }
            catch (Exception ex)
            {
                sErr = "Exception:" + ex.Message;
                return false;
            }

            return true;
        }
        private bool BuildSteps(string sCodeblockName, ref string sErr)
        {
            string sSQL = "select s.step_id, s.step_order, s.step_desc, s.function_name, s.function_xml, s.commented, s.locked," +
                " s.output_parse_type, s.output_row_delimiter, s.output_column_delimiter, s.variable_xml," +
                " f.function_label, f.category_name, c.category_label, f.description, f.help, f.icon," +
                " us.visible, us.breakpoint, us.skip, us.button" +
                " from task_step s" +
                " join lu_task_step_function f on s.function_name = f.function_name" +
                " join lu_task_step_function_category c on f.category_name = c.category_name" +
                " left outer join task_step_user_settings us on us.user_id = '" + sUserID + "' and s.step_id = us.step_id" +
                " where s.task_id = '" + sTaskID + "'" +
                " and s.codeblock_name = '" + sCodeblockName + "'" +
                " order by s.step_order";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                sErr += "Database Error: " + sErr;
                return false;
            }

            //set the text of the step 'add' message...
            string sAddHelpMsg =  "No Commands have been defined in this Codeblock. Drag a Command here to add it.";

            Literal lt = new Literal();
            if (dt.Rows.Count > 0)
            {
                //we always need the no_step item to be there, we just hide it if we have other items
                //it will get unhidden if someone deletes the last step.
                lt.Text = "<li id=\"no_step\" class=\"ui-widget-content ui-corner-all ui-state-active ui-droppable no_step hidden\">" + sAddHelpMsg + "</li>";

                foreach (DataRow dr in dt.Rows)
                {
                    lt.Text += ft.DrawFullStep(dr);
                }
            }
            else
            {
                lt.Text = "<li id=\"no_step\" class=\"ui-widget-content ui-corner-all ui-state-active ui-droppable no_step\">" + sAddHelpMsg + "</li>";
            }
            phSteps.Controls.Add(lt);
            return true;
        }
        #endregion

        private void AddCodeblock()
        {
            try
            {
                string sErr = "";
                string sSQL = "";

                string sNewCBName = hidCodeblockName.Value.Replace("'", "''").Trim();
                if (sNewCBName != "")
                {
                    sSQL = "insert into task_codeblock (task_id, codeblock_name)" +
                           " values (" + "'" + sTaskID + "'," +
                           "'" + sNewCBName + "'" +
                           ")";

                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        ui.RaiseError(Page, "Unable to add Codeblock [" + sNewCBName + "]. " + sErr, true, "");
                        return;
                    }

                    if (!GetCodeblocks(ref sErr))
                    {
                        ui.RaiseError(Page, "The Codeblock was added, but there was an error refreshing the page.  Please refresh the page manually. " + sErr, true, "");
                        return;

                    }

                    if (!GetSteps(sNewCBName, ref sErr))
                    {
                        ui.RaiseError(Page, "The Codeblock was added, but there was an error refreshing the page.  Please refresh the page manually. " + sErr, true, "");
                        return;
                    }

                    udpSteps.Update();
                }
                else
                {
                    ui.RaiseError(Page, "Unable to add Codeblock.", true, "Invalid or missing Codeblock Name.");
                }
            }
            catch (Exception ex)
            {
                ui.RaiseError(Page, "Unable to add Codeblock. " + ex.Message, true, "");
            }
        }
        private void DeleteCodeblock(string sCodeblockID)
        {
            try
            {
                string sErr = "";

                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                //first, delete any steps that are embedded content on steps in this codeblock
                //(because embedded steps have their parent step_id as the codeblock name.)
                oTrans.Command.CommandText = "delete em from task_step em" +
                    " join task_step p on em.task_id = p.task_id" +
                    " and em.codeblock_name = p.step_id" +
                    " where p.task_id = '" + sTaskID + "'" +
                    " and p.codeblock_name = '" + sCodeblockID + "'";
                if (!oTrans.ExecUpdate(ref sErr))
                {
                    ui.RaiseError(Page, "Unable to delete embedded Steps from Codeblock.", true, sErr);
                    return;
                }

                oTrans.Command.CommandText = "delete u from task_step_user_settings u" +
                    " join task_step ts on u.step_id = ts.step_id" +
                    " where ts.task_id = '" + sTaskID + "'" +
                    " and ts.codeblock_name = '" + sCodeblockID + "'";
                if (!oTrans.ExecUpdate(ref sErr))
                {
                    ui.RaiseError(Page, "Unable to delete Steps user settings for Steps in Codeblock.", true, sErr);
                    return;
                }

                oTrans.Command.CommandText = "delete from task_step" +
                    " where task_id = '" + sTaskID + "'" +
                    " and codeblock_name = '" + sCodeblockID + "'";
                if (!oTrans.ExecUpdate(ref sErr))
                {
                    ui.RaiseError(Page, "Unable to delete Steps from Codeblock.", true, sErr);
                    return;
                }

                oTrans.Command.CommandText = "delete from task_codeblock" +
                    " where task_id = '" + sTaskID + "'" +
                    " and codeblock_name = '" + sCodeblockID + "'";
                if (!oTrans.ExecUpdate(ref sErr))
                {
                    ui.RaiseError(Page, "Unable to delete Codeblock.", true, sErr);
                    return;
                }

                oTrans.Commit();

                if (!GetCodeblocks(ref sErr))
                {
                    ui.RaiseError(Page, "Warning.  Successfully deleted the Codeblock" +
                        " but there was an error refreshing the page.  Please reload the page manually. " + sErr, true, "");
                    return;
                }

                if (!GetSteps("MAIN", ref sErr))
                {
                    ui.RaiseError(Page, "Warning.  Successfully deleted the Codeblock" +
                        " but there was an error refreshing the page.  Please reload the page manually. " + sErr, true, "");
                    return;
                }

                udpSteps.Update();

            }
            catch (Exception ex)
            {
                ui.RaiseError(Page, "Exception:", true, ex.Message);
            }
        }

        #region "Buttons"
        protected void btnStepLoad_Click(object sender, System.EventArgs e)
        {
            string sErr = "";

            if (!GetSteps(hidCodeblockName.Value, ref sErr))
            {
                ui.RaiseError(Page, "Error getting Steps:" + sErr, true, "");
                return;
            }
        }
        protected void btnCBDelete_Click(object sender, System.EventArgs e)
        {
            DeleteCodeblock(hidCodeblockDelete.Value);
        }
        protected void btnCBAdd_Click(object sender, System.EventArgs e)
        {
            AddCodeblock();
        }
        protected void btnCBRefresh_Click(object sender, System.EventArgs e)
        {
            string sErr = "";
            if (!GetCodeblocks(ref sErr))
            {
                ui.RaiseError(Page, "Error getting codeblocks:" + sErr, true, "");
            }
        }
        #endregion

        #region "DataBound"
        protected void rpCategoryWidgets_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            RepeaterItem item = e.Item;
            if ((item.ItemType == ListItemType.Item) ||
                (item.ItemType == ListItemType.AlternatingItem))
            {
                Repeater rp = (Repeater)item.FindControl("rpCategoryFunctions");
                DataRowView drv = (DataRowView)item.DataItem;

                rp.DataSource = drv.CreateChildView("CatFuncs");
                rp.DataBind();
            }
        }
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

        private bool GetMaxVersion(string sTaskID, ref string sErr)
        {
            try
            {
                string sMaxVer = tm.wmGetTaskMaxVersion(sTaskID, ref sErr);

                if (!string.IsNullOrEmpty(sMaxVer))
                {
                    lblNewMinor.Text = "New Minor Version (" + String.Format("{0:0.000}", (Convert.ToDouble(sMaxVer) + .001)) + ")";
                    lblNewMajor.Text = "New Major Version (" + String.Format("{0:0.000}", Math.Round((Convert.ToDouble(sMaxVer) + .5), MidpointRounding.AwayFromZero)) + ")";

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

    }
}
