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
    //there are datepickers all over the app.  Anything with a class of "datepicker" will get initialized.
    $(".datepicker").datepicker({ clickInput: true });
    $(".datetimepicker").datetimepicker();
    $(".timepicker").timepicker();


    $("#error_dialog").dialog({
        autoOpen: false,
        bgiframe: false,
        modal: true,
        width: 400,
        overlay: {
            backgroundColor: '#000',
            opacity: 0.5
        },
        buttons: {
            Ok: function () {
                $(this).dialog('close');
            }
        },
        close: function (event, ui) {
            //$("#fullcontent").unblock();
            //$("#head").unblock();
            $.unblockUI();
            //sometimes ajax commands would have blocked the task_steps div.  Unblock that just as a safety catch.
            $("#task_steps").unblock();
        }
    });

    $("#info_dialog").dialog({
        autoOpen: false,
        draggable: false,
        resizable: false,
        bgiframe: true,
        modal: false,
        width: 400
    });
});

//This function shows the error dialog.
function showAlert(msg, info) {
	var trace = '';
	
	// in many cases, the "msg" will be a json object with a stack trace
	//see if it is...
	try
	{
		var o = eval('(' + msg + ')');
		if (o.Message) 
	 	{
	 		msg = o.Message;
			trace = o.StackTrace;
		}
	}
	catch(err)
	{
		//nothing to catch, just will display the original message since we couldn't parse it.
	}
	
	
    hidePleaseWait();
    $("#error_dialog_message").html(msg);
    $("#error_dialog_info").html(info);
    if (trace != null && trace != '') {
    	$("#error_dialog_trace").html(trace);
    	$("#error_dialog_trace").parent().show();
    } else {
    	$("#error_dialog_trace").parent().hide();
    }
    
    $("#error_dialog").dialog('open');

    //send this message via email
    var msgtogo = escape(msg + '\n' + info + '\n' + trace);
    var pagedetails = window.location.pathname;

    $.post("uiMethods.asmx/wmSendErrorReport", { "sMessage": msgtogo, "sPageDetails": pagedetails });

}

//This function shows the info dialog.
function showInfo(msg, info, no_timeout) {
    $("#info_dialog_message").html(msg);

    if (typeof (info) != "undefined" && info.length > 0)
        $("#info_dialog_info").html(info);
    else
        $("#info_dialog_info").html("");

    //set it to auto close after 2 seconds
    //if the no_timeout flag was not passed
    if (typeof (no_timeout) != "undefined" && no_timeout) {
        $("#info_dialog").dialog("option", "buttons", {
            "OK": function () { $(this).dialog("close"); }
        });
    } else {
        $("#info_dialog").dialog("removebutton", "OK");
        setTimeout("hideInfo()", 2000);
    }

    $("#info_dialog").dialog('open');
}
//This function hides the info dialog.
function hideInfo() {
    $("#info_dialog").dialog('close');
}
