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

$(document).ready(function() {
    $("#addVersion_dialog").dialog({
        autoOpen: false,
        modal: true,
        width: 600
    });
});

function ShowVersionAdd() {
    $("#hidMode").val("add");
    $('#addVersion_dialog').dialog('option', 'title', 'Create a New Version');

    $("#addVersion_dialog").dialog('open');
}

function CreateNewVersion() {
    $.blockUI({ message: "Creating new version..." });

    $.ajax({
        type: "POST",
        url: "taskMethods.asmx/wmCreateNewTaskVersion",
        data: '{"sTaskID":"' + $("#ctl00_phDetail_hidTaskID").val() + '","sMinorMajor":"' + $("input[name='rbMinorMajor']:checked").val() + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(msg) {
            if (msg.d.length == 36) {
                location.href = "taskEdit.aspx?task_id=" + msg.d;
            }
            else {
                showAlert(msg.d);
            }
        },
        error: function(response) {
            showAlert(response.responseText);
        }
    });

    $("#addVersion_dialog").dialog('close');

    return false;
}

function CloseVersionDialog() {
    $("#addVersion_dialog").dialog('close');

    return false;
}

function doGetVersions() {
    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmGetTaskVersions",
        data: '{"sTaskID":"' + $("#ctl00_phDetail_hidTaskID").val() + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (retval) {
            $("#versions").html(retval.d);
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}
