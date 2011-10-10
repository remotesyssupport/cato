<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="taskManage.aspx.cs" Inherits="Web.pages.taskManage"
    MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <script type="text/javascript" src="../script/managePageCommon.js"></script>
    <script type="text/javascript" src="../script/taskManage.js"></script>
    <div style="display: none;">
        <input id="hidMode" type="hidden" name="hidMode" />
        <input id="hidEditCount" type="hidden" name="hidEditCount" />
        <input id="hidCurrentEditID" type="hidden" name="hidCurrentEditID" />
        <input id="hidCurrentAttributeGroupEditing" type="hidden" name="hidCurrentAttributeGroupEditing" />
        <input id="hidSelectedArray" type="hidden" name="hidSelectedArray" />
        <asp:HiddenField ID="hidSortColumn" runat="server" />
        <asp:HiddenField ID="hidSortDirection" runat="server" />
        <asp:HiddenField ID="hidLastSortColumn" runat="server" />
        <asp:HiddenField ID="hidPage" runat="server" />
        <!-- Start visible elements -->
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/manage-tasks-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <p>
                        <img src="../images/tooltip.png" alt="" />The Manage Tasks screen allows administrators
                        to modify, add and delete Tasks.</p>
                    <p>
                        Select one or more Tasks from the list to the right, using the checkboxes. Select
                        an action to modify or delete the Tasks you've selected.</p>
                </div>
            </div>
        </div>
    </div>
    <div id="content">
        <asp:UpdatePanel ID="udpTaskList" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <span id="lblItemsSelected">0</span> Items Selected <span id="clear_selected_btn">
                </span><span id="item_create_btn">Create</span> <span id="item_copy_btn">Copy</span>
                <span id="item_export_btn">Export</span>
                <span id="item_delete_btn">Delete</span>
                <asp:TextBox ID="txtSearch" runat="server" class="search_text" />
                <span id="item_search_btn">Search</span>
                <asp:ImageButton ID="btnSearch" class="hidden" OnClick="btnSearch_Click" runat="server" />
                <table class="jtable" cellspacing="1" cellpadding="1" width="99%">
                    <asp:Repeater ID="rpTasks" runat="server">
                        <HeaderTemplate>
                            <tr>
                                <th class="chkboxcolumn">
                                    <input class="chkbox" type="checkbox" id="chkAll" />
                                </th>
                                <th sortcolumn="task_code" width="75px">
                                    <img id="imgUptask_code" src="../images/UpArrow.gif" class="hidden" alt="" />
                                    <img id="imgDowntask_codee" src="../images/DnArrow.gif" class="hidden" alt="" />
                                    Task Code
                                </th>
                                <th sortcolumn="task_name" width="200px">
                                    <img id="imgUptask_name" src="../images/UpArrow.gif" class="hidden" alt="" />
                                    <img id="imgDowntask_name" src="../images/DnArrow.gif" class="hidden" alt="" />
                                    Task Name
                                </th>
                                <th sortcolumn="version" width="50px">
                                    <img id="imgUpversion" src="../images/UpArrow.gif" class="hidden" alt="" />
                                    <img id="imgDownversion" src="../images/DnArrow.gif" class="hidden" alt="" />
                                    Version
                                </th>
                                <th sortcolumn="task_desc" width="400px">
                                    <img id="imgUptask_desc" src="../images/UpArrow.gif" class="hidden" alt="" />
                                    <img id="imgDowntask_desc" src="../images/DnArrow.gif" class="hidden" alt="" />
                                    Description
                                </th>
                                <th sortcolumn="task_status" width="75px">
                                    <img id="imgUptask_status" src="../images/UpArrow.gif" class="hidden" alt="" />
                                    <img id="imgDowntask_status" src="../images/DnArrow.gif" class="hidden" alt="" />
                                    Status
                                </th>
                            </tr>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr task_id="<%#Eval("task_id")%>">
                                <td class="chkboxcolumn">
                                    <input type="checkbox" class="chkbox" id="chk_<%#Eval("original_task_id")%>" task_id="<%#Eval("task_id")%>"
                                        tag="chk" />
                                </td>
                                <td tag="selectable">
                                    <%#Eval("task_code")%>
                                </td>
                                <td tag="selectable">
                                    <%#Eval("task_name")%>
                                </td>
                                <td tag="selectable">
                                    <%#Eval("version")%>
                                </td>
                                <td tag="selectable">
                                    <%#Eval("task_desc")%>
                                </td>
                                <td tag="selectable">
                                    <%#Eval("task_status")%>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </table>
                <asp:PlaceHolder ID="phPager" runat="server"></asp:PlaceHolder>
                <div class="hidden">
                    <asp:Button ID="btnGetPage" runat="server" OnClick="btnGetPage_Click" /></div>
            </ContentTemplate>
        </asp:UpdatePanel>
    </div>
    <div id="edit_dialog" class="hidden" title="Create Task">
        <table id="tblNew" width="100%">
            <tbody>
                <tr>
                    <td colspan="2">
                        <span id="lblNewTaskMessage" />
                    </td>
                </tr>
                <tr>
                    <td>
                        Task Code
                    </td>
                    <td>
                        <asp:TextBox ID="txtTaskCode" runat="server" class="usertextbox" Style="width: 300px;"
                            MaxLength="32" jqname="txtTaskCode"></asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td>
                        Task Name
                    </td>
                    <td>
                        <asp:TextBox ID="txtTaskName" runat="server" class="usertextbox" Style="width: 300px;"
                            MaxLength="256" jqname="txtTaskName"></asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td>
                        Description
                    </td>
                    <td>
                        <asp:TextBox ID="txtTaskDesc" runat="server" class="usertextbox" Style="width: 300px;"
                            jqname="txtTaskDesc" TextMode="MultiLine"></asp:TextBox>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
    <div id="delete_dialog" class="hidden" title="Delete Tasks">
        Are you sure you want to delete these Tasks?
    </div>
    <div id="export_dialog" class="hidden" title="Export Tasks">
        Are you sure you want to export these Tasks?
    </div>
    <div id="copy_dialog" class="hidden" title="Copy Task">
        <table id="tblCopy" width="100%">
            <tr>
                <td colspan="2">
                    <span id="lblTaskCopy" />
                </td>
            </tr>
            <tr>
                <td>
                    Copy From Version
                </td>
                <td>
                    <div id="divTaskVersions">
                    </div>
                </td>
            </tr>
            <tr>
                <td>
                    New Task Code
                </td>
                <td>
                    <asp:TextBox ID="txtCopyTaskCode" runat="server" class="usertextbox" Style="width: 300px;"
                        MaxLength="32" jqname="txtCopyTaskCode"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>
                    New Task Name
                </td>
                <td>
                    <asp:TextBox ID="txtCopyTaskName" runat="server" class="usertextbox" Style="width: 300px;"
                        MaxLength="256" jqname="txtCopyTaskName"></asp:TextBox>
                </td>
            </tr>
        </table>
        <input id="hidCopyTaskID" type="hidden" name="hidCopyTaskID" />
    </div>
</asp:Content>
