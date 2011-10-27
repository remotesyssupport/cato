<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="awsDiscovery.aspx.cs" Inherits="Web.pages.awsDiscovery"
    MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <style type="text/css">
        #content
        {
            padding: 20px 10px 10px 10px;
        }
        #left_panel
        {
            padding: 20px 0px 0px 0px;
        }
    </style>
    <script type="text/javascript" src="../script/awsDiscovery.js"></script>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div style="display: none;">
        <input id="hidEditCount" type="hidden" name="hidEditCount" />
        <input id="hidCloudObjectType" type="hidden" name="hidEcosystemObjectType" />
        <input id="hidSelectedArray" type="hidden" name="hidSelectedArray" />
        <!-- Start visible elements -->
    </div>
    <div id="content">
        <div class="page_title">
            Ecosystem Discovery</div>
        <span id="lblItemsSelected">0</span> Items Selected <a class="ui-button ui-widget ui-state-default ui-corner-all ui-button-icon-only" onclick="ClearSelectedRows();">
            <span class="ui-button-icon-primary ui-icon ui-icon-refresh"></span><span class="ui-button-text">
                </span></a>
        <span>Select an Ecosystem:</span>
        <asp:DropDownList ID="ddlEcosystems" runat="server">
        </asp:DropDownList>
        <span id="add_to_ecosystem_btn">Add</span>
        <hr />
        <h2 id="results_label">
        </h2>
        <div id="results_list">
            <asp:Literal ID="ltResults" runat="server"></asp:Literal>
        </div>
    </div>
    <div id="left_panel">
        <div id="group_tabs">
			<h3>Cloud</h3>
			<asp:DropDownList id="ddlClouds" runat="server"></asp:DropDownList>
			<hr />
            <ul>
                <asp:Literal ID="ltTabs" runat="server"></asp:Literal>
            </ul>
        </div>
    </div>
</asp:Content>
