<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="securityLogView.aspx.cs"
    Inherits="Web.pages.securityLogView" MasterPageFile="~/pages/popupwindow.master" %>


<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <link type="text/css" href="../style/taskSteps.css" rel="stylesheet" />
    <script type="text/javascript">
        $(document).ready(function () {
            $("#item_search_btn").live("click", function () {
                //until we redo this all as ajax, our pretty button clicks the hidden ugly one
                $("#ctl00_phDetail_btnSearch").click();
            });

            // pager functions
            $(".pager_button").live("click", function () {
                // since we are changing pages, 
                //put the page # in the hidden field
                $("#ctl00_phDetail_hidPage").val($(this).attr("page"));
                //and post the update panel
                $("#ctl00_phDetail_btnGetPage").click();
            });

            // sorting the list
            $(".col_header").live("click", function () {
                $("#ctl00_phDetail_hidSortColumn").val($(this).attr("sortcolumn"));
                $("#ctl00_phDetail_btnGetPage").click();
            });

        });
        function pageLoad() {
            $("#item_search_btn").button({ icons: { primary: "ui-icon-search"} });
            initJtable(true, false);
            $.unblockUI();
        }
            
    </script>
    <style type="text/css">
        .ui-datepicker
        {
            z-index: 1000;
        }
    </style>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div style="display: none;">
        <asp:HiddenField ID="hidSortColumn" runat="server" />
        <asp:HiddenField ID="hidPage" runat="server" />
    </div>
    <div id="content2">
        <asp:UpdatePanel ID="uplTaskActivity" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <div class="page_title">
                    Security Log</div>
                Filter:
                <asp:TextBox ID="txtSearch" class="search_text" runat="server" />
                <!-- for some reason this pops up under the list?? -->
                <span id="lblStartDate" style="z-index: 1200;">Begin Date
                    <asp:TextBox ID="txtStartDate" Width="70px" class="datepicker" runat="server" /></span>
                <span id="lblStopDate">End Date
                    <asp:TextBox ID="txtStopDate" Width="70px" class="datepicker" runat="server" /></span>
                <span id="lblMaxRows"># of Results
                    <asp:TextBox ID="txtResultCount" Width="70px" runat="server" /></span><span id="item_search_btn">Search</span>
                <asp:ImageButton ID="btnSearch" class="hidden" OnClick="btnSearch_Click" runat="server" />
                <table class="jtable" cellspacing="1" cellpadding="1" width="100%">
                    <asp:Repeater ID="rpTaskInstances" runat="server">
                        <HeaderTemplate>
                            <tr>
                                <th sortcolumn="log_msg" width="30px">
                                    Log
                                </th>
                                <th sortcolumn="full_name" width="100px">
                                    User
                                </th>
                                <th sortcolumn="log_dt" width="50px">
                                    Date
                                </th>
                            </tr>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td>
                                    <%# (((System.Data.DataRowView)Container.DataItem)["log_msg"])%>
                                </td>
                                <td>
                                    <%# (((System.Data.DataRowView)Container.DataItem)["full_name"])%>
                                </td>
                                <td>
                                    <%# (((System.Data.DataRowView)Container.DataItem)["log_dt"])%>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </table>
                <asp:PlaceHolder ID="phPager" runat="server"></asp:PlaceHolder>
                <div class="hidden">
                    <asp:Button ID="btnGetPage" runat="server" OnClick="btnGetPage_Click" /></div>
            </ContentTemplate>
        </asp:UpdatePanel>
    </div>
</asp:Content>
