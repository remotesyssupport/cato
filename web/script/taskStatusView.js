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

$(document).ready(function() {

	//stripe the list tables
    $("#tblAtsa tr").live("mouseover", function() {
        //alert('mouseover');
        $(this).addClass("row_over");
    });
    $("#tblAtsa tr").live("mouseout", function() {
        $(this).removeClass("row_over");
    });
    //stripe the list table
    //$("#tblManualStatus tr").live("mouseover", function() {
    //    //alert('mouseover');
    //    $(this).addClass("row_over");
    //});
    //$("#tblManualStatus tr").live("mouseout", function() {
    //    $(this).removeClass("row_over");
    //});
    //stripe the list table
    $("#tblAtsc tr").live("mouseover", function() {
        //alert('mouseover');
        $(this).addClass("row_over");
    });
    $("#tblAtsc tr").live("mouseout", function() {
        $(this).removeClass("row_over");
    });
    //stripe the list table
    //$("#tblManualComplete tr").live("mouseover", function() {
    //    //alert('mouseover');
    //    $(this).addClass("row_over");
    //});
    //$("#tblManualComplete tr").live("mouseout", function() {
    //    $(this).removeClass("row_over");
    //});
    //stripe the list table
    //$("#tblUnassignedAssets tr").live("mouseover", function() {
    //    //alert('mouseover');
    //    $(this).addClass("row_over");
    //});
    //$("#tblUnassignedAssets tr").live("mouseout", function() {
    //    $(this).removeClass("row_over");
    //});



    $("#left_tooltip_box_inner").corner("round 8px").parent().css('padding', '2px').corner("round 10px");

    // where to go when you click on an automated task row
    $("[tag='selectAutoTask']").live("click", function() {
        location.href = 'taskActivityLog.aspx?status=' + $(this).attr("status");
    });

    //mouseovers for the Unassigned table
    $("#tblUnassigned tr").live("mouseover", function() {
        //alert('mouseover');
        $(this).addClass("row_over");
    });
    $("#tblUnassigned tr").live("mouseout", function() {
        $(this).removeClass("row_over");
    });

    // where to go when you click on the unassigned asset row
    //$("[tag='selectUnassignedAssets']").live("click", function() {
    //    location.href = 'assetUnassignedEdit.aspx?status=' + $(this).attr("status");
    //});

});