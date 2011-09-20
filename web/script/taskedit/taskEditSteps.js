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

var fieldWithFocus = null;

$(document).ready(function() {
    //toggle it when you click on a step
    $("#steps .step_toggle_btn").live("click", function() {
        //alert($(this).attr("id").replace(/step_header_/, ""));
        var step_id = $(this).attr("step_id");
        var visible = 0;

        //toggle it
        $("#step_detail_" + step_id).toggleClass("step_collapsed");

        //then check it
        if (!$("#step_detail_" + step_id).hasClass("step_collapsed"))
            visible = 1;

        // swap the child image, this will work as long as the up down image stays the first child if the span
        if (visible == 1) {
            $(this).children(":first-child").attr("src", "../images/icons/expand_down.png");
        } else {
            $(this).children(":first-child").attr("src", "../images/icons/expand_up.png");
        }



        //now persist it
        $("#update_success_msg").text("Updating...").show();
        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmToggleStep",
            data: '{"sStepID":"' + step_id + '","sVisible":"' + visible + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function(msg) {
                $("#update_success_msg").text("Update Successful").fadeOut(2000);
            },
            error: function(response) {
                $("#update_success_msg").fadeOut(2000);
                showAlert(response.responseText);
            }
        });
    });

    //toggle all of them
    $("#step_toggle_all_btn").live("click", function() {
        //if all are closed, open them all, else close them all
        // also change the expand images
        if ($("#steps .step_collapsed").length == $(".step_detail").length) {
            $("#steps .step_detail").removeClass("step_collapsed");
            $(this).children(":first-child").attr("src", "../images/icons/expand_down.png");
            $("#steps .expand_image").attr("src", "../images/icons/expand_down.png");
        } else {
            $("#steps .step_detail").addClass("step_collapsed");
            $("#steps .step_common_detail").addClass("step_common_collapsed");
            $(this).children(":first-child").attr("src", "../images/icons/expand_up.png");
            $("#steps .expand_image").attr("src", "../images/icons/expand_up.png");
        }
    });

    //common details within a step
    //toggle it when you click on one of the buttons
    $("#steps .step_common_button").live("click", function() {
        var btn = "";
        var dtl = $(this).attr("id").replace(/btn_/, "");
        var stp = $(this).attr("step_id");

        //if the one we just clicked on is already showing, hide them all
        if ($("#" + dtl).hasClass("step_common_collapsed")) {
            //hide all
            $("#steps div[id^=step_common_detail_" + stp + "]").addClass("step_common_collapsed");
            $("#step_detail_" + stp + " .step_common_button").removeClass("step_common_button_active");

            //show this one
            $("#" + dtl).removeClass("step_common_collapsed");
            $(this).addClass("step_common_button_active");
            btn = $(this).attr("button");
        } else {
            $("#" + dtl).addClass("step_common_collapsed");
            $(this).removeClass("step_common_button_active");
        }

        //now persist it
        $("#update_success_msg").text("Updating...").show();
        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmToggleStepCommonSection",
            data: '{"sStepID":"' + stp + '","sButton":"' + btn + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function(msg) {
                $("#update_success_msg").text("Update Successful").fadeOut(2000);
            },
            error: function(response) {
                $("#update_success_msg").fadeOut(2000);
                showAlert(response.responseText);
            }
        });
    });

    // Command tab details
    //help button
    $("#command_help_btn").button({ icons: { primary: "ui-icon-help"} });

    $("#command_help_btn").live("click", function () {
        var url = "taskCommandHelp.aspx";
        openWindow(url, "commandhelp", "location=no,status=no,scrollbars=yes,width=800,height=700");
    });

    //the onclick event of the 'skip' icon of each step
    $("#steps .step_skip_btn").live("click", function() {
        //click the hidden field and fire the change event
        var step_id = $(this).attr("step_id");
        var field = $(this).attr("datafield_id");
        if ($("#" + field).val() == "1") {
            $("#" + field).val("0");

            this.src = this.src.replace(/cnrgrey/, "cnr");
            $("#" + step_id).removeClass("step_skip");
            $("#step_header_" + step_id).removeClass("step_header_skip");
            $("#step_detail_" + step_id).removeClass("step_collapsed");
        } else {
            $("#" + field).val("1");

            this.src = this.src.replace(/cnr/, "cnrgrey");
            $("#" + step_id).addClass("step_skip");
            $("#step_header_" + step_id).addClass("step_header_skip");
            $("#step_detail_" + step_id).addClass("step_collapsed");
        }

        $("#" + field).change();
    });

    //the onclick event of the 'popout' icon of each step
    $("#steps .variable_popup_btn").live("click", function() {
        var url = "taskStepVarsEdit.aspx?step_id=" + $(this).attr("step_id");
        openWindow(url, "stepedit", "location=no,status=no,scrollbars=yes,width=800,height=700");
    });

    //the onclick event of the 'delete' link of each step
    $("#steps .step_delete_btn").live("click", function() {
        $("#ctl00_phDetail_hidStepDelete").val($(this).attr("remove_id"));
        $("#step_delete_confirm_dialog").dialog('open');
    });

    //the onclick event of the 'copy' link of each step
    $("#steps .step_copy_btn").live("click", function() {
        $("#update_success_msg").text("Copying...").show();
        var step_id = $(this).attr("step_id");

        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmCopyStepToClipboard",
            data: '{"sStepID":"' + step_id + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function(msg) {
                doGetClips();
                $("#update_success_msg").text("Copy Successful").fadeOut(2000);
            },
            error: function(response) {
                $("#update_success_msg").fadeOut(2000);
                showAlert(response.responseText);
            }
        });
    });

    //the onclick event of the 'remove' link of embedded steps
    $("#steps .embedded_step_delete_btn").live("click", function() {
        $("#embedded_step_remove_id").val($(this).attr("remove_id"));
        $("#embedded_step_parent_id").val($(this).attr("parent_id"));
        $("#embedded_step_delete_confirm_dialog").dialog('open');
        //alert('remove step ' + $(this).attr("remove_id") + ' from ' + $(this).attr("parent_id"));
    });

    //the onclick event of 'connection picker' buttons on selected fields
    $("#steps .conn_picker_btn").live("click", function(e) {
        //hide any open pickers
        $("div[id$='_picker']").hide();
        var field = $("#" + $(this).attr("link_to"));

        //first, populate the picker
        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmGetTaskConnections",
            data: '{"sTaskID":"' + $("#ctl00_phDetail_hidTaskID").val() + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function(retval) {
                $("#conn_picker_connections").html(retval.d);
                $("#conn_picker_connections .value_picker_value").hover(
                        function() {
                            $(this).toggleClass("value_picker_value_hover");
                        },
                        function() {
                            $(this).toggleClass("value_picker_value_hover");
                        }
                    );
                $("#conn_picker_connections .value_picker_value").click(function() {
                    field.val($(this).text());
                    field.change();

                    $("#conn_picker").slideUp();
                });
            },
            error: function(response) {
                showAlert(response.responseText);
            }
        });

        //change the click event of this button to now close the dialog
        //        $(this).die("click");
        //        $(this).click(function() { $("#conn_picker").slideUp(); });

        $("#conn_picker").css({ top: e.clientY, left: e.clientX });
        $("#conn_picker").slideDown();
    });


    $("#conn_picker_close_btn").live("click", function(e) {
        //hide any open pickers
        $("div[id$='_picker']").hide();
    });
    $("#var_picker_close_btn").live("click", function(e) {
        //hide any open pickers
        $("div[id$='_picker']").hide();
    });


    //// CODEBLOCK PICKER
    //the onclick event of 'codeblock picker' buttons on selected fields
    $("#steps .codeblock_picker_btn").live("click", function(e) {
        //hide any open pickers
        $("div[id$='_picker']").hide();
        field = $("#" + $(this).attr("link_to"));
        stepid = $(this).attr("step_id");

        //first, populate the picker
        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmGetTaskCodeblocks",
            data: '{"sTaskID":"' + $("#ctl00_phDetail_hidTaskID").val() + '","sStepID":"' + stepid + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function(retval) {
                $("#codeblock_picker_codeblocks").html(retval.d);
                $("#codeblock_picker_codeblocks .value_picker_value").hover(
                        function() {
                            $(this).toggleClass("value_picker_value_hover");
                        },
                        function() {
                            $(this).toggleClass("value_picker_value_hover");
                        }
                    );
                $("#codeblock_picker_codeblocks .value_picker_value").click(function() {
                    field.val($(this).text());
                    field.change();

                    $("#codeblock_picker").slideUp();
                });
            },
            error: function(response) {
                showAlert(response.responseText);
            }
        });

        $("#codeblock_picker").css({ top: e.clientY, left: e.clientX });
        $("#codeblock_picker").slideDown();
    });
    $("#codeblock_picker_close_btn").live("click", function(e) {
        //hide any open pickers
        $("[id$='_picker']").hide();
    });

    //////TASK PICKER
    //the onclick event of the 'task picker' buttons everywhere
    $("#steps .task_picker_btn").live("click", function() {
        $("#task_picker_target_field_id").val($(this).attr("target_field_id"));
        //alert($(this).attr("target_field_id") + "\n" + $(this).prev().prev().val());
        $("#task_picker_step_id").val($(this).attr("step_id"));
        $("#task_picker_dialog").dialog('open');
    });

    // when you hit enter inside 'task picker' for a subtask
    $("#task_search_text").live("keypress", function(e) {
        //alert('keypress');
        if (e.which == 13) {
            $("#task_search_btn").click();
            return false;
        }
    });

    //the onclick event of the 'task picker' search button
    $("#task_search_btn").click(function() {
        var field = $("#" + $("#task_picker_target_field_id").val());

        $("#task_picker_dialog").block({ message: null, cursor: 'wait' });
        var search_text = $("#task_search_text").val();
        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmTaskSearch",
            data: '{"sSearchText":"' + search_text + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function(retval) {
                $("#task_picker_results").html(retval.d);
                //bind the onclick event for the new results
                $("#task_picker_results .task_picker_value").disableSelection();
                $("#task_picker_dialog").unblock();
                
                //gotta kill previously bound clicks or it will stack 'em! = bad.
                $("#task_picker_results li[tag='task_picker_row']").die();
                $("#task_picker_results li[tag='task_picker_row']").live("click", function() {

                    $("#task_steps").block({ message: null });

                    $("#task_picker_dialog").dialog('close');
                    $("#task_picker_results").empty();

                    field.val($(this).attr("original_task_id"));
                    field.change();

                });

                //task description tooltips on the task picker dialog
                $("#task_picker_results .search_dialog_tooltip").tipTip({
                    defaultPosition: "right",
                    keepAlive: false,
                    activation: "hover",
                    maxWidth: "500px",
                    fadeIn: 100
                });
            },
            error: function(response) {
                showAlert(response.responseText);
            }
        });
    });
    //////END TASK PICKER



    //COMMENTED OUT THE BEGINNING OF EDITING A WHOLE STEP AT ONCE.
    //Possible, but cosmic and not crucial right now.

    //////    $("[te_group='step_fields']").live("focus", function() {
    //////        //stick the step_id in a hidden field so we'll know when we move off of it.
    //////        $("editing_step_id").val($(this).attr("step_id"));
    //////    });

    //////DIALOGS
    //init the step delete confirm dialog
    $("#step_delete_confirm_dialog").dialog({
        autoOpen: false,
        draggable: false,
        resizable: false,
        bgiframe: true,
        modal: true,
        overlay: {
            backgroundColor: '#000',
            opacity: 0.5
        },
        buttons: {
            'Delete': function() {
                doStepDelete();
                $(this).dialog('close');
            },
            Cancel: function() {
                $(this).dialog('close');
            }
        }
    });

    //init the embedded step delete confirm dialog
    $("#embedded_step_delete_confirm_dialog").dialog({
        autoOpen: false,
        draggable: false,
        resizable: false,
        bgiframe: true,
        modal: true,
        overlay: {
            backgroundColor: '#000',
            opacity: 0.5
        },
        buttons: {
            'Delete': function() {
                doEmbeddedStepDelete();
                $(this).dialog('close');
            },
            Cancel: function() {
                $(this).dialog('close');
            }
        }
    });

    //init the task picker dialog
    $("#task_picker_dialog").dialog({
        autoOpen: false,
        draggable: false,
        resizable: false,
        bgiframe: true,
        modal: true,
        height: 600,
        width: 600,
        overlay: {
            backgroundColor: '#000',
            opacity: 0.5
        },
        close: function(event, ui) {
            $("#task_search_text").val("");
            $("#task_picker_results").empty();
        }
    });
    //////END DIALOGS


});

/*
NOTE: There is a common issue with async postbacks and jQuery.
It's commonly called the "ASP.NET jQuery postback problem"
see:
http://blog.dreamlabsolutions.com/post/2009/03/25/jQuery-live-and-ASPNET-Ajax-asynchronous-postback.aspx
http://blog.dreamlabsolutions.com/post/2009/02/24/jQuery-document-ready-and-ASP-NET-Ajax-asynchronous-postback.aspx

For 'bindings' in jquery, use the 'live' event.  (clicks, hovers, etc.)

BUT, for initializing UI components that are dynamic, you can't.  (Because this is init code, not bindings.)

So, we do those inits in the pageLoad function.
*/

function pageLoad() {
    //do this here until (if) the codeblock click gets the full step list via ajax
    initSortable();
    validateStep();

    //fix it so you can't select text in the step headers.
    $("#steps .step_header").disableSelection();

    // hate to have to move this here, but when a codeblock is renamed
    // it reloads the repeater, and we have to hide the main codeblock delete button again
    //the delete and rename links on the MAIN codeblock are always unavailable!
    //but it's a pita to take it out of the repeater in C#, so just remove it here.
    $("#codeblock_rename_btn_MAIN").remove();
    $("#codeblock_delete_btn_MAIN").remove();

}

//called in rare cases when the value entered in one field should push it's update through another field.
//(when the "wired" field is hidden, and the data entry field is visible but not wired.)
function pushStepFieldChangeVia(pushing_ctl, push_via_id) {
    //what's the value in the control doing the pushing?
    var field_value = $(pushing_ctl).val();
    
    //shove that value in the push_via field
    $("#" + push_via_id).val(field_value);
    
    //and force the change event?
    $("#" + push_via_id).change();
}

//fires when each individual field on the page is changed.
function onStepFieldChange(ctl, step_id, xpath) {
    $("#update_success_msg").text("Updating...").show();

    //    var step_id = $(ctl).attr("step_id");
    var field_value = $(ctl).val();
    var func = ctl.getAttribute("function");
    var stepupdatefield = "update_" + func + "_" + step_id;
    var reget = ctl.getAttribute("reget_on_change");

    //for checkboxes and radio buttons, we gotta do a little bit more, as the pure 'val()' isn't exactly right.
    var typ = ctl.type;
    if (typ == "checkbox") {
        field_value = (ctl.checked == true ? 1 : 0);
    }
    if (typ == "radio") {
        field_value = (ctl.checked == true ? 1 : 0);
    }

    //simple whack-and-add
    //Why did we use a textarea?  Because the actual 'value' may be complex
    //(sql, code, etc) with lots of chars needing escape sequences.
    //So, stick the complex value in an element that doesn't need any special handling.
    $("#" + stepupdatefield).remove();
    $("#step_update_array").append("<textarea id='" + stepupdatefield + "' step_id='" + step_id + "' function='" + func + "' xpath='" + xpath + "'>" + field_value + "</textarea>");

    doStepDetailUpdate(stepupdatefield, step_id, func, xpath);

    //if reget is true, go to the db and refresh the whole step
    if (reget == "true") {
        $("#task_steps").block({ message: null });
        getStep(step_id, step_id, true);
        $("#task_steps").unblock();
    }
    else {
        //if we're not regetting, we need to handle visualization of validation
        validateStep(step_id);
    }

    $("#update_success_msg").fadeOut(2000);

}


function initSortable() {
    //this is more than just initializing the sortable... it's some other stuff that needs to be set up
    //after adding to the sortable.
    //define the variable picker context menu

    //enable right click on all edit fields
    $("#steps :input[te_group='step_fields']").rightClick(function(e) {
        showVarPicker(e);
    });

    //NOTE: 8-3-09 NSC --- commented this out, performance seems to be better with this
    //declared directly on the field, where values can be passed without dom lookups

    //HATE THIS.. but the 'change' event isn't supported on the live() function.
    //the pageload event doesn't work for bindings or changing attributes, because they will stack.
    //so you have to be careful to REMOVE or UNBIND anything before you add/bind it.
    //    $("#steps :input[te_group='step_fields']").unbind("change", onStepFieldChange);
    //    $("#steps :input[te_group='step_fields']").change(onStepFieldChange);

    //end commented


    //what happens when a step field gets focus?
    //we show the help for that field in the help pane.
    //and set a global variable with it's id
    $("#steps :input[te_group='step_fields']").unbind("focus");
    $("#steps :input[te_group='step_fields']").focus(function() {
        $("#te_help_box_detail").html($(this).attr("help"));
        fieldWithFocus = $(this).attr("id");
    });

    //some steps may have 'required' fields that are not populated
    //set up a visualization
    //validateStep();

    //set up the sortable
    $("#steps").sortable("destroy");
    $("#steps").sortable({
        handle: $(".step_header"),
        distance: 20,
        placeholder: 'ui-widget-content ui-corner-all ui-state-active step_drop_placeholder',
        items: 'li:not(#no_step)',
        update: function(event, ui) {
            //if this is a new step... add
            //(add will reorder internally)
            var new_step = $(ui.item[0]);
            if (new_step.attr("id").indexOf("fn_") == 0 || new_step.attr("id").indexOf("clip_") == 0) {
                doStepAdd(new_step);
            } else {
                //else just reorder what's here.
                doStepReorder();
            }
        }
    });
}

function showVarPicker(e) {
    //hide any open pickers
    $("div[id$='_picker']").hide();

    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmGetTaskVariables",
        data: '{"sTaskID":"' + $("#ctl00_phDetail_hidTaskID").val() + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(retval) {
            $("#var_picker_variables").html(retval.d);

            $("#var_picker_variables .value_picker_group").click(function() {
                $("#" + $(this).attr("target")).toggleClass("hidden");
            });

            $("#var_picker_variables .value_picker_value").hover(
                        function() {
                            $(this).toggleClass("value_picker_value_hover");
                        },
                        function() {
                            $(this).toggleClass("value_picker_value_hover");
                        }
                    );

            $("#var_picker_variables .value_picker_group").hover(
                        function() {
                            $(this).toggleClass("value_picker_value_hover");
                        },
                        function() {
                            $(this).toggleClass("value_picker_value_hover");
                        }
                    );

            $("#var_picker_variables .value_picker_value").click(function() {
                var fjqo = $("#" + fieldWithFocus);
                var f = $("#" + fieldWithFocus)[0];
                var varname = "";

                //note: certain fields should get variable REPLACEMENT syntax [[foo]]
                //others should get the actual name of the variable
                //switch on the function_name to determine this
                var func = fjqo.attr("function");

                switch (func) {
                    case "clear_variable":
                        varname = $(this).text();
                        break;
                    case "substring":
                        // bugzilla 1234 in substring only the variable_name field gets the value without the [[ ]]
                        var xpath = fjqo.attr("xpath");
                        if (xpath == "variable_name") {
                            varname = $(this).text();
                        } else {
                            varname = "[[" + $(this).text() + "]]";
                        }
                        break;
                    case "loop":
                        // bugzilla 1234 in substring only the variable_name field gets the value without the [[ ]]
                        var xpath = fjqo.attr("xpath");
                        if (xpath == "counter") {
                            varname = $(this).text();
                        } else {
                            varname = "[[" + $(this).text() + "]]";
                        }
                        break;
                    case "set_variable":
                        // bugzilla 1234 in substring only the variable_name field gets the value without the [[ ]]
                        var xpath = fjqo.attr("xpath");
                        if (xpath.indexOf("/name", 0) != -1) {
                            varname = $(this).text();
                        } else {
                            varname = "[[" + $(this).text() + "]]";
                        }
                        break;
                    default:
                        varname = "[[" + $(this).text() + "]]";
                        break;
                }

                //IE support
                if (document.selection) {
                    f.focus();
                    sel = document.selection.createRange();
                    sel.text = varname;
                    f.focus();
                }
                //MOZILLA / NETSCAPE support
                else if (f.selectionStart || f.selectionStart == '0') {
                    var startPos = f.selectionStart;
                    var endPos = f.selectionEnd;
                    var scrollTop = f.scrollTop;
                    f.value = f.value.substring(0, startPos) + varname + f.value.substring(endPos, f.value.length);
                    f.focus();
                    f.selectionStart = startPos + varname.length;
                    f.selectionEnd = startPos + varname.length;
                    f.scrollTop = scrollTop;
                } else {
                    f.value = varname;
                    f.focus();
                }

                fjqo.change();
                $("#var_picker").slideUp();
            });
        },
        error: function(response) {
            showAlert(response.responseText);
        }
    });

    $("#var_picker").css({ top: e.clientY, left: e.clientX });
    $("#var_picker").slideDown();
}
function doStepDelete() {
    //if there are any active drop zones on the page, deactivate them first...
    //otherwise the sortable may get messed up.
    $(".step_nested_drop_target_active").each(
        function(intIndex) {
            doDropZoneDisable($(this).attr("id"));
        }
    );


    var step_id = $("#ctl00_phDetail_hidStepDelete").val();
    //don't need to block, the dialog blocks.
    $("#update_success_msg").text("Updating...").show();
    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmDeleteStep",
        data: '{"sStepID":"' + step_id + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(msg) {
            //pull the step off the page
            $("#" + step_id).remove();

            if ($("#steps .step").length == 0) {
                $("#no_step").removeClass("hidden");
            } else {
                //reorder the remaining steps
                //BUT ONLY IN THE BROWSER... the ajax post we just did took care or renumbering in the db.
                $("#steps .step_order_label").each(
                    function(intIndex) {
                        $(this).html(intIndex + 1); //+1 because we are a 1 based array on the page
                    }
                );
            }

            $("#update_success_msg").text("Update Successful").fadeOut(2000);
        },
        error: function(response) {
            $("#update_success_msg").fadeOut(2000);
            showAlert(response.responseText);
        }
    });

    $("#ctl00_phDetail_hidStepDelete").val("");
}

function doStepAdd(new_step) {
    //ok this works, but we need to know if it's a new item before we override the html
    var task_id = $("#ctl00_phDetail_hidTaskID").val();
    var codeblock_name = $("#ctl00_phDetail_hidCodeblockName").val();
    var item = new_step.attr("id");

    //it's a new drop!
    //do the add and get the HTML
    $("#task_steps").block({ message: null });
    $("#update_success_msg").text("Adding...").show();

    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmAddStep",
        data: '{"sTaskID":"' + task_id + '","sCodeblockName":"' + codeblock_name + '","sItem":"' + item + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(retval) {
            $("#no_step").addClass("hidden");
            //use the new html str
            //the first 36 chars are the step_id, everything after that is the html
            var new_step_id = retval.d.substring(0, 36);

            //the rest of the return is the HTML of the step
            new_step.replaceWith(retval.d.substring(36));


            //then reorder the other steps
            doStepReorder();

            //note we're not 'unblocking' the ui here... that will happen in stepReorder

            //NOTE NOTE NOTE: this is temporary
            //until I fix the copy command ... we will delete the clipboard item after pasting
            //this is not permanent, but allows it to be checked in now
            if (item.indexOf('clip_') != -1)
                doClearClipboard(item.replace(/clip_/, ""))

            //but we will change the sortable if this command has embedded commands.
            //you have to add the embedded command NOW, or click cancel.
            if (item == "fn_if" || item == "fn_loop" || item == "fn_exists") {
                doDropZoneEnable($("#" + new_step_id + " .step_nested_drop_target"));
            }
            else {
                initSortable();
                validateStep(new_step_id);
            }
        },
        error: function(response) {
            $("#update_success_msg").fadeOut(2000);
            showAlert(response.responseText);
        }
    });
}
function doStepReorder() {
    //get the steps from the step list
    var steparray = $('#steps').sortable('toArray');

    $("#task_steps").block({ message: null });
    $("#update_success_msg").text("Updating...").show();

    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmReorderSteps",
        data: '{"sSteps":"' + steparray + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(msg) {
            //renumber the step widget labels
            $("#steps .step_order_label").each(
                function(intIndex) {
                    $(this).html(intIndex + 1); //+1 because we are a 1 based array on the page
                }
            );

            $("#task_steps").unblock();
            $("#update_success_msg").text("Update Successful").fadeOut(2000);
        },
        error: function(response) {
            $("#update_success_msg").fadeOut(2000);
            showAlert(response.responseText);
        }
    });
}

function doStepDetailUpdate(field, step_id, func, xpath) {
    if ($("#step_update_array").length > 0) {
        var stuff = new Array();

        //get the value ready to be shipped via json
        //since some steps can have complex script or other sql, there
        //are special characters that need to be escaped for the JSON data.
        //on the web service we are using the actual javascript unescape to unpack this.
        var val = packJSON($("#" + field).val());

        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmUpdateStep",
            data: '{"sStepID":"' + step_id + '","sFunction":"' + func + '","sXPath":"' + xpath + '","sValue":"' + val + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function(msg) {
                $("#task_steps").unblock();
                $("#update_success_msg").text("Update Successful").fadeOut(2000);
            },
            error: function(response) {
                $("#update_success_msg").fadeOut(2000);
                showAlert(response.responseText);
            }
        });


        //tried this... no marked effect on performance
        //ACWebMethods.taskMethods.wmUpdateStep(step_id, func, xpath, val, OnFieldUpdateSuccess, OnFieldUpdateFailure);


        //clear out the div of the stuff we just updated!
        $("#step_update_array").empty()
    }
}

//tried this... no marked effect on performance

//function OnFieldUpdateSuccess(result, userContext, methodName) {
//    //renumber the step widget labels
//    $("#steps .step_order_label").each(
//                function(intIndex) {
//                    $(this).html(intIndex + 1); //+1 because we are a 1 based array on the page
//                }
//            );

//    $("#task_steps").unblock();
//    $("#update_success_msg").text("Update Successful").fadeOut(2000);
//}

//// Callback function invoked on failure of the MS AJAX page method.
//function OnFieldUpdateFailure(error, userContext, methodName) {
//    $("#update_success_msg").fadeOut(2000);

//    if (error !== null) {
//        showAlert(error.get_message());
//    }
//}

function validateStep(in_element_id) {
    var container;

    //if we didn't get a specific step_id, run the test on ALL STEPS!
    if (in_element_id)
        container = "#" + in_element_id;
    else
        container = "#steps";

    //check required fields and alert
    var err_cnt = 0;

    $(container + " :input[is_required='true']").each(
        function(intIndex) {
            var step_id = $(this).attr("step_id");
            var msg = "Missing required value.";

            //clear syntax error list
            $("#info_" + step_id + " .info_syntax_errors").html("");

            //how many errors on this step?
            err_cnt = $(container + " :input[step_id='" + step_id + "']").filter(container + " :input[is_required='true']").filter(container + " :input[value='']").length;

            if ($(this).val() == "") {
                if ($("#info_" + step_id).length == 0) {
                    $("#" + step_id).prepend(drawStepValidationBar(step_id, err_cnt, ''));
                } else {
                    $("#info_" + step_id + " .info_error_count").text(err_cnt);
                }

                $("#info_" + step_id + " .info_syntax_errors").append("<div>* " + msg + "</div>");
                $(this).addClass("is_required");
            }
            else {
                $(this).removeClass("is_required");

                //only remove the popup if there are no remaining errors on the step
                if (err_cnt == 0) {
                    $("#info_" + step_id).remove();
                }
            }
        }
    );

    //check syntax errors and alert
    //if ($("#" + combined_id).length == 0) {
    $(container + " :input[syntax]").each(function(intIndex) {
        var step_id = $(this).attr("step_id");
        var syntax = $(this).attr("syntax");
        var field_value = $(this).val();

        var syntax_error;
        if (syntax != "") {
            syntax_error = checkSyntax(syntax, field_value);
        }

        if (syntax_error != "") {
            //increment the error counter
            err_cnt += 1;

            if ($("#info_" + step_id).length == 0) {
                $("#" + step_id).prepend(drawStepValidationBar(step_id, err_cnt, syntax_error));
            } else {
                $("#info_" + step_id + " .info_error_count").text(err_cnt);
            }

            $("#info_" + step_id + " .info_syntax_errors").append("<div>* " + syntax_error + "</div>");
            $(this).addClass("is_required");
        }
        else {
            $(this).removeClass("is_required");
        }
    });
    // }

}

function drawStepValidationBar(step_id, cnt, msg) {
    return "<div id=\"info_" + step_id.toString() + "\" class=\"step_validation_template\">" +
        "<img src=\"../images/icons/status_unknown_16.png\" alt=\"\" />" +
        "The following Command has <span class=\"info_error_count\">" + cnt.toString() +
        "</span> item(s) needing attention." +
        "<div class=\"info_syntax_errors\"></div>" +
        "</div>";
}