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
    $('#edit_dialog').dialog('option', 'title', 'New Eco Template');
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
        location.href = 'ecoTemplateEdit.aspx?ecotemplate_id=' + $(this).parent().attr("ecotemplate_id");
    });
});
function pageLoad() {
    ManagePageLoad();
}

function ShowItemAdd() {
    $("#hidMode").val("add");

    // clear all of the previous values
    clearEditDialog();

    $("#edit_dialog").dialog('open');
    $("#txtTemplateName").focus();
}

function DeleteItems() {
    var ArrayString = $("#hidSelectedArray").val();
    $.ajax({
        type: "POST",
        url: "uiMethods.asmx/wmDeleteEcotemplates",
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

                $("#delete_dialog").dialog('close');
                
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

    //some client side validation before we attempt to save the user
    if ($("[jqname='txtTemplateName']").val() == "") {
        bSave = false;
        strValidationError += 'Name is required.';
    };

    if (bSave != true) {
        showAlert(strValidationError);
        return false;
    }

    $.ajax({
        async: false,
        type: "POST",
        url: "uiMethods.asmx/wmCreateEcotemplate",
        data: '{"sName":"' + $("[jqname='txtTemplateName']").val() + '","sDescription":"' + $("[jqname='txtTemplateDesc']").val() + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            if (msg.d.length == 36) {
                location.href = "ecoTemplateEdit.aspx?ecotemplate_id=" + msg.d;
            }
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}
