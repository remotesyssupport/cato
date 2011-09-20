<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/pages/site.master" CodeBehind="taskStatusView.aspx.cs" Inherits="Web.pages.taskStatusView" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <link href="../style/statusView.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript" src="../script/taskStatusView.js"></script>
    <div style="display: none;">
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/task-status-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                    <p>
                        <img src="../images/tooltip.png" alt="" />The task status display shows up to the minute information on Tasks.</p>
                    <p>
                        Rows which highlight on mouse-over will display more detailed data when clicked. </p>
                    <p>
                        To refresh the data select the menu choice again. </p>
                </div>
            </div>
        </div>
    </div>
    <div id="content" class="display">
        <br />
        
        <table border="0" cellpadding="0" cellspacing="0">
            <tr>
                <td>
              <div id="column-0a" class="pane-column">
            <div id="pane-0" class="pane">
                <span class="panetitle">Automated Task Status - Active</span>
                <div id="pane-0-content" class="content" style="height: 150px;">
                    <div class="content-container">
                       <table id="tblAtsa" class="table-data">
                            <thead>
                                <tr>
                                    <th width="300px" class="first-child">Status</th>
                                    <th align="center" width="100px" class="last-child">Total</th>
                                </tr>
                            </thead>
                            <tbody> 
                                <tr tag="selectAutoTask" status="processing">
                                    <td width="300px">Processing</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskProcessing" runat="server" /></td>
                                </tr>
                                <tr tag="selectAutoTask" status="staged">
                                    <td width="300px">Staged</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskStaged" runat="server" /></td>
                                </tr>
                                <tr tag="selectAutoTask" status="submitted">
                                    <td width="300px">Submitted</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskSubmitted" runat="server" /></td>
                                </tr>
                                <tr tag="selectAutoTask" status="pending">
                                    <td width="300px">Pending</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskPending" runat="server" /></td>
                                </tr>
                                <tr tag="selectAutoTask" status="aborting">
                                    <td width="300px">Aborting</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskAborting" runat="server" /></td>
                                </tr>
                                <tr tag="selectAutoTask" status="queued">
                                    <td width="300px">Queued</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskQueued" runat="server" /></td>
                                </tr>
                                <tr tag="selectAutoTask" status="processing,submitted,aborting,pending,staged,queued">
                                    <td width="300px">Total Active</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskActive" runat="server" /></td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
                <div class="footer">
                    <div>&nbsp;</div>
                </div>
            </div>
        </div>
        
        
                </td>
                <td>
                 <div id="column-0b" class="pane-column">
            <div id="pane-1" class="pane">
                <span class="panetitle">Automated Task Status - Completed</span>
                <div id="Div3" class="content"  style="height: 150px;">
                    <div class="content-container">
                       <table id="tblAtsc" class="table-data">
                            <thead>
                                <tr>
                                    <th width="300px" class="first-child">Status</th>
                                    <th align="center" width="100px" class="last-child">Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr tag="selectAutoTask" status="completed">
                                    <td width="300px">Completed</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskCompleted" runat="server" /></td>
                                </tr>
                                <tr tag="selectAutoTask"  status="cancelled">
                                    <td width="300px">Cancelled</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskCancelled" runat="server" /></td>
                                </tr>
                                <tr tag="selectAutoTask"  status="error">
                                    <td width="300px">Errored</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskErrored" runat="server" /></td>
                                </tr>
                                <tr tag="selectAutoTask" status="completed,cancelled,error">
                                    <td width="300px">Total Completed</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedTaskTotalCompleted" runat="server" /></td>
                                </tr>
                                <tr tag="selectAutoTask" status="">
                                    <td width="300px">All Statuses</td>
                                    <td align="center" width="100px"><asp:Label ID="lblAutomatedAllStatuses" runat="server" /></td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
                <div class="footer">
                    <div>&nbsp;</div>
                </div>
            </div>
        </div>
                 </td>
            </tr>
            <tr><td colspan="2">&nbsp;</td></tr>
            <!--
            <tr>
                <td>
                     <div id="column-1a" class="pane-column">
            <div id="pane-2" class="pane">
                <span class="panetitle">Manual Task Status - Active</span>
                <div id="Div4" class="content" style="height: 80px;">
                    <div class="content-container">
                       <table id="tblManualStatus" class="table-data">
                            <thead>
                                <tr>
                                    <th width="300px" class="first-child">Status</th>
                                    <th align="center" width="100px" class="last-child">Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr tag="selectManualTask" status="new">
                                    <td width="300px">New</td>
                                    <td align="center" width="100px"><asp:Label ID="lblManualTaskNew" runat="server" /></td>
                                </tr>
                                <tr tag="selectManualTask"  status="active">
                                    <td width="300px">Active</td>
                                    <td align="center" width="100px"><asp:Label ID="lblManualTaskActive" runat="server" /></td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
                <div class="footer">
                    <div>&nbsp;</div>
                </div>
            </div>
        </div>
                </td>
                <td>
                      <div id="column-1b" class="pane-column">
            <div id="pane-3" class="pane">
                <span class="panetitle">Manual Task Status - Completed</span>
                <div id="Div7" class="content"  style="height: 80px;">
                    <div class="content-container">
                        <table id="tblManualComplete" class="table-data">
                           <thead>
                                <tr>
                                    <th width="300px" class="first-child">Status</th>
                                    <th align="center" width="100px" class="last-child">Total</th>
                                </tr>
                            </thead>
                            <tr tag="selectManualTask"  status="completed">
                                <td width="300px">Completed</td>
                                <td align="center" width="100px"><asp:Label ID="lblManualTaskCompleted" runat="server" /></td>
                            </tr>
                            <tr tag="selectManualTask"  status="declined">
                                <td width="300px">Declined</td>
                                <td align="center" width="100px"><asp:Label ID="lblManualTaskDeclined" runat="server" /></td>
                            </tr>
                        </table>
                    </div>
                </div>
                <div class="footer">
                    <div>&nbsp;</div>
                </div>
            </div>
        </div>
                </td>
            </tr>
            <tr><td colspan="2">&nbsp;</td></tr>
            <tr>
                <td>
                <div id="column-2a" class="pane-column">
                    <div id="pane-4" class="pane">
                        <span class="panetitle">Unassigned Assets</span>
                        <div id="pane4content" class="content"  style="height: 60px;">
                            <div class="content-container">
                               <table id="tblUnassignedAssets" class="table-data">
                                    <thead>
                                        <tr>
                                            <th width="300px" class="first-child">Status</th>
                                            <th align="center" width="100px" class="last-child">Total</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr tag="selectUnassignedAssets" status="completed">
                                            <td width="300px">Unassigned Assets with Manual Tasks</td>
                                            <td align="center" width="100px"><asp:Label ID="lblUnassignedAssets" runat="server" /></td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                        <div class="footer">
                            <div>&nbsp;</div>
                        </div>
                    </div>
                </div>
                
                </td>
                <td>&nbsp;</td>
                
                </tr>
            -->
        </table>
    
</div>
    
    
	
</asp:Content>


