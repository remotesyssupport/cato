<%@ Page Language="C#" AutoEventWireup="True" CodeBehind="taskPrint.aspx.cs" Inherits="Web.pages.taskPrint"
    MasterPageFile="~/pages/popupwindow.master" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <link href="../style/taskEdit.css" rel="stylesheet" type="text/css" />
    <link href="../style/taskView.css" rel="stylesheet" type="text/css" />
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="printable_content" class="printable_content">
        <center>
            <h3>
                <asp:Label ID="lblTaskNameHeader" runat="server"></asp:Label>
                <span id="version_tag">Version:
                    <asp:Label ID="lblVersionHeader" runat="server"></asp:Label></span></h3>
        </center>
        <asp:ImageButton ID="btnExport" Visible="false" runat="server" ImageUrl="../images/buttons/pdficon_large.gif"
            OnClick="btnExport_Click" />
        <asp:HiddenField ID="hidExport" runat="server" />
        <div class="codebox">
            <span class="detail_label">Code:</span>
            <asp:Label ID="lblTaskCode" runat="server" CssClass="code" Style="font-size: 1.2em;"></asp:Label>
            <br />
            <span class="detail_label">Status: </span>
            <asp:Label ID="lblStatus" runat="server" CssClass="code" Style="font-size: 1.2em;"></asp:Label>
            <br />
            <span class="detail_label">Concurrent Instances:</span>
            <asp:Label ID="lblConcurrentInstances" runat="server" CssClass="task_details code"
                Style="font-size: 1.2em;"></asp:Label>
            <br />
            <span class="detail_label">Number to Queue:</span>
            <asp:Label ID="lblQueueDepth" runat="server" CssClass="task_details code" Style="font-size: 1.2em;"></asp:Label>
            <br />
            <span class="detail_label">Description:</span>
            <br />
            <asp:Label ID="lblDescription" TextMode="MultiLine" Rows="10" runat="server" CssClass="code"
                Style="font-size: 1.2em;"></asp:Label>
            <!-- *** Commented out per Bug 1029 ***
                     <asp:Label ID="lblDirect" runat="server" CssClass="code"></asp:Label>
                        Direct to Asset?<br />-->
        </div>
        <hr />
        <div id="div_attributes" class="">
            <h3>
                Asset Attributes</h3>
            <div>
                <div id="attribute_groups">
                    <asp:Repeater ID="rpAttributeGroups" runat="server" OnItemDataBound="rpAttributeGroups_ItemDataBound">
                        <HeaderTemplate>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <div class="view_attribute_group">
                                <div class="view_attribute_group_title">
                                    &nbsp;
                                </div>
                                <div>
                                    <asp:Repeater ID="rpGroupAttributes" runat="server">
                                        <ItemTemplate>
                                            <div class="qview_attribute">
                                                <div class="qview_attribute_title">
                                                    <%#(((System.Data.DataRowView)Container.DataItem)["attribute_name"])%>
                                                    ::
                                                    <%#(((System.Data.DataRowView)Container.DataItem)["attribute_value"])%>
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </div>
        <hr />
        <div id="div_parameters" class="">
            <h3>
                Parameters</h3>
            <asp:PlaceHolder ID="phParameters" runat="server"></asp:PlaceHolder>
        </div>
        <hr />
        <div id="div_steps" class="">
            <h3>
                Steps</h3>
            <ul id="codeblock_steps">
                <asp:PlaceHolder ID="phSteps" runat="server"></asp:PlaceHolder>
            </ul>
        </div>
    </div>
</asp:Content>
