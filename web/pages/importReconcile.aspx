<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="importReconcile.aspx.cs"
    Inherits="Web.pages.importReconcile" MasterPageFile="~/pages/site.master" %>

<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <style type="text/css">
        .task_header
        {
            cursor: pointer;
            padding: 1px 2px 1px 4px;
            background-color: #d3d3d3;
        }
        .task_row
        {
            border: solid 2px #d3d3d3;
            margin: 0px 10px 2px 10px;
        }
    </style>

    <script type="text/javascript" src="../script/validation.js"></script>

    <script type="text/javascript">
        $(document).ready(function() {
            // clear the edit array
            $("#hidTaskArray").val("");

            $("#copy_dialog").hide();

            //If a version radio is selected... select the "Versions" main radio....
            $("input:radio[name=rbVersionType]").live("click", function(event) {
                $("input:radio[name=import_mode]")[1].checked = true;
            });

            //if the 'rename' radio is selected... unselect any versions
            $("input:radio[name=import_mode]").click(function(event) {
                $("input:radio[name=rbVersionType]").attr('checked', false);
            });

            //define dialogs
            $("#edit_dialog").dialog({
                autoOpen: false,
                modal: true,
                width: 500,
                buttons: {
                    OK: function() {
                        showPleaseWait();
                        UpdateRow();

                    },
                    Cancel: function() {
                        $(this).dialog('close');

                    }
                }
            });

            $("#btn_import").live("click", function() {
                if (confirm("Are you sure you want to Import the selected Items?\n\nThis cannot be undone.")) {
                    ImportSelected();
                }
            });

            // task check all selects all tasks
            $("#task_chkAll").live("click", function() {

                //now build out the array
                var lst = "";

                if (this.checked) {
                    this.checked = true;

                    // all tasks
                    $("[tag='task_chk']").attr("checked", true);
                    $("[tag='task_chk']").each(
                    function(intIndex) {
                        if (this.checked) {
                            lst += "'" + $(this).attr("task_id") + "',";
                        }
                    }
                    );

                    //chop off the last comma
                    if (lst.length > 0)
                        lst = lst.substring(0, lst.length - 1);

                    $("#hidTaskArray").val(lst);


                } else {
                    this.checked = false;
                    $("[tag='task_chk']").attr("checked", false);
                    $("#hidTaskArray").val('');
                }


            });

            //some tightness on the edit dialog

            //wrong, give them all a class and use that for the selector
            //            $("input[name=import_mode]").click(function() {
            //                if ($("input[name=import_mode]:checked").val() == "New") {
            //                    $("#ctl00_phDetail_txtEditTaskCode").removeAttr("disabled");
            //                    $("#ctl00_phDetail_txtEditTaskName").removeAttr("disabled");
            //                }

            //                if ($("inpu[name=import_mode]:checked").val() == "New Version") {
            //                    $("#ctl00_phDetail_txtEditTaskCode").attr("disabled", true);
            //                    $("#ctl00_phDetail_txtEditTaskName").attr("disabled", true);
            //                }

            //            });


            // by default first time in, all checkboxes are selected
            SelectAll();


        });
        function SelectAll() {

            var lst = "";

            $("#task_chkAll").attr("checked", false);

			// all tasks
            $("[tag='task_chk']").attr("checked", true);
            $("[tag='task_chk']").each(
                    function(intIndex) {
                        if (this.checked) {
                            lst += "'" + $(this).attr("task_id") + "',";
                        }
                    }
                    );

            //chop off the last comma
            if (lst.length > 0)
                lst = lst.substring(0, lst.length - 1);

            $("#hidTaskArray").val(lst);

        }
        function pageLoad() {
			initJtable(true, true);
		
            //this spins thru the check boxes on the page and builds the array.
            //yes it rebuilds the list on every selection, but it's fast.
            $("[tag='task_chk']").change(function() {
                var lst = "";

                $("[tag='task_chk']").each(
                function(intIndex) {
                    if (this.checked) {
                        lst += "'" + $(this).attr("task_id") + "',";
                    }
                }
                );

                //chop off the last comma
                if (lst.length > 0)
                    lst = lst.substring(0, lst.length - 1);

                $("#hidTaskArray").val(lst);
            });

            //what happens when you click a task row
            $("[tag='task']").click(function() {
                $("#edit_type").val("task");
                $("#edit_id").val($(this).parent().attr("task_id"));
                $("#edit_code").val($(this).parent().attr("task_code"));
                $("#edit_name").val($(this).parent().attr("task_name"));

                //check the right button
                //3-18-11 NSC wasn't working anyway...
                //var im = $(this).attr("import_mode");
                //$("#edit_dialog input[value='" + im + "']").attr("checked", true);

                // before opening the dialog, fill out the version information
                //do an if on me if I don't have it try my parent
                LoadVersions($(this).parent().attr("task_name"), "task");

                $("#edit_dialog").dialog("open");
            });
        }

        function LoadVersions(sName, sType) {
            $.ajax({
                type: "POST",
                url: "uiMethods.asmx/wmImportDrawVersion",
                data: '{"sName":"' + sName + '","sType":"' + sType + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function(msg) {

                    if (msg.d.length != 0) {
                        $("#divVersion").html(msg.d);
                    }

                },
                error: function(response) {
                    showAlert(response.responseText);
                }
            });

        }



        function UpdateRow() {
            var sType = $("#edit_type").val();
            var sID = $("#edit_id").val();
            var sNewName = $("#edit_name").val();
            var sNewCode = $("#edit_code").val();
            var sVersion = "1.000";
            var sImportMode = $("input[name=import_mode]:checked").val();

            if (sImportMode == "New Version") {
                var sVersionNumber = $("input:radio[name=rbVersionType]:checked").attr("version");
                if (sVersionNumber == null) {
                    hidePleaseWait();
                    showInfo('Select a new version.');
                    return false;
                }
                sVersion = sVersionNumber
            }


            // make sure we have all of the valid fields
            if (sType == 'Panel' || sType == 'SpyderApp') {
                if (sNewName == '') {
                    showInfo('Name is required.');
                    return false;
                }
            } else if (sNewName == '' || sNewCode == '') {
                showInfo('All values are required.');
                return false;
            }




            $.ajax({
                type: "POST",
                url: "uiMethods.asmx/wmUpdateImportRow",
                data: '{"sObjectType":"' + sType + '","sID":"' + sID + '","sNewCode":"' + sNewCode + '","sNewName":"' + sNewName + '","sVersion":"' + sVersion + '","sImportMode":"' + sImportMode + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function(msg) {

                    if (msg.d.length == 0) {
                        $("#edit_dialog").dialog('close');
                        // fire a reload click
                        $("#ctl00_phDetail_btnReload").click();

                        hidePleaseWait();
                        $("#divVersion").html('');
                    } else {
                        showAlert(msg.d);
                    }
                },
                error: function(response) {
                    showAlert(response.responseText);
                }
            });

        }



        function ImportSelected() {
            // clear all of the previous values
            var TaskArray = $("#hidTaskArray").val();
            if (TaskArray.length == 0) {
                showInfo('Select at least one item to import.');
                return false;
            }

            showPleaseWait();

            $.ajax({
                type: "POST",
                url: "uiMethods.asmx/wmImportSelected",
                data: '{"sTaskList":"' + TaskArray + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function(msg) {
                    if (msg.d.length == 0) {

                        $("#hidTaskArray").val("");

                        // fire a reload click
                        $("#ctl00_phDetail_btnReload").click();

                        hidePleaseWait();
                    } else {
                        showAlert(msg.d);
                    }
                },
                error: function(response) {
                    showAlert(response.responseText);
                }
            });
        }
    </script>

    <div style="display: none;">
        <input id="hidTaskArray" type="hidden" name="hidTaskArray" />
    </div>
    <div id="content">
        <asp:UpdatePanel ID="udpTaskList" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                The following items are in the import file. Select one or more items to import.<br />
                (Rows marked as
                <img src="../images/icons/status_unknown_16.png" alt="" />
                conflicted must be addressed before they can be imported. Click an item header to
                edit conflicts.)
                <hr />
                <input type="checkbox" class="chkbox" id="task_chkAll" /><b>TASKS:</b>
                <table class="jtable" cellspacing="1" cellpadding="1" width="99%">
                    <thead>
						<tr>
	                        <th class="chkboxcolumn">
	                        </th>
	                        <th>
	                            Task Code
	                        </th>
	                        <th>
	                            Task Name
	                        </th>
	                        <th>
	                            Version
	                        </th>
	                        <th>
	                            Conflict
	                        </th>
	                        <th>
	                            Import As
	                        </th>
	                    </tr>
					</thead>
	                <asp:Repeater ID="rpTasks" runat="server">
	                    <ItemTemplate>
	                        <tr class="row" task_id="<%#Eval("task_id")%>" task_code="<%#Eval("task_code")%>"
	                            version="<%#Eval("version")%>" import_mode="<%#Eval("import_mode")%>" task_name="<%#Eval("task_name")%>">
	                            <td class="chkboxcolumn">
	                                <%#Eval("checkbox")%>
	                            </td>
	                            <td class="pointer" tag="task">
	                                <%#Eval("task_code")%>
	                            </td>
	                            <td class="pointer" tag="task">
	                                <%#Eval("task_name")%>
	                            </td>
	                            <td class="pointer" tag="task">
	                                <%#Eval("version")%>
	                            </td>
	                            <td class="pointer" tag="task">
	                                <%#Eval("conflict")%>
	                            </td>
	                            <td class="pointer" tag="task">
	                                <%#Eval("import_mode")%>
	                            </td>
	                        </tr>
	                    </ItemTemplate>
	                </asp:Repeater>
                </table>
				<hr />
                <asp:Button ID="btnReload" OnClick="btnReload_Click" runat="server" CssClass="hidden" />
                Import the selected items.
                <input type="button" id="btn_import" value="Import" />
            </ContentTemplate>
        </asp:UpdatePanel>
    </div>
    <div id="left_panel">
        <div class="left_tooltip">
            <img src="../images/terminal-192x192.png" alt="" />
            <div id="left_tooltip_box_outer">
                <div id="left_tooltip_box_inner">
                        <img src="../images/tooltip.png" alt="" />
                        <div id="help_text">
                            Select an Export package to Import. This will be a .zip file format.</div>
                </div>
            </div>
        </div>
    </div>
    <div id="edit_dialog" class="hidden" title="Resolve Conflicts">
        <input type="radio" name="import_mode" value="New" checked="checked" />
        Rename and import as a new item.
        <br />
        <table width="100%">
            <tr>
                <td>
                    Code:
                </td>
                <td>
                    <input type="text" id="edit_code" class="usertextbox" style="width: 300px;" />
                </td>
            </tr>
            <tr>
                <td>
                    Name:
                </td>
                <td>
                    <input type="text" id="edit_name" class="usertextbox" style="width: 300px;" />
                </td>
            </tr>
        </table>
        <hr />
        <div id="version_selection">
            <input type="radio" name="import_mode" value="New Version" />
            Import as new Version of existing item.
            <br />
            Version:
            <div id="divVersion">
            </div>
        </div>
        <div id="overwrite_selection" class="hidden">
            <input type="radio" name="import_mode" value="Overwrite" />
            Overwrite existing.</div>
        <input id="edit_id" type="hidden" />
        <input id="edit_type" type="hidden" />
    </div>
</asp:Content>
