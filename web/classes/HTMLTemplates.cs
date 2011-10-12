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
using System.Text;
using System.Data;
using System.Web;
using System.Web.Services;
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Text.RegularExpressions;
using Globals;

namespace FunctionTemplates
{
    public partial class HTMLTemplates : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        bool bShowNotes = true;

        public DataRow GetSingleStep(string sStepID, string sUserID, ref string sErr)
        {
            //this is called from many different places...
            string sSQL = "select t.task_name, t.version," +
                " s.step_id, s.step_order, s.codeblock_name, s.step_desc, s.function_name, s.function_xml, s.commented, s.locked," +
                " s.output_parse_type, s.output_row_delimiter, s.output_column_delimiter, s.variable_xml," +
                " f.function_label, f.category_name, c.category_label, f.description, f.help, f.icon," +
                " us.visible, us.breakpoint, us.skip, us.button" +
                " from task_step s" +
                " join task t on s.task_id = t.task_id" +
                " join lu_task_step_function f on s.function_name = f.function_name" +
                " join lu_task_step_function_category c on f.category_name = c.category_name" +
                " left outer join task_step_user_settings us on us.user_id = '" + sUserID + "' and s.step_id = us.step_id" +
                " where s.step_id = '" + sStepID + "' limit 1";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                sErr = "Unable to get data row for step_id [" + sStepID + "].<br />" + sErr;
                return null;
            }

            if (dt.Rows.Count == 1)
            {
                return dt.Rows[0];
            }
            else
            {
                sErr = "ERROR: No data found.<br />This command should be deleted and recreated.<br /><br />ID [" + sStepID + "].<br />" + sErr;
                return null;
            }

        }
        public string DrawFullStep(DataRow dr)
        {
            string sExpandedClass = "";
            if (!string.IsNullOrEmpty(dr["visible"].ToString()))
                sExpandedClass = (dr["visible"].ToString() == "0" ? "step_collapsed" : "");

            string sSkipStepClass = "";
            string sSkipHeaderClass = "";
            string sSkipIcon = "";
            if (!string.IsNullOrEmpty(dr["commented"].ToString()))
            {
                sSkipStepClass = (dr["commented"].ToString() == "0" ? "" : "step_skip");
                sSkipHeaderClass = (dr["commented"].ToString() == "0" ? "" : "step_header_skip");
                sSkipIcon = (dr["commented"].ToString() == "0"
                    ? "../images/icons/cnr.png"
                    : "../images/icons/cnrgrey.png");
                //pay attention
                //this overrides the 'expanded class', making the step collapsed by default if it's commented out.
                //the 'skip' overrides the saved visibility preference.
                if (dr["commented"].ToString() == "1")
                    sExpandedClass = "step_collapsed";
            }

            string sMainHTML = "";
            string sVariableHTML = "";
            string sOptionHTML = "";

            //what's the 'label' for the step strip?
            //(hate this... wish the label was consistent across all step types)
            //hack for initial loading of the step... don't show the order if it's a "-1"... it's making a
            //strange glitch in the browser...you can see it update
            string sIcon = (string.IsNullOrEmpty(dr["icon"].ToString()) ? "" : "../images/" + dr["icon"].ToString());
            string sStepOrder = (dr["step_order"].ToString() == "-1" ? "" : dr["step_order"].ToString());
            string sLabel = "<img class=\"step_header_function_icon\" src=\"" + sIcon + "\" alt=\"\" />" +
                "<span class=\"step_order_label\">" + sStepOrder + "</span> : " +
                dr["category_label"].ToString() + " - " + dr["function_label"].ToString();

            //show a useful snip in the title bar.
            //notes trump values, and not all commands show a value snip
            //but certain ones do.
            string sSnip = "";
            if (!string.IsNullOrEmpty(dr["step_desc"].ToString()))
                sSnip = ui.GetSnip(dr["step_desc"].ToString(), 75);
            else
                sSnip = ui.GetSnip(GetValueSnip(ref dr), 75);
            sLabel += (sSnip == "" ? "" : "<span style=\"padding-left:15px;font-style:italic;font-weight:normal;\">[" + sSnip + "]</span>");

            string sLockClause = (dr["locked"].ToString() != "0" ? " onclick=\"return false;\"" : "");

            string sCommentFieldID = "";

            sMainHTML += "<li class=\"step " + sSkipStepClass + "\"" +
                " id=\"" + dr["step_id"].ToString() + "\" " + sLockClause + ">";
            sMainHTML += "<input type=\"text\"" +
                " value=\"" + dr["commented"].ToString() + "\"" +
                CommonAttribs(dr["step_id"].ToString(), "_common", false, "commented", ref sCommentFieldID, "hidden") +
                " />";

            // step expand image
            string sExpandImage = "expand_down.png";
            if (sExpandedClass == "step_collapsed") { sExpandImage = "expand_up.png"; };

            sMainHTML += "    <div class=\"ui-state-default step_header " + sSkipHeaderClass + "\"" +
                         " id=\"step_header_" + dr["step_id".ToString()] + "\">";
            sMainHTML += "        <div class=\"step_header_title\">";
            sMainHTML += "            <span class=\"step_toggle_btn\" step_id=\"" + dr["step_id"].ToString() + "\">" +
                         " <img class=\"expand_image\" src=\"../images/icons/" + sExpandImage + "\" alt=\"\" title=\"Hide/Show Step\" /></span>";
            sMainHTML += "            <span>" + sLabel + "</span>";
            sMainHTML += "        </div>";
            sMainHTML += "        <div class=\"step_header_icons\">";

            //this button will copy a step into the clipboard.
            sMainHTML += "            <span><img id=\"step_copy_btn_" + dr["step_id"].ToString() + "\"" +
                " class=\"step_copy_btn\" step_id=\"" + dr["step_id"].ToString() + "\"" +
                " src=\"../images/icons/editcopy_16.png\" alt=\"\" title=\"Copy this Step to your Clipboard\"/></span>";

            //this button is data enabled.  it controls the value of the hidden field at the top of the step.
            sMainHTML += "            <span><img id=\"step_skip_btn_" + dr["step_id"].ToString() + "\"" +
                " class=\"step_skip_btn\" step_id=\"" + dr["step_id"].ToString() + "\"" +
                " datafield_id=\"" + sCommentFieldID + "\"" +
                " src=\"" + sSkipIcon + "\" alt=\"\" title=\"Skip this Step\"/></span>";

            sMainHTML += "            <span class=\"step_delete_btn\" remove_id=\"" + dr["step_id"].ToString() + "\">" +
                " <img src=\"../images/icons/fileclose.png\" alt=\"\" title=\"Delete Step\" /></span>";
            sMainHTML += "        </div>";
            sMainHTML += "    </div>";
            sMainHTML += "    <div id=\"step_detail_" + dr["step_id"].ToString() + "\"" +
                " class=\"ui-widget-content ui-corner-bottom step_detail " + sExpandedClass + "\" >";

            sMainHTML += GetStepTemplate(dr, ref sOptionHTML, ref sVariableHTML);
            sMainHTML += DrawStepCommon(dr, sOptionHTML, sVariableHTML);

            sMainHTML += "    </div>";

            sMainHTML += "</li>";

            return sMainHTML;
        }
        public string DrawEmbeddedStep(DataRow dr)
        {
            string sExpandedClass = "";
            if (!string.IsNullOrEmpty(dr["visible"].ToString()))
                sExpandedClass = (dr["visible"].ToString() == "0" ? "step_collapsed" : "");

            string sMainHTML = "";
            string sVariableHTML = "";
            string sOptionHTML = "";

            //labels are different here than in Full Steps.
            string sIcon = (string.IsNullOrEmpty(dr["icon"].ToString()) ? "" : "../images/" + dr["icon"].ToString());
            string sLabel = "<img class=\"step_header_function_icon\" src=\"" + sIcon + "\" alt=\"\" /> " +
                dr["category_label"].ToString() + " - " + dr["function_label"].ToString();

            string sSnip = ui.GetSnip(dr["step_desc"].ToString(), 75);
            sLabel += (string.IsNullOrEmpty(dr["step_desc"].ToString()) ? "" : "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[" + sSnip + "]");

            string sLockClause = (dr["locked"].ToString() != "0" ? " onclick=\"return false;\"" : "");


            // step expand image
            string sExpandImage = "expand_down.png";
            if (sExpandedClass == "step_collapsed") { sExpandImage = "expand_up.png"; };

            sMainHTML += "<div class=\"embedded_step\" id=\"" + dr["step_id"].ToString() + "\" " + sLockClause + ">";
            sMainHTML += "    <div class=\"ui-state-default embedded_step_header\" id=\"embedded_step_header_" + dr["step_id".ToString()] + "\">";
            sMainHTML += "        <div class=\"step_header_title\">";
            sMainHTML += "            <span class=\"step_toggle_btn\"" +
                " step_id=\"" + dr["step_id"].ToString() + "\">" +
                " <img class=\"expand_image\" src=\"../images/icons/" + sExpandImage + "\" alt=\"\" title=\"Hide/Show Step\" /></span>";
            sMainHTML += "            <span>" + sLabel + "</span>";
            sMainHTML += "        </div>";
            sMainHTML += "        <div class=\"step_header_icons\">";

            //this button will copy a step into the clipboard.
            sMainHTML += "            <span><img id=\"step_copy_btn_" + dr["step_id"].ToString() + "\"" +
                " class=\"step_copy_btn\" step_id=\"" + dr["step_id"].ToString() + "\"" +
                " src=\"../images/icons/editcopy_16.png\" alt=\"\" title=\"Copy this Step to your Clipboard\"/></span>";

            //for deleting, the codeblock_name is the step_id of the parent step.
            sMainHTML += "            <span class=\"embedded_step_delete_btn\"" +
                " remove_id=\"" + dr["step_id"].ToString() + "\" parent_id=\"" + dr["codeblock_name"].ToString() + "\">" +
                " <img src=\"../images/icons/fileclose.png\" alt=\"\" title=\"Remove Command\" /></span>";
            sMainHTML += "        </div>";
            sMainHTML += "     </div>";
            sMainHTML += "     <div id=\"step_detail_" + dr["step_id"].ToString() + "\"" +
                " class=\"ui-widget-content ui-corner-bottom step_detail " + sExpandedClass + "\" >";

            sMainHTML += GetStepTemplate(dr, ref sOptionHTML, ref sVariableHTML);
            sMainHTML += DrawStepCommon(dr, sOptionHTML, sVariableHTML);

            sMainHTML += "    </div>";
            sMainHTML += "</div>";

            return sMainHTML;
        }
        public string DrawReadOnlyStep(DataRow dr, bool bDisplayNotes)
        {
            //set this global flag so embedded steps will know what to do
            bShowNotes = bDisplayNotes;

            string sMainHTML = "";
            string sOptionHTML = "";

            string sStepOrder = "";
            sStepOrder = (dr["step_order"].ToString() == "-1" ? "" : dr["step_order"].ToString() + ":");

            //labels are different here than in Full Steps.
            string sLabel = sStepOrder + dr["category_label"].ToString() + " - " + dr["function_label"].ToString();
            string sSkipHeaderClass = "";
            string sSkipClass = "";
            string sExpandedClass = "";
            string sSkipStepClass = "";
            if (!string.IsNullOrEmpty(dr["commented"].ToString()))
            {
                sSkipStepClass = (dr["commented"].ToString() == "0" ? "" : "step_skip");
                sSkipHeaderClass = (dr["commented"].ToString() == "0" ? "" : "step_header_skip");
                if (dr["commented"].ToString() == "1")
                {
                    sExpandedClass = "step_collapsed";
                    sSkipClass = "style=\"color: #ACACAC\"";
                }
            }

            sMainHTML += "<li class=\"step " + sSkipStepClass + "\" id=\"" + dr["step_id"].ToString() + "\">";
            sMainHTML += "<div class=\"view_step " + sSkipHeaderClass + "\" id=\"" + dr["step_id"].ToString() + "\">";
            sMainHTML += "    <div class=\"view_step_header " + sSkipHeaderClass + "\" id=\"view_step_header_" + dr["step_id".ToString()] + "\">";
            sMainHTML += "       <span " + sSkipClass + ">" + sLabel + "</span>";
            sMainHTML += "    </div>";
            sMainHTML += "    <div class=\"view_step_detail " + sExpandedClass + "\" id=\"step_detail_" + dr["step_id"].ToString() + "\">";

            sMainHTML += GetStepTemplate_View(dr, ref sOptionHTML);

            if (bShowNotes)
                if (!string.IsNullOrEmpty(dr["step_desc"].ToString()))
                    sMainHTML += DrawStepNotes_View(dr["step_desc"].ToString());

            if (sOptionHTML != "")
                sMainHTML += DrawStepOptions_View(sOptionHTML);

            //sMainHTML += "    </div>";
            sMainHTML += "</div>";
            sMainHTML += "</li>";

            return sMainHTML;
        }
        public string DrawEmbeddedReadOnlyStep(DataRow dr, bool bDisplayNotes)
        {
            //set this global flag so embedded steps will know what to do
            bShowNotes = bDisplayNotes;

            string sMainHTML = "";
            string sOptionHTML = "";

            string sStepOrder = "";
            sStepOrder = (dr["step_order"].ToString() == "-1" ? "" : dr["step_order"].ToString() + ":");

            //labels are different here than in Full Steps.
            string sLabel = sStepOrder + dr["category_label"].ToString() + " - " + dr["function_label"].ToString();
            string sSkipHeaderClass = "";
            string sSkipClass = "";
            string sExpandedClass = "";
            string sSkipStepClass = "";
            if (!string.IsNullOrEmpty(dr["commented"].ToString()))
            {
                sSkipStepClass = (dr["commented"].ToString() == "0" ? "" : "step_skip");
                sSkipHeaderClass = (dr["commented"].ToString() == "0" ? "" : "step_header_skip");
                if (dr["commented"].ToString() == "1")
                {
                    sExpandedClass = "step_collapsed";
                    sSkipClass = "style=\"color: #ACACAC\"";
                }
            }

            sMainHTML += "<div class=\"embedded_step " + sSkipStepClass + "\" id=\"" + dr["step_id"].ToString() + "\">";
            sMainHTML += "<div class=\"view_step " + sSkipHeaderClass + "\" id=\"" + dr["step_id"].ToString() + "\">";
            sMainHTML += "    <div class=\"view_step_header " + sSkipHeaderClass + "\" id=\"view_step_header_" + dr["step_id".ToString()] + "\">";
            sMainHTML += "       <span " + sSkipClass + ">" + sLabel + "</span>";
            sMainHTML += "    </div>";
            sMainHTML += "    <div class=\"view_step_detail " + sExpandedClass + "\" id=\"step_detail_" + dr["step_id"].ToString() + "\">";

            sMainHTML += GetStepTemplate_View(dr, ref sOptionHTML);

            if (bShowNotes)
                if (!string.IsNullOrEmpty(dr["step_desc"].ToString()))
                    sMainHTML += DrawStepNotes_View(dr["step_desc"].ToString());

            if (sOptionHTML != "")
                sMainHTML += DrawStepOptions_View(sOptionHTML);

            //sMainHTML += "    </div>";
            sMainHTML += "</div>";
            sMainHTML += "</div>";

            return sMainHTML;
        }

        #region "Edit Templates"
        public string DrawStepCommon(DataRow dr, string sOptionHTML, string sVariableHTML)
        {
            //this is the section that is common to all steps.
            string sHTML = "";

            sHTML += "        <hr />";
            sHTML += "        <div class=\"step_common\" >";
            sHTML += "            <div class=\"step_common_header\">";

            string sShowOnLoad = (string.IsNullOrEmpty(dr["button"].ToString()) ? "" : dr["button"].ToString());

            //pill buttons
            if (sVariableHTML != "")
                sHTML += "                <span class=\"step_common_button " +
                    (sShowOnLoad == "variables" ? "step_common_button_active" : "") + "\"" +
                    " id=\"btn_step_common_detail_" + dr["step_id"].ToString() + "_variables\"" +
                    " button=\"variables\"" +
                    " step_id=\"" + dr["step_id"].ToString() + "\">Variables</span>";

            if (sOptionHTML != "")
                sHTML += "                <span class=\"step_common_button " +
                    (sShowOnLoad == "options" ? "step_common_button_active" : "") + "\"" +
                    " id=\"btn_step_common_detail_" + dr["step_id"].ToString() + "_options\"" +
                    " button=\"options\"" +
                    " step_id=\"" + dr["step_id"].ToString() + "\">Options</span>";

            sHTML += "                <span class=\"step_common_button " +
                (sShowOnLoad == "notes" ? "step_common_button_active" : "") + "\"" +
                " id=\"btn_step_common_detail_" + dr["step_id"].ToString() + "_notes\"" +
                " button=\"notes\"" +
                " step_id=\"" + dr["step_id"].ToString() + "\">Notes</span>";

            sHTML += "                <span class=\"step_common_button " +
                (sShowOnLoad == "help" ? "step_common_button_active" : "") + "\"" +
                " id=\"btn_step_common_detail_" + dr["step_id"].ToString() + "_help\"" +
                " button=\"help\"" +
                " step_id=\"" + dr["step_id"].ToString() + "\">Help</span>";


            sHTML += "            </div>";



            //sections
            sHTML += "            <div id=\"step_common_detail_" + dr["step_id"].ToString() + "_notes\"" +
                " class=\"step_common_detail " + (sShowOnLoad == "notes" ? "" : "step_common_collapsed") + "\"" +
                " style=\"height: 100px;\">";
            sHTML += "                <textarea rows=\"4\" " +
                                            CommonAttribs(dr["step_id"].ToString(), "_common", false, "step_desc", "") +
                                            " help=\"Enter notes for this Command.\" reget_on_change=\"true\">" +
                                            (string.IsNullOrEmpty(dr["step_desc"].ToString()) ? "" : dr["step_desc"].ToString()) +
                                        "</textarea>" + Environment.NewLine;
            sHTML += "            </div>";

            sHTML += "            <div id=\"step_common_detail_" + dr["step_id"].ToString() + "_help\"" +
                " class=\"step_common_detail " + (sShowOnLoad == "help" ? "" : "step_common_collapsed") + "\"" +
                " style=\"height: 200px;\">";
            sHTML += (string.IsNullOrEmpty(dr["help"].ToString()) ? "" : dr["help"].ToString());
            sHTML += "            </div>";

            //some steps generate custom options we want in this pane
            //but we don't show the panel if there aren't any
            if (sOptionHTML != "")
            {
                sHTML += "          <div id=\"step_common_detail_" + dr["step_id"].ToString() + "_options\"" +
                    " class=\"step_common_detail " + (sShowOnLoad == "options" ? "" : "step_common_collapsed") + "\">";
                sHTML += "              <div>";
                sHTML += sOptionHTML;
                sHTML += "              </div>";
                sHTML += "          </div>";
            }

            //some steps have variables
            //but we don't show the panel if there aren't any
            if (sVariableHTML != "")
            {
                sHTML += "          <div id=\"step_common_detail_" + dr["step_id"].ToString() + "_variables\"" +
                    " class=\"step_common_detail " + (sShowOnLoad == "variables" ? "" : "step_common_collapsed") + "\">";
                sHTML += "              <div>";
                sHTML += sVariableHTML;
                sHTML += "              </div>";
                sHTML += "          </div>";
            }

            //close it out
            sHTML += "        </div>";

            return sHTML;
        }
        public string GetStepTemplate(DataRow dr, ref string sOptionHTML, ref string sVariableHTML)
        {
            string sStepID = dr["step_id"].ToString();
            string sFunction = dr["function_name"].ToString();
            string sFunctionXML = dr["function_xml"].ToString();

            string sHTML = "";
            XDocument xd = XDocument.Parse(sFunctionXML);

            switch (sFunction.ToLower())
            {
                //we put the most commonly used ones at the top

                /*
                 NOTE: If you are adding a new command type, be aware that 
                 you MIGHT need to modify the code in taskMethods for the wmAddStep function.
                 (depending on how your new command works)
                     
                 IF the command populates variables, it will need a case statement added to that function
                 to ensure the output_parse_type field is properly set.
                 */
                case "if":
                    sHTML = If(sStepID, sFunction, xd);
                    break;
                case "loop":
                    sHTML = Loop(sStepID, sFunction, xd);
                    break;
                case "while":
                    sHTML = While(sStepID, sFunction, xd);
                    break;
                case "new_connection":
                    sHTML = NewConnection(sStepID, sFunction, xd);
                    break;
                case "drop_connection":
                    sHTML = DropConnection(sStepID, sFunction, xd);
                    break;
                case "sql_exec":
                    sHTML = SqlExec(sStepID, sFunction, xd, dr, ref sVariableHTML);
                    break;
                case "cmd_line":
                    sHTML = CmdLine(sStepID, sFunction, xd, dr, ref sOptionHTML, ref sVariableHTML);
                    break;
                case "dos_cmd":
                    sHTML = DOSCmd(sStepID, sFunction, xd, dr, ref sOptionHTML, ref sVariableHTML);
                    break;
                case "set_variable":
                    sHTML = SetVariable(sStepID, sFunction, xd);
                    break;
                case "parse_text":
                    sHTML = ParseText(sStepID, sFunction, xd, dr, ref sVariableHTML);
                    break;
                case "read_file":
                    sHTML = ReadFile(sStepID, sFunction, xd, dr, ref sVariableHTML);
                    break;
                case "clear_variable":
                    sHTML = ClearVariable(sStepID, sFunction, xd);
                    break;
                case "wait_for_tasks":
                    sHTML = WaitForTasks(sStepID, sFunction, xd);
                    break;
                case "codeblock":
                    sHTML = Codeblock(sStepID, sFunction, xd);
                    break;
                case "log_msg":
                    sHTML = LogMessage(sStepID, sFunction, xd);
                    break;
                case "end":
                    sHTML = End(sStepID, sFunction, xd);
                    break;
                case "dataset":
                    sHTML = Dataset(sStepID, sFunction, xd);
                    break;
                case "http":
                    sHTML = HTTP(sStepID, sFunction, xd, dr, ref sVariableHTML);
                    break;
                case "win_cmd":
                    sHTML = WinCmd(sStepID, sFunction, xd, dr, ref sVariableHTML);
                    break;
                case "subtask":
                    sHTML = Subtask(sStepID, sFunction, xd);
                    break;
                case "set_asset_registry":
                    sHTML = SetAssetRegistry(sStepID, sFunction, xd);
                    break;
                case "set_task_registry":
                    sHTML = SetTaskRegistry(sStepID, sFunction, xd);
                    break;
                case "run_task":
                    sHTML = RunTask(sStepID, sFunction, xd);
                    break;
                case "sleep":
                    sHTML = Sleep(sStepID, sFunction, xd);
                    break;
                case "set_debug_level":
                    sHTML = SetDebugLevel(sStepID, sFunction, xd);
                    break;
                case "get_instance_handle":
                    sHTML = GetTaskInstance(sStepID, sFunction, xd);
                    break;
                case "cancel_task":
                    sHTML = CancelTask(sStepID, sFunction, xd);
                    break;
                case "substring":
                    sHTML = Substring(sStepID, sFunction, xd);
                    break;
                case "transfer":
                    sHTML = Transfer(sStepID, sFunction, xd);
                    break;
                case "scriptlet":
                    sHTML = Scriptlet(sStepID, sFunction, xd);
                    break;
                case "question":
                    sHTML = Question(sStepID, sFunction, xd);
                    break;
                case "break_loop":
                    sHTML = BreakLoop(sStepID, sFunction, xd);
                    break;
                case "exists":
                    sHTML = Exists(sStepID, sFunction, xd);
                    break;

                default:
                    //OK, here's some fun.  We didn't find one of our built in commands.  Perhaps it was something else?
                    sHTML = GetCustomStepTemplate(sStepID, sFunction, xd, dr, ref sOptionHTML, ref sVariableHTML);
                    break;
            }


            if (sHTML == "")
                sHTML = "Error: unable to render input form for function type: [" + sFunction + "].";

            return sHTML;
        }
        public string GetValueSnip(ref DataRow dr)
        {
            string sFunction = dr["function_name"].ToString();
            string sFunctionXML = dr["function_xml"].ToString();

            string sSnip = "";
            XDocument xd = XDocument.Parse(sFunctionXML);

            switch (sFunction.ToLower())
            {
                /*
                 NOT EVERY command type has a 'snip'able value.  The default here is to return nothing, and that's OK.
                 * 
                 * Some commands (set_variable, dataset, loop) simply can't be snipped.  Their internal content is dynamic,
                 * and the performance here must be high (as this gets done on page draw)
                 */
                case "new_connection":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//asset").Value))
                        sSnip = xd.XPathSelectElement("//asset").Value;
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//conn_name").Value))
                        sSnip += " : " + xd.XPathSelectElement("//conn_name").Value;
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//conn_type").Value))
                        sSnip += " (" + xd.XPathSelectElement("//conn_type").Value + ")";
                    break;
                case "drop_connection":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//conn_name").Value))
                        sSnip = xd.XPathSelectElement("//conn_name").Value;
                    break;
                case "sql_exec":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//conn_name").Value))
                        sSnip = xd.XPathSelectElement("//conn_name").Value;
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//mode").Value))
                        sSnip += " : " + xd.XPathSelectElement("//mode").Value;
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//sql").Value))
                        sSnip += " (" + xd.XPathSelectElement("//sql").Value + ")";
                    break;
                case "cmd_line":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//command").Value))
                        sSnip = xd.XPathSelectElement("//command").Value;
                    break;
                case "dos_cmd":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//command").Value))
                        sSnip = xd.XPathSelectElement("//command").Value;
                    break;
                case "read_file":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//filename").Value))
                        sSnip = xd.XPathSelectElement("//filename").Value;
                    break;
                case "codeblock":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//codeblock").Value))
                        sSnip = xd.XPathSelectElement("//codeblock").Value;
                    break;
                case "log_msg":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//message").Value))
                        sSnip = xd.XPathSelectElement("//message").Value;
                    break;
                case "win_cmd":
                    if (string.IsNullOrEmpty(xd.XPathSelectElement("//conn_name").Value)) return "";
                    if (string.IsNullOrEmpty(xd.XPathSelectElement("//type").Value)) return "";

                    sSnip = xd.XPathSelectElement("//conn_name").Value + " : " +
                        xd.XPathSelectElement("//type").Value;
                    break;
                case "sleep":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//seconds").Value))
                        sSnip = xd.XPathSelectElement("//seconds").Value;
                    break;
                case "end":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//status").Value))
                        sSnip = xd.XPathSelectElement("//status").Value;
                    break;
                case "send_email":
                    if (!string.IsNullOrEmpty(xd.XPathSelectElement("//subject").Value))
                        sSnip = xd.XPathSelectElement("//subject").Value;
                    break;

                default:
                    sSnip = "";
                    break;
            }

            return sSnip;
        }

        public string Transfer(string sStepID, string sFunction, XDocument xd)
        {
            XElement xFromAsset = xd.XPathSelectElement("//from_asset");
            if (xFromAsset == null) return "Error: XML does not contain from_asset";
            XElement xFromFile = xd.XPathSelectElement("//from_file");
            if (xFromFile == null) return "Error: XML does not contain from_file";
            XElement xToAsset = xd.XPathSelectElement("//to_asset");
            if (xToAsset == null) return "Error: XML does not contain to_asset";
            XElement xToFile = xd.XPathSelectElement("//to_file");
            if (xToFile == null) return "Error: XML does not contain to_file";
            XElement xMode = xd.XPathSelectElement("//mode");
            if (xMode == null) return "Error: XML does not contain mode.";
            XElement xCommand = xd.XPathSelectElement("//command");
            if (xCommand == null) return "Error: XML does not contain command.";
            XElement xRetVar = xd.XPathSelectElement("//returnvar");
            if (xRetVar == null) return "Error: XML does not contain returnvar.";

            string sFromAsset = xFromAsset.Value;
            string sFromAssetName = "";
            string sFromFile = xFromFile.Value;
            string sToAsset = xToAsset.Value;
            string sToAssetName = "";
            string sToFile = xToFile.Value;
            string sMode = xMode.Value;
            string sCommand = xCommand.Value;
            string sRetVar = xRetVar.Value;
            string sFromElementID = "";
            string sToElementID = "";
            string sHTML = "";
            string sErr = "";

            //ASSETS
            //IF IT's A GUID...
            // get the asset name belonging to this asset_id
            // OTHERWISE
            // make the sAssetName value be what's in sAssetID (a literal value in [[variable]] format)


            //FROM ASSET
            if (ui.IsGUID(sFromAsset))
            {
                string sSQL = "select asset_name from asset where asset_id = '" + sFromAsset + "'";

                if (!dc.sqlGetSingleString(ref sFromAssetName, sSQL, ref sErr))
                    return "Error retrieving 'From' Asset." + sErr;

                if (sFromAssetName == "")
                {
                    //clear the bogus value
                    SetNodeValueinCommandXML(sStepID, "//from_asset", "");

                    //return "Unable to find 'From' Asset by ID - [" + sFromAsset + "]." + sErr;
                }
            }
            else
            {
                sFromAssetName = sFromAsset;
            }

            //TO ASSET
            if (ui.IsGUID(sToAsset))
            {
                string sSQL = "select asset_name from asset where asset_id = '" + sToAsset + "'";

                if (!dc.sqlGetSingleString(ref sToAssetName, sSQL, ref sErr))
                    return "Error retrieving 'To' Asset." + sErr;

                if (sToAssetName == "")
                {
                    //clear the bogus value
                    SetNodeValueinCommandXML(sStepID, "//to_asset", "");

                    //return "Unable to find 'To' Asset by ID - [" + sToAsset + "]." + sErr;
                }
            }
            else
            {
                sToAssetName = sToAsset;
            }


            sHTML += "Mode:" + Environment.NewLine;
            sHTML += "<select reget_on_change=\"true\" " + CommonAttribs(sStepID, sFunction, true, "mode", "") + ">" + Environment.NewLine;
            sHTML += "  <option " + SetOption("SCP", sMode) + " value=\"SCP\">SCP</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("FTP", sMode) + " value=\"FTP\">FTP</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("SFTP", sMode) + " value=\"SFTP\">SFTP</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("RCP", sMode) + " value=\"RCP\">RCP</option>" + Environment.NewLine;
            sHTML += "</select>" + Environment.NewLine;


            //Command is not available for all Modes
            if (sMode == "FTP" || sMode == "SFTP")
            {
                sHTML += "Command:" + Environment.NewLine;
                sHTML += "<select reget_on_change=\"true\" " + CommonAttribs(sStepID, sFunction, true, "command", "") + ">" + Environment.NewLine;
                sHTML += "  <option " + SetOption("put", sCommand) + " value=\"put\">put</option>" + Environment.NewLine;
                sHTML += "  <option " + SetOption("get", sCommand) + " value=\"get\">get</option>" + Environment.NewLine;
                sHTML += "  <option " + SetOption("mput", sCommand) + " value=\"mput\">mput</option>" + Environment.NewLine;
                sHTML += "  <option " + SetOption("mget", sCommand) + " value=\"mget\">mget</option>" + Environment.NewLine;
                sHTML += "</select>" + Environment.NewLine;

                //and we install the default value of 'put'
                SetNodeValueinCommandXML(sStepID, "//command", "put");
            }
            else
            {
                //but we actually update it here if it's not visible, setting it to empty...
                SetNodeValueinCommandXML(sStepID, "//command", "");
            }


            sHTML += "Return Code Variable Name:" + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "returnvar", "") +
                " help=\"Enter a variable to receive the command's return code.\"" +
                " value=\"" + sRetVar + "\" /><br />" + Environment.NewLine;


            sHTML += "From&nbsp;Asset:&nbsp;" + Environment.NewLine;
            sHTML += "<input type=\"text\" " +
                CommonAttribs(sStepID, sFunction, false, "from_asset", ref sFromElementID, "hidden") +
                " value=\"" + sFromAsset + "\"" + " />" + Environment.NewLine;
            sHTML += "<input type=\"text\"" +
                " help=\"Select an Asset or enter a variable.\"" +
                (!ui.IsGUID(sFromAsset) ? " syntax=\"variable\"" : "") +
                " step_id=\"" + sStepID + "\"" +
                " class=\"code w200px\"" +
                " is_required=\"true\"" +
                " id=\"fn_xfer_fromassetname_" + sStepID + "\"" +
                (ui.IsGUID(sFromAsset) ? " disabled=\"disabled\"" : "") +
                " onchange=\"javascript:pushStepFieldChangeVia(this, '" + sFromElementID + "');\"" +
                " value=\"" + sFromAssetName + "\" />" + Environment.NewLine;

            sHTML += "<img class=\"fn_field_clear_btn pointer\" clear_id=\"fn_xfer_fromassetname_" + sStepID + "\"" +
                " style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                " alt=\"\" title=\"Clear\" />";

            sHTML += "<img class=\"asset_picker_btn pointer\" alt=\"\"" +
                " link_to=\"" + sFromElementID + "\"" +
                " target_field_id=\"fn_xfer_fromassetname_" + sStepID + "\"" +
                " step_id=\"" + sStepID + "\"" +
                " src=\"../images/icons/search.png\" />" + Environment.NewLine;

            //second file box is not necessary if we are mput/mget
            if (sCommand == "mput" || sCommand == "mget")
            {
                //it's not visible, setting it to empty...
                SetNodeValueinCommandXML(sStepID, "//from_file", "");
            }
            else
            {
                sHTML += "<br />File:" + Environment.NewLine;
                sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "from_file", "w90pct");
                sHTML += " help=\"Enter a filename on the source Asset.\" value=\"" + sFromFile + "\" />";
            }
            sHTML += "<br />";


            sHTML += "To&nbsp;Asset:&nbsp;" + Environment.NewLine;
            sHTML += "<input type=\"text\" " +
                CommonAttribs(sStepID, sFunction, false, "to_asset", ref sToElementID, "hidden") +
                " value=\"" + sToAsset + "\"" + " />" + Environment.NewLine;
            sHTML += "<input type=\"text\"" +
                " help=\"Select an Asset or enter a variable.\"" +
                (!ui.IsGUID(sToAsset) ? " syntax=\"variable\"" : "") +
                " step_id=\"" + sStepID + "\"" +
                " class=\"code w200px\"" +
                " is_required=\"true\"" +
                " id=\"fn_xfer_toassetname_" + sStepID + "\"" +
                (ui.IsGUID(sToAsset) ? " disabled=\"disabled\"" : "") +
                " onchange=\"javascript:pushStepFieldChangeVia(this, '" + sToElementID + "');\"" +
                " value=\"" + sToAssetName + "\" />" + Environment.NewLine;

            sHTML += "<img class=\"fn_field_clear_btn pointer\" clear_id=\"fn_xfer_toassetname_" + sStepID + "\"" +
                " style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                " alt=\"\" title=\"Clear\" />";

            sHTML += "<img class=\"asset_picker_btn pointer\" alt=\"\"" +
                " link_to=\"" + sToElementID + "\"" +
                " target_field_id=\"fn_xfer_toassetname_" + sStepID + "\"" +
                " step_id=\"" + sStepID + "\"" +
                " src=\"../images/icons/search.png\" />" + Environment.NewLine;

            sHTML += "<br />File:" + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "to_file", "w90pct");
            sHTML += " help=\"Enter a filename on the target Asset.\" value=\"" + sToFile + "\" />";

            return sHTML;
        }
        public string ReadFile(string sStepID, string sFunction, XDocument xd, DataRow dr, ref string sVarHTML)
        {
            XElement xFileName = xd.XPathSelectElement("//filename");
            if (xFileName == null) return "Error: XML does not contain filename";
            XElement xStart = xd.XPathSelectElement("//start");
            if (xStart == null) return "Error: XML does not contain start.";
            XElement xNumChars = xd.XPathSelectElement("//num_chars");
            if (xNumChars == null) return "Error: XML does not contain num_chars.";

            string sFileName = xFileName.Value;
            string sStart = xStart.Value;
            string sNumChars = xNumChars.Value;
            string sHTML = "";

            sHTML += "<table width=\"99%\" border=\"0\">" + Environment.NewLine;

            sHTML += "<tr><td class=\"w1pct\">File&nbsp;Name:&nbsp;</td>" + Environment.NewLine;
            sHTML += "<td colspan=\"3\"><input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "filename", "w95pct") +
                " help=\"Enter a file name to read.\" value=\"" + sFileName + "\" /></td></tr>" + Environment.NewLine;

            sHTML += "<tr><td class=\"w1pct\">Starting&nbsp;Position:&nbsp;</td>" + Environment.NewLine;
            sHTML += "<td><input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "start", "") +
                " validate_as=\"posint\"" +
                " help=\"Enter an optional starting position to begin reading.\"" +
                " value=\"" + sStart + "\" /></td>" + Environment.NewLine;

            sHTML += "<tr><td class=\"w1pct\">Number&nbsp;of&nbsp;Characters:&nbsp;</td>" + Environment.NewLine;
            sHTML += "<td><input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "num_chars", "") +
                " help=\"Enter an optional number of characters to read from the start position.\"" +
                " validate_as=\"posint\"" +
                " value=\"" + sNumChars + "\" /></td></tr>" + Environment.NewLine;

            sHTML += "</table>" + Environment.NewLine;


            //variables
            sVarHTML += DrawVariableSectionForDisplay(dr, true);

            return sHTML;
        }
        public string ParseText(string sStepID, string sFunction, XDocument xd, DataRow dr, ref string sVarHTML)
        {
            XElement xText = xd.XPathSelectElement("//text");
            if (xText == null) return "Error: XML does not contain 'text' field.";

            string sCommand = xText.Value;
            string sHTML = "";
            string sFieldID = "";

            // we gotta get the field id first, but don't show the textarea until after
            string sCommonAttribsForTA = CommonAttribs(sStepID, sFunction, true, "text", ref sFieldID, "");

            sHTML += "Text to Process: " + Environment.NewLine;
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sFieldID + "\" /><br />" + Environment.NewLine;

            sHTML += "<textarea " + sCommonAttribsForTA;
            sHTML += " help=\"Enter text to be processed into variables.\">" + sCommand + "</textarea>";

            //variables
            sVarHTML += DrawVariableSectionForDisplay(dr, true);

            return sHTML;
        }
        public string SetTaskRegistry(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";

            XElement xXPath = xd.XPathSelectElement("//xpath");
            if (xXPath == null) return "Error: XML does not contain xpath";
            XElement xValue = xd.XPathSelectElement("//value");
            if (xValue == null) return "Error: XML does not contain value";
            //XElement xCreateNode = xd.XPathSelectElement("//create_node");
            //if (xCreateNode == null) return "Error: XML does not contain create_node";

            string sXPath = xXPath.Value;
            string sValue = xValue.Value;
            //string sCreateNode = xCreateNode.Value;

            sHTML += "XPath:<br />";
            //sHTML += "&nbsp; (Create If Missing? <input type=\"checkbox\" " +
            //    CommonAttribs(sStepID, sFunction, true, "create_node", "") + 
            //    " " + SetCheckRadio("1", sCreateNode) + " />)<br />" + Environment.NewLine;

            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "xpath", "w75pct") +
                " value=\"" + sXPath + "\" />" + Environment.NewLine;

            sHTML += "<br />";
            sHTML += "Value:<br />" + Environment.NewLine;
            sHTML += "<textarea rows=\"2\" " + CommonAttribs(sStepID, sFunction, false, "value", "w75pct") +
                ">" + sValue + "</textarea>" + Environment.NewLine;

            return sHTML;
        }
        public string SetAssetRegistry(string sStepID, string sFunction, XDocument xd)
        {
            string sAssetID = "";
            string sAssetName = "";
            string sHTML = "";
            string sErr = "";

            XElement xAsset = xd.XPathSelectElement("//asset_id");
            if (xAsset == null) return "Error: XML does not contain asset_id";
            sAssetID = xAsset.Value;

            XElement xXPath = xd.XPathSelectElement("//xpath");
            if (xXPath == null) return "Error: XML does not contain xpath";
            XElement xValue = xd.XPathSelectElement("//value");
            if (xValue == null) return "Error: XML does not contain value";
            //XElement xCreateNode = xd.XPathSelectElement("//create_node");
            //if (xCreateNode == null) return "Error: XML does not contain create_node";

            string sXPath = xXPath.Value;
            string sValue = xValue.Value;
            //string sCreateNode = xCreateNode.Value;

            //IF IT's A GUID...
            // get the asset name belonging to this asset_id
            // OTHERWISE
            // make the sAssetName value be what's in sAssetID (a literal value in [[variable]] format)
            if (ui.IsGUID(sAssetID))
            {
                string sSQL = "select asset_name from asset where asset_id = '" + sAssetID + "'";

                if (!dc.sqlGetSingleString(ref sAssetName, sSQL, ref sErr))
                    return "Error retrieving Run Task Asset Name." + sErr;

                if (sAssetName == "")
                    return "Unable to find Asset by ID - [" + sAssetID + "]." + sErr;
            }
            else
            {
                sAssetName = sAssetID;
            }




            //all good, draw the widget
            // asset
            string sFieldID = "";
            sHTML += "<input type=\"text\" " +
                CommonAttribs(sStepID, sFunction, false, "asset_id", ref sFieldID, "hidden") +
                " value=\"" + sAssetID + "\"" + " />"
                + Environment.NewLine;

            sHTML += "Asset: " + Environment.NewLine;
            sHTML += "<input type=\"text\"" +
                " help=\"Select an Asset or enter a variable.\"" +
                (!ui.IsGUID(sAssetID) ? " syntax=\"variable\"" : "") +
                " step_id=\"" + sStepID + "\"" +
                " class=\"code w75pct\"" +
                " id=\"fn_setregistry_assetname_" + sStepID + "\"" +
                (ui.IsGUID(sAssetID) ? " disabled=\"disabled\"" : "") +
                " onchange=\"javascript:pushStepFieldChangeVia(this, '" + sFieldID + "');\"" +
                " value=\"" + sAssetName + "\" />" + Environment.NewLine;

            sHTML += "<img class=\"fn_field_clear_btn pointer\" clear_id=\"fn_setregistry_assetname_" + sStepID + "\"" +
                " style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                " alt=\"\" title=\"Clear\" />";

            sHTML += "<img class=\"asset_picker_btn pointer\" alt=\"\"" +
                " link_to=\"" + sFieldID + "\"" +
                " target_field_id=\"fn_setregistry_assetname_" + sStepID + "\"" +
                " step_id=\"" + sStepID + "\"" +
                " src=\"../images/icons/search.png\" />" + Environment.NewLine;

            sHTML += "<br />";
            sHTML += "XPath:<br />";
            //sHTML += "&nbsp; (Create If Missing? <input type=\"checkbox\" " +
            //    CommonAttribs(sStepID, sFunction, true, "create_node", "") + 
            //    " " + SetCheckRadio("1", sCreateNode) + " />)<br />" + Environment.NewLine;

            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "xpath", "w75pct") +
                " value=\"" + sXPath + "\" />" + Environment.NewLine;

            sHTML += "<br />";
            sHTML += "Value:<br />" + Environment.NewLine;
            sHTML += "<textarea rows=\"2\" " + CommonAttribs(sStepID, sFunction, false, "value", "w75pct") +
                ">" + sValue + "</textarea>" + Environment.NewLine;

            return sHTML;
        }
        public string RunTask(string sStepID, string sFunction, XDocument xd)
        {
            string sOriginalTaskID = "";
            string sVersion = "";
            string sActualTaskID = "";
            string sTime = "";
            string sHandle = "";
            //string sOnSuccess = "";
            //string sOnError = "";
            string sAssetID = "";
            string sAssetName = "";
            string sLabel = "";
            string sHTML = "";
            string sErr = "";

            XElement xOTID = xd.XPathSelectElement("//original_task_id");
            if (xOTID == null) return "Error: XML does not contain original_task_id";
            sOriginalTaskID = xOTID.Value;

            XElement xVer = xd.XPathSelectElement("//version");
            if (xVer == null) return "Error: XML does not contain version";
            sVersion = xVer.Value;

            XElement xHandle = xd.XPathSelectElement("//handle");
            if (xHandle == null) return "Error: XML does not contain handle";
            sHandle = xHandle.Value;

            XElement xTime = xd.XPathSelectElement("//time_to_wait");
            if (xTime == null) return "Error: XML does not contain time_to_wait";
            sTime = xTime.Value;

            //XElement xSuccess = xd.XPathSelectElement("//on_success");
            //if (xSuccess == null) return "Error: XML does not contain on_success";
            //sOnSuccess = xSuccess.Value;

            //XElement xError = xd.XPathSelectElement("//on_error");
            //if (xError == null) return "Error: XML does not contain on_error";
            //sOnError = xError.Value;

            //get the name and code for belonging to this otid and version
            if (ui.IsGUID(sOriginalTaskID))
            {
                string sSQL = "select task_id, task_code, task_name from task" +
                    " where original_task_id = '" + sOriginalTaskID + "'" +
                    (sVersion == "" ? " and default_version = 1" : " and version = '" + sVersion + "'");

                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                {
                    return "Error retrieving target Task.(1)" + sErr;
                }

                if (dr != null)
                {
                    sLabel = dr["task_code"].ToString() + " : " + dr["task_name"].ToString();
                    sActualTaskID = dr["task_id"].ToString();
                }
                else
                {
                    //It's possible that the user changed the task from the picker but had 
                    //selected a version, which is still there now but may not apply to the new task.
                    //so, if the above SQL failed, try again by resetting the version box to the default.
                    sSQL = "select task_id, task_code, task_name from task" +
                        " where original_task_id = '" + sOriginalTaskID + "'" +
                        " and default_version = 1";

                    if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                    {
                        return "Error retrieving target Task.(2)<br />" + sErr;
                    }

                    if (dr != null)
                    {
                        sLabel = dr["task_code"].ToString() + " : " + dr["task_name"].ToString();
                        sActualTaskID = dr["task_id"].ToString();

                        //oh yeah, and set the version field to null since it was wrong.
                        SetNodeValueinCommandXML(sStepID, "//version", "");
                    }
                    else
                    {
                        //a default doesnt event exist, really fail...
                        return "Unable to find task [" + sOriginalTaskID + "] version [" + sVersion + "]." + sErr;
                    }
                }
            }

            XElement xAsset = xd.XPathSelectElement("//asset_id");
            if (xAsset == null) return "Error: XML does not contain asset_id";
            sAssetID = xAsset.Value;

            //IF IT's A GUID...
            // get the asset name belonging to this asset_id
            // OTHERWISE
            // make the sAssetName value be what's in sAssetID (a literal value in [[variable]] format)
            if (ui.IsGUID(sAssetID))
            {
                string sSQL = "select asset_name from asset where asset_id = '" + sAssetID + "'";

                if (!dc.sqlGetSingleString(ref sAssetName, sSQL, ref sErr))
                    return "Error retrieving Run Task Asset Name." + sErr;

                if (sAssetName == "")
                    return "Unable to find Asset by ID - [" + sAssetID + "]." + sErr;
            }
            else
            {
                sAssetName = sAssetID;
            }




            //all good, draw the widget
            string sOTIDField = "";

            sHTML += "<input type=\"text\" " +
                CommonAttribs(sStepID, sFunction, true, "original_task_id", ref sOTIDField, "hidden") +
                " value=\"" + sOriginalTaskID + "\"" + " reget_on_change=\"true\" />"
                + Environment.NewLine;


            sHTML += "Task: " + Environment.NewLine;
            sHTML += "<input type=\"text\"" +
                " onkeydown=\"return false;\"" +
                " onkeypress=\"return false;\"" +
                " is_required=\"true\"" +
                " step_id=\"" + sStepID + "\"" +
                " class=\"code w75pct\"" +
                " id=\"fn_run_task_taskname_" + sStepID + "\"" +
                " value=\"" + sLabel + "\" />" + Environment.NewLine;
            sHTML += "<img class=\"task_picker_btn pointer\" alt=\"\"" +
                " target_field_id=\"" + sOTIDField + "\"" +
                " step_id=\"" + sStepID + "\"" +
                " src=\"../images/icons/search.png\" />" + Environment.NewLine;
            if (sActualTaskID != "")
            {
                sHTML += "<img class=\"task_open_btn pointer\" alt=\"Edit Task\"" +
                    " task_id=\"" + sActualTaskID + "\"" +
                    " src=\"../images/icons/kedit_16.png\" />" + Environment.NewLine;
                sHTML += "<img class=\"task_print_btn pointer\" alt=\"View Task\"" +
                    " task_id=\"" + sActualTaskID + "\"" +
                    " src=\"../images/icons/printer.png\" />" + Environment.NewLine;
            }

            //versions
            if (ui.IsGUID(sOriginalTaskID))
            {
                sHTML += "<br />";
                sHTML += "Version: " + Environment.NewLine;
                sHTML += "<select " + CommonAttribs(sStepID, sFunction, false, "version", "") +
                    " reget_on_change=\"true\">" + Environment.NewLine;
                //default
                sHTML += "<option " + SetOption("", sVersion) + " value=\"\">Default</option>" + Environment.NewLine;

                string sSQL = "select version from task" +
                    " where original_task_id = '" + sOriginalTaskID + "'" +
                    " order by version";
                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    return "Database Error:" + sErr;
                }

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        sHTML += "<option " + SetOption(dr["version"].ToString(), sVersion) + " value=\"" + dr["version"] + "\">" + dr["version"].ToString() + "</option>" + Environment.NewLine;
                    }
                }
                else
                {
                    return "Unable to continue - Cannot find Version for Task [" + sOriginalTaskID + "].";
                }

                sHTML += "</select></span>" + Environment.NewLine;
            }



            sHTML += "<br />";

            // asset
            sHTML += "<input type=\"text\" " +
                CommonAttribs(sStepID, sFunction, false, "asset_id", ref sOTIDField, "hidden") +
                " value=\"" + sAssetID + "\"" + " />"
                + Environment.NewLine;

            sHTML += "Asset: " + Environment.NewLine;
            sHTML += "<input type=\"text\"" +
                " help=\"Select an Asset or enter a variable.\"" +
                (!ui.IsGUID(sAssetID) ? " syntax=\"variable\"" : "") +
                " step_id=\"" + sStepID + "\"" +
                " class=\"code w75pct\"" +
                " id=\"fn_run_task_assetname_" + sStepID + "\"" +
                (ui.IsGUID(sAssetID) ? " disabled=\"disabled\"" : "") +
                " onchange=\"javascript:pushStepFieldChangeVia(this, '" + sOTIDField + "');\"" +
                " value=\"" + sAssetName + "\" />" + Environment.NewLine;

            sHTML += "<img class=\"fn_field_clear_btn pointer\" clear_id=\"fn_run_task_assetname_" + sStepID + "\"" +
                " style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                " alt=\"\" title=\"Clear\" />";

            sHTML += "<img class=\"asset_picker_btn pointer\" alt=\"\"" +
                " link_to=\"" + sOTIDField + "\"" +
                " target_field_id=\"fn_run_task_assetname_" + sStepID + "\"" +
                " step_id=\"" + sStepID + "\"" +
                " src=\"../images/icons/search.png\" />" + Environment.NewLine;

            sHTML += "<br />";
            sHTML += "Task Handle: <input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "handle", "") +
                " value=\"" + sHandle + "\" />" + Environment.NewLine;

            sHTML += "Time to Wait: <input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "time_to_wait", "") +
                " value=\"" + sTime + "\" />" + Environment.NewLine;

            //sHTML += "<br />";
            //sHTML += "The following Command will be executed on Success:<br />" + Environment.NewLine;
            ////enable the dropzone for the Success action
            //sHTML += DrawDropZone(sStepID, sOnSuccess, sFunction, "on_success", "", false);

            //sHTML += "The following Command will be executed on Error:<br />" + Environment.NewLine;
            ////enable the dropzone for the Error action
            //sHTML += DrawDropZone(sStepID, sOnError, sFunction, "on_error", "", false);

            //key/value pairs
            sHTML += "<hr />";
            sHTML += "Parameters:";
            sHTML += DrawKeyValueSection(sStepID, sFunction, xd, true, false, "Input", "Value");

            return sHTML;
        }
        public string End(string sStepID, string sFunction, XDocument xd)
        {
            XElement xStatus = xd.XPathSelectElement("//status");
            if (xStatus == null) return "Error: XML does not contain status";
            XElement xMsg = xd.XPathSelectElement("//message");
            if (xMsg == null) return "Error: XML does not contain message";

            string sStatus = xStatus.Value;
            string sMsg = xMsg.Value;
            string sFieldID = "";
            string sHTML = "";

            sHTML += "End with Status:" + Environment.NewLine;
            sHTML += "<select " + CommonAttribs(sStepID, sFunction, true, "status", "") + ">" + Environment.NewLine;
            sHTML += "  <option " + SetOption("Error", sStatus) + " value=\"Error\">Error</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("Completed", sStatus) + " value=\"Completed\">Completed</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("Cancelled", sStatus) + " value=\"Cancelled\">Cancelled</option>" + Environment.NewLine;
            sHTML += "</select> <br />" + Environment.NewLine;

            // we gotta get the field id first, but don't show the textarea until after
            string sCommonAttribs = CommonAttribs(sStepID, sFunction, false, "message", ref sFieldID, "");

            sHTML += "Log Message (optional): " + Environment.NewLine;
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sFieldID + "\" /><br />" + Environment.NewLine;

            sHTML += "<textarea rows=\"2\" " + sCommonAttribs +
            " help=\"Enter a log message.\"" +
            ">" + sMsg + "</textarea>" + Environment.NewLine;

            return sHTML;
        }
        public string ClearVariable(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";

            sHTML += "<div id=\"v" + sStepID + "_vars\">";
            sHTML += "Variables to Clear:<br />";

            IEnumerable<XElement> xPairs = xd.XPathSelectElements("//variable");
            int i = 1;
            foreach (XElement xe in xPairs)
            {
                XElement xKey = xe.XPathSelectElement("name");

                //Trac#389 - Make sure variable names are trimmed of whitespace if it exists
                //hokey, but doing it here because the field update function is global.
                if (xKey.Value.Trim() != xKey.Value)
                    SetNodeValueinCommandXML(sStepID, "variable[" + i.ToString() + "]/name", xKey.Value.Trim());

                sHTML += "&nbsp;&nbsp;&nbsp;<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "variable[" + i.ToString() + "]/name", "") +
                    " validate_as=\"variable\"" +
                    " value=\"" + xKey.Value + "\"" +
                    " help=\"Enter a Variable name.\"" +
                    " />" + Environment.NewLine;

                sHTML += "<span class=\"fn_var_remove_btn pointer\" index=\"" + i.ToString() + "\" step_id=\"" + sStepID + "\">";
                sHTML += "<img style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                    " alt=\"\" title=\"Remove\" /></span>";

                //break it every three fields
                if (i % 3 == 0 && i >= 3)
                    sHTML += "<br />";

                i++;
            }

            sHTML += "<div class=\"fn_clearvar_add_btn pointer\"" +
                " add_to_id=\"v" + sStepID + "_vars\"" +
                " step_id=\"" + sStepID + "\">" +
                "<img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\"" +
                " alt=\"\" title=\"Add another.\" />( click to add another )</div>";
            sHTML += "</div>";

            return sHTML;
        }
        public string WaitForTasks(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";

            sHTML += "<div id=\"v" + sStepID + "_handles\">";
            sHTML += "Task Handles:<br />";

            IEnumerable<XElement> xPairs = xd.XPathSelectElements("//handle");
            int i = 1;
            foreach (XElement xe in xPairs)
            {
                XElement xKey = xe.XPathSelectElement("name");

                sHTML += "&nbsp;&nbsp;&nbsp;<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "handle[" + i.ToString() + "]/name", "") +
                    " validate_as=\"variable\"" +
                    " value=\"" + xKey.Value + "\"" +
                    " help=\"Enter a Handle name.\"" +
                    " />" + Environment.NewLine;

                sHTML += "<span class=\"fn_handle_remove_btn pointer\" index=\"" + i.ToString() + "\" step_id=\"" + sStepID + "\">";
                sHTML += "<img style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                    " alt=\"\" title=\"Remove\" /></span>";

                //break it every three fields
                if (i % 3 == 0 && i >= 3)
                    sHTML += "<br />";

                i++;
            }

            sHTML += "<div class=\"fn_wft_add_btn pointer\"" +
                " add_to_id=\"v" + sStepID + "_handles\"" +
                " step_id=\"" + sStepID + "\">" +
                "<img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\"" +
                " alt=\"\" title=\"Add another.\" />( click to add another )</div>";
            sHTML += "</div>";

            return sHTML;
        }
        public string Dataset(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";

            // Draw the template selector
            sHTML += "Select Template (optional) ";
            sHTML += "<button onclick=\"return false;\" class=\"template_picker_btn pointer elipse_btn\" step_id=\"" + sStepID + "\">...</button><br/>";
            sHTML += DrawKeyValueSection(sStepID, sFunction, xd, true, true, "Key", "Value");

            return sHTML;
        }
        public string Exists(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";

            //IEnumerable<XElement> xTests = xd.XPathSelectElements("//variable/name");
            //sHTML += "<div id=\"exists_" + sStepID + "_conditions\" number=\"" + xTests.Count().ToString() + "\">";

            sHTML += "<div id=\"v" + sStepID + "_vars\">";
            sHTML += "Variables to Test:<br />";

            IEnumerable<XElement> xPairs = xd.XPathSelectElements("//variable");
            int i = 1;
            foreach (XElement xe in xPairs)
            {
                XElement xKey = xe.XPathSelectElement("name");
                XElement xIsTrue = xe.XPathSelectElement("is_true");

                //Trac#389 - Make sure variable names are trimmed of whitespace if it exists
                //hokey, but doing it here because the field update function is global.
                if (xKey.Value.Trim() != xKey.Value)
                    SetNodeValueinCommandXML(sStepID, "variable[" + i.ToString() + "]/name", xKey.Value.Trim());

                sHTML += "&nbsp;&nbsp;&nbsp;<input type=\"text\" " +
                    CommonAttribs(sStepID, sFunction, true, "variable[" + i.ToString() + "]/name", "") +
                    " validate_as=\"variable\"" +
                    " value=\"" + xKey.Value + "\"" +
                    " help=\"Enter a Variable name.\"" +
                    " />";

                sHTML += "&nbsp; Is True:<input type=\"checkbox\" " +
                    CommonAttribs(sStepID, sFunction, true, "variable[" + i.ToString() +
                    "]/is_true", "") + " " + SetCheckRadio("1", xIsTrue.Value) + " />" + Environment.NewLine;

                sHTML += "<span class=\"fn_var_remove_btn pointer\" index=\"" + i.ToString() + "\" step_id=\"" + sStepID + "\">";
                sHTML += "<img style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                    " alt=\"\" title=\"Remove\" /></span>";

                //break it every three fields
                //if (i % 3 == 0 && i >= 3)
                sHTML += "<br />";

                i++;
            }

            sHTML += "<div class=\"fn_exists_add_btn pointer\"" +
                " add_to_id=\"v" + sStepID + "_vars\"" +
                " step_id=\"" + sStepID + "\">" +
                "<img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\"" +
                " alt=\"\" title=\"Add another.\" />( click to add another )</div>";
            sHTML += "</div>";

            // Exists have a Positive and Negative action
            XElement xPositiveAction = xd.XPathSelectElement("//actions/positive_action");
            if (xPositiveAction == null) return "Error: XML does not contain positive_action";

            XElement xNegativeAction = xd.XPathSelectElement("//actions/negative_action");
            if (xNegativeAction == null) return "Error: XML does not contain negative_action";

            string sCol = "";

            //here's the embedded content
            sCol = "actions/positive_action";

            if (xNegativeAction != null)
                sHTML += DrawDropZone(sStepID, xPositiveAction.Value, sFunction, sCol, "Positive Action:<br />", true);
            else
                sHTML += "ERROR: Malformed XML for Step ID [" + sStepID + "].  Missing '" + sCol + "' element.";


            sCol = "actions/negative_action";

            if (xNegativeAction != null)
                sHTML += DrawDropZone(sStepID, xNegativeAction.Value, sFunction, sCol, "Negative Action:<br />", true);
            else
                sHTML += "ERROR: Malformed XML for Step ID [" + sStepID + "].  Missing '" + sCol + "' element.";


            // The End.
            return sHTML;
        }
        public string If(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";

            IEnumerable<XElement> xTests = xd.XPathSelectElements("//tests/test");
            sHTML += "<div id=\"if_" + sStepID + "_conditions\" number=\"" + xTests.Count().ToString() + "\">";

            int i = 1; //because XPath starts at "1"

            foreach (XElement xTest in xTests)
            {
                XElement xEval = xTest.XPathSelectElement("eval");
                XElement xAction = xTest.XPathSelectElement("action");


                // we gotta get the field id first, but don't show the textarea until after
                string sFieldID = "";
                string sCol = "tests/test[" + i.ToString() + "]/eval";
                string sCommonAttribsForTA = CommonAttribs(sStepID, sFunction, true, sCol, ref sFieldID, "");

                //a way to delete the section you just added
                if (i == 1)
                {

                    XDocument xGlobals = XDocument.Load(HttpContext.Current.Server.MapPath("~/pages/luCompareTemplates.xml"));

                    if (xGlobals == null)
                    {
                        sHTML += "(No Compare templates file available)<br />";
                    }
                    else
                    {

                        sHTML += "Comparison Template: <select class=\"compare_templates\" textarea_id=\"" + sFieldID + "\">" + Environment.NewLine;
                        sHTML += "  <option value=\"\"></option>" + Environment.NewLine;

                        IEnumerable<XElement> xTemplates = xGlobals.XPathSelectElement("templates").XPathSelectElements("template");
                        foreach (XElement xEl in xTemplates)
                        {
                            sHTML += "  <option value=\"" + xEl.XPathSelectElement("value").Value.ToString() + "\">" + xEl.XPathSelectElement("name").Value.ToString() + "</option>" + Environment.NewLine;
                        }
                        sHTML += "</select> <br />" + Environment.NewLine;
                    }


                    sHTML += "If:<br />";
                }
                else
                {
                    sHTML += "<div id=\"if_" + sStepID + "_else_" + i.ToString() + "\" class=\"fn_if_else_section\">";
                    sHTML += "<span class=\"fn_if_remove_btn pointer\" number=\"" + i + "\" step_id=\"" + sStepID + "\">" +
                        "<img style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\" alt=\"\" title=\"Remove this Else If condition.\" /></span> ";
                    sHTML += "&nbsp;&nbsp;&nbsp;Else If:<br />";
                }



                if (xEval != null)
                    sHTML += "<textarea " + sCommonAttribsForTA +
                        " help=\"Enter a test condition.\"" +
                        ">" + xEval.Value + "</textarea><br />" + Environment.NewLine;
                else
                    sHTML += "ERROR: Malformed XML for Step ID [" + sStepID + "].  Missing '" + sCol + "' element.";


                //here's the embedded content
                sCol = "tests/test[" + i.ToString() + "]/action";

                if (xAction != null)
                    sHTML += DrawDropZone(sStepID, xAction.Value, sFunction, sCol, "Action:<br />", true);
                else
                    sHTML += "ERROR: Malformed XML for Step ID [" + sStepID + "].  Missing '" + sCol + "' element.";


                if (i != 1)
                    sHTML += "</div>";

                i++;
            }

            sHTML += "</div>";


            //draw an add link.  The rest will happen on the client.
            sHTML += "<div class=\"fn_if_add_btn pointer\" add_to_id=\"if_" + sStepID + "_conditions\" step_id=\"" + sStepID + "\" next_index=\"" + i.ToString() + "\"><img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\" alt=\"\" title=\"Add another Else If section.\" />( click to add another 'Else If' section )</div>";


            sHTML += "<div id=\"if_" + sStepID + "_else\" class=\"fn_if_else_section\">";

            //the final 'else' area
            XElement xElse = xd.XPathSelectElement("//else");
            if (xElse != null)
            {
                sHTML += "<span class=\"fn_if_removeelse_btn pointer\" step_id=\"" + sStepID + "\">" +
                   "<img style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\" alt=\"\" title=\"Remove this Else condition.\" /></span> ";
                sHTML += "Else (no 'If' conditions matched):";
                sHTML += DrawDropZone(sStepID, xElse.Value, sFunction, "else", "", true);
            }
            else
                //draw an add link.  The rest will happen on the client.
                sHTML += "<div class=\"fn_if_addelse_btn pointer\" add_to_id=\"if_" + sStepID + "_else\" step_id=\"" + sStepID + "\"><img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\" alt=\"\" title=\"Add an Else section.\" />( click to add a final 'Else' section )</div>";

            sHTML += "</div>";


            return sHTML;
        }
        public string SqlExec(string sStepID, string sFunction, XDocument xd, DataRow dr, ref string sVarHTML)
        {
            /*TAKE NOTE:
            * 
            * Similar to the windows command...
            * ... we are updating a record here when we GET the data.
            * 
            * Why?  Because this command has modes.
            * The data has different meaning depending on the 'mode'.
            * 
            * So, on the client if the user changes the 'mode', the new command may not need all the fields
            * that the previous selection needed.
            * 
            * So, we just wipe any unused fields based on the current mode.
            * */
            XElement xCommand = xd.XPathSelectElement("//sql");
            if (xCommand == null) return "Error: XML does not contain sql.";
            XElement xConnName = xd.XPathSelectElement("//conn_name");
            if (xConnName == null) return "Error: XML does not contain conn_name.";
            XElement xMode = xd.XPathSelectElement("//mode");
            if (xMode == null) return "Error: XML does not contain mode.";
            XElement xHandle = xd.XPathSelectElement("//handle");
            if (xHandle == null) return "Error: XML does not contain handle.";

            string sCommand = xCommand.Value;
            string sConnName = xConnName.Value;
            string sMode = xMode.Value;
            string sHandle = xHandle.Value;
            string sHTML = "";
            string sElementID = "";
            string sFieldID = "";
            bool bDrawVarButton = false;
            bool bDrawSQLBox = false;
            bool bDrawHandle = false;
            bool bDrawKeyValSection = false;

            sHTML += "Connection:" + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "conn_name", ref sElementID, "");
            sHTML += " help=\"Enter an active connection where this SQL will be executed.\" value=\"" + sConnName + "\" />";
            sHTML += "<img class=\"conn_picker_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/search.png\"" +
                " link_to=\"" + sElementID + "\" />" + Environment.NewLine;

            sHTML += "Mode:" + Environment.NewLine;
            sHTML += "<select " + CommonAttribs(sStepID, sFunction, true, "mode", "") + " reget_on_change=\"true\">" + Environment.NewLine;
            sHTML += "  <option " + SetOption("SQL", sMode) + " value=\"SQL\">SQL</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("BEGIN", sMode) + " value=\"BEGIN\">BEGIN</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("COMMIT", sMode) + " value=\"COMMIT\">COMMIT</option>" + Environment.NewLine;
            //sHTML += "  <option " + SetOption("COMMIT / BEGIN", sMode) + " value=\"COMMIT / BEGIN\">COMMIT / BEGIN</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("ROLLBACK", sMode) + " value=\"ROLLBACK\">ROLLBACK</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("EXEC", sMode) + " value=\"EXEC\">EXEC</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("PL/SQL", sMode) + " value=\"PL/SQL\">PL/SQL</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("PREPARE", sMode) + " value=\"PREPARE\">PREPARE</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("RUN", sMode) + " value=\"RUN\">RUN</option>" + Environment.NewLine;

            sHTML += "</select>" + Environment.NewLine;


            //here we go!
            //certain modes show different fields.

            if (sMode == "BEGIN" || sMode == "COMMIT" || sMode == "ROLLBACK")
            {
                //these modes have no SQL or pairs or variables
                SetNodeValueinCommandXML(sStepID, "//sql", "");
                SetNodeValueinCommandXML(sStepID, "//handle", "");
                RemoveFromCommandXML(sStepID, "/function/pair");
                RemoveStepVars(sStepID);
            }
            else if (sMode == "PREPARE")
            {
                bDrawSQLBox = true;
                bDrawHandle = true;

                //this mode has no pairs or variables
                RemoveFromCommandXML(sStepID, "/function/pair");
                RemoveStepVars(sStepID);
            }
            else if (sMode == "RUN")
            {
                bDrawVarButton = true;
                bDrawHandle = true;
                bDrawKeyValSection = true;

                //this mode has no SQL
                SetNodeValueinCommandXML(sStepID, "//sql", "");
            }
            else
            {
                bDrawVarButton = true;
                bDrawSQLBox = true;

                SetNodeValueinCommandXML(sStepID, "//handle", "");
                //the default mode has no pairs
                RemoveFromCommandXML(sStepID, "/function/pair");
            }

            if (bDrawVarButton)
                sVarHTML += DrawVariableSectionForDisplay(dr, true);

            if (bDrawHandle)
            {
                sHTML += "Handle:" + Environment.NewLine;
                sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "handle", "");
                sHTML += " help=\"Enter a handle for this prepared statement.\" value=\"" + sHandle + "\" />";
            }

            if (bDrawKeyValSection)
                sHTML += DrawKeyValueSection(sStepID, sFunction, xd, false, false, "Bind", "Value");

            if (bDrawSQLBox)
            {
                // we gotta get the field id first, but don't show the textarea until after
                string sCommonAttribsForTA = CommonAttribs(sStepID, sFunction, true, "sql", ref sFieldID, "");

                sHTML += "<br />SQL:" + Environment.NewLine;
                //big box button
                sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                    " src=\"../images/icons/edit_16.png\"" +
                    " link_to=\"" + sFieldID + "\" /><br />" + Environment.NewLine;

                sHTML += "<textarea " + sCommonAttribsForTA;
                sHTML += " help=\"Enter a SQL query or procedure.\">" + sCommand + "</textarea>";
            }
            return sHTML;
        }
        public string CmdLine(string sStepID, string sFunction, XDocument xd, DataRow dr, ref string sOptionHTML, ref string sVarHTML)
        {
            XElement xCommand = xd.XPathSelectElement("//command");
            if (xCommand == null) return "Error: XML does not contain command.";
            XElement xConnName = xd.XPathSelectElement("//conn_name");
            if (xConnName == null) return "Error: XML does not contain conn_name.";

            string sCommand = xCommand.Value;
            string sConnName = xConnName.Value;
            string sHTML = "";
            string sElementID = "";
            string sTextareaID = "";

            sHTML += "Connection:" + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "conn_name", ref sElementID, "");
            sHTML += " help=\"Enter an active connection where this command will be executed.\" value=\"" + sConnName + "\" />";
            sHTML += "<img class=\"conn_picker_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/search.png\"" +
                " link_to=\"" + sElementID + "\" /><br />" + Environment.NewLine;

            // we gotta get the field id first, but don't show the textarea until after
            string sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "command", ref sTextareaID, "");

            sHTML += "Command: " + Environment.NewLine;
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sTextareaID + "\" /><br />" + Environment.NewLine;

            sHTML += "<textarea " + sCommonAttribs;
            sHTML += " help=\"Enter a command to execute.\">" + sCommand + "</textarea>";

            //variables
            sVarHTML += DrawVariableSectionForDisplay(dr, true);

            //for the options panel
            XElement xTimeout = xd.XPathSelectElement("//timeout");
            if (xCommand == null) return "Error: XML does not contain timeout.";
            XElement xPos = xd.XPathSelectElement("//positive_response");
            if (xPos == null) return "Error: XML does not contain positive_response.";
            XElement xNeg = xd.XPathSelectElement("//negative_response");
            if (xNeg == null) return "Error: XML does not contain negative_response.";

            string sTimeout = xTimeout.Value;
            string sPos = ui.SafeHTML(xPos.Value);
            string sNeg = ui.SafeHTML(xNeg.Value);

            sOptionHTML += "<table class=\"fn_layout_table\">" + Environment.NewLine;

            sOptionHTML += "<tr>" + Environment.NewLine;
            sOptionHTML += "<td class=\"fn_label_cell\">Timeout:</td>" + Environment.NewLine;
            sOptionHTML += "<td><input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "timeout", "w200px");
            sOptionHTML += " help=\"Enter a timeout in seconds.\" value=\"" + sTimeout + "\" /></td>";
            sOptionHTML += "</tr>" + Environment.NewLine;

            sOptionHTML += "<tr>" + Environment.NewLine;
            sOptionHTML += "<td class=\"fn_label_cell\">Positive Response:</td>" + Environment.NewLine;
            sOptionHTML += "<td><input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "positive_response", "w200px");
            sOptionHTML += " help=\"Enter a value to watch for to determine if the command was successful.\" value=\"" + sPos + "\" /></td>";
            sOptionHTML += "</tr>" + Environment.NewLine;

            sOptionHTML += "<tr>" + Environment.NewLine;
            sOptionHTML += "<td class=\"fn_label_cell\">Negative Response:</td>" + Environment.NewLine;
            sOptionHTML += "<td><input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "negative_response", "w200px");
            sOptionHTML += " help=\"Enter a value to watch for to determine if the command failed.\" value=\"" + sNeg + "\" /></td>";
            sOptionHTML += "</tr>" + Environment.NewLine;

            sOptionHTML += "</table>";



            return sHTML;
        }
        public string DOSCmd(string sStepID, string sFunction, XDocument xd, DataRow dr, ref string sOptionHTML, ref string sVarHTML)
        {
            XElement xCommand = xd.XPathSelectElement("//command");
            if (xCommand == null) return "Error: XML does not contain command.";

            string sCommand = xCommand.Value;
            string sHTML = "";
            string sTextareaID = "";

            // we gotta get the field id first, but don't show the textarea until after
            string sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "command", ref sTextareaID, "");

            sHTML += "Command: " + Environment.NewLine;
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sTextareaID + "\" /><br />" + Environment.NewLine;

            sHTML += "<textarea " + sCommonAttribs;
            sHTML += " help=\"Enter a command to execute.\">" + sCommand + "</textarea>";

            //variables
            sVarHTML += DrawVariableSectionForDisplay(dr, true);

            //for the options panel
            XElement xTimeout = xd.XPathSelectElement("//timeout");
            if (xCommand == null) return "Error: XML does not contain timeout.";
            XElement xPos = xd.XPathSelectElement("//positive_response");
            if (xPos == null) return "Error: XML does not contain positive_response.";
            XElement xNeg = xd.XPathSelectElement("//negative_response");
            if (xNeg == null) return "Error: XML does not contain negative_response.";

            string sTimeout = xTimeout.Value;
            string sPos = ui.SafeHTML(xPos.Value);
            string sNeg = ui.SafeHTML(xNeg.Value);

            sOptionHTML += "<table class=\"fn_layout_table\">" + Environment.NewLine;

            sOptionHTML += "<tr>" + Environment.NewLine;
            sOptionHTML += "<td class=\"fn_label_cell\">Timeout:</td>" + Environment.NewLine;
            sOptionHTML += "<td><input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "timeout", "w200px");
            sOptionHTML += " help=\"Enter a timeout in seconds.\" value=\"" + sTimeout + "\" /></td>";
            sOptionHTML += "</tr>" + Environment.NewLine;

            sOptionHTML += "<tr>" + Environment.NewLine;
            sOptionHTML += "<td class=\"fn_label_cell\">Positive Response:</td>" + Environment.NewLine;
            sOptionHTML += "<td><input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "positive_response", "w200px");
            sOptionHTML += " help=\"Enter a value to watch for to determine if the command was successful.\" value=\"" + sPos + "\" /></td>";
            sOptionHTML += "</tr>" + Environment.NewLine;

            sOptionHTML += "<tr>" + Environment.NewLine;
            sOptionHTML += "<td class=\"fn_label_cell\">Negative Response:</td>" + Environment.NewLine;
            sOptionHTML += "<td><input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "negative_response", "w200px");
            sOptionHTML += " help=\"Enter a value to watch for to determine if the command failed.\" value=\"" + sNeg + "\" /></td>";
            sOptionHTML += "</tr>" + Environment.NewLine;

            sOptionHTML += "</table>";



            return sHTML;
        }
        public string Question(string sStepID, string sFunction, XDocument xd)
        {
            XElement xInstructions = xd.XPathSelectElement("//instructions");
            if (xInstructions == null) return "Error: XML does not contain instructions.";
            XElement xQuestion = xd.XPathSelectElement("//question");
            if (xQuestion == null) return "Error: XML does not contain question.";
            XElement xER = xd.XPathSelectElement("//evidence_requirements");
            if (xER == null) return "Error: XML does not contain evidence_requirements.";

            string sInstructions = xInstructions.Value;
            string sQuestion = xQuestion.Value;
            string sER = xER.Value;
            string sFieldID = "";
            string sHTML = "";

            // we gotta get the field id first, but don't show the textarea until after
            string sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "instructions", ref sFieldID, "");
            sHTML += "Instructions: ";
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sFieldID + "\" /><br />" + Environment.NewLine;
            sHTML += "<textarea rows=\"3\" " + sCommonAttribs +
                " help=\"Enter instructions to assist the Assessor.\"" +
                ">" + sInstructions + "</textarea>" + Environment.NewLine;


            sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "question", ref sFieldID, "");
            sHTML += "Question: ";
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sFieldID + "\" /><br />" + Environment.NewLine;
            sHTML += "<textarea rows=\"3\" " + sCommonAttribs +
                " help=\"Enter a Question.\"" +
                ">" + sQuestion + "</textarea>" + Environment.NewLine;


            sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "evidence_requirements", ref sFieldID, "");
            sHTML += "Evidence Requirements: ";
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sFieldID + "\" /><br />" + Environment.NewLine;
            sHTML += "<textarea rows=\"3\" " + sCommonAttribs +
                " help=\"What are the evidence requirements for this Question?\"" +
                ">" + sER + "</textarea>" + Environment.NewLine;

            return sHTML;
        }
        public string NewConnection(string sStepID, string sFunction, XDocument xd)
        {
            XElement xAsset = xd.XPathSelectElement("//asset");
            if (xAsset == null) return "Error: XML does not contain asset";
            XElement xConnName = xd.XPathSelectElement("//conn_name");
            if (xConnName == null) return "Error: XML does not contain conn_name";
            XElement xConnType = xd.XPathSelectElement("//conn_type");
            if (xConnType == null) return "Error: XML does not contain conn_type";

            string sAssetID = xAsset.Value;
            string sConnName = xConnName.Value;
            string sConnType = xConnType.Value;
            string sElementID = "";
            string sAssetName = "";

            string sHTML = "";

            string sErr = "";

            sHTML += "Connect via: " + Environment.NewLine;
            sHTML += "<select " + CommonAttribs(sStepID, sFunction, true, "conn_type", "") + ">" + Environment.NewLine;


            // lookup the valid connection types
            DataTable dt = new DataTable();
            dt = getConnectionTypes(ref sErr);
            if (sErr != "")
            {
                return "Connection lookup error: " + sErr;
            }

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    sHTML += "<option " + SetOption(dr["connection_type"].ToString(), sConnType) +
                        " value=\"" + dr["connection_type"] + "\">" +
                        dr["connection_type"] + "</option>" + Environment.NewLine;
                }
            }
            else
            {
                return "Unable to continue - no connection types defined in the database.";
            }

            sHTML += "</select>" + Environment.NewLine;



            //ASSET
            //IF IT's A GUID...
            // get the asset name belonging to this asset_id
            // OTHERWISE
            // make the sAssetName value be what's in sAssetID (a literal value in [[variable]] format)
	    // 2011-10-11 - PMD - removing asset table reference
            //if (ui.IsGUID(sAssetID))
            //{
            //    string sSQL = "select asset_name from asset where asset_id = '" + sAssetID + "'";

            //    if (!dc.sqlGetSingleString(ref sAssetName, sSQL, ref sErr))
            //        return "Error retrieving New Connection Asset.<br />" + sErr;

            //    if (sAssetName == "")
            //    {
            //        //clear the bogus value
            //        SetNodeValueinCommandXML(sStepID, "asset", "");

            //        //return "Unable to find Asset by ID - [" + sAssetID + "]." + sErr;
            //    }
            //}
            //else
            //{
                sAssetName = sAssetID;
            //}


            sHTML += " to Asset " + Environment.NewLine;
            sHTML += "<input type=\"text\" " +
                CommonAttribs(sStepID, sFunction, false, "asset", ref sElementID, "hidden") +
                " value=\"" + sAssetID + "\"" + " />"
                + Environment.NewLine;
            sHTML += "<input type=\"text\"" +
                " help=\"Select an Asset or enter a variable.\"" +
                " step_id=\"" + sStepID + "\"" +
                " class=\"code w400px\"" +
                " is_required=\"true\"" +
                " id=\"fn_new_connection_assetname_" + sStepID + "\"" +
                " onchange=\"javascript:pushStepFieldChangeVia(this, '" + sElementID + "');\"" +
                " value=\"" + sAssetName + "\" />" + Environment.NewLine;

			
			// 2011-10-11 - PMD - remove asset search box and clear button, issue # 34
            //sHTML += "<img class=\"fn_field_clear_btn pointer\" clear_id=\"fn_new_connection_assetname_" + sStepID + "\"" +
            //    " style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
            //    " alt=\"\" title=\"Clear\" />";

	    	// 2011-10-11 - PMD - remove asset search box and clear button, issue # 34
            //sHTML += "<img class=\"asset_picker_btn pointer\" alt=\"\"" +
            //    " link_to=\"" + sElementID + "\"" +
            //    " target_field_id=\"fn_new_connection_assetname_" + sStepID + "\"" +
            //    " step_id=\"" + sStepID + "\"" +
            //    " src=\"../images/icons/search.png\" />" + Environment.NewLine;

            sHTML += " as " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "conn_name", "w200px") +
                " help=\"Name this connection for reference in the Task.\" value=\"" + sConnName + "\" />" + Environment.NewLine;
            sHTML += "" + Environment.NewLine;


            return sHTML;
        }
        public string DropConnection(string sStepID, string sFunction, XDocument xd)
        {
            XElement xConnName = xd.XPathSelectElement("//conn_name");
            if (xConnName == null) return "Error: XML does not contain conn_name";

            string sConnName = xConnName.Value;

            string sHTML = "";

            string sElementID = "";

            sHTML += "Connection Name: " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "conn_name", ref sElementID, "w200px") +
                " help=\"Name of the connection to drop.\" value=\"" + sConnName + "\" />" + Environment.NewLine;
            sHTML += "<img class=\"conn_picker_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/search.png\"" +
                " link_to=\"" + sElementID + "\" /><br />" + Environment.NewLine;

            return sHTML;
        }
        public string HTTP(string sStepID, string sFunction, XDocument xd, DataRow dr, ref string sVarHTML)
        {
            XElement xType = xd.XPathSelectElement("//type");
            if (xType == null) return "Error: XML does not contain type";
            XElement xURL = xd.XPathSelectElement("//url");
            if (xURL == null) return "Error: XML does not contain url";

            string sType = xType.Value;
            string sURL = xURL.Value;
            string sFieldID = "";
            string sHTML = "";

            sHTML += "Request Type: " + Environment.NewLine;
            sHTML += "<select " + CommonAttribs(sStepID, sFunction, true, "type", "") + ">" + Environment.NewLine;
            //display AND select a blank option only IF THERE is no value in sCommand
            if (sType == "")
                sHTML += "  <option " + SetOption("", sType) + " value=\"\"></option>" + Environment.NewLine;

            //note: commented these out per bug 1176
            //NOTE: the XML template on the lu_function table was also changed to have GET as the default.
            //might should change that when this is undone.

            sHTML += "  <option " + SetOption("GET", sType) + " value=\"GET\">GET</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("POST", sType) + " value=\"POST\">POST</option>" + Environment.NewLine;
            //sHTML += "  <option " + SetOption("HEAD", sType) + " value=\"HEAD\">HEAD</option>" + Environment.NewLine;
            sHTML += "</select> <br />" + Environment.NewLine;

            // we gotta get the field id first, but don't show the textarea until after
            string sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "url", ref sFieldID, "");

            sHTML += "URL: " + Environment.NewLine;
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sFieldID + "\" /><br />" + Environment.NewLine;

            sHTML += "<textarea rows=\"2\" " + sCommonAttribs +
                " help=\"Enter a URL.\"" +
                ">" + sURL + "</textarea>" + Environment.NewLine;

            //key/value pairs
            //note: commented these out per bug 1176
            // until HTTP post is enabled
            sHTML += "<hr />";
            sHTML += "Post Parameters:";
            sHTML += DrawKeyValueSection(sStepID, sFunction, xd, true, false, "Name", "Value");

            //variables
            sVarHTML += DrawVariableSectionForDisplay(dr, true);

            return sHTML;
        }
        public string WinCmd(string sStepID, string sFunction, XDocument xd, DataRow dr, ref string sVarHTML)
        {
            /*TAKE NOTE:
             * 
             * (this stinks but it has to be done)...
             * ... we are updating a record here when we GET the data.
             * 
             * Why?  Because this command stores data in generic fields in the XML.
             * The data has different meaning depending on the 'type' and 'command'.
             * 
             * So, on the client if the user changes the 'command', the new command may not need all the fields
             * that the previous selection needed.
             * 
             * But we don't want to put all that logic on the client in JScript... no that would be nasty.
             * 
             * So, here we just wipe any unused fields based on the current type and command.
             * This means if a user changed type or command, the next time they look at it (which is immediately BTW)
             * this function will clean up some data.
             *
             * 
             * By the way... there are five (5) generic 'parameter_n' columns in the XML.
             * */

            string sFieldsToClear = "";

            XElement xConnName = xd.XPathSelectElement("//conn_name");
            if (xConnName == null) return "Error: XML does not contain conn_name.";
            XElement xType = xd.XPathSelectElement("//type");
            if (xType == null) return "Error: XML does not contain type.";
            XElement xCmd = xd.XPathSelectElement("//command");
            if (xCmd == null) return "Error: XML does not contain command.";

            string sConnName = xConnName.Value;
            string sType = xType.Value;
            string sCommand = xCmd.Value;
            string sElementID = "";
            string sFieldID = "";
            string sHTML = "";

            sHTML += "Connection:" + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "conn_name", ref sElementID, "");
            sHTML += " help=\"Enter an active Windows connection where this command will be executed.\" value=\"" + sConnName + "\" />";
            sHTML += "<img class=\"conn_picker_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/search.png\"" +
                " link_to=\"" + sElementID + "\" /><br />" + Environment.NewLine;

            //still using our ~~ notation for the option tags... they'll get 'selected' by the main routine
            // that parses this HTML *after* we construct it.

            sHTML += "Command Type:" + Environment.NewLine;

            sHTML += "<select " + CommonAttribs(sStepID, sFunction, true, "type", "") +
                " reget_on_change=\"true\">" + Environment.NewLine;

            //display AND select a blank option only IF THERE is no value in sCommand
            if (sType == "")
                sHTML += "  <option " + SetOption("", sType) + " value=\"\"></option>" + Environment.NewLine;

            sHTML += "  <option " + SetOption("WMI", sType) + " value=\"WMI\">WMI Query</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("Registry", sType) +
                " value=\"Registry\">Registry Query</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("Security", sType) +
                " value=\"Security\">Security Policy</option>" + Environment.NewLine;
            sHTML += "</select>" + Environment.NewLine;


            //go ahead and get the parameters here before the switch
            XElement xP0 = xd.XPathSelectElement("//parameter_0");
            if (xP0 == null) return "Error: XML does not contain parameter_0.";
            XElement xP1 = xd.XPathSelectElement("//parameter_1");
            if (xP1 == null) return "Error: XML does not contain parameter_1.";
            XElement xP2 = xd.XPathSelectElement("//parameter_2");
            if (xP2 == null) return "Error: XML does not contain parameter_2.";
            XElement xP3 = xd.XPathSelectElement("//parameter_3");
            if (xP3 == null) return "Error: XML does not contain parameter_3.";
            XElement xP4 = xd.XPathSelectElement("//parameter_4");
            if (xP4 == null) return "Error: XML does not contain parameter_4.";

            string sP0 = xP0.Value;
            string sP1 = xP1.Value;
            string sP2 = xP2.Value;
            //string sP3 = xP3.Value;
            //string sP4 = xP4.Value;

            //which option to show depends on the command type
            //wmi only has one box
            switch (sType)
            {
                case "WMI":
                    //WMI has no 'command' so clear it (because we may have switched from another type
                    // and left a remnant of data in there.
                    SetNodeValueinCommandXML(sStepID, "//command", "");

                    //set a default if it's blank
                    if (sP1 == "")
                    {
                        sP1 = "CIMV2";
                        //and actually set the value
                        SetNodeValueinCommandXML(sStepID, "//parameter_1", sP1);
                    }


                    sHTML += "Namespace: " + Environment.NewLine;
                    sHTML += "<input type=\"text\" " +
                        CommonAttribs(sStepID, sFunction, true, "parameter_1", ref sFieldID, "w200px") +
                        " help=\"Enter a Namespace.\"" +
                        " value=\"" + sP1 + "\" />" +
                        "<img class=\"namespace_picker_btn pointer\" src=\"../images/icons/search.png\" alt=\"\" link_to=\"" + sFieldID + "\" title=\"Select a Namespace\" target=\"col\" />" + Environment.NewLine;

                    sHTML += "Max Rows: " + Environment.NewLine;
                    sHTML += "<input type=\"text\" " +
                        CommonAttribs(sStepID, sFunction, false, "parameter_2", "w100px") +
                        " help=\"Enter a Maximum number of rows to return.\"" +
                        " validate_as=\"posint\"" +
                        " value=\"" + sP2 + "\" />" + Environment.NewLine;

                    // we gotta get the field id first, but don't show the textarea until after
                    string sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "parameter_0", ref sFieldID, "");

                    sHTML += "<br />Command: " + Environment.NewLine;

                    //big box button
                    sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                        " src=\"../images/icons/edit_16.png\"" +
                        " link_to=\"" + sFieldID + "\" /><br />" + Environment.NewLine;

                    sHTML += "<textarea rows=\"2\" " + sCommonAttribs +
                    " help=\"Enter a WMI Command.\"" +
                    ">" + sP0 + "</textarea>" + Environment.NewLine;

                    sFieldsToClear = "3,4";
                    break;
                case "Registry":
                    sHTML += "Command:" + Environment.NewLine;
                    sHTML += "<select " + CommonAttribs(sStepID, sFunction, true, "command", "") +
                        " reget_on_change=\"true\">" + Environment.NewLine;


                    //we may have switched from another type and left a remnant of data in the command field.
                    //clear everything IF IT IS NOT A VALID VALUE for the Registry command.
                    if ("Get Keys,Get Key Values,Get Values".IndexOf(sCommand) == -1)
                    {
                        sCommand = "";
                        SetNodeValueinCommandXML(sStepID, "//command", "");
                        SetNodeValueinCommandXML(sStepID, "//parameter_0", "");
                        SetNodeValueinCommandXML(sStepID, "//parameter_1", "");
                        SetNodeValueinCommandXML(sStepID, "//parameter_2", "");
                        SetNodeValueinCommandXML(sStepID, "//parameter_3", "");
                        SetNodeValueinCommandXML(sStepID, "//parameter_4", "");
                    }

                    //display AND select a blank option only IF THERE is no value in sCommand
                    //this prevents the appearance that a value has been selected.
                    //sure, it required the user to make an extra click, but this is necessary since a change on one field
                    //cannot update another without custom client side script.
                    if (sCommand == "")
                        sHTML += "  <option " + SetOption("", sCommand) +
                            " value=\"\"></option>" + Environment.NewLine;

                    //TODO: temporarily commented out per bug 1117
                    //reactivated 9/1/09 per Patrick
                    sHTML += "  <option " + SetOption("Get Keys", sCommand) +
                       " value=\"Get Keys\">Get Keys</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get Key Values", sCommand) +
                        " value=\"Get Key Values\">Get Key Values</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get Values", sCommand) +
                        " value=\"Get Values\">Get Values</option>" + Environment.NewLine;

                    sHTML += "</select> <br />" + Environment.NewLine;

                    //fields depend on the command
                    switch (sCommand)
                    {
                        case "Get Keys":
                            sHTML += "Registry Path: " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " +
                                CommonAttribs(sStepID, sFunction, true, "parameter_0", "w98pct") +
                                " help=\"Enter a Registry path from which to retrieve Keys.\"" +
                                " value=\"" + sP0 + "\" />" + Environment.NewLine;

                            sFieldsToClear = "1,2,3,4";
                            break;
                        case "Get Key Values":
                            sHTML += "Registry Path: " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " +
                                CommonAttribs(sStepID, sFunction, true, "parameter_0", "w98pct") +
                                " help=\"Enter a Registry path from which to retrieve Key/Value pairs.\"" +
                                " value=\"" + sP0 + "\" />" + Environment.NewLine;

                            sFieldsToClear = "1,2,3,4";
                            break;
                        case "Get Values":
                            sHTML += "Registry Path: " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " +
                                CommonAttribs(sStepID, sFunction, true, "parameter_0", "w98pct") +
                                " help=\"Enter a Registry path.\"" +
                                " value=\"" + sP0 + "\" />" + Environment.NewLine;

                            sHTML += "Key: " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " +
                                CommonAttribs(sStepID, sFunction, true, "parameter_1", "w98pct") +
                                " help=\"Enter a Key.\"" +
                                " value=\"" + sP1 + "\" />" + Environment.NewLine;

                            sFieldsToClear = "2,3,4";
                            break;
                        default:
                            //has nothing
                            sFieldsToClear = "0,1,2,3,4";
                            break;
                    }

                    break;
                case "Security":
                    sHTML += "Command:" + Environment.NewLine;
                    sHTML += "<select " + CommonAttribs(sStepID, sFunction, true, "command", "") +
                        " reget_on_change=\"true\">" + Environment.NewLine;

                    //we may have switched from another type and left a remnant of data in the command field.
                    //clear everything IF IT IS NOT A VALID VALUE for the Security command.
                    if ("Get Password Policy,Get Audit Policy,Get Users,Get User Properties,Get User Groups,Get User Rights,Get Users with Right,Get Local Groups,Get Group Members,Get Group Properties,Get Group Rights".IndexOf(sCommand) == -1)
                    {
                        sCommand = "";
                        SetNodeValueinCommandXML(sStepID, "//command", "");
                        SetNodeValueinCommandXML(sStepID, "//parameter_0", "");
                        SetNodeValueinCommandXML(sStepID, "//parameter_1", "");
                        SetNodeValueinCommandXML(sStepID, "//parameter_2", "");
                        SetNodeValueinCommandXML(sStepID, "//parameter_3", "");
                        SetNodeValueinCommandXML(sStepID, "//parameter_4", "");
                    }

                    //display AND select a blank option only IF THERE is no value in sCommand
                    if (sCommand == "")
                        sHTML += "  <option " + SetOption("", sCommand) + " value=\"\"></option>" + Environment.NewLine;

                    sHTML += "  <option " + SetOption("Get Password Policy", sCommand) +
                        " value=\"Get Password Policy\">Get Password Policy</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get Audit Policy", sCommand) +
                        " value=\"Get Audit Policy\">Get Audit Policy</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get Users", sCommand) +
                        " value=\"Get Users\">Get Users</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get User Properties", sCommand) +
                        " value=\"Get User Properties\">Get User Properties</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get User Groups", sCommand) +
                        " value=\"Get User Groups\">Get User Groups</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get User Rights", sCommand) +
                        " value=\"Get User Rights\">Get User Rights</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get Users with Right", sCommand) +
                        " value=\"Get Users with Right\">Get Users with Right</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get Local Groups", sCommand) +
                        " value=\"Get Local Groups\">Get Local Groups</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get Group Members", sCommand) +
                        " value=\"Get Group Members\">Get Group Members</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get Group Properties", sCommand) +
                        " value=\"Get Group Properties\">Get Group Properties</option>" + Environment.NewLine;
                    sHTML += "  <option " + SetOption("Get Group Rights", sCommand) +
                        " value=\"Get Group Rights\">Get Group Rights</option>" + Environment.NewLine;
                    sHTML += "</select> <br />" + Environment.NewLine;

                    //fields depend on the command
                    switch (sCommand)
                    {
                        case "Get Users":
                            sFieldsToClear = "0,1,2,3,4";
                            break;
                        case "Get User Properties":
                            sHTML += "User Name (required): " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "parameter_0", "w98pct") +
                                " help=\"Gets all the properties for the specified User. Enter a User Name.\"" +
                                " value=\"" + sP0 + "\" />" + Environment.NewLine;

                            sFieldsToClear = "1,2,3,4";
                            break;
                        case "Get User Rights":
                            sHTML += "User Name (required): " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "parameter_0", "w98pct") +
                                " help=\"Enter a User Name for which to retrieve rights.\"" +
                                " value=\"" + sP0 + "\" />" + Environment.NewLine;

                            sFieldsToClear = "1,2,3,4";
                            break;
                        case "Get User Groups":
                            sHTML += "User Name (required): " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "parameter_0", "w98pct") +
                                " help=\"Gets all the Groups to which the specified User belongs  Enter a User Name.\"" +
                                " value=\"" + sP0 + "\" />" + Environment.NewLine;

                            sFieldsToClear = "1,2,3,4";
                            break;
                        case "Get Group Members":
                            sHTML += "Group Name (required): " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "parameter_0", "w98pct") +
                                " help=\"Enter a Group from which to retrieve Users.\"" +
                                " value=\"" + sP0 + "\" />" + Environment.NewLine;

                            sFieldsToClear = "1,2,3,4";
                            break;
                        case "Get Group Properties":
                            sHTML += "Group Name (required): " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "parameter_0", "w98pct") +
                                " help=\"Gets all the properties for the specified Group. Enter a User Name.\"" +
                                " value=\"" + sP0 + "\" />" + Environment.NewLine;

                            sFieldsToClear = "1,2,3,4";
                            break;
                        case "Get Group Rights":
                            sHTML += "Group Name (required): " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "parameter_0", "w98pct") +
                                " help=\"Enter a Group Name for which to retrieve rights.\"" +
                                " value=\"" + sP0 + "\" />" + Environment.NewLine;

                            sFieldsToClear = "1,2,3,4";
                            break;
                        case "Get Users with Right":
                            sHTML += "Right (required): " + Environment.NewLine;
                            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "parameter_0", "w98pct") +
                                " help=\"Enter a Right to retrieve a list of all users having that right.\"" +
                                " value=\"" + sP0 + "\" />" + Environment.NewLine;

                            sFieldsToClear = "1,2,3,4";
                            break;
                        default:
                            //has nothing
                            sFieldsToClear = "0,1,2,3,4";
                            break;
                    }

                    break;
                default:
                    //has nothing
                    sFieldsToClear = "0,1,2,3,4";
                    break;
            }

            //clean up the XML data
            string[] sFields = sFieldsToClear.Split(',');
            foreach (string sField in sFields)
            {
                SetNodeValueinCommandXML(sStepID, "//parameter_" + sField, "");
            }

            //variables
            sVarHTML += DrawVariableSectionForDisplay(dr, true);

            return sHTML;
        }
        public string Codeblock(string sStepID, string sFunction, XDocument xd)
        {
            XElement xCB = xd.XPathSelectElement("//codeblock");
            if (xCB == null) return "Error: XML does not contain codeblock";

            string sCB = xCB.Value;
            string sHTML = "";
            string sElementID = "";

            sHTML += "Codeblock: " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "codeblock", ref sElementID, "") +
                " reget_on_change=\"true\"" +
                " help=\"Enter a Codeblock Name or variable, or select a Codeblock from the picker.\"" +
                " value=\"" + sCB + "\" />" + Environment.NewLine;
            sHTML += "<img class=\"codeblock_picker_btn pointer\" alt=\"Pick a Codeblock\"" +
                " src=\"../images/icons/search.png\"" +
                " link_to=\"" + sElementID + "\" />" + Environment.NewLine;

            if (sCB != "")
            {
                //don't enable the picker if it isn't a valid codeblock on this task.
                string sSQL = "";
                string sErr = "";
                string sCodeblocks = "";

                //lookup the valid codeblocks
                sSQL = "select codeblock_name from task_codeblock" +
                    " where task_id = (select task_id from task_step" +
                    " where step_id = '" + sStepID + "')";
                if (!dc.csvGetList(ref sCodeblocks, sSQL, ref sErr, false))
                {
                    return sErr;
                }

                if (!string.IsNullOrEmpty(sCodeblocks))
                {
                    if (sCodeblocks.IndexOf(sCB) > -1)
                    {
                        sHTML += "<img class=\"codeblock_goto_btn pointer\" alt=\"Go To Codeblock\"" +
                            " codeblock=\"" + sCB + "\"" +
                            " src=\"../images/icons/kedit_16.png\" />" + Environment.NewLine;
                    }
                }
                else
                {
                    return "Unable to continue - no connection types defined in the database.";
                }

            }
            return sHTML;
        }
        public string Subtask(string sStepID, string sFunction, XDocument xd)
        {
            string sOriginalTaskID = "";
            string sVersion = "";
            string sActualTaskID = "";
            string sLabel = "";
            string sHTML = "";
            string sErr = "";

            XElement xOTID = xd.XPathSelectElement("//original_task_id");
            if (xOTID == null) return "Error: XML does not contain original_task_id";
            sOriginalTaskID = xOTID.Value;

            XElement xVer = xd.XPathSelectElement("//version");
            if (xVer == null) return "Error: XML does not contain version";
            sVersion = xVer.Value;

            //get the name and code for belonging to this otid and version
            if (ui.IsGUID(sOriginalTaskID))
            {
                string sSQL = "select task_id, task_code, task_name from task" +
                    " where original_task_id = '" + sOriginalTaskID + "'" +
                    (sVersion == "" ? " and default_version = 1" : " and version = '" + sVersion + "'");

                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                {
                    return "Error retrieving subtask.(1)<br />" + sErr;
                }

                if (dr != null)
                {
                    sLabel = dr["task_code"].ToString() + " : " + dr["task_name"].ToString();
                    sActualTaskID = dr["task_id"].ToString();
                }
                else
                {
                    //It's possible that the user changed the task from the picker but had 
                    //selected a version, which is still there now but may not apply to the new task.
                    //so, if the above SQL failed, try again by resetting the version box to the default.
                    sSQL = "select task_id, task_code, task_name from task" +
                        " where original_task_id = '" + sOriginalTaskID + "'" +
                        " and default_version = 1";

                    if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                    {
                        return "Error retrieving subtask.(2)<br />" + sErr;
                    }

                    if (dr != null)
                    {
                        sLabel = dr["task_code"].ToString() + " : " + dr["task_name"].ToString();
                        sActualTaskID = dr["task_id"].ToString();

                        //oh yeah, and set the version field to null since it was wrong.
                        SetNodeValueinCommandXML(sStepID, "//version", "");
                    }
                    else
                    {
                        //a default doesnt event exist, really fail...
                        return "Unable to find task [" + sOriginalTaskID + "] version [" + sVersion + "]." + sErr;
                    }
                }
            }



            //all good, draw the widget
            string sOTIDField = "";

            sHTML += "<input type=\"text\" " +
                CommonAttribs(sStepID, sFunction, true, "original_task_id", ref sOTIDField, "hidden") +
                " value=\"" + sOriginalTaskID + "\" reget_on_change=\"true\" />" + Environment.NewLine;

            sHTML += "Task: " + Environment.NewLine;
            sHTML += "<input type=\"text\"" +
                " onkeydown=\"return false;\"" +
                " onkeypress=\"return false;\"" +
                " is_required=\"true\"" +
                " step_id=\"" + sStepID + "\"" +
                " class=\"code w75pct\"" +
                " id=\"fn_subtask_taskname_" + sStepID + "\"" +
                " value=\"" + sLabel + "\" />" + Environment.NewLine;
            sHTML += "<img class=\"task_picker_btn pointer\" alt=\"\"" +
                " target_field_id=\"" + sOTIDField + "\"" +
                " step_id=\"" + sStepID + "\"" +
                " src=\"../images/icons/search.png\" />" + Environment.NewLine;
            if (sActualTaskID != "")
            {
                sHTML += "<img class=\"task_open_btn pointer\" alt=\"Edit Task\"" +
                    " task_id=\"" + sActualTaskID + "\"" +
                    " src=\"../images/icons/kedit_16.png\" />" + Environment.NewLine;
                sHTML += "<img class=\"task_print_btn pointer\" alt=\"View Task\"" +
                    " task_id=\"" + sActualTaskID + "\"" +
                    " src=\"../images/icons/printer.png\" />" + Environment.NewLine;
            }
            //versions
            if (ui.IsGUID(sOriginalTaskID))
            {
                sHTML += "<br />";
                sHTML += "Version: " + Environment.NewLine;
                sHTML += "<select " + CommonAttribs(sStepID, sFunction, false, "version", "") +
                    " reget_on_change=\"true\">" + Environment.NewLine;
                //default
                sHTML += "<option " + SetOption("", sVersion) + " value=\"\">Default</option>" + Environment.NewLine;

                string sSQL = "select version from task" +
                    " where original_task_id = '" + sOriginalTaskID + "'" +
                    " order by version";
                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    return "Database Error:<br />" + sErr;
                }

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        sHTML += "<option " + SetOption(dr["version"].ToString(), sVersion) + " value=\"" + dr["version"] + "\">" + dr["version"].ToString() + "</option>" + Environment.NewLine;
                    }
                }
                else
                {
                    return "Unable to continue - no connection types defined in the database.";
                }

                sHTML += "</select></span>" + Environment.NewLine;
            }

            //let's display a div for the parameters
            sHTML += "<div class=\"subtask_view_parameters pointer\" id=\"stvp_" + sActualTaskID + "\">";
            sHTML += "<img src=\"../images/icons/kedit_16.png\" alt=\"Parameters\"> ( click to view parameters )";
            sHTML += "</div>";

            return sHTML;
        }
        public string SetDebugLevel(string sStepID, string sFunction, XDocument xd)
        {
            XElement xDebugLevel = xd.XPathSelectElement("//debug_level");
            if (xDebugLevel == null) return "Error: XML does not contain debug_level";

            string sDebugLevel = xDebugLevel.Value;
            string sHTML = "";

            sHTML += "Logging Level: " + Environment.NewLine;
            sHTML += "Mode:" + Environment.NewLine;
            sHTML += "<select " + CommonAttribs(sStepID, sFunction, true, "debug_level", "") + ">" + Environment.NewLine;
            sHTML += "  <option " + SetOption("0", sDebugLevel) + " value=\"0\">None</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("1", sDebugLevel) + " value=\"1\">Minimal</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("2", sDebugLevel) + " value=\"2\">Normal</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("3", sDebugLevel) + " value=\"3\">Enhanced</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("4", sDebugLevel) + " value=\"4\">Verbose</option>" + Environment.NewLine;
            sHTML += "</select>" + Environment.NewLine;

            return sHTML;
        }
        public string Sleep(string sStepID, string sFunction, XDocument xd)
        {
            XElement xSeconds = xd.XPathSelectElement("//seconds");
            if (xSeconds == null) return "Error: XML does not contain seconds";

            string sSeconds = xSeconds.Value;
            string sHTML = "";

            sHTML += "Seconds to Sleep: " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "seconds", "") +
                " help=\"Enter the number of seconds to sleep.\" value=\"" + sSeconds + "\" />" + Environment.NewLine;

            return sHTML;
        }

        public string GetTaskInstance(string sStepID, string sFunction, XDocument xd)
        {
            XElement xInstance = xd.XPathSelectElement("//instance");
            if (xInstance == null) return "Error: XML does not contain instance";
            XElement xHandle = xd.XPathSelectElement("//handle");
            if (xHandle == null) return "Error: XML does not contain handle";

            string sInstance = xInstance.Value;
            string sHandle = xHandle.Value;
            string sHTML = "";

            sHTML += "Task Instance: " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "instance", "") +
                " help=\"Enter a Task Instance ID.\" value=\"" + sInstance + "\" />" + Environment.NewLine;

            sHTML += "Handle: " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "handle", "") +
                " help=\"Enter a Handle.\" value=\"" + sHandle + "\" />" + Environment.NewLine;

            return sHTML;
        }
        public string CancelTask(string sStepID, string sFunction, XDocument xd)
        {
            XElement xTaskInstance = xd.XPathSelectElement("//task_instance");
            if (xTaskInstance == null) return "Error: XML does not contain task_instance";

            string sTaskInstance = xTaskInstance.Value;
            string sHTML = "";

            sHTML += "Task Instance ID(s): " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "task_instance", "w75pct") +
                " help=\"Enter one or more Task Instance IDs, separated by spaces.  Variables can be used.\" value=\"" + sTaskInstance + "\" />" + Environment.NewLine;

            return sHTML;
        }
        public string SetVariable(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";

            sHTML += "<div id=\"v" + sStepID + "_vars\">";
            sHTML += "<table border=\"0\" class=\"w99pct\" cellpadding=\"0\" cellspacing=\"0\">" + Environment.NewLine;

            IEnumerable<XElement> xPairs = xd.XPathSelectElements("//variable");
            int i = 1;
            foreach (XElement xe in xPairs)
            {
                string sValueFieldID = "";

                XElement xKey = xe.XPathSelectElement("name");
                XElement xVal = xe.XPathSelectElement("value");
                XElement xMod = xe.XPathSelectElement("modifier");

                //Trac#389 - Make sure variable names are trimmed of whitespace if it exists
                //hokey, but doing it here because the field update function is global.
                if (xKey.Value.Trim() != xKey.Value)
                    SetNodeValueinCommandXML(sStepID, "variable[" + i.ToString() + "]/name", xKey.Value.Trim());

                sHTML += "<tr>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\">&nbsp;Variable:&nbsp;</td>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\"><input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "variable[" + i.ToString() + "]/name", "") +
                    " validate_as=\"variable\"" +
                    " value=\"" + xKey.Value + "\"" +
                    " help=\"Enter a Variable name.\"" +
                    " /></td>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\">&nbsp;Value:&nbsp;</td>";

                // we gotta get the field id first, but don't show the textarea until after
                string sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "variable[" + i.ToString() + "]/value", ref sValueFieldID, "w90pct");

                sHTML += "<td class=\"w75pct\" style=\"vertical-align: bottom;\"><textarea rows=\"1\" style=\"height: 18px;\" " + sCommonAttribs +
                    " help=\"Enter a value for the Variable.\"" +
                    ">" + ui.SafeHTML(xVal.Value) + "</textarea>" + Environment.NewLine;

                //big box button
                sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                    " src=\"../images/icons/edit_16.png\"" +
                    " link_to=\"" + sValueFieldID + "\" /></td>" + Environment.NewLine;


                sHTML += "<td class=\"w1pct\">&nbsp;Modifier:&nbsp;</td>";
                sHTML += "<td class=\"w75pct\">";
                sHTML += "<select " + CommonAttribs(sStepID, sFunction, false, "variable[" + i.ToString() + "]/modifier", "") + ">" + Environment.NewLine;
                sHTML += "  <option " + SetOption("", xMod.Value) + " value=\"\">--None--</option>" + Environment.NewLine;
                sHTML += "  <option " + SetOption("TO_UPPER", xMod.Value) + " value=\"TO_UPPER\">UPPERCASE</option>" + Environment.NewLine;
                sHTML += "  <option " + SetOption("TO_LOWER", xMod.Value) + " value=\"TO_LOWER\">lowercase</option>" + Environment.NewLine;
                sHTML += "  <option " + SetOption("TO_BASE64", xMod.Value) + " value=\"TO_BASE64\">base64 encode</option>" + Environment.NewLine;
                sHTML += "  <option " + SetOption("FROM_BASE64", xMod.Value) + " value=\"FROM_BASE64\">base64 decode</option>" + Environment.NewLine;
                sHTML += "</select></td>" + Environment.NewLine;

                sHTML += "<td class=\"w1pct\"><span class=\"fn_var_remove_btn pointer\" index=\"" + i.ToString() + "\" step_id=\"" + sStepID + "\">";
                sHTML += "<img style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                    " alt=\"\" title=\"Remove\" /></span></td>";

                sHTML += "</tr>" + Environment.NewLine;

                i++;
            }

            sHTML += "</table>" + Environment.NewLine;

            sHTML += "<div class=\"fn_setvar_add_btn pointer\"" +
                " add_to_id=\"v" + sStepID + "_vars\"" +
                " step_id=\"" + sStepID + "\">" +
                "<img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\"" +
                " alt=\"\" title=\"Add another.\" />( click to add another )</div>";
            sHTML += "</div>";

            return sHTML;
        }
        public string Substring(string sStepID, string sFunction, XDocument xd)
        {
            XElement xVariable = xd.XPathSelectElement("//variable_name");
            if (xVariable == null) return "Error: XML does not contain variable_name";
            XElement xStart = xd.XPathSelectElement("//start");
            if (xStart == null) return "Error: XML does not contain start";
            XElement xEnd = xd.XPathSelectElement("//end");
            if (xEnd == null) return "Error: XML does not contain end";
            XElement xSource = xd.XPathSelectElement("//source");
            if (xSource == null) return "Error: XML does not contain source";

            string sVariable = xVariable.Value;
            string sStart = xStart.Value;
            string sEnd = xEnd.Value;
            string sSource = xSource.Value;
            string sFieldID = "";
            string sHTML = "";

            //Trac#389 - Make sure variable names are trimmed of whitespace if it exists
            //hokey, but doing it here because the field update function is global.
            if (sVariable.Trim() != sVariable)
                SetNodeValueinCommandXML(sStepID, "variable_name", sVariable.Trim());


            sHTML += "Variable:" + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "variable_name", "") +
                " validate_as=\"variable\"" +
                " help=\"Enter a Variable name.\" value=\"" + sVariable + "\" />";

            sHTML += "Start Index: " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "start", "") +
                " help=\"Start index in the search string.\" value=\"" + sStart + "\" />" + Environment.NewLine;

            sHTML += "End Index: " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "end", "") +
                " help=\"End index in the search string.\" value=\"" + sEnd + "\" /><br />" + Environment.NewLine;

            // we gotta get the field id first, but don't show the textarea until after
            string sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "source", ref sFieldID, "");

            sHTML += "Source String: " + Environment.NewLine;
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sFieldID + "\" />" + Environment.NewLine;

            sHTML += "<textarea rows=\"2\" " + sCommonAttribs +
                " help=\"Enter a string or variable.\"" +
                ">" + sSource + "</textarea>" + Environment.NewLine;

            return sHTML;
        }
        public string Loop(string sStepID, string sFunction, XDocument xd)
        {
            XElement xStart = xd.XPathSelectElement("//start");
            if (xStart == null) return "Error: XML does not contain start";
            XElement xIncrement = xd.XPathSelectElement("//increment");
            if (xIncrement == null) return "Error: XML does not contain increment";
            XElement xCounter = xd.XPathSelectElement("//counter");
            if (xCounter == null) return "Error: XML does not contain counter";
            XElement xTest = xd.XPathSelectElement("//test");
            if (xTest == null) return "Error: XML does not contain test";
            XElement xCompareTo = xd.XPathSelectElement("//compare_to");
            if (xCompareTo == null) return "Error: XML does not contain compare_to";
            XElement xMax = xd.XPathSelectElement("//max");
            if (xMax == null) return "Error: XML does not contain max";
            XElement xAction = xd.XPathSelectElement("//action");
            if (xAction == null) return "Error: XML does not contain action";

            string sStart = xStart.Value;
            string sIncrement = xIncrement.Value;
            string sCounter = xCounter.Value;
            string sTest = xTest.Value;
            string sCompareTo = xCompareTo.Value;
            string sMax = xMax.Value;
            string sAction = xAction.Value;
            string sHTML = "";


            sHTML += "Counter variable " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "counter", "") +
                " validate_as=\"variable\" help=\"Variable to increment.\" value=\"" + sCounter + "\" />" + Environment.NewLine;

            sHTML += " begins at" + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "start", "w100px");
            sHTML += " help=\"Enter an integer value where the loop will begin.\" value=\"" + sStart + "\" />" + Environment.NewLine;

            sHTML += " and increments by " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "increment", "w100px") +
                " help=\"The integer value to increment the counter after each loop.\" value=\"" + sIncrement + "\" />.<br />" + Environment.NewLine;

            sHTML += "Loop will continue while variable is " + Environment.NewLine;
            sHTML += "<select " + CommonAttribs(sStepID, sFunction, false, "test", "") + ">" + Environment.NewLine;
            sHTML += "  <option " + SetOption("==", sTest) + " value=\"==\">==</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("!=", sTest) + " value=\"!=\">!=</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("<=", sTest) + " value=\"<=\">&lt;=</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("<", sTest) + " value=\"<\">&lt;</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption(">=", sTest) + " value=\">=\">&gt;=</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption(">", sTest) + " value=\">\">&gt;</option>" + Environment.NewLine;
            sHTML += "</select>" + Environment.NewLine;
            //" + SetOption(dr["connection_type"].ToString(), sConnType) + "
            //            sHTML += "Compare To: " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "compare_to", "w400px") +
                " help=\"Loop until variable compared with this value becomes 'false'.\" value=\"" + sCompareTo + "\" />" + Environment.NewLine;


            sHTML += "<br /> or " + Environment.NewLine;
            sHTML += "<input type=\"text\" " + CommonAttribs(sStepID, sFunction, false, "max", "w50px") +
                " help=\"For safety, enter a maximum number of times to loop before aborting.\" value=\"" + sMax + "\" /> loops have occured." + Environment.NewLine;

            sHTML += "<hr />" + Environment.NewLine;

            sHTML += "Action:<br />" + Environment.NewLine;
            //enable the dropzone for the Action
            sHTML += DrawDropZone(sStepID, sAction, sFunction, "action", "", true);


            return sHTML;
        }
        public string While(string sStepID, string sFunction, XDocument xd)
        {
            XElement xTest = xd.XPathSelectElement("//test");
            if (xTest == null) return "Error: XML does not contain test";
            XElement xAction = xd.XPathSelectElement("//action");
            if (xAction == null) return "Error: XML does not contain action";

            string sTest = xTest.Value;
            string sAction = xAction.Value;
            string sHTML = "";

            sHTML += "While: " + Environment.NewLine;
            sHTML += "<td class=\"w75pct\" style=\"vertical-align: bottom;\"><textarea rows=\"10\" style=\"height: 18px;\" "
                + CommonAttribs(sStepID, sFunction, false, "test", "") + " help=\"\"" +
                ">" + ui.SafeHTML(sTest) + "</textarea>" + Environment.NewLine;


            sHTML += "<hr />Action:<br />" + Environment.NewLine;
            //enable the dropzone for the Action
            sHTML += DrawDropZone(sStepID, sAction, sFunction, "action", "", true);


            return sHTML;
        }
        public string LogMessage(string sStepID, string sFunction, XDocument xd)
        {
            XElement xMessage = xd.XPathSelectElement("//message");
            if (xMessage == null) return "Error: XML does not contain message";

            string sMessage = xMessage.Value;
            string sFieldID = "";
            string sHTML = "";

            // we gotta get the field id first, but don't show the textarea until after
            string sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "message", ref sFieldID, "");

            sHTML += "Log Message: " + Environment.NewLine;
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sFieldID + "\" /><br />" + Environment.NewLine;

            sHTML += "<textarea rows=\"2\" " + sCommonAttribs +
                " help=\"Enter a log message.\"" +
                ">" + sMessage + "</textarea>" + Environment.NewLine;

            return sHTML;
        }
        public string Scriptlet(string sStepID, string sFunction, XDocument xd)
        {
            XElement xLanguage = xd.XPathSelectElement("//language");
            if (xLanguage == null) return "Error: XML does not contain language";
            XElement xScript = xd.XPathSelectElement("//script");
            if (xScript == null) return "Error: XML does not contain script";

            string sLanguage = xLanguage.Value;
            string sScript = xScript.Value;
            string sFieldID = "";
            string sHTML = "";

            sHTML += "Language:" + Environment.NewLine;
            sHTML += "<select " + CommonAttribs(sStepID, sFunction, true, "language", "") + ">" + Environment.NewLine;
            sHTML += "  <option " + SetOption("Javascript", sLanguage) + " value=\"Javascript\">Javascript</option>" + Environment.NewLine;
            sHTML += "  <option " + SetOption("TCL", sLanguage) + " value=\"TCL\">TCL</option>" + Environment.NewLine;
            sHTML += "</select><br />" + Environment.NewLine;

            // we gotta get the field id first, but don't show the textarea until after
            string sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "script", ref sFieldID, "");

            sHTML += "Script: " + Environment.NewLine;
            //big box button
            sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                " src=\"../images/icons/edit_16.png\"" +
                " link_to=\"" + sFieldID + "\" /><br />" + Environment.NewLine;

            sHTML += "<textarea rows=\"2\" " + sCommonAttribs +
                " help=\"Enter a log message.\"" +
                ">" + sScript + "</textarea>" + Environment.NewLine;

            return sHTML;
        }
        public string BreakLoop(string sStepID, string sFunction, XDocument xd)
        {
            // the break_loop contains nothing, but every command must return something.
            return "<!---->";
        }
        #endregion

        #region "Read Only Templates"
        public string DrawStepNotes_View(string sNotes)
        {
            //this is the section that is common to all steps.
            string sHTML = "";

            sHTML += "<hr />";
            sHTML += "Notes:<br />";
            sHTML += "<div class=\"view_notes\">" + ui.FixBreaks(ui.SafeHTML(sNotes)) + "</div>";

            return sHTML;
        }
        public string DrawStepOptions_View(string sOptionHTML)
        {
            //this is the section that is common to all steps.
            string sHTML = "";

            sHTML += "<hr />";
            sHTML += "Options:<br />";
            sHTML += "<div class=\"codebox\">" + sOptionHTML + "</div>";

            return sHTML;
        }
        public string GetStepTemplate_View(DataRow dr, ref string sOptionHTML)
        {
            string sStepID = dr["step_id"].ToString();
            string sFunction = dr["function_name"].ToString();
            string sFunctionXML = dr["function_xml"].ToString();

            string sHTML = "";
            XDocument xd = XDocument.Parse(sFunctionXML);

            switch (sFunction.ToLower())
            {
                //we put the most commonly used ones at the top

                /*
                NOTE: If you are adding a new command type, be aware that 
                you will need to modify the code in taskMethods for the wmAddStep function.
                         
                IF the command populates variables, it will need a case statement added to that function
                to ensure the output_parse_type field is properly set.
                */
                case "if":
                    sHTML = If_View(sStepID, sFunction, xd);
                    break;
                case "loop":
                    sHTML = Loop_View(sStepID, sFunction, xd);
                    break;
                case "new_connection":
                    sHTML = NewConnection_View(sStepID, sFunction, xd);
                    break;
                case "drop_connection":
                    sHTML = DropConnection_View(sStepID, sFunction, xd);
                    break;
                case "sql_exec":
                    sHTML = SqlExec_View(sStepID, sFunction, xd, dr);
                    break;
                case "cmd_line":
                    sHTML = CmdLine_View(sStepID, sFunction, xd, dr, ref sOptionHTML);
                    break;
                case "dos_cmd":
                    sHTML = DOSCmd_View(sStepID, sFunction, xd, dr, ref sOptionHTML);
                    break;
                case "set_variable":
                    sHTML = SetVariable_View(sStepID, sFunction, xd);
                    break;
                case "parse_text":
                    sHTML = ParseText_View(sStepID, sFunction, xd, dr);
                    break;
                case "read_file":
                    sHTML = ReadFile_View(sStepID, sFunction, xd, dr);
                    break;
                case "clear_variable":
                    sHTML = ClearVariable_View(sStepID, sFunction, xd);
                    break;
                case "wait_for_tasks":
                    sHTML = WaitForTasks_View(sStepID, sFunction, xd);
                    break;
                case "codeblock":
                    sHTML = Codeblock_View(sStepID, sFunction, xd);
                    break;
                case "log_msg":
                    sHTML = LogMessage_View(sStepID, sFunction, xd);
                    break;
                case "end":
                    sHTML = End_View(sStepID, sFunction, xd);
                    break;
                case "dataset":
                    sHTML = Dataset_View(sStepID, sFunction, xd);
                    break;
                case "http":
                    sHTML = HTTP_View(sStepID, sFunction, xd, dr);
                    break;
                case "win_cmd":
                    sHTML = WinCmd_View(sStepID, sFunction, xd, dr);
                    break;
                case "subtask":
                    sHTML = Subtask_View(sStepID, sFunction, xd);
                    break;
                case "run_task":
                    sHTML = RunTask_View(sStepID, sFunction, xd);
                    break;
                case "set_asset_registry":
                    sHTML = SetAssetRegistry_View(sStepID, sFunction, xd);
                    break;
                case "set_task_registry":
                    sHTML = SetTaskRegistry_View(sStepID, sFunction, xd);
                    break;
                case "sleep":
                    sHTML = Sleep_View(sStepID, sFunction, xd);
                    break;
                case "set_debug_level":
                    sHTML = SetDebugLevel_View(sStepID, sFunction, xd);
                    break;
                case "get_instance_handle":
                    sHTML = GetTaskInstance_View(sStepID, sFunction, xd);
                    break;
                case "cancel_task":
                    sHTML = CancelTask_View(sStepID, sFunction, xd);
                    break;
                case "substring":
                    sHTML = Substring_View(sStepID, sFunction, xd);
                    break;
                case "transfer":
                    sHTML = Transfer_View(sStepID, sFunction, xd);
                    break;
                case "scriptlet":
                    sHTML = Scriptlet_View(sStepID, sFunction, xd);
                    break;
                case "question":
                    sHTML = Question_View(sStepID, sFunction, xd);
                    break;
                case "break_loop":
                    sHTML = BreakLoop_View(sStepID, sFunction, xd);
                    break;
                case "exists":
                    sHTML = Exists_View(sStepID, sFunction, xd);
                    break;
                case "while":
                    sHTML = While_View(sStepID, sFunction, xd);
                    break;

                default:
                    //OK, here's some fun.  We didn't find one of our built in commands.  Perhaps it was something else?
                    sHTML = GetCustomStepTemplate_View(sStepID, sFunction, xd, dr, ref sOptionHTML);
                    break;
            }


            if (sHTML == "")
                sHTML = "Error: unable to render view form for function type: [" + sFunction + "].";

            return sHTML;
        }

        public string SetTaskRegistry_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xXPath = xd.XPathSelectElement("//xpath");
            if (xXPath == null) return "Error: XML does not contain xpath";
            XElement xValue = xd.XPathSelectElement("//value");
            if (xValue == null) return "Error: XML does not contain value";
            XElement xCreateNode = xd.XPathSelectElement("//create_node");
            if (xCreateNode == null) return "Error: XML does not contain create_node";

            string sXPath = ui.SafeHTML(xXPath.Value);
            string sValue = ui.SafeHTML(xValue.Value);
            //string sCreateNode = (xCreateNode.Value == "1" ? "Yes" : "No");
            string sHTML = "";

            sHTML += "Key: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sXPath + "</div>" + Environment.NewLine;

            //sHTML += "Create If Missing? <br />" + Environment.NewLine;
            //sHTML += "<div class=\"codebox\">" + sCreateNode + "</div>" + Environment.NewLine;

            sHTML += "Value: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sValue + "</div>" + Environment.NewLine;

            return sHTML;
        }

        public string SetAssetRegistry_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xAsset = xd.XPathSelectElement("//asset_id");
            if (xAsset == null) return "Error: XML does not contain asset_id";
            XElement xXPath = xd.XPathSelectElement("//xpath");
            if (xXPath == null) return "Error: XML does not contain xpath";
            XElement xValue = xd.XPathSelectElement("//value");
            if (xValue == null) return "Error: XML does not contain value";
            XElement xCreateNode = xd.XPathSelectElement("//create_node");
            if (xCreateNode == null) return "Error: XML does not contain create_node";

            string sAsset = xAsset.Value;
            string sXPath = ui.SafeHTML(xXPath.Value);
            string sValue = ui.SafeHTML(xValue.Value);
            //string sCreateNode = (xCreateNode.Value == "1" ? "Yes" : "No");
            string sAssetName = "";
            string sHTML = "";
            string sErr = "";

            //ASSET
            if (ui.IsGUID(sAsset))
            {
                string sSQL = "select asset_name from asset where asset_id = '" + sAsset + "'";

                if (!dc.sqlGetSingleString(ref sAssetName, sSQL, ref sErr))
                    return "Error retrieving Asset name.<br />" + sErr;

                if (sAssetName == "")
                    return "Unable to find Asset by ID - [" + sAsset + "]." + sErr;
            }
            else
            {
                sAssetName = sAsset;
            }

            sHTML += "Asset: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">Asset: [" + sAssetName + "]" + "</div>" + Environment.NewLine;

            sHTML += "Key: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sXPath + "</div>" + Environment.NewLine;

            //sHTML += "Create If Missing? <br />" + Environment.NewLine;
            //sHTML += "<div class=\"codebox\">" + sCreateNode + "</div>" + Environment.NewLine;

            sHTML += "Value: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sValue + "</div>" + Environment.NewLine;

            return sHTML;
        }

        public string Transfer_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xFromAsset = xd.XPathSelectElement("//from_asset");
            if (xFromAsset == null) return "Error: XML does not contain from_asset";
            XElement xFromFile = xd.XPathSelectElement("//from_file");
            if (xFromFile == null) return "Error: XML does not contain from_file";
            XElement xToAsset = xd.XPathSelectElement("//to_asset");
            if (xToAsset == null) return "Error: XML does not contain to_asset";
            XElement xToFile = xd.XPathSelectElement("//to_file");
            if (xToFile == null) return "Error: XML does not contain to_file";
            XElement xCommand = xd.XPathSelectElement("//command");
            if (xCommand == null) return "Error: XML does not contain command.";
            XElement xMode = xd.XPathSelectElement("//mode");
            if (xMode == null) return "Error: XML does not contain mode.";

            string sFromAsset = xFromAsset.Value;
            string sFromFile = ui.SafeHTML(xFromFile.Value);
            string sToAsset = xToAsset.Value;
            string sToFile = ui.SafeHTML(xToFile.Value);
            string sMode = ui.SafeHTML(xMode.Value);
            string sCommand = ui.SafeHTML(xCommand.Value);
            string sFromAssetName = "";
            string sToAssetName = "";
            string sHTML = "";
            string sErr = "";

            //FROM ASSET
            if (ui.IsGUID(sFromAsset))
            {
                string sSQL = "select asset_name from asset where asset_id = '" + sFromAsset + "'";

                if (!dc.sqlGetSingleString(ref sFromAssetName, sSQL, ref sErr))
                    return "Error retrieving 'From' Asset.<br />" + sErr;

                if (sFromAssetName == "")
                    return "Unable to find 'From' Asset by ID - [" + sFromAsset + "]." + sErr;
            }
            else
            {
                sFromAssetName = sFromAsset;
            }

            //TO ASSET
            if (ui.IsGUID(sToAsset))
            {
                string sSQL = "select asset_name from asset where asset_id = '" + sToAsset + "'";

                if (!dc.sqlGetSingleString(ref sToAssetName, sSQL, ref sErr))
                    return "Error retrieving 'To' Asset.<br />" + sErr;

                if (sToAssetName == "")
                    return "Unable to find 'To' Asset by ID - [" + sToAsset + "]." + sErr;
            }
            else
            {
                sToAssetName = sToAsset;
            }

            sHTML += "Mode: <span class=\"code\">" + sMode + "</span><br />" + Environment.NewLine;
            if (sMode == "FTP" || sMode == "SFTP")
                sHTML += "Command: <span class=\"code\">" + sCommand + "</span><br />" + Environment.NewLine;

            sHTML += "From: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">Asset: [" + sFromAssetName + "]";
            if (sCommand != "mput" && sCommand != "mget")
                sHTML += " File: [" + sFromFile + "]</div>" + Environment.NewLine;

            sHTML += "To: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">Asset: [" + sToAssetName + "] File: [" + sToFile + "]</div>" + Environment.NewLine;

            return sHTML;
        }
        public string ReadFile_View(string sStepID, string sFunction, XDocument xd, DataRow dr)
        {
            XElement xFileName = xd.XPathSelectElement("//filename");
            if (xFileName == null) return "Error: XML does not contain filename";
            XElement xStart = xd.XPathSelectElement("//start");
            if (xStart == null) return "Error: XML does not contain start.";
            XElement xNumChars = xd.XPathSelectElement("//num_chars");
            if (xNumChars == null) return "Error: XML does not contain num_chars.";

            string sFileName = xFileName.Value;
            string sStart = xStart.Value;
            string sNumChars = xNumChars.Value;
            string sHTML = "";

            sHTML += "<table width=\"99%\" border=\"0\">" + Environment.NewLine;

            sHTML += "<tr><td class=\"w1pct\">File&nbsp;Name:&nbsp;</td>" + Environment.NewLine;
            sHTML += "<td><span class=\"code\">" + sFileName + "</span>></td></tr>" + Environment.NewLine;

            sHTML += "<tr><td class=\"w1pct\">Starting&nbsp;Position:&nbsp;</td>" + Environment.NewLine;
            sHTML += "<td><span class=\"code\">" + sStart + "</span></td></tr>" + Environment.NewLine;

            sHTML += "<tr><td class=\"w1pct\">Number&nbsp;of&nbsp;Characters:&nbsp;</td>" + Environment.NewLine;
            sHTML += "<td><span class=\"code\">" + sNumChars + "</span></td></tr>" + Environment.NewLine;

            sHTML += "</table>" + Environment.NewLine;

            //variables
            sHTML += DrawVariableSectionForDisplay(dr, false);

            return sHTML;
        }
        public string ParseText_View(string sStepID, string sFunction, XDocument xd, DataRow dr)
        {
            XElement xText = xd.XPathSelectElement("//text");
            if (xText == null) return "Error: XML does not contain 'text' field.";

            string sCommand = ui.SafeHTML(xText.Value);
            string sHTML = "";

            sHTML += "Text to Process:<br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sCommand + "</div>";

            //variables
            sHTML += DrawVariableSectionForDisplay(dr, false);

            return sHTML;
        }
        public string RunTask_View(string sStepID, string sFunction, XDocument xd)
        {
            string sOriginalTaskID = "";
            string sVersion = "";
            string sTime = "";
            //string sOnSuccess = "";
            //string sOnError = "";
            string sHandle = "";
            string sAssetID = "";
            string sAssetName = "";
            string sLabel = "";
            string sHTML = "";
            string sErr = "";

            XElement xOTID = xd.XPathSelectElement("//original_task_id");
            if (xOTID == null) return "Error: XML does not contain original_task_id";
            sOriginalTaskID = ui.SafeHTML(xOTID.Value);

            XElement xVer = xd.XPathSelectElement("//version");
            if (xVer == null) return "Error: XML does not contain version";
            sVersion = ui.SafeHTML(xVer.Value);

            XElement xTime = xd.XPathSelectElement("//time_to_wait");
            if (xTime == null) return "Error: XML does not contain time_to_wait";
            sTime = ui.SafeHTML(xTime.Value);

            //XElement xSuccess = xd.XPathSelectElement("//on_success");
            //if (xSuccess == null) return "Error: XML does not contain on_success";
            //sOnSuccess = ui.SafeHTML(xSuccess.Value);

            //XElement xError = xd.XPathSelectElement("//on_error");
            //if (xError == null) return "Error: XML does not contain on_error";
            //sOnError = ui.SafeHTML(xError.Value);

            XElement xHandle = xd.XPathSelectElement("//handle");
            if (xHandle == null) return "Error: XML does not contain handle";
            sHandle = ui.SafeHTML(xHandle.Value);

            //get the name and code for belonging to this otid and version
            if (ui.IsGUID(sOriginalTaskID))
            {
                string sSQL = "select task_code, task_name from task" +
                    " where original_task_id = '" + sOriginalTaskID + "'" +
                    (sVersion == "" ? " and default_version = 1" : " and version = '" + sVersion + "'");

                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                {
                    return "Error retrieving subtask.<br />" + sErr;
                }

                if (dr != null)
                {
                    sLabel = ui.SafeHTML(dr["task_code"].ToString() + " : " + dr["task_name"].ToString());
                }
                else
                {
                    return "Unable to find task [" + sOriginalTaskID + "] version [" + sVersion + "]." + sErr;
                }
            }

            XElement xAsset = xd.XPathSelectElement("//asset_id");
            if (xAsset == null) return "Error: XML does not contain asset_id";
            sAssetID = xAsset.Value;

            //get the asset name belonging to this asset_id
            if (ui.IsGUID(sAssetID))
            {
                string sSQL = "select asset_name from asset where asset_id = '" + sAssetID + "'";

                if (!dc.sqlGetSingleString(ref sAssetName, sSQL, ref sErr))
                    return "Error retrieving Asset Name.<br />" + sErr;

                if (sAssetName == "")
                    return "Unable to find Asset by ID - [" + sAssetID + "]." + sErr;
            }
            else
            {
                sAssetName = sAssetID;
            }

            sHTML += "Task: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sLabel + "</span>" + Environment.NewLine;
            sHTML += "<br />";
            sHTML += "Version: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + (sVersion == "" ? "Default" : sVersion) + "</span>" + Environment.NewLine;

            sHTML += "<br />";
            sHTML += "Asset: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sAssetName + "</span>" + Environment.NewLine;

            sHTML += "<br />";
            sHTML += "Task Handle: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sHandle + "</span>" + Environment.NewLine;

            sHTML += "<br />";
            sHTML += "Time to Wait: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sTime + "</span>" + Environment.NewLine;

            //sHTML += "<br />";
            //sHTML += "The following Command will be executed on Success:<br />" + Environment.NewLine;
            ////TODO: may wanna draw a box around this to further identify it's embeddedness
            //if (ui.IsGUID(sOnSuccess))
            //{
            //    DataRow dr = GetSingleStep(sOnSuccess, sUserID);
            //    sHTML += DrawReadOnlyStep(dr, bShowNotes);
            //}
            //else
            //{
            //    sHTML += "<div class=\"no_step\">No Success action defined.</div>";
            //}

            //sHTML += "The following Command will be executed on Error:<br />" + Environment.NewLine;
            //if (ui.IsGUID(sOnError))
            //{
            //    DataRow dr = GetSingleStep(sOnError, sUserID);
            //    sHTML += DrawReadOnlyStep(dr, bShowNotes);
            //}
            //else
            //{
            //    sHTML += "<div class=\"no_step\">No Error action defined.</div>";
            //}

            //key/value pairs
            sHTML += "<hr />";
            sHTML += "Parameters:";
            sHTML += DrawKeyValueSection_View(sStepID, sFunction, xd, "Input", "Value");

            return sHTML;
        }
        public string End_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xStatus = xd.XPathSelectElement("//status");
            if (xStatus == null) return "Error: XML does not contain status";
            XElement xMsg = xd.XPathSelectElement("//message");
            if (xMsg == null) return "Error: XML does not contain message";

            string sStatus = ui.SafeHTML(xStatus.Value);
            string sMsg = ui.SafeHTML(xMsg.Value);

            string sHTML = "";

            sHTML += "End with Status:" + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sStatus + "</span><br />" + Environment.NewLine;

            sHTML += "Log Message: <br />" + Environment.NewLine;

            if (string.IsNullOrEmpty(sMsg))
                sMsg = "Message not provided.";

            sHTML += "<div class=\"codebox\">" + sMsg + "</div>" + Environment.NewLine;

            return sHTML;
        }
        public string ClearVariable_View(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";

            sHTML += "<div id=\"v" + sStepID + "_vars\">";
            sHTML += "Variables to Clear:<br />" + Environment.NewLine;

            IEnumerable<XElement> xPairs = xd.XPathSelectElements("//variable");
            foreach (XElement xe in xPairs)
            {
                XElement xKey = xe.XPathSelectElement("name");

                sHTML += "<span class=\"code\">" + ui.SafeHTML(xKey.Value) + "</span>" + Environment.NewLine;
            }

            sHTML += "</div>";

            return sHTML;
        }
        public string WaitForTasks_View(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";

            sHTML += "<div id=\"v" + sStepID + "_handles\">";
            sHTML += "Task Handles:<br />" + Environment.NewLine;

            IEnumerable<XElement> xPairs = xd.XPathSelectElements("//handle");
            foreach (XElement xe in xPairs)
            {
                XElement xKey = xe.XPathSelectElement("name");

                sHTML += "<span class=\"code\">" + ui.SafeHTML(xKey.Value) + "</span>" + Environment.NewLine;
            }

            sHTML += "</div>";

            return sHTML;
        }
        public string Dataset_View(string sStepID, string sFunction, XDocument xd)
        {
            return DrawKeyValueSection_View(sStepID, sFunction, xd, "Key", "Value");
        }
        public string Exists_View(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";
            string sUserID = ui.GetSessionUserID();
            string sErr = "";

            sHTML += "<div id=\"v" + sStepID + "_vars\">";
            sHTML += "Variables to Test:<br />" + Environment.NewLine;

            IEnumerable<XElement> xPairs = xd.XPathSelectElements("//variable");
            foreach (XElement xe in xPairs)
            {
                XElement xKey = xe.XPathSelectElement("name");
                XElement xIsTrue = xe.XPathSelectElement("is_true");
                if (xIsTrue.Value == "1")
                {
                    sHTML += "<span class=\"code\">" + ui.SafeHTML(xKey.Value) + " (IsTrue) </span>" + Environment.NewLine;
                }
                else
                {
                    sHTML += "<span class=\"code\">" + ui.SafeHTML(xKey.Value) + "</span>" + Environment.NewLine;
                };

            }

            // Exists have a Positive and Negative action
            sHTML += "<br /><br />Positive Action:<br />" + Environment.NewLine;
            XElement xPositiveAction = xd.XPathSelectElement("//actions/positive_action");
            if (xPositiveAction == null) return "Error: XML does not contain positive_action";

            if (ui.IsGUID(xPositiveAction.Value))
            {
                DataRow dr = GetSingleStep(xPositiveAction.Value, sUserID, ref sErr);
                if (dr != null && sErr == "")
                    sHTML += DrawEmbeddedReadOnlyStep(dr, bShowNotes);
                else
                    sHTML += "<span class=\"red_text\">" + sErr + "</span>";
            }
            else
            {
                sHTML += "<div class=\"no_step\">No 'Positive Action' defined.</div>";
            }


            sHTML += "Negative Action:<br />" + Environment.NewLine;
            XElement xNegativeAction = xd.XPathSelectElement("//actions/negative_action");
            if (xNegativeAction == null) return "Error: XML does not contain negative_action";

            if (ui.IsGUID(xNegativeAction.Value))
            {
                DataRow dr = GetSingleStep(xNegativeAction.Value, sUserID, ref sErr);
                if (dr != null && sErr == "")
                    sHTML += DrawEmbeddedReadOnlyStep(dr, bShowNotes);
                else
                    sHTML += "<span class=\"red_text\">" + sErr + "</span>";
            }
            else
            {
                sHTML += "<div class=\"no_step\">No 'Negative Action' defined.</div>";
            }



            sHTML += "</div>";

            return sHTML;
        }
        public string If_View(string sStepID, string sFunction, XDocument xd)
        {
            string sUserID = ui.GetSessionUserID();
            string sErr = "";
            string sHTML = "";

            IEnumerable<XElement> xTests = xd.XPathSelectElements("//tests/test");
            sHTML += "<div id=\"if_" + sStepID + "_conditions\" number=\"" + xTests.Count().ToString() + "\">";

            int i = 1; //because XPath starts at "1"

            foreach (XElement xTest in xTests)
            {
                XElement xEval = xTest.XPathSelectElement("eval");
                XElement xAction = xTest.XPathSelectElement("action");
                //a way to delete the section you just added
                if (i == 1)
                {
                    sHTML += "If:<br />";
                }
                else
                {
                    sHTML += "Else If:<br />";
                }

                string sCol = "tests/test[" + i.ToString() + "]/eval";

                if (xEval != null)
                    sHTML += "<div class=\"codebox\">" + ui.SafeHTML(xEval.Value) + "</div>" + Environment.NewLine;
                else
                    sHTML += "ERROR: Malformed XML for Step ID [" + sStepID + "].  Missing '" + sCol + "' element.";


                //here's the embedded content
                sCol = "tests/test[" + i.ToString() + "]/action";

                if (xAction != null)
                {
                    if (ui.IsGUID(xAction.Value))
                    {
                        DataRow dr = GetSingleStep(xAction.Value, sUserID, ref sErr);
                        if (dr != null && sErr == "")
                            sHTML += DrawEmbeddedReadOnlyStep(dr, bShowNotes);
                        else
                            sHTML += "<span class=\"red_text\">" + sErr + "</span>";
                    }
                    else
                    {
                        sHTML += "<div class=\"no_step\">No 'Else If' action defined.</div>";
                    }
                }
                else
                    sHTML += "ERROR: Malformed XML for Step ID [" + sStepID + "].  Missing '" + sCol + "' element.";


                if (i != 1)
                    sHTML += "</div>";

                i++;
            }

            sHTML += "</div>";
            //all done embedded content


            sHTML += "<div>";

            //the final 'else' area
            XElement xElse = xd.XPathSelectElement("//else");
            if (xElse != null)
            {
                sHTML += "Else (no 'If' conditions matched):";
                if (ui.IsGUID(xElse.Value))
                {
                    DataRow dr = GetSingleStep(xElse.Value, sUserID, ref sErr);
                    if (dr != null && sErr == "")
                        sHTML += DrawReadOnlyStep(dr, bShowNotes);
                    else
                        sHTML += "<span class=\"red_text\">" + sErr + "</span>";
                }
                else
                {
                    sHTML += "<div class=\"no_step\">No 'Else' action defined.</div>";
                }
            }

            sHTML += "</div>";


            //The End
            return sHTML;
        }
        public string SqlExec_View(string sStepID, string sFunction, XDocument xd, DataRow dr)
        {
            XElement xCommand = xd.XPathSelectElement("//sql");
            if (xCommand == null) return "Error: XML does not contain sql.";
            XElement xConnName = xd.XPathSelectElement("//conn_name");
            if (xConnName == null) return "Error: XML does not contain conn_name.";
            XElement xMode = xd.XPathSelectElement("//mode");
            if (xMode == null) return "Error: XML does not contain mode.";
            XElement xHandle = xd.XPathSelectElement("//handle");
            if (xHandle == null) return "Error: XML does not contain handle.";

            string sCommand = ui.SafeHTML(xCommand.Value);
            string sConnName = ui.SafeHTML(xConnName.Value);
            string sMode = ui.SafeHTML(xMode.Value);
            string sHandle = ui.SafeHTML(xHandle.Value);
            bool bDrawVarSection = false;
            bool bDrawSQLBox = false;
            bool bDrawHandle = false;
            bool bDrawKeyValSection = false;
            string sHTML = "";

            sHTML += "Connection:" + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sConnName + "</span>" + Environment.NewLine;
            sHTML += "Mode:" + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sMode + "</span>" + Environment.NewLine;

            if (sMode == "BEGIN" || sMode == "COMMIT" || sMode == "ROLLBACK")
            {
            }
            else if (sMode == "PREPARE")
            {
                bDrawSQLBox = true;
                bDrawHandle = true;
            }
            else if (sMode == "RUN")
            {
                bDrawVarSection = true;
                bDrawHandle = true;
                bDrawKeyValSection = true;
            }
            else
            {
                bDrawVarSection = true;
                bDrawSQLBox = true;
            }

            if (bDrawHandle)
            {
                sHTML += "Handle: <span  class=\"code\">" + sHandle + "</span>";
            }

            if (bDrawKeyValSection)
                sHTML += DrawKeyValueSection(sStepID, sFunction, xd, false, false, "Bind", "Value");

            if (bDrawSQLBox)
            {
                sHTML += "<br />SQL:<br />" + Environment.NewLine;
                sHTML += "<div class=\"codebox\">" + sCommand + "</div>" + Environment.NewLine;
            }

            //variables
            if (bDrawVarSection)
                sHTML += DrawVariableSectionForDisplay(dr, false);

            return sHTML;
        }
        public string CmdLine_View(string sStepID, string sFunction, XDocument xd, DataRow dr, ref string sOptionHTML)
        {
            XElement xCommand = xd.XPathSelectElement("//command");
            if (xCommand == null) return "Error: XML does not contain command.";
            XElement xConnName = xd.XPathSelectElement("//conn_name");
            if (xConnName == null) return "Error: XML does not contain conn_name.";

            string sCommand = ui.SafeHTML(xCommand.Value);
            string sConnName = ui.SafeHTML(xConnName.Value);
            string sHTML = "";

            sHTML += "Connection:" + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sConnName + "</span><br />" + Environment.NewLine;

            sHTML += "Command:<br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sCommand + "</div>" + Environment.NewLine;

            //variables
            sHTML += DrawVariableSectionForDisplay(dr, false);

            //for the options panel
            XElement xTimeout = xd.XPathSelectElement("//timeout");
            if (xTimeout == null) return "Error: XML does not contain timeout.";
            XElement xPos = xd.XPathSelectElement("//positive_response");
            if (xPos == null) return "Error: XML does not contain positive_response.";
            XElement xNeg = xd.XPathSelectElement("//negative_response");
            if (xNeg == null) return "Error: XML does not contain negative_response.";

            string sTimeout = ui.SafeHTML(xTimeout.Value);
            string sPos = ui.SafeHTML(xPos.Value);
            string sNeg = ui.SafeHTML(xNeg.Value);

            sOptionHTML += "Timeout: <span class=\"code\">" + sTimeout + "</span><br />";
            sOptionHTML += "Positive Response: <span class=\"code\">" + sPos + "</span><br />";
            sOptionHTML += "Negative Response: <span class=\"code\">" + sNeg + "</span>";

            return sHTML;
        }
        public string DOSCmd_View(string sStepID, string sFunction, XDocument xd, DataRow dr, ref string sOptionHTML)
        {
            XElement xCommand = xd.XPathSelectElement("//command");
            if (xCommand == null) return "Error: XML does not contain command.";

            string sCommand = ui.SafeHTML(xCommand.Value);
            string sHTML = "";

            sHTML += "Command:<br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sCommand + "</div>" + Environment.NewLine;

            //variables
            sHTML += DrawVariableSectionForDisplay(dr, false);

            //for the options panel
            XElement xTimeout = xd.XPathSelectElement("//timeout");
            if (xTimeout == null) return "Error: XML does not contain timeout.";
            XElement xPos = xd.XPathSelectElement("//positive_response");
            if (xPos == null) return "Error: XML does not contain positive_response.";
            XElement xNeg = xd.XPathSelectElement("//negative_response");
            if (xNeg == null) return "Error: XML does not contain negative_response.";

            string sTimeout = ui.SafeHTML(xTimeout.Value);
            string sPos = ui.SafeHTML(xPos.Value);
            string sNeg = ui.SafeHTML(xNeg.Value);

            sOptionHTML += "Timeout: <span class=\"code\">" + sTimeout + "</span><br />";
            sOptionHTML += "Positive Response: <span class=\"code\">" + sPos + "</span><br />";
            sOptionHTML += "Negative Response: <span class=\"code\">" + sNeg + "</span>";

            return sHTML;
        }
        public string Question_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xInstructions = xd.XPathSelectElement("//instructions");
            if (xInstructions == null) return "Error: XML does not contain instructions.";
            XElement xQuestion = xd.XPathSelectElement("//question");
            if (xQuestion == null) return "Error: XML does not contain question.";
            XElement xER = xd.XPathSelectElement("//evidence_requirements");
            if (xER == null) return "Error: XML does not contain evidence_requirements.";

            string sInstructions = ui.SafeHTML(xInstructions.Value);
            string sQuestion = ui.SafeHTML(xQuestion.Value);
            string sER = ui.SafeHTML(xER.Value);
            string sHTML = "";

            sHTML += "Instructions:<br />";
            sHTML += "<span class=\"code\">" + sInstructions + "</span>" + Environment.NewLine;

            sHTML += "Question:<br />";
            sHTML += "<span class=\"code\">" + sQuestion + "</span>" + Environment.NewLine;

            sHTML += "Evidence Requirements:<br />";
            sHTML += "<span class=\"code\">" + sER + "</span>" + Environment.NewLine;

            return sHTML;
        }
        public string NewConnection_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xAsset = xd.XPathSelectElement("//asset");
            if (xAsset == null) return "Error: XML does not contain asset";
            XElement xConnName = xd.XPathSelectElement("//conn_name");
            if (xConnName == null) return "Error: XML does not contain conn_name";
            XElement xConnType = xd.XPathSelectElement("//conn_type");
            if (xConnType == null) return "Error: XML does not contain conn_type";

            string sAsset = xAsset.Value;
            string sConnName = ui.SafeHTML(xConnName.Value);
            string sConnType = ui.SafeHTML(xConnType.Value);

            string sAssetName = "";
            string sHTML = "";
            string sErr = "";

            //get the asset name belonging to this asset_id
            if (ui.IsGUID(sAsset))
            {
                string sSQL = "select asset_name from asset where asset_id = '" + sAsset + "'";

                if (!dc.sqlGetSingleString(ref sAssetName, sSQL, ref sErr))
                    return "Error retrieving Asset Name.<br />" + sErr;

                if (sAssetName == "")
                    return "Unable to find Asset by ID - [" + sAsset + "]." + sErr;
            }
            else
            {
                sAssetName = sAsset;
            }

            sHTML += "Connect via: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sConnType + "</span>" + Environment.NewLine;
            sHTML += " to Asset " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sAssetName + "</span>" + Environment.NewLine;
            sHTML += " as " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sConnName + "</span>" + Environment.NewLine;

            return sHTML;
        }
        public string DropConnection_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xConnName = xd.XPathSelectElement("//conn_name");
            if (xConnName == null) return "Error: XML does not contain conn_name";

            string sConnName = ui.SafeHTML(xConnName.Value);

            string sHTML = "";

            sHTML += "Connection Name: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sConnName + "</span>" + Environment.NewLine;

            return sHTML;
        }
        public string HTTP_View(string sStepID, string sFunction, XDocument xd, DataRow dr)
        {
            XElement xType = xd.XPathSelectElement("//type");
            if (xType == null) return "Error: XML does not contain type";
            XElement xURL = xd.XPathSelectElement("//url");
            if (xURL == null) return "Error: XML does not contain url";

            string sType = ui.SafeHTML(xType.Value);
            string sURL = ui.SafeHTML(xURL.Value);
            string sHTML = "";

            sHTML += "Request Type: <span class=\"code\">" + sType + "</span> <br />" + Environment.NewLine;

            sHTML += "URL: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sURL + "</div>" + Environment.NewLine;

            //key/value pairs
            //note: commented these out per bug 1176
            // until HTTP post is enabled
            //key/value pairs
            //sHTML += "<hr />";
            //sHTML += "Parameters:";
            //sHTML += DrawKeyValueSection_View(sStepID, sFunction, xd, "Key", "Value"); ;

            //variables
            sHTML += DrawVariableSectionForDisplay(dr, false);

            return sHTML;
        }
        public string WinCmd_View(string sStepID, string sFunction, XDocument xd, DataRow dr)
        {
            XElement xConnName = xd.XPathSelectElement("//conn_name");
            if (xConnName == null) return "Error: XML does not contain conn_name.";
            XElement xType = xd.XPathSelectElement("//type");
            if (xType == null) return "Error: XML does not contain type.";
            XElement xCmd = xd.XPathSelectElement("//command");
            if (xCmd == null) return "Error: XML does not contain command.";

            string sConnName = ui.SafeHTML(xConnName.Value);
            string sType = xType.Value;
            string sCommand = ui.SafeHTML(xCmd.Value);

            string sHTML = "";

            sHTML += "Connection Name: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sConnName + "</span><br />" + Environment.NewLine;

            sHTML += "Command Type:" + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sType + "</span><br />" + Environment.NewLine;



            //go ahead and get the parameters here before the switch
            XElement xP0 = xd.XPathSelectElement("//parameter_0");
            if (xP0 == null) return "Error: XML does not contain parameter_0.";
            XElement xP1 = xd.XPathSelectElement("//parameter_1");
            if (xP1 == null) return "Error: XML does not contain parameter_1.";
            //XElement xP2 = xd.XPathSelectElement("//parameter_2");
            //if (xP2 == null) return "Error: XML does not contain parameter_2.";
            //XElement xP3 = xd.XPathSelectElement("//parameter_3");
            //if (xP3 == null) return "Error: XML does not contain parameter_3.";
            //XElement xP4 = xd.XPathSelectElement("//parameter_4");
            //if (xP4 == null) return "Error: XML does not contain parameter_4.";

            string sP0 = ui.SafeHTML(xP0.Value);
            string sP1 = ui.SafeHTML(xP1.Value);
            //string sP2 = ui.SafeHTML(xP2.Value);
            //string sP3 = ui.SafeHTML(xP3.Value);
            //string sP4 = ui.SafeHTML(xP4.Value);

            //which option to show depends on the command type
            //wmi only has one box
            switch (sType)
            {
                case "WMI":
                    sHTML += "Namespace: " + Environment.NewLine;
                    sHTML += "<span class=\"code\">" + sP1 + "</span><br />" + Environment.NewLine;

                    sHTML += "<br />Command: <br />" + Environment.NewLine;
                    sHTML += "<div class=\"codebox\">" + sP0 + "</div>" + Environment.NewLine;

                    break;
                case "Registry":
                    sHTML += "Command:" + Environment.NewLine;
                    sHTML += "<span class=\"code\">" + sCommand + "</span><br />" + Environment.NewLine;


                    //fields depend on the command
                    switch (sCommand)
                    {
                        case "Get Keys":
                            sHTML += "Registry Path: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP0 + "</span><br />" + Environment.NewLine;

                            break;
                        case "Get Key Values":
                            sHTML += "Registry Path: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP0 + "</span><br />" + Environment.NewLine;

                            break;
                        case "Get Values":
                            sHTML += "Registry Path: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP0 + "</span><br />" + Environment.NewLine;

                            sHTML += "Key: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP1 + "</span><br />" + Environment.NewLine;

                            break;
                        default:
                            break;
                    }

                    break;
                case "Security":
                    sHTML += "Command:" + Environment.NewLine;
                    sHTML += "<span class=\"code\">" + sCommand + "</span><br />" + Environment.NewLine;


                    //fields depend on the command
                    switch (sCommand)
                    {
                        case "Get User Properties":
                            sHTML += "User Name: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP0 + "</span><br />" + Environment.NewLine;
                            break;
                        case "Get User Rights":
                            sHTML += "User Name: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP0 + "</span><br />" + Environment.NewLine;
                            break;
                        case "Get User Groups":
                            sHTML += "User Name: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP0 + "</span><br />" + Environment.NewLine;
                            break;
                        case "Get Local Group Members":
                            sHTML += "Group Name: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP0 + "</span><br />" + Environment.NewLine;
                            break;
                        case "Get Group Rights":
                            sHTML += "Group Name: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP0 + "</span><br />" + Environment.NewLine;
                            break;
                        case "Get Group Properties":
                            sHTML += "Group Name: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP0 + "</span><br />" + Environment.NewLine;
                            break;
                        case "Get Users with Right":
                            sHTML += "Right: " + Environment.NewLine;
                            sHTML += "<span class=\"code\">" + sP0 + "</span><br />" + Environment.NewLine;
                            break;
                        default:
                            break;
                    }

                    break;
                default:
                    break;
            }

            //variables
            sHTML += DrawVariableSectionForDisplay(dr, false);

            return sHTML;
        }
        public string Codeblock_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xCB = xd.XPathSelectElement("//codeblock");
            if (xCB == null) return "Error: XML does not contain codeblock";

            string sCB = ui.SafeHTML(xCB.Value);
            string sHTML = "";

            sHTML += "Codeblock: " + Environment.NewLine;
            sHTML += "<span class=\"code pointer\" onclick=\"jumpToAnchor('cbt_" + sCB + "');\">" + sCB + "</span>" + Environment.NewLine;

            return sHTML;
        }
        public string Subtask_View(string sStepID, string sFunction, XDocument xd)
        {
            string sOriginalTaskID = "";
            string sVersion = "";
            string sLabel = "";
            string sHTML = "";
            string sErr = "";

            XElement xOTID = xd.XPathSelectElement("//original_task_id");
            if (xOTID == null) return "Error: XML does not contain original_task_id";
            sOriginalTaskID = xOTID.Value;

            XElement xVer = xd.XPathSelectElement("//version");
            if (xVer == null) return "Error: XML does not contain version";
            sVersion = xVer.Value;

            //get the name and code for belonging to this otid and version
            if (ui.IsGUID(sOriginalTaskID))
            {
                string sSQL = "select task_code, task_name from task" +
                    " where original_task_id = '" + sOriginalTaskID + "'" +
                    (sVersion == "" ? " and default_version = 1" : " and version = '" + sVersion + "'");

                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                {
                    return "Error retrieving subtask.<br />" + sErr;
                }

                if (dr != null)
                {
                    sLabel = dr["task_code"].ToString() + " : " + dr["task_name"].ToString(); ;
                }
                else
                {
                    return "Unable to find task [" + sOriginalTaskID + "] version [" + sVersion + "]." + sErr;
                }
            }

            sHTML += "Task: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sLabel + "</span>" + Environment.NewLine;
            sHTML += "<br />";
            sHTML += "Version: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + (sVersion == "" ? "Default" : sVersion) + "</span>" + Environment.NewLine;

            return sHTML;
        }
        public string Sleep_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xSeconds = xd.XPathSelectElement("//seconds");
            if (xSeconds == null) return "Error: XML does not contain seconds";

            string sSeconds = ui.SafeHTML(xSeconds.Value);
            string sHTML = "";

            sHTML += "Seconds to Sleep: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sSeconds + "</span>" + Environment.NewLine;

            return sHTML;
        }
        public string While_View(string sStepID, string sFunction, XDocument xd)
        {
            string sUserID = ui.GetSessionUserID();

            XElement xTest = xd.XPathSelectElement("//test");
            if (xTest == null) return "Error: XML does not contain test";
            XElement xAction = xd.XPathSelectElement("//action");
            if (xAction == null) return "Error: XML does not contain action";

            string sTest = ui.SafeHTML(xTest.Value);
            string sAction = ui.SafeHTML(xAction.Value);
            string sHTML = "";
            string sErr = "";

            sHTML += "While: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sTest + "</span>" + Environment.NewLine;

            sHTML += "<hr />Action:<br />" + Environment.NewLine;
            if (ui.IsGUID(xAction.Value))
            {
                DataRow dr = GetSingleStep(sAction, sUserID, ref sErr);
                if (dr != null && sErr == "")
                    sHTML += DrawEmbeddedReadOnlyStep(dr, bShowNotes);
                else
                    sHTML += "<span class=\"red_text\">" + sErr + "</span>";
            }
            else
            {
                sHTML += "<div class=\"no_step\">No While action defined.</div>";
            }

            return sHTML;
        }
        public string SetDebugLevel_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xDebugLevel = xd.XPathSelectElement("//debug_level");
            if (xDebugLevel == null) return "Error: XML does not contain debug_level";

            string sDebugLevel = ui.SafeHTML(xDebugLevel.Value);
            string sHTML = "";

            switch (sDebugLevel)
            {
                case "0":
                    sDebugLevel = "None";
                    break;
                case "1":
                    sDebugLevel = "Minimal";
                    break;
                case "2":
                    sDebugLevel = "Normal";
                    break;
                case "3":
                    sDebugLevel = "Enhanced";
                    break;
                case "4":
                    sDebugLevel = "Verbose";
                    break;
                default:
                    break;
            }

            sHTML += "Logging Level: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sDebugLevel + "</span>" + Environment.NewLine;

            return sHTML;
        }

        public string GetTaskInstance_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xInstance = xd.XPathSelectElement("//instance");
            if (xInstance == null) return "Error: XML does not contain instance";
            XElement xHandle = xd.XPathSelectElement("//handle");
            if (xHandle == null) return "Error: XML does not contain handle";

            string sInstance = ui.SafeHTML(xInstance.Value);
            string sHandle = ui.SafeHTML(xHandle.Value);
            string sHTML = "";

            sHTML += "Task Instance: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sInstance + "</span>" + Environment.NewLine;
            sHTML += "Handle: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sHandle + "</span>" + Environment.NewLine;

            return sHTML;
        }
        public string CancelTask_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xTaskInstance = xd.XPathSelectElement("//task_instance");
            if (xTaskInstance == null) return "Error: XML does not contain task_instance";

            string sTaskInstance = ui.SafeHTML(xTaskInstance.Value);
            string sHTML = "";

            sHTML += "Task Instance ID(s): " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sTaskInstance + "</span>" + Environment.NewLine;

            return sHTML;
        }
        public string SetVariable_View(string sStepID, string sFunction, XDocument xd)
        {
            string sHTML = "";

            sHTML += "<div id=\"v" + sStepID + "_vars\">";
            sHTML += "<table border=\"0\" class=\"w99pct\" cellpadding=\"0\" cellspacing=\"0\">" + Environment.NewLine;

            IEnumerable<XElement> xPairs = xd.XPathSelectElements("//variable");
            int i = 1;
            foreach (XElement xe in xPairs)
            {
                XElement xKey = xe.XPathSelectElement("name");
                XElement xVal = xe.XPathSelectElement("value");
                XElement xMod = xe.XPathSelectElement("modifier");

                sHTML += "<tr>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\">&nbsp;Variable:&nbsp;</td>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\"><span class=\"code\">" + ui.SafeHTML(xKey.Value) + "</span></td>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\">&nbsp;Value:&nbsp;</td>";
                sHTML += "<td class=\"w75pct\"><span class=\"code\">" + ui.SafeHTML(xVal.Value) + "</span></td>" + Environment.NewLine;

                sHTML += "<td class=\"w1pct\">&nbsp;Modifier:&nbsp;</td>";
                sHTML += "<td class=\"w75pct\"><span class=\"code\">" + xMod.Value + "</span></td>" + Environment.NewLine;

                sHTML += "</tr>" + Environment.NewLine;

                i++;
            }

            sHTML += "</table>" + Environment.NewLine;
            sHTML += "</div>";

            return sHTML;
        }
        public string Substring_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xVariable = xd.XPathSelectElement("//variable_name");
            if (xVariable == null) return "Error: XML does not contain variable_name";
            XElement xStart = xd.XPathSelectElement("//start");
            if (xStart == null) return "Error: XML does not contain start";
            XElement xEnd = xd.XPathSelectElement("//end");
            if (xEnd == null) return "Error: XML does not contain end";
            XElement xSource = xd.XPathSelectElement("//source");
            if (xSource == null) return "Error: XML does not contain source";

            string sVariable = ui.SafeHTML(xVariable.Value);
            string sStart = ui.SafeHTML(xStart.Value);
            string sEnd = ui.SafeHTML(xEnd.Value);
            string sSource = ui.SafeHTML(xSource.Value);
            string sHTML = "";

            sHTML += "Variable:" + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sVariable + "</span>";

            sHTML += "Start Index: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sStart + "</span>";

            sHTML += "End Index: " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sEnd + "</span>";

            sHTML += "Source String: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sSource + "</div>";

            return sHTML;
        }
        public string Loop_View(string sStepID, string sFunction, XDocument xd)
        {
            string sUserID = ui.GetSessionUserID();

            XElement xStart = xd.XPathSelectElement("//start");
            if (xStart == null) return "Error: XML does not contain start";
            XElement xIncrement = xd.XPathSelectElement("//increment");
            if (xIncrement == null) return "Error: XML does not contain increment";
            XElement xCounter = xd.XPathSelectElement("//counter");
            if (xCounter == null) return "Error: XML does not contain counter";
            XElement xTest = xd.XPathSelectElement("//test");
            if (xTest == null) return "Error: XML does not contain test";
            XElement xCompareTo = xd.XPathSelectElement("//compare_to");
            if (xCompareTo == null) return "Error: XML does not contain compare_to";
            XElement xMax = xd.XPathSelectElement("//max");
            if (xMax == null) return "Error: XML does not contain max";
            XElement xAction = xd.XPathSelectElement("//action");
            if (xAction == null) return "Error: XML does not contain action";

            string sStart = ui.SafeHTML(xStart.Value);
            string sIncrement = ui.SafeHTML(xIncrement.Value);
            string sCounter = ui.SafeHTML(xCounter.Value);
            string sTest = ui.SafeHTML(xTest.Value);
            string sCompareTo = ui.SafeHTML(xCompareTo.Value);
            string sMax = ui.SafeHTML(xMax.Value);
            string sAction = xAction.Value;
            string sErr = "";
            string sHTML = "";


            sHTML += "Counter variable " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sCounter + "</span>" + Environment.NewLine;

            sHTML += " begins at" + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sStart + "</span>" + Environment.NewLine;
            sHTML += " and increments by " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sIncrement + "</span>" + Environment.NewLine;

            sHTML += "Loop will continue while variable is " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sTest + "</span>" + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sCompareTo + "</span>" + Environment.NewLine;

            sHTML += " or " + Environment.NewLine;
            sHTML += "<span class=\"code\">" + sMax + "</span>" + Environment.NewLine;

            sHTML += "<hr />" + Environment.NewLine;

            sHTML += "Action:<br />" + Environment.NewLine;
            if (ui.IsGUID(xAction.Value))
            {
                DataRow dr = GetSingleStep(sAction, sUserID, ref sErr);
                if (dr != null && sErr == "")
                    sHTML += DrawEmbeddedReadOnlyStep(dr, bShowNotes);
                else
                    sHTML += "<span class=\"red_text\">" + sErr + "</span>";
            }
            else
            {
                sHTML += "<div class=\"no_step\">No Loop action defined.</div>";
            }

            return sHTML;
        }
        public string LogMessage_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xMessage = xd.XPathSelectElement("//message");
            if (xMessage == null) return "Error: XML does not contain message";

            string sMessage = ui.SafeHTML(xMessage.Value);
            string sHTML = "";

            sHTML += "Log Message: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sMessage + "</div>" + Environment.NewLine;

            return sHTML;
        }
        public string Scriptlet_View(string sStepID, string sFunction, XDocument xd)
        {
            XElement xLanguage = xd.XPathSelectElement("//language");
            if (xLanguage == null) return "Error: XML does not contain language";
            XElement xScript = xd.XPathSelectElement("//script");
            if (xScript == null) return "Error: XML does not contain script";

            string sLanguage = ui.SafeHTML(xLanguage.Value);
            string sScript = ui.FixBreaks(ui.SafeHTML(xScript.Value));

            string sHTML = "";

            sHTML += "Language: <span class=\"code\">" + sLanguage + "</span><br />" + Environment.NewLine;
            sHTML += "Script: <br />" + Environment.NewLine;
            sHTML += "<div class=\"codebox\">" + sScript + "</div>" + Environment.NewLine;

            return sHTML;
        }
        public string BreakLoop_View(string sStepID, string sFunction, XDocument xd)
        {
            return "<!---->";
        }
        #endregion

        public void RemoveStepVars(string sStepID)
        {
            string sErr = "";
            string sSQL = "update task_step set variable_xml = ''" +
                " where step_id = '" + sStepID + "';";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                ui.RaiseError(Page, "Unable to modify step [" + sStepID + "].", false, sErr);
            }
        }

        private string DrawKeyValueSection(string sStepID, string sFunction, XDocument xd, bool bShowPicker, bool bShowMaskOption, string sKeyLabel, string sValueLabel)
        {
            string sElementID = "";
            string sValueFieldID = "";
            string sHTML = "";

            sHTML += "<div id=\"" + sStepID + "_pairs\">";


            IEnumerable<XElement> xPairs = xd.XPathSelectElements("//pair");
            int i = 1;
            foreach (XElement xe in xPairs)
            {
                XElement xKey = xe.XPathSelectElement("key");
                XElement xVal = xe.XPathSelectElement("value");
                XElement xMask = xe.XPathSelectElement("mask");
                string sMask = (xMask == null ? "0" : xMask.Value);

                sHTML += "<table border=\"0\" class=\"w99pct\" cellpadding=\"0\" cellspacing=\"0\"><tr>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\">&nbsp;" + sKeyLabel + ":&nbsp;</td>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\"><input type=\"text\" " + CommonAttribs(sStepID, sFunction, true, "pair[" + i.ToString() + "]/key", ref sElementID, "") +
                    " validate_as=\"variable\"" +
                    " value=\"" + ui.SafeHTML(xKey.Value) + "\"" +
                    " help=\"Enter a name.\"" +
                    " /></td>";

                if (bShowPicker)
                {
                    sHTML += "<td class=\"w1pct\"><img class=\"key_picker_btn pointer\" alt=\"\"" +
                        " src=\"../images/icons/search.png\"" +
                        " function=\"" + sFunction + "\"" +
                        " target_field_id=\"" + sElementID + "\"" +
                        " link_to=\"" + sElementID + "\" />" + Environment.NewLine;

                    sHTML += "</td>" + Environment.NewLine;
                }

                sHTML += "<td class=\"w1pct\">&nbsp;" + sValueLabel + ":&nbsp;</td>";

                // we gotta get the field id first, but don't show the textarea until after
                string sCommonAttribs = CommonAttribs(sStepID, sFunction, true, "pair[" + i.ToString() + "]/value", ref sValueFieldID, "w90pct");

                sHTML += "<td class=\"w50pct\"><input type=\"text\" " + sCommonAttribs +
                    " value=\"" + ui.SafeHTML(xVal.Value) + "\"" +
                    " help=\"Enter a value.\"" +
                    " />" + Environment.NewLine;

                //big box button
                sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                    " src=\"../images/icons/edit_16.png\"" +
                    " link_to=\"" + sValueFieldID + "\" /></td>" + Environment.NewLine;

                //optional mask option
                if (bShowMaskOption)
                {
                    sHTML += "<td>";

                    sHTML += "&nbsp;Mask?: <input type=\"checkbox\" " +
                        CommonAttribs(sStepID, sFunction, true, "pair[" + i.ToString() + "]/mask", "") + " " + SetCheckRadio("1", sMask) + " />" + Environment.NewLine;


                    sHTML += "</td>" + Environment.NewLine;
                }

                sHTML += "<td class=\"w1pct\" align=\"right\"><span class=\"fn_pair_remove_btn pointer\" index=\"" + i.ToString() + "\" step_id=\"" + sStepID + "\">";
                sHTML += "<img style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                    " alt=\"\" title=\"Remove\" /></span></td>";

                sHTML += "</tr></table>" + Environment.NewLine;

                i++;
            }

            sHTML += "<div class=\"fn_pair_add_btn pointer\"" +
                " add_to_id=\"" + sStepID + "_pairs\"" +
                " step_id=\"" + sStepID + "\">" +
                "<img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\"" +
                " alt=\"\" title=\"Add another.\" />( click to add another )</div>";
            sHTML += "</div>";

            return sHTML;
        }
        private string DrawKeyValueSection_View(string sStepID, string sFunction, XDocument xd, string sKeyLabel, string sValueLabel)
        {
            string sHTML = "";
            sHTML += "<div id=\"" + sStepID + "_pairs\">";

            IEnumerable<XElement> xPairs = xd.XPathSelectElements("//pair");
            int i = 1;
            foreach (XElement xe in xPairs)
            {
                XElement xKey = xe.XPathSelectElement("key");
                XElement xVal = xe.XPathSelectElement("value");

                sHTML += "<table border=\"0\" class=\"w99pct\" cellpadding=\"0\" cellspacing=\"0\"><tr>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\">&nbsp;" + sKeyLabel + ":&nbsp;</td>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\"><span class=\"code\">" + ui.SafeHTML(xKey.Value) + "</span></td>" + Environment.NewLine;
                sHTML += "<td class=\"w1pct\">&nbsp;" + sValueLabel + ":&nbsp;</td>";
                sHTML += "<td class=\"w75pct\"><span class=\"code\">" + ui.SafeHTML(xVal.Value) + "</span></td>" + Environment.NewLine;
                sHTML += "</tr></table>" + Environment.NewLine;

                i++;
            }

            sHTML += "</div>";

            return sHTML;
        }

        private string SetOption(string s1, string s2)
        {
            return (s1 == s2 ? " selected=\"selected\"" : "");
        }

        private string SetCheckRadio(string s1, string s2)
        {
            return (s1 == s2 ? " checked=\"checked\"" : "");
        }

        private string DrawVariableSectionForDisplay(DataRow drStep, bool bShowEditLink)
        {
            //we go check if there are vars first, so that way we don't waste space displaying nothing
            //if there are none
            //BUT only hide this empty section on the 'view' page.
            //if it's an edit page, we still show the empty table!

            string sVariableHTML = GetVariablesForStepForDisplay(drStep);

            if (!bShowEditLink && string.IsNullOrEmpty(sVariableHTML))
                return "";

            string sStepID = drStep["step_id"].ToString();
            //string sFunction = drStep["function_name"].ToString();
            int iParseType = (int)drStep["output_parse_type"];
            int iRowDelimiter = (int)drStep["output_row_delimiter"];
            int iColumnDelimiter = (int)drStep["output_column_delimiter"];

            string sHTML = "";
            if (bShowEditLink)
                sHTML += "<span class=\"variable_popup_btn\" step_id=\"" + sStepID + "\">" +
                    "<img src=\"../images/icons/kedit_16.png\"" +
                    " title=\"Manage Variables\" alt=\"\" /> Manage Variables</span>";

            //some types may only have one of the delimiters
            bool sRowDelimiterVisibility = (iParseType == 2 ? true : false);
            bool sColDelimiterVisibility = (iParseType == 2 ? true : false);


            if (sRowDelimiterVisibility)
                sHTML += "<br />Row Break Indicator: " +
                    " <span class=\"code 100px\">" + LabelNonprintable(iRowDelimiter) + "</span>";

            if (sColDelimiterVisibility)
                sHTML += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Field Break Indicator: " +
                    " <span class=\"code 100px\">" + LabelNonprintable(iColumnDelimiter) + "</span>";


            sHTML += sVariableHTML;

            return sHTML;
        }
        private string GetVariablesForStepForDisplay(DataRow drStep)
        {
            string sStepID = drStep["step_id"].ToString();
            string sHTML = "";

            if (!string.IsNullOrEmpty(drStep["variable_xml"].ToString()))
            {
                XDocument xDoc = XDocument.Parse(drStep["variable_xml"].ToString());
                if (xDoc == null)
                    return "Variable XML data for step [" + sStepID + "] is invalid.";

                XElement xVars = xDoc.Element("variables");
                if (xVars == null)
                    return "Variable XML data for step [" + sStepID + "] does not contain 'variables' root node.";

                if (xVars.Elements("variable").Count() > 0)
                {
                    //build the HTML
                    sHTML += "<table class=\"step_variables\" width=\"99%\" border=\"0\">" + Environment.NewLine;
                    sHTML += "<tbody>";

                    //loop
                    foreach (XElement xVar in xVars.Elements("variable"))
                    {
                        string sName = ui.SafeHTML(xVar.Element("name").Value);
                        string sType = xVar.Element("type").Value.ToLower();

                        sHTML += "<tr class=\"row\">";
                        sHTML += "<td><span class=\"code\">" + sName + "</span></td>";

                        switch (sType)
                        {
                            case "range":
                                string sLProp = "";
                                string sRProp = "";
                                //the markers can be a range indicator or a string.
                                if (xVar.Element("range_begin") != null)
                                {
                                    sLProp = " Position [" + xVar.Element("range_begin").Value + "]";
                                }
                                else if (xVar.Element("prefix") != null)
                                {
                                    sLProp = " Prefix [" + xVar.Element("prefix").Value + "]";
                                }
                                else
                                {
                                    return "Variable XML data for step [" + sStepID + "] does not contain a valid begin marker.";
                                }
                                if (xVar.Element("range_end") != null)
                                {
                                    sRProp = " Position [" + xVar.Element("range_end").Value + "]";
                                }
                                else if (xVar.Element("suffix") != null)
                                {
                                    sRProp = " Suffix [" + xVar.Element("suffix").Value + "]";
                                }
                                else
                                {
                                    return "Variable XML data for step [" + sStepID + "] does not contain a valid end marker.";
                                }



                                sHTML += "<td>Characters in Range:</td><td><span class=\"code\">" + ui.SafeHTML(sLProp) + " - " + ui.SafeHTML(sRProp) + "</span></td>";
                                break;


                            case "delimited":
                                sHTML += "<td>Value at Index Position:</td><td><span class=\"code\">" + ui.SafeHTML(xVar.Element("position").Value) + "</span></td>";
                                break;


                            case "regex":
                                sHTML += "<td>Regular Expression:</td><td><span class=\"code\">" + ui.SafeHTML(xVar.Element("regex").Value) + "</span></td>";
                                break;
                            case "xpath":
                                sHTML += "<td>Xpath:</td><td><span class=\"code\">" + ui.SafeHTML(xVar.Element("xpath").Value) + "</span></td>";
                                break;
                            default:
                                sHTML += "INVALID TYPE";
                                break;
                        }

                        sHTML += "</tr>";

                    }

                    //close it out
                    sHTML += "</tbody></table>" + Environment.NewLine;
                }
            }
            return sHTML;
        }

        public string DrawVariableSectionForEdit(DataRow drStep)
        {
            string sHTML = "";
            //string sStepID = drStep["step_id"].ToString();
            //string sFunction = drStep["function_name"].ToString();
            string sParseType = drStep["output_parse_type"].ToString();
            int iRowDelimiter = (int)drStep["output_row_delimiter"];
            int iColumnDelimiter = (int)drStep["output_column_delimiter"];

            //now, some sections or items may or may not be available.
            string sDelimiterSectionVisiblity = "";
            string sRowDelimiterVisibility = "";
            string sColDelimiterVisibility = "";

            //only 'parsed' types show delimiter pickers.  The hardcoded "delimited" (1) 
            //type does not allow changes to the delimiter.
            sRowDelimiterVisibility = (sParseType == "2" ? "" : "hidden");
            sColDelimiterVisibility = (sParseType == "2" ? "" : "hidden");

            //some code here will replace non-printable delimiters with a token string
            string sRowDelimiterLabel = LabelNonprintable(iRowDelimiter);
            string sColumnDelimiterLabel = LabelNonprintable(iColumnDelimiter);

            sHTML += "<div class=\"" + sDelimiterSectionVisiblity + "\" id=\"div_delimiters\">";
            sHTML += "<span id=\"row_delimiter\" class=\"" + sRowDelimiterVisibility + "\">";
            sHTML += "Row Break Indicator: " +
                " <span id=\"output_row_delimiter_label\"" +
                " class=\"delimiter_label code\">" + sRowDelimiterLabel + "</span>";
            sHTML += "<img class=\"pointer\" src=\"../images/icons/search.png\" alt=\"\" title=\"Select a Delimiter\" name=\"delimiter_picker_btn\" target=\"row\" />";
            sHTML += "<img class=\"pointer\" src=\"../images/icons/fileclose.png\" alt=\"\" title=\"Clear this Delimiter\" name=\"delimiter_clear_btn\" target=\"row\" style=\"width:12px;height:12px;\" />"; sHTML += "</span>";

            sHTML += "<span id=\"col_delimiter\" class=\"" + sColDelimiterVisibility + "\">";
            sHTML += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Field Break Indicator: " +
                " <span id=\"output_col_delimiter_label\"" +
                " class=\"delimiter_label code\">" + sColumnDelimiterLabel + "</span>";
            sHTML += "<img class=\"pointer\" src=\"../images/icons/search.png\" alt=\"\" title=\"Select a Delimiter\" name=\"delimiter_picker_btn\" target=\"col\" />";
            sHTML += "<img class=\"pointer\" src=\"../images/icons/fileclose.png\" alt=\"\" title=\"Clear this Delimiter\" name=\"delimiter_clear_btn\" target=\"col\" style=\"width:12px;height:12px;\" />"; sHTML += "</span>";
            sHTML += "</span>";
            sHTML += "</div>";

            sHTML += "</div>";
            //END DELIMITER SECTION


            sHTML += "<div id=\"div_variables\">";
            sHTML += "<div>Variables <hr /><span id=\"variable_add_btn\">" +
                "<img src=\"../images/icons/bookmark_add_32.png\" width=\"16\" height=\"16\"" +
                " alt=\"Add Variable\" title=\"Add Variable\"/> Add a Variable</span><span id=\"variable_clearall_btn\">" +
                "<img src=\"../images/icons/bookmark_delete_32.png\" width=\"16\" height=\"16\"" +
                " alt=\"Clear All Variables\" title=\"Clear All Variables\"/> Clear All Variables</span></div>";
            sHTML += GetVariablesForStepForEdit(drStep);
            sHTML += "</div>";

            return sHTML;
        }
        public string GetVariablesForStepForEdit(DataRow drStep)
        {
            string sStepID = drStep["step_id"].ToString();
            //string sParseType = drStep["output_parse_type"].ToString();

            string sHTML = "";

            //build the HTML
            sHTML += "<ul id=\"edit_variables\" class=\"variables\">";

            if (!string.IsNullOrEmpty(drStep["variable_xml"].ToString()))
            {
                XDocument xDoc = XDocument.Parse(drStep["variable_xml"].ToString());
                if (xDoc == null)
                    return "Variable XML data for step [" + sStepID + "] is invalid.";

                XElement xVars = xDoc.Element("variables");
                if (xVars == null)
                    return "Variable XML data for step [" + sStepID + "] does not contain 'variables' root node.";


                if (xVars.Elements("variable").Count() > 0)
                {
                    //loop
                    foreach (XElement xVar in xVars.Elements("variable"))
                    {
                        string sName = xVar.Element("name").Value;
                        string sType = xVar.Element("type").Value.ToLower();
                        string sVarStrip = sName;
                        string sDetailStrip = "";
                        string sLProp = "";
                        string sRProp = "";
                        string sLIdxChecked = "";
                        string sRIdxChecked = "";
                        string sLPosChecked = "";
                        string sRPosChecked = "";
                        string sVarGUID = "v" + System.Guid.NewGuid().ToString();

                        switch (sType)
                        {
                            case "range":
                                //the markers can be a range indicator or a string.
                                if (xVar.Element("range_begin") != null)
                                {
                                    sLProp = ui.SafeHTML(xVar.Element("range_begin").Value);
                                    sLIdxChecked = " checked=\"checked\"";
                                }
                                else if (xVar.Element("prefix") != null)
                                {
                                    sLProp = ui.SafeHTML(xVar.Element("prefix").Value);
                                    sLPosChecked = " checked=\"checked\"";
                                }
                                else
                                {
                                    return "Variable XML data for step [" + sStepID + "] does not contain a valid begin marker.";
                                }
                                if (xVar.Element("range_end") != null)
                                {
                                    sRProp = ui.SafeHTML(xVar.Element("range_end").Value);
                                    sRIdxChecked = " checked=\"checked\"";
                                }
                                else if (xVar.Element("suffix") != null)
                                {
                                    sRProp = ui.SafeHTML(xVar.Element("suffix").Value);
                                    sRPosChecked = " checked=\"checked\"";
                                }
                                else
                                {
                                    return "Variable XML data for step [" + sStepID + "] does not contain a valid end marker.";
                                }
                                sVarStrip = "Variable: " +
                                    " <input type=\"text\" class=\"var_name code var_unique\" id=\"" + sVarGUID + "_name\"" +
                                    " validate_as=\"variable\"" +
                                    " value=\"" + sName + "\" />";
                                sDetailStrip = " will contain the output found between <br />" +
                                        "<input type=\"radio\" name=\"" + sVarGUID + "_l_mode\" value=\"index\" " + sLIdxChecked + " class=\"prop\" refid=\"" + sVarGUID + "\" />" +
                                        " position / " +
                                        " <input type=\"radio\" name=\"" + sVarGUID + "_l_mode\" value=\"string\" " + sLPosChecked + " class=\"prop\" refid=\"" + sVarGUID + "\" />" +
                                        " prefix " +
                                    " <input type=\"text\" class=\"w100px code prop\" id=\"" + sVarGUID + "_l_prop\"" +
                                    " value=\"" + sLProp + "\" refid=\"" + sVarGUID + "\" />" +
                                    " and " +
                                        "<input type=\"radio\" name=\"" + sVarGUID + "_r_mode\" value=\"index\" " + sRIdxChecked + " class=\"prop\" refid=\"" + sVarGUID + "\" />" +
                                        " position / " +
                                        " <input type=\"radio\" name=\"" + sVarGUID + "_r_mode\" value=\"string\" " + sRPosChecked + " class=\"prop\" refid=\"" + sVarGUID + "\" />" +
                                        " suffix " +
                                    " <input type=\"text\" class=\"w100px code prop\" id=\"" + sVarGUID + "_r_prop\"" +
                                    " value=\"" + sRProp + "\" refid=\"" + sVarGUID + "\" />.";
                                break;



                            case "delimited":
                                sLProp = xVar.Element("position").Value;
                                sVarStrip = "Variable: " +
                                    " <input type=\"text\" class=\"var_name code var_unique\" id=\"" + sVarGUID + "_name\"" +
                                    " validate_as=\"variable\"" +
                                    " value=\"" + sName + "\" />";
                                sDetailStrip += "" +
                                    " will contain the data from column position" +
                                    " <input type=\"text\" class=\"w100px code\" id=\"" + sVarGUID + "_l_prop\"" +
                                    " value=\"" + sLProp + "\" validate_as=\"posint\" />.";
                                break;


                            case "regex":
                                sLProp = ui.SafeHTML(xVar.Element("regex").Value);
                                sVarStrip = "Variable: " +
                                    " <input type=\"text\" class=\"var_name code var_unique\" id=\"" + sVarGUID + "_name\"" +
                                    " validate_as=\"variable\"" +
                                    " value=\"" + sName + "\" />";
                                sDetailStrip += "" +
                                    " will contain the result of the following regular expression: " +
                                    " <br /><input type=\"text\" class=\"w98pct code\"" +
                                    " id=\"" + sVarGUID + "_l_prop\"" +
                                    " value=\"" + sLProp + "\" />.";
                                break;
                            case "xpath":
                                sLProp = ui.SafeHTML(xVar.Element("xpath").Value);
                                sVarStrip = "Variable: " +
                                    " <input type=\"text\" class=\"var_name code var_unique\" id=\"" + sVarGUID + "_name\"" +
                                    " validate_as=\"variable\"" +
                                    " value=\"" + sName + "\" />";
                                sDetailStrip += "" +
                                    " will contain the Xpath: " +
                                    " <br /><input type=\"text\" class=\"w98pct code\"" +
                                    " id=\"" + sVarGUID + "_l_prop\"" +
                                    " value=\"" + sLProp + "\" />.";
                                break;
                            default:
                                sHTML += "INVALID TYPE";
                                break;
                        }


                        sHTML += "<li id=\"" + sVarGUID + "\" class=\"variable\" var_type=\"" + sType + "\">";
                        sHTML += "<span class=\"variable_name\">" + sVarStrip + "</span>";
                        sHTML += "<span class=\"variable_delete_btn\" remove_id=\"" + sVarGUID + "\">" +
                            " <img src=\"../images/icons/fileclose.png\"" +
                                    "  alt=\"Delete Variable\" title=\"Delete Variable\" /></span>";
                        sHTML += "<span class=\"variable_detail\">" + sDetailStrip + "</span>";
                        //an error message placeholder
                        sHTML += "<br /><span id=\"" + sVarGUID + "_msg\" class=\"var_error_msg\"></span>";

                        sHTML += "</li>" + Environment.NewLine;

                    }
                }
            }
            sHTML += "</ul>" + Environment.NewLine;

            return sHTML;
        }
        private string LabelNonprintable(int iVal)
        {
            //may eventually need to do this with a sortedlist and a loop

            switch (iVal)
            {
                case 0:
                    return "N/A";
                case 9:
                    return "TAB";
                case 10:
                    return "LF";
                case 12:
                    return "FF";
                case 13:
                    return "CR";
                case 27:
                    return "ESC";
                case 32:
                    return "SP";
                default:
                    return "&#" + iVal.ToString() + ";";
            }
        }
        private string DrawDropZone(string sParentStepID, string sEmbeddedStepID, string sFunction, string sColumn, string sLabel, bool bRequired)
        {
            //drop zones are common for all the steps that can contain embedded steps.
            //they are a div to drop on and a hidden field to hold the embedded step id.
            string sUserID = ui.GetSessionUserID();
            string sHTML = "";
            string sErr = "";
            string sElementID = "";

            //a hidden field holds the embedded step_id as the data.
            //Following this is a dropzone that actually gets the graphical representation of the embedded step.
            sHTML += "<input type=\"text\" " +
                CommonAttribs(sParentStepID, sFunction, false, sColumn, ref sElementID, "hidden") +
                " value=\"" + sEmbeddedStepID + "\" />" + Environment.NewLine;

            sHTML += sLabel;

            //some of our 'columns' may be complex XPaths.  XPaths have invalid characters for use in 
            //an HTML ID attribute
            //but we need it to be unique... so just 'clean up' the column name
            sColumn = sColumn.Replace("[", "").Replace("]", "").Replace("/", "");

            //the dropzone
            string sElseDropZone = "<div" +
                (bRequired ? " is_required=\"true\" value=\"\"" : "") +
                " id=\"" + sFunction + "_" + sParentStepID + "_" + sColumn + "_dropzone\"" +
                " datafield_id=\"" + sElementID + "\"" +
                " step_id=\"" + sParentStepID + "\"" +
                " class=\"step_nested_drop_target " + (bRequired ? "is_required" : "") + "\">Click here to add a Command.</div>";

            if (ui.IsGUID(sEmbeddedStepID))
            {
                DataRow dr = GetSingleStep(sEmbeddedStepID, sUserID, ref sErr);
                if (dr != null && sErr == "")
                    sHTML += DrawEmbeddedStep(dr);
                else
                    sHTML += "<span class=\"red_text\">" + sErr + "</span>";
            }
            else
            {
                sHTML += sElseDropZone;
            }

            return sHTML;
        }

        //THIS ONE IS OVERLOADED
        public string CommonAttribs(string sStepID, string sFunction, bool bRequired, string sXPath, string sAdditionalClasses)
        {
            //passed in a reference to return the elements unique ID back to the calling function
            //in case it's needed for client side operations.
            string sElementID = "x" + System.Guid.NewGuid().ToString();

            return " id=\"" + sElementID + "\"" +
                " step_id=\"" + sStepID + "\"" +
                " function=\"" + sFunction + "\"" +
                " xpath=\"" + sXPath + "\"" +
                " te_group=\"step_fields\"" +
                " class=\"step_input code " + sAdditionalClasses + "\"" +
                (bRequired ? " is_required=\"true\"" : "") +
                " onchange=\"javascript:onStepFieldChange(this, '" + sStepID + "', '" + sXPath + "');\"";

        }
        public string CommonAttribs(string sStepID, string sFunction, bool bRequired, string sXPath, ref string sElementID, string sAdditionalClasses)
        {
            //passed in a reference to return the elements unique ID back to the calling function
            //in case it's needed for client side operations.
            sElementID = "x" + System.Guid.NewGuid().ToString();

            return " id=\"" + sElementID + "\"" +
                " step_id=\"" + sStepID + "\"" +
                " function=\"" + sFunction + "\"" +
                " xpath=\"" + sXPath + "\"" +
                " te_group=\"step_fields\"" +
                " class=\"step_input code " + sAdditionalClasses + "\"" +
                (bRequired ? " is_required=\"true\"" : "") +
                " onchange=\"javascript:onStepFieldChange(this, '" + sStepID + "', '" + sXPath + "');\"";

        }
        //END OVERLOADED

        private string DrawReadOnlyStepFromXMLDocument(ref XDocument xd, string sStepID, string sFunction)
        {
            string sHTML = "";
            if (xd.XPathSelectElement("//function") != null)
            {
                foreach (XElement xe in xd.XPathSelectElement("//function").Nodes())
                {
                    sHTML += DrawReadOnlyNode(xe, xe.Name.ToString(), sStepID, sFunction);
                }
            }

            if (sHTML.Length == 0)
                sHTML = "Error: Unable to identify any input fields in the XML for this command.";
            return sHTML;
        }

        private string DrawStepFromXMLDocument(ref XDocument xd, string sStepID, string sFunction)
        {
            /*
             * This block reads the XML for a step and uses certain node attributes to determine how to draw the step.
             *
             * attributes include the type of input control to draw
             * the size of the field,
             * whether or not to HTML break after
             * etc
             * 
             */

            string sHTML = "";
            if (xd.XPathSelectElement("//function") != null)
            {
                foreach (XElement xe in xd.XPathSelectElement("//function").Nodes())
                {
                    sHTML += DrawNode(xe, xe.Name.ToString(), sStepID, sFunction);
                }
            }

            if (sHTML.Length == 0)
                sHTML = "Error: Unable to identify any input fields in the XML for this command.";
            return sHTML;
        }
        private string DrawNode(XElement xeNode, string sXPath, string sStepID, string sFunction)
        {
            string sHTML = "";

            string sNodeName = xeNode.Name.ToString();
            string sNodeLabel = (xeNode.Attribute("label") == null ? xeNode.Name.ToString() : xeNode.Attribute("label").Value);

            IDictionary<string, int> dictNodes = new Dictionary<string, int>();

            string sIsEditable = (xeNode.Attribute("is_array") == null ? "" : xeNode.Attribute("is_array").Value);
            bool bIsEditable = dc.IsTrue(sIsEditable);
            string sIsRemovable = (xeNode.Parent.Attribute("is_array") == null ? "" : xeNode.Parent.Attribute("is_array").Value);
            bool bIsRemovable = dc.IsTrue(sIsRemovable);

            //if a node has children we'll draw it with some hierarchical styling.
            //AND ALSO if it's editable, even if it has no children, we'll still draw it as a container.
            if (xeNode.HasElements || bIsEditable)
            {

                //if there is only one child, AND it's not part of an array
                //don't draw the header or the bounding box, just a composite field label.
                if (xeNode.Nodes().Count() == 1 && !bIsEditable)
                {
                    //get the first (and only) node
                    XElement xeOnlyChild = xeNode.XPathSelectElement("*[1]");
                    //call DrawNode just on the off chance it actually has children
                    string sChildXPath = sXPath + "/" + xeOnlyChild.Name.ToString();
                    sHTML += sNodeName + "." + DrawNode(xeOnlyChild, sChildXPath, sStepID, sFunction);

                    //since we're making it composite, the parents are gonna be off.  Go ahead and draw the delete link here.
                    if (bIsRemovable)
                    {
                        sHTML += "<span class=\"fn_nodearray_remove_btn pointer\" xpath_to_delete=\"" + sXPath + "\" step_id=\"" + sStepID + "\">";
                        sHTML += "<img style=\"width:10px; height:10px; margin-right: 4px;\" src=\"../images/icons/fileclose.png\"" +
                            " alt=\"\" title=\"Remove\" /></span>";
                    }

                }
                else //there is more than one child... business as usual
                {
                    sHTML += "<div class=\"ui-widget-content ui-corner-bottom step_group\">"; //this section

                    sHTML += "  <div class=\"ui-state-default step_group_header\">"; //header
                    sHTML += "      <div class=\"step_header_title\">" + sNodeLabel + "</div>";

                    //look, I know this next bit is crazy... but not really.
                    //if THIS NODE is an editable array, it means you can ADD CHILDREN to it.
                    //so, it gets an add link.
                    sHTML += "<div class=\"step_header_icons\">"; //step header icons

                    if (bIsEditable)
                    {
                        sHTML += "<div class=\"fn_nodearray_add_btn pointer\"" +
                            " step_id=\"" + sStepID + "\"" +
                            " xpath=\"" + sXPath + "\">" +
                            "<img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\"" +
                            " alt=\"\" title=\"Add another...\" /></div>";
                    }
                    //BUT, if this nodes PARENT is editable, that means THIS NODE can be deleted.
                    //so, it gets a delete link
                    //you can't remove unless there are more than one
                    if (bIsRemovable)
                    {
                        sHTML += "<span class=\"fn_nodearray_remove_btn pointer\" xpath_to_delete=\"" + sXPath + "\" step_id=\"" + sStepID + "\">";
                        sHTML += "<img style=\"width:10px; height:10px;\" src=\"../images/icons/fileclose.png\"" +
                            " alt=\"\" title=\"Remove\" /></span>";
                    }

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

                        sHTML += DrawNode(xeChildNode, sChildXPath, sStepID, sFunction);

                    }


                    sHTML += "</div>"; //end section
                }
            }
            else
            {
                sHTML += DrawField(xeNode, sXPath, sStepID, sFunction);
                //it may be that these fields themselves are removable
                if (bIsRemovable)
                {
                    sHTML += "<span class=\"fn_nodearray_remove_btn pointer\" xpath_to_delete=\"" + sXPath + "\" step_id=\"" + sStepID + "\">";
                    sHTML += "<img style=\"width:10px; height:10px; margin-right: 4px;\" src=\"../images/icons/fileclose.png\"" +
                        " alt=\"\" title=\"Remove\" /></span>";
                }

            }


            return sHTML;
        }
        private string DrawField(XElement xe, string sXPath, string sStepID, string sFunction)
        {
            string sHTML = "";

            string sNodeValue = xe.Value;
            string sNodeLabel = (xe.Attribute("label") == null ? xe.Name.ToString() : xe.Attribute("label").Value);

 			string sLabelClasses = (xe.Attribute("label_class") == null ? "" : xe.Attribute("label_class").Value);
			string sLabelStyle = (xe.Attribute("label_style") == null ? "" : xe.Attribute("label_style").Value);
           	sNodeLabel = "<span class=\"" + sLabelClasses + "\" style=\"" + sLabelStyle + "\">" + sNodeLabel + ": </span>";
			
			string sBreakAfter = (xe.Attribute("break_after") == null ? "" : xe.Attribute("break_after").Value);
            string sHRAfter = (xe.Attribute("hr_after") == null ? "" : xe.Attribute("hr_after").Value);
            string sHelp = (xe.Attribute("help") == null ? "" : xe.Attribute("help").Value);
            string sCSSClasses = (xe.Attribute("class") == null ? "" : xe.Attribute("class").Value);
            string sStyle = (xe.Attribute("style") == null ? "" : xe.Attribute("style").Value);
            string sInputType = (xe.Attribute("input_type") == null ? "" : xe.Attribute("input_type").Value);


            string sRequired = (xe.Attribute("required") == null ? "" : xe.Attribute("required").Value);
            bool bRequired = dc.IsTrue(sRequired);

            if (sInputType == "textarea")
            {
                //textareas have additional properties
                string sRows = (xe.Attribute("rows") == null ? "2" : xe.Attribute("rows").Value);

                string sTextareaID = "";
                sHTML += sNodeLabel + " <textarea rows=\"" + sRows + "\"" +
                    CommonAttribs(sStepID, sFunction, bRequired, sXPath, ref sTextareaID, sCSSClasses) +
                    " style=\"" + sStyle + "\"" +
                    " help=\"" + sHelp + "\"" +
                    ">" + sNodeValue + "</textarea>" + Environment.NewLine;

                //big box button
                sHTML += "<img class=\"big_box_btn pointer\" alt=\"\"" +
                    " src=\"../images/icons/edit_16.png\"" +
                    " link_to=\"" + sTextareaID + "\" /><br />" + Environment.NewLine;

            }
            else if (sInputType == "dropdown")
            {
                //the data source of a drop down can be a) an xml file, b) an internal web method or c) an "local" inline list
                //there is no "default" datasource... if nothing is available, it draws an empty picker
                string sDatasource = (xe.Attribute("datasource") == null ? "" : xe.Attribute("datasource").Value);
                string sDataSet = (xe.Attribute("dataset") == null ? "" : xe.Attribute("dataset").Value);

                sHTML += sNodeLabel + " <select " + CommonAttribs(sStepID, sFunction, false, sXPath, "") + ">" + Environment.NewLine;
                
                //empty one
                sHTML += "<option " + SetOption("", sNodeValue) + " value=\"\"></option>" + Environment.NewLine;

                switch (sDatasource)
                {
                    case "file":
                        try
                        {
                            //sDataset is an XML file name.
                            //sTable is the parent node in the XML containing the data
                            string sTable = (xe.Attribute("table") == null ? "" : xe.Attribute("table").Value);
                            string sValueNode = (xe.Attribute("valuenode") == null ? "" : xe.Attribute("valuenode").Value);

                            if (sTable == "" || sValueNode == "")
                                return "Unable to render input element - missing required attribute 'table' or 'valuenode'.";

                            DataSet ds = new DataSet();
                            ds.ReadXml(Server.MapPath("~/pages/" + sDataSet));

                            if (ds.Tables[sTable].Rows.Count > 0)
                            {
                                foreach (DataRow dr in ds.Tables[sTable].Rows)
                                {
                                    string sVal = dr[sValueNode].ToString();
                                    sHTML += "<option " + SetOption(sVal, sNodeValue) + " value=\"" + sVal + "\">" + sVal + "</option>" + Environment.NewLine;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            return "Unable to render input element [" + sXPath + "]. Lookup file [" + sDataSet + "] not found or incorrect format." + ex.Message;
                        }

                        break;
                    case "ajax":
                        break;
                    default: //default is "local"
                        //data is pipe delimited
                        string[] aValues = sDataSet.Split('|');
                        foreach (string sVal in aValues)
                        {
                            sHTML += "<option " + SetOption(sVal, sNodeValue) + " value=\"" + sVal + "\">" + sVal + "</option>" + Environment.NewLine;
                        }
                        break;
                }

                sHTML += "</select>";
            }
            else //input is the default
            {
                sHTML += sNodeLabel + " <input type=\"text\" " +
                    CommonAttribs(sStepID, sFunction, bRequired, sXPath, sCSSClasses) +
                    " style=\"" + sStyle + "\"" +
                    " help=\"" + sHelp + "\"" +
                    " value=\"" + sNodeValue + "\" />" + Environment.NewLine;
            }
            
            //some final layout possibilities
            if (sBreakAfter == "true")
                sHTML += "<br />";
            if (sHRAfter == "true")
                sHTML += "<hr />";

            return sHTML;
        }
		
        private string DrawReadOnlyNode(XElement xeNode, string sXPath, string sStepID, string sFunction)
        {
            string sHTML = "";

            string sNodeName = xeNode.Name.ToString();
            string sNodeLabel = (xeNode.Attribute("label") == null ? xeNode.Name.ToString() : xeNode.Attribute("label").Value);

            IDictionary<string, int> dictNodes = new Dictionary<string, int>();

            string sIsEditable = (xeNode.Attribute("is_array") == null ? "" : xeNode.Attribute("is_array").Value);
            bool bIsEditable = dc.IsTrue(sIsEditable);

            //if a node has children we'll draw it with some hierarchical styling.
            //AND ALSO if it's editable, even if it has no children, we'll still draw it as a container.
            if (xeNode.HasElements || bIsEditable)
            {
                //if there is only one child, AND it's not part of an array
                //don't draw the header or the bounding box, just a composite field label.
                if (xeNode.Nodes().Count() == 1 && !bIsEditable)
                {
                    //get the first (and only) node
                    XElement xeOnlyChild = xeNode.XPathSelectElement("*[1]");
                    //call DrawNode just on the off chance it actually has children
                    string sChildXPath = sXPath + "/" + xeOnlyChild.Name.ToString();
                    sHTML += sNodeName + "." + DrawReadOnlyNode(xeOnlyChild, sChildXPath, sStepID, sFunction);
                }
                else //there is more than one child... business as usual
                {
                    sHTML += "<div class=\"ui-widget-content ui-corner-bottom step_group\">"; //this section

                    sHTML += "  <div class=\"ui-state-default step_group_header\">"; //header
                    sHTML += "      <div class=\"step_header_title\">" + sNodeLabel + "</div>";

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

                        sHTML += DrawReadOnlyNode(xeChildNode, sChildXPath, sStepID, sFunction);

                    }

                    sHTML += "</div>"; //end section
                }
            }
            else
            {
                sHTML += DrawReadOnlyField(xeNode, sXPath, sStepID, sFunction);
            }

            return sHTML;
        }
        private string DrawReadOnlyField(XElement xe, string sXPath, string sStepID, string sFunction)
        {
            string sHTML = "";

            string sNodeValue = xe.Value;
            string sNodeLabel = (xe.Attribute("label") == null ? xe.Name.ToString() : xe.Attribute("label").Value);

            string sBreakAfter = (xe.Attribute("break_after") == null ? "" : xe.Attribute("break_after").Value);
            string sHRAfter = (xe.Attribute("hr_after") == null ? "" : xe.Attribute("hr_after").Value);
            
			//use input_type at some future point if needed
			//string sInputType = (xe.Attribute("input_type") == null ? "" : xe.Attribute("input_type").Value);

            sHTML += sNodeLabel + ": " + "<span class=\"code\">" + sNodeValue + "</span>" + Environment.NewLine;

			//some final layout possibilities
            if (sBreakAfter == "true")
                sHTML += "<br />";
            if (sHRAfter == "true")
                sHTML += "<hr />";

            return sHTML;
        }
		


        #region "Command XML Helper Functions"
        public void AddToCommandXML(string sStepID, string sXPath, string sXMLToAdd)
        {
            try
            {
                string sErr = "";

                if (!ui.IsGUID(sStepID))
                {
                    ui.RaiseError(Page, "Unable to modify step. Invalid or missing Step ID. [" + sStepID + "].", false, sErr);
                    return;
                }

                DataRow dr = null;
                string sSQL = "select task_id, codeblock_name, function_name, step_order from task_step where step_id = '" + sStepID + "'";
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, false, "");
                    return;
                }

                if (dr != null)
                {
                    AddNodeToXMLColumn("task_step", "function_xml", "step_id = '" + sStepID + "'", sXPath, sXMLToAdd);

                    //log it
                    ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, dr["task_id"].ToString(), dr["function_name"].ToString(),
                        "Added a new property to Command Type:" + dr["function_name"].ToString() +
                        " Codeblock:" + dr["codeblock_name"].ToString() +
                        " Step Order:" + dr["step_order"].ToString());
                }

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void RemoveFromCommandXML(string sStepID, string sNodeToRemove)
        {
            try
            {
                string sErr = "";

                if (!ui.IsGUID(sStepID))
                {
                    ui.RaiseError(Page, "Unable to modify step.<br />Invalid or missing Step ID. [" + sStepID + "]<br />", false, sErr);
                    return;
                }

                DataRow dr = null;
                string sSQL = "select task_id, codeblock_name, function_name, step_order from task_step where step_id = '" + sStepID + "'";
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, false);
                    return;
                }

                if (dr != null)
                {
                    RemoveNodeFromXMLColumn("task_step", "function_xml", "step_id = '" + sStepID + "'", sNodeToRemove);

                    //log it
                    ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, dr["task_id"].ToString(), dr["function_name"].ToString(),
                        "Removed a property to Command Type:" + dr["function_name"].ToString() +
                        " Codeblock:" + dr["codeblock_name"].ToString() +
                        " Step Order:" + dr["step_order"].ToString());
                }

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public string GetNodeValueFromCommandXML(string sStepID, string sXPath)
        {
            try
            {
                string sErr = "";

                if (!ui.IsGUID(sStepID))
                    return "Unable to modify step.<br />Invalid or missing Step ID. [" + sStepID + "]<br />" + sErr;

                string sXML = "";
                string sSQL = "select function_xml from task_step where step_id = '" + sStepID + "'";
                if (!dc.sqlGetSingleString(ref sXML, sSQL, ref sErr))
                    return sErr;

                if (!string.IsNullOrEmpty(sXML))
                {
                    //parse the doc from the table
                    XDocument xd = XDocument.Parse(sXML);
                    if (xd == null) return "Error: Unable to parse Command XML.";

                    //get the specified node from the doc
                    XElement xNodeToEdit = xd.XPathSelectElement(sXPath);
                    if (xNodeToEdit == null) return "Error: XML does not contain path [" + sXPath + "].";

                    return xNodeToEdit.Value;
                }
                else
                {
                    return "Error: Could not load row for Step ID [" + sStepID + "].";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //this one has a transaction overload
        public void SetNodeValueinCommandXML(string sStepID, string sNodeToSet, string sValue)
        {
            try
            {
                if (!ui.IsGUID(sStepID))
                {
                    ui.RaiseError(Page, "Unable to modify step. Invalid or missing Step ID. [" + sStepID + "] ", false);
                    return;
                }

                SetNodeValueinXMLColumn("task_step", "function_xml", "step_id = '" + sStepID + "'", sNodeToSet, sValue);

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        //Transaction Overload - build when needed using the above as a template
        //public void SetNodeValueinCommandXML(ref dataAccess.acTransaction oTrans, string sStepID, string sNode, string sValue)
        //{
        //}

        //these are the table generic functions...
        public void SetNodeValueinXMLColumn(string sTable, string sXMLColumn, string sWhereClause, string sNodeToSet, string sValue)
        {
            try
            {
                string sErr = "";
                string sXML = "";

                string sSQL = "select " + sXMLColumn + " from " + sTable + " where " + sWhereClause;
                if (!dc.sqlGetSingleString(ref sXML, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, false);
                    return;
                }
                if (!string.IsNullOrEmpty(sXML))
                {
                    //parse the doc from the table
                    XDocument xd = XDocument.Parse(sXML);
                    if (xd == null)
                    {
                        ui.RaiseError(Page, "Error: Unable to parse XML.", false);
                        return;
                    }

                    //get the specified node from the doc ... if it doesn't exist ADD IT!
                    XElement xNodeToSet = xd.XPathSelectElement(sNodeToSet);
                    if (xNodeToSet == null)
                    {
                        //6/2/2011 NSC : this was never getting hit, and it wouldn't have worked anyway.
                        //the xpath can't be used to add a node this way.

                        //7/8/2011 NSC:
                        //HOWEVER, if you find yourself here again wishing you could just ADD it 
                        //if it doesn't exist.  Look at SetElementValue
                        //that will add a CHILD, or update it if it exists.
                        //so, just go get the PARENT of xNodeToSet and call SetElementValue!

                        return;
                    }
                    else
                    {
                        //set it
                        xNodeToSet.Value = sValue;
                    }


                    //then send the whole doc back to the database
                    sSQL = "update " + sTable + " set " + sXMLColumn + " = '" + xd.ToString().Replace("'", "''") + "'" +
                        " where " + sWhereClause;
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        ui.RaiseError(Page, "Unable to update XML Column [" + sXMLColumn + "] on [" + sTable + "].", false, sErr);
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void SetNodeNameinXMLColumn(string sTable, string sXMLColumn, string sWhereClause, string sNodeToSet, string sOldName, string sNewName)
        {
            try
            {
                string sErr = "";
                string sXML = "";

                string sSQL = "select " + sXMLColumn + " from " + sTable + " where " + sWhereClause;
                if (!dc.sqlGetSingleString(ref sXML, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, false);
                    return;
                }
                if (!string.IsNullOrEmpty(sXML))
                {
                    //parse the doc from the table
                    XDocument xd = XDocument.Parse(sXML);
                    if (xd == null)
                    {
                        ui.RaiseError(Page, "Error: Unable to parse XML.", false);
                        return;
                    }

                    //get the specified node from the doc ... if it doesn't exist ignore this request
                    XElement xNodeToSet = xd.XPathSelectElement(sNodeToSet);
                    if (xNodeToSet == null)
                    {
                        return;
                    }
                    else
                    {
                        //set it by pulling out the xml, removing and readding it.
                        string sOriginalXML = xNodeToSet.ToString();
                        string sNewXML = sOriginalXML.Replace(sOldName, sNewName);

                        //well, now we've munged the XML by hand... so reparse it and make sure it's still valid
                        XElement xNew = XElement.Parse(sNewXML);
                        if (xNew == null)
                        {
                            ui.RaiseError(Page, "Error: XML affected by rename operation cannot be parsed.", false, sErr);
                            return;
                        }

                        xNodeToSet.ReplaceWith(xNew);
                    }


                    //then send the whole doc back to the database
                    sSQL = "update " + sTable + " set " + sXMLColumn + " = '" + xd.ToString().Replace("'", "''") + "'" +
                        " where " + sWhereClause;
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        ui.RaiseError(Page, "Unable to update XML Column [" + sXMLColumn + "] on [" + sTable + "].", false, sErr);
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void AddNodeToXMLColumn(string sTable, string sXMLColumn, string sWhereClause, string sXPath, string sXMLToAdd)
        {
            //BE WARNED! this function is shared by many things, and should not be enhanced
            //with sorting or other niceties.  If you need that stuff, build your own function.
            //AddRegistryNode is a perfect example... we wanted sorting on the registries, and also we don't allow array.
            //but parameters for example are by definition arrays of parameter nodes.
            try
            {
                string sErr = "";

                DataRow dr = null;
                string sSQL = "select " + sXMLColumn + " from " + sTable + " where " + sWhereClause;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, false, "");
                    return;
                }

                if (dr != null)
                {
                    //parse the doc from the table
                    XDocument xd = XDocument.Parse(dr[0].ToString());
                    if (xd == null)
                    {
                        ui.RaiseError(Page, "Error: Unable to parse XML column [" + sXMLColumn + "].", false, sErr);
                        return;
                    }

                    //get the specified node from the doc
                    XElement xNodeToEdit = xd.XPathSelectElement(sXPath);
                    if (xNodeToEdit == null)
                    {
                        ui.RaiseError(Page, "Error: XML does not contain path [" + sXPath + "].", false, sErr);
                        return;
                    }

                    //now parse the new section from the text passed in
                    XElement xNew = XElement.Parse(sXMLToAdd);
                    if (xNew == null)
                    {
                        ui.RaiseError(Page, "Error: XML to be added cannot be parsed.", false, sErr);
                        return;
                    }

                    //if the node we are adding to has a text value, sadly it has to go.
                    //we can't detect that, as the Value property shows the value of all children.
                    //but this works, even if it seems backwards.
                    //if the node does not have any children, then clear it.  that will safely clear any
                    //text but not stomp the text of the children.
                    if (!xNodeToEdit.HasElements)
                        xNodeToEdit.SetValue("");
                    //add it to the doc
                    xNodeToEdit.Add(xNew);


                    //then send the whole doc back to the database
                    sSQL = "update " + sTable + " set " + sXMLColumn + " = '" + xd.ToString().Replace("'", "''") + "'" +
                        " where " + sWhereClause;
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        ui.RaiseError(Page, "Unable to update " + sTable + ".", false, sErr);
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void RemoveNodeFromXMLColumn(string sTable, string sXMLColumn, string sWhereClause, string sNodeToRemove)
        {
            try
            {
                string sErr = "";

                DataRow dr = null;
                string sSQL = "select " + sXMLColumn + " from " + sTable + " where " + sWhereClause;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, false);
                    return;
                }

                if (dr != null)
                {
                    //parse the doc from the table
                    XDocument xd = XDocument.Parse(dr[0].ToString());
                    if (xd == null)
                    {
                        ui.RaiseError(Page, "Error: Unable to parse XML column [" + sXMLColumn + "].", false, sErr);
                        return;
                    }
                    //get the specified node from the doc
                    XElement xNodeToWhack = xd.XPathSelectElement(sNodeToRemove);
                    if (xNodeToWhack == null)
                    {
                        //no worries... what you want to delete doesn't exist?  perfect!
                        return;
                    }

                    //whack it
                    xNodeToWhack.Remove();

                    sSQL = "update " + sTable + " set " + sXMLColumn + " = '" + xd.ToString().Replace("'", "''") + "'" +
                        " where " + sWhereClause;
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        ui.RaiseError(Page, "Unable to update Command.", false, sErr);
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SetNodeAttributeinXMLColumn(string sTable, string sXMLColumn, string sWhereClause, string sNodeToSet, string sAttribute, string sValue)
        {
            //THIS ONE WILL do adds if the attribute doesn't exist, or update it if it does.
            try
            {
                string sErr = "";
                string sXML = "";

                string sSQL = "select " + sXMLColumn + " from " + sTable + " where " + sWhereClause;
                if (!dc.sqlGetSingleString(ref sXML, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, false);
                    return;
                }
                if (!string.IsNullOrEmpty(sXML))
                {
                    //parse the doc from the table
                    XDocument xd = XDocument.Parse(sXML);
                    if (xd == null)
                    {
                        ui.RaiseError(Page, "Error: Unable to parse XML.", false);
                        return;
                    }

                    //get the specified node from the doc ... if it doesn't exist ADD IT!
                    XElement xNodeToSet = xd.XPathSelectElement(sNodeToSet);
                    if (xNodeToSet == null)
                    {
                        //do nothing if we didn't find the node
                        return;
                    }
                    else
                    {
                        //set it
                        xNodeToSet.SetAttributeValue(sAttribute, sValue);
                    }


                    //then send the whole doc back to the database
                    sSQL = "update " + sTable + " set " + sXMLColumn + " = '" + xd.ToString().Replace("'", "''") + "'" +
                        " where " + sWhereClause;
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        ui.RaiseError(Page, "Unable to update XML Column [" + sXMLColumn + "] on [" + sTable + "].", false, sErr);
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public string GetCommandXMLTemplate(string sFunction, ref string sErr)
        {
            try
            {
                if (sFunction == "")
                {
                    sErr = "Unable to look up XML.  Invalid command type. [" + sFunction + "]";
                    return "";
                }

                string sXML = "";
                string sSQL = "select xml_template from lu_task_step_function where function_name = '" + sFunction + "'";
                if (!dc.sqlGetSingleString(ref sXML, sSQL, ref sErr))
                    return "";

                if (!string.IsNullOrEmpty(sXML))
                    return sXML;
                else
                {
                    sErr = "Error: Could not loop up XML for command type [" + sFunction + "].";
                    return "";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public string GetStepCommandXML(string sStepID, ref string sErr)
        {
            try
            {
                if (sStepID == "")
                {
                    sErr = "Unable to look up XML.  Invalid Step ID. [" + sStepID + "]";
                    return "";
                }

                string sXML = "";
                string sSQL = "select function_xml from task_step where step_id = '" + sStepID + "'";
                if (!dc.sqlGetSingleString(ref sXML, sSQL, ref sErr))
                    return "";

                if (!string.IsNullOrEmpty(sXML))
                    return sXML;
                else
                {
                    sErr = "Error: Could not loop up XML for Step ID [" + sStepID + "].";
                    return "";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public void AddNodeToRegistry(string sRegistryID, string sXPath, string sXMLToAdd)
        {
            try
            {
                string sErr = "";

                DataRow dr = null;
                string sSQL = "select registry_xml from object_registry where object_id = '" + sRegistryID + "'";
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, false, "");
                    return;
                }

                if (dr != null)
                {
                    //parse the doc from the table
                    XDocument xd = XDocument.Parse(dr[0].ToString());
                    if (xd == null)
                    {
                        ui.RaiseError(Page, "Error: Unable to parse XML column.", false, sErr);
                        return;
                    }

                    //get the specified node from the doc
                    XElement xNodeToEdit = xd.XPathSelectElement(sXPath);
                    if (xNodeToEdit == null)
                    {
                        ui.RaiseError(Page, "Error: XML does not contain path [" + sXPath + "].", false, sErr);
                        return;
                    }

                    //now parse the new section from the text passed in
                    XElement xNew = XElement.Parse(sXMLToAdd);
                    if (xNew == null)
                    {
                        ui.RaiseError(Page, "Error: XML to be added cannot be parsed.", false, sErr);
                        return;
                    }

                    //we're not doing anything if a node with that name already exists.
                    //that's a hardcoded rule for us... no arrays (at least not now)
                    if (xNodeToEdit.XPathSelectElements(xNew.Name.LocalName).Count() == 0)
                    {
                        //if the node we are adding to has a text value, sadly it has to go.
                        //we can't detect that, as the Value property shows the value of all children.
                        //but this works, even if it seems backwards.
                        //if the node does not have any children, then clear it.  that will safely clear any
                        //text but not stomp the text of the children.
                        if (!xNodeToEdit.HasElements)
                            xNodeToEdit.SetValue("");
                        //add it to the doc
                        xNodeToEdit.Add(xNew);

                        //to keep things nice and clean... sort the parent element by node name
                        xNodeToEdit.ReplaceWith(SortXElementByNodeName(xNodeToEdit));

                        //then send the whole doc back to the database
                        sSQL = "update object_registry set registry_xml = '" + xd.ToString().Replace("'", "''") + "'" +
                            " where object_id = '" + sRegistryID + "'";
                        if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        {
                            ui.RaiseError(Page, "Unable to update registry.", false, sErr);
                        }
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //sorting functions
        public XElement SortXElementByNodeName(XElement element)
        {
            XElement newElement;
            newElement = new XElement(element.Name,
                from child in element.Elements()
                orderby child.Name.ToString()
                select child);

            if (element.HasAttributes && newElement != null)
            {
                foreach (XAttribute attrib in element.Attributes())
                {
                    newElement.SetAttributeValue(attrib.Name, attrib.Value);
                }
            }

            return newElement;
        }

        #endregion

        DataTable getConnectionTypes(ref string sErr)
        {
            try
            {
                DataSet ds = new DataSet();
                ds.ReadXml(Server.MapPath("~/pages/luConnectionType.xml"));

                if (ds.Tables["connectionType"].Rows.Count > 0)
                    return ds.Tables["connectionType"];
                else
                    sErr = "luConnectionType.xml is empty.";
            }
            catch (Exception ex)
            {
                sErr = ex.Message.ToString();
            }

            return null;
        }
    }
}
