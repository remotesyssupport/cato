<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/pages/site.master"
    CodeBehind="systemStatusView.aspx.cs" Inherits="Web.pages.systemStatusView" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <link href="../style/statusView.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript" src="../script/systemStatusView.js"></script>
    <div style="display: none;">
        <!-- Start visible elements -->
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/system-status-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <p>
                        <img src="../images/tooltip.png" alt="" />The system status display shows up to
                        the minute information on system components, services and users.</p>
                    <p>
                        Rows which highlight on mouse-over will display more detailed data when clicked.
                    </p>
                    <p>
                        To refresh the data select the menu choice again.
                    </p>
                </div>
            </div>
        </div>
    </div>
    <div id="content" class="display">
        <br />
        <div id="column-0a" class="pane-column" style="padding-right: 20px;">
            <div id="pane-0" class="pane">
                <span class="panetitle">System Components</span>
                <div id="pane-0-content" class="content">
                    <div class="content-container">
                        <table class="jtable" cellspacing="1" cellpadding="1" width="95%">
                            <thead>
                                <tr>
                                    <th class="first-child" align="left">
                                        Component
                                    </th>
                                    <th align="left">
                                        Instance
                                    </th>
                                    <th align="left">
                                        Heartbeat
                                    </th>
                                    <th align="left">
                                        Enabled?
                                    </th>
                                    <th align="left">
                                        Minutes Since Last Report
                                    </th>
                                    <th class="last-child" align="left">
                                        Logfile
                                    </th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rpSystemComponents" runat="server" OnItemDataBound="rpSystemComponents_ItemDataBound">
                                    <ItemTemplate>
                                        <tr>
                                            <td>
                                                <%# (((System.Data.DataRowView)Container.DataItem)["Component"]) %>
                                            </td>
                                            <td>
                                                <%# (((System.Data.DataRowView)Container.DataItem)["app_instance"]) %>
                                            </td>
                                            <td>
                                                <%# (((System.Data.DataRowView)Container.DataItem)["Heartbeat"]) %>
                                            </td>
                                            <td>
                                                <%# (((System.Data.DataRowView)Container.DataItem)["Master"]) %>
                                            </td>
                                            <td>
                                                <%# (((System.Data.DataRowView)Container.DataItem)["mslr"]) %>
                                            </td>
                                            <td class="logfile_link">
                                                <asp:HyperLink runat="server" ID="lnkCELogFile" Target="_blank"><img src="../images/icons/view_text_16.png" /></asp:HyperLink>
                                            </td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
                <div class="footer">
                    <div>
                        &nbsp;</div>
                </div>
            </div>
        </div>
        <br />
        <div id="column-2" class="pane-column" style="padding-right: 20px;">
            <div id="pane-2" class="pane">
                <span class="panetitle">Current Users</span> <span class="security_log_link" id="show_log_link"
                    class="pointer">
                    <img alt="" src="../images/icons/view_text_16.png" />
                    View Login History</span>
                <div id="pane-2-content" class="content" style="height: 150px; overflow: auto;">
                    <div class="content-container">
                        <table id="tblUserList" cellspacing="1" cellpadding="1" width="95%">
                            <thead>
                                <tr>
                                    <th class="first-child" align="left" width="150px">
                                        User
                                    </th>
                                    <th align="left" width="100px">
                                        Login Time
                                    </th>
                                    <th align="left" width="175px">
                                        Last Update
                                    </th>
                                    <th align="left" width="50px">
                                        Address
                                    </th>
                                    <th class="last-child" align="left" width="100px">
                                        status
                                    </th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptUsers" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td tag="selectuser">
                                                <%# (((System.Data.DataRowView)Container.DataItem)["full_name"]) %>
                                            </td>
                                            <td tag="selectuser">
                                                <%# (((System.Data.DataRowView)Container.DataItem)["login_dt"]) %>
                                            </td>
                                            <td tag="selectuser">
                                                <%# (((System.Data.DataRowView)Container.DataItem)["last_update"]) %>
                                            </td>
                                            <td tag="selectuser">
                                                <%# (((System.Data.DataRowView)Container.DataItem)["address"]) %>
                                            </td>
                                            <td tag="selectuser">
                                                <%# (((System.Data.DataRowView)Container.DataItem)["kick"]) %>
                                            </td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
                <div class="footer">
                    <div>
                        &nbsp;</div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
