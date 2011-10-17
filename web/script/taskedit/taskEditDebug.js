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

$(document).ready(function () {

    //init the asset picker dialog
    $("#asset_picker_dialog").dialog({
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
        close: function (event, ui) {
            $("#asset_search_text").val("");
            $("#asset_picker_results").empty();
        }
    });

    //    //the visual effect for the 'debug' buttons, set on page load
    //    if ($("[id$='lblCurrentStatus']").text() == "Aborting") {
    //        $("#debug_stop_btn").addClass("debug_btn_dim")
    //        $("#debug_run_btn").addClass("debug_btn_dim")
    //    } else if ($("[id$='lblCurrentStatus']").text() == "Inactive") {
    //        $("#debug_stop_btn").addClass("debug_btn_dim")
    //        $("#debug_run_btn").removeClass("debug_btn_dim")
    //    } else {
    //        $("#debug_run_btn").addClass("debug_btn_dim")
    //        $("#debug_stop_btn").removeClass("debug_btn_dim")
    //    }

    //temporary
    $("#debug_run_btn").removeClass("debug_btn_dim")
    $("#debug_stop_btn").removeClass("debug_btn_dim")

    //the click events for the 'debug' buttons
    $("#debug_run_btn").click(function () {
        //don't start it unless it's 'inactive'
        //if ($("[id$='lblCurrentStatus']").text() == "Inactive") {

        var task_id = $("#ctl00_phDetail_hidTaskID").val();
        var task_name = $("#ctl00_phDetail_lblTaskNameHeader").html();
        var asset_id = $("#ctl00_phDetail_txtTestAsset").attr("asset_id");

        var args = '{"task_id":"' + task_id + '", "task_name":"' + task_name + '", "debug_level":"4"}';
            
        //note: we are not passing the account_id - the dialog will use the default
        ShowTaskLaunchDialog(args);
    });
    $("#debug_stop_btn").click(function () {
        doDebugStop();
    });

    //bind the debug show log button
    $("#ctl00_phDetail_debug_view_latest_log").click(function () {
        openDialogWindow('taskRunLog.aspx?task_id=' + $("#ctl00_phDetail_hidTaskID").val(), 'TaskRunLog', 950, 750, 'true');
    });
    //bind the debug show active log button
    $("#debug_view_active_log").click(function () {
        var aid = $("#ctl00_phDetail_hidDebugActiveInstance").val();
        if (aid != "") {
            openDialogWindow('taskRunLog.aspx?task_instance=' + aid, 'TaskRunLog', 950, 750, 'true');
        }
    });


    $("#debug_asset_clear_btn").click(function () {
        $("#ctl00_phDetail_txtTestAsset").val("");
        $("#ctl00_phDetail_txtTestAsset").attr("asset_id", "");
        doSaveDebugAsset();
    });


    //////ASSET PICKER
    //the onclick event of the 'asset picker' buttons everywhere
    $(".asset_picker_btn").live("click", function () {
        $("#asset_picker_target_field").val($(this).attr("link_to"));
        $("#asset_picker_target_name_field").val($(this).attr("target_field_id"));
        $("#asset_picker_dialog").dialog('open');
        $("#asset_search_text").focus();
    });
    // when you hit enter inside an asset search
    $("#asset_search_text").live("keypress", function (e) {
        //alert('keypress');
        if (e.which == 13) {
            $("#asset_search_btn").click();
            return false;
        }
    });
    //the onclick event of the 'asset picker' search button
    $("#asset_search_btn").click(function () {
        var field = $("#" + $("#asset_picker_target_field").val());

        $("#asset_picker_dialog").block({ message: null, cursor: 'wait' });
        var search_text = $("#asset_search_text").val();
        $.ajax({
            async: false,
            type: "POST",
            url: "uiMethods.asmx/wmAssetSearch",
            data: '{"sSearchText":"' + search_text + '","bAllowMultiSelect":"false"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                $("#asset_picker_results").html(retval.d);
                //bind the onclick event for the new results
                $("#asset_picker_results .asset_picker_value").disableSelection();
                $("#asset_picker_dialog").unblock();
                $("#asset_picker_results li[tag='asset_picker_row']").click(function () {


                    if ($("#asset_picker_target_field").val() == "ctl00_phDetail_txtTestAsset") {
                        field.val($(this).attr("asset_name"))
                        //this is the picker for the Debug tab
                        field.attr("asset_id", $(this).attr("asset_id"));
                        doSaveDebugAsset();

                    } else {
                        // this is a picker somewhere on a step
                        field.val($(this).attr("asset_id"))
                        // we need an id hidden, and a name displayed
                        var namefield = $("#" + $("#asset_picker_target_name_field").val());
                        namefield.val($(this).attr("asset_name"))

                        //disable the "name" field...
                        namefield.attr('disabled', 'disabled');

                        //turn off any syntax checking on the "name" field... and reset the style
                        namefield.removeAttr('syntax');
                        namefield.removeClass('is_required');

                        field.change();
                    }

                    $("#asset_picker_dialog").dialog('close');
                    $("#asset_picker_results").empty();
                });

                //if we set up the table, commit the change to the database here.
            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    });
    //////END ASSET PICKER
});


function doDebugStop() {
    //don't stop it unless it's running
    //current_status = $("[id$='lblCurrentStatus']").text();

    //if (current_status != "Inactive" && current_status != "Aborting") {
    $("#update_success_msg").text("Stopping...").fadeIn(200);

    var $instance = $("#ctl00_phDetail_hidDebugActiveInstance");

    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmStopTask",
        data: '{"sInstance":"' + $instance.val() + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $instance.val("");

            //                //after stopping, set the visualness
            //                $("#debug_run_btn").removeClass("debug_btn_dim")
            //                $("#debug_stop_btn").addClass("debug_btn_dim")
            //                $("[id$='lblCurrentStatus']").text("Inactive")

            //$("#task_steps").unblock();
            $("#update_success_msg").text("Stop Successful").fadeOut(2000);
        },
        error: function (response) {
            $("#update_success_msg").fadeOut(2000);
            showAlert(response.responseText);
        }
    });
    //}
}
function doSaveDebugAsset() {
    var task_id = $("#ctl00_phDetail_hidTaskID").val();
    var asset_id = $("#ctl00_phDetail_txtTestAsset").attr("asset_id");
    $("#update_success_msg").text("Updating...").show();

    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmSaveTestAsset",
        data: '{"sTaskID":"' + task_id + '","sAssetID":"' + asset_id + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $("#update_success_msg").text("Update Successful").fadeOut(2000);
        },
        error: function (response) {
            $("#update_success_msg").fadeOut(2000);
            showAlert(response.responseText);
        }
    });
}
