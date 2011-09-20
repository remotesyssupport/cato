<%@ Page Language="C#" AutoEventWireup="True" MasterPageFile="~/pages/site.master"
    CodeBehind="ecoTemplateEdit.aspx.cs" Inherits="Web.pages.ecoTemplateEdit" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <link type="text/css" href="../style/taskEdit.css" rel="stylesheet" />
    <link type="text/css" href="../style/taskView.css" rel="stylesheet" />
    <link type="text/css" href="../style/ecoTemplateEdit.css" rel="stylesheet" />
    <script type="text/javascript" src="../script/toolbox.js"></script>
    <script type="text/javascript" src="../script/parametersOnDialog.js"></script>
    <script type="text/javascript" src="../script/ecoTemplateEdit.js"></script>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div id="left_panel_te">
        <div id="toolbox">
            <div id="toolbox_tabs" class="toolbox_tabs_1row">
                <span id="tab_details" linkto="div_details" class="ui-state-default ui-corner-top ui-state-active toolbox_tab">
                    Details</span><span id="tab_tasks" linkto="div_tasks" class="ui-state-default ui-corner-top toolbox_tab">Tasks</span><span
                        id="tab_ecosystems" linkto="div_ecosystems" class="ui-state-default ui-corner-top toolbox_tab">Ecosystems</span>
            </div>
            <div id="div_details" class="toolbox_panel">
                <div style="padding: 0 0 0 5px;">
                    <span class="detail_label">Name:</span>
                    <asp:TextBox ID="txtEcoTemplateName" runat="server" CssClass="task_details code"
                        te_group="detail_fields" column="ecotemplate_name"></asp:TextBox>
                    <br />
                    <span class="detail_label">Description:</span>
                    <br />
                    <asp:TextBox ID="txtDescription" TextMode="MultiLine" Rows="5" runat="server" CssClass="code"
                        te_group="detail_fields" column="ecotemplate_desc"></asp:TextBox>
                    <br />
                    <br />
                    <br />
                    <hr />
                    <span id="show_log_link" class="pointer">
                        <img alt="" src="../images/icons/view_text_16.png" style="margin: 0px 4px 0px 4px;" />
                        View Change Log</span>
                </div>
            </div>
            <div id="div_tasks" class="toolbox_panel hidden">
                <span>Search:</span>
                <input id="task_search_text" />
                <span id="task_search_btn">Search</span>
                <div id="task_picker_results">
                </div>
            </div>
            <div id="div_ecosystems" class="toolbox_panel hidden">
                <span id="ecosystem_create_btn">New Ecosystem</span>
                <hr />
                <div id="ecosystem_results">
                </div>
            </div>
        </div>
    </div>
    <div id="content_te">
        <center>
            <h3>
                <asp:Label ID="lblEcoTemplateHeader" runat="server"></asp:Label>
            </h3>
        </center>
        <div id="all">
            <div class="ui-state-default te_header">
                <div class="step_section_title">
                    <span id="step_toggle_all_btn" class="pointer">
                        <img class="step_toggle_all_btn" src="../images/icons/expand.png" alt="Expand/Collapse All Tasks"
                            title="Expand/Collapse All Tasks" />
                    </span><span class="step_title">
                        <asp:Label ID="lblActionTitle" runat="server"></asp:Label></span>
                </div>
                <div class="step_section_icons">
                    <span id="print_link" class="pointer">
                        <img class="task_print_link" alt="" src="../images/icons/printer.png" />
                    </span>
                </div>
            </div>
            <div id="actions">
                <div id="action_add" class="ui-widget-content ui-corner-all ui-state-active">
                    <div id="action_name_helper">
                        Drag a Task from the Tasks tab here to create a new Action.</div>
                    <div id="action_add_form" class="ui-widget-content hidden">
                        <input type="hidden" id="action_add_otid" />
                        <div class="ui-state-default">
                            Task: <span id="action_add_task_name"></span>
                        </div>
                        Action:
                        <input id="new_action_name" />
                        <br />
                        <span id="action_add_btn">Add</span> <span id="action_add_cancel_btn">Cancel</span>
                    </div>
                </div>
                <ul class="category_actions" id="category_actions">
                    <asp:PlaceHolder ID="phActions" runat="server"></asp:PlaceHolder>
                </ul>
            </div>
        </div>
    </div>
    <div class="hidden">
        <input type="hidden" id="hidParamType" value="task" />
        <asp:HiddenField ID="hidEcoTemplateID" runat="server"></asp:HiddenField>
    </div>
    <div id="task_remove_confirm_dialog" title="Remove Task" class="hidden ui-state-highlight">
        <p>
            <span class="ui-icon
    ui-icon-info" style="float: left; margin: 0 7px 50px 0;"></span><span>Are you sure you
        want to remove this Task?</span>
        </p>
    </div>
    <div id="action_delete_confirm_dialog" title="Delete Action" class="hidden ui-state-highlight">
        <p>
            <span class="ui-icon
    ui-icon-info" style="float: left; margin: 0 7px 50px 0;"></span><span>Are you sure you
        want to delete this Action?</span>
        </p>
    </div>
    <div id="action_edit_dialog" title="Edit Action" class="hidden">
        <p>
            Name:</p>
        <input id="txtActionNameAdd" />
    </div>
    <div id="action_parameter_dialog" title="Edit Parameters" class="hidden">
        <div id="action_parameter_dialog_params">
        </div>
        <input type="hidden" id="action_parameter_dialog_action_id" />
    </div>
    <div id="ecosystem_add_dialog" class="hidden" title="New Ecosystem">
        <table id="tblNew" width="100%">
            <tbody>
                <tr>
                    <td>
                        Name
                    </td>
                    <td>
                        <input type="text" id="new_ecosystem_name" class="w300px" />
                    </td>
                </tr>
                <tr>
                    <td>
                        Description
                    </td>
                    <td>
                        <textarea id="new_ecosystem_desc" rows="3"></textarea>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
</asp:Content>
