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

//Attribute specific functionality for the taskEdit page

$(document).ready(function() {
    //add an attribute group
    $("#attribute_group_add_btn").live("click", function() {
        $("#ctl00_phDetail_hidAddToAttributeGroupID").val("");
        GetAttributePickerAttributes();
        $("#attribute_picker_dialog").dialog('open');
    });

    //remove an attribute group
    $(".attribute_group_remove_btn").live("click", function() {
        if (confirm("Are you sure?")) {
            doAttributeRemoveGroup($(this).attr("remove_id"));
        }
    });

    //remove a single attribute
    $(".attribute_remove_btn").live("click", function() {
        if (confirm("Are you sure?")) {
            var ids = $(this).attr("remove_id").split('_');
            var gid = ids[0];
            var avid = ids[1];

            //remove the whole group if there is only one attribute
            if ($("#attributes_" + gid + " .attribute").length == 1) {
                doAttributeRemoveGroup(gid);
            }
            else {
                //otherwise just remove this one
                doAttributeRemove(gid, avid);
            }
        }
    });

    //show the add attribute dialog
    $(".attribute_add_btn").live("click", function() {
        $("#ctl00_phDetail_hidAddToAttributeGroupID").val($(this).attr("add_to_group_id"));
        GetAttributePickerAttributes();
        $("#attribute_picker_dialog").dialog('open');
    });

    //select a value in the picker
    $("#attribute_picker_dialog .attribute_picker_value").live("click", function() {
        //if a group id exists in the hidden field, add to it.
        //if not, add the attribute to a new group.

        var gid = $("#ctl00_phDetail_hidAddToAttributeGroupID").val();
        var aid = $(this).attr("attr_id");
        var an = $(this).attr("attr_name");
        var avid = $(this).attr("attr_val_id");
        var av = $(this).attr("attr_val");

        //temporary
        var drawgroup = false;
        if (gid.length == 0)
            drawgroup = true;

        //first, we must do the ajax, and get the proper new id's
        //this will return the group id, whether it's new or not.
        gid = doAttributeAdd(gid, aid, avid);


        if (drawgroup) {
            var ghtml = '<div id="' + gid + '">';
            ghtml += '  <div class="attribute_group">';
            ghtml += '      <div class="attribute_group_title">';
            ghtml += '          <div class="attribute_group_icons">';
            ghtml += '              <span remove_id="' + gid + '" class="attribute_group_remove_btn"><img src="../images/icons/fileclose.png" alt="" /></span>';
            ghtml += '          </div>';
            ghtml += '      </div>';
            ghtml += '      <div id="attributes_' + gid + '">';
            ghtml += '      </div>';
            ghtml += '      <span add_to_group_id="' + gid + '" class="attribute_add_btn pointer"><img src="../images/icons/edit_add.png" alt="" style="width: 12px; height: 12px;"/> click to add more</span>';
            ghtml += '  </div>';
            ghtml += '<span class="attribute_or">OR</span>';
            ghtml += '</div>';

            $("#attribute_groups").append(ghtml);

            //set the hidden value now, so if they keep adding it will append the new group.
            $("#ctl00_phDetail_hidAddToAttributeGroupID").val(gid);
        }

        //if the item already exists... we can't add it again.
        //but the user will never know, they'll just think they added it even though it was already there.
        var combined_id = gid + "_" + avid;

        if ($("#" + combined_id).length == 0) {
            var ahtml = '<div id="' + combined_id + '" class="attribute">';
            ahtml += '<div class="attribute_title">';

            ahtml += '<table width="100%">';
            ahtml += '<tr>';
            ahtml += '<td width="99%">' + an + ' :: ' + av + '</td>';
            ahtml += '<td width="1%">';
            ahtml += '<span class="attribute_remove_btn" remove_id="' + combined_id + '">';
            ahtml += '<img src="../images/icons/fileclose.png" style="width: 12px; height: 12px;" alt="" /></span>';
            ahtml += '</td>';
            ahtml += '</tr>';
            ahtml += '</table>';

            ahtml += '</div>';
            ahtml += '<span class="attribute_and">and</span>';
            ahtml += '</div>';

            $("#attributes_" + gid).append(ahtml);
        }

    });
});

//new wicked one at a time update
function doAttributeAdd(gid, aid, avid) {
    var task_id = $("#ctl00_phDetail_hidTaskID").val();

    $("#update_success_msg").text("Updating...").show();
    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmAddTaskAttribute",
        data: '{"sTaskID":"' + task_id + '","sGroupID":"' + gid + '","sAttributeID":"' + aid + '","sAttributeValueID":"' + avid + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(retval) {
            $("#update_success_msg").text("Update Successful").fadeOut(2000);
            gid = retval.d;
        },
        error: function(response) {
            showAlert(response.responseText);
        }
    });

    return gid;
}
function doAttributeRemove(gid, avid) {
    var task_id = $("#ctl00_phDetail_hidTaskID").val();

    $("#update_success_msg").text("Updating...").show();
    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmRemoveTaskAttribute",
        data: '{"sTaskID":"' + task_id + '","sGroupID":"' + gid + '","sAttributeValueID":"' + avid + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(retval) {
            $("#" + gid + "_" + avid).remove();
            $("#update_success_msg").text("Update Successful").fadeOut(2000);
        },
        error: function(response) {
            showAlert(response.responseText);
        }
    });
}
function doAttributeRemoveGroup(gid) {
    var task_id = $("#ctl00_phDetail_hidTaskID").val();

    $("#update_success_msg").text("Updating...").show();
    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmRemoveTaskAttributeGroup",
        data: '{"sTaskID":"' + task_id + '","sGroupID":"' + gid + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(retval) {
            $("#" + gid).remove();
            $("#update_success_msg").text("Update Successful").fadeOut(2000);
        },
        error: function(response) {
            showAlert(response.responseText);
        }
    });
}