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
    $("#txtDomain").keypress(function (e) { return restrictEntryToIdentifier(e, this); });
    $("#txtAddress").keypress(function (e) { return restrictEntryToHostname(e, this); });

    // clear the edit array
    $("#hidSelectedArray").val("");

    $("[tag='selectable']").live("click", function () {
        LoadEditDialog(0, $(this).parent().attr("domain"));
    });

});

function pageLoad() {
    ManagePageLoad();
}

function LoadEditDialog(editCount, editID) {



    $("#hidMode").val("edit");
    $("#edit_dialog").dialog({
        autoOpen: false,
        modal: true,
        width: 500,
        bgiframe: true,
        buttons: {
            "Save": function () {
                SaveDomain();
            },
            Cancel: function () {
                $("#edit_dialog").dialog('destroy');
            }
        }

    });
    $("#edit_dialog").dialog("option", "title", "Modify Domain");
    $("#edit_dialog").dialog('open');

    $("#hidEditCount").val(editCount);
    $("#hidCurrentEditID").val(editID);
    FillEditForm(editID);

}

function FillEditForm(sEditID) {

    $.ajax({
        type: "POST",
        url: "ldapEdit.aspx/LoadDomain",
        data: '{"sDomain":"' + sEditID + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            //update the list in the dialog
            if (response.d.length == 0) {
                showAlert('error no response');
            } else {

                var oResultData = eval('(' + response.d + ')');
                // show the assets current values
                $("#txtDomain").val(oResultData.sDomain);
                $("#txtAddress").val(oResultData.sAddress)
            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}

function SaveDomain() {
    var bSave = true;
    var strValidationError = '';

    //some client side validation before we attempt to save
    var sEditDomain = $("#hidCurrentEditID").val();

    var sDomain = $("#txtDomain").val();
    if (sDomain == '') {
        bSave = false;
        strValidationError += 'Domain required.<br />';
    };
    var sAddress = $("#txtAddress").val();
    if (sAddress == '') {
        bSave = false;
        strValidationError += 'Address required.';
    };

    if (bSave != true) {
        showInfo(strValidationError);
        return false;
    }


    var stuff = new Array();
    stuff[0] = sEditDomain;
    stuff[1] = sDomain;
    stuff[2] = sAddress
    stuff[3] = $("#hidMode").val();

    if (stuff.length > 0) {
        PageMethods.SaveDomain(stuff, OnUpdateSuccess, OnUpdateFailure);
    } else {
        showAlert('incorrect list of update attributes:' + stuff.length.toString());
    }
}

// Callback function invoked on successful completion of the MS AJAX page method.
function OnUpdateSuccess(result, userContext, methodName) {
    if (result.length == 0) {
        showInfo('LDAP Domain updated.');

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

    // clear domain
    $('#txtDomain').val('');
    $('#txtAddress').val('');

    $('#edit_dialog').dialog('option', 'title', 'Create a New Domain');
    $("#edit_dialog").dialog({
        autoOpen: false,
        modal: true,
        width: 500,
        bgiframe: true,
        buttons: {
            "Create": function () {
                SaveDomain();
            },
            Cancel: function () {
                $("#edit_dialog").dialog('destroy');
            }
        }
    });

    $("#edit_dialog").dialog('open');
    $("#txtDomain").focus();


}

function DeleteItems() {
    var ArrayString = $("#hidSelectedArray").val();
    $.ajax({
        type: "POST",
        url: "ldapEdit.aspx/DeleteDomains",
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