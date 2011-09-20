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

    //specific field validation and masking
    $("#txtCredUsername").keypress(function (e) { return restrictEntryToUsername(e, this); });
    $("#txtCredDomain").keypress(function (e) { return restrictEntryToUsername(e, this); });


    // clear the edit array
    $("#hidSelectedArray").val("");

    $("[tag='selectable']").live("click", function () {
        LoadEditDialog(0, $(this).parent().attr("credential_id"));
    });

    // burn through the grid and disable any checkboxes that have assets associated
    $("[tag='chk']").each(
        function (intIndex) {
            if ($(this).attr("assets") != "0") {
                this.disabled = true;
            }
        }
    );

    $("#edit_dialog").dialog({
        autoOpen: false,
        modal: true,
        width: 500,
        bgiframe: true,
        buttons: {
            "Save": function () {
                SaveCredential();
            },
            Cancel: function () {
                $("#edit_dialog").dialog('close');
            }
        }

    });

});

function pageLoad() {
    ManagePageLoad();
}

function LoadEditDialog(editCount, editID) {
    clearEditDialog();

    $("#hidMode").val("edit");
    $("#edit_dialog").dialog("option", "title", "Modify Credential");
    $("#edit_dialog").dialog('open');

    $("#hidEditCount").val(editCount);
    $("#hidCurrentEditID").val(editID);
    FillEditForm(editID);


}

function FillEditForm(sEditID) {


    //alert('load ' + sEditID);
    //return false;

    $.ajax({
        type: "POST",
        url: "sharedCredentialEdit.aspx/LoadCredential",
        data: '{"sCredentialID":"' + sEditID + '"}',
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
                $("#txtCredName").val(oResultData.sCredName);
                $("#txtCredUsername").val(oResultData.sCredUsername);
                $("#txtCredDomain").val(oResultData.sCredDomain)
                $("#txtCredPassword").val(oResultData.sCredPassword);
                $("#txtCredPasswordConfirm").val(oResultData.sCredPassword);
                $("#txtPrivilegedPassword").val(oResultData.sPriviledgedPassword);
                $("#txtPrivilegedConfirm").val(oResultData.sPriviledgedPassword);
                $("#txtCredDescription").val(oResultData.sCredDesc);
            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}

function SaveCredential() {
    var bSave = true;
    var strValidationError = '';

    //some client side validation before we attempt to save
    var sCredentialID = $("#hidCurrentEditID").val();
    var sCredName = $("#txtCredName").val();
    var sCredUserName = $("#txtCredUsername").val();
    if (sCredName == '') {
        bSave = false;
        strValidationError += 'Credential Name required.';
    };
    if (sCredUserName == '') {
        bSave = false;
        strValidationError += 'User Name required.';
    };

    if ($("#txtCredPassword").val() != $("#txtCredPasswordConfirm").val()) {
        bSave = false;
        strValidationError += 'Passwords do not match.';
    };
    if ($("#txtPrivilegedPassword").val() != $("#txtPrivilegedConfirm").val()) {
        bSave = false;
        strValidationError += 'Priviledged passwords do not match.';
    };

    if (bSave != true) {
        showInfo(strValidationError);
        return false;
    }

    var stuff = new Array();
    stuff[0] = sCredentialID;
    stuff[1] = sCredName;
    stuff[2] = sCredUserName;
    stuff[3] = $("#txtCredDescription").val()
    stuff[4] = $("#txtCredPassword").val()
    stuff[5] = $("#txtCredDomain").val()
    stuff[6] = $("#hidMode").val();
    stuff[7] = $("#txtPrivilegedPassword").val()

    if (stuff.length > 0) {
        PageMethods.SaveCredential(stuff, OnUpdateSuccess, OnUpdateFailure);
    } else {
        showAlert('incorrect list of update attributes:' + stuff.length.toString());
    }
}

// Callback function invoked on successful completion of the MS AJAX page method.
function OnUpdateSuccess(result, userContext, methodName) {
    if (result.length == 0) {
        showInfo('Credential updated.');

        $("#edit_dialog").dialog('close');

        //leave any search string the user had entered, so just click the search button
        $("[id*='btnSearch']").click();
    } else {
        showAlert(result);
    }


}

// Callback function invoked on failure of the MS AJAX page method.
function OnUpdateFailure(error, userContext, methodName) {
    //alert('failure');
    if (error !== null) {
        showAlert(error.get_message());
    }
}

function ShowItemAdd() {
    $("#hidMode").val("add");

    clearEditDialog();
    $('#edit_dialog').dialog("option", "title", 'Create a New Credential');

    $("#edit_dialog").dialog('open');
    $("#txtCredName").focus();


}
function DeleteItems() {
    var ArrayString = $("#hidSelectedArray").val();
    $.ajax({
        type: "POST",
        url: "sharedCredentialEdit.aspx/DeleteCredentials",
        data: '{"sDeleteArray":"' + ArrayString + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            //update the list in the dialog
            var myArray = new Array();
            var myArray = msg.d.split(',');
            if (msg.d.length == 0) {


                $("#hidSelectedArray").val("");
                $("#delete_dialog").dialog('close');

                // clear the search field and fire a search click, should reload the grid
                $("[id*='txtSearch']").val("");
                $("[id*='btnSearch']").click();

                showInfo('Delete Successful');

            } else {
                showAlert(msg.d);

            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });

}