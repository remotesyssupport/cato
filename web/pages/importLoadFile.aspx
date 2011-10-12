<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="importLoadFile.aspx.cs"
    Inherits="Web.pages.importLoadFile" MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="content">
        Select a file to import.<br />
        <input type="file" id="fupFile" runat="server" />
        <asp:Button ID="btnImport" runat="server" Text="Load File" OnClick="btnImport_Click" />
        <asp:Panel ID="pnlResume" runat="server">
            -OR-<br />
            <a href="importReconcile.aspx">Resume reconcilation of the last import session.
            </a>
        </asp:Panel>
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/home-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                        <img src="../images/tooltip.png" alt="" />
                        <div id="help_text">
                            Select an Export package to Import. This will be a .zip file format.</div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
