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


//copy from managePageCommon.js

//only fires on initial load of the page.
$(document).ready(function () {
    $("#left_tooltip_box_inner").corner("round 8px").parent().css('padding', '2px').corner("round 10px");

    // nice, clear all checkboxes selected in a single line!
    $(':input', (".jtable")).attr('checked', false);

    //check/uncheck all checkboxes
    $("#chkAll").live("click", function () {
        if (this.checked) {
            this.checked = true;
            $("[tag='chk']").attr("checked", true);
        } else {
            this.checked = false;
            $("[tag='chk']").attr("checked", false);
        }

        //now build out the array
        var lst = "";
        $("[tag='chk']").each(function (intIndex) {
            if (this.checked) {
                lst += $(this).attr("object_id") + ",";
            }
        });

        //chop off the last comma
        if (lst.length > 0)
            lst = lst.substring(0, lst.length - 1);

        $("#hidSelectedArray").val(lst);
        $("#lblItemsSelected").html($("[tag='chk']:checked").length);

    });


    //this spins thru the check boxes on the page and builds the array.
    //yes it rebuilds the list on every selection, but it's fast.
    $("[tag='chk']").live("click", function () {
        //first, deal with some 'check all' housekeeping
        //if I am being unchecked, uncheck the chkAll box too.
        //we do not have logic here to check the chkAll box if all items are checked.
        //that's not necessary right now.
        if (!this.checked) {
            $("#chkAll").attr("checked", false)
        }

        //now build out the array
        var lst = "";
        $("[tag='chk']").each(function (intIndex) {
            if (this.checked) {
                lst += $(this).attr("object_id") + ",";
            }
        });

        //chop off the last comma
        if (lst.length > 0)
            lst = lst.substring(0, lst.length - 1);

        $("#hidSelectedArray").val(lst);
        $("#lblItemsSelected").html($("[tag='chk']:checked").length);

    });


    $("#add_to_ecosystem_btn").click(function () {
            Save();
    });

    $(".ecosystem_link").live("click", function () {
        if ($(this).attr("ecosystem_id") != "")
            location.href = "ecosystemEdit.aspx?ecosystem_id=" + $(this).attr("ecosystem_id");
    });

    $(".group_tab").click(function () {
        showPleaseWait("Querying AWS...");

        //style tabs
        $(".group_tab").removeClass("group_tab_selected");
        $(this).addClass("group_tab_selected");


        //get content here at some possible point in the future.
        $("#update_success_msg").text("Querying AWS...").show();

        var object_label = $(this).html();
        var object_type = $(this).attr("object_type");

        //well, I have no idea why, but this ajax fires before the showPleaseWait can take effect.
        //delaying it is the only solution I've found... :-(
        setTimeout(function () {
            $.ajax({
                async: false,
                type: "POST",
                url: "awsDiscovery.aspx/wmGetAWSObjectList",
                data: '{"sObjectType":"' + object_type + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (msg) {
                    $("#update_success_msg").fadeOut(2000);

                    $("#results_label").html(object_label);
                    $("#results_list").html(msg.d);

                    //set the cloud object type on a hidden field so we can use it later
                    $("#hidCloudObjectType").val(object_type);

                    initJtable(true, false);
                    hidePleaseWait();

                },
                error: function (response) {
                    $("#update_success_msg").fadeOut(2000);
                    showAlert(response.responseText);
                    hidePleaseWait();
                }
            });
        }, 250);
    });
$("#ctl00_phDetail_ddlEcosystems").empty();
});

function pageLoad() {
    //the add button
    $("#add_to_ecosystem_btn").button({ icons: { primary: "ui-icon-plus"} });

    //the page is ready... go populate the first tab.
    //with a delay... for some reason it wasn't firing without the delay
    setTimeout("$('.group_tab_selected').trigger('click')", 250);
}

function CloudAccountWasChanged() {
    location.reload();
}
//end copy from manage


function Save() {
    var bSave = true;
    var strValidationError = '';

    //some client side validation before we attempt to save the user

    //we will test it, but really we're not gonna use it rather we'll get it server side
    //this just traps if there isn't one.
    if ($("#ctl00_ddlCloudAccounts").val() == null || $("#ctl00_ddlCloudAccounts").val() == "") {
        bSave = false;
        strValidationError += 'No Cloud Accounts are defined. An Administrator must first create at least one Cloud Account.<br /><br />';
    }
    
    var ecosystem = $("#ctl00_phDetail_ddlEcosystems").val();
    var object_type = $("#hidCloudObjectType").val();
    var object_ids = $("#hidSelectedArray").val();

    if (ecosystem == null || ecosystem == "") {
        bSave = false;
        strValidationError += 'At least one Ecosystem is required.<br /><br /><a href="ecosystemManage.aspx">Click here</a> to manage Ecosystems.<br /><br />';
    }
    if (object_ids == null || object_ids == "") {
        bSave = false;
        strValidationError += 'At least one Cloud Object must be selected.<br /><br />';
    }
    if (object_type == null || object_type == "") {
        bSave = false;
        strValidationError += 'Error: Cannot determine the Cloud Object Type.';
    }

    if (bSave != true) {
        showInfo(strValidationError, "", true);
        return false;
    }

    $.ajax({
        async: false,
        type: "POST",
        url: "uiMethods.asmx/wmAddEcosystemObjects",
        data: '{"sEcosystemID":"' + ecosystem + '","sObjectType":"' + object_type + '","sObjectIDs":"' + object_ids + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $("#update_success_msg").text("Add Successful.").show();
            //reload the grid so it will show the proper ecosystems (and uncheck everything)
            $(".group_tab_selected").trigger("click");
        },
        error: function (response) {
            showAlert('Error: Unable to add Cloud Object to Ecosystem.' + response.responseText);
            return false;
        }
    });

}
function ClearSelectedRows() {
    $("#hidSelectedArray").val("");
    $("#lblItemsSelected").html("0");
    $(':input', (".jtable")).attr('checked', false);
}
