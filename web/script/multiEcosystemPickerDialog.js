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

//Shared functionality for the multi-select ecosystem picker dialog

$(document).ready(function() {
    //any page that includes this script will get the following dialog inner code
    //but the page requires a placeholder div... called "multi_ecosystem_picker_dialog"
    var d = '<center>' +
            '<span style="padding-left: 15px; padding-top: 5px; font-weight: bold;">Enter search criteria:' +
            '    <input id="multi_ecosystem_search_text" class="search_text" />' +
            '    <span id="search_ecosystems_btn" onclick="SearchEcosystems();">Search</span>' +
            '<hr />' +
            '<div style="text-align: left; height: 300px; overflow: auto;" id="multi_ecosystem_picker_results">' +
            '</div>' +
            '<input type="button" class="hidden" id="ecosystem_save_btn" value="Save" onclick="SaveMultiEcosystemSelection();return false;" />' +
            '<input type="button" value="Cancel" onclick="CloseMultiEcosystemPicker();return false;" />' +
        '</center>'

    $("#multi_ecosystem_picker_dialog").html(d);

    //init the dialog
    $("#multi_ecosystem_picker_dialog").dialog({
        title: 'Search for Ecosystems',
        autoOpen: false,
        modal: true,
        width: 600,
        height: 410,
        position: ['center', 100],
        close: function(event, ui) {
            $("#multi_ecosystem_search_text").val("");
            $("#multi_ecosystem_picker_results").empty();
        }
    });

    // enter key for ecosystem search
    $("#multi_ecosystem_search_text").live("keypress", function(e) {
        if (e.which == 13) {
            SearchEcosystems();
            return false;
        }
    });

    //if an ecosystem selection check box is checked, show the "save" button
    $("input[name='ecosystemcheckboxes']").live("click", function() {
        if ($("input[name='ecosystemcheckboxes']:checked").length > 0) {
            $("#ecosystem_save_btn").removeClass("hidden");
        } else {
            $("#ecosystem_save_btn").addClass("hidden");
        }
    });

});

function ShowMultiEcosystemPicker() {
    $("#multi_ecosystem_picker_dialog").dialog('open');
    $("#multi_ecosystem_search_text").focus();
}

function SearchEcosystems() {
    $("#multi_ecosystem_picker_dialog").block({ message: null, cursor: 'wait' });
    $.ajax({
        async: false,
        type: "POST",
        url: "../pages/uiMethods.asmx/wmEcosystemSearch",
        data: '{"sSearchText":"' + $("#multi_ecosystem_search_text").val() + '","bAllowMultiSelect":"true"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(msg) {
            $("#multi_ecosystem_picker_dialog").unblock();
            if (msg.d.length == 0) {
                //showAlert('Could not load external references.' + response.responseText);
                //No results found for search
            } else {
                $("#multi_ecosystem_picker_results").html(msg.d);
            }
        },
        error: function(response) {
            $("#multi_ecosystem_picker_dialog").unblock();
            showAlert(response.responseText);
        }
    });
}

function CloseMultiEcosystemPicker() {
    $("#multi_ecosystem_picker_dialog").dialog('close');
    $("#ecosystem_search_text").val('');
    $("#multi_ecosystem_picker_results").html('');
    
    return false;
}

