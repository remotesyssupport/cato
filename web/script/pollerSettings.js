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

});
function pageLoad() {
    $('#tblList tr').addClass('row');
    $('#tblList tr:even').addClass('row_alt');


    //what happens when you click a row
    $("[tag='selectable']").click(function() {
        showPleaseWait();
        //pop up the edit dialog
    });
}


function ValidateValues() {
    // both the poller seconds and max processes must be greater than 0
    if ($("#ctl00_phDetail_txtPollerMaxProcesses").val() == '0') {
        showAlert('The Concurrent Instances must be greater than 0.');
        return false;
    }
    if ($("#ctl00_phDetail_txtLoopDelay").val() == '0') {
        showAlert('The Loop Delay must be greater than 0.');
        return false;
    }
    return true;
}

function setAllPollers(status) {
    $.ajax({
        type: "POST",
        url: "uiMethods.asmx/wmSetAllPollers",
        data: '{"sSetToStatus":"' + status + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(msg) {
            //alert(msg.d);
            if (msg.d.length == 0) {
                showInfo('Poller status update successful.');
            } else {
                showAlert(msg.d);
            }
        },
        error: function(response) {
            showAlert(response.responseText);
        }
    });
}

function kickAllUsers() {
    $.ajax({
        type: "POST",
        url: "uiMethods.asmx/wmKickAllUsers",
        data: '',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(msg) {
            //alert(msg.d);
            if (msg.d.length == 0) {
                showInfo('All Users have been warned.  Kick should begin in two minutes.');
            } else {
                showAlert(msg.d);
            }
        },
        error: function(response) {
            showAlert(response.responseText);
        }
    });

    function setSystemCondition(status) {
        $.ajax({
            type: "POST",
            url: "uiMethods.asmx/setSystemCondition",
            data: '{"sSetToStatus":"' + status + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                //alert(msg.d);
                if (msg.d.length == 0) {
                    showInfo('Set System Condition successful.');
                } else {
                    showAlert(msg.d);
                }
            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    }
}
