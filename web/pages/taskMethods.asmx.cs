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
using System.Text.RegularExpressions;
using System.Web.Services;
using System.Xml.Linq;
using System.Xml.XPath;

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
    public class taskMethods : System.Web.Services.WebService
    {

        #region "Steps"
        [WebMethod(EnableSession = true)]
        public string wmUpdateStep(string sStepID, string sFunction, string sXPath, string sValue)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sErr = "";
            string sSQL = "";

            //we encoded this in javascript before the ajax call.
            //the safest way to unencode it is to use the same javascript lib.
            //(sometimes the javascript and .net libs don't translate exactly, google it.)
            sValue = ui.unpackJSON(sValue);

            //if the function type is "_common" that means this is a literal column on the step table.
            if (sFunction == "_common")
            {
                sValue = sValue.Replace("'", "''"); //escape single quotes for the SQL insert
                sSQL = "update task_step set " +
                    sXPath + " = '" + sValue + "'" +
                    " where step_id = '" + sStepID + "';";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }

            }
            else
            {
                //XML processing
                //get the xml from the step table and update it
                string sXMLTemplate = "";

                sSQL = "select function_xml from task_step where step_id = '" + sStepID + "'";

                if (!dc.sqlGetSingleString(ref sXMLTemplate, sSQL, ref sErr))
                {
                    throw new Exception("Unable to get XML data for step [" + sStepID + "].");
                }

                XDocument xDoc = XDocument.Parse(sXMLTemplate);
                if (xDoc == null)
                    throw new Exception("XML data for step [" + sStepID + "] is invalid.");

                XElement xRoot = xDoc.Element("function");
                if (xRoot == null)
                    throw new Exception("XML data for step [" + sStepID + "] does not contain 'function' root node.");

                try
                {
                    XElement xNode = xRoot.XPathSelectElement(sXPath);
                    if (xNode == null)
                        throw new Exception("XML data for step [" + sStepID + "] does not contain '" + sXPath + "' node.");

                    xNode.SetValue(sValue);
                }
                catch (Exception)
                {
                    try
                    {
                        //here's the deal... given an XPath statement, we simply cannot add a new node if it doesn't exist.
                        //why?  because xpath is a query language.  It doesnt' describe exactly what to add due to wildcards and //foo syntax.

                        //but, what we can do is make an ssumption in our specific case... 
                        //that we are only wanting to add because we changed an underlying command XML template, and there are existing commands.

                        //so... we will split the xpath into segments, and traverse upward until we find an actual node.
                        //once we have it, we will need to add elements back down.

                        //string[] nodes = sXPath.Split('/');

                        //foreach (string node in nodes)
                        //{
                        //    //try to select THIS one, and stick it on the backwards stack
                        //    XElement xNode = xRoot.XPathSelectElement("//" + node);
                        //    if (xNode == null)
                        //        throw new Exception("XML data for step [" + sStepID + "] does not contain '" + sXPath + "' node.");

                        //}

                        XElement xFoundNode = null;
                        ArrayList aMissingNodes = new ArrayList();

                        //of course this skips the full path, but we've already determined it's no good.
                        string sWorkXPath = sXPath;
                        while (sWorkXPath.LastIndexOf("/") > -1)
                        {
                            aMissingNodes.Add(sWorkXPath.Substring(sWorkXPath.LastIndexOf("/") + 1));
                            sWorkXPath = sWorkXPath.Substring(0, sWorkXPath.LastIndexOf("/"));

                            xFoundNode = xRoot.XPathSelectElement(sWorkXPath);
                            if (xFoundNode != null)
                            {
                                //Found it! stop looping
                                break;
                            }
                        }

                        //now that we know where to start (xFoundNode), we can use that as a basis for adding
                        foreach (string sNode in aMissingNodes)
                        {
                            xFoundNode.Add(new XElement(sNode));
                        }

                        //now we should be good to stick the value on the final node.
                        XElement xNode = xRoot.XPathSelectElement(sXPath);
                        if (xNode == null)
                            throw new Exception("XML data for step [" + sStepID + "] does not contain '" + sXPath + "' node.");

                        xNode.SetValue(sValue);

                        //xRoot.Add(new XElement(sXPath, sValue));
                        //xRoot.SetElementValue(sXPath, sValue);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Error Saving Step [" + sStepID + "].  Could not find and cannot create the [" + sXPath + "] property in the XML.");
                    }

                }


                sSQL = "update task_step set " +
                    " function_xml = '" + xDoc.ToString(SaveOptions.DisableFormatting).Replace("'", "''") + "'" +
                    " where step_id = '" + sStepID + "';";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }

            }

            sSQL = "select task_id, codeblock_name, step_order from task_step where step_id = '" + sStepID + "'";
            DataRow dr = null;
            if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                throw new Exception(sErr);

            if (dr != null)
            {
                ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, dr["task_id"].ToString(), sFunction,
                    "Codeblock:" + dr["codeblock_name"].ToString() +
                    " Step Order:" + dr["step_order"].ToString() +
                    " Command Type:" + sFunction +
                    " Property:" + sXPath +
                    " New Value: " + sValue);
            }

            return "";
        }

        [WebMethod(EnableSession = true)]
        public void wmDeleteStep(string sStepID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sErr = "";
                string sSQL = "";

                dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                //you have to know which one we are removing
                string sDeletedStepOrder = "0";
                string sTaskID = "";
                string sCodeblock = "";
                string sFunction = "";
                string sFunctionXML = "";

                sSQL = "select task_id, codeblock_name, step_order, function_name, function_xml" +
                    " from task_step where step_id = '" + sStepID + "'";

                DataRow dr = null;
                if (!dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
                    throw new Exception("Unable to get details for step." + sErr);

                if (dr != null)
                {
                    sDeletedStepOrder = dr["step_order"].ToString();
                    sTaskID = dr["task_id"].ToString();
                    sCodeblock = dr["codeblock_name"].ToString();
                    sFunction = dr["function_name"].ToString();
                    sFunctionXML = dr["function_xml"].ToString();

                    //for logging, we'll stick the whole command XML into the log
                    //so we have a complete record of the step that was just deleted.
                    ui.WriteObjectDeleteLog(Globals.acObjectTypes.Task, sTaskID, sFunction,
                        "Codeblock:" + sCodeblock +
                        " Step Order:" + sDeletedStepOrder +
                        " Command Type:" + sFunction +
                        " Details:" + sFunctionXML);
                }

                //"embedded" steps have a codeblock name referencing their "parent" step.
                //if we're deleting a parent, whack all the children
                sSQL = "delete from task_step where codeblock_name = '" + sStepID + "'";
                oTrans.Command.CommandText = sSQL;
                if (!oTrans.ExecUpdate(ref sErr))
                    throw new Exception("Unable to delete step." + sErr);

                //step might have user_settings
                sSQL = "delete from task_step_user_settings where step_id = '" + sStepID + "'";
                oTrans.Command.CommandText = sSQL;
                if (!oTrans.ExecUpdate(ref sErr))
                    throw new Exception("Unable to delete step user settings." + sErr);

                //now whack the parent
                sSQL = "delete from task_step where step_id = '" + sStepID + "'";
                oTrans.Command.CommandText = sSQL;
                if (!oTrans.ExecUpdate(ref sErr))
                    throw new Exception("Unable to delete step." + sErr);


                sSQL = "update task_step set step_order = step_order - 1" +
                    " where task_id = '" + sTaskID + "'" +
                    " and codeblock_name = '" + sCodeblock + "'" +
                    " and step_order > " + sDeletedStepOrder;
                oTrans.Command.CommandText = sSQL;
                if (!oTrans.ExecUpdate(ref sErr))
                    throw new Exception("Unable to reorder steps after deletion." + sErr);

                oTrans.Commit();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmReorderSteps(string sSteps)
        {
            dataAccess dc = new dataAccess();

            try
            {
                int i = 0;

                string[] aSteps = sSteps.Split(',');
                for (i = 0; i <= aSteps.Length - 1; i++)
                {
                    string sErr = "";
                    string sSQL = "update task_step" +
                            " set step_order = " + (i + 1) +
                            " where step_id = '" + aSteps[i] + "'";

                    //there will be no sSQL if there were no steps, so just skip it.
                    if (!string.IsNullOrEmpty(sSQL))
                    {
                        if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        {
                            throw new Exception("Unable to update steps." + sErr);
                        }
                    }
                }

                //System.Threading.Thread.Sleep(2000);
                return "";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmAddStep(string sTaskID, string sCodeblockName, string sItem)
        {
            dataAccess dc = new dataAccess();
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sUserID = ui.GetSessionUserID();

                string sStepHTML = "";
                string sErr = "";
                string sSQL = "";
                string sNewStepID = "";

                if (!ui.IsGUID(sTaskID))
                    throw new Exception("Unable to add step. Invalid or missing Task ID. [" + sTaskID + "]" + sErr);


                //now, the sItem variable may have a function name (if it's a new command)
                //or it may have a guid (if it's from the clipboard)

                //so, if it's a guid after stripping off the prefix, it's from the clipboard

                //the function has a fn_ or clip_ prefix on it from the HTML.  Strip it off.
                //FIX... test the string to see if it BEGINS with fn_ or clip_
                //IF SO... cut off the beginning... NOT a replace operation.
                if (sItem.StartsWith("fn_")) sItem = sItem.Remove(0, 3);
                if (sItem.StartsWith("clip_")) sItem = sItem.Remove(0, 5);

                //NOTE: !! yes we are adding the step with an order of -1
                //the update event on the client does not know the index at which it was dropped.
                //so, we have to insert it first to get the HTML... but the very next step
                //will serialize and update the entire sortable... 
                //immediately replacing this -1 with the correct position

                if (ui.IsGUID(sItem))
                {
                    sNewStepID = sItem;

                    //copy from the clipboard (using the root_step_id to get ALL associated steps)
                    sSQL = "insert into task_step (step_id, task_id, codeblock_name, step_order, step_desc," +
                        " commented, locked, output_parse_type, output_row_delimiter, output_column_delimiter," +
                        " function_name, function_xml, variable_xml)" +
                        " select step_id, '" + sTaskID + "'," +
                        " case when codeblock_name is null then '" + sCodeblockName + "' else codeblock_name end," +
                        "-1,step_desc," +
                        "0,0,output_parse_type,output_row_delimiter,output_column_delimiter," +
                        "function_name,function_xml,variable_xml" +
                        " from task_step_clipboard" +
                        " where user_id = '" + sUserID + "'" +
                        " and root_step_id = '" + sItem + "'";

                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to add step." + sErr);

                    ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, sTaskID, sItem,
                        "Added Command from Clipboard to Codeblock:" + sCodeblockName);
                }
                else
                {
                    //add a new command
                    sNewStepID = ui.NewGUID();

                    //NOTE: !! yes we are doing some command specific logic here.
                    //Certain commands have different 'default' values for delimiters, etc.
                    //sOPM: 0=none, 1=delimited, 2=parsed
                    string sOPM = "0";

                    switch (sItem)
                    {
                        case "sql_exec":
                            sOPM = "1";
                            break;
                        case "win_cmd":
                            sOPM = "1";
                            break;
                        case "dos_cmd":
                            sOPM = "2";
                            break;
                        case "cmd_line":
                            sOPM = "2";
                            break;
                        case "http":
                            sOPM = "2";
                            break;
                        case "parse_text":
                            sOPM = "2";
                            break;
                        case "read_file":
                            sOPM = "2";
                            break;
                    }

                    sSQL = "insert into task_step (step_id, task_id, codeblock_name, step_order," +
                        " commented, locked, output_parse_type, output_row_delimiter, output_column_delimiter," +
                        " function_name, function_xml)" +
                           " select '" + sNewStepID + "'," +
                           "'" + sTaskID + "'," +
                           (string.IsNullOrEmpty(sCodeblockName) ? "NULL" : "'" + sCodeblockName + "'") + "," +
                           "-1," +
                           "0,0," + sOPM + ",0,0," +
                           "'" + sItem + "'," +
                           " xml_template" +
                           " from lu_task_step_function" +
                           " where function_name = '" + sItem + "' limit 1";

                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to add step." + sErr);

                    ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, sTaskID, sItem,
                        "Added Command Type:" + sItem + " to Codeblock:" + sCodeblockName);
                }

                if (!string.IsNullOrEmpty(sNewStepID))
                {
                    //now... get the newly inserted step and draw it's HTML
                    DataRow dr = ft.GetSingleStep(sNewStepID, sUserID, ref sErr);
                    if (dr != null && sErr == "")
                        sStepHTML += ft.DrawFullStep(dr);
                    else
                        sStepHTML += "<span class=\"red_text\">" + sErr + "</span>";

                    //return the html
                    return sNewStepID + sStepHTML;
                }
                else
                {
                    throw new Exception("Unable to add step.  No new step_id." + sErr);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmGetStep(string sStepID)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            acUI.acUI ui = new acUI.acUI();

            try
            {

                string sStepHTML = "";
                string sErr = "";

                if (!ui.IsGUID(sStepID))
                    throw new Exception("Unable to get step. Invalid or missing Step ID. [" + sStepID + "]" + sErr);

                string sUserID = ui.GetSessionUserID();

                DataRow dr = ft.GetSingleStep(sStepID, sUserID, ref sErr);
                if (dr != null && sErr == "")
                {
                    //embedded steps...
                    //if the step_order is -1 and the codeblock_name is a guid, this step is embedded 
                    //within another step
                    if (dr["step_order"].ToString() == "-1" && ui.IsGUID(dr["codeblock_name"].ToString()))
                        sStepHTML += ft.DrawEmbeddedStep(dr);
                    else
                        sStepHTML += ft.DrawFullStep(dr);
                }
                else
                    sStepHTML += "<span class=\"red_text\">" + sErr + "</span>";

                //return the html
                return sStepHTML;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmToggleStep(string sStepID, string sVisible)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            sVisible = (sVisible == "1" ? "1" : "0");

            try
            {
                if (ui.IsGUID(sStepID))
                {
                    string sErr = "";
                    dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                    string sUserID = ui.GetSessionUserID();

                    //is there a row?
                    int iRowCount = 0;
                    dc.sqlGetSingleInteger(ref iRowCount, "select count(*) from task_step_user_settings" +
                                " where user_id = '" + sUserID + "'" +
                                " and step_id = '" + sStepID + "'", ref sErr);

                    if (iRowCount == 0)
                    {
                        oTrans.Command.CommandText = "insert into task_step_user_settings" +
                            " (user_id, step_id, visible, breakpoint, skip)" +
                            " values ('" + sUserID + "','" + sStepID + "', " + sVisible + ", 0, 0)";

                        if (!oTrans.ExecUpdate(ref sErr))
                            throw new Exception("Unable to toggle step (0) [" + sStepID + "]." + sErr);
                    }
                    else
                    {
                        oTrans.Command.CommandText = " update task_step_user_settings set visible = '" + sVisible + "'" +
                            " where step_id = '" + sStepID + "'";
                        if (!oTrans.ExecUpdate(ref sErr))
                            throw new Exception("Unable to toggle step (1) [" + sStepID + "]." + sErr);
                    }


                    oTrans.Commit();

                    return;
                }
                else
                {
                    throw new Exception("Unable to toggle step. Missing or invalid step_id.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmToggleStepCommonSection(string sStepID, string sButton)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                if (ui.IsGUID(sStepID))
                {
                    string sUserID = ui.GetSessionUserID();

                    sButton = (sButton == "" ? "null" : "'" + sButton + "'");

                    string sErr = "";

                    //is there a row?
                    int iRowCount = 0;
                    dc.sqlGetSingleInteger(ref iRowCount, "select count(*) from task_step_user_settings" +
                                " where user_id = '" + sUserID + "'" +
                                " and step_id = '" + sStepID + "'", ref sErr);

                    if (iRowCount == 0)
                    {
                        string sSQL = "insert into task_step_user_settings" +
                            " (user_id, step_id, visible, breakpoint, skip, button)" +
                            " values ('" + sUserID + "','" + sStepID + "', 1, 0, 0, " + sButton + ")";
                        if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                            throw new Exception("Unable to toggle step button (0) [" + sStepID + "]." + sErr);
                    }
                    else
                    {
                        string sSQL = " update task_step_user_settings set button = " + sButton +
                            " where step_id = '" + sStepID + "';";
                        if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                            throw new Exception("Unable to toggle step button (1) [" + sStepID + "]." + sErr);
                    }


                    return;
                }
                else
                {
                    throw new Exception("Unable to toggle step button. Missing or invalid step_id or button.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmCopyCodeblockStepsToClipboard(string sTaskID, string sCodeblockName)
        {
            dataAccess dc = new dataAccess();

            try
            {
                if (sCodeblockName != "")
                {
                    string sErr = "";
                    string sSQL = "select step_id" +
                        " from task_step" +
                        " where task_id = '" + sTaskID + "'" +
                        " and codeblock_name = '" + sCodeblockName + "'" +
                        " order by step_order desc";

                    DataTable dt = new DataTable();
                    if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                        throw new Exception(sErr);

                    foreach (DataRow dr in dt.Rows)
                    {
                        wmCopyStepToClipboard(dr["step_id"].ToString());
                    }

                    return;
                }
                else
                {
                    throw new Exception("Unable to copy Codeblock. Missing or invalid codeblock_name.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        [WebMethod(EnableSession = true)]
        public void wmCopyStepToClipboard(string sStepID)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                if (ui.IsGUID(sStepID))
                {

                    // should also do this whole thing in a transaction.



                    string sUserID = ui.GetSessionUserID();
                    string sErr = "";

                    //stuff gets new ids when copied into the clpboard.
                    //what way when adding, we don't have to loop
                    //(yes, I know we have to loop here, but adding is already a long process
                    //... so we can better afford to do it here than there.)
                    string sNewStepID = ui.NewGUID();

                    //it's a bit hokey, but if a step already exists in the clipboard, 
                    //and we are copying that step again, 
                    //ALWAYS remove the old one.
                    //we don't want to end up with lots of confusing copies
                    string sSQL = "delete from task_step_clipboard" +
                        " where user_id = '" + sUserID + "'" +
                        " and src_step_id = '" + sStepID + "'";
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to clean clipboard." + sErr);

                    sSQL = " insert into task_step_clipboard" +
                        " (user_id, clip_dt, src_step_id, root_step_id, step_id, function_name, function_xml, step_desc," +
                            " output_parse_type, output_row_delimiter, output_column_delimiter, variable_xml)" +
                        " select '" + sUserID + "', now(), step_id, '" + sNewStepID + "', '" + sNewStepID + "'," +
                            " function_name, function_xml, step_desc," +
                            " output_parse_type, output_row_delimiter, output_column_delimiter, variable_xml" +
                        " from task_step" +
                        " where step_id = '" + sStepID + "'";
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to copy step [" + sStepID + "]." + sErr);


                    //now, if the step we just copied has embedded steps,
                    //we need to get them too, but stick them in the clipboard table
                    //in a hidden fashion. (So they are preserved there, but not visible in the list.)

                    //we are doing it in a recursive call since the nested steps may themselves have nested steps.
                    AlsoCopyEmbeddedStepsToClipboard(sUserID, sStepID, sNewStepID, sNewStepID, ref sErr);

                    return;
                }
                else
                {
                    throw new Exception("Unable to copy step. Missing or invalid step_id.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void AlsoCopyEmbeddedStepsToClipboard(string sUserID, string sSourceStepID, string sRootStepID, string sNewParentStepID, ref string sErr)
        {
            dataAccess dc = new dataAccess();
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
			acUI.acUI ui = new acUI.acUI();
			
            //get all the steps that have the calling stepid as a parent (codeblock)
            string sSQL = "select step_id" +
                " from task_step" +
                " where codeblock_name = '" + sSourceStepID + "'";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                throw new Exception(sErr);

            foreach (DataRow dr in dt.Rows)
            {
                string sThisStepID = dr["step_id"].ToString();
                string sThisNewID = ui.NewGUID();

                //put them in the table
                sSQL = "delete from task_step_clipboard" +
                    " where user_id = '" + sUserID + "'" +
                    " and src_step_id = '" + sThisStepID + "'";
                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception("Unable to clean embedded steps of [" + sSourceStepID + "]." + sErr);

                sSQL = " insert into task_step_clipboard" +
                " (user_id, clip_dt, src_step_id, root_step_id, step_id, function_name, function_xml, step_desc," +
                " output_parse_type, output_row_delimiter, output_column_delimiter, variable_xml, codeblock_name)" +
                " select '" + sUserID + "', now(), step_id, '" + sRootStepID + "', '" + sThisNewID + "'," +
                " function_name, function_xml, step_desc," +
                " output_parse_type, output_row_delimiter, output_column_delimiter, variable_xml, '" + sNewParentStepID + "'" +
                " from task_step" +
                " where step_id = '" + sThisStepID + "'";

                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    throw new Exception("Unable to copy embedded steps of [" + sSourceStepID + "]." + sErr);


                //we need to update the "action" XML of the parent too...

                /*OK here's the deal..I'm out of time
                 
                 This should not be hardcoded, it should be smart enough to find an XML node with a specific
                 value and update that node.
                 
                 I just don't know enought about xpath to figure it out, and don't have time to do it before
                 I gotta start chilling at tmo.
                 
                 So, I've hardcoded it to the known cases so it will work.
                 
                 Add a new dynamic command type that has embedded steps, and this will probably no longer work.
                 */

                ft.SetNodeValueinXMLColumn("task_step_clipboard", "function_xml", "user_id = '" + sUserID + "'" +
                    " and step_id = '" + sNewParentStepID + "'", "//action[text() = '" + sThisStepID + "']", sThisNewID);

                ft.SetNodeValueinXMLColumn("task_step_clipboard", "function_xml", "user_id = '" + sUserID + "'" +
                    " and step_id = '" + sNewParentStepID + "'", "//else[text() = '" + sThisStepID + "']", sThisNewID);

                ft.SetNodeValueinXMLColumn("task_step_clipboard", "function_xml", "user_id = '" + sUserID + "'" +
                    " and step_id = '" + sNewParentStepID + "'", "//positive_action[text() = '" + sThisStepID + "']", sThisNewID);

                ft.SetNodeValueinXMLColumn("task_step_clipboard", "function_xml", "user_id = '" + sUserID + "'" +
                    " and step_id = '" + sNewParentStepID + "'", "//negative_action[text() = '" + sThisStepID + "']", sThisNewID);

                //END OF HARDCODED HACK


                // and check this one for children too
                AlsoCopyEmbeddedStepsToClipboard(sUserID, sThisStepID, sRootStepID, sThisNewID, ref sErr);
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmRemoveFromClipboard(string sStepID)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sUserID = ui.GetSessionUserID();
                string sErr = "";

                //if the sStepID is a guid, we are removing just one
                //otherwise, if it's "ALL" we are whacking them all
                if (ui.IsGUID(sStepID))
                {
                    string sSQL = "delete from task_step_clipboard" +
                        " where user_id = '" + sUserID + "'" +
                        " and root_step_id = '" + sStepID + "'";
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to remove step [" + sStepID + "] from clipboard." + sErr);

                    return;
                }
                else if (sStepID == "ALL")
                {
                    string sSQL = "delete from task_step_clipboard where user_id = '" + sUserID + "'";
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to remove step [" + sStepID + "] from clipboard." + sErr);

                    return;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmGetClips()
        {
            dataAccess dc = new dataAccess();
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sUserID = ui.GetSessionUserID();
                string sErr = "";

                //note: some literal values are selected here.  That's because DrawReadOnlyStep
                //requires a more detailed record, but the snip doesn't have some of those values
                //so we hardcode them.
                string sSQL = "select s.clip_dt, s.step_id, s.step_desc, s.function_name, s.function_xml," +
                    " f.function_label, f.category_name, c.category_label, f.icon," +
                    " s.output_parse_type, s.output_row_delimiter, s.output_column_delimiter, s.variable_xml," +
                    " -1 as step_order, 0 as commented " +
                    " from task_step_clipboard s" +
                    " join lu_task_step_function f on s.function_name = f.function_name" +
                    " join lu_task_step_function_category c on f.category_name = c.category_name" +
                    " where s.user_id = '" + sUserID + "'" +
                    " and s.codeblock_name is null" +
                    " order by s.clip_dt desc";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }

                string sHTML = "";

                foreach (DataRow dr in dt.Rows)
                {
                    string sStepID = dr["step_id"].ToString();
                    string sLabel = dr["function_label"].ToString();
                    string sIcon = dr["icon"].ToString();
                    string sClipDT = dr["clip_dt"].ToString();
                    string sDesc = ui.GetSnip(dr["step_desc"].ToString(), 75);

                    sHTML += "<li" +
                        " id=\"clip_" + sStepID + "\"" +
                        " name=\"clip_" + sStepID + "\"" +
                        " class=\"command_item function clip\"" +
                        ">";

                    //a table for the label so the clear icon can right align
                    sHTML += "<table width=\"99%\" border=\"0\"><tr>";
                    sHTML += "<td width=\"1px\"><img alt=\"\" src=\"../images/" + sIcon + "\" /></td>";
                    sHTML += "<td style=\"vertical-align: middle; padding-left: 5px;\">" + sLabel + "</td>";
                    sHTML += "<td width=\"1px\" style=\"vertical-align: middle;\">";

                    //view icon
                    //due to the complexity of telling the core routines to look in the clipboard table, it 
                    //it not possible to easily show the complex command types
                    // without a redesign of how this works.  NSC 4-19-2011
                    //due to several reasons, most notable being that the XML node for each of those commands 
                    //that contains the step_id is hardcoded and the node names differ.
                    //and GetSingleStep requires a step_id which must be mined from the XML.
                    //so.... don't show a preview icon for them
                    string sFunction = dr["function_name"].ToString();
                    if (!"loop,exists,if,while".Contains(sFunction))
                    {
                        sHTML += "<span id=\"btn_view_clip\" view_id=\"v_" + sStepID + "\">" +
                            "<img src=\"../images/icons/search.png\" style=\"width: 16px; height: 16px;\" alt=\"\" />" +
                            "</span>";
                    }
                    sHTML += "</td></tr>";

                    sHTML += "<tr><td>&nbsp;</td><td><span class=\"code\">" + sClipDT + "</span></td>";
                    sHTML += "<td>";
                    //delete icon
                    sHTML += "<span id=\"btn_clear_clip\" remove_id=\"" + sStepID + "\">" +
                        "<img src=\"../images/icons/fileclose.png\" style=\"width: 16px; height: 16px;\" alt=\"\" />" +
                        "</span>";
                    sHTML += "</td></tr></table>";


                    sHTML += "<div class=\"hidden\" id=\"help_text_clip_" + sStepID + "\">" + sDesc + "</div>";

                    //we use this function because it draws a smaller version than DrawReadOnlyStep
                    string sStepHTML = "";
                    //and don't draw those complex ones either
                    if (!"loop,exists,if,while".Contains(sFunction))
                        sStepHTML = ft.DrawEmbeddedReadOnlyStep(dr, true);

                    sHTML += "<div class=\"hidden\" id=\"v_" + sStepID + "\">" + sStepHTML + "</div>";
                    sHTML += "</li>";

                }

                return sHTML;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region "Tasks"
        [WebMethod(EnableSession = true)]
        public string wmGetAttributePickerAttributes()
        {
            dataAccess dc = new dataAccess();

            try
            {
                string sErr = "";
                string sSQL = "select attribute_id, attribute_name, attribute_desc" +
                              " from lu_asset_attribute" +
                              " order by attribute_name";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }

                string sHTML = "";

                if (dt.Rows.Count > 0)
                {
                    sHTML += "<ul>";
                    foreach (DataRow dr in dt.Rows)
                    {

                        sHTML += " <li class=\"attribute_picker_attribute\"" +
                           " id=\"apa_" + dr["attribute_id"].ToString() + "\"" +
                           ">";
                        sHTML += dr["attribute_name"].ToString();
                        sHTML += " </li>";
                    }
                    sHTML += "</ul>";
                }
                else
                {
                    sHTML += "No Values defined.";
                }

                return sHTML;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        [WebMethod(EnableSession = true)]
        public string wmGetAttributePickerValues(string sAttributeID)
        {
            dataAccess dc = new dataAccess();

            try
            {
                string sErr = "";
                string sSQL = "select a.attribute_id, a.attribute_name," +
                    " av.attribute_value_id, av.attribute_value, av.attribute_value_desc" +
                    " from lu_asset_attribute a join lu_asset_attribute_value av on a.attribute_id = av.attribute_id" +
                    " where a.attribute_id = '" + sAttributeID + "'" +
                    " order by av.attribute_value";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }

                string sHTML = "";

                if (dt.Rows.Count > 0)
                {
                    sHTML += "Select a Value:";
                    sHTML += "<ul>";
                    foreach (DataRow dr in dt.Rows)
                    {

                        sHTML += " <li class=\"attribute_picker_value\"" +
                            " attr_val_id=\"" + dr["attribute_value_id"].ToString() + "\"" +
                            " attr_val=\"" + dr["attribute_value"].ToString() + "\"" +
                            " attr_id=\"" + dr["attribute_id"].ToString() + "\"" +
                            " attr_name=\"" + dr["attribute_name"].ToString() + "\"" +
                            " desc=\"" + dr["attribute_value_desc"].ToString().Replace("\"", "").Replace("'", "") + "\"" +
                            ">";
                        sHTML += dr["attribute_value"].ToString();
                        sHTML += " </li>";
                    }
                    sHTML += "</ul>";
                }
                else
                {
                    sHTML += "No Values defined.";
                }

                return sHTML;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        /*
         * 
         * 
         *                                     <li class="attribute_picker_value" attr_val_id="<%#(((System.Data.DataRowView)Container.DataItem)["attribute_value_id"])%>"
                                        attr_val="<%#(((System.Data.DataRowView)Container.DataItem)["attribute_value"])%>"
                                        attr_id="<%#(((System.Data.DataRowView)Container.DataItem)["attribute_id"])%>"
                                        attr_name="<%#(((System.Data.DataRowView)Container.DataItem)["attribute_name"])%>">
                                        <%#(((System.Data.DataRowView)Container.DataItem)["attribute_value"])%>

         * 
         * 
         * 
         * 
         *        
                string sSQL = "select a.attribute_id, a.attribute_name, av.attribute_value_id, av.attribute_value" +
                    " from lu_asset_attribute a join lu_asset_attribute_value av on a.attribute_id = av.attribute_id" +
                    " where a.attribute_id = '" + sAttributeID + "'" +
                    " order by av.attribute_value";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    ui.RaiseError(Page, sErr, true, "");
                    return;
                }

                if (dt.Rows.Count > 0)
                {
                    rpAttributeValues.DataSource = dt;
                    rpAttributeValues.DataBind();

                    lblNoAttributeValues.Visible = false;
                    rpAttributeValues.Visible = true;
                }
                else
                {
                    lblNoAttributeValues.Visible = true;
                    rpAttributeValues.Visible = false;
                }
            }
            catch (Exception ex)
            {
                ui.RaiseError(Page, ex.Message, true, "");
                return;
            }
        }
         * */
        [WebMethod(EnableSession = true)]
        public string wmApproveTask(string sTaskID, string sMakeDefault)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (ui.IsGUID(sTaskID) && ui.IsGUID(sUserID))
                {
                    string sErr = "";
                    string sSQL = "";

                    //check to see if this is the first task to be approved.
                    //if it is, we will make it default.
                    sSQL = "select count(*) from task" +
                        " where original_task_id = " +
                        " (select original_task_id from task where task_id = '" + sTaskID + "')" +
                        " and task_status = 'Approved'";

                    int iCount = 0;
                    if (!dc.sqlGetSingleInteger(ref iCount, sSQL, ref sErr))
                    {
                        throw new Exception("Unable to count Tasks in this family.." + sErr);
                    }

                    if (iCount == 0)
                        sMakeDefault = "1";



                    dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);


                    //flag all the other tasks as not default if this one is meant to be
                    if (sMakeDefault == "1")
                    {
                        sSQL = "update task set" +
                            " default_version = 0" +
                            " where original_task_id =" +
                            " (select original_task_id from (select original_task_id from task where task_id = '" + sTaskID + "') as x)";
                        oTrans.Command.CommandText = sSQL;
                        if (!oTrans.ExecUpdate(ref sErr))
                        {
                            throw new Exception("Unable to update task [" + sTaskID + "]." + sErr);
                        }
                        sSQL = "update task set" +
                        " task_status = 'Approved'," +
                        " default_version = 1" +
                        " where task_id = '" + sTaskID + "';";
                    }
                    else
                    {
                        sSQL = "update task set" +
                            " task_status = 'Approved'" +
                            " where task_id = '" + sTaskID + "'";
                    }

                    oTrans.Command.CommandText = sSQL;
                    if (!oTrans.ExecUpdate(ref sErr))
                    {
                        throw new Exception("Unable to update task [" + sTaskID + "]." + sErr);
                    }

                    oTrans.Commit();

                    ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, sTaskID, "Status", "Development", "Approved");
                    if (sMakeDefault == "1")
                        ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, sTaskID, "Default", "Set as Default Version.");

                }
                else
                {
                    throw new Exception("Unable to update task. Missing or invalid task id. [" + sTaskID + "]");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return "";
        }

        [WebMethod(EnableSession = true)]
        public string wmAddTaskAttribute(string sTaskID, string sGroupID, string sAttributeID, string sAttributeValueID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            string sErr = "";
            string sSQL = "";

            //the group_id might be empty, in which case we need a new guid
            if (string.IsNullOrEmpty(sGroupID))
            {
                sGroupID = ui.NewGUID();
            }

            sSQL += "insert into task_asset_attribute" +
                " (task_id, attribute_group_id, attribute_value_id)" +
                " values (" +
                " '" + sTaskID + "'," +
                " '" + sGroupID + "'," +
                " '" + sAttributeValueID + "'" +
                ")";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }

            //From bugzilla 917 - not a huge fan of doing this on every change...
            sSQL = "exec refresh_asset_task 'task','" + sTaskID + "';";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }

            // need the name of the attribute added for the log
            sSQL = "select laa.attribute_name + ' :: ' + laav.attribute_value as [Attribute] from lu_asset_attribute_value laav " +
                    "join lu_asset_attribute laa " +
                    "on laav.attribute_id = laa.attribute_id " +
                    "where laav.attribute_value_id = '" + sAttributeValueID + "'";
            string sAttributeName = "";

            if (!dc.sqlGetSingleString(ref sAttributeName, sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }

            ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, sTaskID, "", "Attribute Value " + sAttributeName + " Added");

            return sGroupID;
        }

        [WebMethod(EnableSession = true)]
        public string wmRemoveTaskAttribute(string sTaskID, string sGroupID, string sAttributeValueID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            string sErr = "";
            string sSQL = "";

            sSQL += "delete from task_asset_attribute" +
                " where task_id = '" + sTaskID + "'" +
                " and attribute_group_id = '" + sGroupID + "'" +
                " and attribute_value_id = '" + sAttributeValueID + "'";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }

            //From bugzilla 917 - not a huge fan of doing this on every change...
            sSQL = "exec refresh_asset_task 'task','" + sTaskID + "';";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }


            // need the name of the attribute deleted for the log
            sSQL = "select laa.attribute_name + ' :: ' + laav.attribute_value as [Attribute] from lu_asset_attribute_value laav " +
                    "join lu_asset_attribute laa " +
                    "on laav.attribute_id = laa.attribute_id " +
                    "where laav.attribute_value_id = '" + sAttributeValueID + "'";
            string sAttributeName = "";

            if (!dc.sqlGetSingleString(ref sAttributeName, sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }

            ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, sTaskID, "", "Attribute Value " + sAttributeName + " Removed");

            return "";
        }

        [WebMethod(EnableSession = true)]
        public string wmRemoveTaskAttributeGroup(string sTaskID, string sGroupID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            string sErr = "";
            string sSQL = "";

            sSQL = "select laa.attribute_name from task_asset_attribute taa " +
                    "join lu_asset_attribute_value laav " +
                    "on taa.attribute_value_id = laav.attribute_value_id " +
                    "join lu_asset_attribute laa  " +
                    "on laa.attribute_id = laav.attribute_id " +
                    "where attribute_group_id = '" + sGroupID + "'";
            string sAttributeGroupName = "";

            if (!dc.sqlGetSingleString(ref sAttributeGroupName, sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }


            sSQL += "delete from task_asset_attribute" +
                " where task_id = '" + sTaskID + "'" +
                " and attribute_group_id = '" + sGroupID + "'";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }

            //From bugzilla 917 - not a huge fan of doing this on every change...
            sSQL = "exec refresh_asset_task 'task','" + sTaskID + "';";

            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            {
                throw new Exception(sErr);
            }

            ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, sTaskID, "", "Attribute Group " + sAttributeGroupName + " Removed");

            return "";
        }

        [WebMethod(EnableSession = true)]
        public void wmSaveTestAsset(string sTaskID, string sAssetID)
        {
            wmSaveTaskUserSetting(sTaskID, "asset_id", sAssetID);
        }

        [WebMethod(EnableSession = true)]
        public string wmRerunTask(int iInstanceID, string sClearLog)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (iInstanceID > 0 && ui.IsGUID(sUserID))
                {

                    string sInstance = "";
                    string sErr = "";
                    string sSQL = "";

                    if (dc.IsTrue(sClearLog))
                    {
                        sSQL = "delete from task_instance_log" +
                            " where task_instance = '" + iInstanceID.ToString() + "'";

                        if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        {
                            throw new Exception("Unable to clear task instance log for [" + iInstanceID.ToString() + "]." + sErr);
                        }
                    }
                    sSQL = "update task_instance set task_status = 'Submitted'," +
                        " submitted_by = '" + sUserID + "'" +
                        " where task_instance = '" + iInstanceID.ToString() + "'";

                    if (!dc.sqlGetSingleString(ref sInstance, sSQL, ref sErr))
                    {
                        throw new Exception("Unable to rerun task instance [" + iInstanceID.ToString() + "]." + sErr);
                    }

                    return sInstance;
                }
                else
                {
                    throw new Exception("Unable to run task. Missing or invalid task instance [" + iInstanceID.ToString() + "]");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmRunTask(string sTaskID, string sEcosystemID, string sAccountID, string sAssetID, string sParameterXML, int iDebugLevel)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
			uiMethods um = new uiMethods();
			
            //we encoded this in javascript before the ajax call.
            //the safest way to unencode it is to use the same javascript lib.
            //(sometimes the javascript and .net libs don't translate exactly, google it.)
            sParameterXML = ui.unpackJSON(sParameterXML).Replace("'", "''");
							
			//we gotta peek into the XML and encrypt any newly keyed values
			um.PrepareAndEncryptParameterXML(ref sParameterXML);				

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (ui.IsGUID(sTaskID) && ui.IsGUID(sUserID))
                {

                    string sInstance = "";
                    string sErr = "";

                    string sSQL = "call addTaskInstance ('" + sTaskID + "','" + 
						sUserID + "',NULL," + 
						iDebugLevel + ",NULL,'" + 
						sParameterXML + "','" + 
						sEcosystemID + "','" + 
						sAccountID + "')";

                    if (!dc.sqlGetSingleString(ref sInstance, sSQL, ref sErr))
                    {
                        throw new Exception("Unable to run task [" + sTaskID + "]." + sErr);
                    }

                    return sInstance;
                }
                else
                {
                    throw new Exception("Unable to run task. Missing or invalid task [" + sTaskID + "] or asset [" + sAssetID + "] id.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmStopTask(string sInstance)
        {
            dataAccess dc = new dataAccess();

            try
            {
                if (sInstance != "")
                {
                    string sErr = "";
                    string sSQL = "update task_instance set task_status = 'Aborting'" +
                        " where task_instance = '" + sInstance + "'" +
                        " and task_status in ('Processing');";

                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        throw new Exception("Unable to stop task instance [" + sInstance + "]." + sErr);
                    }

                    sSQL = "update task_instance set task_status = 'Cancelled'" +
                        " where task_instance = '" + sInstance + "'" +
                        " and task_status in ('Submitted','Queued')";

                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        throw new Exception("Unable to stop task instance [" + sInstance + "]." + sErr);
                    }

                    return;
                }
                else
                {
                    throw new Exception("Unable to stop task. Missing or invalid task_instance.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmTaskSearch(string sSearchText)
        {
            try
            {
                dataAccess dc = new dataAccess();
                string sErr = "";
                string sWhereString = "";

                if (sSearchText.Length > 0)
                {
                    sWhereString = " and (a.task_name like '%" + sSearchText +
                                   "%' or a.task_desc like '%" + sSearchText +
                                   "%' or a.task_code like '%" + sSearchText + "%' ) ";
                }

                string sSQL = "select a.original_task_id, a.task_id, a.task_name, a.task_code," +
                    " left(a.task_desc, 255) as task_desc, a.version" +
                       " from task a  " +
                       " where default_version = 1" +
                       sWhereString +
                       " order by task_name, default_version desc, version";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }

                string sHTML = "<hr />";
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
                    sHTML += "<ul id=\"search_task_ul\" class=\"search_dialog_ul\">";

                    for (int i = 0; i < iRowsToGet; i++)
                    {
                        string sTaskName = dt.Rows[i]["task_name"].ToString().Replace("\"", "\\\"");
                        string sLabel = dt.Rows[i]["task_code"].ToString() + " : " + sTaskName;
                        string sDesc = dt.Rows[i]["task_desc"].ToString().Replace("\"", "").Replace("'", "");

                        sHTML += "<li class=\"ui-widget-content ui-corner-all search_dialog_value\" tag=\"task_picker_row\"" +
                            " original_task_id=\"" + dt.Rows[i]["original_task_id"].ToString() + "\"" +
                            " task_label=\"" + sLabel + "\"" +
                            "\">";
                        sHTML += "<div class=\"step_header_title search_dialog_value_name\">" + sLabel + "</div>";

                        sHTML += "<div class=\"step_header_icons\">";

                        //if there's a description, show a tooltip
                        if (!string.IsNullOrEmpty(sDesc))
                            sHTML += "<img src=\"../images/icons/info.png\" class=\"search_dialog_tooltip trans50\" title=\"" + sDesc + "\" />";

                        sHTML += "</div>";
                        sHTML += "<div class=\"clearfloat\"></div>";
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

        [WebMethod(EnableSession = true)]
        public string wmGetTaskConnections(string sTaskID)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                if (ui.IsGUID(sTaskID))
                {
                    string sErr = "";
                    string sSQL = "select conn_name from (" +
                        "select distinct ExtractValue(function_xml, '//conn_name[1]') as conn_name" +
                        " from task_step" +
                            " where function_name = 'new_connection'" +
                            " and task_id = '" + sTaskID + "'" +
                            " ) foo" +
                        " where ifnull(conn_name,'') <> ''" +
                        " order by conn_name";

                    DataTable dt = new DataTable();
                    if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                    {
                        throw new Exception("Unable to get connections for task." + sErr);
                    }

                    string sHTML = "";

                    foreach (DataRow dr in dt.Rows)
                    {
                        sHTML += "<div class=\"ui-widget-content ui-corner-all value_picker_value\">" + dr["conn_name"].ToString() + "</div>";
                    }

                    return sHTML;
                }
                else
                {
                    throw new Exception("Unable to get connections for task. Missing or invalid task_id.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmGetWmiNamespaces()
        {
            string sHTML = "";
            string sNamespace = "";
            try
            {

                XDocument xGlobals = XDocument.Load(Server.MapPath("~/pages/luWmiNamespaces.xml"));

                if (xGlobals == null)
                {
                    return "";
                }
                else
                {

                    IEnumerable<XElement> xTemplates = xGlobals.XPathSelectElement("namespaces").XPathSelectElements("namespace");
                    foreach (XElement xEl in xTemplates)
                    {
                        sNamespace = xEl.XPathSelectElement("name").Value.ToString();
                        sHTML += "<div class=\"ui-widget-content ui-corner-all value_picker_value\">" + sNamespace + "</div>";
                    }

                }
                return sHTML;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmGetTaskCodeblocks(string sTaskID, string sStepID)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                if (ui.IsGUID(sTaskID))
                {
                    string sErr = "";
                    string sSQL = "select codeblock_name from task_codeblock where task_id = '" + sTaskID + "'" +
                        " and codeblock_name not in (select codeblock_name from task_step where step_id = '" + sStepID + "')" +
                        " order by codeblock_name";

                    DataTable dt = new DataTable();
                    if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                    {
                        throw new Exception("Unable to get codeblocks for task." + sErr);
                    }

                    string sHTML = "";

                    foreach (DataRow dr in dt.Rows)
                    {
                        sHTML += "<div class=\"ui-widget-content ui-corner-all value_picker_value\">" + dr["codeblock_name"].ToString() + "</div>";
                    }

                    return sHTML;
                }
                else
                {
                    throw new Exception("Unable to get codeblocks for task. Missing or invalid task_id.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmRenameCodeblock(string sTaskID, string sOldCodeblockName, string sNewCodeblockName)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();

            try
            {
                if (ui.IsGUID(sTaskID))
                {

                    // first make sure we are not trying to rename it something that already exists.
                    string sErr = "";
                    string sSQL = "select count(*) from task_codeblock where task_id = '" + sTaskID + "'" +
                        " and codeblock_name = '" + sNewCodeblockName + "'";
                    int iCount = 0;

                    if (!dc.sqlGetSingleInteger(ref iCount, sSQL, ref sErr))
                    {
                        throw new Exception("Unable to check codeblock names for task." + sErr);
                    }
                    if (iCount != 0)
                    {
                        return ("Codeblock Name already in use, choose another.");
                    }

                    // do it
                    dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                    //update the codeblock table
                    sSQL = "update task_codeblock set codeblock_name = '" + sNewCodeblockName +
                        "' where codeblock_name = '" + sOldCodeblockName +
                        "' and task_id = '" + sTaskID + "'";

                    oTrans.Command.CommandText = sSQL;
                    if (!oTrans.ExecUpdate(ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                    //and any steps in that codeblock
                    sSQL = "update task_step set codeblock_name = '" + sNewCodeblockName +
                        "' where codeblock_name = '" + sOldCodeblockName +
                        "' and task_id = '" + sTaskID + "'";

                    oTrans.Command.CommandText = sSQL;
                    if (!oTrans.ExecUpdate(ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                    //the fun part... rename it where it exists in any steps
                    //but this must be in a loop of only the steps where that codeblock reference exists.
                    sSQL = "select step_id from task_step" +
                        " where task_id = '" + sTaskID + "'" +
                        " and ExtractValue(function_xml, '//codeblock[1]') = '" + sOldCodeblockName + "'";
                    oTrans.Command.CommandText = sSQL;
                    DataTable dtSteps = new DataTable();
                    if (!oTrans.ExecGetDataTable(ref dtSteps, ref sErr))
                    {
                        throw new Exception("Unable to get steps referencing the Codeblock." + sErr);
                    }

                    foreach (DataRow dr in dtSteps.Rows)
                    {
                        ft.SetNodeValueinXMLColumn("task_step", "function_xml", "step_id = '" + dr["step_id"].ToString() + "'", "//codeblock[. = '" + sOldCodeblockName + "']", sNewCodeblockName);
                    }



                    //all done
                    oTrans.Commit();

                    return sErr;

                }
                else
                {
                    throw new Exception("Unable to get codeblocks for task. Missing or invalid task_id.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmGetTaskVariables(string sTaskID)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                if (ui.IsGUID(sTaskID))
                {
                    string sErr = "";
                    string sHTML = "";

                    //VARIABLES
                    string sSQL = "select distinct var_name from (" +
                        "select ExtractValue(function_xml, '//function/counter[1]') as var_name" +
                        " from task_step" +
                        " where task_id = '" + sTaskID + "'" +
                        " and function_name = 'loop'" +
                        " UNION" +
                        " select ExtractValue(variable_xml, '//variable/name[1]') as var_name" +
                        " from task_step" +
                        " where task_id = '" + sTaskID + "'" +
                        " UNION" +
                        " select ExtractValue(function_xml, '//variable/name[1]') as var_name" +
                        " from task_step" +
                        " where task_id = '" + sTaskID + "'" +
                        " and function_name in ('set_variable','substring')" +
                        " ) foo" +
                        " where ifnull(var_name,'') <> ''" +
                        " order by var_name";

                    DataTable dtVars = new DataTable();
                    dtVars.Columns.Add("var_name");
                    DataTable dtStupidVars = new DataTable();
                    if (!dc.sqlGetDataTable(ref dtStupidVars, sSQL, ref sErr))
                        throw new Exception("Unable to get variables for task." + sErr);

                    if (dtStupidVars.Rows.Count > 0)
                    {
                        foreach (DataRow drStupidVars in dtStupidVars.Rows)
                        {
                            if (drStupidVars["var_name"].ToString().IndexOf(' ') > -1)
                            {
                                //split it on the space
                                string[] aVars = drStupidVars["var_name"].ToString().Split(' ');
                                foreach (string sVar in aVars)
                                {
                                    dtVars.Rows.Add(sVar);
                                }
                            }
                            else
                            {
                                dtVars.Rows.Add(drStupidVars["var_name"].ToString());
                            }
                        }
                    }

                    //now that we've manually added some rows, resort it
                    DataView dv = dtVars.DefaultView;
                    dv.Sort = "var_name";
                    dtVars = dv.ToTable();



                    //Finally, we have a table with all the vars!
                    if (dtVars.Rows.Count > 0)
                    {

                        sHTML += "<div target=\"var_picker_group_vars\" class=\"ui-widget-content ui-corner-all value_picker_group\"><img alt=\"\" src=\"../images/icons/expand.png\" style=\"width:12px;height:12px;\" /> Variables</div>";
                        sHTML += "<div id=\"var_picker_group_vars\" class=\"hidden\">";

                        foreach (DataRow drVars in dtVars.Rows)
                        {
                            sHTML += "<div class=\"ui-widget-content ui-corner-all value_picker_value\">" + drVars["var_name"].ToString() + "</div>";
                        }

                        sHTML += "</div>";
                    }

                    //PARAMETERS
                    sSQL = "select parameter_xml from task where task_id = '" + sTaskID + "'";

                    string sParameterXML = "";

                    if (!dc.sqlGetSingleString(ref sParameterXML, sSQL, ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                    if (sParameterXML != "")
                    {
                        XDocument xDoc = XDocument.Parse(sParameterXML);
                        if (xDoc == null)
                            throw new Exception("Parameter XML data for task [" + sTaskID + "] is invalid.");

                        XElement xParams = xDoc.XPathSelectElement("/parameters");
                        if (xParams != null)
                        {
                            sHTML += "<div target=\"var_picker_group_params\" class=\"ui-widget-content ui-corner-all value_picker_group\"><img alt=\"\" src=\"../images/icons/expand.png\" style=\"width:12px;height:12px;\" /> Parameters</div>";
                            sHTML += "<div id=\"var_picker_group_params\" class=\"hidden\">";

                            foreach (XElement xParameter in xParams.XPathSelectElements("parameter"))
                            {
                                sHTML += "<div class=\"ui-widget-content ui-corner-all value_picker_value\">" + xParameter.Element("name").Value + "</div>";
                            }
                        }
                        sHTML += "</div>";
                    }

                    // Global Variables in an xml file
                    try
                    {
                        XDocument xGlobals = XDocument.Load(Server.MapPath("~/pages/luGlobalVariable.xml"));

                        if (xGlobals == null)
                        {
                            sErr = "XML globals is empty.";

                        }
                        else
                        {
                            sHTML += "<div target=\"var_picker_group_globals\" class=\"ui-widget-content ui-corner-all value_picker_group\"><img alt=\"\" src=\"../images/icons/expand.png\" style=\"width:12px;height:12px;\" /> Globals</div>";
                            sHTML += "<div id=\"var_picker_group_globals\" class=\"hidden\">";

                            IEnumerable<XElement> xItems = xGlobals.XPathSelectElement("globals").XPathSelectElements("global/name");
                            foreach (XElement xEl in xItems)
                                sHTML += "<div class=\"ui-widget-content ui-corner-all value_picker_value\">" + xEl.Value.ToString() + "</div>";

                            sHTML += "</div>";
                        }

                    }
                    catch (Exception)
                    {
                        // if the file does not exist, just ignore it
                    }




                    return sHTML;
                }
                else
                {
                    throw new Exception("Unable to get variables for task. Missing or invalid task_id.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmUpdateTaskDetail(string sTaskID, string sColumn, string sValue)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (ui.IsGUID(sTaskID) && ui.IsGUID(sUserID))
                {
                    string sErr = "";
                    string sSQL = "";

                    //we encoded this in javascript before the ajax call.
                    //the safest way to unencode it is to use the same javascript lib.
                    //(sometimes the javascript and .net libs don't translate exactly, google it.)
                    sValue = ui.unpackJSON(sValue);

                    string sOriginalTaskID = "";

                    sSQL = "select original_task_id from task where task_id = '" + sTaskID + "'";

                    if (!dc.sqlGetSingleString(ref sOriginalTaskID, sSQL, ref sErr))
                        throw new Exception("Unable to get original_task_id for [" + sTaskID + "]." + sErr);

                    if (sOriginalTaskID == "")
                        return "Unable to get original_task_id for [" + sTaskID + "].";


                    // bugzilla 1074, check for existing task_code and task_name
                    if (sColumn == "task_code" || sColumn == "task_name")
                    {
                        sSQL = "select task_id from task where " +
                                sColumn.Replace("'", "''") + "='" + sValue.Replace("'", "''") + "'" +
                                " and original_task_id <> '" + sOriginalTaskID + "'";

                        string sValueExists = "";
                        if (!dc.sqlGetSingleString(ref sValueExists, sSQL, ref sErr))
                            throw new Exception("Unable to check for existing names [" + sTaskID + "]." + sErr);

                        if (!string.IsNullOrEmpty(sValueExists))
                            return sValue + " exists, please choose another value.";
                    }

                    if (sColumn == "task_code" || sColumn == "task_name")
                    {
                        //changing the name or code updates ALL VERSIONS
                        string sSetClause = sColumn + "='" + sValue.Replace("'", "''") + "'";
                        sSQL = "update task set " + sSetClause + " where original_task_id = '" + sOriginalTaskID + "'";
                    }
                    else
                    {
                        string sSetClause = sColumn + "='" + sValue.Replace("'", "''") + "'";

                        //some columns on this table allow nulls... in their case an empty sValue is a null
                        if (sColumn == "concurrent_instances" || sColumn == "queue_depth")
                        {
                            if (sValue.Replace(" ", "").Length == 0)
                                sSetClause = sColumn + " = null";
                            else
                                sSetClause = sColumn + "='" + sValue.Replace("'", "''") + "'";
                        }

                        //some columns are checkboxes, so make sure it is a db appropriate value (1 or 0)
                        //some columns on this table allow nulls... in their case an empty sValue is a null
                        if (sColumn == "concurrent_by_asset")
                        {
                            if (dc.IsTrue(sValue))
                                sSetClause = sColumn + " = 1";
                            else
                                sSetClause = sColumn + " = 0";
                        }


                        sSQL = "update task set " + sSetClause + " where task_id = '" + sTaskID + "'";
                    }


                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception("Unable to update task [" + sTaskID + "]." + sErr);

                    ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, sTaskID, sColumn, sValue);
                }
                else
                {
                    throw new Exception("Unable to update task. Missing or invalid task [" + sTaskID + "] id.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return "";
        }

        [WebMethod(EnableSession = true)]
        public string wmDeleteTasks(string sDeleteArray)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            string sSql = null;
            string sErr = "";
            string sTaskNames = "";

            if (sDeleteArray.Length < 36)
                return "";

            sDeleteArray = ui.QuoteUp(sDeleteArray);

            //NOTE: right now this plows ALL versions.  There is an enhancement to possibly 'retire' a task, or
            //only delete certain versions.

            try
            {


                // what about the instance tables?????
                // bugzilla 1290 Tasks that have history (task_instance table) can not be deleted
                // exclude them from the list and return a message noting the task(s) that could not be deleted

                // first we need a list of tasks that will not be deleted
                sSql = "select task_name from task t " +
                        "where t.original_task_id in (" + sDeleteArray.ToString() + ") " +
                        "and t.task_id in (select ti.task_id from tv_task_instance ti where ti.task_id = t.task_id)";

                if (!dc.csvGetList(ref sTaskNames, sSql, ref sErr, true))
                    throw new Exception(sErr);

                // list of tasks that will be deleted
                //we have an array of 'original_task_id'.
                //we need an array or task_id
                //build one.
                sSql = "select t.task_id from task t " +
                    "where t.original_task_id in (" + sDeleteArray.ToString() + ") " +
                    "and t.task_id not in (select ti.task_id from tv_task_instance ti where ti.task_id = t.task_id)";

                string sTaskIDs = "";
                if (!dc.csvGetList(ref sTaskIDs, sSql, ref sErr, true))
                    throw new Exception(sErr);

                // if any tasks can be deleted
                if (sTaskIDs.Length > 1)
                {
                    dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                    //oTrans.Command.CommandText = "delete from task_asset_attribute where task_id in (" + sTaskIDs + ")";
                    //if (!oTrans.ExecUpdate(ref sErr))
                    //    throw new Exception(sErr);

                    oTrans.Command.CommandText = "delete from task_step_user_settings" +
                        " where step_id in" +
                        " (select step_id from task_step where task_id in (" + sTaskIDs + "))";
                    if (!oTrans.ExecUpdate(ref sErr))
                        throw new Exception(sErr);

                    oTrans.Command.CommandText = "delete from task_step where task_id in (" + sTaskIDs + ")";
                    if (!oTrans.ExecUpdate(ref sErr))
                        throw new Exception(sErr);

                    oTrans.Command.CommandText = "delete from task_codeblock where task_id in (" + sTaskIDs + ")";
                    if (!oTrans.ExecUpdate(ref sErr))
                        throw new Exception(sErr);

                    oTrans.Command.CommandText = "delete from task where task_id in (" + sTaskIDs + ")";
                    if (!oTrans.ExecUpdate(ref sErr))
                        throw new Exception(sErr);

                    oTrans.Commit();

                    ui.WriteObjectDeleteLog(Globals.acObjectTypes.Task, "Multiple", "Original Task IDs", sDeleteArray.ToString());

                }


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // if the sTaskNames contains any names, then send back a message that these were not deleted because of history records.
            if (sTaskNames.Length > 0)
            {
                return "Task(s) (" + sTaskNames + ") have history rows and could not be deleted.";
            }
            else
            {
                return sErr;
            }

        }

        [WebMethod(EnableSession = true)]
        public string wmExportTasks(string sTaskArray)
        {
			acUI.acUI ui = new acUI.acUI();
			ImportExport.ImportExportClass ie = new ImportExport.ImportExportClass();
			
			string sErr = "";
			
			//pretty much just call the ImportExport function
			try
			{
				//what are we gonna call the final file?
				string sUserID = ui.GetSessionUserID();
				string sFileName = sUserID + "_backup";
				string sPath = Server.MapPath("~/temp/");
				
				if (sTaskArray.Length < 36)
				return "";
				sTaskArray = ui.QuoteUp(sTaskArray);
				
				if (!ie.doBatchTaskExport(sPath, sTaskArray, sFileName, ref sErr))
				{
					throw new Exception("Unable to export Tasks." + sErr);
				}
				
				if (sErr == "")
					return sFileName + ".zip";
				else
					return sErr;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
        }

        [WebMethod(EnableSession = true)]
        public string wmCreateTask(object[] oObj)
        {
            try
            {

                dataAccess dc = new dataAccess();
                acUI.acUI ui = new acUI.acUI();
                string sSql = null;
                string sErr = null;

                // we are passing in 8 elements, if we have 8 go
                //if (oObj.Length != 8) return "Incorrect list of attributes";

                string sTaskName = oObj[0].ToString().Replace("'", "''").Trim();
                string sTaskCode = oObj[1].ToString().Replace("'", "''").Trim();
                string sTaskDesc = oObj[2].ToString().Replace("'", "''").Trim();

                //string sTaskOrder = "";

                //if (oObj.Length > 4)
                //    sTaskOrder = oObj[4].ToString().Trim();

                // checks that cant be done on the client side
                // is the name unique?
                sSql = "select task_id from task " +
                        " where (task_code = '" + sTaskCode + "' or task_name = '" + sTaskName + "')";

                string sValueExists = "";
                if (!dc.sqlGetSingleString(ref sValueExists, sSql, ref sErr))
                {
                    throw new Exception("Unable to check for existing names." + sErr);
                }

                if (sValueExists != "")
                {
                    return "Another Task with that Code or Name exists, please choose another value.";
                }


                // passed client and server validations, create the user
                string sNewID = ui.NewGUID();

                try
                {
                    dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

                    // all good, save the new user and redirect to the user edit page.
                    sSql = "insert task" +
                        " (task_id, original_task_id, version, default_version," +
                        " task_name, task_code, task_desc, created_dt)" +
                           " values " +
                           "('" + sNewID + "', '" + sNewID + "', 1.0000, 1, '" +
                           sTaskName + "', '" + sTaskCode + "', '" + sTaskDesc + "', now())";
                    oTrans.Command.CommandText = sSql;
                    if (!oTrans.ExecUpdate(ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                    // every task gets a MAIN codeblock... period.
                    sSql = "insert task_codeblock (task_id, codeblock_name)" +
                           " values ('" + sNewID + "', 'MAIN')";
                    oTrans.Command.CommandText = sSql;
                    if (!oTrans.ExecUpdate(ref sErr))
                    {
                        throw new Exception(sErr);
                    }

                    oTrans.Commit();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error updating the DB." + ex.Message);
                }

                // add security log
                ui.WriteObjectAddLog(Globals.acObjectTypes.Task, sNewID, sTaskName, "");

                // success, return the new task_id
                return "task_id=" + sNewID;

            }
            catch (Exception ex)
            {
                throw new Exception("One or more invalid or missing AJAX arguments." + ex.Message);
            }

        }

        [WebMethod(EnableSession = true)]
        public string wmCopyTask(string sCopyTaskID, string sTaskCode, string sTaskName)
        {

            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            string sErr = null;

            // checks that cant be done on the client side
            // is the name unique?
            string sTaskNameInUse = "";
            if (!dc.sqlGetSingleString(ref sTaskNameInUse, "select task_id from task where task_name = '" + sTaskName.Replace("'", "''") + "' limit 1", ref sErr))
            {
                throw new Exception(sErr);
            }
            else
            {
                if (!string.IsNullOrEmpty(sTaskNameInUse))
                {
                    return "Task Name [" + sTaskName + "] already in use.  Please choose another name.";
                }
            }

            // checks that cant be done on the client side
            // is the name unique?
            string sTaskCodeInUse = "";
            if (!dc.sqlGetSingleString(ref sTaskCodeInUse, "select task_id from task where task_code = '" + sTaskCode.Replace("'", "''") + "' limit 1", ref sErr))
            {
                throw new Exception(sErr);
            }
            else
            {
                if (!string.IsNullOrEmpty(sTaskCodeInUse))
                {
                    return "Task Code [" + sTaskCode + "] already in use.  Please choose another code.";
                }
            }

            string sNewTaskGUID = CopyTask(0, sCopyTaskID, sTaskName.Replace("'", "''"), sTaskCode.Replace("'", "''"));

            if (!string.IsNullOrEmpty(sNewTaskGUID))
            {
                ui.WriteObjectAddLog(Globals.acObjectTypes.Task, sNewTaskGUID, sTaskName, "Copied from " + sCopyTaskID);
            }


            // success, return the new task_id
            return sNewTaskGUID;

        }

        [WebMethod(EnableSession = true)]
        public string wmGetTaskCodeFromID(string sOriginalTaskID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            if (!ui.IsGUID(sOriginalTaskID.Replace("'", "")))
                throw new Exception("Invalid or missing Task ID.");

            try
            {
                string sErr = "";
                string sSQL = "";

                sSQL = "select task_code from task where original_task_id = '" + sOriginalTaskID + "' and default_version = 1";
                string sTaskCode = "";
                if (!dc.sqlGetSingleString(ref sTaskCode, sSQL, ref sErr))
                {
                    throw new Exception("Unable to get task code." + sErr);
                }
                else
                {

                    return sTaskCode;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        [WebMethod(EnableSession = true)]
        public string wmCreateNewTaskVersion(string sTaskID, string sMinorMajor)
        {
            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sNewVersionGUID = CopyTask((sMinorMajor == "Major" ? 1 : 2), sTaskID, "", "");

                if (!string.IsNullOrEmpty(sNewVersionGUID))
                {
                    ui.WriteObjectAddLog(Globals.acObjectTypes.Task, sNewVersionGUID, sNewVersionGUID, "");
                }

                return sNewVersionGUID;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmGetTaskMaxVersion(string sTaskID, ref string sErr)
        {
            dataAccess dc = new dataAccess();
            try
            {

                string sSQL = "select max(version) as maxversion from task " +
                       " where original_task_id = " +
                       " (select original_task_id from task where task_id = '" + sTaskID + "')";

                string sMax = "";
                if (!dc.sqlGetSingleString(ref sMax, sSQL, ref sErr)) throw new Exception(sErr);

                return sMax;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmGetTaskVersions(string sTaskID)
        {
            dataAccess dc = new dataAccess();
            string sErr = "";
            try
            {
                string sHTML = "";

                string sSQL = "select task_id, version, default_version," +
                              " case default_version when 1 then ' (default)' else '' end as is_default," +
                              " case task_status when 'Approved' then 'encrypted' else 'unlock' end as status_icon," +
                              " date_format(created_dt, '%Y-%m-%d %T') as created_dt" +
                              " from task" +
                              " where original_task_id = " +
                              " (select original_task_id from task where task_id = '" + sTaskID + "')" +
                              " order by version";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception("Error selecting versions: " + sErr);
                }
                else
                {
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            sHTML += "<li class=\"ui-widget-content ui-corner-all version code\" id=\"v_" + dr["task_id"].ToString() + "\"";
                            sHTML += "task_id=\"" + dr["task_id"].ToString() + "\">";
                            sHTML += "<img src=\"../images/icons/" + dr["status_icon"].ToString() + "_16.png\" alt=\"\" />";
                            sHTML += dr["version"].ToString() + "&nbsp;&nbsp;" + dr["created_dt"].ToString() + dr["is_default"].ToString();
                            sHTML += "</li>";
                            sHTML += "";
                            sHTML += "";

                        }
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
        public string wmGetTaskVersionsDropdown(string sOriginalTaskID)
        {
            dataAccess dc = new dataAccess();
            string sErr = "";
            try
            {
                StringBuilder sbString = new StringBuilder();

                string sSQL = "select task_id, version, default_version" +
                        " from task " +
                        " where original_task_id = '" + sOriginalTaskID + "'" +
                        " order by default_version desc, version";

                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                {
                    throw new Exception("Error selecting versions: " + sErr);
                }
                else
                {
                    if (dt.Rows.Count > 0)
                    {
                        sbString.Append("<select name=\"ddlTaskVersions\" style=\"width: 200px;\" id=\"ddlTaskVersions\">");
                        foreach (DataRow dr in dt.Rows)
                        {
                            string sLabel = dr["version"].ToString() + (dr["default_version"].ToString() == "1" ? " (default)" : "");
                            sbString.Append("<option value=\"" + dr["task_id"].ToString() + "\">" + sLabel + "</option>");
                        }
                        sbString.Append("</select>");
                        //<a onclick=""AddNewUserAttribute();"" id=""btnAddNewAttribute"">[add]</a>")
                        return sbString.ToString();
                    }
                    else
                    {
                        return "<select name=\"ddlTaskVersions\" id=\"ddlTaskVersions\"></select>";
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private string CopyTask(int iMode, string sSourceTaskID, string sNewTaskName, string sNewTaskCode)
        {
            //iMode 0=new task, 1=new major version, 2=new minor version
            dataAccess dc = new dataAccess();
			acUI.acUI ui = new acUI.acUI();
			
            string sErr = "";
            string sSQL = "";

            string sNewTaskID = ui.NewGUID();

            int iIsDefault = 0;
            string sTaskName = "";
            double dVersion = 1.000;
            double dMaxVer = 0.000;
            string sOTID = "";

            //do it all in a transaction
            dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

            //figure out the new name and selected version
            oTrans.Command.CommandText = "select task_name, version, original_task_id from task where task_id = '" + sSourceTaskID + "'";
            DataRow dr = null;
            if (!oTrans.ExecGetDataRow(ref dr, ref sErr))
                throw new Exception("Unable to find task for ID [" + sSourceTaskID + "]." + sErr);

            sTaskName = dr["task_name"].ToString();
            dVersion = Convert.ToDouble(dr["version"]);
            sOTID = dr["original_task_id"].ToString();

            //figure out the new version
            switch (iMode)
            {
                case 0:
                    sTaskName = sNewTaskName;
                    iIsDefault = 1;
                    dVersion = 1.000;
                    sOTID = sNewTaskID;

                    break;
                case 1:
                    //gotta get the highest version
                    sSQL = "select max(version) from task where task_id = '" + sOTID + "'";
                    dc.sqlGetSingleDouble(ref dMaxVer, sSQL, ref sErr);
                    if (sErr != "")
                    {
                        oTrans.RollBack();
                        throw new Exception(sErr);
                    }

                    dVersion = dMaxVer + 1;

                    break;
                case 2:
                    sSQL = "select max(version) from task where task_id = '" + sOTID + "'" +
                        " and cast(version as unsigned) = " + Convert.ToInt32(dVersion);
                    dc.sqlGetSingleDouble(ref dMaxVer, sSQL, ref sErr);
                    if (sErr != "")
                    {
                        oTrans.RollBack();
                        throw new Exception(sErr);
                    }

                    dVersion = dMaxVer + 0.001;

                    break;
                default: //a iMode is required
                    throw new Exception("A mode required for this copy operation." + sErr);
            }

            //if we are versioning, AND there are not yet any 'Approved' versions,
            //we set this new version to be the default
            //(that way it's the one that you get taken to when you pick it from a list)
            if (iMode > 0)
            {
                sSQL = "select case when count(*) = 0 then 1 else 0 end" +
                    " from task where original_task_id = '" + sOTID + "'" +
                    " and task_status = 'Approved'";
                dc.sqlGetSingleInteger(ref iIsDefault, sSQL, ref sErr);
                if (sErr != "")
                {
                    oTrans.RollBack();
                    throw new Exception(sErr);
                }
            }


            //start copying
            oTrans.Command.CommandText = "create temporary table _copy_task" +
                " select * from task where task_id = '" + sSourceTaskID + "'";
            if (!oTrans.ExecUpdate(ref sErr))
                throw new Exception(sErr);

            //update the task_id
            oTrans.Command.CommandText = "update _copy_task set" +
                " task_id = '" + sNewTaskID + "'," +
                " original_task_id = '" + sOTID + "'," +
                " version = '" + dVersion + "'," +
                " task_name = '" + sTaskName + "'," +
                " default_version = " + iIsDefault.ToString() + "," +
                " task_status = 'Development'," +
                " created_dt = now()";
            if (!oTrans.ExecUpdate(ref sErr))
                throw new Exception(sErr);

            //update the task_code if necessary
            if (iMode == 0)
            {
                oTrans.Command.CommandText = "update _copy_task set task_code = '" + sNewTaskCode + "'";
                if (!oTrans.ExecUpdate(ref sErr))
                    throw new Exception(sErr);
            }

            //codeblocks
            oTrans.Command.CommandText = "create temporary table _copy_task_codeblock" +
                " select '" + sNewTaskID + "' as task_id, codeblock_name" +
                " from task_codeblock where task_id = '" + sSourceTaskID + "'";
            if (!oTrans.ExecUpdate(ref sErr))
                throw new Exception(sErr);


            //USING TEMPORARY TABLES... need a place to hold step ids while we manipulate them
            oTrans.Command.CommandText = "create temporary table _step_ids" +
                " select distinct step_id, uuid() as newstep_id" +
                " from task_step where task_id = '" + sSourceTaskID + "'";
            if (!oTrans.ExecUpdate(ref sErr))
                throw new Exception(sErr);

            //steps temp table
            oTrans.Command.CommandText = "create temporary table _copy_task_step" +
                " select step_id, '" + sNewTaskID + "' as task_id, codeblock_name, step_order, commented," +
                " locked, function_name, function_xml, step_desc, output_parse_type, output_row_delimiter," +
                " output_column_delimiter, variable_xml" +
                " from task_step where task_id = '" + sSourceTaskID + "'";
            if (!oTrans.ExecUpdate(ref sErr))
                throw new Exception(sErr);

            //update the step id
            oTrans.Command.CommandText = "update _copy_task_step a, _step_ids b" +
                " set a.step_id = b.newstep_id" +
                " where a.step_id = b.step_id";
            if (!oTrans.ExecUpdate(ref sErr))
                throw new Exception(sErr);

            //update steps with codeblocks that reference a step (embedded steps)
            oTrans.Command.CommandText = "update _copy_task_step a, _step_ids b" +
                " set a.codeblock_name = b.newstep_id" +
                " where b.step_id = a.codeblock_name";
            if (!oTrans.ExecUpdate(ref sErr))
                throw new Exception(sErr);


            //spin the steps and update any embedded step id's in the commands
            oTrans.Command.CommandText = "select step_id, newstep_id from _step_ids";
            DataTable dtStepIDs = new DataTable();
            if (!oTrans.ExecGetDataTable(ref dtStepIDs, ref sErr))
                throw new Exception("Unable to get step ids." + sErr);

            foreach (DataRow drStepIDs in dtStepIDs.Rows)
            {
                oTrans.Command.CommandText = "update _copy_task_step" +
                    " set function_xml = replace(lower(function_xml), '" + drStepIDs["step_id"].ToString().ToLower() + "', '" + drStepIDs["newstep_id"].ToString() + "')" +
                    " where function_name in ('if','loop','exists')";
                if (!oTrans.ExecUpdate(ref sErr))
                    throw new Exception(sErr);
            }


            //finally, put the temp steps table in the real steps table
            oTrans.Command.CommandText = "insert into task select * from _copy_task";
            if (!oTrans.ExecUpdate(ref sErr))
                throw new Exception(sErr);

            oTrans.Command.CommandText = "insert into task_codeblock select * from _copy_task_codeblock";
            if (!oTrans.ExecUpdate(ref sErr))
                throw new Exception(sErr);

            oTrans.Command.CommandText = "insert into task_step select * from _copy_task_step";
            if (!oTrans.ExecUpdate(ref sErr))
                throw new Exception(sErr);

            //finally, if we versioned up and we set this one as the new default_version,
            //we need to unset the other row
            if (iMode > 0 && iIsDefault == 1)
            {
                oTrans.Command.CommandText = "update task" +
                    " set default_version = 0" +
                    " where original_task_id = '" + sOTID + "'" +
                    " and task_id <> '" + sNewTaskID + "'";
                if (!oTrans.ExecUpdate(ref sErr))
                    throw new Exception(sErr);
            }


            oTrans.Commit();

            return sNewTaskID;
        }

        [WebMethod(EnableSession = true)]
        public string wmGetAssetIDFromName(string sName)
        {
            dataAccess dc = new dataAccess();


            try
            {
                if (sName.Length > 0)
                {
                    string sAssetID = "";
                    string sErr = "";
                    string sSQL = "select asset_id from asset where asset_name = '" + sName + "'";

                    if (!dc.sqlGetSingleString(ref sAssetID, sSQL, ref sErr))
                        throw new Exception("Unable to check for Asset [" + sName + "]." + sErr);

                    return sAssetID;
                }
                else
                    throw new Exception("Unable to check for Asset - missing Asset Name.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region "Command Specific"
        [WebMethod(EnableSession = true)]
        public void wmFnSetvarAddVar(string sStepID)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            try
            {
                ft.AddToCommandXML(sStepID, "//function", "<variable>" +
                    "<name input_type=\"text\"></name>" +
                    "<value input_type=\"text\"></value>" +
                    "<modifier input_type=\"select\">DEFAULT</modifier>" +
                    "</variable>");

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmFnClearvarAddVar(string sStepID)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            try
            {
                ft.AddToCommandXML(sStepID, "//function", "<variable><name input_type=\"text\"></name></variable>");

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmFnExistsAddVar(string sStepID)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            try
            {
                ft.AddToCommandXML(sStepID, "//function", "<variable>" +
                    "<name input_type=\"text\"></name><is_true>0</is_true>" +
                    "</variable>");

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmFnVarRemoveVar(string sStepID, int iIndex)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            //NOTE: this function supports both the set_varible AND clear_variable commands
            try
            {
                if (iIndex > 0)
                {
                    ft.RemoveFromCommandXML(sStepID, "//function/variable[" + iIndex.ToString() + "]");

                    return;
                }
                else
                {
                    throw new Exception("Unable to modify step. Invalid index.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmFnIfRemoveSection(string sStepID, int iIndex)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            acUI.acUI ui = new acUI.acUI();

            try
            {
                if (!ui.IsGUID(sStepID))
                    throw new Exception("Unable to remove section from step. Invalid or missing Step ID. [" + sStepID + "]");

                string sEmbStepID = "";

                if (iIndex > 0)
                {
                    //is there an embedded step?
                    sEmbStepID = ft.GetNodeValueFromCommandXML(sStepID, "//function/tests/test[" + iIndex.ToString() + "]/action[1]");

                    if (ui.IsGUID(sEmbStepID))
                        wmDeleteStep(sEmbStepID); //whack it

                    //now adjust the XML
                    ft.RemoveFromCommandXML(sStepID, "//function/tests/test[" + iIndex.ToString() + "]");
                }
                else if (iIndex == -1)
                {
                    //is there an embedded step?
                    sEmbStepID = ft.GetNodeValueFromCommandXML(sStepID, "//function/else[1]");

                    if (ui.IsGUID(sEmbStepID))
                        wmDeleteStep(sEmbStepID); //whack it

                    //now adjust the XML
                    ft.RemoveFromCommandXML(sStepID, "//function/else[1]");
                }
                else
                {
                    throw new Exception("Unable to modify step. Invalid index.");
                }

                return;

            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmFnIfAddSection(string sStepID, int iIndex)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            try
            {
                if (iIndex > 0)
                {
                    //an index > 0 means its one of many 'else if' sections
                    ft.AddToCommandXML(sStepID, "//function/tests", "<test><eval input_type=\"text\" /><action input_type=\"text\" /></test>");
                }
                else if (iIndex == -1)
                {
                    //whereas an index of -1 means its the ONLY 'else' section
                    ft.AddToCommandXML(sStepID, "//function", "<else input_type=\"text\" />");
                }
                else
                {
                    //and of course a missing or 0 index is an error
                    throw new Exception("Unable to modify step. Invalid index.");
                }

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmFnAddPair(string sStepID)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            try
            {
                ft.AddToCommandXML(sStepID, "//function", "<pair><key input_type=\"text\"></key><value input_type=\"text\"></value></pair>");

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmFnRemovePair(string sStepID, int iIndex)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            try
            {
                if (iIndex > 0)
                {
                    ft.RemoveFromCommandXML(sStepID, "//function/pair[" + iIndex.ToString() + "]");

                    return;
                }
                else
                {
                    throw new Exception("Unable to modify step. Invalid index.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public string wmGetDatasetTemplates(string sStepID)
        {
            string sHTML = "";
            string sParentID = "";
            string sTemplateName = "";

            try
            {
                XDocument xGlobals = XDocument.Load(Server.MapPath("~/pages/luDatasetTemplates.xml"));

                if (xGlobals == null)
                {
                    return "";
                }
                else
                {

                    IEnumerable<XElement> xTemplates = xGlobals.XPathSelectElement("templates").XPathSelectElements("template");
                    foreach (XElement xEl in xTemplates)
                    {
                        sParentID = xEl.Attribute("template_id").Value.ToString();
                        sTemplateName = xEl.XPathSelectElement("name").Value.ToString();
                        sHTML += "<div class=\"ui-widget-content ui-corner-all value_picker_value\" step_id=\"" + sStepID + "\" template_id=\"" + sParentID + "\">" + sTemplateName + "</div>";
                    }

                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return sHTML;

        }

        [WebMethod(EnableSession = true)]
        public string wmDatasetTemplateChange(string sStepID, string sTemplateID)
        {
            dataAccess dc = new dataAccess();

            try
            {
                XDocument xGlobals = XDocument.Load(Server.MapPath("~/pages/luDatasetTemplates.xml"));

                if (xGlobals == null)
                {
                    throw new Exception("Could not load templates.");
                }
                else
                {

                    // we have the step_id and the template_id
                    // get the entire <function... section and replace it in the db for this step_id
                    var xFunctionXml = (from node in xGlobals.Descendants("template")
                                        where (string)node.Attribute("template_id") == sTemplateID
                                        select node).Single().Element("function");

                    if (xFunctionXml == null)
                    {
                        // could not find the value, now what?
                        throw new Exception("Template settings null for template id: " + sTemplateID);
                    }
                    else
                    {
                        // now we need the template_id somehow
                        string sSQL = "";
                        string sErr = "";
                        sSQL = "update task_step set function_xml = '" + xFunctionXml.ToString() + "' where step_id = '" + sStepID + "'";

                        if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        {
                            throw new Exception(sErr);
                        }
                    }


                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return "";

        }

        [WebMethod(EnableSession = true)]
        public void wmFnWaitForTasksRemoveHandle(string sStepID, int iIndex)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            try
            {
                if (iIndex > 0)
                {
                    ft.RemoveFromCommandXML(sStepID, "//function/handle[" + iIndex.ToString() + "]");

                    return;
                }
                else
                {
                    throw new Exception("Unable to modify step. Invalid index.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmFnWaitForTasksAddHandle(string sStepID)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            try
            {
                ft.AddToCommandXML(sStepID, "//function", "<handle><name input_type=\"text\"></name></handle>");

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmFnNodeArrayAdd(string sStepID, string sGroupNode)
        {
            dataAccess dc = new dataAccess();
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            try
            {
                string sErr = "";

                //what's the command type?
                string sFunctionName = "";
                string sSQL = "select function_name from task_step where step_id = '" + sStepID + "'";
                if (!dc.sqlGetSingleString(ref sFunctionName, sSQL, ref sErr))
                    throw new Exception(sErr);

                if (sFunctionName.Length > 0)
                {
                    //so, let's get the one we want from the XML template for this step... adjust the indexes, and add it.
                    string sXML = ft.GetCommandXMLTemplate(sFunctionName, ref sErr);
                    if (sErr != "") throw new Exception(sErr);

                    //validate it
                    //parse the doc from the table
                    XDocument xd = XDocument.Parse(sXML);
                    if (xd == null) throw new Exception("Error: Unable to parse Function XML.");

                    //get the original "group" node from the xml_template
                    //here's the rub ... the "sGroupNode" from the actual command instance might have xpath indexes > 1... 
                    //but the template DOESN'T!
                    //So, I'm regexing any [#] on the string back to a [1]... that value should be in the template.

                    Regex rx = new Regex(@"\[[0-9]*\]");
                    string sTemplateNode = rx.Replace(sGroupNode, "[1]");

                    XElement xGroupNode = xd.XPathSelectElement("//" + sTemplateNode);
                    if (xGroupNode == null) throw new Exception("Error: Unable to add.  Source node not found in Template XML. [" + sTemplateNode + "]");

                    //yeah, this wicked single line aggregates the string value of each node
                    string sNewXML = xGroupNode.Nodes().Aggregate("", (str, node) => str += node.ToString());

                    if (sNewXML != "")
                        ft.AddNodeToXMLColumn("task_step", "function_xml", "step_id = '" + sStepID + "'", "//" + sGroupNode, sNewXML);

                }
                else
                    throw new Exception("Error: Could not loop up XML for command type [" + sFunctionName + "].");

                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod(EnableSession = true)]
        public void wmFnNodeArrayRemove(string sStepID, string sXPathToDelete)
        {
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
            try
            {
                //string sErr = "";

                ////gotta have a valid index
                //if (iIndex < 1)
                //    throw new Exception("Unable to modify step. Invalid index.");

                if (sStepID.Length > 0)
                {
                    if (sXPathToDelete != "")
                    {
                        ////so, let's get the XML for this step...
                        //string sXML = ft.GetStepCommandXML(sStepID, ref sErr);
                        //if (sErr != "") throw new Exception(sErr);

                        string sNodeToRemove = "//" + sXPathToDelete;

                        //validate it
                        //XDocument xd = XDocument.Parse(sXML);
                        //if (xd == null) throw new Exception("Error: Unable to parse Function XML.");

                        ft.RemoveNodeFromXMLColumn("task_step", "function_xml", "step_id = '" + sStepID + "'", sNodeToRemove);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region "Parameters"
        [WebMethod(EnableSession = true)]
        public string wmGetTaskParam(string sType, string sID, string sParamID)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            if (!ui.IsGUID(sID))
                throw new Exception("Invalid or missing ID.");

            try
            {
                string sTable = "";

                if (sType == "ecosystem")
                    sTable = "ecosystem";
                else if (sType == "task")
                    sTable = "task";

                //default values if adding - get overridden if there is a record
                string sName = "";
                string sDesc = "";
                string sRequired = "false";
                string sPrompt = "true";
                string sEncrypt = "false";
                string sValuesHTML = "";
                string sPresentAs = "value";

                if (!string.IsNullOrEmpty(sParamID))
                {
                    string sErr = "";
                    string sXML = "";
                    string sSQL = "select parameter_xml" +
                            " from " + sTable +
                            " where " + sType + "_id = '" + sID + "'";

                    if (!dc.sqlGetSingleString(ref sXML, sSQL, ref sErr))
                        throw new Exception("Unable to get parameter_xml.  " + sErr);

                    if (sXML != "")
                    {
                        XDocument xd = XDocument.Parse(sXML);
                        if (xd == null) throw new Exception("XML parameter data is invalid.");

                        XElement xParameter = xd.XPathSelectElement("//parameter[@id = \"" + sParamID + "\"]");
                        if (xParameter == null) return "Error: XML does not contain parameter.";

                        XElement xName = xParameter.XPathSelectElement("name");
                        if (xName == null) return "Error: XML does not contain parameter name.";
                        XElement xDesc = xParameter.XPathSelectElement("desc");
                        if (xDesc == null) return "Error: XML does not contain parameter description.";

                        sName = xName.Value;
                        sDesc = xDesc.Value;

                        if (xParameter.Attribute("required") != null)
                            sRequired = xParameter.Attribute("required").Value;

                        if (xParameter.Attribute("prompt") != null)
                            sPrompt = xParameter.Attribute("prompt").Value;

                        if (xParameter.Attribute("encrypt") != null)
                            sEncrypt = xParameter.Attribute("encrypt").Value;


                        XElement xValues = xd.XPathSelectElement("//parameter[@id = \"" + sParamID + "\"]/values");
                        if (xValues != null)
                        {
                            if (xValues.Attribute("present_as") != null)
                                sPresentAs = xValues.Attribute("present_as").Value;

                            int i = 0;
                            IEnumerable<XElement> xVals = xValues.XPathSelectElements("value");
                            foreach (XElement xVal in xVals)
                            {
                                //since we can delete each item from the page it needs a unique id.
                                string sPID = "pv" + ui.NewGUID();

                                string sValue = xVal.Value;
								string sObscuredValue = "";
								
								if (dc.IsTrue(sEncrypt))
								{
									// 1) obscure the ENCRYPTED value and make it safe to be an html attribute
				                    // 2) return some stars so the user will know a value is there.
									sObscuredValue = "oev=\"" + ui.packJSON(sValue) + "\"";
									sValue = "";
								}

                                sValuesHTML += "<div id=\"" + sPID + "\">" +
                                    "<textarea class=\"param_edit_value\" rows=\"1\" " + sObscuredValue + ">" + sValue + "</textarea>";

                                if (i > 0)
                                {
                                    string sHideDel = (sPresentAs == "list" || sPresentAs == "dropdown" ? "" : " hidden");
                                    sValuesHTML += " <img class=\"param_edit_value_remove_btn pointer " + sHideDel + "\" remove_id=\"" + sPID + "\"" +
                                        " src=\"../images/icons/fileclose.png\" alt=\"\" />";
                                }

                                sValuesHTML += "</div>";

                                i++;
                            }
                        }
                        else
                        {
                            //if, when getting the parameter, there are no values... add one.  We don't want a parameter with no values
                            //AND - no remove button on this only value
                            sValuesHTML += "<div id=\"pv" + ui.NewGUID() + "\">" +
                                "<textarea class=\"param_edit_value\" rows=\"1\"></textarea></div>";
                        }
                    }
                    else
                    {
                        throw new Exception("Unable to get parameter details. Not found.");
                    }
                }
                else
                {
                    //if, when getting the parameter, there are no values... add one.  We don't want a parameter with no values
                    //AND - no remove button on this only value
                    sValuesHTML += "<div id=\"pv" + ui.NewGUID() + "\">" +
                        "<textarea class=\"param_edit_value\" rows=\"1\"></textarea></div>";
                }

                //this draws no matter what, if it's empty it's just an add dialog
                string sHTML = "";

                sHTML += "Name: <input type=\"text\" class=\"w95pct\" id=\"param_edit_name\"" +
                    " validate_as=\"variable\" value=\"" + sName + "\" />";

                sHTML += "Options:<div class=\"param_edit_options\">";
                sHTML += "<span class=\"ui-widget-content ui-corner-all param_edit_option\"><input type=\"checkbox\" id=\"param_edit_required\"" + (sRequired == "true" ? "checked=\"checked\"" : "") + " /> Required?</span>";
                sHTML += "<span class=\"ui-widget-content ui-corner-all param_edit_option\"><input type=\"checkbox\" id=\"param_edit_prompt\"" + (sPrompt == "true" ? "checked=\"checked\"" : "") + " /> Prompt?</span>";
                sHTML += "<span class=\"ui-widget-content ui-corner-all param_edit_option\"><input type=\"checkbox\" id=\"param_edit_encrypt\"" + (sEncrypt == "true" ? "checked=\"checked\"" : "") + " /> Encrypt?</span>";
                sHTML += "</div>";

                sHTML += "<br />Description: <br /><textarea id=\"param_edit_desc\" rows=\"2\">" + sDesc + "</textarea>";

                sHTML += "<div id=\"param_edit_values\">Values:<br />";
                sHTML += "Present As: <select id=\"param_edit_present_as\">";
                sHTML += "<option value=\"value\"" + (sPresentAs == "value" ? "selected=\"selected\"" : "") + ">Value</option>";
                sHTML += "<option value=\"list\"" + (sPresentAs == "list" ? "selected=\"selected\"" : "") + ">List</option>";
                sHTML += "<option value=\"dropdown\"" + (sPresentAs == "dropdown" ? "selected=\"selected\"" : "") + ">Dropdown</option>";
                sHTML += "</select>";

                sHTML += "<hr />" + sValuesHTML + "</div>";

                //if it's not available for this presentation type, it will get the "hidden" class but still be drawn
                string sHideAdd = (sPresentAs == "list" || sPresentAs == "dropdown" ? "" : " hidden");
                sHTML += "<div id=\"param_edit_value_add_btn\" class=\"pointer " + sHideAdd + "\">" +
                    "<img title=\"Add Another\" alt=\"\" src=\"../images/icons/edit_add.png\" style=\"width: 10px;" +
                    "   height: 10px;\" />( click to add a value )</div>";

                return sHTML;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        [WebMethod(EnableSession = true)]
        public string wmUpdateTaskParam(string sType, string sID, string sParamID,
            string sName, string sDesc,
            string sRequired, string sPrompt, string sEncrypt, string sPresentAs, string sValues)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();

            if (!ui.IsGUID(sID))
                throw new Exception("Invalid or missing ID.");

            string sErr = "";
            string sSQL = "";


            //we encoded this in javascript before the ajax call.
            //the safest way to unencode it is to use the same javascript lib.
            //(sometimes the javascript and .net libs don't translate exactly, google it.)
            sDesc = ui.unpackJSON(sDesc).Trim();

            //normalize and clean the values
            sRequired = (dc.IsTrue(sRequired) ? "true" : "false");
            sPrompt = (dc.IsTrue(sPrompt) ? "true" : "false");
            sEncrypt = (dc.IsTrue(sEncrypt) ? "true" : "false");
            sName = sName.Trim().Replace("'", "''");


            string sTable = "";
            string sXML = "";
            string sParameterXPath = "//parameter[@id = \"" + sParamID + "\"]";  //using this to keep the code below cleaner.

            if (sType == "ecosystem")
                sTable = "ecosystem";
            else if (sType == "task")
                sTable = "task";

            bool bParamAdd = false;
            //bool bParamUpdate = false;

            //if sParamID is empty, we are adding
            if (string.IsNullOrEmpty(sParamID))
            {
                sParamID = "p_" + ui.NewGUID();
                sParameterXPath = "//parameter[@id = \"" + sParamID + "\"]";  //reset this if we had to get a new id


                //does the task already have parameters?
                sSQL = "select parameter_xml from " + sTable + " where " + sType + "_id = '" + sID + "'";
                if (!dc.sqlGetSingleString(ref sXML, sSQL, ref sErr))
                    throw new Exception(sErr);

                string sAddXML = "<parameter id=\"" + sParamID + "\" required=\"" + sRequired + "\" prompt=\"" + sPrompt + "\" encrypt=\"" + sEncrypt + "\">" +
                    "<name>" + sName + "</name>" +
                    "<desc>" + sDesc + "</desc>" +
                    "</parameter>";

                if (string.IsNullOrEmpty(sXML))
                {
                    //XML doesn't exist at all, add it to the record
                    sAddXML = "<parameters>" + sAddXML + "</parameters>";

                    sSQL = "update " + sTable + " set " +
                        " parameter_xml = '" + sAddXML + "'" +
                        " where " + sType + "_id = '" + sID + "'";

                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                        throw new Exception(sErr);

                    bParamAdd = true;
                }
                else
                {
                    //XML exists, add the node to it
                    ft.AddNodeToXMLColumn(sTable, "parameter_xml", sType + "_id = '" + sID + "'", "//parameters", sAddXML);
                    bParamAdd = true;
                }
            }
            else
            {
                //update the node values
                ft.SetNodeValueinXMLColumn(sTable, "parameter_xml", sType + "_id = '" + sID + "'", sParameterXPath + "/name", sName);
                ft.SetNodeValueinXMLColumn(sTable, "parameter_xml", sType + "_id = '" + sID + "'", sParameterXPath + "/desc", sDesc);
                //and the attributes
                ft.SetNodeAttributeinXMLColumn(sTable, "parameter_xml", sType + "_id = '" + sID + "'", sParameterXPath, "required", sRequired);
                ft.SetNodeAttributeinXMLColumn(sTable, "parameter_xml", sType + "_id = '" + sID + "'", sParameterXPath, "prompt", sPrompt);
                ft.SetNodeAttributeinXMLColumn(sTable, "parameter_xml", sType + "_id = '" + sID + "'", sParameterXPath, "encrypt", sEncrypt);

                bParamAdd = false;
            }


            // not clean at all handling both tasks and ecosystems in the same method, but whatever.
            if (bParamAdd)
            {
                if (sType == "task") { ui.WriteObjectAddLog(Globals.acObjectTypes.Task, sID, "Parameter", "Added Parameter:" + sName ); };
                if (sType == "ecosystem") { ui.WriteObjectAddLog(Globals.acObjectTypes.Ecosystem, sID, "Parameter", "Added Parameter:" + sName); };
            }
            else
            {
                // would be a lot of trouble to add the from to, why is it needed you have each value in the log, just scroll back
                // so just add a changed message to the log
                if (sType == "task") { dc.addSecurityLog(ui.GetSessionUserID(), Globals.SecurityLogTypes.Object, Globals.SecurityLogActions.ObjectModify, Globals.acObjectTypes.Task, sID, "Parameter Changed:[" + sName + "]", ref sErr); };
                if (sType == "ecosystem") { dc.addSecurityLog(ui.GetSessionUserID(), Globals.SecurityLogTypes.Object, Globals.SecurityLogActions.ObjectModify, Globals.acObjectTypes.Ecosystem, sID, "Parameter Changed:[" + sName + "]", ref sErr); };
            }

            //update the values
            string[] aValues = sValues.Split('|');
            string sValueXML = "";

            foreach (string sVal in aValues)
            {
                string sReadyValue = "";
				
				//if encrypt is true we MIGHT want to encrypt this value.
				//but it might simply be a resubmit of an existing value in which case we DON'T
				//if it has oev: as a prefix, it needs no additional work
				if (dc.IsTrue(sEncrypt))
				{
					if (sVal.IndexOf("oev:") > -1)
						sReadyValue = sVal.Replace("oev:", "");
					else
						sReadyValue = dc.EnCrypt(ui.unpackJSON(sVal));						
				} else {
					sReadyValue = ui.unpackJSON(sVal);						
				}
				
                sValueXML += "<value id=\"pv_" + ui.NewGUID() + "\">" + sReadyValue + "</value>";
            }

            sValueXML = "<values present_as=\"" + sPresentAs + "\">" + sValueXML + "</values>";


            //whack-n-add
            ft.RemoveNodeFromXMLColumn(sTable, "parameter_xml", sType + "_id = '" + sID + "'", sParameterXPath + "/values");
            ft.AddNodeToXMLColumn(sTable, "parameter_xml", sType + "_id = '" + sID + "'", sParameterXPath, sValueXML);

            return "";
        }
        [WebMethod(EnableSession = true)]
        public string wmDeleteTaskParam(string sType, string sID, string sParamID)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();

            string sErr = "";
            string sSQL = "";
            string sTable = "";

            if (sType == "ecosystem")
                sTable = "ecosystem";
            else if (sType == "task")
                sTable = "task";

            if (!string.IsNullOrEmpty(sParamID) && ui.IsGUID(sID))
            {
                // need the name and values for logging
                string sXML = "";

                sSQL = "select parameter_xml" +
                    " from " + sTable +
                    " where " + sType + "_id = '" + sID + "'";

                if (!dc.sqlGetSingleString(ref sXML, sSQL, ref sErr))
                    throw new Exception("Unable to get parameter_xml.  " + sErr);

                if (sXML != "")
                {
                    XDocument xd = XDocument.Parse(sXML);
                    if (xd == null) throw new Exception("XML parameter data is invalid.");

                    XElement xName = xd.XPathSelectElement("//parameter[@id = \"" + sParamID + "\"]/name");
                    string sName = (xName == null ? "" : xName.Value);
                    XElement xValues = xd.XPathSelectElement("//parameter[@id = \"" + sParamID + "\"]/values");
                    string sValues = (xValues == null ? "" : xValues.ToString());

                    // add security log
                    ui.WriteObjectDeleteLog(Globals.acObjectTypes.Parameter, "", sID, "");

                    if (sType == "task") { ui.WriteObjectChangeLog(Globals.acObjectTypes.Task, sID, "Deleted Parameter:[" + sName + "]", sValues); };
                    if (sType == "ecosystem") { ui.WriteObjectChangeLog(Globals.acObjectTypes.Ecosystem, sID, "Deleted Parameter:[" + sName + "]", sValues); };
                }


                //do the whack
                ft.RemoveNodeFromXMLColumn(sTable, "parameter_xml", sType + "_id = '" + sID + "'", "//parameter[@id = \"" + sParamID + "\"]");


                return "";
            }
            else
            {
                throw new Exception("Invalid or missing Task or Parameter ID.");
            }
        }
		/*
		 * wmGetParameters returns a presentable, HTML list of parameters.
		 * it does appear on the Task Edit page, and is editable...
		 * ...but it's editable in a popup for a single value... not anything done here.
		 * 
		 * all we do here is give the label a pointer if it's editable.
		 * 
		 * */
        [WebMethod(EnableSession = true)]
        public string wmGetParameters(string sType, string sID, bool bEditable, bool bSnipValues)
        {
            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sErr = "";
                string sParameterXML = "";
                string sSQL = "";
                string sTable = "";

                if (sType == "ecosystem")
                    sTable = "ecosystem";
                else if (sType == "task")
                    sTable = "task";

                sSQL = "select parameter_xml from " + sTable + " where " + sType + "_id = '" + sID + "'";

                if (!dc.sqlGetSingleString(ref sParameterXML, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }

                if (sParameterXML != "")
                {
                    XDocument xDoc = XDocument.Parse(sParameterXML);
                    if (xDoc == null)
                        throw new Exception("Parameter XML data for " + sType + " [" + sID + "] is invalid.");

                    XElement xParams = xDoc.XPathSelectElement("/parameters");
                    if (xParams == null)
                        throw new Exception("Parameter XML data for " + sType + " [" + sID + "] does not contain 'parameters' root node.");


                    string sHTML = "";

                    foreach (XElement xParameter in xParams.XPathSelectElements("parameter"))
                    {
                        string sPID = xParameter.Attribute("id").Value;
                        string sName = xParameter.Element("name").Value;
                        string sDesc = xParameter.Element("desc").Value;

                        bool bEncrypt = false;
                        if (xParameter.Attribute("encrypt") != null)
                            bEncrypt = dc.IsTrue(xParameter.Attribute("encrypt").Value);


                        sHTML += "<div class=\"parameter\">";
                        sHTML += "  <div class=\"ui-state-default parameter_header\">";

                        sHTML += "<div class=\"step_header_title\"><span class=\"parameter_name";
                        sHTML += (bEditable ? " pointer" : ""); //make the name a pointer if it's editable
                        sHTML += "\" id=\"" + sPID + "\">";
                        sHTML += sName;
                        sHTML += "</span></div>";

                        sHTML += "<div class=\"step_header_icons\">";
                        sHTML += "<img class=\"parameter_help_btn pointer trans50\"" +
                            " src=\"../images/icons/info.png\" alt=\"\" style=\"width: 12px; height: 12px;\"" +
                            " title=\"" + sDesc.Replace("\"", "") + "\" />";

                        if (bEditable)
                        {
                            sHTML += "<img class=\"parameter_remove_btn pointer\" remove_id=\"" + sPID + "\"" +
                                " src=\"../images/icons/fileclose.png\" alt=\"\" style=\"width: 12px; height: 12px;\" />";
                        }

                        sHTML += "</div>";
                        sHTML += "</div>";


                        sHTML += "<div class=\"ui-widget-content ui-corner-bottom clearfloat parameter_detail\">";

                        //desc - a short snip is shown here... 75 chars.

                        //if (!string.IsNullOrEmpty(sDesc))
                        //    if (bSnipValues)
                        //        sDesc = ui.GetSnip(sDesc, 75);
                        //    else
                        //        sDesc = ui.FixBreaks(sDesc);
                        //sHTML += "<div class=\"parameter_desc hidden\">" + sDesc + "</div>";


                        //values
                        XElement xValues = xParameter.XPathSelectElement("values");
                        if (xValues != null)
                        {
                            foreach (XElement xValue in xValues.XPathSelectElements("value"))
                            {
                                string sValue = (string.IsNullOrEmpty(xValue.Value) ? "" : xValue.Value);

                                //only show stars IF it's encrypted, but ONLY if it has a value
                                if (bEncrypt && !string.IsNullOrEmpty(sValue))
									sValue = "********";
								else
                                    if (bSnipValues)
                                        sValue = ui.GetSnip(xValue.Value, 64);
                                    else
                                        sValue = ui.FixBreaks(xValue.Value);

                                sHTML += "<div class=\"ui-widget-content ui-corner-tl ui-corner-bl parameter_value\">" + sValue + "</div>";
                            }
                        }

                        sHTML += "</div>";
                        sHTML += "</div>";

                    }

                    return sHTML;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            //it may just be there are no parameters
            return "";
        }


        /*
         This gets the complete XML parameter document, for the purposes of launching or saving defaults.
         For Tasks, it's taken right from the task table...
         For others, it's merged in with the saved default values.
         * 
         * CRITICAL REMINDER NOTE!!!!
         * 
         * We ALWAYS wanna do merging unless you're looking at the task.
         * Why?  because the task could have had new parameters added since you saved them on the other object.
         * therefore, we always merge in order to expose those new parameters.
        */
        [WebMethod(EnableSession = true)]
        public string wmGetParameterXML(string sType, string sID, string sFilterByEcosystemID)
        {
            if (sType == "task")
                return wmGetObjectParameterXML(sType, sID, "");
            else
                return wmGetMergedParameterXML(sType, sID, sFilterByEcosystemID); //Merging is happening here!
        }


        ///*
        // * This method does MERGING!
        // * 
        // * It gets the XML for the Task, and additionally get the XML for the other record,
        // * and merges them together, using the values from one to basically select values in 
        // * the master task XML.
        // * 
        // * */
        [WebMethod(EnableSession = true)]
        public string wmGetMergedParameterXML(string sType, string sID, string sEcosystemID)
        {

            dataAccess dc = new dataAccess();
            acUI.acUI ui = new acUI.acUI();
            taskMethods tm = new taskMethods();
            FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();

            if (string.IsNullOrEmpty(sID))
                throw new Exception("ID required to look up default Parameter values.");

            string sErr = "";

            //what is the task associated with this action?
            //and get the XML for it
            string sSQL = "";
            string sDefaultsXML = "";
            string sTaskID = "";

            if (sType == "action")
            {
                sDefaultsXML = tm.wmGetObjectParameterXML(sType, sID, "");

                sSQL = "select t.task_id" +
                     " from ecotemplate_action ea" +
                     " join task t on ea.original_task_id = t.original_task_id" +
                     " and t.default_version = 1" +
                     " where ea.action_id = '" + sID + "'";
            }
            else if (sType == "instance")
            {
                sDefaultsXML = tm.wmGetObjectParameterXML(sType, sID, sEcosystemID);

                //IMPORTANT!!! if the ID is not a guid, it's a specific instance ID, and we'll need to get the task_id
                //but if it is a GUID, but the type is "instance", taht means the most recent INSTANCE for this TASK_ID
                if (ui.IsGUID(sID))
                    sTaskID = sID;
                else
                    sSQL = "select task_id" +
                         " from task_instance" +
                         " where task_instance = '" + sID + "'";
            }
            else if (sType == "plan")
            {
                sDefaultsXML = tm.wmGetObjectParameterXML(sType, sID, "");

                sSQL = "select task_id" +
                    " from action_plan" +
                    " where plan_id = '" + sID + "'";
            }
            else if (sType == "schedule")
            {
                sDefaultsXML = tm.wmGetObjectParameterXML(sType, sID, "");

                sSQL = "select task_id" +
                    " from action_schedule" +
                    " where schedule_id = '" + sID + "'";
            }

            //if we didn't get a task id directly, use the SQL to look it up
            if (string.IsNullOrEmpty(sTaskID))
                if (!dc.sqlGetSingleString(ref sTaskID, sSQL, ref sErr))
                    throw new Exception(sErr);

            if (!ui.IsGUID(sTaskID))
                throw new Exception("Unable to find Task ID for record.");


            XDocument xTPDoc = new XDocument();
            XDocument xDefDoc = new XDocument();

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


            //we populated this up above too
            if (!string.IsNullOrEmpty(sDefaultsXML))
            {
                xDefDoc = XDocument.Parse(sDefaultsXML);
                if (xDefDoc == null)
                    throw new Exception("Defaults XML data is invalid.");

                XElement xDefParams = xDefDoc.XPathSelectElement("/parameters");
                if (xDefParams == null)
                    throw new Exception("Defaults XML data does not contain 'parameters' root node.");
            }

            //spin the nodes in the DEFAULTS xml, then dig in to the task XML and UPDATE the value if found.
            //(if the node no longer exists, delete the node from the defaults xml IF IT WAS AN ACTION)
            //and default "values" take precedence over task values.
            foreach (XElement xDefault in xDefDoc.XPathSelectElements("//parameter"))
            {
				//nothing to do if it's empty
				if (xDefault == null)
					break;

				//look it up in the task param xml
                XElement xDefName = xDefault.XPathSelectElement("name");
                string sDefName = (xDefName == null ? "" : xDefName.Value);
                XElement xDefValues = xDefault.XPathSelectElement("values");
				
				//nothing to do if there is no values node...
				if (xDefValues == null)
					break;
				//or if it contains no values.
				if (!xDefValues.HasElements)
					break;
				//or if there is no parameter name
				if (string.IsNullOrEmpty(sDefName))
					break;
				
				
				//so, we have some valid data in the defaults xml... let's merge!
				
				//we have the name of the parameter... go find it in the TASK param XML
                XElement xTaskParam = xTPDoc.XPathSelectElement("//parameter/name[. = '" + sDefName + "']/..");  //NOTE! the /.. gets the parent of the name node!

                //if it doesn't exist in the task params, remove it from this document, permanently
                //but only for action types... instance data is historical and can't be munged
                if (xTaskParam == null && sType == "action")
                {
                    ft.RemoveNodeFromXMLColumn("ecotemplate_action", "parameter_defaults", "action_id = '" + sID + "'", "//parameter/name[. = '" + sDefName + "']/..");
                    continue;
                }


				//is this an encrypted parameter?
				string sEncrypt = "";
				if (xTaskParam.Attribute("encrypt") != null)
                	sEncrypt = xTaskParam.Attribute("encrypt").Value;

 
				//and the "values" collection will be the 'next' node
                XElement xTaskParamValues = xTaskParam.XPathSelectElement("values");

                string sPresentAs = xTaskParamValues.Attribute("present_as").Value;
                if (sPresentAs == "dropdown")
                {
                    //dropdowns get a "selected" indicator
                    string sValueToSelect = xDefValues.XPathSelectElement("value").Value;

                    //find the right one by value and give it the "selected" attribute.
                    XElement xVal = xTaskParamValues.XPathSelectElement("value[. = '" + sValueToSelect + "']");
                    if (xVal != null)
                        xVal.SetAttributeValue("selected", "true");
                }
                else if (sPresentAs == "list")
                {
                    //first, a list gets ALL the values replaced...
                    xTaskParamValues.ReplaceNodes(xDefValues);
               	}
                else
                {
					//IMPORTANT NOTE:
					//remember... both these XML documents came from wmGetObjectParameterXML...
					//so any encrypted data IS ALREADY OBFUSCATED and base64'd in the oev attribute.
					
                    //it's a single value, so just replace it with the default.
                    XElement xVal = xTaskParamValues.XPathSelectElement("value[1]");
                    if (xVal != null)
					{
						//if this is an encrypted parameter, we'll be replacing (if a default exists) the oev attribute
						//AND the value... don't want them to get out of sync!
						if (dc.IsTrue(sEncrypt))
						{
							if (xDefValues.XPathSelectElement("value") != null)
								if (xDefValues.XPathSelectElement("value").Attribute("oev") != null)
								{
									xVal.SetAttributeValue("oev", xDefValues.XPathSelectElement("value").Attribute("oev").Value);
									xVal.Value = xDefValues.XPathSelectElement("value").Value;
								}
						}
						else
						{
							//not encrypted, just replace the value.
							if (xDefValues.XPathSelectElement("value") != null)
								xVal.Value = xDefValues.XPathSelectElement("value").Value;
						}
					}
				}
            }

            return xTPDoc.ToString(SaveOptions.DisableFormatting); ;

        }



        ///*
        // This method simply gets the XML directly from the db for the type.
        // It may be different by type!

        // The schema should be the same, but some documents (task) are complete, while
        // others (action, instance) are JUST VALUES, not the complete document.
        //*/
        [WebMethod(EnableSession = true)]
        public string wmGetObjectParameterXML(string sType, string sID, string sFilterByEcosystemID)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sErr = "";
                string sParameterXML = "";
                string sSQL = "";

                if (sType == "instance")
                {
                    //if sID is a guid, it's a task_id ... get the most recent run
                    //otherwise assume it's a task_instance and try that.
                    if (ui.IsGUID(sID))
                        sSQL = "select parameter_xml from task_instance_parameter where task_instance = " +
                            "(select max(task_instance) from task_instance where task_id = '" + sID + "')";
                    else if (ui.IsGUID(sFilterByEcosystemID))  //but if there's an ecosystem_id, limit it to that
                        sSQL = "select parameter_xml from task_instance_parameter where task_instance = " +
                            "(select max(task_instance) from task_instance where task_id = '" + sID + "')" +
                            " and ecosystem_id = '" + sFilterByEcosystemID + "'";
                    else
                        sSQL = "select parameter_xml from task_instance_parameter where task_instance = '" + sID + "'";
                }
                else if (sType == "action")
                {
                    sSQL = "select parameter_defaults from ecotemplate_action where action_id = '" + sID + "'";
                }
                else if (sType == "plan")
                {
                    sSQL = "select parameter_xml from action_plan where plan_id = " + sID;
                }
                else if (sType == "schedule")
                {
                    sSQL = "select parameter_xml from action_schedule where schedule_id = '" + sID + "'";
                }
                else if (sType == "task")
                {
                    sSQL = "select parameter_xml from task where task_id = '" + sID + "'";
                }

                if (!dc.sqlGetSingleString(ref sParameterXML, sSQL, ref sErr))
                {
                    throw new Exception(sErr);
                }

                if (!string.IsNullOrEmpty(sParameterXML))
                {
                    XDocument xDoc = XDocument.Parse(sParameterXML);
                    if (xDoc == null)
                        throw new Exception("Parameter XML data for [" + sType + ":" + sID + "] is invalid.");

                    XElement xParams = xDoc.XPathSelectElement("/parameters");
                    if (xParams == null)
                        throw new Exception("Parameter XML data for[" + sType + ":" + sID + "] does not contain 'parameters' root node.");
					
                    //NOTE: some values on this document may have a "encrypt" attribute.
					//If so, we will:
					// 1) obscure the ENCRYPTED value and make it safe to be an html attribute
                    // 2) return some stars so the user will know a value is there.
                    foreach (XElement xEncryptedValue in xDoc.XPathSelectElements("//parameter[@encrypt='true']/values/value"))
                    {
						//if the value is empty, it still gets an oev attribute
						string sVal = (string.IsNullOrEmpty(xEncryptedValue.Value) ? "" : ui.packJSON(xEncryptedValue.Value));
						xEncryptedValue.SetAttributeValue("oev", sVal);
						//but it only gets stars if it has a value
						if (!string.IsNullOrEmpty(sVal))
                        	xEncryptedValue.Value = "********";
                    }

                    return xDoc.ToString(SaveOptions.DisableFormatting);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            //it may just be there are no parameters
            return "";
        }



        #endregion

        [WebMethod(EnableSession = true)]
        public string wmGetAccountEcosystems(string sAccountID, string sEcosystemID)
        {
            dataAccess dc = new dataAccess();

            string sHTML = "";

            try
            {
                //if the ecosystem passed in isn't really there, make it "" so we can compare below.
                if (string.IsNullOrEmpty(sEcosystemID))
                    sEcosystemID = "";

                if (!string.IsNullOrEmpty(sAccountID))
                {
                    string sErr = "";
                    string sSQL = "select ecosystem_id, ecosystem_name from ecosystem" +
                         " where account_id = '" + sAccountID + "'" +
                         " order by ecosystem_name"; ;

                    DataTable dtEcosystems = new DataTable();
                    if (dc.sqlGetDataTable(ref dtEcosystems, sSQL, ref sErr))
                    {
                        if (dtEcosystems.Rows.Count > 0)
                        {
                            sHTML += "<option value=''></option>";

                            foreach (DataRow dr in dtEcosystems.Rows)
                            {
                                string sSelected = (sEcosystemID == dr["ecosystem_id"].ToString() ? "selected=\"selected\"" : "");
                                sHTML += "<option value=\"" + dr["ecosystem_id"].ToString() + "\" " + sSelected + ">" + dr["ecosystem_name"].ToString() + "</option>";
                            }
                        }
                    };
                }

                return sHTML;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void wmSaveTaskUserSetting(string sTaskID, string sSettingKey, string sSettingValue)
        {
            dataAccess dc = new dataAccess();

            acUI.acUI ui = new acUI.acUI();

            try
            {
                string sUserID = ui.GetSessionUserID();

                if (ui.IsGUID(sTaskID) && ui.IsGUID(sUserID))
                {
                    //1) get the settings
                    //2) update/add the appropriate value
                    //3) update the settings to the db

                    string sSettingXML = "";
                    string sErr = "";
                    string sSQL = "select settings_xml from users where user_id = '" + sUserID + "'";

                    if (!dc.sqlGetSingleString(ref sSettingXML, sSQL, ref sErr))
                    {
                        throw new Exception("Unable to get settings for user." + sErr);
                    }

                    if (sSettingXML == "")
                        sSettingXML = "<settings><debug><tasks></tasks></debug></settings>";

                    XDocument xDoc = XDocument.Parse(sSettingXML);
                    if (xDoc == null) throw new Exception("XML settings data for user is invalid.");


                    //we have to analyze the doc and see if the appropriate section exists.
                    //if not, we need to construct it
                    if (xDoc.Element("settings").Descendants("debug").Count() == 0)
                        xDoc.Element("settings").Add(new XElement("debug"));

                    if (xDoc.Element("settings").Element("debug").Descendants("tasks").Count() == 0)
                        xDoc.Element("settings").Element("debug").Add(new XElement("tasks"));

                    XElement xTasks = xDoc.Element("settings").Element("debug").Element("tasks");


                    //to search by attribute we must get back an array and we shouldn't have an array anyway
                    //so to be safe and clean, delete all matches and just add back the one we want
                    xTasks.Descendants("task").Where(
                        x => (string)x.Attribute("task_id") == sTaskID).Remove();


                    //add it
                    XElement xTask = new XElement("task");
                    xTask.Add(new XAttribute("task_id", sTaskID));
                    xTask.Add(new XAttribute(sSettingKey, sSettingValue));

                    xTasks.Add(xTask);


                    sSQL = "update users set settings_xml = '" + xDoc.ToString(SaveOptions.DisableFormatting) + "'" +
                        " where user_id = '" + sUserID + "'";
                    if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    {
                        throw new Exception("Unable to save Task User Setting." + sErr);
                    }

                    return;
                }
                else
                {
                    throw new Exception("Unable to run task. Missing or invalid task [" + sTaskID + "] or unable to get current user.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
