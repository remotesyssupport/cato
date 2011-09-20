//Copyright 2011 Cloud Sidekick
// 
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
//

//This is all the functions to support the ecoTemplateEdit page.
$(document).ready(function () {
    //used a lot
    g_id = $("#ctl00_phDetail_hidEcoTemplateID").val();

    //fix certain ui elements to not have selectable text
    $("#toolbox .toolbox_tab").disableSelection();

    //specific field validation and masking
    $("#ctl00_phDetail_txtEcoTemplateName").keypress(function (e) { return restrictEntryToSafeHTML(e, this); });
    $("#ctl00_phDetail_txtDescription").keypress(function (e) { return restrictEntryToSafeHTML(e, this); });

    //enabling the 'change' event for the Details tab
    $("#div_details :input[te_group='detail_fields']").change(function () { doDetailFieldUpdate(this); });

    //jquery buttons
    $("#task_search_btn").button({ icons: { primary: "ui-icon-search"} });

    //create_ecosystem button
    $("#ecosystem_create_btn").button({ icons: { primary: "ui-icon-plus"} });
    $("#ecosystem_create_btn").live("click", function () {
        $("#new_ecosystem_name").val('');
        $("#new_ecosystem_desc").val('');

        $("#ecosystem_add_dialog").dialog('open');
    });

    //create ecosystem dialog
    $("#ecosystem_add_dialog").dialog({
        autoOpen: false,
        modal: true,
        width: 500,
        buttons: {
            "Create": function () {
                NewEcosystem();
            },
            Cancel: function () {
                $(this).dialog('close');
            }
        }
    });

    //the hook for the 'show log' link
    $("#show_log_link").click(function () {
        var url = "securityLogView.aspx?type=42&id=" + g_id;
        openWindow(url, "logView", "location=no,status=no,scrollbars=yes,resizable=yes,width=800,height=700");
    });




    //turn on the "add" button in the dropzone
    $("#action_add_btn").button({ icons: { primary: "ui-icon-plus"} });
    $("#action_add_btn").click(function () {
        addAction();
    });
    //and the cancel button
    $("#action_add_cancel_btn").button({ icons: { primary: "ui-icon-plus"} });
    $("#action_add_cancel_btn").click(function () {
        $("#action_add_form").hide().prev().show();
    });

    //dragging tasks from the task tab can be dropped on the drop zone to add a new Action
    $("#action_add").droppable({
        drop: function (event, ui) {
            var task_label = ui.draggable.find(".search_dialog_value_name").html();

            //clear the field
            $("#new_action_name").val("");

            $("#action_add_otid").val(ui.draggable.attr("original_task_id"));
            $("#action_add_task_name").html(task_label);
            $("#action_name_helper").hide().next().show();
            $("#new_action_name").focus();

            $(this).removeClass("ui-state-highlight")
        },
        over: function (event, ui) {
            $(this).addClass("ui-state-highlight")
        },
        out: function (event, ui) {
            $(this).removeClass("ui-state-highlight")
        }
    });


    //this is crazy... dropping a task onto an existing action will CHANGE THE TASK
    //simply by updating the field to the new ID, and firing a change event.
    $(".action").droppable({
        drop: function (event, ui) {
            var task_label = ui.draggable.find(".search_dialog_value_name").html();
            var otid = ui.draggable.attr("original_task_id");
            var action_id = $(this).attr("id").replace(/ac_/,"");

            //update the hidden task field
            $(this).find('[column="original_task_id"]').val(otid);
            //fire the change event
            $(this).find('[column="original_task_id"]').change();
            //reset version
            $(this).find('[column="task_version"]').val("");
            //fire the change event
            $(this).find('[column="task_version"]').change();

            //reget the whole action, as the versions and parameters will be different
            $.ajax({
                async: false,
                type: "POST",
                url: "uiMethods.asmx/wmGetEcotemplateAction",
                data: '{"sActionID":"' + action_id + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (retval) {
                    //replace our inner content
                    $("#ac_" + action_id).html(retval.d);
                },
                error: function (response) {
                    showAlert(response.responseText);
                }
            });
        },
        over: function (event, ui) {
            $(this).find(".step_header").addClass("ui-state-highlight")
        },
        out: function (event, ui) {
            $(this).find(".step_header").removeClass("ui-state-highlight")
        }
    });


    //the onclick event of the Task Tab search button
    $(".action_remove_btn").live("click", function () {
        var action_id = $(this).parents("li").attr('id').replace(/ac_/, "");

        if (confirm("Are you sure?")) {
            $("#actions").block({ message: null, cursor: 'wait' });

            var search_text = $("#task_search_text").val();
            $.ajax({
                async: true,
                type: "POST",
                url: "uiMethods.asmx/wmDeleteEcotemplateAction",
                data: '{"sActionID":"' + action_id + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (retval) {
                    //just remove it from the DOM
                    $("#ac_" + action_id).remove();

                    $("#actions").unblock();
                },
                error: function (response) {
                    showAlert(response.responseText);
                }
            });
        }
    });

    //the onclick event of the Task Tab search button
    $("#task_search_btn").click(function () {
        //var field = $("#" + $("#task_picker_target_field_id").val());

        $("#div_tasks").block({ message: null, cursor: 'wait' });
        var search_text = $("#task_search_text").val();
        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmTaskSearch",
            data: '{"sSearchText":"' + search_text + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                $("#task_picker_results").html(retval.d);

                $("#task_picker_results .task_picker_value").disableSelection();
                $("#div_tasks").unblock();

                //task description tooltips on the task picker dialog
                $("#task_picker_results .search_dialog_tooltip").tipTip({
                    defaultPosition: "right",
                    keepAlive: false,
                    activation: "hover",
                    maxWidth: "500px",
                    fadeIn: 100
                });

                initDraggable();

            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    });

    //what happens when you change a value on an action?
    //(we're binding this to any field that has a "field" attribute.
    $('[column]').live('change', function () {
        doActionFieldUpdate(this);
    });


    //the task print link
    $("#category_actions .task_print_btn").live("click", function () {
        var url = "taskPrint.aspx?task_id=" + $(this).attr("task_id");
        openWindow(url, "taskPrint", "location=no,status=no,scrollbars=yes,resizable=yes,width=800,height=700");
    });
    //the task edit link
    $("#category_actions .task_open_btn").live("click", function () {
        location.href = "taskEdit.aspx?task_id=" + $(this).attr("task_id");
    });


    // enter key for task search
    $("#task_search_txt").live("keypress", function (e) {
        if (e.which == 13) {
            $("#task_search_btn").click();
            return false;
        }
    });

    // enter key for Action Add
    $("#new_action_name").live("keypress", function (e) {
        if (e.which == 13) {
            $("#action_add_btn").click();
            return false;
        }
    });


    $(".action_param_edit_btn").live("click", function () {
        var action_id = $(this).parents("li").attr('id').replace(/ac_/, "");
        ShowParameterDialog(action_id);
    });

    $(".ecosystem_name").live("click", function () {
        showPleaseWait();
        location.href = 'ecosystemEdit.aspx?ecosystem_id=' + $(this).parents("li").attr('ecosystem_id');
    });

    //init the parameter dialog
    $("#action_parameter_dialog").dialog({
        autoOpen: false,
        modal: false,
        width: 500,
        open: function (event, ui) { $(".ui-dialog-titlebar-close", ui).hide(); },
        buttons: {
            OK: function () {
                SaveParameterDefaults();
            },
            Cancel: function () {
                $(this).dialog('close');
            }
        }
    });


    //all done... load up the Ecosystems div
    GetEcosystems();
});

function CloudAccountWasChanged() {
    GetEcosystems();
}

function GetEcosystems() {
    $.ajax({
        async: false,
        type: "POST",
        url: "uiMethods.asmx/wmGetEcotemplateEcosystems",
        data: '{"sEcoTemplateID":"' + g_id + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (retval) {
            $("#ecosystem_results").html(retval.d);

            //task description tooltips on the task picker dialog
            $("#ecosystem_results .ecosystem_tooltip").tipTip({
                defaultPosition: "right",
                keepAlive: false,
                activation: "hover",
                maxWidth: "500px",
                fadeIn: 100
            });

        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}


function ShowParameterDialog(action_id) {
    $("#action_parameter_dialog_action_id").val(action_id);

    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmGetParameterXML",
        data: '{"sType":"action","sID":"' + action_id + '","sFilterByEcosystemID":""}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (retval) {
            if (retval.d != "")
                parameter_xml = $.parseXML(retval.d);
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });

    //using the same function and layout that we do for the task launch dialog.
    var output = DrawParameterEditForm(parameter_xml);
    $("#action_parameter_dialog_params").html(output);

    bindParameterToolTips();

    $("#action_parameter_dialog").dialog('open');
}

function SaveParameterDefaults() {
    var action_id = $("#action_parameter_dialog_action_id").val();

    $("#update_success_msg").text("Saving Defaults...");

    //build the XML from the dialog
    var parameter_xml = packJSON(buildXMLToSubmit());
    //alert(parameter_xml);

    $.ajax({
        async: false,
        type: "POST",
        url: "uiMethods.asmx/wmSaveActionParameterXML",
        data: '{"sActionID":"' + action_id + '","sActionDefaultsXML":"' + parameter_xml + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $("#update_success_msg").text("Save Successful").fadeOut(2000);
        },
        error: function (response) {
            $("#update_success_msg").fadeOut(2000);
            showAlert(response.responseText);
        }
    });

    $("#action_parameter_dialog").dialog('close');

}

function getActions() {
    $.ajax({
        async: false,
        type: "POST",
        url: "uiMethods.asmx/wmGetEcotemplateActions",
        data: '{"sEcoTemplateID":"' + g_id + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (retval) {
            $("#category_actions").html(retval.d);

            $("#actions").unblock();
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}

function addAction() {
    var otid = $("#action_add_otid").val();
    var action_name = $("#new_action_name").val();


    //loop the name fields looking for this value.
    var exists = $("input[column='action_name'][value='" + action_name + "']").length;

    if (exists > 0) {
        alert("An Action with that name already exists.");
        $("#new_action_name").val("");
        $("#new_action_name").focus();
        return;
    }

    if (action_name.length > 0) {
        //this ajax takes forever... block it.
        $("#actions").block({ message: null, cursor: 'wait' });

        $("#update_success_msg").text("Updating...").show();
        $.ajax({
            async: true,
            type: "POST",
            url: "uiMethods.asmx/wmAddEcotemplateAction",
            data: '{"sEcoTemplateID":"' + g_id + '","sActionName":"' + action_name + '","sOTID":"' + otid + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                $("#update_success_msg").text("Update Successful").fadeOut(2000);

                //reset the input dropzone
                $("#action_add_form").hide().prev().show();

                // get the whole list again
                getActions();
            },
            error: function (response) {
                $("#update_success_msg").fadeOut(2000);
                showAlert(response.responseText);
            }
        });
    }
    else {
        alert("Please enter an Action Name.");
    }

}
function doActionFieldUpdate(ctl) {
    var action_id = $(ctl).parents("li").attr('id').replace(/ac_/, "");
    var column = $(ctl).attr("column");
    var value = $(ctl).val();

    //escape it
    value = packJSON(value);

    if (column.length > 0) {
        $("#update_success_msg").text("Updating...").show();
        $.ajax({
            async: false,
            type: "POST",
            url: "uiMethods.asmx/wmUpdateEcoTemplateAction",
            data: '{"sEcoTemplateID":"' + g_id + '","sActionID":"' + action_id + '","sColumn":"' + column + '","sValue":"' + value + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                if (msg.d != '') {
                    $("#update_success_msg").text("Update Failed").fadeOut(2000);
                    showInfo(msg.d);
                }
                else {
                    $("#update_success_msg").text("Update Successful").fadeOut(2000);

                    // Change the name in the header
                    if (column == "action_name") { $(ctl).parents(".action_name_lbl").html(unescape(value)); };
                    if (column == "category") { $(ctl).parents(".action_category_lbl").html(unescape(value) = " - "); };
                }
            },
            error: function (response) {
                $("#update_success_msg").fadeOut(2000);
                showAlert(response.responseText);
            }
        });
    }

}
function doDetailFieldUpdate(ctl) {
    var column = $(ctl).attr("column");
    var value = $(ctl).val();

    //for checkboxes and radio buttons, we gotta do a little bit more, as the pure 'val()' isn't exactly right.
    //and textareas will not have a type property!
    if ($(ctl).attr("type")) {
        var typ = $(ctl).attr("type").toLowerCase();
        if (typ == "checkbox") {
            value = (ctl.checked == true ? 1 : 0);
        }
        if (typ == "radio") {
            value = (ctl.checked == true ? 1 : 0);
        }
    }

    //escape it
    value = packJSON(value);

    if (column.length > 0) {
        $("#update_success_msg").text("Updating...").show();
        $.ajax({
            async: false,
            type: "POST",
            url: "uiMethods.asmx/wmUpdateEcoTemplateDetail",
            data: '{"sEcoTemplateID":"' + g_id + '","sColumn":"' + column + '","sValue":"' + value + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                if (msg.d != '') {
                    $("#update_success_msg").text("Update Failed").fadeOut(2000);
                    showInfo(msg.d);
                }
                else {
                    $("#update_success_msg").text("Update Successful").fadeOut(2000);

                    // Change the name in the header
                    if (column == "ecotemplate_name") { $("#ctl00_phDetail_lblEcoTemplateHeader").html(unescape(value)); };
                }
            },
            error: function (response) {
                $("#update_success_msg").fadeOut(2000);
                showAlert(response.responseText);
            }
        });
    }
}

function initDraggable() {
    //initialize the 'commands' tab and the clipboard tab to be draggable to the step list
    $("#toolbox .search_dialog_value").draggable("destroy");
    $("#toolbox .search_dialog_value").draggable({
        distance: 30,
        // connectToSortable: '#action_categories',
        appendTo: 'body',
        revert: 'invalid',
        scroll: false,
        opacity: 0.95,
        helper: 'clone'
        //        start: function(event, ui) {
        //            $("#dd_dragging").val("true");
        //        },
        //        stop: function(event, ui) {
        //            $("#dd_dragging").val("false");
        //        }
    })


    //unbind it first so they don't stack (since hover doesn't support "live")
    //    $("#toolbox .function").unbind("mouseenter mouseleave");
    //    $("#toolbox .command_item").unbind("mouseenter mouseleave");

    //set the help text on hover over a function
    //    $("#toolbox .function").live("hover", function () {
    //        $("#te_help_box_detail").html($("#help_text_" + $(this).attr("name")).html());
    //    }, function () {
    //        $("#te_help_box_detail").html("");
    //    });

    //    $("#toolbox .command_item").hover(function () {
    //        $(this).addClass("command_item_hover");
    //    }, function () {
    //        $(this).removeClass("command_item_hover");
    //    });
}

function NewEcosystem() {
    var bSave = true;
    var strValidationError = '';

    //name is required
    if ($("#new_ecosystem_name").val() == "") {
        bSave = false;
        strValidationError += 'Please enter an Ecosystem Name.';
        return false;
    };


    //we will test it, but really we're not gonna use it rather we'll get it server side
    //this just traps if there isn't one.
    if ($("#ctl00_ddlCloudAccounts").val() == "") {
        bSave = false;
        strValidationError += 'Error: Unable to determine Cloud Account.';
    };

    if (bSave != true) {
        showAlert(strValidationError);
        return false;
    }

    $.ajax({
        async: false,
        type: "POST",
        url: "uiMethods.asmx/wmCreateEcosystem",
        data: '{"sName":"' + $("#new_ecosystem_name").val() + '","sDescription":"' + $("#new_ecosystem_desc").val() + '","sEcotemplateID":"' + g_id + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            if (msg.d.length == 36) {
                //just add it to the list here
                GetEcosystems();
                $("#ecosystem_add_dialog").dialog('close');
            } else {
                showAlert(msg.d);
            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}
