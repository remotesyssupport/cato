<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="about.aspx.cs" Inherits="Web.pages.about"
    MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    
    <div id="content">
        <div style="padding: 40px 0 0 40px;">
            <h2>
                <asp:Label runat="server" ID="lblAbout"></asp:Label></h2>
            <br />
            <b>On the Web: </b>
            <asp:HyperLink ID="lnkWeb" runat="server"></asp:HyperLink><br />
            <br />
            <b>Web Environment: </b>
            <asp:Label runat="server" ID="lblEnv"></asp:Label><br />
            <b>Application Instance: </b>
            <asp:Label runat="server" ID="lblInstance"></asp:Label><br />
            <br />
            <b>Database Server: </b>
            <asp:Label runat="server" ID="lblDatabaseAddress"></asp:Label><br />
            <b>Database: </b>
            <asp:Label runat="server" ID="lblDatabaseName"></asp:Label><br />
            <br />
            <b>Version: </b>
            <asp:Label runat="server" ID="lblVersion"></asp:Label><br />
            <b>Build: </b>
            <asp:Label runat="server" ID="lblBuild"></asp:Label><br />
            <br />
            <p>
                To report an issue or enhancement request, send an email to
                <asp:HyperLink ID="lnkEmail" runat="server"></asp:HyperLink><br />
                Include as much detail as possible including screenshots to describe the issue.</p>
        </div>
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/about-192x192.png" alt="" />
        </div>
    </div>
</asp:Content>
