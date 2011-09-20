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


//dynamic field validation based on custom xhtml tags
$(document).ready(function() {
    $(":input[validate_as='posint']").live("keypress", function(e) {
        return restrictEntryToPositiveInteger(e, this);
    });
    $(":input[validate_as='posnum']").live("keypress", function(e) {
        return restrictEntryToPositiveNumber(e, this);
    });
    $(":input[validate_as='variable']").live("keypress", function(e) {
        return restrictEntryCustom(e, this, /[\[\]a-zA-Z0-9_\-,]/);
    });
    $(":input[validate_as='identifier']").live("keypress", function(e) {
        return restrictEntryToIdentifier(e, this);
    });
    $(":input[validate_as='email']").live("keypress", function(e) {
        return restrictEntryToEmail(e, this);
    });
    $(":input[validate_as='username']").live("keypress", function(e) {
        return restrictEntryToUsername(e, this);
    });
    $(":input[validate_as='hostname']").live("keypress", function(e) {
        return restrictEntryToHostname(e, this);
    });
    $(":input[validate_as='tag']").live("keypress", function(e) {
        return restrictEntryToTag(e, this);
    });


    // PROPRIETARY VALIDATIONS
    //These validations are specific to certain fields and business logic.
    //but we still wanted to handle and process them the same as more typical validations

    //an example of specific validation for handling a special case
    $(":input[validate_as='my_special_case']").live("keypress", function(e) {
        //do logic here...

        //then
        return;
    });

});

//some validations are not on a keypress basis, rather should happen on the blur event.
//these funcs are called based on field type from the onStepFieldChange function.

//this is the main function that determines how to test the syntax
function checkSyntax(syntax, strtocheck) {
    var msg = "";

    if (syntax != "") {
        switch (syntax) {
            case "asset_name_or_var":
                if (strtocheck != "") {
                    msg = validateAssetNameOrVar(strtocheck);
                    if (msg && msg != "")
                        return msg;
                }
                break;
            case "variable":
                if (strtocheck != "") {
                    msg = validateVariable(strtocheck);
                    if (msg && msg != "")
                        return msg;
                }
                break;
        }
    }

    return "";
}
function validateAssetNameOrVar(name) {
    //first, is it a variable?
    if (name.indexOf('[') > -1 || name.indexOf(']') > -1) {
        //someone typed a bracket, they must have meant for this to be a variable.  
        //so, do the end braces match?
        if (/\[\[.*]\]/.test(name)) {
            return "";
        }
    }

    //nope...

    //will do some ajax here to test the name in the db since it isn't a [[var]]
    var asset_id = "";

    $.ajax({
        async: false,
        type: "POST",
        url: "taskMethods.asmx/wmGetAssetIDFromName",
        data: '{"sName":"' + name + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(retval) {
            //the web method returned the ID if it exists.
            if (retval.d != null)
                asset_id = retval.d;
        },
        error: function(response) {
            showAlert(response.responseText);
            return "";
        }
    });

    if (asset_id.length == 36)  //guids are 36 chars long
        return "";

    return "Entry must be a valid Asset Name, or a [[variable]].";
}
function validateVariable(name) {
    if (name.indexOf('[') > -1 || name.indexOf(']') > -1) {
        //someone typed a bracket, they must have meant for this to be a variable.  
        //so, do the end braces match?
        if (/\[\[.*]\]/.test(name)) {
            return "";
        }
    }

    return "Entry must be a [[variable]].";
}