<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="sharedCredentialEdit.aspx.cs"
    Inherits="Web.pages.sharedCredentialEdit" MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <script type="text/javascript" src="../script/managePageCommon.js"></script>
    <script type="text/javascript" src="../script/sharedCredentialEdit.js"></script>
    <div style="display: none;">
        <input id="hidMode" type="hidden" name="hidMode" />
        <input id="hidEditCount" type="hidden" name="hidEditCount" />
        <input id="hidCurrentEditID" type="hidden" name="hidCurrentEditID" />
        <input id="hidSelectedArray" type="hidden" name="hidSelectedArray" />
        <input id="hidCredentialID" type="hidden" name="hidCredentialID" />
        <input id="hidCredentialType" type="hidden" name="hidCredentialID" />
        <asp:HiddenField ID="hidSortColumn" runat="server" />
        <asp:HiddenField ID="hidPage" runat="server" />
        <!-- Start visible elements -->
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/manage-credentials-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <p>
                        <img src="../images/tooltip.png" alt="" />The Administer Shared Credentials screen
                        allows administrators to modify, add and delete credentials that will be shared
                        across multiple assets.</p>
                    <p>
                        Select a shared credential from the list or select an action to add or delete the
                        shared credentials you've selected.</p>
                </div>
            </div>
        </div>
    </div>
    <div id="content">
        <asp:UpdatePanel ID="uplList" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <span id="lblItemsSelected">0</span> Items Selected <span id="clear_selected_btn">
                </span><span id="item_create_btn">Create</span><span id="item_delete_btn">Delete</span>
                <asp:TextBox ID="txtSearch" runat="server" class="search_text" />
                <span id="item_search_btn">Search</span>
                <asp:ImageButton ID="btnSearch" class="hidden" OnClick="btnSearch_Click" runat="server" />
                <table class="jtable" cellspacing="1" cellpadding="1" width="99%">
                    <asp:Repeater ID="rptCredentials" runat="server">
                        <HeaderTemplate>
                            <tr>
                                <th class="chkboxcolumn">
                                </th>
                                <th sortcolumn="credential_name" width="100px">
                                    Credential
                                </th>
                                <th sortcolumn="username" width="100px">
                                    UserName
                                </th>
                                <th sortcolumn="domain" width="100px">
                                    Domain
                                </th>
                                <th sortcolumn="shared_cred_desc" width="350px">
                                    Description
                                </th>
                            </tr>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr credential_id="<%# (((System.Data.DataRowView)Container.DataItem)["credential_id"]) %>">
                                <td class="chkboxcolumn" id="<%# (((System.Data.DataRowView)Container.DataItem)["credential_id"]) %>">
                                    <input type="checkbox" class="chkbox" id="chk_<%# (((System.Data.DataRowView)Container.DataItem)["credential_id"]) %>"
                                        credential_id="<%# (((System.Data.DataRowView)Container.DataItem)["credential_id"]) %>"
                                        tag="chk" />
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["credential_name"])%>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["username"]) %>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["domain"]) %>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["shared_cred_desc"]) %>
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
    <div id="edit_dialog" class="hidden" title="Create Credential">
        <table width="100%">
            <tr>
                <td>
                    Name
                </td>
                <td>
                    <input id="txtCredName" class="usertextbox" name="txtCredName" />
                </td>
            </tr>
            <tr>
                <td>
                    Username
                </td>
                <td>
                    <input id="txtCredUsername" class="usertextbox" name="txtCredUsername" />
                </td>
            </tr>
            <tr>
                <td>
                    Domain
                </td>
                <td>
                    <input id="txtCredDomain" class="usertextbox" name="txtCredDomain" />
                </td>
            </tr>
            <tr>
                <td>
                    Description
                </td>
                <td>
                    <textarea id="txtCredDescription" class="usertextbox" cols="20" rows="8" name="txtCredDescription"></textarea>
                </td>
            </tr>
            <tr>
                <td>
                    Password
                </td>
                <td>
                    <input id="txtCredPassword" type="password" class="usertextbox" name="txtCredPassword" />
                </td>
            </tr>
            <tr>
                <td>
                    Password Confirm
                </td>
                <td>
                    <input id="txtCredPasswordConfirm" type="password" class="usertextbox" name="txtCredPasswordConfirm" />
                </td>
            </tr>
            <tr>
                <td colspan="2">
                    &nbsp;
                </td>
            </tr>
            <tr>
                <td colspan="2">
                    Privileged Mode Password
                </td>
            </tr>
            <tr>
                <td>
                    Password
                </td>
                <td>
                    <input id="txtPrivilegedPassword" type="password" class="usertextbox" name="txtPrivilegedPassword" />
                </td>
            </tr>
            <tr>
                <td>
                    Password Confirm
                </td>
                <td>
                    <input id="txtPrivilegedConfirm" type="password" class="usertextbox" name="txtPrivilegedConfirm" />
                </td>
            </tr>
        </table>
    </div>
    <div id="delete_dialog" class="hidden" title="Delete Credentials">
        Are you sure you want to delete these credentials?
    </div>
    <div style="display: none;">
        Anything you want to hide goes in here.
    </div>
</asp:Content>
