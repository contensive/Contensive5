
# TEmpalte Design Pattern

## Overview
The Contensive Page Manager addon creates a website using Contensive when configured. Page Manager composes a page by creating the body from a page template and constructing the content prepared by content managers.

## Base architecture and other pattens

- [Contensive Architecture](https://github.com/contensive/Contensive5/blob/master/patterns/contensive-architecture.md)

Best practice is to never include html code elements in the programming code. Html code is developed in html files in the /ui folder and is deployed to Layout records. Programming code reads layout records with the cp.Layout methods and merges html with data using the cp.Mustache methods.

### Page TEemplate recod

The system stores page templates for use in the Page Template table. Only the html inside the html's body tag is stored with the template. 

### Page rendering

A page is rendered by first associating the request url to a Link Alias record. Link Alias records have a url and the associated Page Content record id that handles the page.

If the root of the site is selected, which has no page name, the Domain Name record is looked up and the landing page property points to the Page Content record to be used.

The Page Content record is opened and the Page Template id is read, If it is blank, teh page's parentId is checked and if set, that page is checked for a Page Template. If the current page is not set, and all the parent pages are not set, then the Domain Name record is checked for the template id.

When a template id is determined, that record is looked up and the content in its Body field is scanned for embedded addons, which are executed. One embedded addon is the Content Box addon, which renders the html from the Page Content record and adds that to the page where the Content Box addon widget was located.

While excecuting the page, each page widget excecuted may have javascript and css included, which is added to the html heaad and end-of-body where appropriate. Addons may have addon dependencies, which check if the references addon has been run yet, and if not, runs it. These are used for libraries like Jquery and Bootstrap which should only excecute once no matter how many times they are required.

Other head tags are added from each addon that executes. The completed head and body are added to the html structure and returned

### Deploying Page Templates

Page Templates are deployed two ways, in a collection file or with the Html Import tools.

In a collection file an addon can either be a data record, which just populates a Page Template record. This is handy when a collection is exported from one site and imported into another. The collection can also include a resource tag to copy an html file into the destination website, then the onInstall addon of the collection can use the cp.Layout.Upgrade() method to create or update the page template record

The html import tool can be used at any time to upload an html template to the page TEmplate record. 

### Html Import tools

#### Mustache Properties and Data Properties
There are two types of replacement properties supported. Data properties modify the html as described. Mustache properties create html that supports Mustache templating. Mustache is a popular templating scheme. You may choose to include mustache tags in your html directly in which case the html may not render well in a browser. You can alternativly choose to set special styles outlined here and the import tool will add the Mustache tags you indicate. Reference any of the many Mustache references online.

#### data-mustache-section
Add a Mustache Section around content to be removed or repeated. If the object property is false, null, or an empty list, the section is removed. If the value is true the section is included. If the value is a list the section is repeated for each item in the list.

<ul data-mustache-section="staff"> <li data-mustache-variable="name">Sample Name</li> <li data-delete>Name To Skip</li> </ul> <ul> {{#staff}} <li class="">{{name}}</li> {{/staff}} </ul>

#### data-mustache-inverted-section
Add a Mustache Inverted Section data attrbiute around content to be included if the object property value is false.

<div data-mustache-inverted-section="emptyList"> <p>No items were found.</p> </div> <div> {{^emptyList}} <p>No items were found.</p> {{/itemList}} </div>

#### data-body
The data-body attribute is used to locate the html to be processed. Anything outside of this region will not be processed. If a data-body attribute is found, only the html within that element will be included. If no data-body is used, the content of the entire html body tag is imported.

<body><span data-body>This content will be included without the span tag</span> and this copy will not be imported</body> This content will be included without the span tag

#### data-layout
If a data-layout attribute is found, the html within that element will be saved to the named layout record.

<body><span data-layout="New-Site-Header">This content will be saved to the layout named 'New-Site-Header' without the span tag</span> and this copy will not be imported. If a tag includes both a data-delete and a data-layout, the innter content will be saved to a layout and deleted from the html.</body> This content will be included without the span tag

#### data-cdn
Set data-cdn to an attribute in the html tag, like src, and the file in the url will be copied to the cdn and the html will be updated.

<body><div><img data-cdn="src" src="/img/sample.png"></div></body> When imported, the image in src will be copied to the cdn data source, and the html will be updated to target the cdn version.

#### data-href
Adds an href to the current element, replacing what is there if it has one.

<body><p><a href="MainMenu.html" data-href="{{/mainmenu}}">Click here to see the main menu.</a></p></body> The html will click to MainMenu.html during design. When imported, it will click to /menumenu.

#### data-value
Replace the value of the html tag with the provided value.

<p>My example is <span value="0" data-value="{{id}}">content</span>.</p> <p>My example is <span value="{{id}}">content</span>.</p>

#### data-src
Adds an src to the current element, replacing what is there if it has one.

<body><image src="placeholder-image.jpg" data-src="{{user-photo}}"></body> The html will show placeholder-image.jpg. When imported, the src will be the mustache tag {{user-photo}}.

#### data-alt
Adds an alt to the current element, replacing what is there if it has one.

<body><image src="image.jpg" data-alt="{{photo-alt}}"></body> The html will have no alt tag. When imported, the alt tag will be {{photo-alt}}.

#### data-addon
NOTE: If the addon name contains spaces, replace each space with a _ instead. Ex. content box would be content_box. Replace the inner content of the html tag with the addon after the Mustache Addon tag.

<span data-addon="content_box">content</span> <span>{% "content box" %}</span>

#### data-innertext
<div><span data-innertext="{{myMustacheProperty}}">content</span></div> <div><span>{{myMustacheProperty}}</span></div>

#### data-delete
Delete the tag that contains this class, and all child tags.

<p>This is in the layout.<span data-delete>This is not.</span></p> <p>This is in the layout.</p>

#### data-mustache-value
Legacy. Use data-value instead, adding your own mustache braces.

#### data-mustache-variable
Legacy, use data-innertext instead.
