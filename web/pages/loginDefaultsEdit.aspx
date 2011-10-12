<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="loginDefaultsEdit.aspx.cs"
    Inherits="Web.pages.loginDefaultsEdit" MasterPageFile="~/pages/site.master" ValidateRequest="false" %>


<asp:Content ID="Content1" ContentPlaceHolderID="phDetail" runat="server">
    <asp:UpdatePanel ID="udpLoginDefaults" runat="server">
        <ContentTemplate>
            <div id="left_panel">
                <div class="left_tooltip">
                    <img src="../images/login-defaults-192x192.png" alt="" />
                    <div id="left_tooltip_box_outer">
                        <div id="left_tooltip_box_inner">
                            <p>
                                <img src="../images/tooltip.png" alt="" />Setting login defaults is an important
                                part of maintaining proper security.</p>
                            <p>
                                By altering these settings, you can change the global password policy.</p>
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
                                    Password Maximum Age
                                </td>
                                <td width="300" class="row_odd">
                                    <asp:TextBox ID="txtPassMaxAge" runat="server" MaxLength="5" />
                                    <asp:RequiredFieldValidator ID="rfvPassMaxAge" runat="server" Text="Required" ControlToValidate="txtPassMaxAge"
                                        Display="Dynamic" />
                                    <asp:RangeValidator ID="rvPassMaxAge" runat="server" ErrorMessage="RangeValidator"
                                        Type="Integer" MaximumValue="9999" MinimumValue="0" ControlToValidate="txtPassMaxAge"
                                        Text="Must be between 0 and 9999" Display="Dynamic" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_even">
                                    Password Maximum Attempts
                                </td>
                                <td class="row_even">
                                    <asp:TextBox ID="txtPassMaxAttempts" runat="server" MaxLength="5" />
                                    <asp:RequiredFieldValidator ID="rfvPassMaxAttempts" runat="server" Text="Required"
                                        Display="Dynamic" ControlToValidate="txtPassMaxAttempts" />
                                    <asp:RangeValidator ID="rvPassMaxAttempts" runat="server" ErrorMessage="RangeValidator"
                                        Type="Integer" MaximumValue="9999" MinimumValue="0" ControlToValidate="txtPassMaxAge"
                                        Text="Must be between 0 and 9999" Display="Dynamic" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_odd">
                                    Password Maximum Length
                                </td>
                                <td class="row_odd">
                                    <asp:TextBox ID="txtPassMaxLength" runat="server" MaxLength="5" />
                                    <asp:RequiredFieldValidator ID="rfvPassMaxLength" runat="server" ErrorMessage="Required"
                                        Display="Dynamic" ControlToValidate="txtPassMaxLength" />
                                    <asp:RangeValidator ID="rvPassMaxLength" runat="server" ErrorMessage="RangeValidator"
                                        Type="Integer" MaximumValue="100" MinimumValue="0" ControlToValidate="txtPassMaxLength"
                                        Text="Must be between 0 and 100" Display="Dynamic" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_even">
                                    Password Minimum Length
                                </td>
                                <td class="row_even">
                                    <asp:TextBox ID="txtPassMinLength" runat="server" MaxLength="5" />
                                    <asp:RequiredFieldValidator ID="rfvPassMinLength" runat="server" ErrorMessage="Required"
                                        ControlToValidate="txtPassMinLength" Display="Dynamic" />
                                    <asp:RangeValidator ID="rvPassMinLength" runat="server" ErrorMessage="RangeValidator"
                                        Type="Integer" MaximumValue="100" MinimumValue="0" ControlToValidate="txtPassMinLength"
                                        Text="Must be between 0 and 100" Display="Dynamic" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_odd">
                                    Complex Password?
                                </td>
                                <td class="row_odd">
                                    <asp:CheckBox ID="cbPassComplexity" runat="server" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_even">
                                    Password Age Warning Days
                                </td>
                                <td class="row_even">
                                    <asp:TextBox ID="txtPassAgeWarn" runat="server" MaxLength="5" />
                                    <asp:RequiredFieldValidator ID="rfvPassAgeWarn" runat="server" ErrorMessage="Required"
                                        ControlToValidate="txtPassAgeWarn" Display="Dynamic" />
                                    <asp:RangeValidator ID="rvPassAgeWarn" runat="server" ErrorMessage="RangeValidator"
                                        Type="Integer" MaximumValue="9999" MinimumValue="0" ControlToValidate="txtPassAgeWarn"
                                        Text="Must be between 0 and 9999" Display="Dynamic" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_odd">
                                    Password History
                                </td>
                                <td class="row_odd">
                                    <asp:TextBox ID="txtPasswordHistory" runat="server" MaxLength="5" />
                                    <asp:RequiredFieldValidator ID="rfvPasswordHistory" runat="server" ErrorMessage="Required"
                                        ControlToValidate="txtPasswordHistory" Display="Dynamic" />
                                    <asp:RangeValidator ID="rvPasswordHistory" runat="server" ErrorMessage="RangeValidator"
                                        Type="Integer" MaximumValue="9999" MinimumValue="0" ControlToValidate="txtPasswordHistory"
                                        Text="Must be between 0 and 9999" Display="Dynamic" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_even">
                                    Require Password Change on First Login?
                                </td>
                                <td class="row_even">
                                    <asp:CheckBox ID="cbPassRequireInitialChange" runat="server" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_odd">
                                    Auto Lock Reset
                                </td>
                                <td class="row_odd">
                                    <asp:TextBox ID="txtAutoLockReset" runat="server" MaxLength="5" />
                                    <asp:RequiredFieldValidator ID="rfvAutoLockReset" runat="server" ErrorMessage="Required"
                                        Display="Dynamic" ControlToValidate="txtAutoLockReset" />
                                    <asp:RangeValidator ID="rvAutoLockReset" runat="server" ErrorMessage="RangeValidator"
                                        Type="Integer" MaximumValue="9999" MinimumValue="0" ControlToValidate="txtAutoLockReset"
                                        Text="Must be between 0 and 9999" Display="Dynamic" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_even">
                                    Login Message
                                </td>
                                <td class="row_even">
                                    <asp:TextBox ID="txtLoginMessage" runat="server" TextMode="MultiLine" Rows="4" Width="500"
                                        MaxLength="8192" />
                                    <asp:RequiredFieldValidator ID="rfvLoginMessage" runat="server" ErrorMessage="Required"
                                        ControlToValidate="txtLoginMessage" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_odd">
                                    Authentication Error Message
                                </td>
                                <td class="row_odd">
                                    <asp:TextBox ID="txtAuthErrorMessage" runat="server" TextMode="MultiLine" Width="500"
                                        Rows="4" />
                                    <asp:RequiredFieldValidator ID="rfvAuthErrorMessage" runat="server" ErrorMessage="Required"
                                        ControlToValidate="txtAuthErrorMessage" />
                                </td>
                            </tr>
                             <tr>
                                <td class="row_even">
                                    New User Welcome Email
                                </td>
                                <td class="row_even">
                                    <asp:TextBox ID="txtNewUserMessage" runat="server" TextMode="MultiLine" Rows="4" Width="500"
                                        MaxLength="8192" />
                                </td>
                            </tr>
                           <tr>
                                <td class="row_odd">
                                    Verbose Page View Logging
                                </td>
                                <td class="row_odd">
                                    <asp:CheckBox ID="cbPageViewLogging" runat="server" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_even">
                                    Report View Logging
                                </td>
                                <td class="row_even">
                                    <asp:CheckBox ID="cbReportViewLogging" runat="server" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_odd">
                                    Allow Users to Log In?
                                </td>
                                <td class="row_odd">
                                    <asp:CheckBox ID="cbAllowLogin" runat="server" />
                                </td>
                            </tr>
                            <tr>
                                <td colspan="2" class="row_odd" height="25">
                                    <asp:Label ID="lblMessage" runat="server" />
                                </td>
                            </tr>
                            <tr>
                                <td colspan="2" style="text-align: center;" height="25" class="row_odd">
                                    <asp:Button ID="btnSave" runat="server" OnClick="btnSave_Click" Text="Save" />
                                </td>
                            </tr>
                        </table>
                    </div>
                </div>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
