<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="cloudEdit.aspx.cs"
    Inherits="Web.pages.cloudEdit" MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <script type="text/javascript" src="../script/managePageCommon.js"></script>    
	<script type="text/javascript" src="../script/cloudEdit.js"></script>
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
            <img src="../images/manage-cld_acc-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <p>
                        <img src="../images/tooltip.png" alt="" />The Clouds screen allows administrators
                        to modify, add and delete Clouds.</p>
                    <p>
                        Select a Cloud from the list or select an action to edit or delete the Clouds
                        you've selected.</p>
                </div>
            </div>
        </div>
    </div>
    <div id="content">
        <span id="lblItemsSelected">0</span> Items Selected <span id="clear_selected_btn">
        </span><span id="item_create_btn">Create</span><span id="item_delete_btn">Delete</span>
        <asp:TextBox ID="txtSearch" runat="server" class="search_text" />
        <span id="item_search_btn">Search</span>
		<div id="clouds">
			<asp:Literal id="ltClouds" runat="server"></asp:Literal>
		</div>
    </div>
    <div id="edit_dialog" class="hidden" title="Create Cloud">
        <table width="100%">
            <tr>
                <td>
                    Cloud Name:
                </td>
                <td>
                    <input id="txtCloudName" class="usertextbox" name="txtCloudName" />
                </td>
            </tr>
            <tr>
                <td>
                    Provider:
                </td>
                <td>
                    <select id="ddlProvider">
                        <asp:Literal id="ltProviders" runat="server"></asp:Literal>
                    </select>
                </td>
            </tr>
            <tr>
                <td>
                    API URL:
                </td>
                <td>
                    <input id="txtAPIUrl" class="usertextbox" name="txtAPIUrl" />
                </td>
            </tr>
             <tr>
                <td colspan="2">
					<hr />
				</td>
            </tr>
             <tr>
                <td>
					Test Account:
                </td>
                <td>
					<asp:DropDownList id="ddlTestAccount" runat="server"></asp:DropDownList>
				</td>
            </tr>
			<tr>
				<td colspan="2" style="text-align: center;">
					<span id="test_connection_btn">Test Connection</span>
				</td>
			</tr>
             <tr>
                <td colspan="2">
					<div id="conn_test_result" style="margin-top: 10px;"></div>
					<div id="conn_test_error" style="font-size: 0.8em; font-style: italic; margin-top: 10px;"></div>
			</td>
            </tr>
        </table>
    </div>
    <div id="delete_dialog" class="hidden" title="Delete Clouds">
        Are you sure you want to delete these Clouds?
    </div>
</asp:Content>
