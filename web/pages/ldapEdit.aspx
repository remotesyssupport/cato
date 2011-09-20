<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ldapEdit.aspx.cs" Inherits="Web.pages.ldapEdit"
    MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <script type="text/javascript" src="../script/managePageCommon.js"></script>
    <script type="text/javascript"src="../script/ldapEdit.js"></script>
    <div style="display: none;">
        <input id="hidMode" type="hidden" name="hidMode" />
        <input id="hidEditCount" type="hidden" name="hidEditCount" />
        <input id="hidCurrentEditID" type="hidden" name="hidCurrentEditID" />
        <input id="hidSelectedArray" type="hidden" name="hidSelectedArray" />
        <asp:HiddenField ID="hidSortColumn" runat="server" />
        <asp:HiddenField ID="hidPage" runat="server" />
        <!-- Start visible elements -->
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/manage-domains-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <p>
                        <img src="../images/tooltip.png" alt="" />The LDAP Connectivity Administration page
                        allows you to define LDAP domains and the domain controller addresses to which they
                        are associated.</p>
                    <p>
                        If your users will be logging in using their Active Directory or LDAP accounts,
                        add the Domains and the Domain Controller Addresses on this screen.
                    </p>
                </div>
            </div>
        </div>
    </div>
    <div id="content">
        <asp:UpdatePanel ID="uplList" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <span id="lblItemsSelected">0</span> Items Selected <a onclick="ClearSelectedRows();">
                    <img src="../images/buttons/clear_16.png" alt="" class="cancel_x" /></a>
                <img id="item_create_btn" src="../images/buttons/btnCreate.png" class="pointer btn_image"
                    alt="" />
                <img id="item_delete_btn" src="../images/buttons/btnDelete.png" class="pointer btn_image"
                    alt="" />
                <asp:TextBox ID="txtSearch" class="search_text" runat="server" />
                <span id="item_search_btn">Search</span>
                <asp:ImageButton ID="btnSearch" class="hidden" OnClick="btnSearch_Click" runat="server" />
                <table class="jtable" cellspacing="1" cellpadding="1" width="99%">
                    <asp:Repeater ID="rptDomains" runat="server">
                        <HeaderTemplate>
                            <tr>
                                <td sortcolumn="" style="width: 2%;">
                                </td>
                                <td sortcolumn="ldap_domain">
                                    Domain
                                </td>
                                <td sortcolumn="address">
                                    Address
                                </td>
                                <td sortcolumn="in_use">
                                    # of Credentials
                                </td>
                            </tr>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr domain="<%# (((System.Data.DataRowView)Container.DataItem)["ldap_domain"]) %>">
                                <td id="<%# (((System.Data.DataRowView)Container.DataItem)["ldap_domain"]) %>">
                                    <input type="checkbox" class="chkbox" id="chk_<%# (((System.Data.DataRowView)Container.DataItem)["ldap_domain"]) %>"
                                        domain="<%# (((System.Data.DataRowView)Container.DataItem)["ldap_domain"]) %>"
                                        tag="chk" assets="<%# (((System.Data.DataRowView)Container.DataItem)["in_use"]) %>" />
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["ldap_domain"]) %>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["address"]) %>
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["in_use"]) %>
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
    <div id="edit_dialog" class="hidden" title="Create a New Domain">
        <div id="DomainTab">
            <table id="tblDomainAdd" width="100%">
                <tbody>
                    <tr>
                        <td>
                            <div id="EditDomain">
                                <table width="100%">
                                    <tr>
                                        <td>
                                            Domain
                                        </td>
                                        <td>
                                            <input id="txtDomain" class="usertextbox" name="txtDomain" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            Address
                                        </td>
                                        <td>
                                            <input id="txtAddress" class="usertextbox" name="txtAddress" />
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
    <div id="delete_dialog" class="hidden" title="Delete Domains">
        Are you sure you want to delete these domains?
    </div>
    <div style="display: none;">
        Anything you want to hide goes in here.
    </div>
</asp:Content>
