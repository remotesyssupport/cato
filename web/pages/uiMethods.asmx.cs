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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.Services;
using System.Xml.Linq;
using System.Xml.XPath;
using Globals;

namespace ACWebMethods
{
    /// <summary>
    /// Summary description for taskMethods
    /// </summary>
    [WebService(Namespace = "ACWebMethods")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class uiMethods : System.Web.Services.WebService
    {

        [WebMethod(EnableSession = true)]
        public string wmGetTime()
        {
            return DateTime.Now.ToString();
        }

        [WebMethod(EnableSession = true)]
        public string wmGetHostname()
        {
            return Server.MachineName.ToString();
        }

        [WebMethod(EnableSession = true)]
        public string wmSendEmail(string sTo, string sFrom, string sSubject, string sBody)
        {
            acUI.acUI ui = new acUI.acUI();
            string sMsg = "";
            ui.SendEmailMessage(sTo, sFrom, sSubject, sBody, ref sMsg);
            return sMsg;
        }

        [WebMethod(EnableSession = true)]
        public void wmSendErrorReport(string sMessage, string sPageDetails)
        {
            acUI.acUI ui = new acUI.acUI();
            string sErr = "";

            string sTo = (string)ui.GetSessionObject("admin_email", "Security");

            if (!string.IsNullOrEmpty(sTo) && !string.IsNullOrEmpty(sMessage))
            {
                string sFrom = "ui@" + Server.MachineName.ToString();

                sMessage = ui.unpackJSON(sMessage);

                ui.SendEmailMessage(sTo, sFrom, "UI Error Report", sMessage + Environment.NewLine + Environment.NewLine + sPageDetails, ref sErr);
            }

            return;
        }

        [WebMethod(EnableSession = true)]
        public string wmUpdateHeartbeat()
        {
            acUI.acUI ui = new acUI.acUI();

            string sMsg = "";
            ui.UpdateUserSession(ref sMsg);

            return sMsg;
        }

        [WebMethod(EnableSession = true)]
        public string wmAssetSearch(string sSearchText, bool bAllowMultiSelect)
        {
            try
            {
                dataAccess dc = new dataAccess();
                acUI.acUI ui = new acUI.acUI();
                string sErr = "";
                string sWhereString = " where 1=1 and a.asset_status ='Active'";

                if (sSearchText.Length > 0)
                {
                    sSearchText = sSearchText.Replace("'", "''").Trim();

                    //split on spaces
                    int i = 0;
                    string[] aSearchTerms = sSearchText.Split(' ');
                    for (i = 0; i <= aSearchTerms.Length - 1; i++)
                    {

                        //if the value is a guid, it's an existing task.
                        //otherwise it's a new task.
                        if (aSearchTerms[i].Length > 0)
                        {
                            sWhereString += " and (a.asset_name like '%" + aSearchTerms[i] + "%' " +
                                "or a.port like '%" + aSearchTerms[i] + "%' " +
                                "or a.address like '%" + aSearchTerms[i] + "%' " +
                                "or a.connection_type like '%" + aSearchTerms[i] + "%' " +
                                "or a.db_name like '%" + aSearchTerms[i] + "%' " +
                                "or u.full_name like '%" + aSearchTerms[i] + "%' " +
                                "or ta.attributes like '%" + aSearchTerms[i] + "%') ";
                        }
                    }
                }

                //limit the results to the users 'tags' unless they are a super user
                if (!ui.UserIsInRole("Developer") && !ui.UserIsInRole("Administrator"))
                {
                    sWhereString += " and a.asset_id in (" +
                        "select distinct at.object_id" +
                        " from object_tags ut" +
                        " join object_tags at on ut.tag_name = at.tag_name" +
                        " and ut.object_type = 1 and at.object_type = 2" +
                        " where ut.object_id = '" + ui.GetSessionUserID() + "'" +
                        ")";
                }

                string sSQL = "set nocount on;" +
                        "create table #TempAttributes(" +
                        "asset_id varchar(36)," +
                        "attributes varchar(max)" +
                        ");" +
                        "insert into #TempAttributes " +
                        "select aa2.asset_id, stuff((select ', '+ltrim(laa.attribute_name) +':: '+ltrim(laav.attribute_value) " +
                        "from asset_attribute aa " +
                        "join lu_asset_attribute_value laav on laav.attribute_value_id = " +
                        "aa.attribute_value_id " +
                        "join lu_asset_attribute laa on laa.attribute_id = laav.attribute_id " +
                        "where aa2.asset_id = aa.asset_id " +
                        "FOR XML PATH('')), 1, 2, '') as Attributes " +
                        "from asset_attribute aa2 " +
                        "group by asset_id;" +
                        "select a.asset_id,a.asset_name,ifnull(u.full_name,'') as [owner],a.port,a.address,a.db_name,a.connection_type,ac.username, " +
                        "case when a.is_connection_system = '1' then 'Yes' else 'No' end as [is_connection_system], ta.attributes " +
                        "from asset a " +
                        "left outer join asset_credential ac " +
                        "on ac.credential_id = a.credential_id " +
                        "left outer join user_asset_assignment uaa " +
                        "on uaa.asset_id = a.asset_id " +
                        "left outer join users u " +
                        "on u.user_id = uaa.user_id " +
                        "left outer join #TempAttributes ta " +
                        "on ta.asset_id = a.asset_id " +
                        sWhereString +
                        " order by a.asset_name;" +
                        "drop table #TempAttributes;";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }

                string sHTML = "";
                if (dt.Rows.Count == 0)
                {
                    sHTML += "No results found";
                }
                else
                {
                    int iRowsToGet = dt.Rows.Count;
                    if (iRowsToGet >= 100)
                    {
                        sHTML += "<div>Search found " + dt.Rows.Count + " results.  Displaying the first 100.</div>";
                        iRowsToGet = 99;
                    }
                    sHTML += "<ul id=\"search_asset_ul\" class=\"search_dialog_ul\">";

                    for (int i = 0; i < iRowsToGet; i++)
                    {
                        sHTML += "<li class=\"search_dialog_value\" tag=\"asset_picker_row\" asset_id=\"" + dt.Rows[i]["asset_id"].ToString() + "\" asset_name=\"" + dt.Rows[i]["asset_name"].ToString() + "\">";

                        sHTML += "<div class=\"search_dialog_value_name\">";
                        if (bAllowMultiSelect)
                            sHTML += "<input type='checkbox' name='assetcheckboxes' id='assetchk_" + dt.Rows[i]["asset_id"].ToString() + "' value='assetchk_" + dt.Rows[i]["asset_id"].ToString() + "'>";
                        sHTML += "<span>" + dt.Rows[i]["asset_name"].ToString() + "</span>";
                        sHTML += "</div>";

                        sHTML += "<span class=\"search_dialog_value_inline_item\">Owner: " + dt.Rows[i]["owner"].ToString() + "</span>";
                        sHTML += "<span class=\"search_dialog_value_inline_item\">Address: " + dt.Rows[i]["address"].ToString() + "</span>";
                        sHTML += "<span class=\"search_dialog_value_inline_item\"> Connection Type: " + dt.Rows[i]["connection_type"].ToString() + "</span>";
                        sHTML += "<div class=\"search_dialog_value_block_item italic\">Attributes: " + dt.Rows[i]["attributes"].ToString() + "</div>";
                        sHTML += "</li>";
                    }
                }
                sHTML += "</ul>";

                return sHTML;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #region "Import/Export"
        [WebMethod(EnableSession = true)]
        public string wmUpdateImportRow(string sObjectType, string sID, string sNewCode, string sNewName, string sVersion, string sImportMode)
        {
            //note not all arguments are used for all types, but are required for all

            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            ImportExport.ImportExportClass ie = new ImportExport.ImportExportClass();

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (sID != "")
                {
                    string sErr = "";
                    string sSQL = "";

                    //just to be safe...
                    sObjectType = sObjectType.ToLower();

                    switch (sObjectType)
                    {
                        case "task":
                            switch (sImportMode)
                            {
                                case "New":
                                    sSQL = "update import_task set" +
                                        " task_code = '" + sNewCode.Replace("'", "''") + "'," +
                                        " task_name = '" + sNewName.Replace("'", "''") + "'," +
                                        " version = 1.000," +
                                        " conflict = null," +
                                        " import_mode = '" + sImportMode + "'" +
                                        " where task_id = '" + sID + "'" +
                                        " and user_id = '" + sUserID + "'";
                                    break;
                                case "New Version":
                                    sSQL = "update import_task set" +
                                        " task_code = src_task_code," +
                                        " task_name = src_task_name," +
                                        " version = '" + sVersion + "'," +
                                        " conflict = null," +
                                        " import_mode = '" + sImportMode + "'" +
                                        " where task_id = '" + sID + "'" +
                                        " and user_id = '" + sUserID + "'";
                                    break;
                                //case "Overwrite":
                                //    sSQL = "update import_task set" +
                                //        " task_code = src_task_code," +
                                //        " task_name = src_task_name," +
                                //        " version = src_version," +
                                //        " conflict = null," +
                                //        " import_mode = '" + sImportMode + "'" +
                                //        " where task_id = '" + sID + "'" +
                                //        " and user_id = '" + sUserID + "'";
                                //    break;
                                default:
                                    break;
                            }
                            break;

						default:
                            break;
                    }


                    if (sSQL != "")
                    {
                        if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        {
                            throw new Exception("Unable to update.<br />" + sErr);
                        }
                    }
                    else
                        throw new Exception("Update SQL not set.<br />" + sErr);

                    //now, rescan for conflicts.
                    switch (sObjectType)
                    {
                        case "task":
                            if (!ie.IdentifyTaskConflicts(sUserID, sID, ref sErr))
                                throw new Exception("Unable to scan conflicts.<br />" + sErr);

                            break;

						default:
                            break;
                    }


                    return "";
                }
                else
                {
                    throw new Exception("Unable to update row. Missing required data. (Fields cannot be blank.)");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmImportSelected(string sTaskList)
        {
            acUI.acUI ui = new acUI.acUI();
            ImportExport.ImportExportClass ie = new ImportExport.ImportExportClass();

            string sErr = "";

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (!ie.Import(sUserID, sTaskList, ref sErr))
                    throw new Exception("Unable to import Items.<br />" + sErr);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return sErr;
        }
        [WebMethod(EnableSession = true)]
        public string wmImportDrawVersion(string sName, string sType)
        {
            dataAccess dc = new dataAccess();

            try
            {
                if (sName != "")
                {
                    string sErr = "";
                    string sSQL = "";

                    switch (sType)
                    {
                        case "task":
                            string sMaxVer = "";
                            sSQL = "select max(version) as maxversion from task " +
                               " where original_task_id = " +
                               " (select original_task_id from task where task_name = '" + sName + "' limit 1)";

                            if (!dc.sqlGetSingleString(ref sMaxVer, sSQL, ref sErr))
                                throw new Exception(sErr);


                            if (!string.IsNullOrEmpty(sMaxVer))
                            {
                                string sMinor = String.Format("{0:0.000}", (Convert.ToDouble(sMaxVer) + .001));
                                string sMajor = String.Format("{0:0.000}", Math.Round((Convert.ToDouble(sMaxVer) + .5), MidpointRounding.AwayFromZero));
                                return "&nbsp;&nbsp;<input id=\"rbMinor\" name=\"rbVersionType\" value=\"Minor\" type=\"radio\" version=\"" + sMinor + "\" />" +
                                        "New Minor Version (" + sMinor + ")<br />" +
                                        "&nbsp;&nbsp;<input id=\"rbMajor\" name=\"rbVersionType\" value=\"Major\" type=\"radio\" version=\"" + sMajor + "\"  />" +
                                        "New Major Version (" + sMajor + ")";

                                //return true;
                            };
                            break;
                        default:
                            break;
                    }
                    return "";
                }
                else
                {
                    throw new Exception("Unable to verify versions. Missing required data. (Fields cannot be blank.)");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region "Tags"
        [WebMethod(EnableSession = true)]
        public string wmGetObjectsTags(string sObjectID)
        {

            dataAccess dc = new dataAccess();
            string sSQL = null;
            string sErr = null;

            string sHTML = "";

            //NOTE: this will be from the object_tags table
            sSQL = "select tag_name from object_tags" +
                " where object_id = '" + sObjectID + "'" +
                " order by tag_name";
            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }
            else
            {
                foreach (DataRow dr in dt.Rows)
                {

                    sHTML += " <li id=\"ot_" + dr["tag_name"].ToString().Replace(" ", "") + "\" val=\"" + dr["tag_name"].ToString() + "\" class=\"tag\">";

                    sHTML += "<table class=\"object_tags_table\"><tr>";
                    sHTML += "<td style=\"vertical-align: middle;\">" + dr["tag_name"].ToString() + "</td>";
                    sHTML += "<td width=\"1px\"><img class=\"tag_remove_btn\" remove_id=\"ot_" + dr["tag_name"].ToString().Replace(" ", "") + "\" src=\"../images/icons/fileclose.png\" alt=\"\" /></td>";
                    sHTML += "</tr></table>";

                    sHTML += " </li>";
                }
            }

            return sHTML;

        }
        [WebMethod(EnableSession = true)]
        public string wmGetTagList(string sObjectID)
        {
            dataAccess dc = new dataAccess();
            string sSQL = null;
            string sErr = null;

            string sHTML = "";

            try
            {
                ////this will be from lu_tags table
                //if the passed in objectid is empty, get them all, otherwise filter by it
                if (!string.IsNullOrEmpty(sObjectID))
                {
                    sSQL = "select tag_name, tag_desc" +
                        " from lu_tags " +
                        " where tag_name not in (" +
                        " select tag_name from object_tags where object_id = '" + sObjectID + "'" +
                        ")" +
                        " order by tag_name";
                }
                else
                {
                    sSQL = "select tag_name, tag_desc" +
                        " from lu_tags" +
                        " order by tag_name";
                }
                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }

                if (dt.Rows.Count > 0)
                {
                    sHTML += "<ul>";
                    foreach (DataRow dr in dt.Rows)
                    {

                        sHTML += " <li class=\"tag_picker_tag\"" +
                           " id=\"tpt_" + dr["tag_name"].ToString() + "\"" +
                           " desc=\"" + dr["tag_desc"].ToString().Replace("\"", "").Replace("'", "") + "\"" +
                           ">";
                        sHTML += dr["tag_name"].ToString();
                        sHTML += " </li>";
                    }
                    sHTML += "</ul>";
                }
                else
                {
                    sHTML += "No Unassociated Tags exist.";
                }

                return sHTML;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        [WebMethod(EnableSession = true)]
        public void wmAddObjectTag(string sObjectID, string sObjectType, string sTagName)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            string sErr = "";
            string sSQL = "";

            int iObjectType = Int32.Parse(sObjectType);

            //fail on missing values
            if (iObjectType < 0) { throw new Exception("Invalid Object Type."); }
            if (string.IsNullOrEmpty(sObjectID) || string.IsNullOrEmpty(sTagName))
                throw new Exception("Missing or invalid Object ID or Tag Name.");


            sSQL += "insert into object_tags" +
                " (object_id, object_type, tag_name)" +
                " values (" +
                " '" + sObjectID + "'," +
                " " + iObjectType.ToString() + "," +
                " '" + sTagName + "'" +
                ")";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                throw new Exception(sErr);

            //get the enum type from an integer value
            acObjectTypes ot = (acObjectTypes)Enum.ToObject(typeof(acObjectTypes), iObjectType);
            ui.WriteObjectChangeLog(ot, sObjectID, "", "Tag [" + sTagName + "] added.");

            return;
        }
        [WebMethod(EnableSession = true)]
        public void wmRemoveObjectTag(string sObjectID, string sObjectType, string sTagName)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            string sErr = "";
            string sSQL = "";

            int iObjectType = Int32.Parse(sObjectType);

            //fail on missing values
            if (iObjectType < 0) { throw new Exception("Invalid Object Type."); }
            if (string.IsNullOrEmpty(sObjectID) || string.IsNullOrEmpty(sTagName))
                throw new Exception("Missing or invalid Object ID or Tag Name.");

            sSQL += "delete from object_tags" +
                " where" +
                " object_id = '" + sObjectID + "'" +
                " and" +
                " tag_name = '" + sTagName + "'";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                throw new Exception(sErr);

            //get the enum type from an integer value
            acObjectTypes ot = (acObjectTypes)Enum.ToObject(typeof(acObjectTypes), iObjectType);
            ui.WriteObjectChangeLog(ot, sObjectID, "", "Tag [" + sTagName + "] removed.");

            return;
        }

        //TAG EDIT PAGE
        [WebMethod(EnableSession = true)]
        public string wmDeleteTags(string sDeleteArray)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = "";

            if (sDeleteArray.Length == 0)
                return "";

            sDeleteArray = ui.QuoteUp(sDeleteArray);

            try
            {
                sSQL = "delete from object_tags where tag_name in (" + sDeleteArray.ToString() + ")";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);

                sSQL = "delete from lu_tags where tag_name in (" + sDeleteArray.ToString() + ")";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // if we made it here, so save the logs
            ui.WriteObjectDeleteLog(acObjectTypes.None, sDeleteArray.ToString(), sDeleteArray.ToString(), "Tag(s) Deleted");

            return sErr;
        }

        [WebMethod(EnableSession = true)]
        public string wmCreateTag(string sTagName, string sDescription)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = null;

            try
            {
                sSQL = "select tag_name from lu_tags where tag_name = '" + sTagName + "'";
                string sTagExists = "";
                if (!dc.sqlGetSingleString(ref sTagExists, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }
                else
                {
                    if (!string.IsNullOrEmpty(sTagExists))
                    {
                        return "Tag exists - choose another name.";
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


            try
            {
                sSQL = "insert into lu_tags (tag_name, tag_desc)" +
                    " values ('" + sTagName + "'," +
                    "'" + sDescription + "')";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);

                ui.WriteObjectAddLog(acObjectTypes.None, sTagName, sTagName, "Tag created.");
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }

            // no errors to here, so return an empty string
            return "";
        }
        [WebMethod(EnableSession = true)]
        public string wmUpdateTag(string sOldTagName, string sNewTagName, string sDescription)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = null;

            //do the description no matter what just to be quick
            sSQL = "update lu_tags set tag_desc = '" + sDescription + "' where tag_name = '" + sNewTagName + "'";
            if (!dc.sqlExecuteUpdate(sSQL, ref sErr)) { throw new Exception(sErr); }

            //don't do this unless the name has changed
            if (sNewTagName != sOldTagName)
            {
                try
                {
                    sSQL = "select tag_name from lu_tags where tag_name = '" + sNewTagName + "'";
                    string sTagExists = "";
                    if (!dc.sqlGetSingleString(ref sTagExists, sSQL, ref sErr))
                        throw new Exception(sErr);
                    else
                    {
                        if (!string.IsNullOrEmpty(sTagExists))
                            return "Tag [" + sNewTagName + "] exists - choose another name.";
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }


                try
                {
                    dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                    sSQL = "update object_tags set tag_name = '" + sNewTagName + "' where tag_name = '" + sOldTagName + "'";
                    oTrans.Command.CommandText = sSQL;
                    if (!oTrans.ExecUpdate(ref sErr))
                        throw new Exception(sErr);

                    sSQL = "update lu_tags set tag_name = '" + sNewTagName + "'" +
                            " where tag_name = '" + sOldTagName + "'";
                    oTrans.Command.CommandText = sSQL;
                    if (!oTrans.ExecUpdate(ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                    oTrans.Commit();

                    ui.WriteObjectChangeLog(acObjectTypes.None, sNewTagName, "", "Tag Updated [" + sOldTagName + "-->" + sNewTagName + "].");
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }


            // no errors to here, so return an empty string
            return "";
        }

        //END TAG EDIT
        #endregion

        #region "Admin"
        [WebMethod(EnableSession = true)]
        public string wmSetAllPollers(string sSetToStatus)
        {
            acUI.acUI ui = new acUI.acUI();
            if (!ui.UserIsInRole("Administrator"))
            {
                return "Only an Administrator can perform this action.";
            }
            else
            {
                if (sSetToStatus == "")
                {
                    return "Unable to set Pollers - 'on' or 'off' status argument is required.";
                }

                dataAccess dc = new dataAccess();
                string sErr = null;

                /*
                 Simply flips the switch on all pollers and logservers to either on or off based on the argument.
                 */
                string sStatus = (dc.IsTrue(sSetToStatus) ? "on" : "off");

                try
                {
                    if (!dc.sqlExecuteUpdate("update poller_settings set mode_off_on = '" + sStatus + "'", ref sErr)) { throw new Exception(sErr); }
                    //if (!dc.sqlExecuteUpdate("update logserver_settings set mode_off_on = '" + sStatus + "'", ref sErr)) { throw new Exception(sErr); }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

                // add security log  
                dc.addSecurityLog(ui.GetSessionUserID(), SecurityLogTypes.Security, SecurityLogActions.ConfigChange, acObjectTypes.None, "", "All Pollers set to [" + sStatus + "]", ref sErr);

                // no errors to here, so return an empty string
                return "";
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmSetScheduler(string sSetToStatus)
        {
            acUI.acUI ui = new acUI.acUI();
            if (!ui.UserIsInRole("Administrator"))
            {
                return "Only an Administrator can perform this action.";
            }
            else
            {
                if (sSetToStatus == "")
                {
                    return "Unable to set Scheduler - 'on' or 'off' status argument is required.";
                }

                dataAccess dc = new dataAccess();
                string sErr = null;

                /*
                 Simply flips the switch on all pollers and logservers to either on or off based on the argument.
                 */
                string sStatus = (dc.IsTrue(sSetToStatus) ? "on" : "off");

                try
                {
                    if (!dc.sqlExecuteUpdate("update scheduler_settings set mode_off_on = '" + sStatus + "'", ref sErr)) { throw new Exception(sErr); }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

                // add security log  
                dc.addSecurityLog(ui.GetSessionUserID(), SecurityLogTypes.Security, SecurityLogActions.ConfigChange, acObjectTypes.None, "", "Scheduler set to [" + sStatus + "]", ref sErr);

                // no errors to here, so return an empty string
                return "";
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmKickAllUsers()
        {
            acUI.acUI ui = new acUI.acUI();
            if (!ui.UserIsInRole("Administrator"))
            {
                return "Only an Administrator can perform this action.";
            }
            else
            {
                dataAccess dc = new dataAccess();
                string sErr = null;

                /*
                 Gives all users a one minute warning, then kicks them.
                 */

                try
                {
                    if (!dc.sqlExecuteUpdate("update user_session set kick=1 where user_id in " +
                        " (select user_id from users_roles where role_name <> 'Administrator')"
                        , ref sErr)) { throw new Exception(sErr); }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

                // add security log  
                dc.addSecurityLog(ui.GetSessionUserID(), SecurityLogTypes.Security, SecurityLogActions.ConfigChange, acObjectTypes.None, "", "All Users were just kicked by [" + ui.GetSessionUsername() + "]", ref sErr);

                // no errors to here, so return an empty string
                return "";
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmSetSystemCondition(string sSetToStatus)
        {
            acUI.acUI ui = new acUI.acUI();
            if (!ui.UserIsInRole("Administrator"))
            {
                return "Only an Administrator can perform this action.";
            }
            else
            {
                if (sSetToStatus == "")
                {
                    return "Unable to set System condition - 'up' or 'down' status argument is required.";
                }

                dataAccess dc = new dataAccess();
                string sErr = null;

                if (sSetToStatus == "down")
                {
                    wmKickAllUsers();
                    wmSetAllPollers("off");
                    wmSetScheduler("off");

                    //kill all running tasks
                    if (!dc.sqlExecuteUpdate("update task_instance set task_status = 'Aborting' where task_status = 'Processing'", ref sErr))
                    { throw new Exception(sErr); }

                    //lock out users
                    if (!dc.sqlExecuteUpdate("update login_security_settings set allow_login = 0", ref sErr))
                    { throw new Exception(sErr); }

                }
                else
                {
                    wmSetAllPollers("on");
                    wmSetScheduler("on");

                    //allow login
                    if (!dc.sqlExecuteUpdate("update login_security_settings set allow_login = 1", ref sErr))
                    { throw new Exception(sErr); }
                }

                // add security log  
                dc.addSecurityLog(ui.GetSessionUserID(), SecurityLogTypes.Security, SecurityLogActions.ConfigChange, acObjectTypes.None, "", "System Condition set to [" + sSetToStatus + "]", ref sErr);

                // no errors to here, so return an empty string
                return "";
            }
        }
        #endregion

        #region "Registry"
        public XDocument GetRegistry(string sObjectID, ref string sErr)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sXML = "";
				
                string sSQL = "select registry_xml from object_registry where object_id = '" + sObjectID + "'";
                if (!dc.sqlGetSingleString(ref sXML, sSQL, ref sErr))
                    throw new Exception("Error: Could not look up Registry XML." + sErr);

               if (!string.IsNullOrEmpty(sXML))
                {
                    XDocument xd = XDocument.Parse(sXML);
                    if (xd == null)
                    {
                        throw new Exception("Error: Unable to parse XML.");
                    }

                    return xd;
                }
                else
                {
                    //if the object_id is a guid, it's an object registry... add one if it's not there.
                    if (ui.IsGUID(sObjectID))
                    {
                        sSQL = "insert into object_registry values ('" + sObjectID + "', '<registry />')";
                        if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                            throw new Exception("Error: Could not create Registry." + sErr);

                        XDocument xd = XDocument.Parse("<registry />");
                        return xd;
                    }
                    else
                        throw new Exception("Error: Could not look up Registry XML.");

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public string DrawRegistryDocument(XDocument xd)
        {
            string sHTML = "";
			if (xd.XPathSelectElement("registry") != null)
            {
                sHTML += "<div class=\"ui-widget-content ui-corner-bottom registry\">";
                sHTML += "  <div class=\"ui-state-default registry_section_header\" xpath=\"registry\">"; //header
                sHTML += "      <div class=\"registry_section_header_title\">Registry</div>";

                sHTML += "<div class=\"registry_section_header_icons\">"; //step header icons

                sHTML += "<span id=\"registry_refresh_btn\" class=\"pointer\">" +
                    "<img style=\"width:10px; height:10px;\" src=\"../images/icons/reload_16.png\"" +
                    " alt=\"\" title=\"Refresh\" /></span>";

                sHTML += "<span class=\"registry_node_add_btn pointer\"" +
                    " xpath=\"registry\">" +
                    "<img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\"" +
                    " alt=\"\" title=\"Add another...\" /></span>";

                sHTML += "</div>";  //end step header icons
                sHTML += "  </div>"; //end header

                foreach (XElement xe in xd.XPathSelectElement("registry").Nodes())
                {
                    sHTML += DrawRegistryNode(xe, "registry/" + xe.Name.ToString());
                }

                sHTML += "</div>";
            }

            if (sHTML.Length == 0)
                sHTML = "Registry is empty.";
            return sHTML;
        }
        private string DrawRegistryNode(XElement xeNode, string sXPath)
        {
            string sHTML = "";

            string sNodeLabel = xeNode.Name.ToString();

            IDictionary<string, int> dictNodes = new Dictionary<string, int>();


            //if a node has children we'll draw it with some hierarchical styling.
            //AND ALSO if it's editable, even if it has no children, we'll still draw it as a container.
            if (xeNode.HasElements)
            {
                string sGroupID = Guid.NewGuid().ToString();

                sHTML += "<div class=\"ui-widget-content ui-corner-bottom registry_section\" id=\"" + sGroupID + "\">"; //this section

                sHTML += "  <div class=\"ui-state-default registry_section_header\" xpath=\"" + sXPath + "\">"; //header
                sHTML += "      <div class=\"registry_section_header_title editable\" id=\"" + Guid.NewGuid().ToString() + "\">" + sNodeLabel + "</div>";

                sHTML += "<div class=\"registry_section_header_icons\">"; //step header icons

                sHTML += "<span class=\"registry_node_add_btn pointer\"" +
                    " xpath=\"" + sXPath + "\">" +
                    "<img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\"" +
                    " alt=\"\" title=\"Add another...\" /></span>";

                sHTML += "<span class=\"registry_node_remove_btn pointer\" xpath_to_delete=\"" + sXPath + "\" id_to_remove=\"" + sGroupID + "\">";
                sHTML += "<img style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                    " alt=\"\" title=\"Remove\" /></span>";

                sHTML += "</div>";  //end step header icons



                sHTML += "  </div>"; //end header

                foreach (XElement xeChildNode in xeNode.Nodes())
                {
                    string sChildNodeName = xeChildNode.Name.ToString();
                    string sChildXPath = sXPath + "/" + xeChildNode.Name.ToString();

                    //here's the magic... are there any children nodes here with the SAME NAME?
                    //if so they need an index on the xpath
                    if (xeNode.XPathSelectElements(sChildNodeName).Count() > 1)
                    {
                        //since the document won't necessarily be in perfect order,
                        //we need to keep track of same named nodes and their indexes.
                        //so, stick each array node up in a lookup table.

                        //is it already in my lookup table?
                        int iLastIndex = 0;
                        dictNodes.TryGetValue(sChildNodeName, out iLastIndex);
                        if (iLastIndex == 0)
                        {
                            //not there, add it
                            iLastIndex = 1;
                            dictNodes.Add(sChildNodeName, iLastIndex);
                        }
                        else
                        {
                            //there, increment it and set it
                            iLastIndex++;
                            dictNodes[sChildNodeName] = iLastIndex;
                        }

                        sChildXPath = sChildXPath + "[" + iLastIndex.ToString() + "]";
                    }

                    sHTML += DrawRegistryNode(xeChildNode, sChildXPath);

                }


                sHTML += "</div>"; //end section
            }
            else
            {
                sHTML += DrawRegistryItem(xeNode, sXPath);
            }


            return sHTML;
        }
        private string DrawRegistryItem(XElement xe, string sXPath)
        {
            string sHTML = "";
            string sEncrypt = "false";

            if (xe.Attribute("encrypt") != null)
                sEncrypt = xe.Attribute("encrypt").Value;

            //if the node value is empty or encrypted, we still need something to click on to edit the value
            string sNodeValue = (sEncrypt == "true" ? "(********)" : (xe.Value == "" ? "(empty)" : xe.Value));
            string sNodeLabel = xe.Name.ToString();


            string sGroupID = Guid.NewGuid().ToString();

            sHTML += "<div class=\"ui-widget-content ui-corner-tl ui-corner-bl registry_node\" xpath=\"" + sXPath + "\" id=\"" + sGroupID + "\">";
            sHTML += "<span class=\"registry_node_label editable\" id=\"" + Guid.NewGuid().ToString() + "\">" + sNodeLabel +
                "</span> : <span class=\"registry_node_value editable\" id=\"" + Guid.NewGuid().ToString() + "\" encrypt=\"" + sEncrypt + "\">" + sNodeValue + "</span>" + Environment.NewLine;

            sHTML += "<div class=\"registry_section_header_icons\">"; //step header icons

            sHTML += "<span class=\"registry_node_add_btn pointer\"" +
                " xpath=\"" + sXPath + "\">" +
                "<img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\"" +
                " alt=\"\" title=\"Add another...\" /></span>";

            sHTML += "<span class=\"registry_node_remove_btn pointer\" xpath_to_delete=\"" + sXPath + "\" id_to_remove=\"" + sGroupID + "\">";
            sHTML += "<img style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                " alt=\"\" title=\"Remove\" /></span>";

            sHTML += "</div>";
            sHTML += "</div>";

            return sHTML;
        }

        [WebMethod(EnableSession = true)]
        public string wmGetRegistry(string sObjectID)
        {
            //NOTE: there is only one global registry... id=1
            //rather than spread that all over the script,if the objectid is "global"
            //we'll change it to a 1
            if (sObjectID == "global") sObjectID = "1";

            string sErr = "";

            XDocument xd = GetRegistry(sObjectID, ref sErr);
            if (sErr != "") throw new Exception(sErr);

			return DrawRegistryDocument(xd);
        }
        [WebMethod(EnableSession = true)]
        public void wmAddRegistryNode(string sObjectID, string sXPath, string sName)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();

            //fail on missing values
            if (string.IsNullOrEmpty(sXPath))
                throw new Exception("Missing XPath to add to.");

            //fail on empty name
            if (string.IsNullOrEmpty(sName))
                throw new Exception("Node Name required.");

            string sNewXML = "<" + sName + " />";

            if (sObjectID == "global") sObjectID = "1";
            ft.AddNodeToRegistry(sObjectID, sXPath, sNewXML);

            return;
        }
        [WebMethod(EnableSession = true)]
        public void wmDeleteRegistryNode(string sObjectID, string sXPath)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();

            //fail on missing values
            if (string.IsNullOrEmpty(sXPath))
                throw new Exception("Missing XPath to add to.");

            if (sObjectID == "global") sObjectID = "1";
            ft.RemoveNodeFromXMLColumn("object_registry", "registry_xml", "object_id = '" + sObjectID + "'", sXPath);

            return;
        }
        [WebMethod(EnableSession = true)]
        public void wmUpdateRegistryValue(string sObjectID, string sXPath, string sValue, string sEncrypt)
        {
            dataAccess dc = new dataAccess();
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();

            //fail on missing values
            if (string.IsNullOrEmpty(sXPath))
                throw new Exception("Missing XPath to update.");

            //masked means update an attribute AND encrypt the value
            sEncrypt = (dc.IsTrue(sEncrypt) ? "true" : "false");
            sValue = (dc.IsTrue(sEncrypt) ? dc.EnCrypt(sValue) : sValue);

            //update
            if (sObjectID == "global") sObjectID = "1";
            ft.SetNodeValueinXMLColumn("object_registry", "registry_xml", "object_id = '" + sObjectID + "'", sXPath, sValue);
            ft.SetNodeAttributeinXMLColumn("object_registry", "registry_xml", "object_id = '" + sObjectID + "'", sXPath, "encrypt", sEncrypt);

            return;
        }
        [WebMethod(EnableSession = true)]
        public void wmRenameRegistryNode(string sObjectID, string sXPath, string sOldName, string sNewName)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();

            //fail on missing values
            if (string.IsNullOrEmpty(sXPath))
                throw new Exception("Missing XPath to update.");

            if (sObjectID == "global") sObjectID = "1";
            ft.SetNodeNameinXMLColumn("object_registry", "registry_xml", "object_id = '" + sObjectID + "'", sXPath, sOldName, sNewName);

            return;
        }
        #endregion "Registry"

        #region "Ecosystems"
        [WebMethod(EnableSession = true)]
        public string wmUpdateEcosystemDetail(string sEcosystemID, string sColumn, string sValue)
        {
            dataAccess dc = new dataAccess();
            
            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (ui.IsGUID(sEcosystemID) && ui.IsGUID(sUserID))
                {
                    string sErr = "";
                    string sSQL = "";

                    //we encoded this in javascript before the ajax call.
                    //the safest way to unencode it is to use the same javascript lib.
                    //(sometimes the javascript and .net libs don't translate exactly, google it.)
                    sValue = ui.unpackJSON(sValue);

                    // check for existing name
                    if (sColumn == "ecosystem_name")
                    {
                        sSQL = "select ecosystem_id from ecosystem where " +
                                " ecosystem_name = '" + sValue.Replace("'", "''") + "'" +
                                " and ecosystem_id <> '" + sEcosystemID + "'";

                        string sValueExists = "";
                        if (!dc.sqlGetSingleString(ref sValueExists, sSQL, ref sErr))
                            throw new Exception("Unable to check for existing names [" + sEcosystemID + "]." + sErr);

                        if (!string.IsNullOrEmpty(sValueExists))
                            return sValue + " exists, please choose another value.";
                    }

                    string sSetClause = sColumn + "='" + sValue.Replace("'", "''") + "'";

                    //some columns on this table allow nulls... in their case an empty sValue is a null
                    if (sColumn == "ecosystem_desc")
                    {
                        if (sValue.Replace(" ", "").Length == 0)
                            sSetClause = sColumn + " = null";
                        else
                            sSetClause = sColumn + "='" + sValue.Replace("'", "''") + "'";
                    }

                    sSQL = "update ecosystem set " + sSetClause + " where ecosystem_id = '" + sEcosystemID + "'";
                    //}


                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to update Ecosystem [" + sEcosystemID + "]." + sErr);

                    ui.WriteObjectChangeLog(Globals.acObjectTypes.Ecosystem, sEcosystemID, sColumn, sValue);
                }
                else
                {
                    throw new Exception("Unable to update Ecosystem. Missing or invalid id [" + sEcosystemID + "].");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return "";
        }

        [WebMethod(EnableSession = true)]
        public string wmDeleteEcosystems(string sDeleteArray)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = "";

            if (sDeleteArray.Length == 0)
                return "";

            sDeleteArray = ui.QuoteUp(sDeleteArray);

            try
            {
                sSQL = "delete from ecosystem_object where ecosystem_id in (" + sDeleteArray.ToString() + ")";
                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);

                sSQL = "delete from ecosystem where ecosystem_id in (" + sDeleteArray.ToString() + ")";
                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // if we made it here, so save the logs
            ui.WriteObjectDeleteLog(acObjectTypes.Ecosystem, "", "", "Ecosystems(s) Deleted [" + sDeleteArray.ToString() + "]");

            return sErr;
        }

        [WebMethod(EnableSession = true)]
        public string wmCreateEcosystem(string sName, string sDescription, string sEcotemplateID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = null;

            try
            {
                sSQL = "select ecosystem_name from ecosystem where ecosystem_name = '" + sName + "'";
                string sExists = "";
                if (!dc.sqlGetSingleString(ref sExists, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }
                else
                {
                    if (!string.IsNullOrEmpty(sExists))
                    {
                        return "Ecosystem exists - choose another name.";
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


            try
            {
                string sNewID = Guid.NewGuid().ToString();
                string sCloudAccountID = ui.GetCloudAccountID();

                if (string.IsNullOrEmpty(sCloudAccountID))
                    return "Unable to create - No Cloud Account selected.";
                if (string.IsNullOrEmpty(sEcotemplateID))
                    return "Unable to create - An Ecosystem Template is required..";

                sSQL = "insert into ecosystem (ecosystem_id, ecosystem_name, ecosystem_desc, account_id, ecotemplate_id)" +
                    " values ('" + sNewID + "'," +
                    " '" + sName + "'," +
                    (string.IsNullOrEmpty(sDescription) ? " null" : " '" + sDescription + "'") + "," +
                    " '" + sCloudAccountID + "'," +
                    " '" + sEcotemplateID + "'" +
                    ")";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);

                ui.WriteObjectAddLog(acObjectTypes.Ecosystem, sNewID, sName, "Ecosystem created.");

                return sNewID;
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }


        [WebMethod(EnableSession = true)]
        public void wmAddEcosystemObjects(string sEcosystemID, string sObjectType, string sObjectIDs)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            string sErr = "";

            if (string.IsNullOrEmpty(sEcosystemID) || string.IsNullOrEmpty(sObjectType) || string.IsNullOrEmpty(sObjectIDs))
                throw new Exception("Missing or invalid Ecosystem ID, Cloud Object Type or Object ID.");

            ArrayList aObjectIDs = new ArrayList(sObjectIDs.Split(','));
            foreach (string sObjectID in aObjectIDs)
            {

                string sSQL = "insert into ecosystem_object " +
                     " (ecosystem_id, ecosystem_object_id, ecosystem_object_type, added_dt)" +
                     " values (" +
                     " '" + sEcosystemID + "'," +
                     " '" + sObjectID + "'," +
                     " '" + sObjectType + "'," +
                     " now() " +
                     ")";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                {
                    //don't throw an error if its just a PK collision.  That just means it's already there.
                    if (sErr.IndexOf("Duplicate entry") == 0)
                        throw new Exception(sErr);
                }
            }

            ui.WriteObjectChangeLog(acObjectTypes.Ecosystem, sEcosystemID, "", "Objects Added : {" + sObjectIDs + "}");

            return;
        }

        [WebMethod(EnableSession = true)]
        public string wmDeleteEcosystemObject(string sEcosystemID, string sObjectType, string sObjectID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = "";

            if (sObjectID.Length == 0)
                return "";

            try
            {
                sSQL = "delete from ecosystem_object" +
                    " where ecosystem_id ='" + sEcosystemID + "'" +
                    " and ecosystem_object_id ='" + sObjectID + "'" +
                    " and ecosystem_object_type ='" + sObjectType + "'";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // if we made it here, so save the logs
            ui.WriteObjectDeleteLog(acObjectTypes.Ecosystem, "", "", "Object [" + sObjectID + "] removed from Ecosystem [" + sEcosystemID + "]");

            return sErr;
        }


        //Eco Templates
        [WebMethod(EnableSession = true)]
        public string wmUpdateEcoTemplateDetail(string sEcoTemplateID, string sColumn, string sValue)
        {
            dataAccess dc = new dataAccess();
            
            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (ui.IsGUID(sEcoTemplateID) && ui.IsGUID(sUserID))
                {
                    string sErr = "";
                    string sSQL = "";

                    //we encoded this in javascript before the ajax call.
                    //the safest way to unencode it is to use the same javascript lib.
                    //(sometimes the javascript and .net libs don't translate exactly, google it.)
                    sValue = ui.unpackJSON(sValue);

                    // check for existing name
                    if (sColumn == "ecotemplate_name")
                    {
                        sSQL = "select ecotemplate_id from ecotemplate where " +
                                " ecotemplate_name = '" + sValue.Replace("'", "''") + "'" +
                                " and ecotemplate_id <> '" + sEcoTemplateID + "'";

                        string sValueExists = "";
                        if (!dc.sqlGetSingleString(ref sValueExists, sSQL, ref sErr))
                            throw new Exception("Unable to check for existing names [" + sEcoTemplateID + "]." + sErr);

                        if (!string.IsNullOrEmpty(sValueExists))
                            return sValue + " exists, please choose another value.";
                    }

                    string sSetClause = sColumn + "='" + sValue.Replace("'", "''") + "'";

                    //some columns on this table allow nulls... in their case an empty sValue is a null
                    if (sColumn == "ecotemplate_desc")
                    {
                        if (sValue.Replace(" ", "").Length == 0)
                            sSetClause = sColumn + " = null";
                        else
                            sSetClause = sColumn + "='" + sValue.Replace("'", "''") + "'";
                    }

                    sSQL = "update ecotemplate set " + sSetClause + " where ecotemplate_id = '" + sEcoTemplateID + "'";
                    //}


                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to update Eco Template [" + sEcoTemplateID + "]." + sErr);

                    ui.WriteObjectChangeLog(Globals.acObjectTypes.EcoTemplate, sEcoTemplateID, sColumn, sValue);
                }
                else
                {
                    throw new Exception("Unable to update Eco Template. Missing or invalid id [" + sEcoTemplateID + "].");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return "";
        }

        [WebMethod(EnableSession = true)]
        public string wmCreateEcotemplate(string sName, string sDescription)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = null;

            try
            {
                sSQL = "select ecotemplate_name from ecotemplate where ecotemplate_name = '" + sName + "'";
                string sExists = "";
                if (!dc.sqlGetSingleString(ref sExists, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }
                else
                {
                    if (!string.IsNullOrEmpty(sExists))
                    {
                        return "Eco Template exists - choose another name.";
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


            try
            {
                string sNewID = Guid.NewGuid().ToString();

                sSQL = "insert into ecotemplate (ecotemplate_id, ecotemplate_name, ecotemplate_desc)" +
                    " values ('" + sNewID + "'," +
                    " '" + sName + "'," +
                    (string.IsNullOrEmpty(sDescription) ? " null" : " '" + sDescription + "'") + ")";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);

                ui.WriteObjectAddLog(acObjectTypes.Ecosystem, sNewID, sName, "Ecotemplate created.");

                return sNewID;
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmDeleteEcotemplates(string sDeleteArray)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = "";

            if (sDeleteArray.Length == 0)
                return "";

            try
            {
                sDeleteArray = ui.QuoteUp(sDeleteArray);

                //can't delete templates that have references...
                int iResults = 0;
                sSQL = "select count(*) from ecosystem where ecotemplate_id in (" + sDeleteArray.ToString() + ")";
                if (!dc.sqlGetSingleInteger(ref iResults, sSQL, ref sErr))
                    throw new Exception(sErr);

                if (iResults == 0)
                {
                    sSQL = "delete from ecotemplate_action where ecotemplate_id in (" + sDeleteArray.ToString() + ")";
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception(sErr);

                    sSQL = "delete from ecotemplate where ecotemplate_id in (" + sDeleteArray.ToString() + ")";
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception(sErr);

                    // if we made it here, so save the logs
                    ui.WriteObjectDeleteLog(acObjectTypes.EcoTemplate, "", "", "Eco Templates(s) Deleted [" + sDeleteArray.ToString() + "]");
                }
                else
                    sErr = "Unable to delete Ecosystem Template - " + iResults.ToString() + " Ecosystems reference it.";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return sErr;
        }

        /*These methods are for populating the Action TAB on the Ecosystem page.*/
        [WebMethod(EnableSession = true)]
        public string wmGetEcotemplateActionCategories(string sEcoTemplateID)
        {
            dataAccess dc = new dataAccess();
            string sSQL = null;
            string sErr = null;

            string sHTML = "";

            try
            {
                if (!string.IsNullOrEmpty(sEcoTemplateID))
                {
                    string sIcon = "action_category_48.png";

                    //the 'All' category is hardcoded
                    sHTML += " <div class=\"ui-state-focus action_btn_focus ui-widget-content ui-corner-all action_btn action_category\" id=\"ac_all\">" +
                        "<img src=\"../images/actions/" + sIcon + "\" alt=\"\" />" +
                        "<span>All</span></div>";


                    sSQL = "select distinct category" +
                        " from ecotemplate_action" +
                        " where ecotemplate_id = '" + sEcoTemplateID + "'" +
                        " and category != ''" +
                        " and category != 'all'" +
                        " order by category";

                    DataTable dt = new DataTable();
                    if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            sHTML += " <div class=\"ui-widget-content ui-corner-all action_btn action_category\"" +
                               " id=\"" + dr["category"].ToString() + "\"" +
                               ">";
                            sHTML += "<img src=\"../images/actions/" + sIcon + "\" alt=\"\" />";
                            sHTML += "<span>" + dr["category"].ToString() + "</span>";
                            sHTML += " </div>";
                        }
                    }
                }
                else
                {
                    throw new Exception("Unable to get Action Categories - Missing EcoTemplate ID");
                }

                return sHTML;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        [WebMethod(EnableSession = true)]
        public string wmGetEcotemplateActionButtons(string sEcoTemplateID)
        {
            dataAccess dc = new dataAccess();
            string sSQL = null;
            string sErr = null;

            string sHTML = "";

            try
            {
                if (!string.IsNullOrEmpty(sEcoTemplateID))
                {

                    //this will need to change if we add version - as it might be default and might be explicit.

                    //right now it's the default!

                    sSQL = "select ea.action_id, ea.action_name, ea.category, ea.action_desc, ea.original_task_id, ea.action_icon," +
                        " t.task_id, t.task_name, t.version" +
                        " from ecotemplate_action ea" +
                        " left outer join task t on ea.original_task_id = t.original_task_id" +
                        " and t.default_version = 1" +
                        " where ea.ecotemplate_id = '" + sEcoTemplateID + "'" +
                        " order by ea.action_name";

                    DataTable dt = new DataTable();
                    if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                    if (dt.Rows.Count > 0)
                    {
                        //int i = 0;
                        //sHTML += "<table id=\"actions_table\">";

                        foreach (DataRow dr in dt.Rows)
                        {
                            string sActionID = dr["action_id"].ToString();
                            string sActionName = dr["action_name"].ToString();
                            string sTaskID = dr["task_id"].ToString();
                            string sTaskName = dr["task_name"].ToString();
                            string sVersion = dr["version"].ToString();
                            string sIcon = (string.IsNullOrEmpty(dr["action_icon"].ToString()) ? "action_default_48.png" : dr["action_icon"].ToString());

                            //sDesc is the tooltip
                            string sDesc = "<p>" + dr["action_desc"].ToString().Replace("\"", "").Replace("'", "") +
                                "</p><p>" + sTaskName + "</p>";


                            //string sAction = "";

                            sHTML += " <div class=\"action\"" +
                               " id=\"" + sActionID + "\"" +
                               " action=\"" + sActionName + "\"" +
                               " task_id=\"" + sTaskID + "\"" +
                               " task_name=\"" + sTaskName + "\"" +
                               " version=\"" + sVersion + "\"" +
                               " category=\"" + dr["category"].ToString() + "\"" +
                               ">";

                            sHTML += "<div class=\"ui-widget-content ui-corner-all action_btn action_inner\">"; //outer div with no styling at all

                            sHTML += "<div class=\"step_header_title\">";
                            sHTML += "</div>";

                            sHTML += "<div class=\"step_header_icons\">";
                            sHTML += "<img class=\"action_help_btn\"" +
                                " src=\"../images/icons/info.png\" alt=\"\" style=\"width: 12px; height: 12px;\"" +
                                " title=\"" + sDesc + "\" />";
                            sHTML += "</div>";

                            //gotta clear the floats
                            sHTML += "<div class=\"clearfloat\">";

                            sHTML += "<img src=\"../images/actions/" + sIcon + "\" alt=\"\" />";


                            sHTML += " </div>";
                            sHTML += " </div>"; //end inner div

                            sHTML += "<span class=\"action_name\">";
                            sHTML += sActionName;
                            sHTML += "</span>";


                            sHTML += " </div>"; //end outer div


                            // //we are building this as a two-column stack.  We'll use tables.
                            // //and a modulus to see if we're on an even numbered pass
                            // if (i % 2 == 0)
                            // {
                            //     //even - a first column
                            //     sHTML += "<tr><td width=\"50%\">" + sAction + "</td>";
                            // }
                            // else
                            // {
                            //     //odd - a second column
                            //     sHTML += "</td><td width=\"50%\">" + sAction + "</td></tr>";
                            // }


                            //i++;
                        }

                        //sHTML += "</table>";

                        //need a clearfloat div here, as javascript flow logic will be 
                        //dynamically adding and removing floats
                        sHTML += "<div class=\"clearfloat\"></div>";

                    }
                }
                else
                {
                    throw new Exception("Unable to get Actions - Missing EcoTemplate ID");
                }

                return sHTML;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        /*This gets the Ecotemplates list on the EcoTemplate Edit page.*/
        [WebMethod(EnableSession = true)]
        public string wmGetEcotemplateEcosystems(string sEcoTemplateID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            string sSQL = null;
            string sErr = null;

            string sHTML = "";

            try
            {
                if (!string.IsNullOrEmpty(sEcoTemplateID))
                {
                    sSQL = "select ecosystem_id, ecosystem_name, ecosystem_desc" +
                        " from ecosystem" +
                        " where ecotemplate_id = '" + sEcoTemplateID + "'" +
                        " and account_id = '" + ui.GetCloudAccountID() + "'" +
                        " order by ecosystem_name";

                    DataTable dt = new DataTable();
                    if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                    if (dt.Rows.Count > 0)
                    {
                        sHTML += "<ul>";

                        foreach (DataRow dr in dt.Rows)
                        {
                            string sEcosystemID = dr["ecosystem_id"].ToString();
                            string sEcosystemName = dr["ecosystem_name"].ToString();
                            string sDesc = dr["ecosystem_desc"].ToString().Replace("\"", "").Replace("'", "");

                            sHTML += "<li class=\"ui-widget-content ui-corner-all\"" +
                                " ecosystem_id=\"" + sEcosystemID + "\"" +
                                "\">";
                            sHTML += "<div class=\"step_header_title ecosystem_name pointer\">" + sEcosystemName + "</div>";

                            sHTML += "<div class=\"step_header_icons\">";

                            //if there's a description, show a tooltip
                            if (!string.IsNullOrEmpty(sDesc))
                                sHTML += "<img src=\"../images/icons/info.png\" class=\"ecosystem_tooltip trans50\" title=\"" + sDesc + "\" />";

                            sHTML += "</div>";
                            sHTML += "<div class=\"clearfloat\"></div>";
                            sHTML += "</li>";
                        }

                        sHTML += "</ul>";
                    }
                }
                else
                {
                    throw new Exception("Unable to get Ecosystems - Missing EcoTemplate ID");
                }

                return sHTML;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }


        /*And these are for populating the Action LIST on the EcoTemplate Edit page.*/
        private string DrawEcotemplateAction(DataRow dr)
        {
            acUI.acUI ui = new acUI.acUI();
            dataAccess dc = new dataAccess();

            string sSQL = null;
            string sErr = null;

            string sHTML = "";

            //string sActionID = dr["action_id"].ToString();
            string sActionName = dr["action_name"].ToString();
            string sCategory = dr["category"].ToString();
            string sDesc = (string.IsNullOrEmpty(dr["action_desc"].ToString()) ? "" : dr["action_desc"].ToString());
            string sOriginalTaskID = dr["original_task_id"].ToString();
            string sTaskID = dr["task_id"].ToString();
            string sTaskName = dr["task_name"].ToString();
            string sVersion = (string.IsNullOrEmpty(dr["task_version"].ToString()) ? "" : dr["task_version"].ToString());
            string sTaskParameterXML = (string.IsNullOrEmpty(dr["task_param_xml"].ToString()) ? "" : dr["task_param_xml"].ToString());
            //string sActionParameterXML = (string.IsNullOrEmpty(dr["action_param_xml"].ToString()) ? "" : dr["action_param_xml"].ToString());

            sHTML += "<div class=\"ui-state-default step_header\">";

            sHTML += "<div class=\"step_header_title\">";
            sHTML += "<span class=\"action_category_lbl\">" + (sCategory != "" ? sCategory + " - " : "") + "</span>";
            sHTML += "<span class=\"action_name_lbl\">" + sActionName + "</span>";
            sHTML += "</div>";

            sHTML += "<div class=\"step_header_icons\">";
            sHTML += "<img class=\"action_remove_btn\" remove_id=\"" + sActionName + "\"" +
                " src=\"../images/icons/fileclose.png\" alt=\"\" style=\"width: 12px; height: 12px;\"" +
                " title=\"Delete\" />";
            sHTML += "</div>";

            sHTML += "</div>";


            //gotta clear the floats
            sHTML += "<div class=\"ui-widget-content step_detail\">";

            //Action Name
            sHTML += "Action: " + Environment.NewLine;
            sHTML += "<input type=\"text\" " +
                " column=\"action_name\"" +
                " class=\"code\"" +
                " value=\"" + sActionName + "\" />"
                + Environment.NewLine;

            //Category
            sHTML += "Category: " + Environment.NewLine;
            sHTML += "<input type=\"text\" " +
                " column=\"category\"" +
                " class=\"code\"" +
                " value=\"" + sCategory + "\" />"
                + Environment.NewLine;

            sHTML += "<br />";

            //Description
            sHTML += "Description:<br />" + Environment.NewLine;
            sHTML += "<textarea rows=\"4\"" +
                " column=\"action_desc\"" +
                " class=\"code\"" +
                ">" + sDesc + "</textarea>" + Environment.NewLine;

            sHTML += "<br />";

            //Task
            //hidden field
            sHTML += "<input type=\"hidden\" " +
                " column=\"original_task_id\"" +
                " value=\"" + sOriginalTaskID + "\" />"
                + Environment.NewLine;

            //visible stuff
            sHTML += "Task: " + Environment.NewLine;
            sHTML += "<input type=\"text\"" +
                " onkeydown=\"return false;\"" +
                " onkeypress=\"return false;\"" +
                " is_required=\"true\"" +
                " class=\"code w75pct task_name\"" +
                " value=\"" + sTaskName + "\" />" + Environment.NewLine;
            if (sTaskID != "")
            {
                sHTML += "<img class=\"task_open_btn pointer\" alt=\"Edit Task\"" +
                    " task_id=\"" + sTaskID + "\"" +
                    " src=\"../images/icons/kedit_16.png\" />" + Environment.NewLine;
                sHTML += "<img class=\"task_print_btn pointer\" alt=\"View Task\"" +
                    " task_id=\"" + sTaskID + "\"" +
                    " src=\"../images/icons/printer.png\" />" + Environment.NewLine;
            }

            //NOT SURE if this is a requirement.  The looping actually slows things down quite a bit
            //so I don't mind not doing it.

            //versions
            if (ui.IsGUID(sOriginalTaskID))
            {
                sHTML += "<br />";
                sHTML += "Version: " + Environment.NewLine;
                sHTML += "<select " +
                    " column=\"task_version\"" +
                    " reget_on_change=\"true\">" + Environment.NewLine;
                //default
                sHTML += "<option " + (sVersion == "" ? " selected=\"selected\"" : "") + " value=\"\">Default</option>" + Environment.NewLine;

                sSQL = "select version from task" +
                    " where original_task_id = '" + sOriginalTaskID + "'" +
                    " order by version";
                DataTable dtVer = new DataTable();
                if (!dc.sqlGetDataTable(ref dtVer, sSQL, ref sErr))
                {
                    return "Database Error:" + sErr;
                }

                if (dtVer.Rows.Count > 0)
                {
                    foreach (DataRow drVer in dtVer.Rows)
                    {
                        sHTML += "<option " + (sVersion == drVer["version"].ToString() ? " selected=\"selected\"" : "") +
                            " value=\"" + drVer["version"] + "\">" +
                            drVer["version"].ToString() + "</option>" + Environment.NewLine;
                    }
                }
                else
                {
                    return "Unable to continue - Cannot find Version for Task [" + sOriginalTaskID + "].";
                }

                sHTML += "</select>" + Environment.NewLine;
            }


            //we have the parameter xml for the task here.
            //let's peek.  if there are parameters, show the button
            //if any are "required", but a ! by it.
            if (sTaskParameterXML != "")
            {
                XDocument xDoc = XDocument.Parse(sTaskParameterXML);
                if (xDoc != null)
                {
                    IEnumerable<XElement> xParams = xDoc.XPathSelectElements("/parameters/parameter");
                    if (xParams != null)
                    {
                        //there are task params!  draw the edit link

                        //are any of them required?
                        IEnumerable<XElement> xRequired = xDoc.XPathSelectElements("/parameters/parameter[@required='true']");

                        ////and most importantly, do the required ones have values?
                        ////this should be in a loop of xRequired, and looking in the other XML document.
                        //IEnumerable<XElement> xEmpty1 = xDoc.XPathSelectElements("/parameters/parameter[@required='true']/values[not(text())]");
                        //IEnumerable<XElement> xEmpty2 = xDoc.XPathSelectElements("/parameters/parameter[@required='true']/values[not(node())]");


                        sHTML += "<hr />";
                        sHTML += "<div class=\"action_param_edit_btn pointer\" style=\"vertical-align: bottom;\">";

                        if (xRequired.Count() > 0) // && (xEmpty1.Count() > 0 || xEmpty2.Count() > 0))
                            sHTML += "<img src=\"../images/icons/status_unknown_16.png\" />";
                        else
                            sHTML += "<img src=\"../images/icons/edit_16.png\" />";

                        sHTML += "<span> Edit Parameters</span>";
                        sHTML += "<span> (" + xParams.Count() + ")</span>";


                        //close the div
                        sHTML += "</div>";
                    }
                }
            }

            sHTML += "</div>";


            return sHTML;
        }
        [WebMethod(EnableSession = true)]
        public string wmGetEcotemplateActions(string sEcoTemplateID)
        {
            
            dataAccess dc = new dataAccess();

            string sSQL = null;
            string sErr = null;

            string sHTML = "";

            try
            {
                if (!string.IsNullOrEmpty(sEcoTemplateID))
                {
                    sSQL = "select ea.action_id, ea.action_name, ea.category, ea.action_desc, ea.original_task_id, ea.task_version," +
                        " t.task_id, t.task_name," +
                        " ea.parameter_defaults as action_param_xml, t.parameter_xml as task_param_xml" +
                        " from ecotemplate_action ea" +
                        " join task t on ea.original_task_id = t.original_task_id" +
                        " and t.default_version = 1" +
                        " where ea.ecotemplate_id = '" + sEcoTemplateID + "'" +
                        " order by ea.category, ea.action_name";

                    DataTable dt = new DataTable();
                    if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            sHTML += " <li class=\"ui-widget-content ui-corner-all action pointer\" id=\"ac_" + dr["action_id"].ToString() + "\">";
                            sHTML += DrawEcotemplateAction(dr);
                            sHTML += " </li>";
                        }
                    }
                }
                else
                {
                    throw new Exception("Unable to get Actions - Missing EcoTemplate ID");
                }

                return sHTML;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        [WebMethod(EnableSession = true)]
        public string wmGetEcotemplateAction(string sActionID)
        {
            
            dataAccess dc = new dataAccess();

            string sSQL = null;
            string sErr = null;

            string sHTML = "";

            try
            {
                if (!string.IsNullOrEmpty(sActionID))
                {
                    sSQL = "select ea.action_id, ea.action_name, ea.category, ea.action_desc, ea.original_task_id, ea.task_version," +
                        " t.task_id, t.task_name," +
                        " ea.parameter_defaults as action_param_xml, t.parameter_xml as task_param_xml" +
                        " from ecotemplate_action ea" +
                        " join task t on ea.original_task_id = t.original_task_id" +
                        " and t.default_version = 1" +
                        " where ea.action_id = '" + sActionID + "'";

                    DataRow dr = null;
                    if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                        throw new Exception(sErr);

                    //GetDataRow returns a message if there are no rows...
                    if (string.IsNullOrEmpty(sErr))
                    {
                        sHTML = DrawEcotemplateAction(dr);
                    }
                }
                else
                {
                    throw new Exception("Unable to get Actions - Missing EcoTemplate ID");
                }

                return sHTML;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        //And this is for updating a single field on an Action (just like a Task Step)
        [WebMethod(EnableSession = true)]
        public string wmUpdateEcoTemplateAction(string sEcoTemplateID, string sActionID, string sColumn, string sValue)
        {
            dataAccess dc = new dataAccess();
            
            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (ui.IsGUID(sEcoTemplateID) && ui.IsGUID(sUserID))
                {
                    string sErr = "";
                    string sSQL = "";

                    //we encoded this in javascript before the ajax call.
                    //the safest way to unencode it is to use the same javascript lib.
                    //(sometimes the javascript and .net libs don't translate exactly, google it.)
                    sValue = ui.unpackJSON(sValue);

                    // check for existing name
                    if (sColumn == "action_name")
                    {
                        sSQL = "select action_id from ecotemplate_action where " +
                                " action_name = '" + sValue.Replace("'", "''") + "'" +
                                " and ecotemplate_id = '" + sEcoTemplateID + "'";

                        string sValueExists = "";
                        if (!dc.sqlGetSingleString(ref sValueExists, sSQL, ref sErr))
                            throw new Exception("Unable to check for existing names [" + sEcoTemplateID + "]." + sErr);

                        if (!string.IsNullOrEmpty(sValueExists))
                            return sValue + " exists, please choose another value.";
                    }

                    string sSetClause = sColumn + "='" + sValue.Replace("'", "''") + "'";

                    //some columns on this table allow nulls... in their case an empty sValue is a null
                    if (sColumn == "action_desc" || sColumn == "category")
                    {
                        if (sValue.Replace(" ", "").Length == 0)
                            sSetClause = sColumn + " = null";
                        else
                            sSetClause = sColumn + "='" + sValue.Replace("'", "''") + "'";
                    }

                    sSQL = "update ecotemplate_action set " + sSetClause + " where action_id = '" + sActionID + "'";


                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to update Eco Template Action [" + sActionID + "]." + sErr);

                    ui.WriteObjectChangeLog(Globals.acObjectTypes.EcoTemplate, sEcoTemplateID, sActionID, "Action updated: [" + sSetClause + "]");
                }
                else
                {
                    throw new Exception("Unable to update Eco Template Action. Missing or invalid EcoTemplate/Action ID.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return "";
        }

        //This adds a new Action
        [WebMethod(EnableSession = true)]
        public void wmAddEcotemplateAction(string sEcoTemplateID, string sActionName, string sOTID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            string sErr = "";

            if (string.IsNullOrEmpty(sEcoTemplateID) || string.IsNullOrEmpty(sActionName) || string.IsNullOrEmpty(sOTID))
                throw new Exception("Missing or invalid EcoTemplate ID, Action Name or Task.");

            string sSQL = "insert into ecotemplate_action " +
                 " (action_id, action_name, ecotemplate_id, original_task_id)" +
                 " values (" +
                 " '" + Guid.NewGuid().ToString() + "'," +
                 " '" + sActionName + "'," +
                 " '" + sEcoTemplateID + "'," +
                 " '" + sOTID + "'" +
                 ")";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                //don't throw an error if its just a PK collision.  That just means it's already there.
                if (sErr.IndexOf("Duplicate entry") == 0)
                    throw new Exception(sErr);
            }

            ui.WriteObjectChangeLog(acObjectTypes.EcoTemplate, sEcoTemplateID, "", "Action Added : [" + sActionName + "]");

            return;
        }

        //delete an Action
        [WebMethod(EnableSession = true)]
        public string wmDeleteEcotemplateAction(string sActionID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = "";

            if (sActionID.Length == 0)
                return "";

            try
            {
                sSQL = "delete from ecotemplate_action" +
                    " where action_id ='" + sActionID + "'";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // if we made it here, so save the logs
            ui.WriteObjectDeleteLog(acObjectTypes.EcoTemplate, "", "", "Action [" + sActionID + "] removed from EcoTemplate.");

            return sErr;
        }

        //get the list of schedules for an entire ecosystem
        [WebMethod(EnableSession = true)]
        public string wmGetEcosystemSchedules(string sEcosystemID)
        {
            try
            {

                dataAccess dc = new dataAccess();
                string sSQL = null;
                string sErr = null;

                string sHTML = "";

                sSQL = "select s.schedule_id, s.label, s.descr, t.task_name, a.action_name" +
                    " from action_schedule s" +
                    " join task t on s.task_id = t.task_id" +
                    " left outer join ecotemplate_action a on s.action_id = a.action_id" +
                    " where s.ecosystem_id = '" + sEcosystemID + "'";
                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }
                else
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        sHTML += " <div class=\"ui-widget-content ui-corner-all pointer clearfloat action_schedule\"" +
                            " id=\"as_" + dr["schedule_id"].ToString() + "\"" +
                        ">";
                        sHTML += " <div class=\"floatleft schedule_name\">";

                        sHTML += "<span class=\"floatleft ui-icon ui-icon-calculator schedule_tip\" title=\"" + dr["descr"].ToString() + "\"></span>";

                        //show the action name, or the task name if no action exists.
                        if (string.IsNullOrEmpty(dr["action_name"].ToString()))
                            sHTML += "(" + dr["task_name"].ToString() + ")";
                        else
                            sHTML += dr["action_name"].ToString();

                        //and the schedule label
                        sHTML += " - " + (string.IsNullOrEmpty(dr["label"].ToString()) ? dr["schedule_id"].ToString() : dr["label"].ToString());

                        sHTML += " </div>";

                        sHTML += " <div class=\"floatright\">";
                        //sHTML += "<span class=\"ui-icon ui-icon-trash schedule_remove_btn\" title=\"Delete Schedule\"></span>";
                        sHTML += " </div>";


                        sHTML += " </div>";
                    }
                }

                return sHTML;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        //get the list of pending plans for an entire ecosystem
        [WebMethod(EnableSession = true)]
        public string wmGetEcosystemPlans(string sEcosystemID)
        {
            try
            {

                dataAccess dc = new dataAccess();
                string sSQL = null;
                string sErr = null;

                string sHTML = "";

                sSQL = "select ap.plan_id, date_format(ap.run_on_dt, '%m/%d/%Y %H:%i') as run_on_dt, ap.source, ap.action_id, t.task_id," +
                    " ea.action_name, t.task_name, ap.source, ap.schedule_id" +
                    " from action_plan ap" +
                    " join task t on ap.task_id = t.task_id" +
                    " left outer join ecotemplate_action ea on ap.action_id = ea.action_id" +
                    " where ap.ecosystem_id = '" + sEcosystemID + "'" +
                    " order by ap.run_on_dt, ea.action_name, t.task_name";
                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }
                else
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        sHTML += " <div class=\"ui-widget-content ui-corner-all pointer clearfloat action_plan\"" +
                            " id=\"ap_" + dr["plan_id"].ToString() + "\"" +
                            " plan_id=\"" + dr["plan_id"].ToString() + "\"" +
                            " run_on=\"" + dr["run_on_dt"].ToString() + "\"" +
                            " source=\"" + dr["source"].ToString() + "\"" +
                            " schedule_id=\"" + dr["schedule_id"].ToString() + "\"" +
                        ">";
                        sHTML += " <div class=\"floatleft action_plan_name\">";

                        //an icon denotes if it's manual or scheduled
                        if (dr["source"].ToString() == "schedule")
                            sHTML += "<span class=\"floatleft ui-icon ui-icon-calculator\" title=\"Scheduled\"></span>";
                        else
                            sHTML += "<span class=\"floatleft ui-icon ui-icon-document\" title=\"Run Later\"></span>";

                        //show the time
                        sHTML += dr["run_on_dt"].ToString();

                        //show the action name, or the task name if no action exists.
                        if (string.IsNullOrEmpty(dr["action_name"].ToString()))
                            sHTML += " - (" + dr["task_name"].ToString() + ")";
                        else
                            sHTML += " - " + dr["action_name"].ToString();


                        sHTML += " </div>";

                        sHTML += " <div class=\"floatright\">";
                        //sHTML += "<span class=\"ui-icon ui-icon-trash action_plan_remove_btn\" title=\"Delete Plan\"></span>";
                        sHTML += " </div>";


                        sHTML += " </div>";
                    }
                }

                return sHTML;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }



        /*
        FOR UPDATING action parameters, again we spin the action XML, comparing to TASK PARAMETER xml, and only save DIFFERENCES!
        */
        [WebMethod(EnableSession = true)]
        public void wmSaveActionParameterXML(string sActionID, string sActionDefaultsXML)
        {
            dataAccess dc = new dataAccess();
            
            acUI.acUI ui = new acUI.acUI();
            taskMethods tm = new taskMethods();

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (ui.IsGUID(sActionID) && ui.IsGUID(sUserID))
                {
                    string sErr = "";
                    string sSQL = "";

                    //we encoded this in javascript before the ajax call.
                    //the safest way to unencode it is to use the same javascript lib.
                    //(sometimes the javascript and .net libs don't translate exactly, google it.)
                    sActionDefaultsXML = ui.unpackJSON(sActionDefaultsXML);


                    //so, like when we read it, we gotta spin and compare, and build an XML that only represents *changes*
                    //to the defaults on the task.

                    //what is the task associated with this action?
                    sSQL = "select t.task_id" +
                        " from ecotemplate_action ea" +
                        " join task t on ea.original_task_id = t.original_task_id" +
                        " and t.default_version = 1" +
                        " where ea.action_id = '" + sActionID + "'";

                    string sTaskID = "";
                    if (!dc.sqlGetSingleString(ref sTaskID, sSQL, ref sErr))
                        throw new Exception(sErr);

                    if (!ui.IsGUID(sTaskID))
                        throw new Exception("Unable to find Task ID for Action.");


                    string sOverrideXML = "";
                    XDocument xTPDoc = new XDocument();
                    XDocument xADDoc = new XDocument();

                    //get the parameter XML from the TASK
                    string sTaskParamXML = tm.wmGetParameterXML("task", sTaskID, "");
                    if (!string.IsNullOrEmpty(sTaskParamXML))
                    {
                        xTPDoc = XDocument.Parse(sTaskParamXML);
                        if (xTPDoc == null)
                            throw new Exception("Task Parameter XML data is invalid.");

                        XElement xTPParams = xTPDoc.XPathSelectElement("/parameters");
                        if (xTPParams == null)
                            throw new Exception("Task Parameter XML data does not contain 'parameters' root node.");
                    }

                    //we had the ACTION defaults handed to us
                    if (!string.IsNullOrEmpty(sActionDefaultsXML))
                    {
                        xADDoc = XDocument.Parse(sActionDefaultsXML);
                        if (xADDoc == null)
                            throw new Exception("Action Defaults XML data is invalid.");

                        XElement xADParams = xADDoc.XPathSelectElement("/parameters");
                        if (xADParams == null)
                            throw new Exception("Action Defaults XML data does not contain 'parameters' root node.");
                    }

                    //spin the nodes in the ACTION xml, then dig in to the task XML and UPDATE the value if found.
                    //(if the node no longer exists, delete the node from the action XML)
                    //and action "values" take precedence over task values.
                    foreach (XElement xDefault in xADDoc.XPathSelectElements("//parameter"))
                    {
                        //look it up in the task param xml
                        XElement xADName = xDefault.XPathSelectElement("name");
                        string sADName = (xADName == null ? "" : xADName.Value);
                        XElement xADValues = xDefault.XPathSelectElement("values");
                        //string sValues = (xValues == null ? "" : xValues.ToString());


                        //now we have the name of the parameter, go find it in the TASK param XML
                        XElement xTaskParam = xTPDoc.XPathSelectElement("//parameter/name[. = '" + sADName + "']/..");  //NOTE! the /.. gets the parent of the name node!

                        //if it doesn't exist in the task params, remove it from this document
                        if (xTaskParam == null)
                        {
                            xDefault.Remove();
                            continue;
                        }


                        //and the "values" collection will be the 'next' node
                        XElement xTaskParamValues = xTaskParam.XPathSelectElement("values");

                        //if the values node matches, remove it
                        if (xTaskParamValues.Value.Equals(xADValues.Value))
                            xDefault.Remove();
                    }

                    //done
                    sOverrideXML = xADDoc.ToString();

                    //FINALLY, we have an XML that represents only the differences we wanna save.
                    sSQL = "update ecotemplate_action set" +
                        " parameter_defaults = '" + sOverrideXML + "'" +
                        " where action_id = '" + sActionID + "'";


                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to update Eco Template Action [" + sActionID + "]." + sErr);

                    ui.WriteObjectChangeLog(Globals.acObjectTypes.EcoTemplate, sActionID, sActionID, "Action default parameters updated: [" + sOverrideXML + "]");
                }
                else
                {
                    throw new Exception("Unable to update Eco Template Action. Missing or invalid Action ID.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return;
        }
        #endregion

        #region "Cloud"
        [WebMethod(EnableSession = true)]
        public string wmSetActiveCloudAccount(string sAccountID)
        {
            acUI.acUI ui = new acUI.acUI();
            string sErr = null;

            try
            {
                if (!ui.SetActiveCloudAccount(sAccountID, ref sErr))
                {
                    return sErr;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return "";
        }

        #endregion

        #region "Task Launch Dialog"
        [WebMethod(EnableSession = true)]
        public string wmGetActionPlans(string sTaskID, string sActionID, string sEcosystemID)
        {
            try
            {

                dataAccess dc = new dataAccess();
                string sSQL = null;
                string sErr = null;

                string sHTML = "";

                sSQL = "select plan_id, date_format(ap.run_on_dt, '%m/%d/%Y %H:%i') as run_on_dt, ap.source, ap.action_id, ap.ecosystem_id," +
                    " ea.action_name, e.ecosystem_name, ap.source, ap.schedule_id" +
                    " from action_plan ap" +
                    " left outer join ecotemplate_action ea on ap.action_id = ea.action_id" +
                    " left outer join ecosystem e on ap.ecosystem_id = e.ecosystem_id" +
                    " where ap.task_id = '" + sTaskID + "'" +
                    (!string.IsNullOrEmpty(sActionID) ? " and ap.action_id = '" + sActionID + "'" : "") +
                    (!string.IsNullOrEmpty(sEcosystemID) ? " and ap.ecosystem_id = '" + sEcosystemID + "'" : "") +
                    " order by ap.run_on_dt";
                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }
                else
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        sHTML += " <div class=\"ui-widget-content ui-corner-all pointer clearfloat action_plan\"" +
                            " id=\"ap_" + dr["plan_id"].ToString() + "\"" +
                            " plan_id=\"" + dr["plan_id"].ToString() + "\"" +
                            " eco_id=\"" + dr["ecosystem_id"].ToString() + "\"" +
                            " run_on=\"" + dr["run_on_dt"].ToString() + "\"" +
                            " source=\"" + dr["source"].ToString() + "\"" +
                            " schedule_id=\"" + dr["schedule_id"].ToString() + "\"" +
                        ">";
                        sHTML += " <div class=\"floatleft action_plan_name\">";

                        //an icon denotes if it's manual or scheduled
                        if (dr["source"].ToString() == "schedule")
                            sHTML += "<span class=\"floatleft ui-icon ui-icon-calculator\" title=\"Scheduled\"></span>";
                        else
                            sHTML += "<span class=\"floatleft ui-icon ui-icon-document\" title=\"Run Later\"></span>";

                        sHTML += dr["run_on_dt"].ToString();

                        //show the action and ecosystem if it's in the results but NOT passed in
                        //that means we are looking at this from a TASK
                        if (string.IsNullOrEmpty(sActionID))
                        {
                            sHTML += " " + dr["ecosystem_name"].ToString();

                            if (!string.IsNullOrEmpty(dr["action_name"].ToString()))
                                sHTML += " (" + dr["action_name"].ToString() + ")";
                        }
                        sHTML += " </div>";

                        sHTML += " <div class=\"floatright\">";
                        sHTML += "<span class=\"ui-icon ui-icon-trash action_plan_remove_btn\" title=\"Delete Plan\"></span>";
                        sHTML += " </div>";


                        sHTML += " </div>";
                    }
                }

                return sHTML;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [WebMethod(EnableSession = true)]
        public string wmGetActionSchedules(string sTaskID, string sActionID, string sEcosystemID)
        {
            try
            {

                dataAccess dc = new dataAccess();
                string sSQL = null;
                string sErr = null;

                string sHTML = "";

                sSQL = "select s.schedule_id, s.label, s.descr, e.ecosystem_name, a.action_name" +
                    " from action_schedule s" +
                    " left outer join ecotemplate_action a on s.action_id = a.action_id" +
                    " left outer join ecosystem e on s.ecosystem_id = e.ecosystem_id" +
                    " where s.task_id = '" + sTaskID + "'";
                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }
                else
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        string sToolTip = "";
                        //show the action and ecosystem if it's in the results but NOT passed in
                        //that means we are looking at this from a TASK
                        if (string.IsNullOrEmpty(sActionID))
                        {
                            sToolTip += "Ecosystem: " + dr["ecosystem_name"].ToString() + "<br />";

                            if (!string.IsNullOrEmpty(dr["action_name"].ToString()))
                                sToolTip += "Action: " + dr["action_name"].ToString() + "<br />";
                        }
                        sToolTip += dr["descr"].ToString();

                        //draw it
                        sHTML += " <div class=\"ui-widget-content ui-corner-all pointer clearfloat action_schedule\"" +
                            " id=\"as_" + dr["schedule_id"].ToString() + "\"" +
                        ">";
                        sHTML += " <div class=\"floatleft schedule_name\">";

                        sHTML += "<span class=\"floatleft ui-icon ui-icon-calculator schedule_tip\" title=\"" + sToolTip + "\"></span>";

                        sHTML += (string.IsNullOrEmpty(dr["label"].ToString()) ? dr["schedule_id"].ToString() : dr["label"].ToString());

                        sHTML += " </div>";

                        sHTML += " <div class=\"floatright\">";
                        sHTML += "<span class=\"ui-icon ui-icon-trash schedule_remove_btn\" title=\"Delete Schedule\"></span>";
                        sHTML += " </div>";


                        sHTML += " </div>";
                    }
                }

                return sHTML;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [WebMethod(EnableSession = true)]
        public string wmDeleteActionPlan(int iPlanID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = "";

            if (iPlanID < 1)
                throw new Exception("Missing Action Plan ID.");

            try
            {
                sSQL = "delete from action_plan where plan_id = " + iPlanID.ToString();

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // if we made it here, so save the logs
            ui.WriteObjectDeleteLog(acObjectTypes.EcoTemplate, "", "", "Action Plan [" + iPlanID.ToString() + "] deleted.");

            return sErr;
        }
        [WebMethod(EnableSession = true)]
        public string wmDeleteSchedule(string sScheduleID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sSQL = null;
            string sErr = "";

            if (string.IsNullOrEmpty(sScheduleID))
                throw new Exception("Missing Schedule ID.");

            try
            {
                sSQL = "delete from action_plan where schedule_id = '" + sScheduleID + "'";
                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);

                sSQL = "delete from action_schedule where schedule_id = '" + sScheduleID + "'";
                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // if we made it here, so save the logs
            ui.WriteObjectDeleteLog(acObjectTypes.EcoTemplate, "", "", "Schedule [" + sScheduleID + "] deleted.");

            return sErr;
        }
        [WebMethod(EnableSession = true)]
        public void wmRunRepeatedly(string sTaskID, string sActionID, string sEcosystemID,
            string sMonths, string sDays, string sHours, string sMinutes, string sDaysOrWeeks,
            string sParameterXML, int iDebugLevel)
        {
            acUI.acUI ui = new acUI.acUI();

            try
            {
                if (sTaskID.Length == 0 || sMonths.Length == 0 || sDays.Length == 0 || sHours.Length == 0 || sMinutes.Length == 0 || sDaysOrWeeks.Length == 0)
                    throw new Exception("Missing or invalid Schedule timing or Task ID.");

                //we encoded this in javascript before the ajax call.
                //the safest way to unencode it is to use the same javascript lib.
                //(sometimes the javascript and .net libs don't translate exactly, google it.)
                sParameterXML = ui.unpackJSON(sParameterXML).Replace("'", "''");

                dataAccess dc = new dataAccess();
                string sSQL = null;
                string sErr = null;

                //figure out a label and a description
                string sDesc = "";
                string sLabel = wmGenerateScheduleLabel(sMonths, sDays, sHours, sMinutes, sDaysOrWeeks, ref sDesc);

                sSQL = "insert into action_schedule (schedule_id, task_id, action_id, ecosystem_id, months, days, hours, minutes, days_or_weeks, label, descr, parameter_xml, debug_level)" +
                   " values (uuid(), " +
                   " '" + sTaskID + "'," +
                   (!string.IsNullOrEmpty(sActionID) ? " '" + sActionID + "'" : "''") + "," +
                   (!string.IsNullOrEmpty(sEcosystemID) ? " '" + sEcosystemID + "'" : "''") + "," +
                   " '" + sMonths + "'," +
                   " '" + sDays + "'," +
                   " '" + sHours + "'," +
                   " '" + sMinutes + "'," +
                   " '" + sDaysOrWeeks + "'," +
                   (!string.IsNullOrEmpty(sLabel) ? " '" + sLabel + "'" : "null") + "," +
                   (!string.IsNullOrEmpty(sDesc) ? " '" + sDesc + "'" : "null") + "," +
                   (!string.IsNullOrEmpty(sParameterXML) ? " '" + sParameterXML + "'" : "null") + "," +
                   (iDebugLevel > -1 ? iDebugLevel.ToString() : "null") +
                   ")";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr)) { throw new Exception(sErr); }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [WebMethod(EnableSession = true)]
        public void wmRunLater(string sTaskID, string sActionID, string sEcosystemID, string sRunOn, string sParameterXML, int iDebugLevel)
        {
            acUI.acUI ui = new acUI.acUI();
            
            try
            {
                if (sTaskID.Length == 0 || sRunOn.Length == 0)
                    throw new Exception("Missing Action Plan date or Task ID.");

                //we encoded this in javascript before the ajax call.
                //the safest way to unencode it is to use the same javascript lib.
                //(sometimes the javascript and .net libs don't translate exactly, google it.)
                sParameterXML = ui.unpackJSON(sParameterXML).Replace("'", "''");

                dataAccess dc = new dataAccess();
                string sSQL = null;
                string sErr = null;

                sSQL = "insert into action_plan (task_id, action_id, ecosystem_id, run_on_dt, parameter_xml, debug_level, source)" +
                   " values (" +
                   " '" + sTaskID + "'," +
                   (!string.IsNullOrEmpty(sActionID) ? " '" + sActionID + "'" : "''") + "," +
                   (!string.IsNullOrEmpty(sEcosystemID) ? " '" + sEcosystemID + "'" : "''") + "," +
                   " str_to_date('" + sRunOn + "', '%m/%d/%Y %H:%i')," +
                   (!string.IsNullOrEmpty(sParameterXML) ? " '" + sParameterXML + "'" : "null") + "," +
                   (iDebugLevel > -1 ? iDebugLevel.ToString() : "null") + "," +
                   " 'manual'" +
                   ")";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr)) { throw new Exception(sErr); }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [WebMethod(EnableSession = true)]
        public void wmSavePlan(int iPlanID, string sParameterXML, int iDebugLevel)
        {
            acUI.acUI ui = new acUI.acUI();

            try
            {
                if (iPlanID < 1)
                    throw new Exception("Missing Action Plan ID.");

                //we encoded this in javascript before the ajax call.
                //the safest way to unencode it is to use the same javascript lib.
                //(sometimes the javascript and .net libs don't translate exactly, google it.)
                sParameterXML = ui.unpackJSON(sParameterXML).Replace("'", "''");

                dataAccess dc = new dataAccess();
                string sSQL = null;
                string sErr = null;

                sSQL = "update action_plan" +
                    " set parameter_xml = " + (!string.IsNullOrEmpty(sParameterXML) ? "'" + sParameterXML + "'" : "null") + "," +
                    " debug_level = " + (iDebugLevel > -1 ? iDebugLevel.ToString() : "null") +
                    " where plan_id = " + iPlanID.ToString();

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr)) { throw new Exception(sErr); }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [WebMethod(EnableSession = true)]
        public void wmSaveSchedule(string sScheduleID,
            string sMonths, string sDays, string sHours, string sMinutes, string sDaysOrWeeks,
            string sParameterXML, int iDebugLevel)
        {
            acUI.acUI ui = new acUI.acUI();

            try
            {
                if (sScheduleID.Length == 0 || sMonths.Length == 0 || sDays.Length == 0 || sHours.Length == 0 || sMinutes.Length == 0 || sDaysOrWeeks.Length == 0)
                    throw new Exception("Missing Schedule ID or invalid timetable.");

                //we encoded this in javascript before the ajax call.
                //the safest way to unencode it is to use the same javascript lib.
                //(sometimes the javascript and .net libs don't translate exactly, google it.)
                sParameterXML = ui.unpackJSON(sParameterXML).Replace("'", "''");

                dataAccess dc = new dataAccess();
                string sSQL = null;
                string sErr = null;

                //whack all plans for this schedule, it's been changed
                sSQL = "delete from action_plan where schedule_id = '" + sScheduleID + "'";
                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception(sErr);


                //figure out a label
                string sDesc = "";
                string sLabel = wmGenerateScheduleLabel(sMonths, sDays, sHours, sMinutes, sDaysOrWeeks, ref sDesc);

                sSQL = "update action_schedule set" +
                    " months = '" + sMonths + "'," +
                    " days = '" + sDays + "'," +
                    " hours = '" + sHours + "'," +
                    " minutes = '" + sMinutes + "'," +
                    " days_or_weeks = '" + sDaysOrWeeks + "'," +
                    " label = " + (!string.IsNullOrEmpty(sLabel) ? "'" + sLabel + "'" : "null") + "," +
                    " descr = " + (!string.IsNullOrEmpty(sDesc) ? "'" + sDesc + "'" : "null") + "," +
                    " parameter_xml = " + (!string.IsNullOrEmpty(sParameterXML) ? "'" + sParameterXML + "'" : "null") + "," +
                    " debug_level = " + (iDebugLevel > -1 ? iDebugLevel.ToString() : "null") +
                    " where schedule_id = '" + sScheduleID + "'";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr)) { throw new Exception(sErr); }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmGetRecurringPlanByPlanID(string sPlanID)
        {
            dataAccess dc = new dataAccess();
            string sSQL = null;
            string sErr = null;
            string sOut = "";

            //first, get the schedule that created this plan
            sSQL = "select schedule_id from action_plan where plan_id = '" + sPlanID + "'";
            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                throw new Exception(sErr);

            if (dr != null)
            {
                sOut = wmGetRecurringPlan(dr["schedule_id"].ToString());
            }

            return sOut;
        }

        //this one will get it for an explicit set of ids.
        [WebMethod(EnableSession = true)]
        public string wmGetRecurringPlan(string sScheduleID)
        {
            dataAccess dc = new dataAccess();
            string sSQL = null;
            string sErr = null;

            StringBuilder sb = new StringBuilder();

            //now we know the details, go get the timetable for that specific schedule
            sSQL = "select schedule_id, months, days, hours, minutes, days_or_weeks, label" +
                " from action_schedule" +
                " where schedule_id = '" + sScheduleID + "'";
            DataRow drSchedule = null;
            if (!dc.sqlGetDataRow(ref drSchedule, sSQL, ref sErr))
                throw new Exception(sErr);

            //GetDataRow returns a message if there are no rows...
            if (string.IsNullOrEmpty(sErr))
            {
                string sMo = drSchedule["months"].ToString();
                string sDa = drSchedule["days"].ToString();
                string sHo = drSchedule["hours"].ToString();
                string sMi = drSchedule["minutes"].ToString();
                string sDW = drSchedule["days_or_weeks"].ToString();
                string sDesc = (string.IsNullOrEmpty(drSchedule["label"].ToString()) ? drSchedule["schedule_id"].ToString() : drSchedule["label"].ToString());

                sb.Append("{");
                sb.AppendFormat("\"{0}\" : \"{1}\",", "sDescription", sDesc);
                sb.AppendFormat("\"{0}\" : \"{1}\",", "sMonths", sMo);
                sb.AppendFormat("\"{0}\" : \"{1}\",", "sDays", sDa);
                sb.AppendFormat("\"{0}\" : \"{1}\",", "sHours", sHo);
                sb.AppendFormat("\"{0}\" : \"{1}\",", "sMinutes", sMi);
                sb.AppendFormat("\"{0}\" : \"{1}\",", "sDaysOrWeeks", sDW);
                sb.Append("}");
            }
            else
            {
                throw new Exception("Unable to find details for Recurring Action Plan. " + sErr);
            }

            return sb.ToString();
        }

        //figure out a label given a set of time data
        [WebMethod(EnableSession = true)]
        public string wmGenerateScheduleLabel(string sMo, string sDa, string sHo, string sMi, string sDW, ref string sTooltip)
        {
            string sDesc = "";
            sTooltip = "";

            //we can analyze the details and come up with a pretty name for this schedule.
            //this may need to be it's own web method eventually...
            if (sMo != "0,1,2,3,4,5,6,7,8,9,10,11,")
                sDesc += "Some Months, ";

            if (sDW == "0")
            { //explicit days 
                if (sDa == "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,")
                    sDesc += "Every Day, ";
            }
            else
            { //weekdays
                if (sDa == "0,1,2,3,4,5,6,")
                    sDesc += "Every Weekday, ";
            }

            //hours and minutes  labels play together, and are sometimes exclusive of one another
            //we'll figure that out later...

            if (sHo == "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,")
                sDesc += "Hourly, ";
            else
                sDesc += "Selected Hours, ";

            if (sMi == "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,")
                sDesc += "Every Minute";
            else
                sDesc += "Selected Minutes";

            //just use the guid if we couldn't derive a label.
            if (sDesc != "")
                sDesc += ".";




            //build a verbose description too
            string sTmp = "";
            string[] a = { };

            //months
            if (sMo == "0,1,2,3,4,5,6,7,8,9,10,11,")
                sTmp = "Every Month";
            else
            {
                a = sMo.Split(',');
                foreach (string s in a)
                {
                    sTmp += s + ",";
                }
                sTmp = sTmp.Replace("0", "Jan").Replace("1", "Feb").Replace("2", "Mar").Replace("3", "Apr").Replace("4", "May").Replace("5", "Jun").Replace("6", "Jul").Replace("7", "Aug").Replace("8", "Sep").Replace("9", "Oct").Replace("10", "Nov").Replace("11", "Dec");
            }
            sTooltip += "Months: (" + sTmp.TrimEnd(',') + ")<br />" + Environment.NewLine;

            //days
            sTmp = "";
            a = sDa.Split(',');
            foreach (string s in a)
            {
                //days are +1
                if (!string.IsNullOrEmpty(s)) sTmp += (Convert.ToInt32(s) + 1).ToString() + ",";
            }
            if (sDW == "0")
            {
                if (sDa == "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,")
                    sTmp = "Every Day";

                sTooltip += "Days: (" + sTmp.TrimEnd(',') + ")<br />" + Environment.NewLine;
            }
            else
            {
                if (sDa == "0,1,2,3,4,5,6,")
                    sTmp = "Every Weekday";
                else
                    sTmp = sTmp.Replace("0", "Sun").Replace("1", "Mon").Replace("2", "Tue").Replace("3", "Wed").Replace("4", "Thu").Replace("5", "Fri").Replace("6", "Sat");

                sTooltip += "Weekdays: (" + sTmp.TrimEnd(',') + ")<br />" + Environment.NewLine;
            }

            //hours
            sTmp = "";
            if (sHo == "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,")
                sTmp = "Every Hour";
            else
            {
                a = sHo.Split(',');
                foreach (string s in a)
                {
                    sTmp += s + ",";
                }
            }
            sTooltip += "Hours: (" + sTmp.TrimEnd(',') + ")<br />" + Environment.NewLine;

            //minutes
            sTmp = "";
            if (sMi == "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,")
                sTmp = "Every Minute";
            else
            {
                a = sMi.Split(',');
                foreach (string s in a)
                {
                    sTmp += s + ",";
                }
            }
            sTooltip += "Minutes: (" + sTmp.TrimEnd(',') + ")<br />" + Environment.NewLine;

            return sDesc;
        }
        #endregion

    }

}
