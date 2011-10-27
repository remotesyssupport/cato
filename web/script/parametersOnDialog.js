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

//Shared functionality for any page or dialog that captures parameter values.

$(document).ready(function () {
    $(".parameter_dialog_remove_btn").live('click', function () {
        $(this).parent().remove();
    });


    //adding a value on the launch dialog
    $(".parameter_dialog_add_btn").live("click", function () {
        //construct a new input section
        var output = "";
        output += "<div class=\"task_launch_parameter_value\">";
        output += "<textarea class=\"task_launch_parameter_value_input\" width=\"90%\" rows=\"1\"></textarea>";
        output += "<img class=\"parameter_dialog_remove_btn\" src=\"../images/icons/fileclose.png\" alt=\"Remove Value\" />";
        output += "</div>";

        //and add it to the dialog
        $(this).before(output);
    });
    
   
    //any change to a value at all sets the dirty flag
    $(".task_launch_parameter_value_input").live('change', function () {
        $(this).attr("dirty","true");
    });
    
    /*
	//encrypted fields are a pair of fields... 
	$(".encunderlay").live("keydown", function(event) {
		var $ovr = $(this).next(".encoverlay");
		var stars = "";
		
		//length of the text?
		var ln = $(this).val().length;
		for (i=0;i<=ln;i++)
		{
			stars += "*";
		}

		//setTimeout(function() {
			$ovr.text(stars);
		//}, 50);

	});
	*/

});

function DrawParameterEditForm(parameter_xml) {
    var output = "";

    //not really xpath but it works!   an array of the <parameter> nodes 
    var $param = $(parameter_xml).find("parameter");

    //for each "parameter"
    $($param).each(function (pidx, p) {

        if ($(p)) {
            var parameter_id = $(p).attr("id");
            var required = $(p).attr("required");
            var prompt = $(p).attr("prompt");
            var parameter_name = $(p).find("name").text();
            var parameter_desc = $(p).find("desc").text().replace(/\"/g, "");  //take out any double quotes so we don't bust the title attribute
            var $values = $(p).find("value");
            //how do we display the values? (not part of the array but an attribute of the "values" node)
            var present_as = $(p).find("values").attr("present_as");
			var encrypt = null;
			
            var encryptattr = "";
            if ($(p).attr("encrypt"))
            	if ($(p).attr("encrypt") == "true") {
            		encryptattr = "encrypt=\"true\"";
					encrypt = true;
				}

            //well, we basically show it unless prompt is false
            if (prompt != "false") {
                //show the required one slightly different
                var required_class = (required == "true" ? "task_launch_parameter_required" : "");

                output += "<div id=\"tlp" + parameter_id + "\" class=\"task_launch_parameter " + required_class + "\" present_as=\"" + present_as + "\"" + encryptattr + ">";
                output += "<div class=\"task_launch_parameter_name\">" + parameter_name + "</div>";

                //don't show tooltip icons for empty descriptions
                if (parameter_desc > "")
                    output += "<div class=\"task_launch_parameter_icons\">" +
                    "<span class=\"floatright ui-icon ui-icon-info parameter_help_btn\" title=\"" + parameter_desc + "\"></span>" +
                    "</div>";

                //values
                //if "present_as" is missing or invalid, default to a single "value"
                if (present_as == "dropdown") {
                    output += "<br />Select One: <select class=\"task_launch_parameter_value_input\">";

                    $($values).each(function (vidx, v) {
                        var is_selected = $(v).attr("selected");
                        var selected = (is_selected == "true" ? "selected=\"selected\"" : "");

                        output += "<option " + selected + ">" + $(v).text() + "</option>";
                    });

                    output += "</select>";
                }
                else if (present_as == "list") {
                    $($values).each(function (vidx, v) {
						var attr = "";
		
                        output += "<div class=\"task_launch_parameter_value\">";

	                    if (encrypt) {
	                    	//what's the oev?
				            if ($(v).attr("oev"))
			            		attr = "oev=\"" + $(v).attr("oev") + "\"";
                        }

                    	//the actual textarea
                    	//don't break it to multiple lines or it adds spaces!
						output += "<textarea class=\"task_launch_parameter_value_input\" rows=\"1\" " + attr + ">" + $(v).text() + "</textarea>";

                                                                                                                                                
                        //don't draw the 'x' on the first value... make at least one value required.
                        if (vidx > 0) {
                            output += "<span class=\"floatright ui-icon ui-icon-trash parameter_dialog_remove_btn\" title=\"Remove Value\"></span>";
                            //output += "<img class=\"parameter_dialog_remove_btn\" src=\"../images/icons/fileclose.png\" alt=\"Remove Value\" />";
                        }

                        output += "</div>";
                    });

                    //draw an add link.
                    output += "<div class=\"parameter_dialog_add_btn pointer\" add_to=\"" + parameter_id + "\"><img style=\"width:10px; height:10px;\" src=\"../images/icons/edit_add.png\" alt=\"\" title=\"Add another value.\" />( click to add another value )</div>";
                }
                else {
                    //if there happen to be more than one defined somehow... too bad.  Just show the first one
                    $($values).each(function (vidx, v) {
                        //break out after the first one
                        if (vidx > 0)
                            return false;

                        var attr = "";
                        
                        output += "<div class=\"task_launch_parameter_value\">";

	                    //if it's "encrypt", draw a hidden field, and flag the entry one for input masking
	                    //NOTE: any value here will be encrypted because it came from the database.
	                    
	                    //MORE IMPORTANT NOTE: if the value is changed by the user, it will NOT be encrypted any more
	                    //see the document.ready for the binding of the change event that sets a dirty flag in this case.
	                    
	                    if (encrypt) {
	                    	//what's the oev?
				            if ($(v).attr("oev"))
			            		attr = "oev=\"" + $(v).attr("oev") + "\"";

	                    	
            				//TODO: PARAMS: hidden field/masking crap
	                    	//ALL THIS IS A GOOD IDEA... just will take hours of tinkering to get it right.
	                    	
	                    	//give the main textarea the "encunderlay" class
	                    	/*
	                    	var stars = "";
							var ln = $(v).text().length;
							for (i=0;i<=ln;i++) 
							{
								stars += "*";
							}

	                    	//and this covering div lies over it
	                    	output += "<textarea class=\"encoverlay\">" + stars + "</textarea>";
	                    	*/
                    	}
	                    
                    	//the actual textarea
						output += "<textarea class=\"task_launch_parameter_value_input\" rows=\"1\" " + attr + ">" + $(v).text() + "</textarea>";

                        output += "</div>";
                    });
                }

                output += "</div>";
            }
        }
    });

    return output;
}


function buildXMLToSubmit() {
    var xml = "<parameters>\n";
	
    $(".task_launch_parameter").each(function (pidx, p) {
        if ($(p)) {
            var parameter_id = $(p).attr("id").replace(/tlp/, ""); ;
            var required = $(p).hasClass("task_launch_parameter_required");
            var parameter_name = $(p).find(".task_launch_parameter_name").text();
            var $values = $(p).find(".task_launch_parameter_value_input");
            var present_as = $(p).attr("present_as");

            var encryptattr = "";
            var encrypt = false;
            if ($(p).attr("encrypt"))
            	if ($(p).attr("encrypt") == "true") {
            		encryptattr = "encrypt=\"true\"";
					encrypt = true;
				}

            //note: no need to save "prompt" ... it wouldn't be here if it wasn't and 
            //looking at previous parameters the dialog will always show parameters without a prompt attribute.

            xml += "<parameter id=\"" + parameter_id + "\" required=\"" + required + "\" " + encryptattr + ">\n";
            xml += "<name>" + parameter_name + "</name>\n";

            //values
            xml += "<values present_as=\"" + present_as + "\">\n";
            $($values).each(function (vidx, v) {
            	var val = $(v).val();;
            	var attr = "";
            	//TODO: PARAMS: hidden field/masking crap (remove after testing)
  	            
  	            if (encrypt) {
	  	            var is_dirty = false;
		            if ($(v).attr("dirty"))
	            		is_dirty = true;
	
	            	//if the value textarea has the "encrypt" flag, it will also have an associated json packed attribute...
	            	//use that hidden attribute instead, unless the value is dirty.
					if (is_dirty) {
						//this is a new value, needs to be encrypted on the server.
		                attr = " do_encrypt=\"true\"";
					} else {
			            if ($(v).attr("oev"))
			            {
							attr = " oev=\"true\"";
							val = $(v).attr("oev");
	            		}
					}
				}

                xml += "<value" + attr + ">";
                xml += val
                xml += "</value>\n";
            });
            
            xml += "</values>\n";
            xml += "</parameter>\n";
        }
    });

    xml += "</parameters>";

    return xml;
}

function bindParameterToolTips() {
    $(".parameter_help_btn").tipTip({
        defaultPosition: "bottom",
        keepAlive: false,
        activation: "hover",
        maxWidth: "400px",
        fadeIn: 100
    });
}
