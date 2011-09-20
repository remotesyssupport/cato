<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="logserverSettings.aspx.cs"
    Inherits="Web.pages.logserverSettings" MasterPageFile="~/pages/site.master" %>


<asp:Content ID="Content1" ContentPlaceHolderID="phDetail" runat="server">

  
    <style type="text/css">
        .usertextbox
        {
            width: 300px;
        }
    </style>
    <script type="text/javascript">

        function ValidateValues() {

            // both the seconds and max processes must be greater than 0
            if ($("#ctl00_phDetail_txtPort").val() == '0') { 
                showAlert('The Port must be a positive number.'); 
                return false;         
            }
            if ($("#ctl00_phDetail_txtLoopDelay").val() == '0') { 
                showAlert('The Loop Delay must be greater than 0.'); 
                return false;         
            }
            return true;
        }
    </script>
    <asp:UpdatePanel ID="udpNotifications" runat="server">
        <ContentTemplate>
            <div id="left_panel">
                <div class="left_tooltip">
                    <span class="page_title">Log Server Settings</span>
                    <img src="../images/terminal-192x192.png" alt="" />
                    <div id="left_tooltip_box_outer">
                        <div id="left_tooltip_box_inner">
                            <img src="../images/tooltip.png" alt="" />
                            <p>Change the settings for the Log Server process.</p>
                        </div>
                    </div>
                </div>
            </div>
            <div id="content">
                <div id="content_form_outer">
                    <div id="content_form_inner">
                        <table width="100%">
                            <tr>
                                <td width="150" class="row_odd">
                                    Log Server On / Off
                                </td>
                                <td class="row_odd">
                                    <asp:DropDownList ID="ddlOnOff" CssClass="usertextbox" runat="server">
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
                                    <asp:TextBox ID="txtLoopDelay" onkeypress="Javascript:return restrictEntryToNumber(event, this);" CssClass="usertextbox" runat="server" MaxLength="5" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_odd">
                                    Listen on Port
                                </td>
                                <td class="row_odd">
                                    <asp:TextBox ID="txtPort" onkeypress="Javascript:return restrictEntryToNumber(event, this);" CssClass="usertextbox" runat="server" MaxLength="5" />
                                </td>
                            </tr>
                                                        <tr>
                                <td class="row_even">
                                    Delete Log Files Older Than n Days?
                                </td>
                                <td class="row_even">
                                    <asp:TextBox ID="txtLogFileDays" onkeypress="Javascript:return restrictEntryToNumber(event, this);" CssClass="usertextbox" runat="server" MaxLength="5" />
                                </td>
                            </tr>
                            <tr>
                                <td class="row_odd">
                                    Delete Task Logs Older Than n Days?
                                </td>
                                <td class="row_odd">
                                    <asp:TextBox ID="txtLogTableDays" onkeypress="Javascript:return restrictEntryToNumber(event, this);" CssClass="usertextbox" runat="server" MaxLength="5" />
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
