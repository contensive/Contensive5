## Portal Edit Page Description

An edit portal page creates a page in a portal that lets a user edit a group of fields.
- the page is built by creating an object using cp.AdminUI.CreateLayoutBuilderNameValue(), populating the object's properties, and calling the object's getHtml() method.

An example of the Edit Page pattern is in 
- https://github.com/contensive/aoFormWizard/tree/master/Source/aoFormWizard3/Addons/FormEdit/FormPageEditAddon.cs

The following rules describe the best practices for creating a portal edit page
- Rename the file to follow the new patten, remove the Ecommerce prefix and add the EditAddon suffix to represent that these are Edit-type, and are addons.
- In this previous pattern, the addon file references a Get and a Process method in a View file. In the new pattern, move the get and process methods to the addon file and delete the view file
- the file \collections\ecommerce\ecommerce.xml is an installation for this pattern. Each addon xml element represents one addon to install, and in that element is the dotnet namespace of the file. Each addon must also have a data record for a Portal Feature. in the new pattern, those two elements are moved next to each other at the top of the file.
- each addon has 2 constants in the constants file, one for the guid of the addon and one for guid of the portal feature. These guids must match the entries for the addon and the portal feature in the xml file. Move these two constants to the addon file and rename them as guidPortalFeature, guidAddon.
- Add a constant viewName to each file that is the name of the view.
- change the process() method to return false if the process method executes a redirect method, and the get() method should then return a blank string immediately. If the process() method returns true then the execute methods should return the result from the get() method.
- the execute method should include an authentication check, but the check for "validate portal environment" should be moved to the process() method and if the validate fails, the call redirect and return false from the process method, which then exits from execute() immediately with a blank string.
- querystring and form arguments are read with the cp.doc.getText(), cp.doc.getInteger(), etc methods. Instead of reading these arguments directly in the get and process methods, instead create a new private class in the file called RequestClass that exposes public properties for each of the arguments read from the request. In the RequestClass constructor populate the public properties using cp.doc.getText() cp.doc.getInteger(), etc, then in execute() create an instance of the class called request, and pass that object into the process and get methods to provide these arguments.
- In the execute method, create a string userErrorMessage and pass it into both the process and the get methods. If there is an error in the process() method, set a user message in this variable and after process() returns, then pass it into the get method.
