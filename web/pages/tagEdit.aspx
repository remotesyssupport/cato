<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="tagEdit.aspx.cs" Inherits="Web.pages.tagEdit"
    MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <script type="text/javascript" src="../script/managePageCommon.js"></script>
    <script type="text/javascript" src="../script/validation.js"></script>
    <script type="text/javascript" src="../script/tagEdit.js"></script>
    <div style="display: none;">
        <input id="hidMode" type="hidden" name="hidMode" />
        <input id="hidCurrentEditID" type="hidden" name="hidCurrentEditID" />
        <input id="hidSelectedArray" type="hidden" name="hidSelectedArray" />
        <asp:HiddenField ID="hidSortColumn" runat="server" />
        <asp:HiddenField ID="hidPage" runat="server" />
        <!-- Start visible elements -->
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <br />
            <span class="page_title">Manage Tags</span>
            <img src="../images/manage-tags-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <p>
                        <img src="../images/tooltip.png" alt="" />Tags are used to group objects together
                        for various reasons, including access control and reporting.
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
                    <asp:Repeater ID="rptTags" runat="server">
                        <HeaderTemplate>
                            <tr>
                                <td sortcolumn="" style="width: 2%;">
                                </td>
                                <td sortcolumn="tag_name">
                                    Tag
                                </td>
                                <td sortcolumn="tag_desc">
                                    Description
                                </td>
                                <td sortcolumn="in_use">
                                    # of References
                                </td>
                            </tr>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr tag_name="<%# (((System.Data.DataRowView)Container.DataItem)["tag_name"]) %>"
                                desc_id="d_<%# (((System.Data.DataRowView)Container.DataItem)["id"]) %>">
                                <td id="<%# (((System.Data.DataRowView)Container.DataItem)["tag_name"]) %>">
                                    <input type="checkbox" class="chkbox" id="chk_<%# (((System.Data.DataRowView)Container.DataItem)["tag_name"]) %>"
                                        tag_name="<%# (((System.Data.DataRowView)Container.DataItem)["tag_name"]) %>"
                                        tag="chk" assets="<%# (((System.Data.DataRowView)Container.DataItem)["in_use"]) %>" />
                                </td>
                                <td tag="selectable">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["tag_name"]) %>
                                </td>
                                <td tag="selectable" id="d_<%# (((System.Data.DataRowView)Container.DataItem)["id"]) %>">
                                    <%# (((System.Data.DataRowView)Container.DataItem)["tag_desc"]) %>
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
    <div id="edit_dialog" class="hidden" title="Create a New Tag">
        <div>
            Tag:</div>
        <input id="txtTag" name="txtTag" class="w95pct" validate_as="tag" />
        <br />
        <div>
            Description:</div>
        <textarea class="w95pct" rows="3" id="txtDescription" name="txtDescription"></textarea>
    </div>
    <div id="delete_dialog" class="hidden" title="Delete Tags">
        Are you sure you want to delete the selected Tag(s)?
    </div>
</asp:Content>
