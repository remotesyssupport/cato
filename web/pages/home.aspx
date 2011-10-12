<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="home.aspx.cs" Inherits="Web.pages.home"
    MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <style type="text/css">
        #content
        {
            padding: 10px 10px 10px 10px;
        }
    </style>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="content">
        <div id="dash_content">
        </div>
    </div>
    <div id="left_panel">
        <div id="group_tabs">
            <asp:PlaceHolder ID="phGroupTabs" runat="server"></asp:PlaceHolder>
        </div>
    </div>
    <div id="multi_asset_picker_dialog" class="hidden" title="Search for Assets">
    </div>
</asp:Content>
