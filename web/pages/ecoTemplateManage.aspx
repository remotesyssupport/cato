<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ecoTemplateManage.aspx.cs"
    Inherits="Web.pages.ecoTemplateManage" MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <script type="text/javascript" src="../script/managePageCommon.js"></script>
    <script type="text/javascript" src="../script/ecoTemplateManage.js"></script>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div style="display: none;">
        <input id="hidMode" type="hidden" name="hidMode" />
        <input id="hidEditCount" type="hidden" name="hidEditCount" />
        <input id="hidCurrentEditID" type="hidden" name="hidCurrentEditID" />
        <input id="hidSelectedArray" type="hidden" name="hidSelectedArray" />
        <asp:HiddenField ID="hidSortColumn" runat="server" />
        <asp:HiddenField ID="hidSortDirection" runat="server" />
        <asp:HiddenField ID="hidLastSortColumn" runat="server" />
        <asp:HiddenField ID="hidPage" runat="server" />
        <!-- Start visible elements -->
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/terminal-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <p>
                        <img src="../images/tooltip.png" alt="" />The Manage Eco Templates screen allows
                        administrators to modify, add and delete Templates for Ecosystems.</p>
                    <p>
                        Select one or more Templates from the list to the right, using the checkboxes. Select
                        an action to modify or delete the Templates you've selected.</p>
                </div>
            </div>
        </div>
    </div>
    <div id="content">
        <asp:UpdatePanel ID="udpList" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <span id="lblItemsSelected">0</span> Items Selected <span id="clear_selected_btn">
                </span><span id="item_create_btn">Create</span><span id="item_delete_btn">Delete</span>
                <asp:TextBox ID="txtSearch" runat="server" class="search_text" />
                <span id="item_search_btn">Search</span>
                <asp:ImageButton ID="btnSearch" class="hidden" OnClick="btnSearch_Click" runat="server" />
                <table class="jtable" cellspacing="1" cellpadding="1" width="99%">
                    <asp:Repeater ID="rpTasks" runat="server">
                        <HeaderTemplate>
                            <tr>
                                <th class="chkboxcolumn">
                                    <input class="chkbox" type="checkbox" id="chkAll" />
                                </th>
                                <th sortcolumn="ecotemplate_name" width="200px">
                                    <img id="imgUpecotemplate_name" src="../images/UpArrow.gif" class="hidden" alt="" />
                                    <img id="imgDownecotemplate_name" src="../images/DnArrow.gif" class="hidden" alt="" />
                                    Name
                                </th>
                                <th sortcolumn="ecotemplate_desc" width="200px">
                                    <img id="imgUpecotemplate_desc" src="../images/UpArrow.gif" class="hidden" alt="" />
                                    <img id="imgDownecotemplate_desc" src="../images/DnArrow.gif" class="hidden" alt="" />
                                    Description
                                </th>
                            </tr>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr ecotemplate_id="<%#Eval("ecotemplate_id")%>">
                                <td class="chkboxcolumn">
                                    <input type="checkbox" class="chkbox" id="chk_<%#Eval("ecotemplate_id")%>" ecotemplate_id="<%#Eval("ecotemplate_id")%>"
                                        tag="chk" />
                                </td>
                                <td tag="selectable">
                                    <%#Eval("ecotemplate_name")%>
                                </td>
                                <td tag="selectable">
                                    <%#Eval("ecotemplate_desc")%>
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
    <div id="edit_dialog" class="hidden" title="New Eco Template">
        <table id="tblNew" width="100%">
            <tbody>
                <tr>
                    <td colspan="2">
                        <span id="lblNewMessage" />
                    </td>
                </tr>
                <tr>
                    <td>
                        Name
                    </td>
                    <td>
                        <asp:TextBox ID="txtTemplateName" runat="server" class="usertextbox" Style="width: 300px;"
                            MaxLength="256" jqname="txtTemplateName"></asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td>
                        Description
                    </td>
                    <td>
                        <asp:TextBox ID="txtTemplateDesc" runat="server" class="usertextbox" Style="width: 300px;"
                            jqname="txtTemplateDesc" TextMode="MultiLine"></asp:TextBox>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
    <div id="delete_dialog" class="hidden" title="Delete Eco Templates">
        Are you sure you want to delete these Eco Templates?
    </div>
</asp:Content>
