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
    $("#txtTag").keypress(function (e) { return restrictEntryToSafeHTML(e, this); });

    $("[tag='selectable']").live("click", function () {
        var tag = $(this).parent().attr("tag_name");
        var desc = $.trim($('#' + $(this).parent().attr("desc_id")).html());

        $("#hidMode").val("edit");
        $("#edit_dialog").dialog('open');

        $("#hidCurrentEditID").val(tag);

        //show what was clicked
        $("#txtTag").val(tag);
        $("#txtDescription").val(desc);
    });

    $("#edit_dialog").dialog({
        autoOpen: false,
        modal: true,
        width: 500,
        bgiframe: true,
        title: 'Modify Tag',
        buttons: {
            "Save": function () {
                SaveTag();
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

function SaveTag() {
    var bSave = true;
    var strValidationError = '';

    //some client side validation before we attempt to save
    var old_tag = $("#hidCurrentEditID").val();

    var new_tag = $("#txtTag").val();
    if (new_tag == '') {
        bSave = false;
        strValidationError += 'Tag required.<br />';
    };

    if (bSave != true) {
        showInfo(strValidationError);
        return false;
    }

    var desc = $("#txtDescription").val();
    if ($("#hidMode").val() == 'edit') {
        //edit an existing tag
        $.ajax({
            async: false,
            type: "POST",
            url: "uiMethods.asmx/wmUpdateTag",
            data: '{"sOldTagName":"' + old_tag + '","sNewTagName":"' + new_tag + '","sDescription":"' + desc + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                showInfo('Tag Saved');

                $("#hidCurrentEditID").val("");
                $("#edit_dialog").dialog('close');

                //leave any search string the user had entered, so just click the search button
                $("[id*='btnSearch']").click();
            },
            error: function (response) {
                showAlert('Error: ' + response.responseText);
            }
        });
    } else if ($("#hidMode").val() == 'add') {
        //add a new tag
        $.ajax({
            async: false,
            type: "POST",
            url: "uiMethods.asmx/wmCreateTag",
            data: '{"sTagName":"' + new_tag + '","sDescription":"' + desc + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (retval) {
                showInfo('Tag Created');

                $("#hidCurrentEditID").val("");
                $("#edit_dialog").dialog('close');

                //leave any search string the user had entered, so just click the search button
                $("[id*='btnSearch']").click();
            },
            error: function (response) {
                showAlert('Error: ' + response.responseText);
            }
        });
    }
}

function ShowItemAdd() {
    $("#hidMode").val("add");

    // clear fields
    $('#txtTag').val('');
    $('#txtDescription').val('');

    $('#edit_dialog').dialog('option', 'title', 'Create a New Tag');
    $("#edit_dialog").dialog('open');
    $("#txtTag").focus();
}

function DeleteItems() {
    var ArrayString = $("#hidSelectedArray").val();
    $.ajax({
        type: "POST",
        url: "uiMethods.asmx/wmDeleteTags",
        data: '{"sDeleteArray":"' + ArrayString + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            if (msg.d.length == 0) {
                $("#hidSelectedArray").val("");
                $("#delete_dialog").dialog('close');

                showInfo('Delete Successful');

                $("[id*='btnSearch']").click();
            } else {
                showAlert(msg.d);
            }
        },
        error: function (response) {
            showAlert('Error: ' + response.responseText);
        }
    });

}