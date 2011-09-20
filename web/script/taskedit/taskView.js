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

//some code is shared between taskEdit and taskView pages.  That shared code is in this file.
//because we dont' want to include taskEdit.js all over the place, it's full of stuff specific to that page.

$(document).ready(function () {
    //used a lot
    g_task_id = $("[id$='hidTaskID']").val();

    //jquery buttons
    $("#asset_search_btn").button({ icons: { primary: "ui-icon-search"} });

    //asset, print and show log links
    //the print link
    $("#print_link").live("click", function () {
        var url = "taskPrint.aspx?task_id=" + g_task_id;
        openWindow(url, "taskPrint", "location=no,status=no,scrollbars=yes,resizable=yes,width=800,height=700");
    });

    //the show log link
    $("#show_log_link").click(function () {
        var url = "securityLogView.aspx?type=3&id=" + g_task_id;
        openWindow(url, "logView", "location=no,status=no,scrollbars=yes,resizable=yes,width=800,height=700");
    });

    //the show runlog link
    $("#show_runlog_link").click(function () {
        var url = "taskRunLog.aspx?task_id=" + g_task_id;
        openWindow(url, "runLogView", "location=no,status=no,scrollbars=yes,resizable=yes,width=800,height=700");
    });

    //the show assets report link
    $("#show_assets_link").click(function () {
        var url = "taskAssets.aspx?task_id=" + g_task_id;
        openWindow(url, "taskAssets", "location=no,status=no,scrollbars=yes,resizable=yes,width=800,height=700");
    });

    //VERSION TOOLBOX
    $("#versions .version").disableSelection();
    //the onclick event of the 'version' elements
    $("#versions .version").live("click", function () {
        location.href = "taskEdit.aspx?task_id=" + $(this).attr("task_id") + "&tab=versions";
    });
    //whatever the current version is... change it's class in the list
    $("#v_" + $("#ctl00_phDetail_hidTaskID").val()).addClass("version_selected");

    //SCHEDULE TOOLBOX
    $("#div_schedules .schedule").live("click", function () {
        location.href = "scheduleEdit.aspx?schedule_id=" + $(this).attr("schedule_id");
    });

});

function tabWasClicked(tab) {
    //load on this page is taking too long.  So, we'll get the tab content on click instead.
    //NOTE: shared on several pages, so there might be some cases here that don't apply to all pages.
    //not a problem, they just won't be hit.

    if (tab == "parameters") {
        doGetParams("task", $("#ctl00_phDetail_hidTaskID").val());
    } else if (tab == "versions") {
        doGetVersions();
    } else if (tab == "schedules") {
    } else if (tab == "registry") {
        GetRegistry($("#ctl00_phDetail_hidOriginalTaskID").val());
    } else if (tab == "tags") {
        GetObjectsTags($("#ctl00_phDetail_hidOriginalTaskID").val());
    } else if (tab == "clipboard") {
        doGetClips();
    }
}
