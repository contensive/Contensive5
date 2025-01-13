//
// --------------------
//	-- page manager server post
// process: server reads data from cp.request.body then deserializes to a request object of type that matches this data object
// on success, callback with responseData
// on fail, callback with error message
function pageManagerAjax(url, requestData, successCallBack, errorCallback) {
    console.log("pageManagerAjax, url [" + url + "], data [" + requestData + "]");
    $.ajax({
        type: "POST",
        url: url,
        data: JSON.stringify(requestData),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            if (response.errors.length > 0) {
                errorCallback(response.errors[0]);
                return;
            }
            // -- update seat buttons
            successCallBack(response.data);
        },
        error: function (xhr) {
            if(xhr.responseText) {
                errorCallback(xhr.responseText);
                return;
            }
            errorCallback("Please try again. There was an unexpected error from " + url + ".");
        }
    });
}
//
// --------------------
//	-- page manager child list sortable save
//
function pageManagerChildListSave(listId) {
    let e, c, requestData = {};
    requestData.listId = listId;
    requestData.childList = "";
    e = document.getElementById(listId);
    for (i = 0; i < e.childNodes.length; i++) {
        c = e.childNodes[i];
        if (c.id) { requestData.childList += "," + c.id }
    }
  pageManagerAjax("savePageManagerChildListSort", requestData, function(){}, function(){});
}
document.addEventListener("DOMContentLoaded", function(event) { 
    jQuery(".ccEditWrapper .ccChildList").sortable({
        items: "li.allowSort",
        stop: function (event, ui) {
            pageManagerChildListSave(jQuery(this).attr("id"));
        },
        connectWith: ".ccChildList",
		receive: function(event,ui) {
            pageManagerChildListSave(jQuery(this).attr("id"));
		},		
		placeholder: "ui-state-highlight"
    });
});
//
//-------------------------------------------------------------------------
// Add an onLoad event
// source webreference.com, SimonWilson.net
//-------------------------------------------------------------------------
//
function cjAddLoadEvent(func) {
    var oldonload = window.onload;
    if (typeof window.onload !== "function") {
        window.onload = func;
    } else {
        window.onload = function () {
            if (oldonload) {
                oldonload();
            }
            func();
        }
    }
}
//
//-------------------------------------------------------------------------
// Add head script code
// adding to the head after load has no effect. Instead, add it to the document
// with eval.
//-------------------------------------------------------------------------
//
function cjAddHeadScriptCode(codeAsString) {
    eval(codeAsString)
}
//
//-------------------------------------------------------------------------
// Add head script link
//-------------------------------------------------------------------------
//
function cjAddHeadScriptLink(link) {
    var st = document.createElement("script");
    st.type = "text/javascript";
    st.src = link;
    var ht = document.getElementsByTagName("head")[0];
    ht.appendChild(st);
}
//
//-------------------------------------------------------------------------
// Add head style link tag
//-------------------------------------------------------------------------
//
function cjAddHeadStyleLink(linkHref) {
    var st = document.createElement("link");
    st.rel = "stylesheet";
    st.type = "text/css";
    st.href = Source;
    var ht = document.getElementsByTagName("head")[0];
    ht.appendChild(st);
}
//
//-------------------------------------------------------------------------
// Add head style tag
//-------------------------------------------------------------------------
//
function cjAddHeadStyle(styles) {
    var st = document.createElement("style");
    st.type = "text/css";
    if (st.styleSheet) {
        st.styleSheet.cssText = styles;
    } else {
        st.appendChild(document.createTextNode(styles));
    }
    var ht = document.getElementsByTagName("head")[0];
    ht.appendChild(st);
}
function cjSetFrameHeight(frameId) {
    var e, h, f;
    e = document.getElementById(frameId);
    if (e) {
        h = e.contentWindow.document.body.offsetHeight;
        e.style.height = (h + 16) + "px";
    }
}
//
//-------------------------------------------------------------------------
// convert textarea tab key to a tab character
//-------------------------------------------------------------------------
//

function cjEncodeTextAreaKey(sender, e) {
    if (e.keyCode === 9) {
        if (e.srcElement) {
            sender.selection = document.selection.createRange();
            sender.selection.text = "\t";
        } else if (e.target) {
            var start = sender.value.substring(0, sender.selectionStart);
            var end = sender.value.substring(sender.selectionEnd, sender.value.length);
            var newRange = sender.selectionEnd + 1;
            var scrollPos = sender.scrollTop;
            sender.value = String.concat(start, "\t", end);
            sender.setSelectionRange(newRange, newRange);
            sender.scrollTop = scrollPos;
        }
        return false;
    } else {
        return true;
    }
}
//
//----------
// Do NOT call directly, call as cj.ajax.url()
//
// should be cjAjaxURL()
//
// Perform an Ajax call to a URL, and push the results into the object with ID=DestinationID
// LocalURL is a URL on this site (/index.asp or index.asp?a=1 for instance)
// FormID is the ID of a form on this page that should be submitted with the call
// DestinationID is the ID of the DIV tag where the returned content will go
// onEmptyHideID is the ID of a DIV to hide if the ajax call returns nothing
// onEmptyShowID is the ID of a DIV to show if the ajax call returns nothing
//----------
//
function cjAjaxURL(LocalURL, FormID, DestinationID, onEmptyHideID, onEmptyShowID) {
    var xmlHttp, fo, i, url, serverResponse, el1, pos, ret, e;
    var eSelected = new Array();
    try {
        // Firefox, Opera 8.0+, Safari
        xmlHttp = new XMLHttpRequest();
    } catch (e) {
        // Internet Explorer
        try {
            xmlHttp = new ActiveXObject("Msxml2.XMLHTTP");
        } catch (e) {
            try {
                xmlHttp = new ActiveXObject("Microsoft.XMLHTTP");
            } catch (e) {
                alert("Your browser does not support this function");
                return false;
            }
        }
    }
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState === 4 && xmlHttp.status === 200) {
            console.log('xmlHttp.responseURL='+xmlHttp.responseURL);
            serverResponse = xmlHttp.responseText;
            if ((serverResponse !== "") && (DestinationID !== "")) {
                console.log('xmlHttp response ['+serverResponse+']');
                if(serverResponse.substr(0,8)=='redirect'){
                    console.log('xmlHttp response redirect ['+serverResponse.substr(8)+']');
                    window.location.href = serverResponse.substr(8);
                    return;
                }
                if (document.getElementById) {
                    var el1 = document.getElementById(DestinationID);
                } else if (document.all) {
                    var el1 = document.All[DestinationID];
                } else if (document.layers) {
                    var el1 = document.layers[DestinationID];
                }
                if (el1) {
                    el1.innerHTML = serverResponse
                    // run embedded scripts
                    var oldOnLoad = window.onload;
                    window.onload = "";
                    var scripts = el1.getElementsByTagName("script");
                    for (var i = 0; i < scripts.length; i++) {
                        if (scripts[i].src) {
                            var s = document.createElement("script");
                            s.src = scripts[i].src;
                            s.type = "text/javascript";
                            document.getElementsByTagName("head")[0].appendChild(s);
                        } else {
                            eval(scripts[i].innerHTML);
                        }
                    }
                    if (window.onload) {
                        window.onload();
                    }
                }
            }
            if (serverResponse === "") {
                if (document.getElementById(onEmptyHideID)) { document.getElementById(onEmptyHideID).style.display = "none" }
                if (document.getElementById(onEmptyShowID)) { document.getElementById(onEmptyShowID).style.display = "block" }
            }
        }
    }
    // get host from document url
    url = document.URL;
    pos = url.indexOf("#");
    if (pos !== -1) { url = url.substr(0, pos) }
    pos = url.indexOf("?");
    if (pos !== -1) { url = url.substr(0, pos) }
    pos = url.indexOf("://");
    if (pos !== -1) {
        pos = url.indexOf("/", pos + 3);
        if (pos !== -1) { url = url.substr(0, pos) }
    }
    url += LocalURL;
    pos = url.indexOf("?")
    if (pos === -1) { url += "?" } else { url += "&" }
    url += "nocache=" + Math.random();
    fo = document.getElementById(FormID);
    if (fo) {
        if (fo.elements) {
            for (i = 0; i < fo.elements.length; i++) {
                e = fo.elements[i];
                ret = "";
                if (e.type === "select-multiple") {
                    while (e.selectedIndex !== -1) {
                        eSelected.push(e.selectedIndex);
                        if (ret) ret += ",";
                        ret += e.options[e.selectedIndex].value;
                        e.options[e.selectedIndex].selected = false;
                    }
                    while (eSelected.length > 0) {
                        e.options[eSelected.pop()].selected = true;
                    }
                    url += "&" + escape(e.name) + "=" + escape(ret)
                } else if (e.type === "radio") {
                    if (e.checked) {
                        ret = e.value;
                        url += "&" + escape(e.name) + "=" + escape(ret)
                    }
                } else if (e.type === "checkbox") {
                    if (e.checked) {
                        ret = e.value;
                        url += "&" + escape(e.name) + "=" + escape(ret)
                    }
                } else {
                    ret = e.value
                    url += "&" + escape(e.name) + "=" + escape(ret)
                }
            }
        }
    }	
    xmlHttp.open("GET", url, true);
    xmlHttp.send(null);
}
//
//----------
// deprecated
//----------
//
function GetURLAjax(LocalURL, FormID, DestinationID, onEmptyHideID, onEmptyShowID) {
    cjAjaxURL(LocalURL, FormID, DestinationID, onEmptyHideID, onEmptyShowID);
}
//
//----------
// Do NOT call directly, call as cj.ajax.addon()
//
// should be cjAjaxAddon() -- will be renamed
//
// Perform an Ajax call, and push the results into the object with ID=DestinationID
// AddonName is the name of the Ajax Addon to call serverside
// QueryString is to be added to the current URL -- less its querystring
// FormID is the ID of a form on this page that should be submitted with the call
// DestinationID is the ID of the DIV tag where the returned content will go
// onEmptyHideID is the ID of a DIV to hide if the ajax call returns nothing
// onEmptyShowID is the ID of a DIV to show if the ajax call returns nothing
//----------
//
function cjAjaxAddon(AddonName, QueryString, FormID, DestinationID, onEmptyHideID, onEmptyShowID) {
    //
    // compatibility mode
    //
    var url, pos;
    url = document.URL;
    pos = url.indexOf("#");
    if (pos !== -1) { url = url.substr(0, pos) }
    pos = url.indexOf("?");
    if (pos !== -1) { url = url.substr(0, pos) }
    pos = url.indexOf("://");
    if (pos !== -1) {
        pos = url.indexOf("/", pos + 3);
        if (pos !== -1) { url = url.substr(pos) }
    }
    url += "?nocache=0";
    if (AddonName !== "") { url += "&remotemethodaddon=" + AddonName }
    if (QueryString !== "") { url += "&" + QueryString }
    cjAjaxURL(url, FormID, DestinationID, onEmptyHideID, onEmptyShowID);
}
//
//----------
// deprecated
//----------
//
function GetAjax(AddonName, QueryString, FormID, DestinationID, onEmptyHideID, onEmptyShowID) {
    cjAjaxAddon(AddonName, QueryString, FormID, DestinationID, onEmptyHideID, onEmptyShowID);
}
//
//----------
// Do NOT call directly, call as cj.ajax.addonCallback()
//
// cjAjaxAddonCallback()
//
// Perform an Ajax call, and call a callback with the results
// AddonName is the name of the Ajax Addon to call serverside
// QueryString is to be added to the current URL -- less its querystring
// Callback is a javascript function to be called when the data returns. The
//    function must accept two arguments, the ajax return and an argument CallbackArg
//    that can be passed in to the ajax call. Use this second argument to pass
//    status etc to the callback (like what id the response goes in)
//----------
//
function cjAjaxAddonCallback(AddonName, QueryString, Callback, CallbackArg) {
    var xmlHttp, url, pos;
    try {
        // Firefox, Opera 8.0+, Safari
        xmlHttp = new XMLHttpRequest();
    } catch (e) {
        // Internet Explorer
        try {
            xmlHttp = new ActiveXObject("Msxml2.XMLHTTP");
        } catch (e) {
            try {
                xmlHttp = new ActiveXObject("Microsoft.XMLHTTP");
            } catch (e) {
                alert("Your browser does not support this function");
                return false;
            }
        }
    }
    xmlHttp.onreadystatechange = function () {
        var serverResponse, sLen, i, scripts, sLen, srcArray, codeArray;
        if (xmlHttp.readyState === 4 && xmlHttp.status === 200) {
            serverResponse = xmlHttp.responseText;
            var isSrcArray = new Array();
            var codeArray = new Array();
            var oldOnLoad = window.onload;
            if (serverResponse !== "") {
                // remove embedded scripts
                var e = document.createElement("div");
                if (e) {
                    // create array of scripts to add
                    // this is a workaround for an issue with ie
                    // where the scripts collection is 0ed after the first eval
                    window.onload = "";
                    e.innerHTML = serverResponse;
                    scripts = e.getElementsByTagName("script");
                    sLen = scripts.length;
                    if (sLen > 0) {
                        for (i = 0; i < sLen; i++) {
                            if (scripts[0].src) {
                                isSrcArray.push(true);
                                codeArray.push(scripts[0].src);
                                scripts[0].parentNode.removeChild(scripts[0]);
                            } else {
                                isSrcArray.push(false);
                                codeArray.push(scripts[0].innerHTML);
                                scripts[0].parentNode.removeChild(scripts[0]);
                            }
                        }
                        serverResponse = e.innerHTML;
                    }
                }
            }
            // execute the callback which may save the response
            if (Callback) {
                Callback(serverResponse, CallbackArg);
            }
            // execute any scripts found if response
            for (z = 0; z < codeArray.length; z++) {
                if (isSrcArray[z]) {
                    var s = document.createElement("script");
                    s.src = codeArray[z];
                    s.type = "text/javascript";
                    document.getElementsByTagName("head")[0].appendChild(s);
                } else {
                    eval(codeArray[z]);
                }

            }
            if (window.onload) {
                window.onload();
            }

        }
    }
    // create LocalURL
    url = document.URL;
    pos = url.indexOf("#");
    if (pos !== -1) { url = url.substr(0, pos) }
    pos = url.indexOf("?");
    if (pos !== -1) { url = url.substr(0, pos) }
    pos = url.indexOf("://");
    if (pos !== -1) {
        pos = url.indexOf("/", pos + 3);
        if (pos !== -1) { url = url.substr(pos) }
    }
    url += "?nocache=" + Math.random()
    if (AddonName !== "") { url += "&remotemethodaddon=" + AddonName; }
    if (QueryString !== "") { url += "&" + QueryString; }
    xmlHttp.open("GET", url, true);
    xmlHttp.send(null);
}
//
//----------
// Do NOT call directly, call as cj.ajax.qs()
//
// Perform an Ajax call, and push the results into the object with ID=DestinationID
// QueryString is to be added to the current URL -- less its querystring
// FormID is the ID of a form on this page that should be submitted with the call
// DestinationID is the ID of the DIV tag where the returned content will go
// onEmptyHideID is the ID of a DIV to hide if the ajax call returns nothing
// onEmptyShowID is the ID of a DIV to show if the ajax call returns nothing
//----------
//
function cjAjaxQs(QueryString, FormID, DestinationID, onEmptyHideID, onEmptyShowID) {
    var url;
    var pos;
    // create LocalURL
    url = document.URL;
    pos = url.indexOf("#");
    if (pos !== -1) { url = url.substr(0, pos) }
    pos = url.indexOf("?");
    if (pos !== -1) { url = url.substr(0, pos) }
    pos = url.indexOf("://");
    if (pos !== -1) {
        pos = url.indexOf("/", pos + 3);
        if (pos !== -1) { url = url.substr(pos) }
    }
    url += "?nocache=" + Math.random()
    if (QueryString !== "") { url += "&" + QueryString; }
    cjAjaxURL(url, FormID, DestinationID, onEmptyHideID, onEmptyShowID);
}
//
//----------
// Do NOT call directly, call as cj.ajax.qsCallback()
//
// Perform an Ajax call, and on return, calls a callback routine
// QueryString is to be added to the current URL -- less its querystring
//----------
//
function cjAjaxQsCallback(QueryString, Callback, CallbackArg) {
    cjAjaxAddonCallback("", QueryString, Callback, CallbackArg);
}
//
//-------------------------------------------------------------------------
//
function cjAjaxData(handler, queryKey, args, pageSize, pageNumber, responseFormat, ajaxMethod) {
    var xmlHttp;
    var url;
    var serverResponse;
    var el1;
    var pos;
    var response;
    try {
        // Firefox, Opera 8.0+, Safari
        xmlHttp = new XMLHttpRequest();
    } catch (e) {
        // Internet Explorer
        try {
            xmlHttp = new ActiveXObject("Msxml2.XMLHTTP");
        } catch (e) {
            try {
                xmlHttp = new ActiveXObject("Microsoft.XMLHTTP");
            } catch (e) {
                alert("Your browser does not support this function");
                return false;
            }
        }
    }
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState === 4 && xmlHttp.status === 200) {
            response = xmlHttp.responseText;
            var str = response.toLowerCase();
            pos = str.indexOf("</data>");
            if (pos !== -1) { response = response.substr(0, pos) }
            pos = str.indexOf("<data>")
            if (pos !== -1) { response = response.substr(pos + 6) }
            if (handler) { handler(response) }
        }
    }
    // get url w/o QS
    url = document.URL;
    pos = url.indexOf("#");
    if (pos !== -1) { url = url.substr(0, pos) }
    pos = url.indexOf("://");
    if (pos !== -1) {
        pos = url.indexOf("/", pos + 3);
        if (pos !== -1) { url = url.substr(0, pos + 1) }
    }
    pos = url.indexOf("?")
    if (pos = -1) { url += "?" } else { url += "&" }
    url += "ajaxfn=" + ajaxMethod;
    url += "&key=" + queryKey;
    if (pageSize) url += "&pagesize=" + escape(pageSize)
    if (pageNumber) url += "&pagenumber=" + escape(pageNumber)
    if (args) url += "&args=" + escape(args)
    url += "&nocache=" + Math.random();
    url += "&responseformat=" + responseFormat;
    xmlHttp.open("GET", url, true);
    xmlHttp.send(null);
}
//
//-------------------------------------------------------------------------
// cjAjaxGetTable
//	runs cj.ajax.data with a "jsontable" responseFormat
//-------------------------------------------------------------------------
//
function cjAjaxGetTable(handler, queryKey, args, pageSize, pageNumber) {
    return this.data(handler, queryKey, args, pageSize, pageNumber, "jsontable", "data")
}
//
//-------------------------------------------------------------------------
// cjAjaxGetNameArray
//	runs cj.ajax.data with a "namearray" responseFormat
//-------------------------------------------------------------------------
//
function cjAjaxGetNameArray(handler, queryKey, args, pageSize, pageNumber) {
    return this.data(handler, queryKey, args, pageSize, pageNumber, "jsonnamearray", "data")
}
//
//-------------------------------------------------------------------------
// cjAjaxGetNameValue
//	runs cj.ajax.data with 1 row and a "namevalue" responseFormat
//-------------------------------------------------------------------------
//
function cjAjaxGetNameValue(handler, queryKey, args) {
    return this.data(handler, queryKey, args, 1, 1, "jsonnamevalue", "data")
}
//
//-------------------------------------------------------------------------
// cjAjaxSetVisitProperty
//	sets a visit property for the current visit
//-------------------------------------------------------------------------
//
function cjAjaxSetVisitProperty(handler, propertyName, propertyValue) {
    return this.data(handler, "", escape(propertyName) + "=" + escape(propertyValue), 0, 0, "jsonnamevalue", "setvisitproperty")
}
//
//-------------------------------------------------------------------------
// cjAjaxGetVisitProperty
//	gets a visit property for the current visit
//	it is returned in jsonnamevalue format, so you use it like jo.PropertyName=PropertyValue
//-------------------------------------------------------------------------
//
function cjAjaxGetVisitProperty(handler, propertyName, propertyValueDefault) {
    return this.data(handler, "", escape(propertyName) + "=" + escape(propertyValueDefault), 0, 0, "jsonnamevalue", "getvisitproperty")
}
//
//-------------------------------------------------------------------------
// cjAjaxUpdate
//	Runs a Content Update query
//	handler - the routine called when the results return
//	queryKey - the key used to lookup the query (ccLib.getAjaxQueryKey) for this update
//	criteria - sql compatible criteria
//	setPairs - array of name=value pairs formatted as:
//		setPair=[["name0","value0"],["name1","value1"],...]
//		where names are valid field names in the content
//-------------------------------------------------------------------------
//
function cjAjaxUpdate(handler, queryKey, criteria, setPairs) {
    var nameValue, argSet, args, x;
    argSet = "";
    for (x = 0; x < setPairs.length; x++) {
        argSet += "&" + escape(setPairs[x][0]) + "=" + escape(setPairs[x][1])
    }
    args = "criteria=" + escape(criteria) + "&setpairs=" + escape(argSet);
    return this.data(handler, queryKey, args, 0, 0, "", "data")
}
//
function cjAddListener(element, event, listener, bubble) {
    if (element.addEventListener) {
        if (typeof (bubble) === "undefined") bubble = false;
        element.addEventListener(event, listener, bubble);
    } else if (this.attachEvent) {
        element.attachEvent("on" + event, listener);
    }
}
//
//-------------------------------------------------------------------------
//
//-------------------------------------------------------------------------
//
function cjEncodeHTML(source) {
    if (source) {
        var r = source;
        var r = r.replace(/&/g, "&amp;");
        r = r.replace(/</g, "&lt;");
        r = r.replace(/>/g, "&gt;");
        r = r.replace(/"/g, "&quot;");
        r = r.replace(/\n/, "<br>");
        return r;
    }
}
//
//-------------------------------------------------------------------------
//
//-------------------------------------------------------------------------
//
function cjHide(id) {
    var e = document.getElementById(id);
    if (e) e.style.display = "none";
}
//
//-------------------------------------------------------------------------
//
//-------------------------------------------------------------------------
//
function cjShow(id) {
    var e = document.getElementById(id);
    if (e) e.style.display = "block";
}
//
//-------------------------------------------------------------------------
//
//-------------------------------------------------------------------------
//
function cjInvisible(id) {
    var e = document.getElementById(id);
    if (e) e.style.visibility = "hidden";
}
//
//-------------------------------------------------------------------------
//
//-------------------------------------------------------------------------
//
function cjVisible(id) {
    var e = document.getElementById(id);
    e.style.visibility = "visible";
}
//
//-------------------------------------------------------------------------
//	adds a spinner to a target div - used when ajaxing in content
//-------------------------------------------------------------------------
//
function cjSetSpinner(destinationID, message, destinationHeight) {
    if (document.getElementById) {
        var el1 = document.getElementById(destinationID);
    } else if (document.all) {
        var el1 = document.All[destinationID];
    } else if (document.layers) {
        var el1 = document.layers[destinationID];
    }
    if (el1) {
        if (message) {
            el1.innerHTML = "<div style=\"text-align: center;\"><img src=\"https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/images/ajax-loader-big.gif\" /><p>" + message + "</p></div>";
        } else {
            el1.innerHTML = "<div style=\"text-align: center;\"><img src=\"https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/images/ajax-loader-big.gif\" /></div>";
        }
        if (destinationHeight) {
            el1.style.height = destinationHeight;
        }
    }
    //
}
//
//-------------------------------------------------------------------------
// cj.xml
// Load an xml string into an xml object and return it
// 	from multiple sources, 
//	including http://www.webreference.com/programming/javascript/definitive2/2.html
//-------------------------------------------------------------------------
//
function cjXMLLoadString(txt) {
    if (typeof DOMParser !== "undefined") {
        // Mozilla, Firefox, and related browsers 
        return (new DOMParser()).parseFromString(text, "application/xml");
    } else if (typeof ActiveXObject !== "undefined") {
        // Internet Explorer. 
        var doc = XML.newDocument();  // Create an empty document 
        doc.loadXML(text);            // Parse text into it 
        return doc;                   // Return it 
    } else {
        // As a last resort, try loading the document from a data: URL 
        // This is supposed to work in Safari. Thanks to Manos Batsis and 
        // his Sarissa library (sarissa.sourceforge.net) for this technique. 
        var url = "data:text/xml;charset=utf-8," + encodeURIComponent(text);
        var request = new XMLHttpRequest();
        request.open("GET", url, false);
        request.send(null);
        return request.responseXML;
    }
}
//
//-------------------------------------------------------------------------
// cj.admin
//	save all the field names that are empty into a targetField
//-------------------------------------------------------------------------
//
function cjAdminSaveEmptyFieldList(targetFieldId) {
    var e = document.getElementById(targetFieldId);
    var c = document.getElementsByTagName("input");
    for (i = 0; i < c.length; i++) {
        if (c[i].type === "checkbox") {
            if (c[i].checked === false) {
                e.value += c[i].name + ","
            }
        } else if (c[i].type === "radio") {
            if (c[i].checked === false) {
                e.value += c[i].name + ","
            }
        } else if (c[i].value === "") {
            e.value += c[i].name + ","
        }
    }
    c = document.getElementsByTagName("select");
    for (i = 0; i < c.length; i++) {
        if (c[i].value === "") {
            e.value += c[i].name + ","
        }
    }
}
//
//-------------------------------------------------------------------------
// cj.admin object
//	routines for the admin site
//-------------------------------------------------------------------------
//
function cjAdminClass() {
    this.saveEmptyFieldList = cjAdminSaveEmptyFieldList;
}
//
//-------------------------------------------------------------------------
// cj.ajax object
//	perform remote calls back to the server (ajax)
//	cj.ajax.getTable()
//	cj.ajax.getRow()
//-------------------------------------------------------------------------
//
function cjAjaxClass() {
    this.getTable = cjAjaxGetTable;
    this.getNameArray = cjAjaxGetNameArray;
    this.getNameValue = cjAjaxGetNameValue;
    this.update = cjAjaxUpdate;
    this.data = cjAjaxData;
    this.setVisitProperty = cjAjaxSetVisitProperty;
    this.getVisitProperty = cjAjaxGetVisitProperty;
    this.url = cjAjaxURL;
    this.addon = cjAjaxAddon;
    this.addonCallback = cjAjaxAddonCallback;
    this.qs = cjAjaxQs;
    this.qsCallback = cjAjaxQsCallback;
}
//
//-------------------------------------------------------------------------
// cj.xml object
//	perform common xml tasks
//		cj.xml.loadString()
//-------------------------------------------------------------------------
//
function cjXMLClass() {
    this.loadString = cjXMLLoadString;
}
//
//-------------------------------------------------------------------------
// Do NOT call directly
//
// should be cj.remote()
//
// cj.method(options) 
//		options:
//			"name": string - addonname
//			"formId": string - html Id of form to be submitted
//			"callback": function - function to call on completion, two arguments, response and callbackArg
//			"callbackArg": object - passed direction to the callback function
//			"destinationId": string - html Id of an element to set innerHtml
//			"onEmptyHideId": string - if return is empty, hide this element
//			"onEmptyShowId": string - if return is empty, show this element
//			"queryString": string - queryString formatted name=value pairs added to post
//			"url": string - link to hit if not addon name provided
//-------------------------------------------------------------------------
//
function cjRemote(options) {
    var url, fo, ptr, e, ret, pos;
    if (options.url) {
        url = options.url;
    }
    else {
        url = document.URL;
        pos = url.indexOf("#");
        if (pos !== -1) { url = url.substr(0, pos) }
        pos = url.indexOf("?");
        if (pos !== -1) { url = url.substr(0, pos) }
        pos = url.indexOf("://");
        if (pos !== -1) {
            pos = url.indexOf("/", pos + 3);
            if (pos !== -1) { url = url.substr(pos) }
        }
    }
    url += "?nocache=" + Math.random()
    if (options.method) { url += "&remotemethodaddon=" + options.method }
    if (options.queryString) { url += "&" + options.queryString }
    if (options.formId) {
        fo = document.getElementById(options.formId);
        if (fo) {
            if (fo.elements) {
                for (ptr = 0; ptr < fo.elements.length; ptr++) {
                    e = fo.elements[ptr];
                    ret = "";
                    if (e.type === "select-multiple") {
                        while (e.selectedIndex !== -1) {
                            eSelected.push(e.selectedIndex);
                            if (ret) ret += ",";
                            ret += e.options[e.selectedIndex].value;
                            e.options[e.selectedIndex].selected = false;
                        }
                        while (eSelected.length > 0) {
                            e.options[eSelected.pop()].selected = true;
                        }
                        url += "&" + escape(e.name) + "=" + escape(ret)
                    }
                    else if (e.type === "radio") {
                        if (e.checked) {
                            ret = e.value;
                            url += "&" + escape(e.name) + "=" + escape(ret)
                        }
                    }
                    else if (e.type === "checkbox") {
                        if (e.checked) {
                            ret = e.value;
                            url += "&" + escape(e.name) + "=" + escape(ret)
                        }
                    }
                    else {
                        ret = e.value
                        url += "&" + escape(e.name) + "=" + escape(ret)
                    }
                }
            }
        }
    }
    jQuery.get(url,
        function (serverResponse) {
            var e, start, end, test;
            var isSrcArray = new Array();
            var codeArray = new Array();
            var oldOnLoad = window.onload;
            if (serverResponse === "") {
                if (options.onEmptyHideId) {
                    e = document.getElementById(options.onEmptyHideId);
                    if (e) {
                        e.style.display = "none"
                    }
                }
                if (options.onEmptyShowID) {
                    e = document.getElementById(options.onEmptyShowID);
                    if (e) {
                        e.style.display = "block"
                    }
                }
            }
            else {
                // remove embedded scripts
                e = document.createElement("div");
                if (e) {
                    // create array of scripts to add
                    // this is a workaround for an issue with ie
                    // where the scripts collection is 0ed after the first eval
                    window.onload = "";
                    e.innerHTML = serverResponse;
                    if ((serverResponse !== "") && (e.innerHTML === "")) {
                        //ie7-8,blocks scripts in response
                        test = serverResponse;
                        start = test.indexOf("<script");
                        while (start > -1) {
                            end = test.indexOf("/script>");
                            if (end > -1) {
                                serverResponse = serverResponse.substr(0, start) + serverResponse.substr(end + 8);
                            }
                            else {
                                serverResponse = serverResponse.substr(0, start);
                            }
                            test = serverResponse;
                            start = test.indexOf("<script");
                        }
                    }
                    else {
                        scripts = e.getElementsByTagName("script");
                        sLen = scripts.length;
                        if (sLen > 0) {
                            for (i = 0; i < sLen; i++) {
                                if (scripts[0].src) {
                                    isSrcArray.push(true);
                                    codeArray.push(scripts[0].src);
                                    scripts[0].parentNode.removeChild(scripts[0]);
                                }
                                else {
                                    isSrcArray.push(false);
                                    codeArray.push(scripts[0].innerHTML);
                                    scripts[0].parentNode.removeChild(scripts[0]);
                                }
                            }
                            serverResponse = e.innerHTML;
                        }
                    }
                }
                // execute any scripts found
                for (i = 0; i < codeArray.length; i++) {
                    if (isSrcArray[i]) {
                        var s = document.createElement("script");
                        s.src = codeArray[i];
                        s.type = "text/javascript";
                        document.getElementsByTagName("head")[0].appendChild(s);
                    }
                    else {
                        eval(codeArray[i]);
                    }

                }
                // if destination set, store html response.
                if (options.destinationId) {
                    if (document.getElementById) {
                        var el1 = document.getElementById(options.destinationId);
                    }
                    else if (document.all) {
                        var el1 = document.All[options.destinationId];
                    }
                    else if (document.layers) {
                        var el1 = document.layers[options.destinationId];
                    }
                    if (el1) {
                        el1.innerHTML = serverResponse
                    }
                }
            }
            // execute the callback which may save the response
            if (options.callback) {
                options.callback(serverResponse, options.callbackArg);
            }
            if (window.onload) {
                window.onload();
            }
        },
        "text"
    );
}
//
//-------------------------------------------------------------------------
// Contensive Javascript object
//-------------------------------------------------------------------------
//
function ContensiveJavascriptClass() {
    this.encodeHTML = cjEncodeHTML;
    this.invisible = cjInvisible;
    this.visible = cjVisible;
    this.hide = cjHide;
    this.show = cjShow;
    this.setFrameHeight = cjSetFrameHeight;
    this.encodeTextAreaKey = cjEncodeTextAreaKey;
    this.addHeadScriptLink = cjAddHeadScriptLink;
    this.addHeadStyle = cjAddHeadStyle;
    this.addHeadStyleLink = cjAddHeadStyleLink;
    this.addHeadScriptCode = cjAddHeadScriptCode;
    this.addLoadEvent = cjAddLoadEvent;
    this.addListener = cjAddListener;
    this.setSpinner = cjSetSpinner;
    //
    this.ajax = new cjAjaxClass();
    this.xml = new cjXMLClass();
    this.admin = new cjAdminClass();
    this.remote = cjRemote;
    //
    //-------------------------------------------------------------------------
    // Add head tag
    //-------------------------------------------------------------------------
    //
    this.addHeadTag = function (tag) {
        var i, oTag;
        var ht = document.getElementsByTagName("head")[0];
        try {
            ht.innerHTML += tag;
        }
        catch (e) {
            oTag = document.createElement("div");
            try {
                oTag.outerHTML = tag;
                ht.appendChild(oTag);
            }
            catch (e) {
                oTag.innerHTML = tag;
                for (i = 0; i < oTag.childNodes.length; i++) {
                    ht.appendChild(oTag.childNodes[i]);
                }
            }
        }
    }
}
var cj = new ContensiveJavascriptClass()

function cjAddHeadTag(tag) {
    cj.addHeadTag(tag)
}
if (typeof cj.frame !== 'object') {
    //
    // -- wrap in test to block if legacy code installed
    function cjFrameSubmitForm(remoteMethodName, frameHtmlId, formHtmlId) {
        jQuery('#' + frameHtmlId).block();
        cj.ajax.addon(
            remoteMethodName
            , 'frameName=' + frameHtmlId
            , formHtmlId
            , frameHtmlId
        );
    }
    function cjFrameUpdate(remoteMethodName, frameHtmlId, qs) {
        jQuery('#' + frameHtmlId).block();
        cj.ajax.addon(
            remoteMethodName
            , qs + '&frameName=' + frameHtmlId
            , ''
            , frameHtmlId
        );
    }
    //
    // attach this to the contensive cj object
    class cjFrame {
        constructor() {
            this.submitForm = cjFrameSubmitForm;
            this.update = cjFrameUpdate;
        }
    }
    cj.frame = new cjFrame();
    //
    // legacy names for cj.frame
    function afwPostFrame(remoteMethodName, formHtmlId, frameHtmlId) { cj.frame.submitForm(remoteMethodName, frameHtmlId, formHtmlId) }
    function afwUpdateFrame(remoteMethodName, qs, frameHtmlId) { cj.frame.update(remoteMethodName, frameHtmlId, qs) }
}