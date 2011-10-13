<!--
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
-->
<%@ Page Language="C#" AutoEventWireup="True" CodeBehind="login.aspx.cs" Inherits="Web.login" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>
        <asp:Literal ID="ltTitle" runat="server"></asp:Literal></title>
    <link type="text/css" href="style/login.css" rel="stylesheet" />
    <link rel="icon" href="images/favicon.png" />
    <script type="text/javascript" src="script/jquery/jquery-1.6.1.js"></script>
    <script type="text/javascript">
        $(document).ready(function () {
            $('#txtLoginUser').focus();
        });
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <div id="wrapper">
        <div>
            <img alt="" src="images/login-logo.png" />
        </div>
        <div id="container">
            <div id="loginbox">
                <div class="loginerror">
                    <asp:Label ID="lblErrorMessage" runat="server" />
                </div>
                <asp:Label ID="lblMessage" runat="server" />
                <asp:Panel ID="pnlLogin" runat="server">
                    <table style="width: 100%;">
                        <tr>
                            <td>
                                <asp:Label ID="lblLoginUser" runat="server" Text="Username:" />
                            </td>
                            <td>
                                <asp:TextBox ID="txtLoginUser" Width="255" autocomplete="off" runat="server" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="lblLoginPassword" runat="server" Text="Password:" />
                            </td>
                            <td>
                                <asp:TextBox ID="txtLoginPassword" Width="255" runat="server" autocomplete="off"
                                    TextMode="Password" />
                            </td>
                        </tr>
                        <tr>
                            <td style="text-align: center;" colspan="2">
                                <asp:Button ID="btnLogin" runat="server" Text="Login" OnClick="btnLogin_Click" />
                            </td>
                        </tr>
                    </table>
                    <div>
                        <a class="pointer" onclick="$('#btnForgotPassword').click();">Forgot password?
                            <img src="images/icons/password.png" alt="" style="vertical-align: top;" />
                        </a>
                        <div style="display: none;">
                            <asp:Button ID="btnForgotPassword" runat="server" Text="testlogout" OnClick="ForgotPassword_Click" /></div>
                    </div>
                </asp:Panel>
                <asp:Panel ID="pnlForgotPassword" Visible="false" runat="server">
                    <table>
                        <tr>
                            <td colspan="2">
                                <b>
                                    <asp:Label ID="lblRecoverMessage" runat="server" /></b>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                Username:
                            </td>
                            <td>
                                <asp:Label ID="lblRecoverUserName" runat="server"></asp:Label>
                            </td>
                        </tr>
                        <asp:Panel ID="pnlPasswordReset" Visible="false" runat="server">
                            <tr>
                                <td>
                                    Question:
                                </td>
                                <td>
                                    <asp:Label ID="lblSecurityQuestion" runat="server" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    Answer:
                                </td>
                                <td>
                                    <asp:TextBox ID="txtSecurityAnswer" Width="250px" runat="server" TextMode="Password" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    Password:
                                </td>
                                <td>
                                    <asp:TextBox ID="txtResetPassword" Width="250px" runat="server" TextMode="Password" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    Password Confirm:
                                </td>
                                <td>
                                    <asp:TextBox ID="txtResetPasswordConfirm" Width="250px" runat="server" TextMode="Password" />
                                </td>
                            </tr>
                            <tr>
                                <td style="text-align: center;" colspan="2">
                                    <asp:Button ID="btnResetPassword" runat="server" OnClick="btnResetPassword_Click"
                                        Text="Save Password" />
                                    <asp:Button ID="btnCancelPasswordReset" runat="server" OnClick="btnCancelPasswordReset_Click"
                                        Text="Cancel" />
                                </td>
                            </tr>
                        </asp:Panel>
                        <asp:Panel ID="pnlNoQaOnFile" Visible="false" runat="server">
                            <tr>
                                <td colspan="2">
                                    <asp:Button ID="btnCancelNoQa" runat="server" OnClick="btnCancelPasswordReset_Click"
                                        Text="Cancel" />
                                </td>
                            </tr>
                        </asp:Panel>
                    </table>
                </asp:Panel>
                <asp:Panel ID="pnlResetPassword" Visible="false" runat="server">
                    <table>
                        <tr>
                            <td colspan="2">
                                <b>
                                    <asp:Label ID="lblResetMessage" runat="server" /></b>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                User:
                            </td>
                            <td>
                                <asp:Label ID="lblUserName" runat="server" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                Current Password:
                            </td>
                            <td>
                                <asp:TextBox ID="txtCurrentPassword" Width="200px" runat="server" TextMode="Password" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                New Password:
                            </td>
                            <td>
                                <asp:TextBox ID="txtNewPassword" Width="200px" runat="server" TextMode="Password" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                New Password Confirm:
                            </td>
                            <td>
                                <asp:TextBox ID="txtNewPasswordConfirm" Width="200px" runat="server" TextMode="Password" />
                            </td>
                        </tr>
                        <tr>
                            <td style="text-align: center;" colspan="2">
                                <asp:Button ID="btnNewPassword" runat="server" OnClick="btnUpdatePassword_Click"
                                    Text="Update Password" />
                                <asp:Button ID="btnCancelNewPassword" runat="server" OnClick="btnCancelPasswordReset_Click"
                                    Text="Cancel" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>
                <asp:Panel ID="pnlExpireWarning" Visible="false" runat="server">
                    <table>
                        <tr>
                            <td colspan="2">
                                <b>
                                    <asp:Label ID="lblExpireWarning" runat="server" /></b>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                User:
                            </td>
                            <td>
                                <asp:Label ID="lblWarnUsername" runat="server" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                Current Password:
                            </td>
                            <td>
                                <asp:TextBox ID="txtWarningCurrentPassword" Width="200px" runat="server" TextMode="Password"
                                    Text="" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                New Password:
                            </td>
                            <td>
                                <asp:TextBox ID="txtWarningNewPassword" Width="200px" runat="server" TextMode="Password" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                New Password Confirm:
                            </td>
                            <td>
                                <asp:TextBox ID="txtWarningNewPasswordConfirm" Width="200px" runat="server" TextMode="Password" />
                            </td>
                        </tr>
                        <tr>
                            <td style="text-align: center;" colspan="2">
                                <asp:Button ID="btnExpiredPassword" runat="server" OnClick="btnExpiredPassword_Click"
                                    Text="Update Password" />
                                <asp:Button ID="btnUpdateLater" runat="server" OnClick="btnUpdateLater_Click" Text="Update Later" />
                                <asp:HiddenField ID="hidUpdateLater" runat="server" Value="now" />
                                <asp:HiddenField ID="hidID" runat="server" Value="" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>
            </div>
        </div>
    </div>
    </form>
</body>
</html>
