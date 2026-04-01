

A remote method is an executable endpoint, created and controlled by record in Add-ons. The execution of the remote method on an applicaton typically invokes dotnet code to generate a response, often formated json.

In a simpler case, the remote method might just return content stored in the Add-on record. This is a typical method used to mock a response for the UI during development, then use the final mocked response to model the code.

### Example

To create a simple mock remote
- Create a new Add-on record. (nav: Design > Add-ons)
- name = sampleMock
- Content tab, Content Text = Hello World
- Placement Tab, check 'Is Remote Method'
- save

Now in the url, go to http://applicationName/sampleMock

The response is Hello World

Change the content to {"success":true, "result":"Hello World"} and you have mocked a simple remote method

### Adding the Remote Method to a Collection

A Collection groups many remote methods together into one deployment. To create a collection, add a record to Add-on Collections (nav: Design > Add-on Collections) with the name MyDeployment and save.

Now go back to your Add-on and select this collection. 

You can now go the the Add-on Manager tool and export the collection. It will download a zip file that can be installed on a different site.

This zip file includes everything needed to install the add-ons, including an xml collection installation file.

You can install a collection by uploading to the Add-on Manager, but you might typically install using the cc.exe command line tool on the destination server. This could part of a deployment automation.

### Creating a Remote Method that calls a Dotnet Class Library

To continue with the previous example. Create a dotnet framework Class library solution with a single project called MyDotNetCode

Using Nuget, make a reference to Contensive.CPBaseClasses. Add an Add-on class as follows:
-----
using Contensive.BaseClasses;

namespace MyNamespace.MyProject {
	//
	// -- an addon executes the Execute method of classes that inherit AddonBaseClass, passing in the cp object, and returning an object.
	// -- the cp base object has properties and methods that abstrate services layouts to interface 
	// -- with the systems like the file systems, database, cache, http request, html document, etc.
	// -- if the object is a string, it is used as-is. If any other object it is json serialized.
	//
    public class SampleRemoteMethodClass : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
			var sampleObject = new SampleResponse {
				success = true,
				result = "DotNet code execution result"
			};
            return sampleObject;
        }
    }
	// sample response object
	public class SampleResponse {
		public bool success;
		public string result;
	}
}

Go back to the SampleMock add-on record and click Create Duplicate. Name the duplicate sampleDotnet.

In the Add-on record, add the Code tab, set DotNet Namespace Class to MyNamespace.MyProject.SampleRemoteMethodClass.

And finally, in the Content tab, remove the text from the Content text, and save.

Export the deployment changes using the Add-on Manager to a temp folder, and copy the DLL files from the project into the deployment zip file.

Reinstall the collection zip file.

Now in the url, go to http://applicationName/sampleDotNet returns 


DotNet code execution result

### Routing

The addon name becomes the endpoint. Any path that begins with with the addon name will execute this remote method.  Note that you cannot create an addon with a blank name, or the name "/"

For example
- Create an add-on named "api". It will be run for endpoints /api and anything that starts with /api, like /api/user/create
- Create two add-ons, "api" and "api/user". Endpoint /api/user/delete will go to addon "api/user", endpoint /api/group/add will go to addon "api"



