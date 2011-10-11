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

namespace Web.pages
{
    public partial class importLoadFile : System.Web.UI.Page
    {
        acUI.acUI ui = new acUI.acUI();
        dataAccess dc = new dataAccess();

        protected void Page_Load(object sender, EventArgs e)
        {
            //if there are rows for this user_id in the import tables
            //it means they have uploaded a file, but not reconciled and imported.

            //show a link so they can continue on that process.
            string sSQL = "";
            string sErr = "";
            string sUserID = ui.GetSessionUserID();

            sSQL = "select count(*) as cnt from import_task where user_id = '" + sUserID + "'";
            int iCount = 0;
            if (!dc.sqlGetSingleInteger(ref iCount, sSQL, ref sErr))
            {
                throw new Exception("Unable to count saved import sessions.<br />" + sErr);
            }

            if (iCount > 0)
                pnlResume.Visible = true;
            else
                pnlResume.Visible = false;
        }

        protected void btnImport_Click(object sender, EventArgs e)
        {
            string sErr = "";

            ImportExport.ImportExportClass ie = new ImportExport.ImportExportClass();
            string sPath = Server.MapPath("~/temp/");

            //get the file from the browser
            if ((fupFile.PostedFile != null) && (fupFile.PostedFile.ContentLength > 0))
            {
                string sFileName = System.IO.Path.GetFileName(fupFile.PostedFile.FileName);
                string sFullPath = sPath + sFileName;
                try
                {
                    fupFile.PostedFile.SaveAs(sFullPath);
                    ////Response.Write("The file has been uploaded.");

                    ////confirm the zip file exists in our temp directory
                    if (File.Exists(sFullPath))
                        if (!ie.doFileLoad(sPath, sFullPath, sFileName, ui.GetSessionUserID(), ref sErr))
                        {
                            ui.RaiseError(Page, sErr, true, "");
                        }
                        else
                        {
                            //all good

                            Response.Redirect("importReconcile.aspx", false);
                        }
                }
                catch (Exception ex)
                {
                    ui.RaiseError(Page, ex.Message, true, "");
                }
            }
            else
            {
                ui.RaiseError(Page, "Please select a file to upload.", true, "");
            }
        }
    }
}
