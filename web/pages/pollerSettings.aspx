<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="pollerSettings.aspx.cs"
    Inherits="Web.pages.pollerSettings" MasterPageFile="~/pages/site.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="phDetail" runat="server">
    <script type="text/javascript" src="../script/pollerSettings.js"></script>
    <style type="text/css">
        .usertextbox
        {
            width: 300px;
        }
    </style>
    <asp:UpdatePanel ID="udpNotifications" runat="server">
        <ContentTemplate>
            <div id="left_panel">
                <div class="left_tooltip">
                    <span class="page_title">Poller Settings</span>
                    <img src="../images/terminal-192x192.png" alt="" />
                    <div id="left_tooltip_box_outer">
                        <div id="left_tooltip_box_inner">
                            <img src="../images/tooltip.png" alt="" />
                            <p>
                                Change the settings for the Poller process.</p>
                        </div>
                    </div>
                </div>
            </div>
            <div id="content">
                <div id="content_form_outer">
                    <div id="content_form_inner">
                        <table width="100%">
                            <tr>
                                <td width="150px" class="row_odd">
                                    Poller On / Off
                                </td>
                                <td class="row_odd">
                                    <asp:DropDownList ID="ddlPollerOnOff" CssClass="usertextbox" runat="server">
                                        <asp:ListItem>on</asp:ListItem>
                                        <asp:ListItem>off</asp:ListItem>
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td class="row_even">
                                    Loop Delay (seconds)
                                </td>
                                <td class="row_even">
                                    <asp:TextBox ID="txtLoopDelay" onkeypress="Javascript:return restrictEntryToNumber(event, this);"
                                        CssClass="usertextbox" runat="server" MaxLength="5" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_odd">
                                    Concurrent Instances
                                </td>
                                <td class="row_odd">
                                    <asp:TextBox ID="txtPollerMaxProcesses" onkeypress="Javascript:return restrictEntryToNumber(event, this);"
                                        CssClass="usertextbox" runat="server" MaxLength="5" />
                                </td>
                            </tr>
                            <tr>
                                <td colspan="2" style="text-align: center;" class="row_odd">
                                    <asp:Button ID="btnSave" runat="server" Text="Save" OnClientClick="return ValidateValues();"
                                        OnClick="btnSave_Click" />
                                </td>
                            </tr>
                            <tr>
                                <td colspan="2" class="row_odd">
                                    <asp:Label ID="lblMessage" runat="server" />
                                </td>
                            </tr>
                        </table>
                    </div>
                </div>
                <hr />
                <a href="#" onclick="if (confirm('Are you sure you want to turn all Pollers and Log Servers OFF?')){setAllPollers('off');}">
                    Turn All Pollers OFF</a>
                <br />
                <a href="#" onclick="if (confirm('Are you sure you want to switch all Pollers and Log Servers ON?')){setAllPollers('on');}">
                    Turn All Pollers ON</a>
                <br />
                <a href="#" onclick="if (confirm('Are you sure you want to give all Users the 1 Minute Warning then kick them out?')){kickAllUsers();}">
                    Kick All Users</a>
                <br />
                <br />
                <a href="#" onclick="if (confirm('Are you sure you want to take down the system?')){setSystemCondition('down');}">
                    SHUTDOWN</a> (Stop processing schedules, cancel all currently running Tasks,
                disable login, and kick all Users.)
                <br />
                <a href="#" onclick="if (confirm('Are you sure you want to bring up the system?')){setSystemCondition('up');}">
                    STARTUP</a> (Start processing schedules, enable Tasks, enable User login.)
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
