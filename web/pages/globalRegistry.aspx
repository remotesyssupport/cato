<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="globalRegistry.aspx.cs"
    Inherits="Web.pages.globalRegistry" MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <link type="text/css" href="../style/registry.css" rel="stylesheet" />
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
    <script type="text/javascript" src="../script/registry.js"></script>
    <script type="text/javascript">
        function pageLoad() {
            setTimeout('GetRegistry("global")', 250)
        }

    </script>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="content">
        <div id="registry_content">
        </div>
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/terminal-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <p>
                        <img src="../images/icons/global_registry_32.png" alt="" />
                        The Global Registry is the central repository for any User-Defined values required
                        for the operation of Tasks.
                    </p>
                    <p>
                        Double click on a Key or Value to edit it.</p>
                    <p>
                        Use the
                        <img style="float: none; height: 10px; width: 10px;" alt="" src="../images/icons/edit_add.png" />
                        and
                        <img style="float: none; height: 10px; width: 10px;" alt="" src="../images/icons/fileclose.png" />
                        icons to add and remove items.</p>
                </div>
            </div>
        </div>
    </div>
    <script type="text/javascript">
        $("#left_tooltip_box_inner").corner("round 8px").parent().css('padding', '2px').corner("round 10px"); 
    </script>
    <!-- Registry dialogs. -->
    <div id="reg_edit_dialog" class="hidden">
    </div>
    <div id="reg_add_dialog" class="hidden">
    </div>
    <!-- End Registry dialogs.-->
    <input type="hidden" id="reg_type" value="global" />
</asp:Content>
