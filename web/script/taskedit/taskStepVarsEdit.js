$(document).ready(function () {
    //specific field validation and masking
    $(".var_name").live("keypress", function (e) { return restrictEntryToIdentifier(e, this); });
    $("#new_delimited_position").keypress(function (e) { return restrictEntryToInteger(e, this); });

    $("#error_dialog").dialog({
        autoOpen: false,
        draggable: false,
        resizable: false,
        bgiframe: true,
        modal: true,
        overlay: {
            backgroundColor: '#000',
            opacity: 0.5
        },
        buttons: {
            Ok: function () {
                $(this).dialog('close');
            }
        }
    });

    $("#info_dialog").dialog({
        autoOpen: false,
        draggable: false,
        resizable: false,
        bgiframe: true,
        modal: false
    });

    $("#delimiter_picker_dialog").dialog({
        autoOpen: false,
        draggable: false,
        resizable: false,
        bgiframe: true,
        modal: true
    });

    //the change event of one of the variable name fields does a quick validation check
    //against all the other value fields.
    //(because you can't have two of the same values)
    $("#edit_variables .var_name").live("change", function () {
        checkForNameConflicts()
    });

    //the change event of the value fields, specifically to do some validation testing
    //left properties
    $("#edit_variables .prop").live("change", function () {
        validateParsedVar(this);
    });

    //DELIMITED DIALOG
    $("#variable_add_dialog_delimited").dialog({
        autoOpen: false,
        draggable: false,
        resizable: false,
        bgiframe: true,
        modal: true,
        width: 400,
        buttons: {
            "Add": function () {
                addDelimitedVar();
            },
            "Close": function () {
                $(this).dialog('close');
                $("#delimited_msg").html("");
            }
        }
    });
    $("#new_delimited_var_name").keypress(function (e) {
        if (e.which == 13) {
            addDelimitedVar();
        }
    });
    $("#new_delimited_position").keypress(function (e) {
        if (e.which == 13) {
            addDelimitedVar();
        }
    });

    //END DELIMITED

    //PARSED DIALOG
    $("#variable_add_dialog_parsed").dialog({
        autoOpen: false,
        draggable: false,
        resizable: false,
        bgiframe: true,
        modal: true,
        width: 400,
        buttons: {
            OK: function () {
                addParsedVar();
            },
            Cancel: function () {
                $(this).dialog('close');
                $("#parsed_msg").html("");
            }
        }
    });
    $("#new_parsed_var_name").keypress(function (e) {
        if (e.which == 13) {
            addParsedVar();
        }
    });
    $("#new_parsed_l_val").keypress(function (e) {
        if (e.which == 13) {
            addParsedVar();
        }
    });
    $("#new_parsed_r_val").keypress(function (e) {
        if (e.which == 13) {
            addParsedVar();
        }
    });

    $("[name=parsed_type]").change(function () {
        $("#new_parsed_l_val").val("");
        $("#new_parsed_r_val").val("");
        $("#new_parsed_l_label").text("");
        $("#new_parsed_r_label").text("");

        $("#parsed_lmode_div").addClass("hidden");
        $("#parsed_rmode_div").addClass("hidden");

        switch ($(this).val()) {
            case "range":
                $("#new_parsed_r_val").removeClass("hidden");
                $("#parsed_lmode_div").removeClass("hidden");
                $("#parsed_rmode_div").removeClass("hidden");
                $("#new_parsed_l_label").text("Begin Position: ");
                $("#new_parsed_r_label").text("End Position: ");
                break;
            case "delimited":
                $("#new_parsed_r_val").addClass("hidden");
                $("#new_parsed_l_label").text("Column Position:");
                break;
            case "regex":
                $("#new_parsed_r_val").addClass("hidden");
                $("#new_parsed_l_label").text("Regular Expression:");
                break;
            case "xpath":
                $("#new_parsed_r_val").addClass("hidden");
                $("#new_parsed_l_label").text("Xpath:");
        }
    });
    //END PARSED

    //clear out all the variables
    $("#variable_clearall_btn").click(function () {
        if (confirm("Clear all Variables?"))
            $("#edit_variables").empty();

    });

    //bind the delimiter picker to the select buttons
    $("[name=delimiter_picker_btn]").click(function () {
        $("#delimiter_picker_target").val($(this).attr("target"));
        $("#delimiter_picker_dialog").dialog("open");
    });

    //bind the delimiter clear to the select buttons
    $("[name=delimiter_clear_btn]").click(function () {
        var target = $(this).attr("target");
        if (target == 'row') {
            $("#ctl00_phDetail_hidRowDelimiter").val("");
            $("#output_row_delimiter_label").html("N/A");
        }
        else if (target == 'col') {
            $("#ctl00_phDetail_hidColDelimiter").val("");
            $("#output_col_delimiter_label").html("N/A");
        }
    });

    //bind the delimiter picker to the select buttons
    $(".delimiter").click(function () {
        if ($("#delimiter_picker_target").val() == 'row') {
            $("#ctl00_phDetail_hidRowDelimiter").val($(this).attr("val"));
            $("#output_row_delimiter_label").html($(this).html());
        }
        else if ($("#delimiter_picker_target").val() == 'col') {
            $("#ctl00_phDetail_hidColDelimiter").val($(this).attr("val"));
            $("#output_col_delimiter_label").html($(this).html());
        }

        $("#delimiter_picker_dialog").dialog('close');
    });

    //bind the variable add button
    $("#variable_add_btn").click(function () {
        var opm = $("#ctl00_phDetail_hidOutputParseType").val();
        switch (opm) {
            case "1": //delimited
                $("#new_delimited_var_name").val("");
                $("#variable_add_dialog_delimited").dialog("open");
                break;
            case "2": //parsed
                $("#new_parsed_l_val").val("");
                $("#new_parsed_r_val").val("");
                $("#new_parsed_var_name").val("");

                $("#variable_add_dialog_parsed").dialog("open");
                $("#new_parsed_var_name").focus();
                break;
        }
    });

    //the variable delete button
    $(".variable_delete_btn").live("click", function (event) {
        $("#" + $(this).attr("remove_id")).remove();
        checkForNameConflicts();
    });

    //the save button binding
    $("#save_btn").click(function () {
        doUpdate();
    });
    //the cancel button binding
    $("#cancel_btn").click(function () {
        self.close();
    });
});
function checkForNameConflicts() {
    $("#edit_variables .var_name").removeClass("conflicted_value");

    $("#edit_variables .var_name").each(
        function (intIndex) {
            var $me = $(this);

            $("#edit_variables .var_name").each(
            function (intIndex) {
                var $other = $(this);

                //all others but me
                if ($me.attr("id") == $other.attr("id"))
                    return;

                if ($me.val() == $other.val()) {
                    $other.addClass("conflicted_value");
                    $me.addClass("conflicted_value");
                }
            });
        });
}
function validateParsedVar(ctl) {
    var id_part = $(ctl).attr("refid");

    var $msg = $("#" + id_part + "_msg");
    $msg.html("");

    var $l = $("#" + id_part + "_l_prop");
    var l_val = $l.val();
    var l_mode = $("[name=" + id_part + "_l_mode]:checked").val();

    $l.removeClass("conflicted_value");

    var $r = $("#" + id_part + "_r_prop");
    var r_val = $r.val();
    var r_mode = $("[name=" + id_part + "_r_mode]:checked").val();

    $r.removeClass("conflicted_value");

    //some validation rules based on the 'mode'
    if (l_mode == "index") {
        var l_int = parseInt(l_val);
        if (isNaN(l_int)) {
            $msg.append("Begin position must be a positive number. ");
            $l.addClass("conflicted_value");
            //return false;
        } else {
            if (l_int < 0) {
                $msg.append("Begin position must be a positive number. ");
                $l.addClass("conflicted_value");
                //return false;
            }
        }

        //test the right index inside the left, because this rule is dependent.
        if (r_mode == "index") {
            var r_int = parseInt(r_val);

            if (r_int < l_int) {
                $msg.append("End position must be greater than or equal to the Begin position. ");
                $r.addClass("conflicted_value");
                //return false;
            }
        }
    }

    if (r_mode == "index") {
        var r_int = parseInt(r_val);

        if (isNaN(r_int)) {
            if (r_val.toLowerCase() != "end") {
                $msg.append("End position must be a positive number or the keyword 'end'. ");
                $r.addClass("conflicted_value");
                //return false;
            }
        } else {
            if (r_int < 0) {
                $msg.append("End position must be a positive number. ");
                $r.addClass("conflicted_value");
                //return false;
            }
        }
    }

}
function addDelimitedVar() {
    $("#delimited_msg").html("");

    varname = $("#new_delimited_var_name").val();
    position = $("#new_delimited_position").val();

    //===== can't add nothing
    if (!varname || !position) {
        $("#delimited_msg").html("A variable name and values are required.");
        return false;
    }

    //can't add a var name that already exists
    var exists = false;

    $("#edit_variables .var_unique").each(
        function (intIndex) {
            var other = $(this).val();
            if (varname == other) {
                $("#delimited_msg").html("Another variable by that name already exists on this step.<br />Please choose another.");
                //can't return from the parent function here, so we set a flag
                exists = true;
                return false;
            }
        });

    if (exists)
        return false;
    //===== end can't add


    //need a unique id for this new row, because many will be added before committing
    var now = new Date();
    var vid = now.getTime();

    var foo;
    foo = "<li id=\"v" + vid + "\" class=\"variable\" var_type=\"delimited\">";
    foo += "<span class=\"variable_name\">";
    foo += "Variable:" +
                    " <input type=\"text\" value=\"" + varname + "\" id=\"v" + vid + "_name\"" +
                    " class=\"code var_name var_unique\" />";
    foo += "</span>";
    foo += "<span class=\"variable_delete_btn\" remove_id=\"v" + vid + "\">" +
                    "<img src=\"../images/icons/fileclose.png\" alt=\"\" /></span>";
    foo += "<br /><span class=\"variable_detail\">" +
                            "will contain the data from column position: " +
                            " <input type=\"text\" class=\"w100px code\" id=\"v" + vid + "_l_prop\"" +
                            " value=\"" + position + "\" validate_as=\"posint\" />.</span>";

    //an error message placeholder
    foo += "<br /><span id=\"v" + vid + "_msg\" class=\"var_error_msg\"></span>";

    foo += "</li>";

    $("#edit_variables").append(foo);

    $("#new_delimited_var_name").val("");
    $("#new_delimited_position").val("");
    $("#new_delimited_var_name").focus();

}

function addParsedVar() {
    //need a unique id for this new row, because many will be added before committing
    var now = new Date();
    var vid = now.getTime();

    $("#parsed_msg").html("");

    var typ = $("[name=parsed_type]:checked").val();
    var varname = $("#new_parsed_var_name").val();

    var l_val = $("#new_parsed_l_val").val();
    var r_val = $("#new_parsed_r_val").val();

    //=====can't add nothing
    if (!varname || !l_val) {
        $("#parsed_msg").html("A variable name and values are required.");
        return false;
    }

    //===== Warn if the "field break" identifier is not set.
    var cold = $("#ctl00_phDetail_hidColDelimiter").val();
    if (cold < 9) { //9 is the lowest value in the picker list
        if (typ == "delimited") {
            $("#parsed_msg").html("You have defined a delimited type variable, but no Field Break Indicator is set.\n\nPlease set a Field Break Indicator.");
            $("#output_col_delimiter_label").css("border-color", "red");
        }
    }

    //can't add a var name that already exists
    var exists = false;

    $("#edit_variables .var_unique").each(
        function (intIndex) {
            var other = $(this).val();
            if (varname == other) {
                $("#parsed_msg").html("Another variable by that name already exists on this step.<br />Please choose another.");
                //can't return from the parent function here, so we set a flag
                exists = true;
                return false;
            }
        });

    if (exists)
        return false;
    //===== end can't add



    var foo;
    foo = "<li id=\"v" + vid + "\" class=\"variable\" var_type=\"" + typ + "\">";
    foo += "<span class=\"variable_name\">";
    foo += "Variable: <input type=\"text\" value=\"" + varname + "\" id=\"v" + vid + "_name\"" +
                            "  class=\"code var_name var_unique\" />";
    foo += "</span>";

    foo += "<span class=\"variable_delete_btn\" remove_id=\"v" + vid + "\">" +
                    "<img src=\"../images/icons/fileclose.png\" alt=\"\" /></span>";

    //switch here on the type
    switch (typ) {
        case "range":
            if (!r_val) {
                $("#parsed_msg").html("A variable name and values are required.");
                return false;
            }

            var l_mode = $("[name=parsed_lmode_rb]:checked").val();
            var r_mode = $("[name=parsed_rmode_rb]:checked").val();

            //some validation rules based on the 'mode'
            if (l_mode == "index") {
                var l_int = parseInt(l_val);

                if (isNaN(l_int)) {
                    $("#parsed_msg").html("Begin position must be a number.");
                    return false;
                } else {
                    if (l_int < 0) {
                        $("#parsed_msg").html("Begin position must be a positive number.");
                        return false;
                    }

                    //test the right index inside the left, because at least one rule is dependent.
                    if (r_mode == "index") {
                        var r_int = parseInt(r_val);

                        if (isNaN(r_int)) {
                            if (r_val.toLowerCase() != "end") {
                                $("#parsed_msg").html("End position must be a number or the keyword 'end'.");
                                return false;
                            }
                        } else {
                            if (r_int < 0) {
                                $("#parsed_msg").html("End position must be a positive number.");
                                return false;
                            }
                            if (r_int < l_int) {
                                $("#parsed_msg").html("End position must be greater than or equal to the Begin position.");
                                return false;
                            }
                        }
                    }
                }
            }
            //end validation rules

            //we can't leave both unchecked.  If 'idx' is checked that's fine, if not, check the other one
            var l_idx_checked = (l_mode == "index" ? " checked=\"checked\"" : "");
            var l_pos_checked = (l_idx_checked == "" ? " checked=\"checked\"" : "");

            var l_msg = "<input type=\"radio\" name=\"v" + vid + "_l_mode\" value=\"index\" " + l_idx_checked + " class=\"prop\" refid=\"v" + vid + "\" />" +
                " position / " +
                " <input type=\"radio\" name=\"v" + vid + "_l_mode\" value=\"string\" " + l_pos_checked + " class=\"prop\" refid=\"v" + vid + "\" />" +
                " prefix";

            var r_idx_checked = (r_mode == "index" ? " checked=\"checked\"" : "");
            var r_pos_checked = (r_idx_checked == "" ? " checked=\"checked\"" : "");
            var r_msg = "<input type=\"radio\" name=\"v" + vid + "_r_mode\" value=\"index\" " + r_idx_checked + " class=\"prop\" refid=\"v" + vid + "\" />" +
                " position / " +
                " <input type=\"radio\" name=\"v" + vid + "_r_mode\" value=\"string\" " + r_pos_checked + " class=\"prop\" refid=\"v" + vid + "\" />" +
                " suffix";

            foo += "<span class=\"variable_detail\">" +
                            "  will contain the output found between<br />" + l_msg +
                            "  <input type=\"text\" class=\"w100px code prop\" id=\"v" + vid + "_l_prop\"" +
                            "  value=\"" + l_val.replace('"', '&quot;') + "\" refid=\"v" + vid + "\" />" +
                            " and " + r_msg +
                            " <input type=\"text\" class=\"w100px code prop\" id=\"v" + vid + "_r_prop\"" +
                            "  value=\"" + r_val.replace('"', '&quot;') + "\" refid=\"v" + vid + "\" />.</span>";
            break;

        case "delimited":
            foo += " <span class=\"variable_detail\">" +
                            "will contain the data from column position: " +
                            " <input type=\"text\" class=\"w100px code\" id=\"v" + vid + "_l_prop\"" +
                            " value=\"" + l_val.replace('"', '&quot;') + "\" validate_as=\"posint\" />.</span>";
            break;

        case "regex":
            foo += " <span class=\"variable_detail\">" +
                            "will contain the result of the following regular expression: <br />" +
                            " <input type=\"text\" class=\"w98pct code\" id=\"v" + vid + "_l_prop\"" +
                            " value=\"" + l_val.replace('"', '&quot;') + "\" />.</span>";
            break;
        case "xpath":
            foo += " <span class=\"variable_detail\">" +
                            "will contain the Xpath: <br />" +
                            " <input type=\"text\" class=\"w98pct code\" id=\"v" + vid + "_l_prop\"" +
                            " value=\"" + l_val.replace('"', '&quot;') + "\" />.</span>";
            break;
    }

    //an error message placeholder
    foo += "<br /><span id=\"v" + vid + "_msg\" class=\"var_error_msg\"></span>";

    foo += "</li>";

    $("#edit_variables").append(foo);

    $("#new_parsed_var_name").val("");
    $("#new_parsed_l_val").val("");
    $("#new_parsed_r_val").val("");
    $("#new_parsed_var_name").focus();
}


function doUpdate() {
    //not doing anything if any of the fields are marked as conflicted
    if ($("#edit_variables .conflicted_value").length > 0) {
        showInfo("One or more values are invalid and need attention.")
        return false;
    }

    var step_id = $("#ctl00_phDetail_hidStepID").val();
    var opm = $("#ctl00_phDetail_hidOutputParseType").val();
    var rowd = $("#ctl00_phDetail_hidRowDelimiter").val();
    var cold = $("#ctl00_phDetail_hidColDelimiter").val();
    var vars = new Array();

    //build the variable array
    if ($("#edit_variables .variable").length > 0) {
        //we can select the variables by class
        //and we have put all the
        $("#edit_variables .variable").each(
        function (intIndex) {
            vars[intIndex] = new Array();

            var rowid = $(this).attr("id");

            //some types have only one property, others have two, here we're just grabbing both
            //handle missing elements
            var lprop = ($("#" + rowid + "_l_prop").length > 0 ? $("#" + rowid + "_l_prop").val() : "");
            var rprop = ($("#" + rowid + "_r_prop").length > 0 ? $("#" + rowid + "_r_prop").val() : "");

            //if it happens to be 'parsed' type, there will be two 'type' indicators on each var.
            //                var l_mode = $("[name=parsed_lmode_rb]:checked").val();
            var ltype = ($("[name=" + rowid + "_l_mode]").length > 0 ? $("[name=" + rowid + "_l_mode]:checked").val() : "");
            var rtype = ($("[name=" + rowid + "_r_mode]").length > 0 ? $("[name=" + rowid + "_r_mode]:checked").val() : "");

            vars[intIndex][0] = $("#" + rowid + "_name").val();
            vars[intIndex][1] = $(this).attr("var_type");
            vars[intIndex][2] = lprop;
            vars[intIndex][3] = rprop;
            vars[intIndex][4] = ltype;
            vars[intIndex][5] = rtype;
        }
        );
    }


    //do it!
    //if (vars.length > 0) {
    //Doing the Microsoft ajax call because the jQuery one doesn't work for arrays.
    PageMethods.wmUpdateVars(step_id, opm, rowd, cold, vars, OnSuccess, OnFailure);
    //}


}

function OnSuccess(result, userContext, methodName) {
    step_id = $("#ctl00_phDetail_hidStepID").val();
    self.opener.getStep(step_id, step_id, true);
    //            getVars();
    //            showInfo("Update Successful.");
    self.close();
}

// Callback function invoked on failure of the MS AJAX page method.
function OnFailure(error, userContext, methodName) {
    if (error !== null) {
        showAlert(error.get_message());
    }
}

function getVars() {
    step_id = $("#ctl00_phDetail_hidStepID").val();
    $.ajax({
        async: false,
        type: "POST",
        url: "taskStepVarsEdit.aspx/wmGetVars",
        data: '{"sStepID":"' + step_id + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (retval) {
            $("#edit_variables").replaceWith(retval.d);
        },
        error: function (response) {
            showAlert(response.responseText);
        }
    });
}//Copyright 2011 Cloud Sidekick
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

