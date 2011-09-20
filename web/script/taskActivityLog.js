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
    $("#item_search_btn").live("click", function () {
        //until we redo this all as ajax, our pretty button clicks the hidden ugly one
        $("#ctl00_phDetail_btnSearch").click();
    });

    // where to go on row click
    $("[tag='selectable']").live("click", function () {
        openDialogWindow('taskRunLog.aspx?task_instance=' + $(this).parent().attr("task_instance"), 'TaskRunLog', 950, 750, 'true');
    });
});

function pageLoad() {
    $("#item_search_btn").button({ icons: { primary: "ui-icon-search"} });
    initJtable(true, false);
    $.unblockUI();
}