<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="schedulerSettings.aspx.cs"
    Inherits="Web.pages.schedulerSettings" MasterPageFile="~/pages/site.master" %>


<asp:Content ID="Content1" ContentPlaceHolderID="phDetail" runat="server">
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
                    <span class="page_title">Scheduler Settings</span>
                    <img src="../images/terminal-192x192.png" alt="" />
                    <div id="left_tooltip_box_outer">
                        <div id="left_tooltip_box_inner">
                            <img src="../images/tooltip.png" alt="" />
                            <p>
                                Change the settings for the Scheduler process.</p>
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
                                    Scheduler On / Off
                                </td>
                                <td class="row_odd">
                                    <asp:DropDownList ID="ddlSchedulerOnOff" CssClass="usertextbox" runat="server">
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
                                    Schedule Minimum Depth
                                </td>
                                <td class="row_odd">
                                    <asp:TextBox ID="txtScheduleMinDepth" onkeypress="Javascript:return restrictEntryToNumber(event, this);"
                                        CssClass="usertextbox" runat="server" MaxLength="5" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_even">
                                    Schedule Max Days
                                </td>
                                <td class="row_even">
                                    <asp:TextBox ID="txtScheduleMaxDays" onkeypress="Javascript:return restrictEntryToNumber(event, this);"
                                        CssClass="usertextbox" runat="server" MaxLength="5" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_odd">
                                    Clean App Registry (minutes)
                                </td>
                                <td class="row_odd">
                                    <asp:TextBox ID="txtCleanAppRegistry" onkeypress="Javascript:return restrictEntryToNumber(event, this);"
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
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
