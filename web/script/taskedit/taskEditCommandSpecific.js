/*
There are some script that applies to instances of Steps on a Task.

Here is all the code relevant to Steps and their dynamic nature.
*/

//This link can appears anywhere it is needed.
//it clears a field and enables it for data entry.
$(document).ready(function () {
    $(".fn_field_clear_btn").live("click", function () {
        var field_id_to_clear = $(this).attr("clear_id");

        //clear it
        $("#" + field_id_to_clear).val("");

        //in case it's disabled, enable it
        $("#" + field_id_to_clear).removeAttr('disabled');

        //push an change event to it.
        $("#" + field_id_to_clear).change();
    });
});

//the SUBTASK command
//this will get the parameters in read only format for each subtask command.
$(document).ready(function () {
    $("#steps .subtask_view_parameters").live("click",
    function () {
        var task_id = $(this).attr("id").replace(/stvp_/, "");
        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmGetParameters",
            data: '{"sType":"task","sID":"' + task_id + '","bEditable":"false","bSnipValues":"true"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                $("#stvp_" + task_id).html(retval.d);
                $("#stvp_" + task_id).removeClass("pointer");

                //have to rebind the tooltips here
                bindParameterToolTips();
            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    });
});

//end SUBTASK command

//the CODEBLOCK command
//the onclick event of the 'codeblock' elements
$(document).ready(function () {
    $("#steps .codeblock_goto_btn").live("click", function () {
        cb = $(this).attr("codeblock");
        $("#ctl00_phDetail_lblStepSectionTitle").text(cb);
        $("#ctl00_phDetail_hidCodeblockName").val(cb);
        $("#ctl00_phDetail_btnStepLoad").click();
    });
});

//end CODEBLOCK command

//the SUBTASK and RUN TASK commands
//the view link
$(document).ready(function () {
    $("#steps .task_print_btn").live("click", function () {
        var url = "taskPrint.aspx?task_id=" + $(this).attr("task_id");
        openWindow(url, "taskPrint", "location=no,status=no,scrollbars=yes,resizable=yes,width=800,height=700");
    });
    //the edit link
    $("#steps .task_open_btn").live("click", function () {
        location.href = "taskEdit.aspx?task_id=" + $(this).attr("task_id");
    });
});
// end SUBTASK and RUN TASK commands

//the IF command
$(document).ready(function () {
    $("#steps .fn_if_remove_btn").live("click", function () {
        var step_id = $(this).attr("step_id");
        var idx = $(this).attr("number");

        doRemoveIfSection(step_id, idx)
    });

    $("#steps .fn_if_removeelse_btn").live("click", function () {
        var step_id = $(this).attr("step_id");
        doRemoveIfSection(step_id, -1)
    });

    $("#steps .fn_if_add_btn").live("click", function () {
        var step_id = $(this).attr("step_id");
        var idx = $(this).attr("next_index");

        doAddIfSection(step_id, idx);
    });

    $("#steps .fn_if_addelse_btn").live("click", function () {
        var step_id = $(this).attr("step_id");
        doAddIfSection(step_id, -1);
    });
    $("#steps .compare_templates").live("change", function () {
        // add whatever was selected into the textarea
        var textarea_id = $(this).attr("textarea_id");
        var tVal = $("#" + textarea_id).val();
        $("#" + textarea_id).val(tVal + this.value);

        // clear the selection
        this.selectedIndex = 0;
    });
});
function doAddIfSection(step_id, idx) {
    $("#task_steps").block({ message: null });
    $("#update_success_msg").text("Updating...").show();

    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmFnIfAddSection",
        data: '{"sStepID":"' + step_id + '","iIndex":"' + idx + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (retval) {
            //go get the step
            getStep(step_id, step_id, true);
            $("#task_steps").unblock();
            $("#update_success_msg").text("Update Successful").fadeOut(2000);

            //hardcoded index for the last "else" section
            if (idx == -1)
                doDropZoneEnable($("#if_" + step_id + "_else .step_nested_drop_target"));
            else
                doDropZoneEnable($("#if_" + step_id + "_else_" + idx + " .step_nested_drop_target"));

        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}
function doRemoveIfSection(step_id, idx) {
    if (confirm("Are you sure?")) {
        $("#task_steps").block({ message: null });
        $("#update_success_msg").text("Updating...").show();

        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmFnIfRemoveSection",
            data: '{"sStepID":"' + step_id + '","iIndex":"' + idx + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                getStep(step_id, step_id, true);
                $("#task_steps").unblock();
                $("#update_success_msg").text("Update Successful").fadeOut(2000);
            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    }

}
//end IF command


//FUNCTIONS for adding/deleting key/value pairs on a step.
$(document).ready(function () {
    $("#steps .fn_pair_remove_btn").live("click", function () {
        if (confirm("Are you sure?")) {
            var step_id = $(this).attr("step_id");
            var idx = $(this).attr("index");

            $("#task_steps").block({ message: null });
            $("#update_success_msg").text("Updating...").show();

            $.ajax({
                async: false,
                type: "POST",
                url: "taskMethods.asmx/wmFnRemovePair",
                data: '{"sStepID":"' + step_id + '","iIndex":"' + idx + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (retval) {
                    getStep(step_id, step_id, true);
                    $("#task_steps").unblock();
                    $("#update_success_msg").text("Update Successful").fadeOut(2000);
                },
                error: function (response) {
                    showAlert(response.responseText);
                }
            });
        }
    });

    $("#steps .fn_pair_add_btn").live("click", function () {
        var step_id = $(this).attr("step_id");

        $("#task_steps").block({ message: null });
        $("#update_success_msg").text("Updating...").show();

        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmFnAddPair",
            data: '{"sStepID":"' + step_id + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                //go get the step
                getStep(step_id, step_id, true);
                $("#task_steps").unblock();
                $("#update_success_msg").text("Update Successful").fadeOut(2000);

            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    });

    //BUT! Key/Value pairs can appear on lots of different command types
    //and on each type there is a specific list of relevant lookup values
    //So, switch on the function and popup a picker
    //the onclick event of 'connection picker' buttons on selected fields
    $("#steps .key_picker_btn").live("click", function (e) {
        //hide any open pickers
        $("div[id$='_picker']").hide();
        var field = $("#" + $(this).attr("link_to"));
        var func = $(this).attr("function");

        var item_html = "N/A";

        //first, populate the picker
        //(in a minute build this based on the 'function' attr of the picker icon
        switch (func) {
            case "dataset":
                item_html = "<div class=\"ui-widget-content ui-corner-all value_picker_value\">pass_fail</div>";
                break;
            case "http":
                break;
            case "run_task":
                break;
        }

        $("#key_picker_keys").html(item_html);

        //set the hover effect
        $("#key_picker_keys .value_picker_value").hover(
            function () {
                $(this).toggleClass("value_picker_value_hover");
            },
            function () {
                $(this).toggleClass("value_picker_value_hover");
            }
        );

        //click event
        $("#key_picker_keys .value_picker_value").click(function () {
            field.val($(this).text());
            field.change();

            $("#key_picker").slideUp();
        });

        $("#key_picker").css({ top: e.clientY, left: e.clientX });
        $("#key_picker").slideDown();
    });
    $("#key_picker_close_btn").live("click", function (e) {
        //hide any open pickers
        $("div[id$='_picker']").hide();
    });

    //onclick event for the select template button in dataset
    $("#steps .template_picker_btn").live("click", function (e) {
        // hide any open pickers
        $("div[id$='_picker']").hide();

        var step_id = $(this).attr("step_id")

        // load the templates div
        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmGetDatasetTemplates",
            data: '{"sStepID":"' + step_id + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {

                //put the results in the dialog
                $("#template_picker_templates").html(retval.d);

            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });

        //set the hover effect
        $("#template_picker_templates .value_picker_value").hover(
                function () {
                    $(this).toggleClass("value_picker_value_hover");
                },
                function () {
                    $(this).toggleClass("value_picker_value_hover");
                }
            );

        //click event
        $("#template_picker_templates .value_picker_value").click(function () {
            var template_id = $(this).attr("template_id")

            $("#dataset_template_id").val(template_id);
            $("#dataset_step_id").val(step_id);
            $("#dataset_template_change_confirm").dialog('open');
            $("#dataset_template_picker").slideUp();

        });

        $("#dataset_template_picker").css({ top: e.clientY, left: e.clientX });
        $("#dataset_template_picker").slideDown();

    });
    $("#template_picker_close_btn").live("click", function (e) {
        //hide any open pickers
        $("div[id$='_picker']").hide();
    });

    //init the dataset template change confirm dialog
    $("#dataset_template_change_confirm").dialog({
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
            'Apply': function () {
                doDatasetStepTemplateChange();
                $(this).dialog('close');
            },
            Cancel: function () {
                $(this).dialog('close');
            }
        }
    });


});

function doDatasetStepTemplateChange() {
    var dataset_template_id = $("#dataset_template_id").val();
    var step_id = $("#dataset_step_id").val();

    //    //don't need to block, the dialog blocks.
    $("#update_success_msg").text("Updating...").show();
    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmDatasetTemplateChange",
        data: '{"sStepID":"' + step_id + '","sTemplateID":"' + dataset_template_id + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {

            //reget the step
            getStep(step_id, step_id, true);
            $("#update_success_msg").text("Update Successful").fadeOut(2000);

        },
        error: function (response) {
            $("#update_success_msg").fadeOut(2000);
            showAlert(response.responseText);
        }
    });

    $("#dataset_template_id").val("");
    $("#dataset_step_id").val("");

}
//end adding/deleting key/value pairs on a step.

//FUNCTIONS for adding/deleting variables on the set_variable AND clear_variable commands.
$(document).ready(function () {
    $("#steps .fn_var_remove_btn").live("click", function () {
        if (confirm("Are you sure?")) {
            var step_id = $(this).attr("step_id");
            var idx = $(this).attr("index");

            $("#task_steps").block({ message: null });
            $("#update_success_msg").text("Updating...").show();

            $.ajax({
                async: false,
                type: "POST",
                url: "taskMethods.asmx/wmFnVarRemoveVar",
                data: '{"sStepID":"' + step_id + '","iIndex":"' + idx + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (retval) {
                    getStep(step_id, step_id, true);
                    $("#task_steps").unblock();
                    $("#update_success_msg").text("Update Successful").fadeOut(2000);
                },
                error: function (response) {
                    showAlert(response.responseText);
                }
            });
        }
    });

    $("#steps .fn_setvar_add_btn").live("click", function () {
        var step_id = $(this).attr("step_id");

        $("#task_steps").block({ message: null });
        $("#update_success_msg").text("Updating...").show();

        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmFnSetvarAddVar",
            data: '{"sStepID":"' + step_id + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                //go get the step
                getStep(step_id, step_id, true);
                $("#task_steps").unblock();
                $("#update_success_msg").text("Update Successful").fadeOut(2000);

            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    });

    $("#steps .fn_clearvar_add_btn").live("click", function () {
        var step_id = $(this).attr("step_id");

        $("#task_steps").block({ message: null });
        $("#update_success_msg").text("Updating...").show();

        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmFnClearvarAddVar",
            data: '{"sStepID":"' + step_id + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                //go get the step
                getStep(step_id, step_id, true);
                $("#task_steps").unblock();
                $("#update_success_msg").text("Update Successful").fadeOut(2000);

            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    });

});

//end adding/deleting variables on set_variable AND clear_variable.

//FUNCTIONS for adding variables on the exists commands.
$(document).ready(function () {
    $("#steps .fn_exists_add_btn").live("click", function () {
        var step_id = $(this).attr("step_id");

        $("#task_steps").block({ message: null });
        $("#update_success_msg").text("Updating...").show();

        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmFnExistsAddVar",
            data: '{"sStepID":"' + step_id + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                //go get the step
                getStep(step_id, step_id, true);
                $("#task_steps").unblock();
                $("#update_success_msg").text("Update Successful").fadeOut(2000);

            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    });

});

//end adding/deleting variables on set_variable AND clear_variable.

// wmi section
$(document).ready(function () {
    //the onclick event of 'namespace picker' buttons on selected fields
    $("#steps .namespace_picker_btn").live("click", function (e) {
        //hide any open pickers
        $("div[id$='_picker']").hide();
        var field = $("#" + $(this).attr("link_to"));

        //first, populate the picker
        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmGetWmiNamespaces",
            data: '{}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                $("#namespace_picker_namespaces").html(retval.d);
                $("#namespace_picker_namespaces .value_picker_value").hover(
            function () {
                $(this).toggleClass("value_picker_value_hover");
            },
                function () {
                    $(this).toggleClass("value_picker_value_hover");
                }
            );
                $("#namespace_picker_namespaces .value_picker_value").click(function () {

                    field.val($(this).text());
                    field.change();
                    $("#namespace_picker").slideUp();

                });
            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });


        $("#namespace_picker").css({ top: e.clientY, left: e.clientX });
        $("#namespace_picker").slideDown();


    });
});
// end wmi section

//FUNCTIONS for adding and removing handles from the Wait For Task command
$(document).ready(function () {
    $("#steps .fn_wft_add_btn").live("click", function () {
        var step_id = $(this).attr("step_id");

        $("#task_steps").block({ message: null });
        $("#update_success_msg").text("Updating...").show();

        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmFnWaitForTasksAddHandle",
            data: '{"sStepID":"' + step_id + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                //go get the step
                getStep(step_id, step_id, true);
                $("#task_steps").unblock();
                $("#update_success_msg").text("Update Successful").fadeOut(2000);

            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    });

    $("#steps .fn_handle_remove_btn").live("click", function () {
        if (confirm("Are you sure?")) {
            var step_id = $(this).attr("step_id");
            var idx = $(this).attr("index");

            $("#task_steps").block({ message: null });
            $("#update_success_msg").text("Updating...").show();

            $.ajax({
                async: false,
                type: "POST",
                url: "taskMethods.asmx/wmFnWaitForTasksRemoveHandle",
                data: '{"sStepID":"' + step_id + '","iIndex":"' + idx + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (retval) {
                    getStep(step_id, step_id, true);
                    $("#task_steps").unblock();
                    $("#update_success_msg").text("Update Successful").fadeOut(2000);
                },
                error: function (response) {
                    showAlert(response.responseText);
                }
            });
        }
    });
});

//End Wait For Task functions


//FUNCTIONS for adding and removing xml "node arrays" from any step that might have one.
$(document).ready(function () {
    $("#steps .fn_nodearray_add_btn").live("click", function () {
        var step_id = $(this).attr("step_id");
        var xpath = $(this).attr("xpath");

        $("#task_steps").block({ message: null });
        $("#update_success_msg").text("Updating...").show();

        $.ajax({
            async: false,
            type: "POST",
            url: "taskMethods.asmx/wmFnNodeArrayAdd",
            data: '{"sStepID":"' + step_id + '","sGroupNode":"' + xpath + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                //go get the step
                getStep(step_id, step_id, true);
                $("#task_steps").unblock();
                $("#update_success_msg").text("Update Successful").fadeOut(2000);

            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    });

    $("#steps .fn_nodearray_remove_btn").live("click", function () {
        if (confirm("Are you sure?")) {
            var step_id = $(this).attr("step_id");
            var xpath_to_delete = $(this).attr("xpath_to_delete");

            $("#task_steps").block({ message: null });
            $("#update_success_msg").text("Updating...").show();

            $.ajax({
                async: false,
                type: "POST",
                url: "taskMethods.asmx/wmFnNodeArrayRemove",
                data: '{"sStepID":"' + step_id + '","sXPathToDelete":"' + xpath_to_delete + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (retval) {
                    getStep(step_id, step_id, true);
                    $("#task_steps").unblock();
                    $("#update_success_msg").text("Update Successful").fadeOut(2000);
                },
                error: function (response) {
                    showAlert(response.responseText);
                }
            });
        }
    });
});

//End XML Node Array functions