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

//This file contains common functions use throughout the main web application.  Changes made in this
//file will have global impact, so please be careful.

//GLOBALS
isiPad = navigator.userAgent.match(/iPad/i) != null;

//used in many places where we send strings as json data, replaces the two critical json chars " and \
//THESE CALL a jQuery plugin
function packJSON(instr) {
	//if it's nothing, return nothing
	if (instr == "")
		return instr;
		
	//NOTE! base64 encoding still has a couple of reserved characters so we explicitly replace them AFTER
	var outstr = $.base64.encode(instr);
    return outstr.replace(/\//g, "%2F").replace(/\+/g, "%2B").replace(/=/g, "%3D");
}
function unpackJSON(instr) {
	//if it's nothing, return nothing
	if (outstr == "")
		return outstr;
		
	//NOTE! base64 decoding still has a couple of reserved characters so we explicitly replace them BEFORE
    var outstr = instr.replace(/%2F/g, "/").replace(/%2B/g, "+").replace(/%3D/g, "=");
	return $.base64.decode(outstr);
}



//this is a debugging function.
function printClickEvents(ctl) {
    var e = ctl.data("events").click;
    jQuery.each(e, function (key, handlerObj) {
        console.log(handlerObj.handler) // prints "function() { console.log('clicked!') }"
    });
}
function printKeypressEvents(ctl) {
    var e = ctl.data("events").keypress;
    jQuery.each(e, function (key, handlerObj) {
        console.log(handlerObj.handler) // prints "function() { console.log('clicked!') }"
    });
}

//this function shows the "please wait" blockui effect.
function showPleaseWait(msg) {
    var msg = ((msg == "" || msg === undefined) ? "<img src='../images/busy.gif' />Please Wait ..." : msg);
    $.blockUI({
        message: msg,
        css: {
            'background-position': 'center center',
            'background-repeat': 'no-repeat',
            'width': '200px',
            'height': '20px',
            'padding-top': '6px',
            '-webkit-border-radius': '10px',
            '-moz-border-radius': '10px',
            'font-size': '0.8em'
        },
        overlayCSS: {
            'backgroundColor': '#C0C0C0',
            'opacity': '0.6'
        }
    });
}
function hidePleaseWait() {
    $.unblockUI();
    document.body.style.cursor = 'default';
}


//PROTOTYPES
String.prototype.replaceAll = function (s1, s2) { return this.split(s1).join(s2) }

//SET UP ANY INSTANCES OF our "jTable" ... striped and themed 
function initJtable(stripe, hover) {
    stripe = ((stripe == "" || stripe === undefined) ? false : true);
    hover = ((hover == "" || hover === undefined) ? false : true);

    //Theme the tables with jQueryUI
    $(".jtable th").each(function () {
        $(this).addClass("col_header");
    });
    $(".jtable td").each(function () {
        $(this).addClass("row");
    });

    if (hover) {
        $(".jtable tr").hover(
        function () {
            $(this).children("td").addClass("row_over");
        },
        function () {
            $(this).children("td").removeClass("row_over");
        });
    }
    //    $(".jtable tr").click(function () {
    //        $(this).children("td").toggleClass("ui-state-highlight");
    //    });
    if (stripe) {
        $('.jtable tr:even td').addClass('row_alt');
    }
}

//THESE FUNCTION SWITCH from web formatting (<br>, &nbsp;, etc) into text formatting (\n, \t, etc)
function formatHTMLToText(s) {
    var replaceWith = '';
    //firefox (moz) uses carriage returns in text areas, IE uses newlines AND carriage returns.  
    if (ie) { replaceWith = '\r' } else { replaceWith = '\n' }
    s = s.replaceAll('<br />', replaceWith);
    s = s.replaceAll('<br>', replaceWith);
    s = s.replaceAll('<BR>', replaceWith);
    s = s.replaceAll('&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;', '\t');

    return s;
}

function formatTextToHTML(s) {
    var replaceFrom;
    //firefox (moz) uses carriage returns in text areas, IE uses newlines AND carriage returns.  
    if (ie) { replaceFrom = '\r' } else { replaceFrom = '\n' }
    s = s.replaceAll(replaceFrom, '<br />');
    s = s.replaceAll('\t', '&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;');

    return s;
}

//will scroll a page to the ID specified
function jumpToAnchor(anchor) {
    //here's what I'm doing... if the parent of me is "content_te"
    //which is a special scrolling div in the app... scroll it.
    //otherwise just try to scroll the page.

    var new_position = $('#' + anchor).offset();

    if ($("#content_te").length > 0)
        $("#content_te").scrollTo($('#' + anchor));
    else
        $.scrollTo($('#' + anchor));

    return false;
}


//------------------------------------------------------------
//Generic functions
function getQuerystringVariable(variable) {
    var querystring = location.search.substring(1);
    var vars = querystring.split("&");
    for (var i = 0; i < vars.length; i++) {
        var pair = vars[i].split("=");
        if (pair[0] == variable) {
            return pair[1];
        }
    }
    return null;
}


function AreYouSure() {
    if (confirm('Are you sure you want to delete this item?')) {
        return true;
    }
    return false;
}

//------------------------------------------------------------
//These are data entry validation functions.

function restrictEntryToSafeHTML(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (c == '<' || c == '>')
        return false;

    return true;
}

function restrictEntryToIdentifier(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (/[a-zA-Z0-9_\-]/.test(c)) {
        return true;
    }

    return false;
}

function restrictEntryToIdentifierUpper(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (/[A-Z0-9_\-]/.test(c)) {
        return true;
    }
    else {
        if (/[a-z]/.test(c)) {
            field.value += c.toUpperCase();
        }
        return false;
    }
}

function restrictEntryToUsername(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (/[\\a-zA-Z0-9_.@\-]/.test(c)) {
        return true;
    }
    else {
        return false;
    }
}

function restrictEntryToEmail(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (/[a-zA-Z0-9_.@,\-]/.test(c)) {
        return true;
    }
    else {
        return false;
    }
}
function restrictEntryToHostname(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (/[a-zA-Z0-9_.\-]/.test(c)) {
        return true;
    }
    else {
        return false;
    }
}
function restrictEntryToNumber(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (/[0-9.\-]/.test(c)) {
        //if user pressed '.', and a '. exists in the string, cancel
        if (c == '.' && field.value.indexOf('.') != -1)
            return false;

        //if there is anything in the field and user pressed '-', cancel.
        if (c == '-' && field.value.length > 0)
            return false;

        return true;
    }
    return false;
}

function restrictEntryToInteger(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (/[0-9\-]/.test(c)) {
        //if there is anything in the field and user pressed '-', cancel.
        if (c == '-' && field.value.length > 0)
            return false;

        return true;
    }
    return false;
}

function restrictEntryToPositiveNumber(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (/[0-9.]/.test(c)) {
        //if user pressed '.', and a '. exists in the string, cancel
        if (c == '.' && field.value.indexOf('.') != -1)
            return false;

        return true;
    }
    return false;
}

function restrictEntryToPositiveInteger(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (/[0-9]/.test(c)) {
        return true;
    }
    return false;
}

function restrictEntryToTag(e, field) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (/[a-zA-Z0-9_.\-@#& ]/.test(c)) {
        return true;
    }
    else {
        return false;
    }
}

function restrictEntryCustom(e, field, regex) {
    var c = whatKey(e);
    if (c.indexOf('~') != -1)
        return true;

    if (regex.test(c)) {
        return true;
    }
    return false;
}
function whatKey(e) {
    //this will return the actual character IF it was a character key
    //if not, it will return ~#, where # is the key code
    //so, pressing an 'n' will return n
    //and pressing backspace will return ~8

    //this way we can write code to test against the differences

    //OF COURSE, stymied again by Microsoft...
    //charCode is not recognized by IE... 
    //but it is a way in FF to tell character keys from action keys
    if (typeof (e.charCode) != "undefined") {
        //if the charCode isn't a 0, it is a real character key
        if (e.charCode != 0 && e.keyCode == 0)
            return String.fromCharCode(e.charCode);
        else // but if it is 0, it's an action key, so return something special and testable
            return "~" + e.keyCode;
    }

    if (!e.charCode && e.keyCode)
        return String.fromCharCode(e.keyCode);
}
//SOME FUNCTIONS DEALING with the size of a page
function getPageWidth() {
    if (window.innerWidth) {
        return window.innerWidth;
    }
    if (document.body.clientWidth) {
        return document.body.clientWidth;
    }
}

function getPageHeight() {
    if (window.innerHeight) {
        return window.innerHeight;
    }
    if (document.body.clientHeight) {
        return document.body.clientHeight;
    }
}

//and this one is wicked... given an input value (the width or height of an element)
//these will return the value to give the top or left in order to center it on the page.
//even if the page is scrolled!
function getPageCenteredTop(elem) {
    var st
    if (document.body.scrollTop) {
        st = document.body.scrollTop;
    }
    if (window.pageYOffset) {
        st = window.pageYOffset;
    }

    //in case the height is null (Firefox does this if you haven't scrolled yet.)
    if (!st) { st = 0 }

    var eh = getElementHeight(elem)
    var h = getPageHeight();

    return ((h - eh) / 2) + st;
}

function getPageCenteredLeft(elem) {
    var ew = getElementWidth(elem)
    var w = getPageWidth();
    return (w - ew) / 2;
}

function getElementHeight(elem) {
    if (elem.style.pixelHeight) xPos = elem.style.pixelHeight;
    if (elem.offsetHeight) xPos = elem.offsetHeight;

    return xPos;
}

function getElementWidth(elem) {
    if (elem.style.pixelHeight) xPos = elem.style.pixelWidth;
    if (elem.offsetHeight) xPos = elem.offsetWidth;

    return xPos;
}

//SIZE OF SCREEN FUNCTIONS
function getScreenCenteredTop(eh) {
    var h = screen.height;

    return (h - eh) / 2;
}

function getScreenCenteredLeft(ew) {
    var w = screen.width;
    return (w - ew) / 2;
}


//ELEMENT POSITION
function getLeft(obj) {
    var curleft = 0;
    if (obj.offsetParent) {
        while (1) {
            curleft += obj.offsetLeft;
            if (!obj.offsetParent) {
                break;
            }
            obj = obj.offsetParent;
        }
    } else if (obj.x) {
        curleft += obj.x;
    }
    return curleft;
}

function getTop(obj) {
    var curtop = 0;
    if (obj.offsetParent) {
        while (1) {
            curtop += obj.offsetTop;
            if (!obj.offsetParent) {
                break;
            }
            obj = obj.offsetParent;
        }
    } else if (obj.y) {
        curtop += obj.y;
    }
    return curtop;
}


//A couple more functions for resizing iframes.
function getDocHeight(doc) {
    var docHt = 0, sh, oh;
    if (doc.height) docHt = doc.height;
    else if (doc.body) {
        if (doc.body.scrollHeight) docHt = sh = doc.body.scrollHeight;
        if (doc.body.offsetHeight) docHt = oh = doc.body.offsetHeight;
        if (sh && oh) docHt = Math.max(sh, oh);
    }
    return docHt;
}
function setIframeHeight(iframeName) {
    var iframeWin = window.frames[iframeName];
    var iframeEl = document.getElementById ? document.getElementById(iframeName) : document.all ? document.all[iframeName] : null;
    if (iframeEl && iframeWin) {
        iframeEl.style.height = "auto"; // helps resize (for some) if new doc shorter than previous  
        var docHt = getDocHeight(iframeWin.document);
        // need to add to height to be sure it will all show
        if (docHt) iframeEl.style.height = docHt + "px";
    }
}



//------------------------------------------------------------
//These are the window open functions.  They will update a window if one exists with 
//the same name.

//openMaxWindow - opens a maximized window.
//openDialogWindow - opens a small dialog window, centered on the screen.
//openWindow - Allows full control of window options.

//TODO...
//Add functions with a _New suffix use a random number 
//to ensure that the window name is unique.  This will create a new window every time.


function openMaxWindow(aURL, aWinName) {
    var wOpen;
    var sOptions;

    sOptions = 'status=no,menubar=no,scrollbars=yes,resizable=yes,toolbar=no';
    sOptions = sOptions + ',width=' + screen.width + ',height=' + screen.height + ',top=0,left=0';

    wOpen = window.open(aURL, aWinName, sOptions);
    wOpen.focus();
}

function openDialogWindow(aURL, aWinName, w, h, scroll) {
    var wOpen;
    var sOptions;

    var sc = (scroll == 'yes' || scroll == 'true') ? sc = 'yes' : sc = 'no'
    var t = getScreenCenteredTop(h);
    var l = getScreenCenteredLeft(w);

    sOptions = 'location=no,titlebar=no,status=no,menubar=no,resizable=yes,toolbar=no,scrollbars=' + sc;
    sOptions = sOptions + ',width=' + w + ',height=' + h + ',left=' + l + ',top=' + t;

    wOpen = window.open(aURL, aWinName, sOptions);
    wOpen.focus();
}

function openWindow(URLtoOpen, windowName, windowFeatures) {
    wOpen = window.open(URLtoOpen, windowName, windowFeatures);
    wOpen.focus();
}

/*the following is a test of asp/php integration
since we use master pages, we need ASP for the main page.
so, we'll try getting the php page via ajax and sticking it in a div on the blank asp wrapper page

this is the javascript call to get the php content
*/
function grabPHP(php_url, target) {
    if (typeof target == "undefined")
        target = "php_content";

    $.ajax({
        async: false,
        type: "POST",
        cache: false,
        url: php_url,
        data: '',
        contentType: "text/html;",
        dataType: "html",
        success: function (retval) {
            $("#" + target).html(retval);
        },
        error: function (response) {
            $("#" + target).html('ERROR:\n\n ' + response.responseText);
        }
    });
}