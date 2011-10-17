<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="notificationEdit.aspx.cs"
    Inherits="Web.pages.notificationEdit" MasterPageFile="~/pages/site.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="phDetail" runat="server">
    <script type="text/javascript" src="../script/notificationEdit.js"></script>
    <style type="text/css">
        .usertextbox
        {
            width: 300px;
        }
    </style>
    <div id="left_panel">
        <div class="left_tooltip">
            <span class="page_title">Manage Notifications</span>
            <img src="../images/terminal-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <img src="../images/tooltip.png" alt="" />
                    <p>
                        Change the settings for the Messenger process.</p>
                    <p>
                        The Messenger sends email alerts and notifications via email and needs to be configured
                        with your specific organization's SMTP settings.</p>
                </div>
            </div>
        </div>
    </div>
    <div id="content">
        <div id="content_form_outer">
            <div id="content_form_inner">
                <table width="100%">
                    <tr>
                        <td class="row_odd" width="150px">
                            Messenger On / Off
                        </td>
                        <td class="row_odd">
                            <asp:DropDownList ID="ddlMessengerOnOff" CssClass="usertextbox" runat="server">
                                <asp:ListItem>on</asp:ListItem>
                                <asp:ListItem>off</asp:ListItem>
                            </asp:DropDownList>
                        </td>
                    </tr>
                    <tr>
                        <td class="row_even">
                            Poll Loop
                        </td>
                        <td class="row_even">
                            <asp:TextBox ID="txtPollLoop" CssClass="usertextbox" runat="server" MaxLength="5" />
                        </td>
                    </tr>
                    <tr>
                        <td class="row_odd">
                            Retry Delay
                        </td>
                        <td class="row_odd">
                            <asp:TextBox ID="txtRetryDelay" CssClass="usertextbox" runat="server" MaxLength="5" />
                        </td>
                    </tr>
                    <tr>
                        <td class="row_even">
                            Retry Max Attempts
                        </td>
                        <td class="row_even">
                            <asp:TextBox ID="txtRetryMaxAttempts" CssClass="usertextbox" runat="server" MaxLength="5" />
                        </td>
                    </tr>
                    <tr>
                        <td class="row_odd">
                            SMTP Server Address
                        </td>
                        <td class="row_odd">
                            <asp:TextBox ID="txtSMTPServerAddress" CssClass="usertextbox" runat="server" MaxLength="256" />
                        </td>
                    </tr>
                    <tr>
                        <td class="row_even">
                            SMTP User Account
                        </td>
                        <td class="row_even">
                            <asp:TextBox ID="txtSMTPUserAccount" CssClass="usertextbox" runat="server" MaxLength="256" />
                        </td>
                    </tr>
                    <tr>
                        <td class="row_odd">
                            SMTP User Password
                        </td>
                        <td class="row_odd">
                            <asp:TextBox ID="txtSMTPUserPassword" CssClass="usertextbox" runat="server" value="($%#d@x!&"
                                MaxLength="512" TextMode="Password" />
                        </td>
                    </tr>
                    <tr>
                        <td class="row_even">
                            SMTP Password Confirm
                        </td>
                        <td class="row_even">
                            <asp:TextBox ID="txtSMTPPasswordConfirm" CssClass="usertextbox" runat="server" value="($%#d@x!&"
                                MaxLength="512" TextMode="Password" />
                        </td>
                    </tr>
                    <tr>
                        <td class="row_odd">
                            SMTP Server Port
                        </td>
                        <td class="row_odd">
                            <asp:TextBox ID="txtSMTPServerPort" CssClass="usertextbox" runat="server" MaxLength="5" />
                        </td>
                    </tr>
                    <tr>
                        <td class="row_even">
                            From Email
                        </td>
                        <td class="row_even">
                            <asp:TextBox ID="txtFromEmail" CssClass="usertextbox" runat="server" MaxLength="256" />
                        </td>
                    </tr>
                    <tr>
                        <td class="row_odd">
                            From Name
                        </td>
                        <td class="row_odd">
                            <asp:TextBox ID="txtFromName" CssClass="usertextbox" runat="server" MaxLength="256" />
                        </td>
                    </tr>
                    <tr>
                        <td class="row_even">
                            Administrative Emails
                        </td>
                        <td class="row_even">
                            <asp:TextBox ID="txtAdminEmail" CssClass="usertextbox" TextMode="MultiLine" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2" style="text-align: center;" class="row_odd">
                            <asp:Button ID="btnSave" runat="server" Text="Save" OnClientClick="ValidateValues();return false;" />
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
</asp:Content>
