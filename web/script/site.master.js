$(document).ready(function () {
    //NOTE: this is the main jQuery function that will execute
    //when the DOM is ready.  Things you want defined on 'page load' should 
    //go in here.

    //use this to define constants, set up jQuery objects, etc.

    $("ul.sf-menu").supersubs({
        minWidth: 18,   // minimum width of sub-menus in em units 
        maxWidth: 27,   // maximum width of sub-menus in em units 
        extraWidth: .5     // extra width can ensure lines don't sometimes turn over 
        // due to slight rounding differences and font-family 
    }).superfish();  // call supersubs first, then superfish, so that subs are 
    // not display:none when measuring. Call before initialising 
    // containing tabs for same reason.


    //the cloud accounts dropdown updates the server session
    $("#ctl00_ddlCloudAccounts").change(function () {
        $("#update_success_msg").text("Updating...").show();

        var account_id = $(this).val();
        $.ajax({
            type: "POST",
            async: false,
            url: "uiMethods.asmx/wmSetActiveCloudAccount",
            data: '{"sAccountID":"' + account_id + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                $("#update_success_msg").text("Update Successful").fadeOut(2000);

                //some pages have cloud content, but they are all different.
                //This expects a page to have the following function...
                //if it doesn't, nothing really happens after the ajax call.
                //if it does, it will do some sort of nice clean refresh logic.
                CloudAccountWasChanged();

                if (msg.d.length > 0) {
                    showInfo(msg.d);
                }
            },
            error: function (response) {
                showAlert(response.responseText);
            }
        });
    });
});


// Menu
$(function () {
    var tabContainers = $('div.tabs > div');
    tabContainers.hide().filter(':home').hide();
    $('div.tabs ul.tabNavigation a').click(function () {
        tabContainers.slideUp();
        tabContainers.filter(this.hash).slideDown();
        //$('div.tabs ul.tabNavigation a').removeClass('selected');
        //$(this).addClass('selected');
    }).filter(':first').click();
});
// End Menu


//the timer that keeps the heartbeat updated
window.setInterval(updateHeartbeat, 120000);

//and the jquery ajax it performs
function updateHeartbeat() {
    $.ajax({
        type: "POST",
        url: "../pages/uiMethods.asmx/wmUpdateHeartbeat",
        data: '{}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            if (msg.d.length > 0) {
                showAlert(msg.d);
            }
        },
        error: function (response) {
            //nothing to do here but throw you back to the login page.
            location.href = "../login.aspx?msg=Server+Session+has+expired+(7)."
            //&err=" + escape(response.responseText.replace(/(\r\n|\n|\r)/gm, ""));
        }
    });
}

//logging out
function logout() {
    $.ajax({
        type: "POST",
        url: "../pages/uiMethods.asmx/wmLogout",
        data: '{}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            location.href = "../login.aspx?"        },
        error: function (response) {
			showAlert(response.responseText);
        }
    });
}


