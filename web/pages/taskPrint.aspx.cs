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
using System.IO;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;



namespace Web.pages
{
    public partial class taskPrint : System.Web.UI.Page
    {
        dataAccess dc = new dataAccess();
        acUI.acUI ui = new acUI.acUI();
        FunctionTemplates.HTMLTemplates ft = new FunctionTemplates.HTMLTemplates();
        ACWebMethods.taskMethods tm = new ACWebMethods.taskMethods();

        string sUserID;
        string sTaskID;

        //use a checkbox on the page to set this
        bool bShowNotes = true;

        protected void Page_Load(object sender, EventArgs e)
        {
            sUserID = ui.GetSessionUserID();
            sTaskID = ui.GetQuerystringValue("task_id", typeof(string)).ToString();

            string sErr = "";

            
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


            //parameters (for edit)
            if (!GetParameters(sTaskID, ref sErr))
            {
                ui.RaiseError(Page, "Parameters record not found for task_id [" + sTaskID + "]. " + sErr, true, "");
                return;
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
                    lblTaskCode.Text = dr["task_code"].ToString();
                    lblDescription.Text = ui.SafeHTML(((!object.ReferenceEquals(dr["task_desc"], DBNull.Value)) ? dr["task_desc"].ToString() : ""));

                    //lblDirect.Text = ((int)dr["use_connector_system"] == 1 ? "Yes" : "No");
                    //txtConcurrentInstances.Text = ((!object.ReferenceEquals(dr["concurrent_instances"], DBNull.Value)) ? dr["concurrent_instances"].ToString() : "");
                    //txtQueueDepth.Text = ((!object.ReferenceEquals(dr["queue_depth"], DBNull.Value)) ? dr["queue_depth"].ToString() : "");

                    lblStatus.Text = dr["task_status"].ToString();

                    //the header
                    lblTaskNameHeader.Text = ui.SafeHTML(dr["task_name"].ToString());
                    lblVersionHeader.Text = dr["version"].ToString() + ((int)dr["default_version"] == 1 ? " (default)" : "");

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

        #region "Steps"
        private bool GetSteps(ref string sErr)
        {
            try
            {
                Literal lt = new Literal();

                //MAIN codeblock first
                lt.Text += "<div class=\"ui-state-default te_header\">MAIN</div>";
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
                        lt.Text += "<div class=\"ui-state-default te_header\" id=\"cbt_" + dr["codeblock_name"].ToString() + "\">" + dr["codeblock_name"].ToString() + "</div>";
                        lt.Text += "<div class=\"codeblock_box\">";
                        lt.Text += BuildSteps(dr["codeblock_name"].ToString());
                        lt.Text += "</div>";
                    }
                }

                phSteps.Controls.Add(lt);

                return true;
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
                return false;
            }
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
                " left outer join task_step_user_settings us on us.user_id = '" + sUserID + "' and s.step_id = us.step_id" +
                " where s.task_id = '" + sTaskID + "'" +
                " and s.codeblock_name = '" + sCodeblockName + "'" +
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

        private bool GetParameters(string sTaskID, ref string sErr)
        {
            try
            {
                Literal lt = new Literal();
                lt.Text += "<div class=\"view_parameters\">";
                lt.Text += tm.wmGetParameters("task", sTaskID, false, false);
                lt.Text += "</div>";
                phParameters.Controls.Add(lt);

                return true;
            }
            catch (Exception ex)
            {
               sErr = ex.Message;
               return false;
            }
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


        protected override void Render(HtmlTextWriter Output)
        {
            string sHTML = "";

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            HtmlTextWriter hw = new HtmlTextWriter(sw);

            base.Render(hw);
            sHTML = sb.ToString();

            if (hidExport.Value == "export")
            {
                //5/10/2011 NSC 
                //Winnovative PDF generator was not working with VS2010.
                //if this is needed we will need to find a new PDF generator.

                //PdfConverter pdfConverter = new PdfConverter();
                //pdfConverter.PdfDocumentOptions.PdfPageSize = PdfPageSize.A4;
                //pdfConverter.PdfDocumentOptions.PdfPageOrientation = PDFPageOrientation.Portrait;
                //pdfConverter.PdfDocumentOptions.PdfCompressionLevel = PdfCompressionLevel.Normal;
                //pdfConverter.LicenseKey = "DnGDxKtBfgZuI0rkd765/VniiDaWDOTQkzk1k/tpGW9SYwYC0qMAvqNTuqGjw+dn";

                //string sFile = Server.MapPath("~/reports/" + sUserID + ".html");
                //string sPDF = Server.MapPath("~/reports/" + sUserID + ".pdf");

                //File.WriteAllText(sFile, sHTML);

                //pdfConverter.SavePdfFromUrlToFile(sFile, sPDF);

                //System.IO.FileInfo fPDF = new System.IO.FileInfo(sPDF);
                         
                //  if (fPDF.Exists) {
                //      Response.Clear();
                //      Response.AddHeader("Content-Disposition", "attachment; filename=" + fPDF.Name);
                //      Response.AddHeader("Content-Length", fPDF.Length.ToString());
                //      Response.ContentType = "application/octet-stream";
                //      Response.WriteFile(fPDF.FullName);
                //      Response.Flush();
                //      Response.End();
                //  }

            }
            else
            {

                Output.Write(sHTML);
            }

        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            hidExport.Value = "export"; 

        }

    }
    
}
