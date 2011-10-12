<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="userPreferenceEdit.aspx.cs"
    Inherits="Web.pages.userPreferenceEdit" MasterPageFile="~/pages/site.master" %>


<asp:Content ID="Content1" ContentPlaceHolderID="phDetail" runat="server">
    <asp:UpdatePanel ID="udpUserPreferences" runat="server">
        <ContentTemplate>
            <div id="left_panel">
                <div class="left_tooltip">
                    <span class="page_title">User Preferences</span>
                    <img src="../images/terminal-192x192.png" alt="" />
                    <div id="left_tooltip_box_outer">
                        <div id="left_tooltip_box_inner">
                            <p>
                                <img src="../images/tooltip.png" alt="" />User Preferences can be configured on
                                this screen.</p>
                        </div>
                    </div>
                </div>
            </div>
            <div id="content">
                <br />
                &nbsp;<br />
                <table width="98%">
                    <tr class="row_even">
                        <td>
                            Name
                        </td>
                        <td>
                            <asp:Label ID="lblName" runat="server" Text="Label"></asp:Label>
                        </td>
                    </tr>
                    <tr class="row_even">
                        <td>
                            Username
                        </td>
                        <td>
                            <asp:Label ID="lblUserName" runat="server" Text="Label"></asp:Label>
                        </td>
                    </tr>
                    <tr class="row_even">
                        <td>
                            Authentication Type
                        </td>
                        <td>
                            <asp:Label ID="lblAuthenticationType" runat="server" Text="Label"></asp:Label>
                        </td>
                    </tr>
                    <tr class="row_odd">
                        <td>
                            Email
                        </td>
                        <td>
                            <asp:TextBox ID="txtEmail" Width="350" runat="server"></asp:TextBox>
                            <asp:HiddenField ID="hidEmail" runat="server" />
                        </td>
                    </tr>
                    <asp:Panel ID="pnlLocalSettings" runat="server">
                        <tr class="row_even">
                            <td>
                                Password
                            </td>
                            <td>
                                <asp:TextBox ID="txtPassword" Width="350" TextMode="Password" runat="server"></asp:TextBox>
                            </td>
                        </tr>
                        <tr class="row_odd">
                            <td>
                                Confirm Password
                            </td>
                            <td>
                                <asp:TextBox ID="txtPasswordConfirm" Width="350" TextMode="Password" runat="server"></asp:TextBox>
                            </td>
                        </tr>
                        <tr class="row_even">
                            <td>
                                Security Question
                            </td>
                            <td>
                                <asp:TextBox ID="txtSecurityQuestion" TextMode="MultiLine" Rows="5" Width="350" runat="server"></asp:TextBox>
                            </td>
                        </tr>
                        <tr class="row_odd">
                            <td>
                                Security Answer
                            </td>
                            <td>
                                <asp:TextBox ID="txtSecurityAnswer" Width="350" TextMode="Password" runat="server"></asp:TextBox>
                                <asp:HiddenField ID="hidSecurityAnswer" runat="server" />
                            </td>
                        </tr>
                    </asp:Panel>
                    <tr class="row_even">
                        <td colspan="2" style="text-align: center;">
                            <asp:Button ID="btnSave" runat="server" Text="Save" OnClick="btnSave_Click" />
                        </td>
                    </tr>
                    <tr class="row_odd">
                        <td colspan="2">
                            <asp:Label ID="lblMessage" runat="server" />
                        </td>
                    </tr>
                </table>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
