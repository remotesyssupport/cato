<%@ Page Language="C#" AutoEventWireup="true" Inherits="Web.pages.taskRunLog" CodeBehind="taskRunLog.aspx.cs"
    MasterPageFile="~/pages/popupwindow.master" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <link type="text/css" href="../style/taskRunLog.css" rel="stylesheet" />
    <script type="text/javascript" src="../script/parametersOnDialog.js"></script>
    <script type="text/javascript" src="../script/taskRunLog.js"></script>
    <script type="text/javascript" src="../script/taskLaunchDialog.js"></script>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="top_part">
        <div class="ui-widget-header center">
            <asp:Label ID="lblBackLink" CssClass="link" runat="server" />
            <h4>
                <asp:Label ID="lblTaskName" runat="server" /></h4>
        </div>
        <div class="ui-widget-content ui-corner-all instance_header">
            <span class="instance_label">Instance: </span>
            <asp:Label ID="lblTaskInstance" runat="server" />
            - <span class="instance_label">Status: </span>
            <asp:Label ID="lblStatus" runat="server" />
            - <span class="instance_label">Started: </span>
            <asp:Label ID="lblStartedDT" runat="server" />
            - <span class="instance_label">Completed: </span>
            <asp:Label ID="lblCompletedDT" runat="server" />
        </div>
        <div class="ui-widget-content ui-corner-all instance_header action_bar">
            <div class="floatleft">
                <span id="refresh_btn" class="pointer">
                    <img alt="Refresh" onclick="return false;" src="../images/icons/reload.png" /></span>
                <asp:PlaceHolder ID="phCancel" runat="server"><span id="abort_btn" class="pointer">
                    <img alt="Cancel Task" onclick="$('#abort_dialog').dialog('open');return false;"
                        src="../images/icons/player_stop_22.png" /></span></asp:PlaceHolder>
                <asp:PlaceHolder ID="phResubmit" runat="server"><span id="resubmit_btn" class="pointer">
                    <img alt="Run Task" onclick="return false;" src="../images/icons/player_play_22.png" />
                </span></asp:PlaceHolder>
            </div>
            <div class="floatright">
                <span id="show_detail" class="vis_btn vis_btn_off">Details</span><span id="show_cmd"
                    class="vis_btn vis_btn_off">Commands</span><span id="show_results" class="vis_btn">Results</span>
                <span id="get_all" class="vis_btn vis_btn_off">All Rows</span><span id="show_logfile"
                    class="vis_btn vis_btn_off">Logfile</span>
            </div>
            <div class="clearfloat">
            </div>
        </div>
        <div id="full_details" class="ui-widget-content ui-corner-all instance_header hidden">
            <table width="100%">
                <tr>
                    <td width="150px">
                        <span class="instance_label">Cloud Account:</span>
                    </td>
                    <td>
                        <asp:Label ID="lblAccountName" runat="server" />
                    </td>
                    <td width="150px">
                        <span class="instance_label">Submitted By:</span>
                    </td>
                    <td>
                        <asp:Label ID="lblSubmittedBy" runat="server" />
                    </td>
                </tr>
                <tr>
                    <td width="150px">
                        <span class="instance_label">Ecosystem:</span>
                    </td>
                    <td>
                        <asp:Label ID="lblEcosystemName" runat="server" />
                    </td>
                    <td width="150px">
                        <span class="instance_label">Submitted:</span>
                    </td>
                    <td>
                        <asp:Label ID="lblSubmittedDT" runat="server" />
                    </td>
                </tr>
                <tr>
                    <td width="150px">
                        <span class="instance_label">Asset:</span>
                    </td>
                    <td>
                        <asp:Label ID="lblAssetName" runat="server" />
                    </td>
                    <td width="150px">
                        <span class="instance_label">Submitted By Instance:</span>
                    </td>
                    <td>
                        <asp:Label ID="lblSubmittedByInstance" tag="jump_to_instance" runat="server" />
                    </td>
                </tr>
                <tr>
                    <td width="150px">
                        <span class="instance_label">CE Node:</span>
                    </td>
                    <td>
                        <asp:Label ID="lblCENode" runat="server" />
                    </td>
                    <td width="150px">
                        <span class="instance_label">PID:</span>
                    </td>
                    <td>
                        <asp:Label ID="lblPID" runat="server" />
                    </td>
                </tr>
            </table>
        </div>
        <hr />
        <asp:Panel ID="pnlOtherInstances" runat="server" Visible="false" CssClass="ui-widget-content ui-corner-all instance_header">
            <table class="jtable" cellspacing="1" cellpadding="1" width="100%">
                <asp:Repeater ID="rpOtherInstances" runat="server">
                    <HeaderTemplate>
                        <tr>
                            <th class="col_header">
                                Instance
                            </th>
                            <th class="col_header">
                                Status
                            </th>
                        </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr task_instance="<%# (((System.Data.DataRowView)Container.DataItem)["task_instance"]) %>">
                            <td tag="selectable">
                                <%# (((System.Data.DataRowView)Container.DataItem)["task_instance"]) %>
                            </td>
                            <td tag="selectable">
                                <%# (((System.Data.DataRowView)Container.DataItem)["task_status"]) %>
                            </td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </table>
            <hr />
        </asp:Panel>
    </div>
    <div id="bottom_part">
        <asp:PlaceHolder ID="phLog" runat="server"></asp:PlaceHolder>
    </div>
    <div id="resubmit_dialog" title="Run Task" class="ui-state-highlight">
        <asp:Label ID="lblResubmitMessage" runat="server" />
        Are you sure?
    </div>
    <div id="abort_dialog" title="Cancel Task" class="ui-state-highlight">
        Are you sure?
    </div>
    <div id="task_launch_dialog" title="Run Task">
    </div>
    <asp:HiddenField ID="hidTaskID" runat="server" Value="" />
    <asp:HiddenField ID="hidAssetID" runat="server" Value="" />
    <asp:HiddenField ID="hidEcosystemID" runat="server" Value="" />
    <asp:HiddenField ID="hidAccountID" runat="server" Value="" />
    <asp:HiddenField ID="hidInstanceID" runat="server" Value="" />
    <asp:HiddenField ID="hidSubmittedByInstance" runat="server" Value="" />
    <asp:HiddenField ID="hidRows" runat="server" Value="100" />
    <asp:HiddenField ID="hidCELogFile" runat="server" />
    <asp:HiddenField ID="hidDebugLevel" runat="server" />
</asp:Content>
