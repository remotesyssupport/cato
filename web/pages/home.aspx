<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="home.aspx.cs" Inherits="Web.pages.home"
    MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <style type="text/css">
        #content
        {
            padding: 10px 10px 10px 10px;
			width: 600px;
			margin: 0 200px;
        }
    </style>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="content">
		<asp:Panel id="pnlGettingStarted" runat="server">
		    <div id="getting_started" class="ui-widget-content ui-corner-all" style="padding: 20px; margin-top: 20px;">
				<h1>Welcome to Cloud Sidekick Cato!</h1>
				<p>There are just a few more things you need to do to get started.</p>
				<asp:PlaceHolder id="phGettingStartedItems" runat="server"></asp:PlaceHolder>
			</div>
		</asp:Panel>
    </div>
	<div id="left_panel">
        <div id="group_tabs">
            <asp:PlaceHolder ID="phGroupTabs" runat="server"></asp:PlaceHolder>
        </div>
    </div>
    <div id="multi_asset_picker_dialog" class="hidden" title="Search for Assets">
    </div>
</asp:Content>
