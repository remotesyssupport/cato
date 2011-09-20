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

});

function pageLoad() {
    $("#left_tooltip_box_inner").corner("round 8px").parent().css('padding', '2px').corner("round 10px");
}

function isNumeric(value) {
    if (value.match(/^\d+$/) == null)
        return false;
    else
        return true;
}

function ValidateValues() {

    var bValid = true;
    var strError = "";
    // do validation here
    //txtRetryDelay
    //Must be between 0 and 9999
    if (!isNumeric($("#ctl00_phDetail_txtRetryDelay").val())) {
        bValid = false;
        strError += "Retry Delay must be between 0 and 9999<br />";
    } else if ($("#ctl00_phDetail_txtRetryDelay").val() < 0 || $("#ctl00_phDetail_txtRetryDelay").val() > 9999) {
        bValid = false;
        strError += "Retry Delay must be between 0 and 9999<br />";
    }


    //txtRetryMaxAttempts
    if (!isNumeric($("#ctl00_phDetail_txtRetryMaxAttempts").val())) {
        bValid = false;
        strError += "Retry Attempts must be between 0 and 9999<br />";
    } else if ($("#ctl00_phDetail_txtRetryMaxAttempts").val() < 0 || $("#ctl00_phDetail_txtRetryMaxAttempts").val() > 9999) {
        bValid = false;
        strError += "Retry Attempts must be between 0 and 9999<br />";
    }

    //txtRefreshMinutes
    if (!isNumeric($("#ctl00_phDetail_txtRefreshMinutes").val())) {
        bValid = false;
        strError += "Refresh Minutes must be between 0 and 9999<br />";
    } else if ($("#ctl00_phDetail_txtRefreshMinutes").val() < 0 || $("#ctl00_phDetail_txtRefreshMinutes").val() > 9999) {
        bValid = false;
        strError += "Refresh Minutes must be between 0 and 9999<br />";
    }

    //txtSMTPServerPort
    if (!isNumeric($("#ctl00_phDetail_txtSMTPServerPort").val())) {
        bValid = false;
        strError += "SMTP Server Port must be between 0 and 9999<br />";
    } else if ($("#ctl00_phDetail_txtSMTPServerPort").val() < 0 || $("#ctl00_phDetail_txtSMTPServerPort").val() > 9999) {
        bValid = false;
        strError += "SMTP Server Port must be between 0 and 9999<br />";
    }

    //txtSMTPUserPassword
    if ($("#ctl00_phDetail_txtSMTPUserPassword").val() != $("#ctl00_phDetail_txtSMTPPasswordConfirm").val()) {
        bValid = false;
        strError += "Passwords do not match<br />";
    }

    if (!bValid) {
        showAlert(strError);
        return false;
    } else {
    
        // save here.
        SaveNotificationSettings();
        //return true;
    }

}
function SaveNotificationSettings() {

    var stuff = new Array();
    stuff[0] = $("#ctl00_phDetail_ddlMessengerOnOff").val();
    stuff[1] = $("#ctl00_phDetail_txtPollLoop").val();
    stuff[2] = $("#ctl00_phDetail_txtRetryDelay").val();
    stuff[3] = $("#ctl00_phDetail_txtRetryMaxAttempts").val();
    stuff[4] = $("#ctl00_phDetail_txtRefreshMinutes").val();
    stuff[5] = $("#ctl00_phDetail_txtSMTPServerAddress").val();
    stuff[6] = $("#ctl00_phDetail_txtSMTPUserAccount").val();
    stuff[7] = $("#ctl00_phDetail_txtSMTPUserPassword").val();
    stuff[8] = $("#ctl00_phDetail_txtSMTPServerPort").val();
    stuff[9] = $("#ctl00_phDetail_txtFromEmail").val();
    stuff[10] = $("#ctl00_phDetail_txtFromName").val();
    stuff[11] = $("#ctl00_phDetail_txtAdminEmail").val();

    if (stuff.length > 0) {
        PageMethods.SaveNotifications(stuff, OnUpdateSuccess, OnUpdateFailure);
    } else {
        showAlert('incorrect list of update attributes:' + stuff.length.toString());
    }

}
function OnUpdateSuccess(result, userContext, methodName) {
    if (methodName == "SaveNotifications") {
        showInfo(result);
    }

}
function OnUpdateFailure(error, userContext, methodName) {
    if (error !== null) {
        showAlert(error.get_message());
    }
}
