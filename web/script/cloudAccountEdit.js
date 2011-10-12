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
                SaveItem();
            },
            Cancel: function () {
                $("#edit_dialog").dialog('close');
            }
        }
    });

    $("#edit_dialog_tabs").tabs();

    $("#keypair_dialog").dialog({
        autoOpen: false,
        modal: true,
        height: 400,
        width: 400,
        bgiframe: true,
        buttons: {
            "Save": function () {
                SaveKeyPair();
            },
            Cancel: function () {
                $("#keypair_dialog").dialog('close');
            }
        }
    });


    //keypair add button
    $("#keypair_add_btn").button({ icons: { primary: "ui-icon-plus"} });
    $("#keypair_add_btn").click(function () {
        //wipe the fields
        $("#keypair_id").val("");
        $("#keypair_name").val("");
        $("#keypair_passphrase").val("");

        //provide a template for the private key
        $("#keypair_private_key").val("-----BEGIN RSA PRIVATE KEY-----\n-----END RSA PRIVATE KEY-----");

        $("#keypair_dialog").dialog('open');
    });

    //keypair delete button
    $(".keypair_delete_btn").live("click", function () {
        if (confirm("Are you sure?")) {
            $("#update_success_msg").text("Deleting...").show().fadeOut(2000);

            var kpid = $(this).parents(".keypair").attr("id").replace(/kp_/, "");

            $.ajax({
                type: "POST",
                url: "cloudAccountEdit.aspx/DeleteKeyPair",
                data: '{"sKeypairID":"' + kpid + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (msg) {
                    $("#kp_" + kpid).remove();
                    $("#update_success_msg").text("Delete Successful").fadeOut(2000);
                },
                error: function (response) {
                    showAlert(response.responseText);
                }
            });
        }
    });

    //edit a keypair
    $(".keypair_label").live("click", function () {
        //clear the optional fields
        $("#keypair_private_key").val("");
        $("#keypair_passphrase").val("");

        //fill them
        $("#keypair_id").val($(this).parents(".keypair").attr("id"));
        $("#keypair_name").val($(this).html());

        //show stars for the private key and passphrase if they were populated
        //the server sent back a flag denoting that
        var pk = "-----BEGIN RSA PRIVATE KEY-----\n";

        if ($(this).parents(".keypair").attr("has_pk") == "true")
            pk += "**********\n";

        pk += "-----END RSA PRIVATE KEY-----";
        $("#keypair_private_key").val(pk);


        if ($(this).parents(".keypair").attr("has_pp") == "true")
            $("#keypair_passphrase").val("!2E4S6789O");


        $("#keypair_dialog").dialog('open');
    });


	//override the search click button as defined on managepagecommon.js, because this page is now ajax!
	$("#item_search_btn").die();
	//and rebind it
	$("#item_search_btn").live("click", function () {
        GetAccounts();
    });
});

function pageLoad() {
    ManagePageLoad();
}

function GetAccounts() {
    $.ajax({
        type: "POST",
        async: false,
        url: "cloudAccountEdit.aspx/wmGetAccounts",
        data: '{"sSearch":"' + $("#ctl00_phDetail_txtSearch").val() + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            $('#accounts').html(response.d);
            //gotta restripe the table
            initJtable(true, true);
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}

function LoadEditDialog(editID) {
    clearEditDialog();
    $("#hidMode").val("edit");

    $("#hidCurrentEditID").val(editID);

    FillEditForm(editID);

    $('#edit_dialog_tabs').tabs('select', 0);
    $('#edit_dialog_tabs').tabs( "option", "disabled", [] );
    $("#edit_dialog").dialog("option", "title", "Modify Account");
    $("#edit_dialog").dialog('open');

}

function FillEditForm(sEditID) {
    $.ajax({
        type: "POST",
        async: false,
        url: "cloudAccountEdit.aspx/LoadAccount",
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
                $("#txtAccountName").val(oResultData.sAccountName);
                $("#txtAccountNumber").val(oResultData.sAccountNumber)
                $("#ddlAccountType").val(oResultData.sAccountType);
                $("#txtLoginID").val(oResultData.sLoginID);
                $("#txtLoginPassword").val(oResultData.sLoginPassword);
                $("#txtLoginPasswordConfirm").val(oResultData.sLoginPassword);

                if (oResultData.sIsDefault == "1") $("#chkDefault").attr('checked', true);
                if (oResultData.sAutoManage == "1") $("#chkAutoManageSecurity").attr('checked', true);
            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });

    //get the keypairs
    GetKeyPairs(sEditID);
}

function GetKeyPairs(sEditID) {
    $.ajax({
        type: "POST",
        async: false,
        url: "cloudAccountEdit.aspx/GetKeyPairs",
        data: '{"sID":"' + sEditID + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            $('#keypairs').html(response.d);
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}

function SaveItem() {
    var bSave = true;
    var strValidationError = '';

    //some client side validation before we attempt to save
    var sAccountID = $("#hidCurrentEditID").val();

    var sAccountName = $("#txtAccountName").val();
    if (sAccountName == '') {
        bSave = false;
        strValidationError += 'Account Name required.';
    };

    if ($("#txtLoginPassword").val() != $("#txtLoginPasswordConfirm").val()) {
        bSave = false;
        strValidationError += 'Passwords do not match.';
    };

    if (bSave != true) {
        showInfo(strValidationError);
        return false;
    }

    var args = '{"sMode":"' + $("#hidMode").val() + '", \
    	"sAccountID":"' + sAccountID + '", \
        "sAccountName":"' + sAccountName + '", \
        "sAccountNumber":"' + $("#txtAccountNumber").val() + '", \
        "sAccountType":"' + $("#ddlAccountType").val() + '", \
        "sLoginID":"' + $("#txtLoginID").val() + '", \
        "sLoginPassword":"' + $("#txtLoginPassword").val() + '", \
        "sLoginPasswordConfirm":"' + $("#txtLoginPasswordConfirm").val() + '", \
        "sIsDefault":"' + ($("#chkDefault").attr("checked") ? "1" : "0") + '", \
        "sAutoManageSecurity":"' + ($("#chkAutoManageSecurity").attr("checked") ? "1" : "0") + '"}';


	$.ajax({
        type: "POST",
        async: false,
        url: "cloudAccountEdit.aspx/SaveAccount",
        data: args,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            var ret = eval('(' + response.d + ')');
	        if (ret) {
                // clear the search field and fire a search click, should reload the grid
                $("[id*='txtSearch']").val("");
				GetAccounts();
	            
	            $("#edit_dialog").dialog('close');
	
				//if we are adding a new one, add it to the dropdown too
				if ($("#hidMode").val() == "add") {
		            $('#ctl00_ddlCloudAccounts').append($('<option>', { value : ret.account_id }).text(ret.account_name + ' (' + ret.account_type + ')')); 
		          	//if this was the first one, get it in the session by nudging the change event.
		          	if ($("#ctl00_ddlCloudAccounts option").length == 1)
		          		$("#ctl00_ddlCloudAccounts").change();
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
    clearEditDialog();
    $("#hidMode").val("add");

    $('#edit_dialog_tabs').tabs('select', 0);
    $('#edit_dialog_tabs').tabs( "option", "disabled", [1] );
    $('#edit_dialog').dialog('option', 'title', 'Create a New Account');
    $("#edit_dialog").dialog('open');
    $("#txtAccountName").focus();
}

function SaveKeyPair() {
    var kpid = $("#keypair_id").val().replace(/kp_/, "");
    var name = $("#keypair_name").val();
	//pack up the PK field, JSON doesn't like it
    var pk = packJSON($("#keypair_private_key").val());
    var pp = $("#keypair_passphrase").val();
    var account_id = $("#hidCurrentEditID").val();

    //some client side validation before we attempt to save
    if (name == '') {
        showInfo("KeyPair Name is required.");
        return false;
    };

    $("#update_success_msg").text("Saving...").show().fadeOut(2000);

    $.ajax({
        type: "POST",
        url: "cloudAccountEdit.aspx/SaveKeyPair",
        data: "{'sKeypairID' : '" + kpid + "','sAccountID' : '" + account_id + "','sName' : '" + name + "','sPK' : '" + pk + "','sPP' : '" + pp + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            if (msg.d == "") {
                if (kpid) {
                    //find the label and update it
                    $("#kp_" + kpid + " .keypair_label").html(name);
                } else {
                    //re-get the list
                    GetKeyPairs(account_id);
                }
                $("#update_success_msg").text("Save Successful").show().fadeOut(2000);
                $("#keypair_dialog").dialog('close');
            }
            else {
                showAlert(msg.d);
            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}

function DeleteItems() {
    $("#update_success_msg").text("Deleting...").show().fadeOut(2000);

    var ArrayString = $("#hidSelectedArray").val();
    $.ajax({
        type: "POST",
        url: "cloudAccountEdit.aspx/DeleteAccounts",
        data: '{"sDeleteArray":"' + ArrayString + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
        	var do_refresh = false;
        	
        	//remove the selected ones from the cloud account dropdown
			myArray = $("#hidSelectedArray").val().split(',');
			$.each(myArray, function(name, value) {
				//first, which one is selected?
				var current = $("#mySelect option:selected").val();
				//whack it
				$('#ctl00_ddlCloudAccounts [value="' + value + '"]').remove();
				//if we whacked what was selected, flag for change push
				if (value == current)
					do_refresh = true;
			});
            
            if (do_refresh)
            	$('#ctl00_ddlCloudAccounts').change();
            
            //update the list in the dialog
            if (msg.d.length == 0) {
                $("#hidSelectedArray").val("");
                $("#delete_dialog").dialog('close');

                // clear the search field and fire a search click, should reload the grid
                $("[id*='txtSearch']").val("");
				GetAccounts();

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
