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
    $(".link").disableSelection();

    //refresh button
    $("#refresh_btn").click(function () {
        showPleaseWait();
        self.location.reload();
        hidePleaseWait();
    });

    //parent instance link
    $("#ctl00_phDetail_lblSubmittedByInstance").click(function () {
        showPleaseWait();
        self.location.href = "taskRunLog.aspx?task_instance=" + $("#ctl00_phDetail_hidSubmittedByInstance").val(); ;
        hidePleaseWait();
    });

    //"other" instances jump links
    $("[tag='selectable']").click(function () {
        self.location.href = "taskRunLog.aspx?task_instance=" + $(this).parent().attr("task_instance"); ;
    });

    initJtable();

    //show the abort button if applicable
    status = $("#ctl00_phDetail_lblStatus").text();
    if ("Submitted,Processing,Queued".indexOf(status) > -1)
        $("#abort_btn").removeClass("hidden");

    $("#abort_dialog").dialog({
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
            Cancel: function () {
                $(this).dialog('close');
            },
            OK: function () {
                doDebugStop();
                $(this).dialog('destroy');
            }
        }
    });



    //resubmit button
    //shows a dialog with options
    $("#resubmit_btn").click(function () { $("#resubmit_dialog").dialog("open"); });

    //show/hide content based on user preference
    $("#show_detail").click(function () {
        $("#full_details").toggleClass("hidden");
        $("#show_detail").toggleClass("vis_btn_off");

        if ($("#full_details").hasClass("hidden")) {
            $(".log").height($(".log").height() + 64);
        } else {
            $(".log").height($(".log").height() - 64);
        }
    });
    $("#show_cmd").click(function () {
        $(".log_command").toggleClass("hidden");
        $("#show_cmd").toggleClass("vis_btn_off");
    });
    $("#show_results").click(function () {
        $(".log_results").toggleClass("hidden");
        $("#show_results").toggleClass("vis_btn_off");
    });
    $("#show_logfile").click(function () {
        //pop a window with the log file
        var url = $("#ctl00_phDetail_hidCELogFile").val();
        openDialogWindow(url, 'TaskLogFile', 800, 700, 'true');
    });

    //repost and ask for the whole log
    $("#get_all").click(function () {
        if (confirm("This may take a long time.\n\nAre you sure?"))
            self.location.href = 'taskRunLog.aspx?task_instance=' + $("#ctl00_phDetail_hidInstanceID").val() + '&rows=all';
    });


    //enable/disable the 'clear log' button on the dialog
    $("#rb_new").click(function () { $("#clear_log").attr("disabled", "disabled"); });
    $("#rb_existing").click(function () { $("#clear_log").removeAttr("disabled"); });


    $("#resubmit_dialog").dialog({
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
            Cancel: function () {
                $(this).dialog('close');
            },
            OK: function () {
                $(this).dialog('close');

                var task_id = $("#ctl00_phDetail_hidTaskID").val();
                var task_name = $("#ctl00_phDetail_lblTaskName").html();
                var asset_id = $("#ctl00_phDetail_hidAssetID").val();
                var ecosystem_id = $("#ctl00_phDetail_hidEcosystemID").val();
                var account_id = $("#ctl00_phDetail_hidAccountID").val();
                var account_name = $("#ctl00_phDetail_lblAccountName").val();
                var instance = $("#ctl00_phDetail_hidInstanceID").val();

                var args = '{"task_id":"' + task_id + '", \
                    "task_name":"' + task_name + '", \
                    "account_id":"' + account_id + '", \
                    "account_name":"' + account_name + '", \
                    "ecosystem_id":"' + ecosystem_id + '", \
                    "instance":"' + instance + '", \
                    "asset_id":"' + asset_id + '"}';
                ShowTaskLaunchDialog(args);

            }
        }
    });
    hidePleaseWait();

    //if there's no value in the CELogfile ... hide the button.
    if ($("#ctl00_phDetail_hidCELogFile").val() == "")
        $("#show_logfile").hide();

});

function doDebugStop() {

    instance = $("#ctl00_phDetail_hidInstanceID").val();


    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmStopTask",
        data: '{"sInstance":"' + instance + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {

            location.reload();

        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });


}

function pageLoad() {
    initJtable();
}