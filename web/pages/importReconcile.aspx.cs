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

namespace Web.pages
{
    public partial class importReconcile : System.Web.UI.Page
    {
        acUI.acUI ui = new acUI.acUI();
        dataAccess dc = new dataAccess();
        string sUserID;
        protected void Page_Load(object sender, EventArgs e)
        {
            sUserID = ui.GetSessionUserID();

            GetTasks();
        }

        protected void GetTasks()
        {
            string sSQL = "";
            string sErr = "";

            //fill the repeater with the tasks from the temp table(s)
            sSQL = "select it.task_id, it.original_task_id, it.task_name, it.task_code," +
                " it.task_desc, it.version, it.conflict, it.import_mode," +
                " case when it.import_mode is null" +
                " then '<img src=\"../images/icons/status_unknown_16.png\" alt=\"\" />'" +
                " else concat('<input type=\"checkbox\" class=\"chkbox\" id=\"chk_', it.task_id, '\" task_id=\"', it.task_id, '\" tag=\"task_chk\" />') end as checkbox" +
                " from import_task it " +
                " where it.user_id = '" + sUserID + "'" +
                " order by it.task_code";

            DataTable dt = new DataTable();
            if (!dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                ui.RaiseError(Page, sErr, true, "");
            }

            rpTasks.DataSource = dt;
            rpTasks.DataBind();
        }


        protected void btnReload_Click(object sender, EventArgs e)
        {
            GetTasks();
        }
        
    }
}
