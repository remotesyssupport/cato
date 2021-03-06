<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="notAllowed.aspx.cs" Inherits="Web.pages.notAllowed"
    MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="content">
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/error_128.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <p>
                        <img src="../images/tooltip.png" alt="" />Not Allowed</p>
                    <p>
                        <br />
                        <br />
                        You requested a page or action that is not allowed.</p>
                    <p>
                        <asp:Label ID="lblMessage" runat="server">
                        </asp:Label></p>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        $(document).ready(function() {
            hidePleaseWait();
        });
    </script>

</asp:Content>
