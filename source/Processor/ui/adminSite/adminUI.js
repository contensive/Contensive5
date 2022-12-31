
//
// -- contensive base: included for all base addons: adminSite, pageManager, etc
//
var TableRowPropertyObject;
var TablePropertyObject;
var TableBodyPropertyObject;
var tableDialogEditor;
var currentRow;
var currentCol;
var rtNumRows;
var rtNumCols;
var rtTblAlign;
var rtTblWidth;
var outOfContext;
//
// FlyoutMenu Globals
//
var activeButton = null;
var selectsHidden = false;
var menuClicks = 0;
var width;
var height;
var flag = 0;
var updated = 0;
var clickButton;
var lastClipCheck = false;
var lastSelCheck = false;
var currentPanel;
//
var ButtonObjectCount = 0;
var MenuObjectCount = 0;
var MenuHideBlock = false;
var isNS = (navigator.appName === "Netscape");
var isMacIE = ((navigator.userAgent.indexOf("IE 4") > -1) && (navigator.userAgent.indexOf("Mac") > -1));
var RLMediaType = "";
//
//	build admin site workspace
//
var AFinnerHTML;
//
//
// cross browser window size function
//
window.size = function () {
    var w = 0;
    var h = 0;
    //IE
    if (!window.innerWidth) {
        //strict mode
        if (!(document.documentElement.clientWidth === 0)) {
            w = document.documentElement.clientWidth;
            h = document.documentElement.clientHeight;
        }
        //quirks mode
        else {
            w = document.body.clientWidth;
            h = document.body.clientHeight;
        }
    }
    //w3c
    else {
        w = window.innerWidth;
        h = window.innerHeight;
    }
    return { width: w, height: h };
}
//
//-- Admin Find row search on enter-key
//
function KeyCheck(e){
    var code = e.keyCode;
    if(code==13){
      document.getElementById('FindButton').focus();
      document.getElementById('FindButton').click();
      return false;
    }
  } 
//
//----------
//
function DeleteCheck() {
    return confirm("This action will delete this record.\n\nAre you sure you would like to delete this record?\n\n");
}
//
//----------
//
function DeleteCheckWithChildren() {
    return confirm("This action will delete this record and its child records.\n\nAre you sure you would like to delete this record?\n\n");
}
//
//----------
//
function DeletePageCheck() {
    return confirm("This action will delete this page and its child pages.\n\nAre you sure you would like to delete this page?\n\n");
}
//
//----------
//
function CheckInputs(TargetID, SetValue) {
    for (let input of document.getElementsByTagName("input")) {
        if (input.id === TargetID) { input.checked = SetValue; }
    }
}
//
//----------
//
function SetFieldName(FieldName) {
    document.all["fn"].value = FieldName; 
    return true;
}
//
// ----- hide select menus
//
function hideselect(hiddenIn) {
    var i, j, index, objs, wmode, paramName
    //
    if (hiddenIn) {
        menuClicks++;
    }
    //
    if (!hiddenIn && menuClicks === 0 && !selectsHidden) {
        return;
    }
    // Hide MS Selects
    if (browser.isIE) {
        //alert("ie");
        objs = document.getElementsByTagName("SELECT");
        for (i = 0; i < objs.length; i++) {
            if (selectsHidden && menuClicks === 0) {
                objs[i].style.visibility = "";
            } else {
                objs[i].style.visibility = "hidden";
            }
        }
        // Hide Objects if no <Param name=wmode> for ie
        objs = document.getElementsByTagName("OBJECT");
        for (i = 0; i < objs.length; i++) {
            if (selectsHidden && menuClicks === 0) {
                objs[i].style.visibility = "";
            } else {
                //var wmodeSet=false;
                var p = objs[i].getElementsByTagName("param");
                for (var pi = 0; pi < p.length; pi++) {
                    paramName = p[pi].name.toLowerCase();
                    if (paramName === "wmode") {
                        wmode = p[pi].value;
                        if (wmode) {
                            wmode = wmode.toLowerCase();
                            if (wmode === "opaque") {
                                // it hides itself
                            } else if (wmode === "transparent") {
                                // it hides itself
                            } else if (wmode === "window") {
                                // it hides itself
                                objs[i].style.visibility = "hidden";
                            } else {
                                objs[i].style.visibility = "hidden";
                            }
                        }
                    }
                }
            }
        }
    } else {
        // Hide MS Embeds for Mozilla
        objs = document.getElementsByTagName("EMBED");
        for (i = 0; i < objs.length; i++) {
            if (selectsHidden && menuClicks === 0) {
                objs[i].style.visibility = "";
            } else {
                wmode = objs[i].getAttribute("wmode")
                if (wmode) {
                    wmode = wmode.toLowerCase();
                }
                if (wmode === "opaque") {
                    // it hides itself
                } else if (wmode === "transparent") {
                    // it hides itself
                } else if (wmode === "window") {
                    // it hides itself
                } else {
                    objs[i].style.visibility = "hidden";
                }
            }
        }
    }
    //
    if (selectsHidden && menuClicks === 0) {
        selectsHidden = false;
    }
    //
    else if (selectsHidden && menuClicks !== 0 && clickButton !== 2) {
        menuClicks--;
    }
    //
    else {
        selectsHidden = true;
    }
}
//
// ----- Browser Detect
//
class BrowserType {
    constructor() {
        this.isIE = false; // Internet Explorer
        this.isNS = false; // Netscape
        this.isMac = false;
        this.version = 0.0;
        if (navigator.appVersion.indexOf("Mac") !== -1) {
            this.isMac = true;
        }
        var s = "MSIE";
        var ua = navigator.userAgent;
        var i = ua.indexOf(s);
        if (i >= 0) {
            this.isIE = true;
            this.version = parseFloat(ua.substr(i + s.length));
            return;
        }
        s = "Netscape6/";
        i = ua.indexOf(s);
        if (i >= 0) {
            this.isNS = true;
            this.version = parseFloat(ua.substr(i + s.length));
            return;
        }
        s = "Gecko";
        i = ua.indexOf(s);
        if (i >= 0) {
            this.isNS = true;
            this.version = 6.1;
            return;
        }
    }
}
var browser = new BrowserType();
//
// ----- Code for handling the menu bar and active button.
//
function PageClick(event) {
    var el;
    if (activeButton == null) { return; }
    if (browser.isIE)
        el = window.event.srcElement;
    else
        el = (event.target.tagName ? event.target : event.target.parentNode);

    if (el === activeButton)
        return;
    if (getContainerWith(el, "DIV", "kmaMenu") == null) {
        resetButton(activeButton);
        activeButton = null;
    }
}
//
// ----- Clear the menu
//
function ClearMenu() {
    if (activeButton == null) { return; }
    resetButton(activeButton);
    activeButton = null;
}
//
// -----
//
function ccFlyoutHoverMode(val) {
    flag = val;
    if (val === 0) {
        setTimeout(timerEvent, 1000);
    }
    if (val === 1) {
        updated = 1;
    }
    return;
}
//
// -----
//
function timerEvent() {
    if (flag === 0) {
        resetFlag = 1;
        resetButton(activeButton);
        menuClicks = 0;
        hideselect(0);
        updated = 1;
    }

}
//
// -----
//
function setAnchorTagsToColor(classToFind, colorToSet) {
    var i;
    var aTags = document.getElementsByTagName("A");
    for (i = 0; i < aTags.length; i++) {
        if (hasClassName(aTags[i], classToFind)) {
            aTags[i].style.color = colorToSet;
        }
    }
}
//
// -----
//
function removeClassNameAfter(el, name) {
    var i, curList, newList;
    if (el.className == null) { return; }
    curList = el.className.split(" ");
    newList = "";
    for (i = 0; i < curList.length; i++)
        if (curList[i] !== name) {
            newList = newList + " " + curList[i];
        } else {
            // newList=newList+" "+curList[i];
            el.className = newList;
            return;
        }
}
//
// -----
//
function resetButton(button) {
    if (button) {
        removeClassNameAfter(button, "kmaMenuDown");
        if (button.menu != null) {
            closeSubMenu(button.menu);
            button.menu.style.visibility = "hidden";
        }
    }
}
//
// -----
//
function activateButton(button) {
    if (button.menu != null) {
        button.menu.style.visibility = "visible";
    }
}
//
// -----
//
function ccFlyoutButtonClick(event, menuId, position, StyleSheetPrefix, OpenOnHover) {
    var button;
    var x, y;
    var offY;
    //
    if ((!(clickButton === 2 && outOfContext))) {
        hideselect(true);
    }
    if (browser.isIE) {
        offY = event.offsetY;
        button = event.srcElement;
    } else {
        button = event.currentTarget;
    }
    if (button.tagName !== "A") {
        button = getContainer(button, "A")
        if (button === null) { return false; }
    }
    if (menuId === "") { return false; }
    width = document.body.scrollWidth;
    height = document.body.scrollHeight;
    button.blur();
    if (button.menu == null) {
        button.menu = document.getElementById(menuId);
        if (button.menu.title === "")
            menuInit(button.menu);
    }
    if (activeButton != null)
        resetButton(activeButton);
    if (button != activeButton || updated === 1) {
        updated = 0;
        // Update the button"s style so it looks down
        if (clickButton !== 2) {
            button.className += " kmaMenuDown " + StyleSheetPrefix + "ButtonDown";
        }
        //button.className += " ccFlyoutButtonDown";
        // put menu under button
        if (position === 1) {
            // Flyout to the right
            x = getPageOffsetLeft(button) + button.offsetWidth;
            y = getPageOffsetTop(button);
        } else if (position === 2) {
            // Flyout up
            x = getPageOffsetLeft(button);
            y = getPageOffsetTop(button) - button.menu.offsetHeight;
        } else if (position === 3) {
            // Flyout to the left
            x = getPageOffsetLeft(button) - button.menu.offsetWidth;
            y = getPageOffsetTop(button);
        } else {
            // Flyout down
            x = getPageOffsetLeft(button);
            y = getPageOffsetTop(button) + button.offsetHeight;

        }

        if (clickButton === 2) {
            x = event.clientX;
            y = event.clientY;
            if (document.body) {
                x += document.body.scrollLeft;
                y += document.body.scrollTop;
            }
        }

        if ((x + button.menu.scrollWidth) > width) {
            x = width - button.menu.scrollWidth;
        }

        button.menu.style.left = x + "px";
        button.menu.style.top = y + "px";
        // turn it on
        button.menu.style.visibility = "visible";
        activeButton = button;
    }
    else {
        if (!OpenOnHover) {
            activeButton = null;
        }
    }
    clickButton = 1;
    return false;
}
//
// -----
//
function ccFlyoutButtonHover(event, menuId, position) {
    var button;
    if (browser.isIE)
        button = window.event.srcElement;
    else
        button = event.currentTarget;
    if (activeButton !== null && activeButton !== button)
        ccFlyoutButtonClick(event, menuId, position);
}
//
// -----
//
function ccFlyoutPanelHover(event, StyleSheetPrefix) {
    var menu;
    if (browser.isIE)
        menu = getContainerWith(window.event.srcElement, "DIV", StyleSheetPrefix + "Panel");
    //menu = getContainerWith(window.event.srcElement, "DIV", "ccFlyoutPanel");
    else
        menu = event.currentTarget;
    if (menu.title !== "") {
        if (menu.activeItem == null) {
            // do nothing
        } else {
            closeSubMenu(menu);
        }
    }
}
//
// -----
//
function ccFlyoutPanelButtonHover(event, menuId, StyleSheetPrefix) {
    var item, menu, x, y;
    var maxX, maxY;
    if (browser.isIE)
        item = getContainerWith(window.event.srcElement, "A", StyleSheetPrefix + "PanelButton");
    //item = getContainerWith(window.event.srcElement, "A", "ccFlyoutPanelButton");
    else
        item = event.currentTarget;
    menu = getContainerWith(item, "DIV", StyleSheetPrefix + "Panel");
    if (menu.activeItem == null) {
        // nothing
    } else {
        closeSubMenu(menu);
    }
    if (!(menuId)) { return false }
    if (menuId === "") { return false; }
    menu.activeItem = item;
    item.className += " kmaMenuDown " + StyleSheetPrefix + "PanelButtonDown";
    if (item.subMenu == null) {
        item.subMenu = document.getElementById(menuId);
        if (item.subMenu.title === "")
            menuInit(item.subMenu);
    }
    x = getPageOffsetLeft(item) + item.offsetWidth;
    y = getPageOffsetTop(item);
    if (browser.isNS) {
        maxX = window.scrollX + window.innerWidth;
        maxY = window.scrollY + window.innerHeight;
    }
    if (browser.isIE) {
        maxX = (document.documentElement.scrollLeft !== 0 ? document.documentElement.scrollLeft : document.body.scrollLeft)
            + (document.documentElement.clientWidth !== 0 ? document.documentElement.clientWidth : document.body.clientWidth);
        maxY = (document.documentElement.scrollTop !== 0 ? document.documentElement.scrollTop : document.body.scrollTop)
            + (document.documentElement.clientHeight !== 0 ? document.documentElement.clientHeight : document.body.clientHeight);
    }
    maxX -= item.subMenu.offsetWidth;
    maxY -= item.subMenu.offsetHeight;
    if (x > maxX) {
        x = Math.max(0, x - item.offsetWidth - item.subMenu.offsetWidth + (menu.offsetWidth - item.offsetWidth));
    }
    y = Math.max(0, Math.min(y, maxY));
    item.subMenu.style.left = x + "px";
    if (browser.isMac) {
        if (browser.version > 5.1) {
            item.subMenu.style.top = y + "px";
        }
    } else {
        item.subMenu.style.top = y + "px";
    }
    item.subMenu.style.visibility = "visible";
    if (browser.isIE) {
        window.event.cancelBubble = true;
    } else {
        event.stopPropagation();
    }
}
//
// -----
//
function closeSubMenu(menu) {
    if (!menu) { return; }
    if (menu.activeItem == null) { return; }
    if (menu.activeItem.subMenu) {
        closeSubMenu(menu.activeItem.subMenu);
        menu.activeItem.subMenu.style.visibility = "hidden";
        menu.activeItem.subMenu = null;
    }
    removeClassNameAfter(menu.activeItem, "kmaMenuDown");
    menu.activeItem = null;
}
//
// -----
//
function menuInit(menu) {
    var itemList, spanList;
    var textEl, arrowEl;
    var itemWidth;
    var w, dw;
    var i, j;
    itemList = menu.getElementsByTagName("A");
    if (itemList.length === 0) {
        menu.activeItem = null
        return false;
    }
    if (browser.isIE) {
        w = itemList[0].offsetWidth;
        itemList[0].style.width = w + "px";
        dw = itemList[0].offsetWidth - w;
        w -= dw;
        itemList[0].style.width = w + "px";
    }
    menu.activeItem = null
}
//
// -----
//
function getContainerWith(node, tagName, className) {
    while (node != null) {
        if (node.tagName != null && node.tagName === tagName && hasClassName(node, className)) {
            return node;
        }
        node = node.parentNode;
    }
    return node;
}
//
// -----
//
function getContainer(node, tagName) {
    while (node != null) {
        if (node.tagName != null && node.tagName === tagName) {
            return node;
        }
        node = node.parentNode;
    }
    return node;
}
//
// -----
//
function hasClassName(el, name) {
    var list = el.className.split(" ");
    for (var i = 0; i < list.length; i++) {
        if (list[i] === name) { return true; }
    }
    return false;
}
//
// -----
//
function removeClassName(el, name) {
    var i, newList;
    if (el.className == null) { return; }
    var curList = el.className.split(" ");
    for (i = 0; i < curList.length; i++)
        if (curList[i] !== name) {
            newList += " " + curList[i];
        } else {
            el.className = newList;
            return;
        }
}
//
//----------
//
function getPageOffsetLeft(el) {
    var offset = jQuery(el).position();
    return offset.left;
}
//
//----------
//
function getPageOffsetTop(el) {
    var offset = jQuery(el).position();
    return offset.top;
}
//
//----------
//
function SaveFieldPlus(ObjectName) {
    var RefreshTemp;
    RefreshTemp = document.all.tags("div")[ObjectName].innerHTML;
    document.all[ObjectName + "Field"].value = RefreshTemp;
}
//
//----------
//
var globalAddonIconObj;
//
//----------
//
function InsertTag(OpenTag, CloseTag) {
    var SelectionObject;
    var SelectionObjectType;
    var ControlRangeCollection;
    var ParentElementObject;
    var Pointer;
    var ControlRange;
    //
    SelectionObject = document.selection;
    SelectionObjectType = SelectionObject.type;
    if (SelectionObjectType === "Control") {
        ControlRangeCollection = SelectionObject.createRange();
        for (Pointer = 0; Pointer < ControlRangeCollection.length; Pointer++) {
            ControlRange = ControlRangeCollection(Pointer);
            ControlRange.outerHTML = OpenTag + ControlRange.outerHTML + CloseTag;
        }
    } else {
        var TextRange = SelectionObject.createRange();
        var text = TextRange.text;
        if (text) {
            TextRange.pasteHTML(OpenTag + text + CloseTag);
        } else {
            TextRange.pasteHTML(OpenTag + CloseTag);
        }
        ClearMenu();
    }
}
//
//----------
//
function PrintElement(ElementIdentifier) {
    var ElementObject = document.all(ElementIdentifier);
    var tr = ElementObject.createTextRange();
    var msgWind = window.open("", "_blank", "scrollbars=yes,toolbar=no,status=no,resizable=yes");
    msgWind.document.body.innerHTML = tr.text;
    msgWind.print();
    msgWind.close();
}
//
//----------
//
function RemoveStyle(ElementIdentifier) {
    var ElementObject;
    var SelectedObject;
    var TextRange;
    ElementObject = document.all(ElementIdentifier);
    ElementObject.focus;
    SelectedObject = document.selection;
    TextRange = SelectedObject.createRange();
    TextRange.pasteHTML(TextRange.text);
    ClearMenu();
}

//
//----------
//
function ExecCmdPlus(cmd, ui, opt, ObjectName) {
    var CommandPassed, editor;
    editor = document.all[ObjectName]
    if (!editor.document.queryCommandSupported(cmd)) {
        alert("This function is not supported. Your browser may not support this command, or you may need to make a text selection before using this function.")
    } else {
        document.body.all.tags("div")[ObjectName].focus();
        CommandPassed = editor.document.execCommand(cmd, ui, opt);
        if (CommandPassed) {
            // removed to allow formating undo -- onblur covers the save case
            //SaveFieldPlus(ObjectName);
            document.body.all.tags("div")[ObjectName].focus();
        } else {
            alert("This function failed. Your browser may not support this command, or you may need to make a text selection before using this function.")
        }
    }
    ClearMenu();
}
//
//----------
//
function OpenResourceLibrary(EditorObjectName) {
    window.open("?ccIPage=s033l8dm15&EditorObjectName=" + EditorObjectName, "ImageSelector", "menubar=no,toolbar=no,location=no,status=no,scrollbars=yes,resizable")
}
//
//----------
//
function InsertImage(Name, Src, Alt, Width, Height, ObjectName) {
    var tag
    tag = "<IMG SRC=\"" + Src + "\""
    tag += "name=\"" + Name + "\" ID=\"" + Name + "\""
    if (Width !== 0) tag += " WIDTH=\"" + Width + "\""
    if (Height !== 0) tag += " HEIGHT=\"" + Height + "\""
    tag += " BORDER=0>"
    oUtil.obj.insertHTML(tag);
}
//
//----------
//
function InsertDownload(RecordID, ObjectName, ImageLink) {
    var tag;
    tag = "<a href=\"?downloadid=" + RecordID + "\" target=\"_blank\"><img src=\"" + ImageLink + "\" border=0 width=16 height=16 alt=\"Download\" ></a>";
    oUtil.obj.insertHTML(tag);

}
//
//----------
//
function InsertActiveContentPlus(ACType, ACSubType, ACArg1, ObjectName) {
    var SelectionObject;
    var SelectionObjectType;
    var ControlRangeObject;
    var ParentElementObject;
    var Pointer;
    var ImageObject;
    ExecCmdPlus("InsertImage", false, "https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/images/AC" + ACType + ACSubType + ".gif", ObjectName);
    SelectionObject = document.selection;
    SelectionObjectType = SelectionObject.type;
    if (SelectionObjectType !== "Control") {
        // 
    } else {
        ControlRangeObject = SelectionObject.createRange();
        for (Pointer = 0; Pointer < ControlRangeObject.length; Pointer++) {
            ImageObject = ControlRangeObject(Pointer);
            if (ImageObject.tagName === "IMG") {
                ImageObject.id = "AC," + ACType + "," + ACSubType + "," + ACArg1;
            } else {
                // Non-Image found in Control Object
            }
        }
    }
    ClearMenu();
}
//
//----------
//
function InsertActiveChildList(ACType, ObjectName) {
    var RandomNumber;
    RandomNumber = Math.floor(Math.random() * 1000000);
    InsertActiveContentPlus(ACType, "", RandomNumber, ObjectName);
}
//
//----------
//
function tableDialog(ObjectName) {
    ClearMenu();
    var rtNumRows = null;
    var rtNumCols = null;
    var rtTblAlign = null;
    var rtTblWidth = null;
    tableDialogEditor = ObjectName;
    showModalDialog("https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/popup/tablepop.htm", window, "status:false;dialogWidth:275px;");
}
//
//----------
//
function tdDialog(tabIndex, fieldName) {
    ClearMenu();
    var h = new Object();
    h.win = window;
    h.tab = tabIndex;
    h.fieldName = fieldName;
    showModalDialog("https://s3.amazonaws.com/cdn.contensive.com/assets/20190729/popup/tdpopup.htm", h, "scroll:off;status:false;dialogWidth:274px;dialogHeight:400px;");
}
//
//----------
//
function createTable() {
    var HTMLEditor = document.all[tableDialogEditor]
    var CellWidth;
    if (rtNumRows === "" || rtNumRows === "0") {
        rtNumRows = "1";
    }
    if (rtNumCols === "" || rtNumCols === "0") {
        rtNumCols = "1";
    }
    var rttrnum = 1;
    var rttdnum = 1;
    var rtNewTable = "<P><table border=\"1\" align=\"" + rtTblAlign + "\" cellpadding=\"0\" cellspacing=\"0\" width=\"" + rtTblWidth + "\">"
    CellWidth = (100 / rtNumCols);
    while (rttrnum <= rtNumRows) {
        rttrnum = rttrnum + 1
        rtNewTable = rtNewTable + "<tr>"
        while (rttdnum <= rtNumCols) {
            rtNewTable = rtNewTable + "<td width=\"" + CellWidth + "%\">&nbsp;</td>"
            rttdnum = rttdnum + 1
        }
        rttdnum = 1
        rtNewTable = rtNewTable + "</tr>"
    }
    rtNewTable = rtNewTable + "</table></P>"
    HTMLEditor.focus();
    InsertTag(rtNewTable, "");
}
//
//----------
//
function clickIE() {
    return false;
}
//
//----------
//
function createTableNew() {
    rtNumRows = currentRow;
    rtNumCols = currentCol;
    rtTblAlign = "";
    rtTblWidth = "100%";
    createTable();
    rtNumRows = null;
    rtNumCols = null;
    rtTblAlign = null;
    rtTblWidth = null;
}
//
//----------
//
function hideTableIF(ObjectName) {
    var tableDiv = document.all[ObjectName];
    tableDiv.style.visibility = "hidden";
    var tableCreateIF = tableDiv.getElementsByTagName("TABLE");
    var tableCells = tableCreateIF[0].getElementsByTagName("TD");
    for (i = 0; i < tableCells.length; i++) {
        tableCells[i].style.backgroundColor = "#FFFFFF";
    }
}
//
//----------
//
function highlightTDs(ObjectName, num, CellDataHolder) {
    var tableCreateIF = document.all[ObjectName];
    var tableCells = tableCreateIF.getElementsByTagName("TD");
    var tableRows = tableCreateIF.getElementsByTagName("TR");
    var tableWidth = tableCells.length / tableRows.length;
    var col;
    var row;
    var i;
    if (num === 0) {
        col = 1;
    }
    else {
        col = (num + 1) % tableWidth;
        if (col === 0) {
            col = tableWidth;
        }
    }
    row = Math.floor((num + tableWidth) / tableWidth);
    currentCol = col;
    currentRow = row;
    for (i = 0; i < tableCells.length; i++) {
        if (colVal(i, tableWidth) <= col && rowVal(i, tableWidth) <= row) {
            tableCells[i].style.backgroundColor = "#185A98";
        }
        else {
            tableCells[i].style.backgroundColor = "#FFFFFF";
        }
    }
    var TableSize = document.all[CellDataHolder];
    TableSize.value = row + " x " + col;
}
//
//----------
//
function rowVal(num, tableWidth) {
    return Math.floor((num + tableWidth) / tableWidth);
}
//
//----------
//
function colVal(num, tableWidth) {
    var retVal;
    if (num === 0) {
        retVal = 1;
    }
    else {
        retVal = (num + 1) % tableWidth;
        if (retVal === 0) {
            retVal = tableWidth;
        }
    }
    return retVal;
}
//
//----------
//
function setEditor(objectName) {
    tableDialogEditor = objectName;
}
//
//----------
//
var TableCellPropertyObject;
function editorRightClick(prefix, divName) {
    var possibleTableElement;
    if (parseInt(navigator.appVersion) > 3) {
        var clickType = 1;
        if (navigator.appName == "Netscape") clickType = e.which;
        else clickType = event.button;
        if (clickType !== 1) {
            var SelectedObject;
            var tRange;
            SelectedObject = document.selection;
            tRange = SelectedObject.createRange();

            if (SelectedObject.type === "Control") {
                if (tRange.length >= 1) {
                    var selectedEl = tRange(0);
                }
            }
            else {
                var selectedEl = tRange.parentElement();
            }
            if (selectedEl.tagName !== "TD") {
                selectedEl = document.elementFromPoint(event.x, event.y);
            }

            if (selectedEl.tagName !== "TD") {
                possibleTableElement = findTagAmongstParents(selectedEl, "TD", divName);
            }

            if (possibleTableElement != null) {
                selectedEl = possibleTableElement;
            }
            if (selectedEl.tagName === "TD") {
                var tempEl2 = selectedEl.parentElement;
                do {
                    if (tempEl2.tagName === "TR") {
                        TableRowPropertyObject = tempEl2;
                    }
                    tempEl2 = tempEl2.parentElement;
                } while (tempEl2.tagName !== "TBODY");

                TableCellPropertyObject = selectedEl;
                TableBodyPropertyObject = tempEl2;
                TablePropertyObject = tempEl2.parentElement;

                outOfContext = false;
                return prefix + "TableMenu";

            } else {
                outOfContext = true;
                return "";
            }
        } 
    } 
}
//
//----------
//
function findTagAmongstParents(startingElement, tagName, divName) {
    var found;
    var currEl;
    found = false;
    if ((startingElement.tagName === "DIV") && (startingElement.name === divName)) {
        return null;
    }
    currEl = startingElement.parentElement;
    while (currEl != null && !found) {
        if ((currEl.tagName === "DIV") && (currEl.name === divName)) {
            return null;
        }
        if (currEl.tagName === tagName) {
            found = true;
        }
        else {
            currEl = currEl.parentElement;
        }
    }
    return currEl;
}
//
//----------
//
function hideRightMenu(ObjectName) {
    var i;
    var tableMenu = document.all[ObjectName];
    for (i = 0; i < tableMenu.childNodes.length; i++) {
        if (tableMenu.childNodes[i].tagName === "DIV") {
            tableMenu.childNodes[i].style.visibility = "hidden";
        }
    }
    tableMenu.style.visibility = "hidden";
}
//
//----------
//
function insertRowBefore() {
    var tRows = TablePropertyObject.getElementsByTagName("TR");
    var tCells = TablePropertyObject.getElementsByTagName("TD");
    var tCols = Math.round(tCells.length / tRows.length);
    trElement = document.createElement("TR");
    TableBodyPropertyObject.insertBefore(trElement, TableRowPropertyObject);
    var i;
    for (i = 0; i < tCols; i++) {
        var nc = trElement.insertCell();
        nc.insertAdjacentText("afterBegin", " ");
    }
    ClearMenu();
}
//
//----------
//
function insertRowAfter() {
    var tRows = TablePropertyObject.getElementsByTagName("TR");
    var tCells = TablePropertyObject.getElementsByTagName("TD");
    var tCols = Math.round(tCells.length / tRows.length);
    trElement = document.createElement("TR");
    TableBodyPropertyObject.insertBefore(trElement, TableRowPropertyObject);
    TableRowPropertyObject.removeNode(true);
    TableBodyPropertyObject.insertBefore(TableRowPropertyObject, trElement);
    var i;
    for (i = 0; i < tCols; i++) {
        var nc = trElement.insertCell();
        nc.insertAdjacentText("afterBegin", " ");
    }
    ClearMenu();
}
//
//----------
//
function findColumn() {
    var i;
    for (i = 0; i < TableRowPropertyObject.childNodes.length; i++) {
        if (TableRowPropertyObject.childNodes[i] === TableCellPropertyObject) {
            return i;
        }
    }
}
//
//----------
//
function insertColLeft() {
    var colIndex = findColumn();
    var tRows = TableBodyPropertyObject.getElementsByTagName("TR");
    for (i = 0; i < tRows.length; i++) {
        if (tRows[i].childNodes[colIndex]) {
            if (tRows[i].childNodes[colIndex].tagName === "TD") {
                var newTD = tRows[i].insertCell(colIndex);
                newTD.insertAdjacentText("afterBegin", " ");
                newTD.style.width = Math.round(50 / tRows[i].childNodes.length) + "%";
            }
        }
    }
    ClearMenu();
}
//
//----------
//
function deleteColumn() {
    var colIndex = findColumn();
    var tRows = TableBodyPropertyObject.getElementsByTagName("TR");
    for (i = 0; i < tRows.length; i++) {
        if (tRows[i].childNodes[colIndex]) {
            if (tRows[i].childNodes[colIndex].tagName === "TD") {
                tRows[i].childNodes[colIndex].removeNode(true);
            }
        }
    }
    ClearMenu();
}
//
//----------
//
function insertColRight() {
    var colIndex = findColumn();
    var tRows = TableBodyPropertyObject.getElementsByTagName("TR");
    for (i = 0; i < tRows.length; i++) {
        if (tRows[i].childNodes[colIndex]) {
            if (tRows[i].childNodes[colIndex].tagName === "TD") {
                var newTD = tRows[i].insertCell(colIndex + 1);
                newTD.insertAdjacentText("afterBegin", " ");
                newTD.style.width = Math.round(50 / tRows[i].childNodes.length) + "%";
            }
        }
    }
    ClearMenu();
}
//
//----------
//
function deleteRow() {
    TableRowPropertyObject.removeNode(true);
    ClearMenu();
}
//
//----------
//
function showPanel(panelNum) {
    if (currentPanel != null) {
        hidePanel();
    }
    document.getElementById("panel" + panelNum).style.visibility = "visible";
    currentPanel = panelNum;
    setState(panelNum);
}
//
//----------
//
function hidePanel() {
    document.getElementById("panel" + currentPanel).style.visibility = "hidden";
    document.getElementById("tab" + currentPanel).style.backgroundColor = "#ffffff";
    document.getElementById("tab" + currentPanel).style.color = "navy";
}
//
//----------
//
function setState(tabNum) {
    if (tabNum === currentPanel) {
        document.getElementById("tab" + tabNum).style.backgroundColor = "#ddddff";
        document.getElementById("tab" + tabNum).style.color = "red";
    } else {
        document.getElementById("tab" + tabNum).style.backgroundColor = "#ffffff";
        document.getElementById("tab" + tabNum).style.color = "navy";
    }
}
//
//----------
//
function hover(tab) {
    tab.style.backgroundColor = "ddddff";
}
//
//----------
//
function showTab(tabnum) {
    var tabsDef = new Array(4)
    for (i = 1; i <= tabsDef.length; i++) {
        tabName = "tabContent" + i;
        eval(tabName + ".style.display=\"none\"");
        tabName = "tabLabel" + i;
        eval(tabName + ".style.backgroundColor=\"lightgrey\"");
        eval(tabName + ".style.fontWeight=\"normal\"");
        undName = "tabUnderline" + i;
        eval(undName + ".style.backgroundColor=\"white\"");
    }
    tabName = "tabContent" + tabnum;
    eval(tabName + ".style.display=\"block\"");
    tabName = "tabLabel" + tabnum;
    eval(tabName + ".style.backgroundColor=\"lightgrey\"");
    eval(tabName + ".style.fontWeight=\"bold\"");
    undName = "tabUnderline" + tabnum;
    eval(undName + ".style.backgroundColor=\"lightgrey\"");
}
//
//----------
//
function ImagePreview(ImageURL, ImageHeight, ImageWidth) {
    var Options
    Options = "menubar=no,toolbar=no,location=no,status=no,scrollbars=no,resizable,directories=no"
    if (ImageWidth) {
        Options += ",width=" + ImageWidth;
    }
    if (ImageHeight) {
        Options += ",height=" + ImageHeight;
    }
    var MsgWin = window.open("", "_blank", Options)
    var MsgText = "<HTML><body bgcolor=\"white\" topmargin=\"0\" leftmargin=\"0\" marginwidth=\"0\" marginheight=\"0\">" + ImageURL + "</BODY></HTML>"
    MsgWin.document.write(MsgText);
}
//
//----------
//
var LastBubbleID;
function HelpBubbleOn(BubbleID, Source) {
    var DivTarget, DivLeft, DivTop, pageYOffset;
    //
    HelpBubbleOff(LastBubbleID);
    DivTarget = document.getElementById(BubbleID);
    if (DivTarget) {
        if (navigator.appName === "Microsoft Internet Explorer") {
            pageYOffset = window.document.body.scrollTop;
        } else {
            pageYOffset = window.pageYOffset;
        }
        DivLeft = getPageOffsetLeft(Source) + Source.offsetWidth;
        if (DivLeft > document.body.scrollWidth) {
            DivLeft = document.body.scrollWidth - DivTarget.offsetWidth;
        }
        DivTop = getPageOffsetTop(Source) - DivTarget.offsetHeight + 15;
        if (DivTop < pageYOffset) {
            DivTop = pageYOffset;
        }
        DivTarget.style.visibility = "visible";
        DivTarget.style.display = "block"; //move to inline, not end of body
        LastBubbleID = BubbleID;
    }
}
//
//----------
//
function HelpBubbleOff(BubbleID) {
    var DivTarget = document.getElementById(BubbleID);
    if (DivTarget) {
        DivTarget.style.visibility = "hidden";
        DivTarget.style.display = "none"; //move to inline, not end of body
    }
}
//
//----------
//
function HelpBubbleHover(BubbleID, Source) {
    var DivTarget;
    var DivLeft, DivTop;
    //
    DivTarget = document.getElementById(BubbleID);
    if (DivTarget) {
        DivTarget.style.visibility = "visible";
    }
}
//
//----------
//
function HelpBubbleAjaxOn(BubbleID, Source, ContentName, FieldName) {
    var DivTarget;
    var DivLeft, DivTop;
    //
    HelpBubbleOff(LastBubbleID);
    DivTarget = document.getElementById(BubbleID);
    if (DivTarget) {
        DivLeft = getPageOffsetLeft(Source) + Source.offsetWidth;
        if (DivLeft > document.body.scrollWidth) {
            DivLeft = document.body.scrollWidth - DivTarget.offsetWidth;
        }
        DivTop = getPageOffsetTop(Source) - DivTarget.offsetHeight + 15;
        if (DivTop < 0) {
            DivTop = 0;
        }
        DivTarget.style.left = DivLeft + "px";
        DivTarget.style.top = DivTop + "px";
        DivTarget.style.visibility = "visible";
        LastBubbleID = BubbleID;
    }
}
//
//------------------------------------------------------------------------------------------------------
//	Update the help message
//
function updateFieldHelp(fieldId, helpEditorId, helpClosedContentId) {
    var eDst = document.getElementById(helpClosedContentId);
    var eSrc = document.getElementById(helpEditorId);
    eDst.innerHTML = eSrc.value;
    jQuery.ajax({
        url: "/setAdminSiteFieldHelp?fieldId=" + fieldId + "&helpCustom=" + eSrc.value,
    }).done(function (data) {
        //
    });
}
//
//----------
//
function InsertFolderRow() {
    var RLTable = document.getElementById("AddFolderTable");
    var CountElement = document.getElementById("AddFolderCount");
    var tCols, tRows, NewRowNumber;
    var NewRow, NewCell1, NewCell2, NewCell3, NewCell4;
    var RowCnt = RLTable.rows.length;
    if (RLTable) {
        NewRowNumber = parseInt(CountElement.value) + 1;
        CountElement.value = NewRowNumber;
        tRows = RLTable.getElementsByTagName("TR");
        NewRow = RLTable.insertRow(RowCnt);
        NewCell1 = NewRow.insertCell(0);
        NewCell1.align = "right";
        NewCell1.innerHTML = NewRowNumber + "&nbsp;";
        NewCell3 = NewRow.insertCell(1);
        NewCell3.innerHTML = "<INPUT TYPE=\"Text\" NAME=\"FolderName." + NewRowNumber + "\" SIZE=\"30\">";
        NewCell4 = NewRow.insertCell(2);
        NewCell4.innerHTML = "<INPUT TYPE=\"Text\" NAME=\"FolderDescription." + NewRowNumber + "\" SIZE=\"40\">";
    }
}
//
//----------
//
function InsertUploadRow() {
    var RLTable = document.getElementById("UploadInsert");
    var CountElement = document.getElementById("LibraryUploadCount");
    var tCols, tRows, NewRowNumber;
    var RowCnt = RLTable.rows.length;
    var NewRow, NewCell1, NewCell2, NewCell3, NewCell4;
    if (RLTable) {
        NewRowNumber = parseInt(CountElement.value) + 1;
        CountElement.value = NewRowNumber;
        tRows = RLTable.getElementsByTagName("TR");
        NewRow = RLTable.insertRow(RowCnt);
        NewCell1 = NewRow.insertCell(0);
        NewCell1.align = "right";
        NewCell1.innerHTML = NewRowNumber + "&nbsp;";
        NewCell2 = NewRow.insertCell(1);
        NewCell2.innerHTML = "<INPUT TYPE=\"file\" name=\"LibraryUpload." + NewRowNumber + "\">";
        NewCell3 = NewRow.insertCell(2);
        NewCell3.innerHTML = "<INPUT TYPE=\"Text\" NAME=\"LibraryName." + NewRowNumber + "\" SIZE=\"30\">";
        NewCell4 = NewRow.insertCell(3);
        NewCell4.innerHTML = "<INPUT TYPE=\"Text\" NAME=\"LibraryDescription." + NewRowNumber + "\" SIZE=\"40\">";
    }
}
//
//----------
//
function InsertUpload() {
    var RLTable = document.getElementById("UploadInsert");
    var CountElement = document.getElementById("UploadCount");
    var tCols, tRows, NewRowNumber;
    var RowCnt = RLTable.rows.length;
    var NewRow, NewCell1, NewCell2, NewCell3, NewCell4;
    if (RLTable) {
        NewRowNumber = parseInt(CountElement.value);
        CountElement.value = NewRowNumber + 1;
        tRows = RLTable.getElementsByTagName("TR");
        NewRow = RLTable.insertRow(RowCnt);
        NewCell2 = NewRow.insertCell(0);
        NewCell2.innerHTML = "<INPUT TYPE=\"file\" name=\"Upload" + NewRowNumber + "\">";
    }
}
//
//----------
//
function RLRowClick(IsChecked, RowID) {
    var RLRow = document.getElementById(RowID);
    if (IsChecked) {
        RLRow.style.backgroundColor = "#F0F0F0";
    } else {
        RLRow.style.backgroundColor = "";
    }
}
//
//----------
//
var DPanel = "0";
function ToggleToolPanelDev() {
    if (DPanel === "0") {
        DPanel = "1";
        document.all["ToolPanelDev"].style.visibility = "visible";
    } else {
        DPanel = "0";
        document.all["ToolPanelDev"].style.visibility = "hidden";
    }
    return false;
}
//
//----------
//
function SubmitToolsPanel() {
    var SelectedObject;
    var TextRange;
    SelectedObject = document.selection;
    TextRange = SelectedObject.createRange();
    if (TextRange.text) {
        if (TextRange.text.length !== 0) {
            document.all["ToolsPanelSelection"].value = TextRange.htmlText;
        }
    }
    return true;
}
//
//----------
//
function InsertTemplateIcon(ImageLink, ObjectName) {
    ExecCmdPlus("InsertImage", false, ImageLink, ObjectName);
    ClearMenu();
}
//
//----------
//
function switchContentFolderDiv(ShowID, HideID, ContentCaptionDivID, ContentCaption, EmptyDivID) {
    var ShowDiv, HideDiv, CaptionDiv, EmptyDiv, ptr;
    var HiddenInput, Hit;
    //
    if (ShowID === HideID) { return false; }
    //
    // hide old content
    //
    Hit = false;
    HideDiv = document.getElementById(HideID);
    if (HideDiv) {
        if (HideDiv.id === HideID) {
            HideDiv.style.display = "none";
            Hit = true;
        }
    }
    if (!Hit) {
        HiddenInput = document.getElementsByName(HideID);
        if (HiddenInput.length > 0) {
            for (ptr = 0; ptr < HiddenInput.length; ptr++) {
                HiddenInput[ptr].parentNode.style.display = "none";
            }
        }
    }
    if (!Hit) {
        EmptyDiv = document.getElementById(EmptyDivID)
        if (EmptyDiv) {
            EmptyDiv.style.display = "none";
        }
    }
    //
    // show new content
    //
    Hit = false;
    ShowDiv = document.getElementById(ShowID);
    if (ShowDiv) {
        if (ShowDiv.id === ShowID) {
            //ShowDiv.style.visibility = "hidden";
            ShowDiv.style.display = "block";
            //ShowDiv.style.visibility = "visible";
            Hit = true;
        }
    }
    if (!Hit) {
        HiddenInput = document.getElementsByName(ShowID);
        if (HiddenInput.length > 0) {
            Hit = true
            for (ptr = 0; ptr < HiddenInput.length; ptr++) {
                //HiddenInput[ptr].parentNode.style.visibility = "hidden";
                HiddenInput[ptr].parentNode.style.display = "block";
                //HiddenInput[ptr].parentNode.style.visibility = "visible";
            }
        }
    }
    if (!Hit) {
        EmptyDiv = document.getElementById(EmptyDivID)
        if (EmptyDiv) {
            EmptyDiv.style.display = "block";
        }
    }
}
//
//----------
//
function SetDisplay(Id, Value) {
    document.getElementById(Id).style.display = Value;
}
//
// Close TextAreaExpandable
//
function CloseTextArea(TextAreaID) {
    var tc = document.getElementById(TextAreaID);
    var to = document.getElementById(TextAreaID + "Opened");
    var hc = document.getElementById(TextAreaID + "Head")
    var ho = document.getElementById(TextAreaID + "HeadOpened")
    var HeadHeight = 24;
    scroll(0, 0);
    tc.value = to.value;
    ho.style.display = "none";
    to.style.display = "none";
    hc.style.display = "block";
    tc.style.display = "block";
    document.body.style.overflow = "";
    tc.focus();
}

//
// Open TextAreaExpandable
//
function OpenTextArea(TextAreaID) {
    window.scroll(0, 0);
    var tc = document.getElementById(TextAreaID);
    var to = document.getElementById(TextAreaID + "Opened");
    var hc = document.getElementById(TextAreaID + "Head");
    var ho = document.getElementById(TextAreaID + "HeadOpened");
    var HeadHeight = hc.offsetHeight;
    var h;
    to.value = tc.value;
    document.body.style.overflow = "hidden";
    if (document.compatMode && document.compatMode !== "BackCompat") {
        document.body.style.height = "100%";
        h = window.size().height;
        w = window.size().width;
        document.body.style.height = "";
    } else {
        if (document.body.style.overflow === "hidden") {
            w = document.body.offsetWidth;
        } else {
            w = document.body.offsetWidth - 22;
        }
        h = document.body.offsetHeight - 4;
    }
    if (!browser.isIE) {
        ho.style.width = "100%";
        to.style.width = "100%";
        to.style.top = HeadHeight + "px";
        to.style.height = h + "px";
        to.style.overflow = "auto";
        to.style.zIndex = "9999";
    } else {
        ho.style.width = w + "px";
        to.style.width = w + "px";
        to.style.top = HeadHeight + "px";
        to.style.height = (h - HeadHeight) + "px";
        to.style.overflow = "auto";
        to.style.zIndex = "9999";
    }
    ho.style.display = "block";
    to.style.display = "block";
    to.focus()
}
//
// Disable all buttons on the page
//
function processSubmit(e) {
    if (typeof (docLoaded) !== "undefined") {
        if (!docLoaded) {
            alert("This page has not loaded completed. Please wait for the page to load before submitting the form. If the page has loaded, there may have been an error. Please refresh the page.");
            return false;
        }
    }
    var x = document.getElementsByTagName("input");
    for (var i = 0; i < x.length; i++) {
        if ((x[i].type === "submit") && (x[i] !== e)) {
            x[i].disabled = true;
        }
    }
}
//
//
//
function EncodeAddonOptionArgument(EncodedArg) {
    var a = EncodedArg;
    a = a.replace(/&/g, "#0038#");
    a = a.replace(/=/g, "#0061#");
    a = a.replace(/,/g, "#0044#");
    a = a.replace(/\\/g, "#0034#");
    a = a.replace(/'/g, "#0039#");
    a = a.replace(/\|/g, "#0124#");
    a = a.replace(/\[/g, "#0091#");
    a = a.replace(/]/g, "#0093#");
    a = a.replace(/:/g, "#0058#");
    return a;
}
//
//
//
function DecodeAddonOptionArgument(EncodedArg) {
    var a = EncodedArg;
    a = a.replace(/#0058#/g, ":");
    a = a.replace(/#0093#/g, "]");
    a = a.replace(/#0091#/g, "[");
    a = a.replace(/#0124#/g, "|");
    a = a.replace(/#0039#/g, "'");
    a = a.replace(/#0034#/g, "\"");
    a = a.replace(/#0044#/g, ",");
    a = a.replace(/#0061#/g, "=");
    a = a.replace(/#0038#/g, "&");
    return a;
}
//
//-------------------------------------------------------------------------
//	child list methods
//-------------------------------------------------------------------------
//
// function saveSortable(listId) {
//     var e, c, s;
//     s = listId;
//     e = document.getElementById(listId);
//     for (i = 0; i < e.childNodes.length; i++) {
//         c = e.childNodes[i];
//         if (c.id) { s += "," + c.id }
//     }
//     cj.ajax.addon("savePageManagerChildListSort", "sortlist=" + s)
// }
// $(function () {
//     jQuery(".ccEditWrapper .ccChildList").sortable({
//         items: "li.allowSort",
//         stop: function (event, ui) {
//             saveSortable(jQuery(this).attr("id"));
//         },
//         connectWith: ".ccChildList",
// 		receive: function(event,ui) {
//             saveSortable(jQuery(this).attr("id"));
// 		},		
// 		placeholder: "ui-state-highlight"
//     });
// })
//
//-------------------------------------------------------------------------
// Legacy
//-------------------------------------------------------------------------
//
/*
* Admin List, submit FindButton if enter while find input field focus 
*/
jQuery(".findInput").keypress(function (e) {
    if (e.which === 13) {
        e.stopPropagation();
        e.preventDefault();
        jQuery("#FindButton").click();
    }
});
/*
 *  dynamicTab
*/
var numRows;
var numCols;
var initialized = 0;
var topAndBottomBarHeight = 50;
var ContentHeight = 0;

function TabInit() {
    var i;
    return false;
    if (initialized === 0) {
        initialized = 1;
        var AllContentDivs = document.getElementsByTagName("div");
        var ContentTop = 999;
        var styleTop = 0;
        var FirstTop = 0;
        //
        for (i = 0; i < AllContentDivs.length; i++) {
            if (AllContentDivs[i].className === "ccAdminTabBody") {
                if (AllContentDivs[i].style.display === "block") {
                    FirstTop = getTop(AllContentDivs[i]);
                } else {
                }
                styleTop = getTop(AllContentDivs[i]);
                if (ContentTop > styleTop) {
                    ContentTop = styleTop;
                }
                if (AllContentDivs[i].offsetHeight > ContentHeight) {
                    ContentHeight = AllContentDivs[i].offsetHeight;
                }
            }
        }
    }
}


function switchLiveTab2(ContentDivID, ClickedTab, TabTableID, StylePrefix, TabWrapperID) {
    var e, ePtr
    var i, TCnt, BCnt, RCnt;
    var newIndex;
    var oldIndex;
    var TabTable;
    var TBodyNode, TRNode, TDNode
    var NewContentDiv;
    var OldContentDiv;
    var TabWrapper;
    var StyleHeight;
    var width;
    //
    if (ActiveTabTableID === TabTableID) { return false; }
    NewContentDiv = document.getElementById(ContentDivID);
    OldContentDiv = document.getElementById(ActiveContentDivID);
    TabWrapper = document.getElementById(TabWrapperID);
    var classHide = NewContentDiv.className;
    var classShow = OldContentDiv.className;
    //
    // hide old content
    //
    OldContentDiv.className = classHide;
    NewContentDiv.className = classShow;
    //
    // turn off current active tab
    //
    TabTable = document.getElementById(ActiveTabTableID)
    for (TCnt = 0; TCnt < TabTable.childNodes.length; TCnt++) {
        TBodyNode = TabTable.childNodes[TCnt];
        if (TBodyNode.tagName === "TBODY") {
            for (BCnt = 0; BCnt < TBodyNode.childNodes.length; BCnt++) {
                TRNode = TBodyNode.childNodes[BCnt];
                for (RCnt = 0; RCnt < TRNode.childNodes.length; RCnt++) {
                    TDNode = TRNode.childNodes[RCnt];
                    if (TDNode.className === StylePrefix + "Hit") {
                        TDNode.className = StylePrefix;
                    }
                }
            }
        }
    }
    ActiveTabTableID = TabTableID
    // turn on new active tab
    TabTable = document.getElementById(TabTableID)
    for (TCnt = 0; TCnt < TabTable.childNodes.length; TCnt++) {
        TBodyNode = TabTable.childNodes[TCnt];
        if (TBodyNode.tagName === "TBODY") {
            for (BCnt = 0; BCnt < TBodyNode.childNodes.length; BCnt++) {
                TRNode = TBodyNode.childNodes[BCnt];
                for (RCnt = 0; RCnt < TRNode.childNodes.length; RCnt++) {
                    TDNode = TRNode.childNodes[RCnt];
                    if (TDNode.className === StylePrefix) {
                        TDNode.className = StylePrefix + "Hit";
                    }
                }
            }
        }
    }
    // toggle tab links
    var tabLinks = document.getElementsByName("tabLink");
    for (i = 0; i < tabLinks.length; i++) {
        if (tabLinks[i] !== ClickedTab) {
            tabLinks[i].className = StylePrefix + "Link";
            tabLinks[i].blur();
        }
        else {
            tabLinks[i].className = StylePrefix + "HitLink";
            tabLinks[i].blur();
        }
    }
    ActiveContentDivID = ContentDivID;
}
//
function getTop(el) {
    var p = el.offsetParent;
    var y = el.offsetTop;
    while (p != null) {
        y += p.offsetTop;
        p = p.offsetParent;
    }
    return y;
}
function getPadding(x) {
    if (x.currentStyle) {
        var Top = x.currentStyle["paddingTop"];
        var Bottom = x.currentStyle["paddingBottom"];
    } else if (window.getComputedStyle) {
        var Top = document.defaultView.getComputedStyle(x, null).getPropertyValue("padding-top");
        var Bottom = document.defaultView.getComputedStyle(x, null).getPropertyValue("padding-bottom");
    }
    var TopNoPx = Top.split("p");
    var BottomNoPx = Top.split("p");
    y = parseInt(TopNoPx[0]) + parseInt(BottomNoPx[0])
    return y;
}
//
function EmbedWiggle() {
    document.write("<input type=text name=\"iewiggle\" style=\"width:1px;display:none;enable:false\">")
}
/*
 * body on click
 */
function BodyOnClick() {
    hideselect(false);
}
/*
 * on ready bindings
 */
jQuery(document).ready(function () {
    jQuery("body").on("click",BodyOnClick);
    document.addEventListener("mousedown", PageClick, true);
    document.addEventListener("mouseclick", BodyOnClick, true);
})
/*
* cp.AdminUI referenced js
*/
/*
if (typeof google !== 'object') {
    document.write('<scr' + 'ipt type="text/javascript" src="https://www.google.com/jsapi"></scr' + 'ipt>');
}
*/

/*
* DateTime editor creates descrete inputs for date and for time. This method populates a datetime hidden used for request.
*/
function setDateTimeEditorHidden(htmlId) {
    var dateVal = $("#component-" + htmlId + "-date").val();
    var timeVal = $("#component-" + htmlId + "-time").val();
    $("#" + htmlId ).val(dateVal + ((timeVal) ? "T" + timeVal : "" ));
}
/*
* admin header command
*/
$( function() {
    $( "#cmdSearch" ).autocomplete({
      source: function (request, response) {
        $.ajax({
            dataType: "json",
            type : 'Get',
            url: '/AdminAutocompleteCommands?term=' + request.term,
            success: function(data) { response(data); },
            error: function(data) { console.log("error in remote method"); }
        });
    },
      select: function( event, ui ) { 
          console.log("selected label:" + ui.item.label + ", value: " + ui.item.value + ", url: " + ui.item.url); 
          window.location.href = ui.item.url;        
      }
    });
  } );