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

    $("#edit_dialog").hide();
    $("#delete_dialog").hide();

    //define dialogs
    $('#edit_dialog').dialog('option', 'title', 'New Ecosystem');
    $("#edit_dialog").dialog({
        autoOpen: false,
        modal: true,
        width: 500,
        buttons: {
            "Create": function () {
                showPleaseWait();
                Save();
            },
            Cancel: function () {
                $("[id*='lblNewMessage']").html("");
                $("#hidCurrentEditID").val("");

                $("#hidSelectedArray").val("");
                $("#lblItemsSelected").html("0");

                // nice, clear all checkboxes selected in a single line!
                $(':input', (".jtable")).attr('checked', false);

                $(this).dialog('close');
            }
        }
    });

    //what happens when you click a row?
    $("[tag='selectable']").live("click", function () {
        showPleaseWait();
        location.href = 'ecosystemEdit.aspx?ecosystem_id=' + $(this).parent().attr("ecosystem_id");
    });
});
function pageLoad() {
    ManagePageLoad();
}

function CloudAccountWasChanged() {
    location.reload();
}

function ShowItemAdd() {
    $("#hidMode").val("add");

    // clear all of the previous values
    clearEditDialog();

    $("#edit_dialog").dialog('open');
    $("#txtTaskName").focus();
}

function DeleteItems() {
    var ArrayString = $("#hidSelectedArray").val();
    $.ajax({
        type: "POST",
        url: "uiMethods.asmx/wmDeleteEcosystems",
        data: '{"sDeleteArray":"' + ArrayString + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            if (msg.d.length == 0) {

                $("#hidSelectedArray").val("");
                $("#delete_dialog").dialog('close');

                // clear the search field and fire a search click, should reload the grid
                $("[id*='txtSearch']").val("");
                $("[id*='btnSearch']").click();

                hidePleaseWait();
                showInfo('Delete Successful');

            } else {
                showAlert(msg.d);
                // reload the list, some may have been deleted.
                // clear the search field and fire a search click, should reload the grid
                $("[id*='txtSearch']").val("");
                $("[id*='btnSearch']").click();
            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });

}



function Save() {
    var bSave = true;
    var strValidationError = '';

    //some client side validation before we attempt to save
    if ($("#ctl00_phDetail_ddlEcotemplates").val() == "") {
        bSave = false;
        strValidationError += 'Ecosystems must belong to an Ecosystem Template.  Create an Ecosystem Template first.';
    };

    if ($("#new_ecosystem_name").val() == "") {
        bSave = false;
        strValidationError += 'Name is required.';
    };

    //we will test it, but really we're not gonna use it rather we'll get it server side
    //this just traps if there isn't one.
    if ($("#ctl00_ddlCloudAccounts").val() == "") {
        bSave = false;
        strValidationError += 'Error: Unable to determine Cloud Account.';
    };

    if (bSave != true) {
        showAlert(strValidationError);
        return false;
    }

    $.ajax({
        async: false,
        type: "POST",
        url: "uiMethods.asmx/wmCreateEcosystem",
        data: '{"sName":"' + $("#new_ecosystem_name").val() + '","sDescription":"' + $("#new_ecosystem_desc").val() + '","sEcotemplateID":"' + $("#ctl00_phDetail_ddlEcotemplates").val() + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            if (msg.d.length == 36) {
                location.href = "ecosystemEdit.aspx?ecosystem_id=" + msg.d;
            } else {
                showAlert(msg.d);
                hidePleaseWait();
            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}
