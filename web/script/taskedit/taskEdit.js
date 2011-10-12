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

//This is all the functions to support the taskEdit page.
$(document).ready(function () {
    //used a lot
    g_task_id = $("[id$='hidTaskID']").val();

    // Round the corners of all the pill buttons in the task edit panel
    //    $('.command_item').corner("round 4px");
    //    $('.version').corner("round 4px");
    //    $('.codeblock').corner("round 4px");

    //fix certain ui elements to not have selectable text
    $("#toolbox .toolbox_tab").disableSelection();
    $("#toolbox .codeblock").disableSelection();
    $("#toolbox .category").disableSelection();
    $("#toolbox .function").disableSelection();
    $(".step_common_button").disableSelection();
    $("#toolbox .attribute_picker_attribute").disableSelection();

    //specific field validation and masking
    $("#ctl00_phDetail_txtTaskCode").keypress(function (e) { return restrictEntryCustom(e, this, /[a-zA-Z0-9 _\-]/); });
    $("#ctl00_phDetail_txtTaskName").keypress(function (e) { return restrictEntryToSafeHTML(e, this); });
    $("#ctl00_phDetail_txtTaskDesc").keypress(function (e) { return restrictEntryToSafeHTML(e, this); });
    $("#ctl00_phDetail_txtConcurrentInstances").keypress(function (e) { return restrictEntryToPositiveInteger(e, this); });
    $("#ctl00_phDetail_txtQueueDepth").keypress(function (e) { return restrictEntryToPositiveInteger(e, this); });

    //enabling the 'change' event for the Details tab
    $("#div_details :input[te_group='detail_fields']").change(function () { doDetailFieldUpdate(this); });

    //jquery buttons
    $("#task_search_btn").button({ icons: { primary: "ui-icon-search"} });

    //the 'Approve' button
    $("#approve_dialog").dialog({
        autoOpen: false,
        draggable: false,
        resizable: false,
        bgiframe: true,
        modal: true,
        width: 400,
        overlay: {
            backgroundColor: '#000',
            opacity: 0.5
        },
        buttons: {
            'Approve': function () {
                $.blockUI({ message: null });

                var $chk = $("#ctl00_phDetail_chkMakeDefault");
                var make_default = 0;

                if ($chk.is(':checked'))
                    make_default = 1;

                $.ajax({
                    async: false,
                    type: "POST",
                    url: "taskMethods.asmx/wmApproveTask",
                    data: '{"sTaskID":"' + g_task_id + '","sMakeDefault":"' + make_default + '"}',
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (msg) {
                        //now redirect (using replace so they can't go "back" to the editable version)
                        location.replace("taskView.aspx?task_id=" + g_task_id);
                    },
                    error: function (response) {
                        $("#update_success_msg").fadeOut(2000);
                        showAlert(response.responseText);
                    }
                });
            },
            Cancel: function () {
                $(this).dialog('close');
            }
        }
    });

    $("#approve_btn").click(function () {
        $("#approve_dialog").dialog('open');
    });

    //make the clipboard clear button
    $("#clear_clipboard_btn").button({ icons: { primary: "ui-icon-close"} });

    //clear the whole clipboard
    $("#clear_clipboard_btn").click(function () {
        doClearClipboard("ALL");
    });

    //clear just one clip
    $("#btn_clear_clip").live("click", function () {
        doClearClipboard($(this).attr("remove_id"));
    });

    //the clip view dialog
    $("#clip_dialog").dialog({
        autoOpen: false,
        modal: true,
        width: 800
    });
    //pop up the clip to view it
    $("#btn_view_clip").live("click", function () {
        var clip_id = $(this).attr("view_id");

        var html = $("#" + clip_id).html();

        $("#clip_dialog_clip").html(html);
        $("#clip_dialog").dialog('open');
    });


    //big edit box dialog
    //init the big box dialog
    $("#big_box_dialog").dialog({
        autoOpen: false,
        modal: true,
        width: 800,
        buttons: {
            OK: function () {
                var ctl = $("#big_box_link").val();
                $("#" + ctl).val($("#big_box_text").val());

                //do something to fire the blur event so it will update
                $("#" + ctl).change();

                $(this).dialog('close');
            },
            Cancel: function () {
                $(this).dialog('close');
            }
        }
    });
    $(".big_box_btn").live("click", function () {
        var ctl = $(this).attr("link_to");

        $("#big_box_link").val(ctl);
        $("#big_box_text").val($("#" + ctl).val());

        $("#big_box_dialog").dialog('open');
    });

    // tab handling for the big_box_text textarea
    $("#big_box_text").tabby();

    //activating the dropzone for nested steps
    $("#steps .step_nested_drop_target").live("click", function () {
        doDropZoneEnable($(this));
    });

    // unblock when ajax activity stops 
    //$().ajaxStop($.unblockUI);


    //set the help text on hover over a category
    $("#toolbox .category").hover(function () {
        $("#te_help_box_detail").html($("#help_text_" + $(this).attr("name")).html());
    }, function () {
        $("#te_help_box_detail").html("");
    });

    //toggle categories
    $("#toolbox .category").click(function () {
        //unselect all the categories
        $("#toolbox .category").removeClass("category_selected");

        //and select this one you clicked
        //alert($(this).attr("id"));
        $(this).addClass("category_selected");

        //hide 'em all
        $("#toolbox .functions").addClass("hidden");

        //show the one you clicked
        $("#" + $(this).attr("id") + "_functions").removeClass("hidden");
    });

    //finally, init the draggable items (commands and the clipboard)
    //this will also be called when items are added/removed from the clipboard.
    initDraggable();

});

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
            url: "taskMethods.asmx/wmUpdateTaskDetail",
            data: '{"sTaskID":"' + g_task_id + '","sColumn":"' + column + '","sValue":"' + value + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {

                if (msg.d != '') {
                    $("#update_success_msg").text("Update Failed").fadeOut(2000);
                    showInfo(msg.d);
                }
                else {
                    $("#update_success_msg").text("Update Successful").fadeOut(2000);

                    // bugzilla 1037 Change the name in the header
                    if (column == "task_name") { $("#ctl00_phDetail_lblTaskNameHeader").html(unpackJSON(value)); };
                }


            },
            error: function (response) {
                $("#update_success_msg").fadeOut(2000);
                showAlert(response.responseText);
            }
        });
    }
}

function doEmbeddedStepAdd(func, droptarget) {
    $("#task_steps").block({ message: null });
    $("#update_success_msg").text("Adding...").show();

    var item = func.attr("id");
    var drop_step_id = $("#" + droptarget).attr("step_id");

    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmAddStep",
        data: '{"sTaskID":"' + g_task_id + '","sCodeblockName":"' + drop_step_id + '","sItem":"' + item + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (retval) {
            var new_step_id = retval.d.substring(0, 36);

            //got it.  now, update the specific field that is receiving this step
            field = $("#" + droptarget).attr("datafield_id");
            $("#" + field).val(new_step_id);
            $("#" + field).change();

            //just get the internal content
            getStep(new_step_id, droptarget, false);
            $("#update_success_msg").text("Add Successful").fadeOut(2000);

            //NOTE NOTE NOTE: this is temporary
            //until I fix the copy command ... we will delete the clipboard item after pasting
            //this is not permanent, but allows it to be checked in now
            if (item.indexOf('clip_') != -1)
                doClearClipboard(item.replace(/clip_/, ""))

            //you have to add the embedded command NOW, or click cancel.
            if (item == "fn_if" || item == "fn_loop" || item == "fn_exists") {
                doDropZoneEnable($("#" + new_step_id + " .step_nested_drop_target"));
            }

        },
        error: function (response) {
            $("#update_success_msg").fadeOut(2000);
            showAlert(response.responseText);
        }
    });
}

function doEmbeddedStepDelete() {
    var remove_step_id = $("#embedded_step_remove_id").val();
    var parent_id = $("#embedded_step_parent_id").val();

    //    //don't need to block, the dialog blocks.
    $("#update_success_msg").text("Updating...").show();
    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmDeleteStep",
        data: '{"sStepID":"' + remove_step_id + '","sParentID":"' + parent_id + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            //crazy, but this page is so dynamic, on the server there is no way to identify the hidden field
            //that is holding the value for this embedded command.
            //but, thanks to jQuery... you can select a field based on it's value!
            //since GUID's are unique,
            //get the field holding the step_id of this child... that will be the text field that triggers the update!
            //and the change event will fire the regular update event that clears the XML for that specific value!
            //THANK YOU AND GOOD NIGHT!
            var field = $("#steps :input[value='" + remove_step_id + "']").filter("#steps :input[te_group='step_fields']");
            field.val("");
            field.change();

            //reget the parent step
            getStep(parent_id, parent_id, true);
            $("#update_success_msg").text("Update Successful").fadeOut(2000);
        },
        error: function (response) {
            $("#update_success_msg").fadeOut(2000);
            showAlert(response.responseText);
        }
    });

    $("#embedded_step_remove_id").val("");
    $("#embedded_step_parent_id").val("");
}

function getStep(step_id, target, init) {
    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmGetStep",
        data: '{"sStepID":"' + step_id + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (retval) {
            $("#" + target).replaceWith(retval.d);

            if (init)
                initSortable();

            validateStep(step_id);

            $("#task_steps").unblock();
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}

// Callback function invoked on successful completion of the MS AJAX page method.
function OnSuccess(result, userContext, methodName) {
    $("#update_success_msg").text("Update Successful").fadeOut(2000);
}

// Callback function invoked on failure of the MS AJAX page method.
function OnFailure(error, userContext, methodName) {
    if (error !== null) {
        showAlert(error.get_message());
    }
}

function doDropZoneEnable($ctl) {
    $ctl.html("Drag a command from the toolbox and drop it here now, or <span class=\"step_nested_drop_target_cancel_btn\" onclick=\"doDropZoneDisable('" + $ctl.attr("id") + "');\">click " +
        "here to cancel</span> add add it later.");
    //
    //$("#" + $(this).attr("id") + " > span").removeClass("hidden");
    $ctl.addClass("step_nested_drop_target_active");

    //gotta destroy the sortable to receive drops in the action area
    //we'll reenable it after we process the drop
    $("#steps").sortable("destroy");

    $ctl.everyTime(2000, function () {
        $ctl.animate({
            backgroundColor: "#ffbbbb"
        }, 999).animate({
            backgroundColor: "#ffeeee"
        }, 999)
    });


    $ctl.droppable({
        accept: ".function",
        hoverClass: "step_nested_drop_target_hover",
        drop: function (event, ui) {
            //add the new step
            var new_step = $(ui.draggable[0]);
            var func = new_step.attr("id");

            if (func.indexOf("fn_") == 0 || func.indexOf("clip_") == 0) {
                doEmbeddedStepAdd(new_step, $ctl.attr("id"));
            }

            $ctl.removeClass("step_nested_drop_target_active");
            $ctl.droppable("destroy");

            //DO NOT init the sortable if the command you just dropped has an embedded command
            //at this time it's IF and LOOP and EXISTS
            if (func != "fn_if" && func != "fn_loop" && func != "fn_exists")
                initSortable();
        }
    });
}
function doDropZoneDisable(id) {
    $("#" + id).stopTime();
    $("#" + id).css("background-color", "#ffeeee");
    $("#" + id).html("Click here to add a command.");
    $("#" + id).removeClass("step_nested_drop_target_active");
    $("#" + id).droppable("destroy");
    initSortable();
}


// Callback function invoked on failure of the MS AJAX page method.
function OnUpdateFailure(error, userContext, methodName) {
    //alert('failure');
    if (error !== null) {
        showAlert(error.get_message());
    }
}

function doClearClipboard(id) {
    $("#update_success_msg").text("Removing...").show();
    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmRemoveFromClipboard",
        data: '{"sStepID":"' + id + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            doGetClips();
            $("#update_success_msg").text("Remove Successful").fadeOut(2000);
        },
        error: function (response) {
            $("#update_success_msg").fadeOut(2000);
            showAlert(response.responseText);
        }
    });
}
function doGetClips() {
    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmGetClips",
        data: '{}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (retval) {
            $("#clipboard").html(retval.d);
            initDraggable();
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}
function initDraggable() {
    //initialize the 'commands' tab and the clipboard tab to be draggable to the step list
    $("#toolbox .function").draggable("destroy");
    $("#toolbox .function").draggable({
        distance: 30,
        connectToSortable: '#steps',
        appendTo: 'body',
        revert: 'invalid',
        scroll: false,
        opacity: 0.95,
        helper: 'clone',
        start: function (event, ui) {
            $("#dd_dragging").val("true");
        },
        stop: function (event, ui) {
            $("#dd_dragging").val("false");
        }
    })


    //unbind it first so they don't stack (since hover doesn't support "live")
    $("#toolbox .function").unbind("mouseenter mouseleave");
    $("#toolbox .command_item").unbind("mouseenter mouseleave");

    //set the help text on hover over a function
    $("#toolbox .function").hover(function () {
        $("#te_help_box_detail").html($("#help_text_" + $(this).attr("name")).html());
    }, function () {
        $("#te_help_box_detail").html("");
    });

    $("#toolbox .command_item").hover(function () {
        $(this).addClass("command_item_hover");
    }, function () {
        $(this).removeClass("command_item_hover");
    });
}