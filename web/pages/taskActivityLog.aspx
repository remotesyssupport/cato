<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/pages/site.master"
    CodeBehind="taskActivityLog.aspx.cs" Inherits="Web.pages.taskActivityLog" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <script type="text/javascript" src="../script/managePageCommon.js"></script>
    <script type="text/javascript" src="../script/taskActivityLog.js"></script>
	<div style="display: none;">
        <asp:HiddenField ID="hidSortColumn" runat="server" />
        <asp:HiddenField ID="hidPage" runat="server" />
        <!-- Start visible elements -->
    </div>
    <div id="fullwidthcontent">
        <asp:UpdatePanel ID="uplTaskActivity" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <div class="page_title">
                    Automated Task Activity Log</div>
                Filter:
                <asp:TextBox ID="txtSearch" class="search_text" runat="server" />
                <!-- for some reason this pops up under the list?? -->
                <span id="lblStartDate" style="z-index: 1200;">Begin Date
                    <asp:TextBox ID="txtStartDate" Width="70px" class="datepicker" runat="server" /></span>
                <span id="lblStopDate">End Date
                    <asp:TextBox ID="txtStopDate" Width="70px" class="datepicker" runat="server" /></span>
                <span id="lblMaxRows"># of Results
                    <asp:TextBox ID="txtResultCount" Width="70px" runat="server" /></span><span id="item_search_btn">Search</span>
                <asp:ImageButton ID="btnSearch" class="hidden" OnClick="btnSearch_Click" runat="server" />
                <table class="jtable" cellspacing="1" cellpadding="1" width="100%">
                    <asp:Repeater ID="rpTaskInstances" runat="server" OnItemDataBound="rpTaskInstances_ItemDataBound">
                        <HeaderTemplate>
                            <tr>
                                <th sortcolumn="task_instance" width="30px">
                                    Instance
                                </th>
                                <th sortcolumn="task_code">
                                    Task Code
                                </th>
                                <th sortcolumn="task_name">
                                    Task
                                </th>
                                <th sortcolumn="asset_name">
                                    Asset
                                </th>
                                <th sortcolumn="task_status">
                                    Status
                                </th>
                                <th sortcolumn="started_by">
                                    Started By
                                </th>
                                <th sortcolumn="ce_name">
                                    CE
                                </th>
                                <th sortcolumn="process_id">
                                    PID
                                </th>
                                <th sortcolumn="ecosystem_name">
                                    Ecosystem
                                </th>
                                <th sortcolumn="completed_dt" width="100px">
                                    Timing
                                </th>
                                <th width="10px" align="center">
                                </th>
                            </tr>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr style="font-size: .8em;" task_instance="<%# (((System.Data.DataRowView)Container.DataItem)["task_instance"]) %>">
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["task_instance"]) %>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["task_code"])%>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["task_name"])%>
                                    (<%# (((System.Data.DataRowView)Container.DataItem)["version"])%>)
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["asset_name"])%>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["task_status"]) %>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["started_by"])%>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["ce_name"]) %>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["process_id"]) %>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["ecosystem_name"]) %>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["submitted_dt"])%><br />
                                    <%# (((System.Data.DataRowView)Container.DataItem)["started_dt"]) %><br />
                                    <%# (((System.Data.DataRowView)Container.DataItem)["completed_dt"]) %>
                                </td>
                                <td>
                                    <asp:HyperLink runat="server" ID="lnkEdit"><img src="../images/icons/edit_16.png" /></asp:HyperLink>
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
</asp:Content>
