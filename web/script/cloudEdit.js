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

    // clear the edit array
    $("#hidSelectedArray").val("");

    $("[tag='selectable']").live("click", function () {
        LoadEditDialog($(this).parent().attr("account_id"));
    });

    //dialogs

    $("#edit_dialog").dialog({
        autoOpen: false,
        modal: true,
        height: 500,
        width: 500,
        bgiframe: true,
        buttons: {
            "Save": function () {
                SaveItem(1);
            },
            Cancel: function () {
                $("#edit_dialog").dialog('close');
            }
        }
    });

    $("#edit_dialog_tabs").tabs();



	//override the search click button as defined on managepagecommon.js, because this page is now ajax!
	$("#item_search_btn").die();
	//and rebind it
	$("#item_search_btn").live("click", function () {
        GetClouds();
    });
    
    //the test connection buttton
    $("#test_connection_btn").button({ icons: { primary: "ui-icon-link"} });
	$("#test_connection_btn").live("click", function () {
        TestConnection();
    });
});

function pageLoad() {
    ManagePageLoad();
}

function TestConnection() {
	SaveItem(0);

    var cloud_id = $("#hidCurrentEditID").val();
    var account_id = $("#ctl00_phDetail_ddlTestAccount").val();

    //if there's no cloud_id, it's not been saved yet.  We can't do anything.
    //but there's no point in displaying anything, as the save routine should handle all
    //the field level validation.
    if (cloud_id != "")
    {    
		
		$("#conn_test_result").text("Testing...");
		$("#conn_test_error").empty();
	
	    
	    $.ajax({
	        type: "POST",
	        async: false,
	        url: "awsMethods.asmx/wmTestCloudConnection",
	        data: '{"sAccountID":"' + account_id + '","sCloudID":"' + cloud_id + '"}',
	        contentType: "application/json; charset=utf-8",
	        dataType: "json",
	        success: function (response) {
				try
				{
		        	var oResultData = eval('(' + response.d + ')');
					if (oResultData != null)
					{
						if (oResultData.result == "success") {
							$("#conn_test_result").css("color","green");
							$("#conn_test_result").text("Connection Successful.");
						}
						if (oResultData.result == "fail") {
							$("#conn_test_result").css("color","red");
							$("#conn_test_result").text("Connection Failed.");
							$("#conn_test_error").text(unpackJSON(oResultData.error));
						}			
					
					}
				}
				catch(err)
				{
					alert(err);
				}
	        },
	        error: function (response) {
	            showAlert(response.responseText);
	        }
	    });
	}
}

function GetClouds() {
    $.ajax({
        type: "POST",
        async: false,
        url: "cloudEdit.aspx/wmGetClouds",
        data: '{"sSearch":"' + $("#ctl00_phDetail_txtSearch").val() + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            $('#clouds').html(response.d);
            //gotta restripe the table
            initJtable(true, true);
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}

function LoadEditDialog(editID) {
    //specifically for the test connection feature
	$("#conn_test_result").empty();
	$("#conn_test_error").empty();
   
    clearEditDialog();
    $("#hidMode").val("edit");

    $("#hidCurrentEditID").val(editID);

    FillEditForm(editID);

    $('#edit_dialog_tabs').tabs('select', 0);
    $('#edit_dialog_tabs').tabs( "option", "disabled", [] );
    $("#edit_dialog").dialog("option", "title", "Modify Cloud");
    $("#edit_dialog").dialog('open');

}

function FillEditForm(sEditID) {
    $.ajax({
        type: "POST",
        async: false,
        url: "cloudEdit.aspx/LoadCloud",
        data: '{"sID":"' + sEditID + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            //update the list in the dialog
            if (response.d.length == 0) {
                showAlert('error no response');
                // do we close the dialog, leave it open to allow adding more? what?
            } else {
                var oResultData = eval('(' + response.d + ')');

                // show the assets current values
                $("#txtCloudName").val(oResultData.sCloudName);
                $("#ddlProvider").val(oResultData.sProvider);
                $("#txtAPIUrl").val(oResultData.sAPIUrl);
            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}

function SaveItem(close_after_save) {
    var bSave = true;
    var strValidationError = '';

    //some client side validation before we attempt to save
    var sCloudID = $("#hidCurrentEditID").val();

    var sCloudName = $("#txtCloudName").val();
    if (sCloudName == '') {
        bSave = false;
        strValidationError += 'Cloud Name required.';
    };

    var sAPIUrl = $("#txtAPIUrl").val();
    if (sAPIUrl == '') {
        bSave = false;
        strValidationError += 'API URL required.';
    };

    if (bSave != true) {
        showInfo(strValidationError);
        return false;
    }

    var args = '{"sMode":"' + $("#hidMode").val() + '", \
    	"sCloudID":"' + sCloudID + '", \
        "sCloudName":"' + sCloudName + '", \
        "sProvider":"' + $("#ddlProvider").val() + '", \
        "sAPIUrl":"' + sAPIUrl + '" \
        }';


	$.ajax({
        type: "POST",
        async: false,
        url: "cloudEdit.aspx/SaveCloud",
        data: args,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            var ret = eval('(' + response.d + ')');
	        if (ret) {
                // clear the search field and fire a search click, should reload the grid
                $("[id*='txtSearch']").val("");
				GetClouds();
	            
	            if (close_after_save) {
	            	$("#edit_dialog").dialog('close');
            	} else {
	            	//we aren't closing? fine, we're now in 'edit' mode.
	            	$("#hidMode").val("edit");
            		$("#hidCurrentEditID").val(ret.cloud_id);
            		$("#edit_dialog").dialog("option", "title", "Modify Cloud");	
            	}
	        } else {
	            showAlert(response.d);
	        }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}

function ShowItemAdd() {
    //specifically for the test connection feature
	$("#conn_test_result").empty();
	$("#conn_test_error").empty();
    $("#hidCurrentEditID").val("");
    
  
    clearEditDialog();
    $("#hidMode").val("add");

    $('#edit_dialog_tabs').tabs('select', 0);
    $('#edit_dialog_tabs').tabs( "option", "disabled", [1] );
    $('#edit_dialog').dialog('option', 'title', 'Create a New Cloud');
    $("#edit_dialog").dialog('open');
    $("#txtCloudName").focus();
}

function DeleteItems() {
    $("#update_success_msg").text("Deleting...").show().fadeOut(2000);

    var ArrayString = $("#hidSelectedArray").val();
    $.ajax({
        type: "POST",
        url: "cloudEdit.aspx/DeleteClouds",
        data: '{"sDeleteArray":"' + ArrayString + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            //update the list in the dialog
            if (msg.d.length == 0) {
                $("#hidSelectedArray").val("");
                $("#delete_dialog").dialog('close');

                // clear the search field and fire a search click, should reload the grid
                $("[id*='txtSearch']").val("");
				GetClouds();

                $("#update_success_msg").text("Delete Successful").show().fadeOut(2000);
            } else {
                showAlert(msg.d);
            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}
