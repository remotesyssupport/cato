<%@ Page Language="C#" AutoEventWireup="True" MasterPageFile="~/pages/site.master"
    CodeBehind="taskEdit.aspx.cs" Inherits="Web.pages.taskEdit" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <link type="text/css" href="../style/registry.css" rel="stylesheet" />
    <link type="text/css" href="../style/taskEdit.css" rel="stylesheet" />
    <link type="text/css" href="../style/taskView.css" rel="stylesheet" />
    <script type="text/javascript" src="../script/taskedit/taskEdit.js"></script>
    <script type="text/javascript" src="../script/taskedit/taskView.js"></script>
    <script type="text/javascript" src="../script/taskedit/taskVersions.js"></script>
    <script type="text/javascript" src="../script/toolbox.js"></script>
    <script type="text/javascript" src="../script/taskedit/taskEditDebug.js"></script>
    <script type="text/javascript" src="../script/taskedit/taskEditCodeblock.js"></script>
    <script type="text/javascript" src="../script/taskedit/taskEditParams.js"></script>
    <script type="text/javascript" src="../script/taskedit/taskEditSteps.js"></script>
    <script type="text/javascript" src="../script/taskedit/taskEditCommandSpecific.js"></script>
    <script type="text/javascript" src="../script/validation.js"></script>
    <script type="text/javascript" src="../script/jquery/jquery.rightClick.js"></script>
    <script type="text/javascript" src="../script/objectTag.js"></script>
    <script type="text/javascript" src="../script/registry.js"></script>
    <script type="text/javascript" src="../script/parametersOnDialog.js"></script>
    <script type="text/javascript" src="../script/taskLaunchDialog.js"></script>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="left_panel_te">
        <div id="toolbox">
            <div id="toolbox_tabs" class="toolbox_tabs_2row">
                <span id="tab_schedules" linkto="div_schedules" class="ui-state-default ui-corner-top toolbox_tab"
                    runat="server">Schedules</span><span id="tab_parameters" linkto="div_parameters"
                        class="ui-state-default ui-corner-top toolbox_tab">Parameters</span><span id="tab_clipboard"
                            linkto="div_clipboard" class="ui-state-default ui-corner-top toolbox_tab" runat="server">Clipboard</span><span
                                id="tab_debug" linkto="div_debug" class="ui-state-default ui-corner-top toolbox_tab">Run</span><br />
                <span id="tab_details" linkto="div_details" class="ui-state-default ui-corner-top ui-state-active toolbox_tab">
                    Details</span><span id="tab_versions" linkto="div_versions" class="ui-state-default ui-corner-top toolbox_tab">Versions</span><span
                        id="tab_codeblocks" linkto="div_codeblocks" class="ui-state-default ui-corner-top toolbox_tab">Codeblocks</span><span
                            id="tab_commands" linkto="div_commands" class="ui-state-default ui-corner-top toolbox_tab">Commands</span>
                <!--<<span
                            id="tab_registry" linkto="div_registry" class="ui-state-default ui-corner-top toolbox_tab"
                            style="padding-left: 7px; padding-right: 7px;" runat="server">Registry</span>span id="tab_attributes" linkto="div_attributes" class="ui-state-default ui-corner-top toolbox_tab"
                    style="padding-left: 7px; padding-right: 7px;">Attributes</span><span
                                id="tab_tags" linkto="div_tags" class="ui-state-default ui-corner-top toolbox_tab">Tags</span>-->
            </div>
            <div id="div_details" class="toolbox_panel">
                <div style="padding: 0 0 0 5px;">
                    <span class="detail_label">Code:</span>
                    <asp:TextBox ID="txtTaskCode" runat="server" CssClass="task_details code" validate_as=""
                        te_group="detail_fields" column="task_code"></asp:TextBox>
                    <br />
                    <span class="detail_label">Name:</span>
                    <asp:TextBox ID="txtTaskName" runat="server" CssClass="task_details code" te_group="detail_fields"
                        column="task_name"></asp:TextBox>
                    <br />
                    <span class="detail_label">Description:</span>
                    <br />
                    <asp:TextBox ID="txtDescription" TextMode="MultiLine" Rows="5" runat="server" CssClass="code"
                        te_group="detail_fields" column="task_desc"></asp:TextBox>
                    <br />
                    <!-- *** Commented out per Bug 1029 ***
                        <input type="checkbox" class="chkbox" id="chkDirect" runat="server" te_group="detail_fields"
                            column="use_connector_system" />
                        Direct to Asset?<br />-->
                    <span class="detail_label">Concurrent Instances:</span>
                    <asp:TextBox ID="txtConcurrentInstances" runat="server" CssClass="task_details code"
                        te_group="detail_fields" column="concurrent_instances" validate_as="posint"></asp:TextBox>
                    <br />
                    <span class="detail_label">Number to Queue:</span>
                    <asp:TextBox ID="txtQueueDepth" runat="server" CssClass="task_details code" te_group="detail_fields"
                        column="queue_depth" validate_as="posint"></asp:TextBox>
                    <br />
                    <br />
                    <hr />
                    <span id="show_log_link" class="pointer">
                        <img alt="" src="../images/icons/view_text_16.png" style="margin: 0px 4px 0px 4px;" />
                        View Change Log</span>
                    <br />
                    <span id="show_runlog_link" class="pointer">
                        <img alt="" src="../images/icons/view_text_16.png" style="margin: 0px 4px 0px 4px;" />
                        View Most Recent Run Log</span>
                    <br />
                </div>
            </div>
            <div id="div_versions" class="toolbox_panel hidden">
                <span class="detail_label">Selected Version:</span>
                <asp:Label ID="lblVersion" runat="server" CssClass="code"></asp:Label><br />
                <span class="detail_label">Status: </span>
                <asp:Label ID="lblStatus2" runat="server" CssClass="code"></asp:Label><br />
                <input type="button" id="approve_btn" value="Approve" />
                <hr />
                <span class="detail_label">All Versions:</span>
                <ul id="versions">
                </ul>
                <input type="button" value="New Version" onclick="ShowVersionAdd();return false;" />
            </div>
            <div id="div_codeblocks" class="toolbox_panel hidden">
                <div>
                    <asp:UpdatePanel ID="udpCodeblocks" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <div class="hidden">
                                <asp:Button ID="btnCBAdd" runat="server" Text="Add" UseSubmitBehavior="false" OnClick="btnCBAdd_Click" /></div>
                            <span id="codeblock_add_btn">Add New</span>
                            <hr />
                            <ul>
                                <asp:Repeater ID="rpCodeblocks" runat="server">
                                    <ItemTemplate>
                                        <li class="ui-widget-content ui-corner-all codeblock" id="cb_<%#(((System.Data.DataRowView)Container.DataItem)["codeblock_name"])%>">
                                            <div>
                                                <div class="codeblock_title" name="<%#(((System.Data.DataRowView)Container.DataItem)["codeblock_name"])%>">
                                                    <span>
                                                        <%#(((System.Data.DataRowView)Container.DataItem)["codeblock_name"])%></span>
                                                </div>
                                                <div class="codeblock_icons pointer">
                                                    <span id="codeblock_rename_btn_<%#(((System.Data.DataRowView)Container.DataItem)["codeblock_name"])%>">
                                                        <img class="codeblock_rename" codeblock_name="<%#(((System.Data.DataRowView)Container.DataItem)["codeblock_name"])%>"
                                                            src="../images/icons/edit_16.png" alt="" /></span><span class="codeblock_copy_btn"
                                                                codeblock_name="<%#(((System.Data.DataRowView)Container.DataItem)["codeblock_name"])%>">
                                                                <img src="../images/icons/editcopy_16.png" alt="" /></span><span id="codeblock_delete_btn_<%#(((System.Data.DataRowView)Container.DataItem)["codeblock_name"])%>"
                                                                    class="codeblock_delete_btn codeblock_icon_delete" remove_id="<%#(((System.Data.DataRowView)Container.DataItem)["codeblock_name"])%>">
                                                                    <img src="../images/icons/fileclose.png" alt="" /></span>
                                                </div>
                                            </div>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                            <div class="hidden">
                                <asp:Button ID="btnCBDelete" runat="server" OnClick="btnCBDelete_Click" />
                                <asp:Button ID="btnCBRefresh" runat="server" OnClick="btnCBRefresh_Click" />
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
            </div>
            <div id="div_commands" class="toolbox_panel hidden">
                <span id="command_help_btn">Help</span>
                <hr />
                <table border="0" width="100%">
                    <tr>
                        <td width="30%">
                            <ul id="categories">
                                <asp:Repeater ID="rpCategories" runat="server">
                                    <HeaderTemplate>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <li class="ui-widget-content ui-corner-all command_item category" id="cat_<%#(((System.Data.DataRowView)Container.DataItem)["category_name"])%>"
                                            name="<%#(((System.Data.DataRowView)Container.DataItem)["category_name"])%>">
                                            <div>
                                                <img class="category_icon" src="../images/<%#(((System.Data.DataRowView)Container.DataItem)["icon"])%>"
                                                    alt="" />
                                                <span>
                                                    <%#(((System.Data.DataRowView)Container.DataItem)["category_label"])%></span>
                                            </div>
                                            <div id="help_text_<%#(((System.Data.DataRowView)Container.DataItem)["category_name"])%>"
                                                class="hidden">
                                                <%#(((System.Data.DataRowView)Container.DataItem)["description"])%></div>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </td>
                        <td width="70%">
                            <div class="category_functions" id="category_functions">
                                <asp:Repeater ID="rpCategoryWidgets" runat="server" OnItemDataBound="rpCategoryWidgets_ItemDataBound">
                                    <ItemTemplate>
                                        <div class="functions hidden" id="cat_<%#(((System.Data.DataRowView)Container.DataItem)["category_name"])%>_functions">
                                            <asp:Repeater ID="rpCategoryFunctions" runat="server">
                                                <ItemTemplate>
                                                    <div class="ui-widget-content ui-corner-all command_item function" id="fn_<%#(((System.Data.DataRowView)Container.DataItem)["function_name"])%>"
                                                        name="<%#(((System.Data.DataRowView)Container.DataItem)["function_name"])%>">
                                                        <img class="function_icon" src="../images/<%#(((System.Data.DataRowView)Container.DataItem)["icon"])%>"
                                                            alt="" />
                                                        <span>
                                                            <%#(((System.Data.DataRowView)Container.DataItem)["function_label"])%></span>
                                                        <div id="help_text_<%#(((System.Data.DataRowView)Container.DataItem)["function_name"])%>"
                                                            class="hidden">
                                                            <%#(((System.Data.DataRowView)Container.DataItem)["description"])%></div>
                                                    </div>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </td>
                    </tr>
                </table>
            </div>
            <div id="div_debug" class="toolbox_panel hidden">
                <!--<p>
                    Test Asset:<br />
                    <asp:TextBox ID="txtTestAsset" runat="server" Enabled="false" asset_id="" CssClass="code"
                        Width="80%"></asp:TextBox>
                    <img class="asset_picker_btn pointer" src="../images/icons/search.png" link_to="ctl00_phDetail_txtTestAsset"
                        alt="" />
                    <img id="debug_asset_clear_btn" class="pointer" src="../images/icons/fileclose.png"
                        alt="" />
                </p>-->
                <p>
                    Last Run Time:
                    <asp:Label ID="lblLastRunDT" runat="server"></asp:Label><br />
                    Last Run Status:
                    <asp:Label ID="lblLastRunStatus" runat="server"></asp:Label>
                    <asp:Image ID="debug_view_latest_log" CssClass="debug_btn" ImageUrl="../images/icons/view_text_16.png"
                        runat="server" />
                </p>
                <br />
                <hr />
                <br />
                <p style="text-align: center;">
                    Current Status:
                    <asp:Label ID="lblCurrentStatus" runat="server"></asp:Label>
                    <br />
                    <img id="debug_stop_btn" class="debug_btn" src="../images/icons/player_stop_32.png"
                        alt="" />
                    <img id="debug_run_btn" class="debug_btn" src="../images/icons/player_play_32.png"
                        alt="" />
                    <img id="debug_view_active_log" class="debug_btn" src="../images/icons/view_text_32.png"
                        alt="" />
                </p>
            </div>
            <div id="div_parameters" class="toolbox_panel hidden">
                <span id="parameter_add_btn">Add New</span>
                <hr />
                <div id="parameters">
                </div>
            </div>
            <div id="div_schedules" class="toolbox_panel hidden">
                <asp:UpdatePanel ID="udpSchedule" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <ul>
                            <asp:Repeater ID="rpSchedules" runat="server">
                                <ItemTemplate>
                                    <li class="schedule" id="schedule_<%#(((System.Data.DataRowView)Container.DataItem)["schedule_id"])%>"
                                        schedule_id="<%#(((System.Data.DataRowView)Container.DataItem)["schedule_id"])%>">
                                        <div style="text-align: left;">
                                            <h5>
                                                <img alt="Schedule" src="../images/icons/1day.png" border="0" />
                                                <%#(((System.Data.DataRowView)Container.DataItem)["schedule_name"])%>
                                                -
                                                <%#(((System.Data.DataRowView)Container.DataItem)["status"])%></h5>
                                        </div>
                                    </li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
            <div id="div_clipboard" class="toolbox_panel hidden">
                <span id="clear_clipboard_btn">Clear All</span>
                <hr />
                <ul id="clipboard">
                    <asp:PlaceHolder ID="phClipboard" runat="server"></asp:PlaceHolder>
                </ul>
            </div>
            <div id="div_tags" class="toolbox_panel hidden">
                <span id="tag_add_btn" class="tag_add_btn pointer">
                    <img alt="" src="../images/icons/edit_add.png" />
                    click to add </span>
                <ul id="objects_tags">
                </ul>
            </div>
            <div id="div_registry" class="toolbox_panel hidden">
                <div id="registry_content">
                </div>
            </div>
        </div>
    </div>
    <div id="content_te">
        <center>
            <h3>
                <asp:Label ID="lblTaskNameHeader" runat="server"></asp:Label>
                <span id="version_tag">Version:
                    <asp:Label ID="lblVersionHeader" runat="server"></asp:Label></span></h3>
        </center>
        <div class="te_help_box">
            <img src="../images/icons/about_24.png" height="16px" width="16px" alt="" />
            <div id="te_help_box_detail" class="te_help_box_detail">
            </div>
        </div>
        <div id="all">
            <asp:UpdatePanel ID="udpSteps" runat="server" UpdateMode="Conditional">
                <ContentTemplate>
                    <div class="ui-state-default te_header">
                        <div class="step_section_title">
                            <span id="step_toggle_all_btn" class="pointer">
                                <img class="step_toggle_all_btn" src="../images/icons/expand.png" alt="Expand/Collapse All Steps"
                                    title="Expand/Collapse All Steps" />
                            </span><span class="step_title">
                                <asp:Label ID="lblStepSectionTitle" runat="server"></asp:Label></span>
                        </div>
                        <div class="step_section_icons">
                            <span id="print_link" class="pointer">
                                <img class="task_print_link" alt="" src="../images/icons/printer.png" />
                            </span>
                        </div>
                    </div>
                    <div id="task_steps">
                        <ul id="steps" class="sortable">
                            <asp:PlaceHolder ID="phSteps" runat="server"></asp:PlaceHolder>
                        </ul>
                        <div class="hidden">
                            <asp:Button ID="btnStepLoad" runat="server" OnClick="btnStepLoad_Click" Text="Load" />
                        </div>
                    </div>
                </ContentTemplate>
            </asp:UpdatePanel>
        </div>
    </div>
    <div id="step_update_array" class="hidden">
    </div>
    <div class="hidden">
        <input type="hidden" id="hidParamType" value="task" />
        <input id="hidPageSaveType" type="hidden" value="dynamic" />
        <input id="hidObjectType" type="hidden" value="3" />
        <asp:HiddenField ID="hidTaskID" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidOriginalTaskID" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidOriginalStatus" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidDefault" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidCodeblockName" runat="server" Value="MAIN"></asp:HiddenField>
        <asp:HiddenField ID="hidStepArray" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidStepDelete" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidCodeblockDelete" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidParamDelete" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidAddAttributeID" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidAddToAttributeGroupID" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidDebugActiveInstance" runat="server"></asp:HiddenField>
        <asp:HiddenField ID="hidV3" runat="server"></asp:HiddenField>
    </div>
    <div id="step_delete_confirm_dialog" title="Delete Step" class="hidden ui-state-highlight">
        <p>
            <span class="ui-icon ui-icon-info" style="float: left; margin: 0 7px 50px 0;"></span>
            <span>Are you sure you want to delete this step?</span>
        </p>
    </div>
    <div id="embedded_step_delete_confirm_dialog" title="Remove Command" class="hidden ui-state-highlight">
        <p>
            <span class="ui-icon ui-icon-info" style="float: left; margin: 0 7px 50px 0;"></span>
            <span>Are you sure you want to remove this Command?</span>
            <input type="hidden" id="embedded_step_remove_id" value="" />
            <input type="hidden" id="embedded_step_parent_id" value="" />
        </p>
    </div>
    <div id="codeblock_delete_confirm_dialog" title="Delete Codeblock" class="hidden ui-state-highlight">
        <p>
            <span class="ui-icon ui-icon-info" style="float: left; margin: 0 7px 50px 0;"></span>
            <span>Are you sure you want to delete this Codeblock and all the Steps it contains?</span>
        </p>
    </div>
    <div id="dataset_template_change_confirm" title="Apply Template" class="hidden ui-state-highlight">
        <p>
            <span class="ui-icon ui-icon-info" style="float: left; margin: 0 7px 50px 0;"></span>
            <span>Are you sure you want to apply this template?<br />
                All existing Keys and Values for this step will be replaced.</span>
            <input type="hidden" id="dataset_template_id" value="" />
            <input type="hidden" id="dataset_step_id" value="" />
        </p>
    </div>
    <div id="asset_picker_dialog" title="Select an Asset" class="hidden ui-state-highlight">
        <center>
            <span style="padding-left: 15px; padding-top: 5px; font-weight: bold;">Enter search
                criteria:</span> <span style="padding-left: 15px; padding-top: 5px;">
                    <input id="asset_search_text" class="search_text" /></span><span id="asset_search_btn">Search</span>
            <hr />
            <div style="text-align: left; height: 300px; overflow: auto;" id="asset_picker_results">
            </div>
            <input type="hidden" id="asset_picker_target_field" value="" />
            <input type="hidden" id="asset_picker_target_name_field" value="" />
        </center>
    </div>
    <div id="task_picker_dialog" title="Select a Task" class="hidden ui-state-highlight">
        <span style="padding-left: 15px; padding-top: 5px; font-weight: bold;">Enter search
            criteria:</span>
        <input id="task_search_text" />
        <span id="task_search_btn">Search</span>
        <hr />
        <div id="task_picker_results">
        </div>
        <input type="hidden" id="task_picker_target_field_id" value="" />
        <input type="hidden" id="task_picker_step_id" value="" />
    </div>
    <div id="step_reorder_helper" class="step_reorder_helper">
        Drop me in the new position.
    </div>
    <div id="step_validation_template" class="step_validation_template hidden">
        <img src="../images/icons/status_unknown_32.png" alt="" />
        The following Command has <span></span>item(s) needing attention.
    </div>
    <div id="conn_picker" class="ui-widget-content ui-corner-all value_picker hidden">
        <span class="ui-state-default value_picker_title">
            <img src="../images/icons/connect_no.png" alt="" />
            Select a Connection &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <img id="conn_picker_close_btn" class="pointer" alt="" src="../images/icons/fileclose.png" /></span>
        <div id="conn_picker_connections">
            No connections have been defined on this Task.
        </div>
    </div>
    <div id="namespace_picker" class="ui-widget-content ui-corner-all value_picker hidden">
        <span class="ui-state-default value_picker_title">
            <img src="../images/icons/connect_no.png" alt="" />
            Select a Namespace &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <img id="namespace_picker_close_btn" class="pointer" alt="" src="../images/icons/fileclose.png" /></span>
        <div id="namespace_picker_namespaces">
            No namespaces have been defined.
        </div>
    </div>
    <div id="key_picker" class="ui-widget-content ui-corner-all value_picker hidden">
        <span class="ui-state-default value_picker_title">
            <img src="../images/icons/connect_no.png" alt="" />
            Select a Key &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <img id="key_picker_close_btn" class="pointer" alt="" src="../images/icons/fileclose.png" /></span>
        <div id="key_picker_keys">
            No keys have been defined for this Command type.
        </div>
    </div>
    <div id="dataset_template_picker" class="ui-widget-content ui-corner-all value_picker hidden">
        <span class="ui-state-default value_picker_title">
            <img src="../images/icons/connect_no.png" alt="" />
            Select a Template &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <img id="template_picker_close_btn" class="pointer" alt="" src="../images/icons/fileclose.png" /></span>
        <div id="template_picker_templates">
            No templates have been defined for this Command type.
        </div>
    </div>
    <div id="codeblock_picker" class="ui-widget-content ui-corner-all value_picker hidden">
        <span class="ui-state-default value_picker_title">
            <img src="../images/icons/news_subscribe.png" alt="" />
            Select a Codeblock &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <img id="codeblock_picker_close_btn" class="pointer" alt="" src="../images/icons/fileclose.png" /></span>
        <div id="codeblock_picker_codeblocks">
            No codeblocks have been defined on this Task.
        </div>
    </div>
    <div id="var_picker" class="ui-widget-content ui-corner-all value_picker hidden">
        <span class="ui-state-default value_picker_title">
            <img src="../images/icons/bookmark.png" alt="" />
            Select a Variable &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <img id="var_picker_close_btn" class="pointer" alt="" src="../images/icons/fileclose.png" /></span>
        <div id="var_picker_variables">
            No variables have been defined on this Task.
        </div>
    </div>
    <div id="addVersion_dialog" class="hidden" title="Create Another Version">
        Current Version Number:
        <asp:Label ID="lblCurrentVersion" runat="server" Text="" />
        <center>
            <input id="rbMinor" checked="checked" name="rbMinorMajor" value="Minor" type="radio" />
            <asp:Label ID="lblNewMinor" runat="server" Text="New Minor Version" /><br />
            <input id="rbMajor" name="rbMinorMajor" value="Major" type="radio" />
            <asp:Label ID="lblNewMajor" runat="server" Text="New Major Version" /><br />
            <input type="button" value="Create New Version" onclick="CreateNewVersion();return false;" />
            <input type="button" value="Cancel" onclick="CloseVersionDialog();return false;" />
        </center>
    </div>
    <div id="task_launch_dialog" title="Run Task">
    </div>
    <div id="param_edit_dialog" title="Parameter">
    </div>
    <div id="param_delete_confirm_dialog" title="Remove Parameter">
    </div>
    <div id="command_help_dialog" title="Command Help">
        <div id="command_help_dialog_detail">
        </div>
    </div>
    <div id="approve_dialog" title="Approve" class="hidden">
        <p>
            Changing this Version to Approved status will permanently lock it.</p>
        <br />
        <p style="font-style: italic;">
            (Once Approved, changes to this version will not be allowed.)</p>
        <br />
        <p>
            This action cannot be undone.</p>
        <br />
        <p>
            Are you sure you wish to Approve this Version?</p>
        <p style="text-align: center;">
            <asp:CheckBox ID="chkMakeDefault" runat="server" Text="Mark as the 'Default' Version?" />
        </p>
    </div>
    <div id="codeblock_edit_dialog" title="Codeblock" class="hidden">
        <p>
            Enter a name for the Codeblock:</p>
        <asp:TextBox ID="txtCodeblockNameAdd" runat="server" validate_as="identifier" Width="100%"></asp:TextBox>
        <div id="codeblock_edit_dialog_msg" style="color: Red;">
        </div>
    </div>
    <div id="big_box_dialog" title="Edit Value" class="hidden">
        <textarea id="big_box_text" rows="20" class="code w100pct"></textarea>
        <input type="hidden" id="big_box_link" />
    </div>
    <div id="clip_dialog" title="Clipboard Item" class="hidden">
        <div style="overflow: auto; height: 600px;">
            <ul id="clip_dialog_clip">
            </ul>
        </div>
    </div>
    <!-- Registry dialogs. -->
    <div id="reg_edit_dialog" class="hidden">
    </div>
    <div id="reg_add_dialog" class="hidden">
    </div>
    <!-- End Registry dialogs.-->
    <input type="hidden" id="reg_type" value="task" />
</asp:Content>
