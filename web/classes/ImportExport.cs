using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Configuration;
using System.IO;
using Globals;
using System.Collections;
using System.Data;
using System.Data.Odbc;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Xml.XPath;
using ICSharpCode.SharpZipLib.Zip;


/*
 panel_id, item_id, item_type, item, parent, child, next, previous," +
                            " value_1, value_2, value_3, value_4, value_5, value_6, value_7, last_update," +
                            " font-family, font-style, font-variant, font-weight, font-size, font," +
                            " text-decoration, vertical-align, text-transform, text-align, text-indent," +
                            " line-height, margin, padding, border, width, height, float_side," +
                            " top_offset, bottom_offset, left_offset, right_offset," +
                            " clear, color, background_color, display, position
 */
namespace ImportExport
{
    public class ImportExportClass
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();

        //a global placeholder for building the manifest
        XDocument xManifest = XDocument.Parse("<files></files>");

        #region "Import"
        public bool doFileLoad(string sPath, string sFullPath, string sZipFileName, string sUserID, ref string sErr)
        {
			try 
			{
	            string sSQL = "";
	
	            //FIRST THINGS FIRST - clean up any crap for this user in the import tables
	            sSQL = "delete from import_task_step where user_id = '" + sUserID + "'";
	            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
	                return false;
	            sSQL = "delete from import_task_codeblock where user_id = '" + sUserID + "'";
	            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
	                return false;
	            sSQL = "delete from import_task where user_id = '" + sUserID + "'";
	            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
	                return false;
	
	            //Now, import the files 
	            string sManifestName = "";
	
	            //first, unzip the package
	            if (!File.Exists(sFullPath))
	            {
	                sErr = "Cannot find backup file '" + sFullPath + "'";
	                return false;
	            }

	            using (ZipInputStream s = new ZipInputStream(File.OpenRead(sFullPath)))
	            {
	                ZipEntry zEntry;
	                while ((zEntry = s.GetNextEntry()) != null)
	                {
	                    //string sDirectoryName = Path.GetDirectoryName(zEntry.Name);
	                    string sEntryFileName = Path.GetFileName(zEntry.Name);
	
	                    // create directory
	                    //if (sDirectoryName.Length > 0)
	                    //    Directory.CreateDirectory(sDirectoryName);
	
	                    if (sEntryFileName != String.Empty)
	                    {
	                        if (sEntryFileName.IndexOf("manifest") > -1)
	                            sManifestName = sEntryFileName;
	
	                        using (FileStream sw = File.Create(sPath + "/" + sEntryFileName))
	                        {
	                            int iSize = 2048;
	                            byte[] data = new byte[2048];
	                            while (true)
	                            {
	                                iSize = s.Read(data, 0, data.Length);
	                                if (iSize > 0)
	                                {
	                                    sw.Write(data, 0, iSize);
	                                }
	                                else
	                                {
	                                    break;
	                                }
	                            }
	                        }
	                    }
	                }
	            }
	
	            if (sManifestName == "")
	            {
	                sErr = "Unable to find manifest.xml file in backup.";
	                return false;
	            }
            
				XDocument xManifest = XDocument.Load(sPath + sManifestName);

	            if (xManifest == null)
	            {
	                sErr = "XML manifest is empty.";
	                return false;
	            }
	
	            XElement xe = xManifest.XPathSelectElement("files");
	            foreach (XElement xFile in xe.XPathSelectElements("file"))
	            {
	                if (xFile.Value.IndexOf("tasks") > -1)
	                {
	                    if (!doLoadTasks(sPath + xFile.Value, sUserID, ref sErr)) return false;
	                    continue;
	                }
	                if (xFile.Value.IndexOf("codeblocks") > -1)
	                {
	                    if (!doLoadTaskCodeblocks(sPath + xFile.Value, sUserID, ref sErr)) return false;
	                    continue;
	                }
	                if (xFile.Value.IndexOf("steps") > -1)
	                {
	                    if (!doLoadTaskSteps(sPath + xFile.Value, sUserID, ref sErr)) return false;
	                    continue;
	                }
	            }
	
	            //whack the files using the manifest
	            foreach (XElement xFile in xe.XPathSelectElements("file"))
	            {
	                File.Delete(sPath + xFile.Value);
	            }
	            //whack the manifest
	            File.Delete(sPath + sManifestName);
	            //whack the zip file
	            File.Delete(sFullPath);
	
	            return true;
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
				return false;            
			}
			

        }

        private bool doLoadTasks(string sXMLFile, string sUserID, ref string sErr)
        {
            string sSQL = "";
            XDocument xDoc = XDocument.Load(sXMLFile);

            if (xDoc == null)
            {
                sErr = "XML data for tasks is empty.";
                return false;
            }
            //did a lot of reading on this.
            //can't find a better way that fits our needs
            //so we'll just hard code this, and expect that we know the structure of the XML
            //(since we created it)

            //THIS IS NOT DYNAMIC
            //sure, I could read the XML and create the tables based on the data, but why?
            XElement xe = xDoc.XPathSelectElement("tasks");
			if (xe == null) {
				sErr = "Tasks: Unable to continue - xml does not contain any records.";
                return false;
			}
            foreach (XElement xTask in xe.XPathSelectElements("task"))
            {
                //gotta handle nulls for the XML fields
                string sParameterXML = "";
                if (xTask.Element("parameter_xml").FirstNode != null)
                    sParameterXML = xTask.Element("parameter_xml").FirstNode.ToString().Replace("'", "''");

                sSQL = "insert into import_task " +
                    "(user_id, task_id, original_task_id, version, task_code, task_name, task_desc, task_status," +
                    " use_connector_system, default_version, concurrent_instances, queue_depth," +
                    " parameter_xml, created_dt, src_task_code, src_task_name, src_version)" +
                    " values (" +
                    "'" + sUserID + "'," +
                    "'" + xTask.Element("task_id").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("original_task_id").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("version").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("task_code").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("task_name").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("task_desc").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("task_status").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("use_connector_system").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("default_version").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("concurrent_instances").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("queue_depth").Value.Replace("'", "''") + "'," +
                    "'" + sParameterXML + "'," +
                    "str_to_date('" + xTask.Element("created_dt").Value.Replace("'", "''") + "','%m/%d/%Y %T')," +
                    "'" + xTask.Element("task_code").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("task_name").Value.Replace("'", "''") + "'," +
                    "'" + xTask.Element("version").Value.Replace("'", "''") + "'" +
                    ")";
            	
				if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    return false;
            }
            
			//scan the import table for any issues and update messages
            if (!IdentifyTaskConflicts(sUserID, "", ref sErr))
                return false;
            
            return true;
        }
        private bool doLoadTaskCodeblocks(string sXMLFile, string sUserID, ref string sErr)
        {
            string sSQL = "";
            XDocument xDoc = XDocument.Load(sXMLFile);

            if (xDoc == null)
            {
                sErr = "XML data for task_codeblock is empty.";
                return false;
            }

            XElement xe = xDoc.XPathSelectElement("codeblocks");
			if (xe == null) {
				sErr = "Codeblocks: Unable to continue - xml does not contain any records.";
                return false;
			}
            foreach (XElement xCB in xe.XPathSelectElements("codeblock"))
            {
                sSQL = "insert into import_task_codeblock " +
                    "(user_id, task_id, codeblock_name)" +
                    " values (" +
                    "'" + sUserID + "'," +
                    "'" + xCB.Element("task_id").Value.Replace("'", "''") + "'," +
                    "'" + xCB.Element("codeblock_name").Value.Replace("'", "''") + "'" +
                    ");" + Environment.NewLine;
                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    return false;
            }

            return true;
        }
        private bool doLoadTaskSteps(string sXMLFile, string sUserID, ref string sErr)
        {
            string sSQL = "";
            XDocument xDoc = XDocument.Load(sXMLFile);

            if (xDoc == null)
                sErr = "XML data for task_step is empty.";

            XElement xe = xDoc.XPathSelectElement("steps");
			if (xe == null) {
				sErr = "Steps: Unable to continue - xml does not contain any records.";
                return false;
			}
            foreach (XElement xStep in xe.XPathSelectElements("step"))
            {
                //since we have stored XML in XML, we have to get the "first node" of any of our XML fields.
                string sVariableXML = "";
                if (xStep.Element("variable_xml").FirstNode != null)
                    sVariableXML = xStep.Element("variable_xml").FirstNode.ToString().Replace("'", "''");


                sSQL = "insert into import_task_step " +
                    "(user_id, step_id, task_id, codeblock_name, step_order, commented, locked, function_name, function_xml, step_desc, output_parse_type, output_row_delimiter, output_column_delimiter, variable_xml)" +
                    " values (" +
                    "'" + sUserID + "'," +
                    "'" + xStep.Element("step_id").Value.Replace("'", "''") + "'," +
                    "'" + xStep.Element("task_id").Value.Replace("'", "''") + "'," +
                    "'" + xStep.Element("codeblock_name").Value.Replace("'", "''") + "'," +
                    "'" + xStep.Element("step_order").Value.Replace("'", "''") + "'," +
                    "'" + xStep.Element("commented").Value.Replace("'", "''") + "'," +
                    "'" + xStep.Element("locked").Value.Replace("'", "''") + "'," +
                    "'" + xStep.Element("function_name").Value.Replace("'", "''") + "'," +
                    "'" + xStep.Element("function_xml").FirstNode.ToString().Replace("'", "''") + "'," +
                    "'" + xStep.Element("step_desc").Value.Replace("'", "''") + "'," +
                    "'" + xStep.Element("output_parse_type").Value.Replace("'", "''") + "'," +
                    "'" + xStep.Element("output_row_delimiter").Value.Replace("'", "''") + "'," +
                    "'" + xStep.Element("output_column_delimiter").Value.Replace("'", "''") + "'," +
                    "'" + sVariableXML + "'" +
                    ");" + Environment.NewLine;
                if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                    return false;
            }

            return true;
        }

        #endregion

        #region "Export"
        public bool doBatchTaskExport(string sPath, string sOriginalTaskIDs, string sUniqueName, ref string sErrorMessage)
        {
            try
            {
                string sSQL = "";
                string sErr = "";

                /* some of these tasks may have steps that call other tasks.

                 * rather than deal with it at the step level,
                 * just scan all the steps now.
                 * if there are any subtasks or run_tasks, 
                 * SIMPLY APPEND THEM TO THE sOriginalTaskIDs 
                */
                GetEmbeddedTasks(ref sOriginalTaskIDs, ref sErr);


                //I know it's against the rules, but "select *" has an advantage as the
                //database evolves, that we won't miss new columns or forget to update this code.

                //TASK
                sSQL = "select *" +
                    " from task" +
                    " where original_task_id in (" + sOriginalTaskIDs + ")" +
                    " and default_version = 1";
                if (!writeXMLFile(sPath, sUniqueName, "tasks", sSQL, ref sErr))
                    throw new Exception("Unable to generate Task export file." + sErr);

                //CODEBLOCKS
                sSQL = "select tc.*" +
                    " from task_codeblock tc" +
                    " join task t on tc.task_id = t.task_id" +
                    " where t.original_task_id in (" + sOriginalTaskIDs + ")" +
                    " and t.default_version = 1";
                if (!writeXMLFile(sPath, sUniqueName, "codeblocks", sSQL, ref sErr))
                    throw new Exception("Unable to generate Codeblock export file." + sErr);

                //TASK_STEP
                sSQL = "select ts.*" +
                    " from task_step ts" +
                    " join task t on ts.task_id = t.task_id" +
                    " where t.original_task_id in (" + sOriginalTaskIDs + ")" +
                    " and t.default_version = 1";
                if (!writeXMLFile(sPath, sUniqueName, "steps", sSQL, ref sErr))
                    throw new Exception("Unable to generate Steps export file." + sErr);



                //ALL DONE - write the manifest file
                string sManifestFile = sPath + sUniqueName + "_manifest.xml";

                FileStream fsManifest = null;
                fsManifest = File.Open(sManifestFile, FileMode.Create);

                byte[] baXML = Encoding.ASCII.GetBytes(xManifest.ToString());
                fsManifest.Write(baXML, 0, baXML.Length);
                fsManifest.Close();

                //zip it all up
                //right now using dir name as the file name, this will probable be the task/proc name.
                //cleaned up with no spaces, etc.
                makeZip(sPath, sUniqueName);

                //delete the xml files
                string[] sFiles = Directory.GetFiles(sPath, sUniqueName + "*.xml");
                foreach (string sFileToDel in sFiles)
                    File.Delete(sFileToDel);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return true;
        }
        private void GetEmbeddedTasks(ref string sOriginalTaskIDs, ref string sErr)
        {
            //this is recursive, but the SQL excludes any tasks already in the list
            //so it should never spin out of control.
            string sSQL = "";

            sSQL = "select ExtractValue(function_xml, '(//original_task_id)')" +
                " from task_step ts" +
                " join task t on ts.task_id = t.task_id" +
                " where t.original_task_id in (" + sOriginalTaskIDs + ")" +
                    " and t.default_version = 1" +
                " and ts.function_name in ('subtask','run_task')" +
                " and ifnull(ExtractValue(function_xml, '(//original_task_id)'), '') <> ''" +
                " and ExtractValue(function_xml, '(//original_task_id)') not in (" + sOriginalTaskIDs + ")";

            string sMoreIDs = "";
            if (!dc.csvGetList(ref sMoreIDs, sSQL, ref sErr, true))
                throw new Exception(sErr);

            if (sMoreIDs.Length > 0)
            {
                //this is recursive... if there are more id's, we need to check them as well
                sOriginalTaskIDs += "," + sMoreIDs;

                GetEmbeddedTasks(ref sOriginalTaskIDs, ref sErr);
            }
        }
        private bool writeXMLFile(string sPath, string sFileName, string sType, string sSQL, ref string sErr)
        {

            DataTable dt = new DataTable();

            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                return false;

            try
            {
                if (dt.Rows.Count > 0)
                {
                    //write a row in the manifest
                    string sFullPath = sPath + sFileName + "_" + sType + ".xml";
                    xManifest.Element("files").Add(new XElement("file", sFileName + "_" + sType + ".xml"));
					
					string sXML = "<" + sType + ">";
					string sRecordName = sType.Substring(0, sType.Length - 1);

					foreach (DataRow dr in dt.Rows)
                    {
						sXML += "<" + sRecordName + ">";
						
			            foreach (DataColumn col in dt.Columns)
			            {
			                sXML += "<" + col.ColumnName + ">";
			                sXML += dr[col.ColumnName].ToString();
			                sXML += "</" + col.ColumnName + ">";
			            }
 					
						sXML += "</" + sRecordName + ">";
					}


					sXML += "</" + sType + ">";
					
					//get a file handle
                    FileStream fsOut = null;
                    fsOut = File.Open(sFullPath, FileMode.Create);
                    byte[] baXML = Encoding.ASCII.GetBytes(sXML);
                    fsOut.Write(baXML, 0, baXML.Length);
					fsOut.Close();
                }
                else
                {
                    //TODO: error logging goes here if there were no rows
                }
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                return false;
            }

            return true;
        }
        private void makeZip(string sPath, string sFileName)
        {
            try
            {
                //delete the zip if it's already there.  Just as a safety measure, 
                //otherwise it will try to put it in itself.
                File.Delete(sPath + sFileName + ".zip");

                // Depending on the directory this could be very large and would require more attention
                // in a commercial package.
                string[] filenames = Directory.GetFiles(sPath, "*.xml");

                // 'using' statements gaurantee the stream is closed properly which is a big source
                // of problems otherwise.  Its exception safe as well which is great.
                using (ZipOutputStream s = new ZipOutputStream(File.Create(sPath + sFileName + ".zip")))
                {

                    s.SetLevel(9); // 0 - store only to 9 - means best compression

                    byte[] buffer = new byte[4096];

                    foreach (string file in filenames)
                    {

                        // Using GetFileName makes the result compatible with XP
                        // as the resulting path is not absolute.
                        ZipEntry entry = new ZipEntry(Path.GetFileName(file));

                        // Setup the entry data as required.

                        // Crc and size are handled by the library for seakable streams
                        // so no need to do them here.

                        // Could also use the last write time or similar for the file.
                        entry.DateTime = DateTime.Now;
                        s.PutNextEntry(entry);

                        using (FileStream fs = File.OpenRead(file))
                        {

                            // Using a fixed size buffer here makes no noticeable difference for output
                            // but keeps a lid on memory usage.
                            int sourceBytes;
                            do
                            {
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                s.Write(buffer, 0, sourceBytes);
                            } while (sourceBytes > 0);
                        }
                    }

                    // Finish/Close arent needed strictly as the using statement does this automatically

                    // Finish is important to ensure trailing information for a Zip file is appended.  Without this
                    // the created file would be invalid.
                    s.Finish();

                    // Close is important to wrap things up and unlock the file.
                    s.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        public bool IdentifyTaskConflicts(string sUserID, string sID, ref string sErr)
        {
            string sSQL = "";
            //here's some magic.  We have two special columns on the import table, conflict and import_mode.
            //we need to look at all the rows here, and update those columns
            string sClause = (sID == "" ? "" : " and it.task_id = '" + sID + "'");

            //first, update any rows that do not have a task_code conflict as "new"
            sSQL = "update import_task it" +
                " left outer join task t on t.task_name = it.task_name" +
					" set" +
                    " it.conflict = null," +
                    " it.import_mode = 'New'" +
                " where it.user_id = '" + sUserID + "'" +
                sClause +
                " and t.task_id is null" +
                " and it.import_mode is null";
            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                return false;

            //next, clear the conflict on any rows with an updated import_mode
            sSQL = "update import_task it" +
                " left outer join task t on t.task_name = it.task_name and t.version = it.version" +
					" set it.conflict = null" +
                " where it.user_id = '" + sUserID + "'" +
                sClause +
                " and t.task_id is null" +
                " and it.import_mode is not null";
            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                return false;

            //now, update the conflict message field on any rows that have issues.
            sSQL = "update import_task it" +
                " left outer join task t on t.task_name = it.task_name and t.version = it.version" +
					" set" +
                    " it.conflict='Another Task exists with this Name and Version.'," +
                    " it.import_mode = null" +
                " where it.user_id = '" + sUserID + "'" +
                sClause +
                " and t.task_id is not null";
            //" and ifnull(id.import_mode,'') <> 'Overwrite'";
            if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
                return false;

            ////now, update the conflict message field on any rows that have issues.
            //sSQL = "update import_task set" +
            //        " conflict='Another Task exists with this ID.'," +
            //        " import_mode = null" +
            //    " from import_task it" +
            //    " left outer join task t on t.task_id = it.task_id" +
            //    " where user_id = '" + sUserID + "'" +
            //    sTaskClause +
            //    " and t.task_id is not null" +
            //    " and it.import_mode <> 'Overwrite'";
            //if (!dc.sqlExecuteUpdate(sSQL, ref sErr))
            //    return false;

            return true;
        }
        public bool Import(string sUserID, string sTaskIDs, ref string sErr)
        {
            string sSQL = "";

            dataAccess.acTransaction oTrans = new dataAccess.acTransaction(ref sErr);

            //we're doing this in a loop
            //why?  because each row may have different import requirements
            //(overwrite, new version, etc.)

            //Now we are adding different import types not necessarily tied to task/proc.
            //for now we're still just doing it all here until there's a good reason to split it out


            if (sTaskIDs.Length > 0)
            {
                sSQL = "select task_id, task_code, task_name, import_mode from import_task" +
                    " where user_id = '" + sUserID + "'" +
                    " and task_id in (" + sTaskIDs + ")";
				
                DataTable dt = new DataTable();
                if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
                    throw new Exception(sErr);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        string sTaskID = dr["task_id"].ToString();
                        string sNewTaskID = dr["task_id"].ToString();
                        string sTaskCode = dr["task_code"].ToString();
                        string sImportMode = dr["import_mode"].ToString();
                        string sOTID = "";
                        int iCount = 0;

                        switch (sImportMode)
                        {
                            case "New":
                                //just jam in the new row, IF there are no GUID conflicts.

                                //~~~ NEW tasks get manipulated.
                                //* MIGHT have their ID changed if there's a collision
                                //* WILL have the original_task_id reset
                                //* WILL have the version reset to 1.000
                                //* WILL be loaded as 'Development' status
                                //* WILL be the default version

                                //check ID
                                oTrans.Command.CommandText = "select count(*) from task" +
                                    " where task_id = '" + sTaskID + "'";
                                if (!oTrans.ExecGetSingleInteger(ref iCount, ref sErr))
                                    throw new Exception("Unable to check for GUID conflicts.<br />" + sErr);

                                //if there's a GUID conflict then just generate a new guid for this task.
                                if (iCount > 0)
                                {
                                    sNewTaskID = ui.NewGUID();
                                }

                                //check the steps to see if there are any GUID conflicts
                                //and repair them if necessary
                                if (!ReIDSteps(ref oTrans, sUserID, sTaskID, ref sErr))
                                    throw new Exception("Unable to issue new Step GUIDs.<br />" + sErr);

                                //insert the manipulated TASK
                                oTrans.Command.CommandText = "insert into task" +
                                    " (task_id, original_task_id, version," +
                                    " task_name, task_code, task_desc, task_status," +
                                    " use_connector_system, default_version," +
                                    " concurrent_instances, queue_depth, parameter_xml, created_dt)" +
                                    " select" +
                                    " '" + sNewTaskID + "', '" + sNewTaskID + "', 1.000," +
                                    " task_name, task_code, task_desc, 'Development'," +
                                    " use_connector_system, 1," +
                                    " concurrent_instances, queue_depth, parameter_xml, created_dt" +
                                    " from import_task" +
                                    " where user_id = '" + sUserID + "'" +
                                    " and task_id = '" + sTaskID + "'";
                                if (!oTrans.ExecUpdate(ref sErr))
                                    throw new Exception(sErr);

                                ui.WriteObjectAddLog(Globals.acObjectTypes.Task, sNewTaskID, dr["task_code"].ToString() + " - " + dr["task_name"].ToString(), "New - Created via Import");

                                break;




                            case "New Version":
                                //if it's a new version, we need to make sure the "original_task_id"
                                //is correct in the target database.
                                //(no guarantee the original_id in the source db is correct.)

                                //just jam in the new row, IF there are no GUID conflicts.

                                //~~~ NEW VERSION tasks get manipulated.
                                //* MIGHT have their ID changed if there's a collision
                                //* MIGHT have the original_task_id reset (to match the target)
                                //* WILL have the version reset to the user selection
                                //* WILL be loaded as 'Development' status
                                //* WILL NOT be the default version

                                //check ID
                                oTrans.Command.CommandText = "select count(*) from task" +
                                    " where task_id = '" + sTaskID + "'";
                                if (!oTrans.ExecGetSingleInteger(ref iCount, ref sErr))
                                    throw new Exception("Unable to check for GUID conflicts.<br />" + sErr);

                                //if there's a GUID conflict then just generate a new guid for this task.
                                //and re-id the steps
                                if (iCount > 0)
                                    sNewTaskID = ui.NewGUID();

                                //check the steps to see if there are any GUID conflicts
                                //and repair them if necessary
                                if (!ReIDSteps(ref oTrans, sUserID, sTaskID, ref sErr))
                                    throw new Exception("Unable to issue new Step GUIDs.<br />" + sErr);


                                //NOW, we need to make sure this task is connected to it's family
                                //we do this by ensuring the original_task_id matches.

                                //BUT, we got here by assuming the task_code was the key.
                                //so... find the original_task_id for this task_code
                                oTrans.Command.CommandText = "select original_task_id" +
                                    " from task where task_code = '" + sTaskCode + "' limit 1";
                                if (!oTrans.ExecGetSingleString(ref sOTID, ref sErr))
                                    throw new Exception("Unable to get original task ID for [" + sTaskCode + "].<br />" + sErr);


                                //insert the manipulated TASK
                                oTrans.Command.CommandText = "insert into task" +
                                    " (task_id, original_task_id, version," +
                                    " task_name, task_code, task_desc, task_status," +
                                    " use_connector_system, default_version," +
                                    " concurrent_instances, queue_depth, parameter_xml, created_dt)" +
                                    " select" +
                                    " '" + sNewTaskID + "', '" + sOTID + "', version," +
                                    " task_name, task_code, task_desc, 'Development'," +
                                    " use_connector_system, 0," +
                                    " concurrent_instances, queue_depth, parameter_xml, created_dt" +
                                    " from import_task" +
                                    " where user_id = '" + sUserID + "'" +
                                    " and task_id = '" + sTaskID + "'";

                                if (!oTrans.ExecUpdate(ref sErr))
                                    throw new Exception(sErr);

                                ui.WriteObjectAddLog(Globals.acObjectTypes.Task, sNewTaskID, dr["task_code"].ToString() + " - " + dr["task_name"].ToString(), "New Version - Created via Import");
                                break;

                            /*
                             Note: I stopped here because I'm not convinced that "Overwrite" is a safe or useful
                             feature.
 
                             So, we'll come back to it if needed.
 
                             Be aware, this was pseudocode... it never worked, was just placeholder stuff and ideas.
                             */



                            //case "Overwrite":
                            //    //stomp it, but make sure to set the task_id/original_task_id
                            //    //to match in the target db.
                            //    //just because the code matches doesn't mean the ID's do.

                            //    //we can just UPDATE the task row

                            //    //DON'T FORGET to DELETE the existing steps and codeblocks


                            //    //we need to make sure the "original_task_id"
                            //    //is correct in the target database.
                            //    //(no guarantee the original_id in the source db is correct.)


                            //    //~~~ OVERWRITE tasks get manipulated.
                            //    //* MIGHT have their ID changed to match the code/version being imported
                            //    //* MIGHT have the original_task_id reset (to match the target)
                            //    //* WILL have the version reset to the user selection
                            //    //* WILL be loaded as 'Development' status
                            //    //* MIGHT be the default version (not gonna update that value)

                            //    //NOW, we need to make sure this task is connected to it's family
                            //    //we do this by ensuring the original_task_id matches.

                            //    //BUT, we got here by assuming the task_code was the key.
                            //    //so... find the task AND original_task_id for this task_code
                            //    oTrans.Command.CommandText = "select top 1 task_id" +
                            //        " from task where task_code = '" + sTaskCode + "'";
                            //    if (!oTrans.ExecGetSingleString(ref sNewTaskID, ref sErr))
                            //        throw new Exception("Unable to get task ID for [" + sTaskCode + "].<br />" + sErr);
                            //    oTrans.Command.CommandText = "select top 1 original_task_id" +
                            //        " from task where task_code = '" + sTaskCode + "'";
                            //    if (!oTrans.ExecGetSingleString(ref sOTID, ref sErr))
                            //        throw new Exception("Unable to get original task ID for [" + sTaskCode + "].<br />" + sErr);

                            //    //get a datareader on the import_task row
                            //    sSQL = "select task_desc, manual_or_digital, use_connector_system," +
                            //        " concurrent_instances, queue_depth, parameter_xml, created_dt" +
                            //        " from import_task" +
                            //        " where user_id = '" + sUserID + "'" +
                            //        " and task_id = '" + sTaskID + "'";
                            //    OdbcDataReader drTaskRow = null;
                            //    if (!dc.sqlGetDataReader(ref drTaskRow, sSQL, ref sErr)) return false;

                            //    if (drTaskRow.HasRows)
                            //    {
                            //        //insert the manipulated TASK
                            //        //THIS WAS NEVER TESTED
                            //        oTrans.Command.CommandText = "update task" +
                            //            " set task_desc = ''," +
                            //            " manual_or_digital = ''," +
                            //            " use_connector_system = ''," +
                            //            " concurrent_instances = ''," +
                            //            " queue_depth = ''," +
                            //            " parameter_xml = ''," +
                            //            " created_dt = ''" +
                            //            " select" +
                            //            " '" + sNewTaskID + "', '" + sOTID + "', version," +
                            //            " task_name, task_code, task_desc, 'Development', manual_or_digital," +
                            //            " use_connector_system, 0," +
                            //            " concurrent_instances, queue_depth, parameter_xml, created_dt" +
                            //            " from import_task" +
                            //            " where user_id = '" + sUserID + "'" +
                            //            " and task_id = '" + sTaskID + "'";

                            //        if (!oTrans.ExecUpdate(ref sErr))
                            //            throw new Exception(sErr);
                            //    }


                            //    ui.WriteObjectAddLog(Globals.acObjectTypes.Task, sNewTaskID, dr["task_code"].ToString() + " - " + dr["task_name"].ToString(), "Overwritten by Import");

                            //    break;
                            default:
                                break;
                        }




                        //CODEBLOCKS AND STEPS can be done here... they are just inserted
                        // (because they were manipulated already if needed)


                        //CODEBLOCKS
                        oTrans.Command.CommandText = "insert into task_codeblock" +
                            " (task_id, codeblock_name)" +
                            " select" +
                            " '" + sNewTaskID + "', codeblock_name" +
                            " from import_task_codeblock" +
                            " where user_id = '" + sUserID + "'" +
                            " and task_id = '" + sTaskID + "'";
                        if (!oTrans.ExecUpdate(ref sErr))
                            throw new Exception(sErr);

                        //STEPS
                        oTrans.Command.CommandText = "insert into task_step" +
                            " (step_id, task_id, codeblock_name, step_order, commented," +
                            " locked, function_name, function_xml, step_desc, output_parse_type," +
                            " output_row_delimiter, output_column_delimiter, variable_xml)" +
                            " select" +
                            " step_id, '" + sNewTaskID + "', codeblock_name, step_order, commented," +
                            " locked, function_name, function_xml, step_desc, output_parse_type," +
                            " output_row_delimiter, output_column_delimiter, variable_xml" +
                            " from import_task_step" +
                            " where user_id = '" + sUserID + "'" +
                            " and task_id = '" + sTaskID + "'";
                        if (!oTrans.ExecUpdate(ref sErr))
                            throw new Exception(sErr);

                    }
                }
                else
                {
                    sErr = "No Task import items were found.";
                    oTrans.RollBack();
                    return false;
                }


                //whack those rows from the import table.  
                //why?  their disposition has now changed, and we don't wanna accidentally reload them.
                //or add confusion to the user.
                oTrans.Command.CommandText = "delete from import_task where user_id = '" + sUserID + "' and task_id in (" + sTaskIDs + ")";
                if (!oTrans.ExecUpdate(ref sErr))
                    throw new Exception(sErr);

				oTrans.Command.CommandText = "delete from import_task_codeblock where user_id = '" + sUserID + "' and task_id in (" + sTaskIDs + ")";
				if (!oTrans.ExecUpdate(ref sErr))
                	throw new Exception(sErr);

				oTrans.Command.CommandText = "delete from import_task_step where user_id = '" + sUserID + "' and task_id in (" + sTaskIDs + ")";
                if (!oTrans.ExecUpdate(ref sErr))
                    throw new Exception(sErr);
            }

            //all done with everything... close it out
            oTrans.Commit();

            return true;
        }
        private bool ReIDSteps(ref dataAccess.acTransaction oTrans, string sUserID, string sTaskID, ref string sErr)
        {
            //We join this to task_step on step_id, and only issue new GUID's 
            //to the rows that are in conflict.

            //saves time, and if no steps are in conflict, the imported version is an exact match of the
            //exported one.
            //( and yes, I realize I did not join task_step on the task_id.
            // thats because the step_id is a guid AND the PK on the table.  not necessary to check
            // it against task_id too.)
            oTrans.Command.CommandText = "select its.step_id" +
                " from import_task_step its" +
                " join task_step ts on its.step_id = ts.step_id" +
                " where its.user_id = '" + sUserID + "'" +
                " and its.task_id = '" + sTaskID + "'";

            DataTable dtSteps = new DataTable();
            if (!oTrans.ExecGetDataTable(ref dtSteps, ref sErr))
                throw new Exception(sErr);

            if (dtSteps.Rows.Count > 0)
            {
                foreach (DataRow drSteps in dtSteps.Rows)
                {
                    //update each row by:
                    //*) getting a new guid
                    //*) searching for references to the old ID and replacing them
                    //  specifically in function_xml
                    //*) updating the row with the new guid

                    string sOrigStepID = drSteps["step_id"].ToString();
                    string sNewStepID = ui.NewGUID();


                    //this will update any references in function_xml with 
                    // the new guid of this step

                    oTrans.Command.CommandText = "update import_task_step" +
                        " set function_xml = replace(function_xml, '" + sOrigStepID + "', '" + sNewStepID + "')" +
                        " where ifnull(ExtractValue(function_xml, '(//*[. = ''" + sOrigStepID + "''])'), '') <> ''";
                    if (!oTrans.ExecUpdate(ref sErr))
                        throw new Exception(sErr);

                    //then finally, we will update the actual step rows with the new id
                    oTrans.Command.CommandText = "update import_task_step" +
                        " set step_id = '" + sNewStepID + "'" +
                        " where step_id = '" + sOrigStepID + "'";
                    if (!oTrans.ExecUpdate(ref sErr))
                        throw new Exception(sErr);

                }
            }

            return true;

        }
    }

}
