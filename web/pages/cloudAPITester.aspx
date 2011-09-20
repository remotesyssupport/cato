<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="cloudAPITester.aspx.cs"
    Inherits="Web.pages.cloudAPITester" MasterPageFile="~/pages/site.master" %>


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
        #results_list div
        {
            border: 1px solid #cccccc;
        }
    </style>
    <script type="text/javascript">
        $(document).ready(function () {
            $(".group_tab").click(function () {
                showPleaseWait("Querying AWS...");

                //style tabs
                $(".group_tab").removeClass("group_tab_selected");
                $(this).addClass("group_tab_selected");


                //get content here at some possible point in the future.
                $("#update_success_msg").text("Querying AWS...").show();

                var object_label = $(this).html();
                var object_type = $(this).attr("object_type");

                //well, I have no idea why, but this ajax fires before the showPleaseWait can take effect.
                //delaying it is the only solution I've found... :-(
                setTimeout(function () {
                    $.ajax({
                        async: false,
                        type: "POST",
                        url: "cloudAPITester.aspx/wmGetCloudObjectList",
                        data: '{"sObjectType":"' + object_type + '"}',
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        success: function (msg) {
                            $("#update_success_msg").fadeOut(2000);

                            $("#results_label").html(object_label);
                            $("#results_list").html(msg.d);

                            hidePleaseWait();
                        },
                        error: function (response) {
                            $("#update_success_msg").fadeOut(2000);
                            showAlert(response.responseText);
                            hidePleaseWait();
                        }
                    });
                }, 250);
            });

        });</script>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="content">
        <div class="page_title">
            Cloud Object API Tester</div>
        <hr />
        <h2 id="results_label">
        </h2>
        <div id="results_list">
        </div>
    </div>
    <div id="left_panel">
        <div id="group_tabs">
            <ul>
                <asp:Literal ID="ltTabs" runat="server"></asp:Literal>
            </ul>
        </div>
    </div>
</asp:Content>
