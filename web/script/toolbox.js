//tabs appear on the many pages.  Shared code.

//Tabs are simply hidden divs already drawn on the page.
//their content may be ajax... that's up to the specific page.

//at the least, clicking a tab a) hides all other tabs and b) shows the one that was clicked.

//to support additional custom functionality, IF a page
//has a "tabWasClicked" function, it will also be fired.

//gives the ability to have page-specific tab enhancements.

$(document).ready(function () {
    $("#toolbox .toolbox_panel").height($("#left_panel_te").height() - $("#toolbox_tabs").height() - 5);

    //the tab toggle click event
    $("#toolbox_tabs .toolbox_tab").click(function () {
        //we only do stuff if this tab isn't already selected.
        //if it is, do nothing
        if ($(this).hasClass("ui-tabs-selected ui-state-active"))
            return false;


        //hide 'em all
        $("#toolbox .toolbox_panel").addClass("hidden");
        $("#toolbox .toolbox_tab").removeClass("ui-tabs-selected ui-state-active");

        //show the one you clicked
        d = $(this).attr("linkto");
        $("#" + d).removeClass("hidden");
        $(this).addClass("ui-tabs-selected ui-state-active");

        //does the page have a tabClick handler function?
        if (typeof tabWasClicked == 'function') {
            tabWasClicked($(this).attr("id").replace(/tab_/, "").replace(/ctl00_phDetail_/, ""));
        }
    });

    //was there a directive to show a specific tab?  click it!
    var tab = getQuerystringVariable("tab");
    if (tab != "") {
        if ($("#div_" + tab).length != 0) {
            $("#div_" + tab).click();
        }
    }

});
