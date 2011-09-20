<%@ Page Language="C#" AutoEventWireup="True" MasterPageFile="~/pages/site.master"
    CodeBehind="ecosystemEdit.aspx.cs" Inherits="Web.pages.ecosystemEdit" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <script type="text/javascript" src="../script/ecosystemEdit.js"></script>
    <script type="text/javascript" src="../script/toolbox.js"></script>
    <script type="text/javascript" src="../script/taskedit/taskEditParams.js"></script>
    <script type="text/javascript" src="../script/parametersOnDialog.js"></script>
    <script type="text/javascript" src="../script/taskLaunchDialog.js"></script>
    <script type="text/javascript" src="../script/registry.js"></script>
    <link type="text/css" href="../style/taskEdit.css" rel="stylesheet" />
    <link type="text/css" href="../style/taskView.css" rel="stylesheet" />
    <link type="text/css" href="../style/ecosystemEdit.css" rel="stylesheet" />
    <link type="text/css" href="../style/registry.css" rel="stylesheet" />
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="left_panel_te">
        <div id="toolbox">
            <div id="toolbox_tabs" class="toolbox_tabs_1row">
                <span id="tab_details" linkto="div_details" class="ui-state-default ui-corner-top toolbox_tab">
                    Details</span><span id="tab_objects" linkto="div_objects" class="ui-state-default ui-corner-top toolbox_tab">Objects</span><span
                        id="tab_actions" linkto="div_actions" class="ui-state-default ui-corner-top ui-tabs-selected ui-state-active toolbox_tab">Actions</span>
            </div>
            <div id="div_details" class="toolbox_panel hidden">
                <div style="padding: 0 0 0 5px;">
                    <span class="detail_label">Template:</span><br />
                    <asp:Label ID="lblEcotemplateName" runat="server" CssClass="code"></asp:Label><span
                        id="view_ecotemplate_btn" class="pointer">
                        <img alt="" src="../images/icons/edit_16.png" />
                    </span>
                    <br />
                    <span class="detail_label">Name:</span><br />
                    <asp:TextBox ID="txtEcosystemName" TextMode="MultiLine" Rows="5" runat="server" CssClass="task_details code"
                        te_group="detail_fields" column="ecosystem_name">
                    </asp:TextBox>
                    <br />
                    <span class="detail_label">Description:</span><br />
                    <asp:TextBox ID="txtDescription" TextMode="MultiLine" Rows="10" runat="server" te_group="detail_fields"
                        column="ecosystem_desc" CssClass="task_details code">
                    </asp:TextBox>
                    <br />
                    <br />
                    <br />
                    <span id="show_log_link" class="pointer">&nbsp;<br />
                        <img alt="" src="../images/icons/view_text_16.png" />
                        View Change Log</span>&nbsp;
                </div>
            </div>
            <div id="div_objects" class="toolbox_panel hidden">
                <!-- I imagine the "actions" for a specific object would go here.-->
            </div>
            <div id="div_actions" class="toolbox_panel">
                <span class="detail_label">Select a Category:</span>
                <div id="action_categories">
                </div>
            </div>
        </div>
    </div>
    <div id="content_te">
        <center>
            <h3>
                <asp:Label ID="lblEcosystemNameHeader" runat="server"></asp:Label>
            </h3>
        </center>
        <div id="div_objects_detail" class="detail_panel hidden">
            <div class="ui-state-default te_header">
                <div class="step_section_title">
                    <span class="step_title" id="breadcrumbs"><span class="breadcrumb" id="top"><span
                        class="ui-icon ui-icon-refresh floatleft"></span>Ecosystem Objects</span></span>
                </div>
                <div class="step_section_icons">
                </div>
            </div>
            <div id="random_content">
                <asp:Literal ID="ltItems" runat="server"></asp:Literal>
            </div>
        </div>
        <div id="div_actions_detail" class="detail_panel">
            <table id="action_flow">
            </table>
            <div class="category_actions hidden" id="category_actions">
            </div>
        </div>
        <div id="div_details_detail" class="detail_panel hidden">
            <!-- temporarily hiddend until enabled in the CE
           also see line 120is of ecosystemEdit.js

           <div id="div_parameters" class="ui-widget-content ui-corner-bottom">
                <div class="ui-state-default h18px">
                    <div class="floatleft">
                        <span class="detail_label">Parameters</span>
                    </div>
                    <div class="floatright">
                        <span id="parameter_add_btn">Add New</span>
                    </div>
                </div>
                <div id="parameters">
                </div>
            </div>
            <hr />
            -->
            <div id="registry_content">
            </div>
            <hr />
            <div id="div_schedules" class="ui-widget-content ui-corner-bottom">
                <div class="ui-state-default h18px">
                    <span class="">Scheduling</span>
                </div>
                <div id="ecosystem_schedules">
                    There are no Actions scheduled to run on this Ecosystem.
                </div>
            </div>
            <hr />
            <div id="div_plans" class="ui-widget-content ui-corner-bottom">
                <div class="ui-state-default h18px">
                    <span class="">Upcoming Actions</span>
                </div>
                <div id="ecosystem_plans">
                    There are no pending Actions on this Ecosystem.
                </div>
            </div>
        </div>
    </div>
    <div class="te_container_footer">
        <!--The footer has to be here so some element can contain the 'clear:both;' style.-->
    </div>
    <div class="hidden">
        <asp:HiddenField ID="hidEcosystemID" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidEcoTemplateID" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidParamDelete" runat="server"></asp:HiddenField>
        <input type="hidden" id="hidParamType" value="ecosystem" />
        <input type="hidden" id="selected_object_type" value="" />
    </div>
    <div id="task_launch_dialog" title="Perform Action">
        <div id="plan_edit_dialog" title="Edit Plan">
        </div>
    </div>
    <div id="param_edit_dialog" title="Parameter">
    </div>
    <div id="param_delete_confirm_dialog" title="Remove Parameter">
    </div>
    <!-- Registry dialogs. -->
    <div id="reg_edit_dialog" class="hidden">
    </div>
    <div id="reg_add_dialog" class="hidden">
    </div>
    <!-- End Registry dialogs.-->
    <input type="hidden" id="reg_type" value="ecosystem" />
</asp:Content>
