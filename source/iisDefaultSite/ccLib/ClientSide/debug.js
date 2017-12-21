//
//*****************************************************************************
// Do not remove this notice.
//
// Copyright 2000, 2001 by Mike Hall.
// See http://www.brainjar.com for terms of use.
//*****************************************************************************

// Netscape 6 (or Mozilla)?

var isNS6 = (navigator.userAgent.indexOf("Gecko") > 0) ? 1 : 0;

// Array for tracking objects to expand or collapse.

var objectList = new Array();

function showProperties(obj, name) {

  var i, j, s;
  var propertyList, temp;
  var property, value;
  var lines;

  if (!name && obj && obj.id)
    name = obj.id;

  // Create a list of the object's properties sorted alphabetically by
  // name so they can be listed in order.

  propertyList = new Array();
  for (property in obj)
    propertyList[propertyList.length] = String(property);
  propertyList.sort();

  // Build a list of properties and values for this object as a string
  // in HTML format.

  s = "v1<BR><ul>";

  for (i = 0; i < propertyList.length; i++) {
    property = String(propertyList[i]);
    try {
      value = String(obj[propertyList[i]]);
    }
    catch (exception) {
      value = "<i>" + String(exception) + "</i>"
    }

    // If the object property is itself an object, create it as a link
    // so its properties can be expanded and collapsed.

    if (value.indexOf("[") == 0 && value.lastIndexOf("]") == value.length - 1) {
      objectList[objectList.length] = obj[propertyList[i]];
      value = makeLink(objectList.length - 1, name + "." + property, value);
    }

    // If the property text contains HTML, encode it for display.

    if (property == "innerHTML" || property == "outerHTML") {
      var lines = value.split("\n");
      value = "";
      for (j = 0; j < lines.length; j++) {
        lines[j] = lines[j].replace(/(.?)<(.?)/g, "$1&lt;$2");
        lines[j] = lines[j].replace(/(.?)>(.?)/g, "$1&gt;$2");
        value += lines[j];
      }
    }

    // Add the property/value string as an HTML list item.

    s += "<li>" + name + "." + property + " = " + value + "</li>";
  }

  // For Netscape, enumerate items in an array or collection.
  // Note: IE reflects individual item using the index number as
  // the property name so this is not necessary.

  if (isNS6 && obj.item) {
    for (j = 0; j < obj.length; j++) {
      objectList[objectList.length] = obj[j];
      temp = makeLink(objectList.length - 1, name + "[" + j + "]",
                      String(obj[j]));
      if (obj[j])
        s += "<li>" + name + "[" + j + "] " + temp + "</li>";
    }
  }

  // End the HTML list and return the string.

  s += "</ul>";

  return s;
}

function makeLink(i, name, text) {

  return '<a class="object" href="" onclick="'
       + 'if (!this.isExpanded)'
       + 'createList(this,objectList[' + i + '],\'' + name + '\');'
       + 'else destroyList(this);event.cancelBubble=true;return false;">'
       + text + '</a>';
}

function createList(target, obj, name) {

  var node;

  // Generate property list and append it to document after the current node.

  node = document.createElement("SPAN");
  node.innerHTML = showProperties(obj, name);
  target.parentNode.appendChild(node);
  target.isExpanded = true;
}

function destroyList(target) {

  // Remove a generated property list from the document.

  target.parentNode.removeChild(target.parentNode.lastChild);
  target.isExpanded = false;
}

