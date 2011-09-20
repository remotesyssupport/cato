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

//Shared functionality for the "tag" picker dialog.

$(document).ready(function() {
    //dialog init
    $("#tag_picker_dialog").dialog({
        modal: true,
        autoOpen: false,
        bgiframe: true,
        width: 300,
        height: 500,
        buttons: {
            Close: function() {
                $(this).dialog('close');
            }
        }
    });

    //what happens when you select a value in the tag picker?
    $("#tag_picker_dialog .tag_picker_tag").live("click", function() {
        //if the item already exists... we can't add it again.
        //but the user will never know, they'll just think they added it even though it was already there.
        var id = $(this).attr("id").replace(/tpt_/, "ot_");
        var tag_name = $(this).attr("id").replace(/tpt_/, "");

        if ($("#" + id).length == 0) {
            var ahtml = '<li id="' + id.replace(/ /g, "") + '" val="' + tag_name + '" class="tag">';
            ahtml += '<table class="object_tags_table"><tr>';
            ahtml += '<td style="vertical-align: middle;">' + tag_name + '</td>';
            ahtml += '<td width="1px"><img class="tag_remove_btn" remove_id="' + id + '" src="../images/icons/fileclose.png" alt="" /></td>';
            ahtml += '</tr></table>';
            ahtml += '</li>';

            $("#objects_tags").append(ahtml);

            //commit the tag add (ONLY IF ON A DYNAMIC PAGE!)...
            if ($("#hidPageSaveType").val() == "dynamic") {
                var oid = GetCurrentObjectID();
                var ot = GetCurrentObjectType();
                $.ajax({
                    async: false,
                    type: "POST",
                    url: "../pages/uiMethods.asmx/wmAddObjectTag",
                    data: '{"sObjectID":"' + oid + '","sObjectType":"' + ot + '","sTagName":"' + tag_name + '"}',
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function(retval) { },
                    error: function(response) {
                        showAlert(response.responseText);
                    }
                });
            }

        }
        //and whack it from this list so you can't add twice
        //and clear the description
        $(this).remove();
        $("#tag_picker_description").html("");
    });


    //hover shows the description for the tag
    $("#tag_picker_dialog .tag_picker_tag").live("mouseover", function() {
        $(this).addClass("tag_picker_tag_hover");
        $("#tag_picker_description").html($(this).attr("desc"));
    });
    $("#tag_picker_dialog .tag_picker_tag").live("mouseout", function() {
        $(this).removeClass("tag_picker_tag_hover");
    });



    //NOW! this stuff can hopefully stay here, as the mechanism for adding/removing tags from an object should be the same
    //styles might be different on the task/proc pages?
    //remove a tag
    $(".tag_remove_btn").live("click", function() {
        $("#" + $(this).attr("remove_id").replace(/ /g, "")).remove();

        //commit the tag removal
        if ($("#hidPageSaveType").val() == "dynamic") {
            $("#update_success_msg").text("Updating...").show();

            var oid = GetCurrentObjectID();
            var ot = GetCurrentObjectType();
            var tag_name = $(this).attr("remove_id").replace(/ot_/, "");

            $.ajax({
                async: false,
                type: "POST",
                url: "../pages/uiMethods.asmx/wmRemoveObjectTag",
                data: '{"sObjectID":"' + oid + '","sObjectType":"' + ot + '","sTagName":"' + tag_name + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function(retval) { $("#update_success_msg").text("Update Successful").fadeOut(2000); },
                error: function(response) {
                    showAlert(response.responseText);
                }
            });
        }
    });

    //add a tag
    $(".tag_add_btn").live("click", function() {

        //hokey, but our "ids" are in different hidden fields on different pages!
        //that should be normalized eventually
        var oid = GetCurrentObjectID();
        GetTagList(oid);

        //hide the title bar on this page, it's a double stacked dialog
        $("#tag_picker_dialog").dialog().parents(".ui-dialog").find(".ui-dialog-titlebar").remove();

        $("#tag_picker_dialog").dialog('open');
    });
});

function GetCurrentObjectID() {
    var oid = "";
    if (oid == "" || (typeof oid == "undefined")) { oid = $("input[id$='hidObjectID']").val(); }
    if (oid == "" || (typeof oid == "undefined")) { oid = $("input[id$='hidCurrentEditID']").val(); }
    if (oid == "" || (typeof oid == "undefined")) { oid = $("input[id$='hidOriginalTaskID']").val(); }
    if (oid == "" || (typeof oid == "undefined")) { oid = $("input[id$='hidProcedureID']").val(); }

    if (typeof oid == "undefined")
        oid = "";

    return oid;
}
function GetCurrentObjectType() {
    var ot = $("#hidObjectType").val();
    if (typeof ot == "undefined")
        ot = -1;
    return ot;
}

function GetTagList(sObjectID) {
    if (typeof sObjectID == "undefined")
        sObjectID = "";

    $.ajax({
        async: false,
        type: "POST",
        url: "../pages/uiMethods.asmx/wmGetTagList",
        data: '{"sObjectID":"' + sObjectID + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(retval) {
            //put the results in the dialog
            $("#tag_picker_list").html(retval.d);
        },
        error: function(response) {
            showAlert(response.responseText);
        }
    });
}

function GetObjectsTags(sObjectID) {
    $.ajax({
        type: "POST",
        url: "../pages/uiMethods.asmx/wmGetObjectsTags",
        data: '{"sObjectID":"' + sObjectID + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(retval) {
            $("#objects_tags").html(retval.d);
        },
        error: function(response) {
            showAlert(response.responseText);
        }
    });
}