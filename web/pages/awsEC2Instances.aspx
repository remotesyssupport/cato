<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="awsEC2Instances.aspx.cs"
    Inherits="Web.pages.awsEC2Instances" MasterPageFile="~/masterpages/site.master" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <link type="text/css" rel="stylesheet" href="../style/taskView.css">
    <style type="text/css">
        #content
        {
            margin-top: 20px;
        }
    </style>

    <script type="text/javascript">
        $(document).ready(function() {
            //jquery placeholder
        });

        function CloudAccountWasChanged() {
            location.reload();
        }
    </script>

</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="content">
        <asp:Repeater ID="rptReservations" runat="server">
            <HeaderTemplate>
            </HeaderTemplate>
            <ItemTemplate>
                <div class="step">
                    <div id="d<%=Guid.NewGuid()%>" class="view_step">
                        <div class="view_step_header">
                            <img src="../images/status_<%# XPath("RunningInstance/InstanceState/Name")%>.png"
                                alt="" /><%# XPath("RunningInstance/Tag[Key='Name']/Value")%>
                        </div>
                        <div class="view_step_detail">
                            <table width="99%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td width="33%" style="border-style: none;">
                                        <div class="view_step_detail_block">
                                            <div>
                                                Status: <span class="code">
                                                    <%# XPath("RunningInstance/InstanceState/Name")%></span>
                                            </div>
                                            <div>
                                                Launch Time: <span class="code">
                                                    <%# XPath("RunningInstance/LaunchTime")%></span>
                                            </div>
                                            <div>
                                                Dept: <span class="code">
                                                    <%# XPath("RunningInstance/Tag[Key='Dept']/Value")%></span>
                                            </div>
                                            <div>
                                                Group: <span class="code">
                                                    <%# XPath("RunningInstance/Tag[Key='Group']/Value")%></span>
                                            </div>
                                            <div>
                                                Name: <span class="code">
                                                    <%# XPath("RunningInstance/Tag[Key='Name']/Value")%></span>
                                            </div>
                                        </div>
                                    </td>
                                    <td width="33%" style="border-style: none;">
                                        <div class="view_step_detail_block">
                                            <div>
                                                Instance: <span class="code">
                                                    <%# XPath("RunningInstance/InstanceId")%></span>
                                            </div>
                                            <div>
                                                AMI Image: <span class="code">
                                                    <%# XPath("RunningInstance/ImageId")%></span>
                                            </div>
                                            <div>
                                                Type: <span class="code">
                                                    <%# XPath("RunningInstance/InstanceType")%></span>
                                            </div>
                                            <div>
                                                Security: <span class="code">
                                                    <%# XPath("GroupName")%></span>
                                            </div>
                                        </div>
                                    </td>
                                    <td width="33%" style="border-style: none;">
                                        <div class="view_step_detail_block">
                                            <div>
                                                Root Device: <span class="code">
                                                    <%# XPath("RunningInstance/RootDeviceType")%></span>
                                            </div>
                                            <div>
                                                KeyPair: <span class="code">
                                                    <%# XPath("RunningInstance/KeyName")%></span>
                                            </div>
                                            <div>
                                                AMI Launch: <span class="code">
                                                    <%# XPath("RunningInstance/AmiLaunchIndex")%></span>
                                            </div>
                                            <div>
                                                Monitoring: <span class="code">
                                                    <%# XPath("monitoring")%></span>
                                            </div>
                                            <div>
                                                Virtualization: <span class="code">
                                                    <%# XPath("RunningInstance/VirtualizationType")%></span>
                                            </div>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </div>
                    </div>
                </div>
            </ItemTemplate>
            <FooterTemplate>
            </FooterTemplate>
        </asp:Repeater>
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <span class="page_title">EC2 Instances</span>
            <img src="../images/aws.png" height="192" width="192" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <img src="../images/tooltip.png" alt="" />
                    <p>
                        AWS EC2 Instances and Status</p>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        $("#left_tooltip_box_inner").corner("round 8px").parent().css('padding', '2px').corner("round 10px"); 
    </script>

</asp:Content>
