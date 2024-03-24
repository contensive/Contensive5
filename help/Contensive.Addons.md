


This /register thing we are working on is an Add-on. A much better word is "Feature", and I have started using that word, so I will here.

A feature in the system uses programming code + html layouts + assets like css,javascript,images to create something that can be added to a site.

There are several types of features in the system, but only two that are important here. 
A Widget = the type that you think of for a website. A customer might turn on the widget tool and drag-drop an image-carousel widget on the page, then edit the carousel and add the images they want
A Remote-Method = the type of widget that runs when you types its name in the URL. A remote-method can return anything you want to the display when it runs. If called by the javascript in the UI code, it might return a json string. Or it might return just plain text.

This register feature is a remote-method type

And one last piece of the puzzle. A feature can be configure so the system automatically builds an entire html5 document around it's output. So you might create a remote-method that produces the string "<b>Hello World</b>". If that feature is marked as an html document, all the html head etc would be created and the end-user would see a bold Hello World. If you removed the html-document setting, you would just see the text with the <b> etc.

We can set any url to any feature, but for simplicity we typically name the feature that same as the url that calls it, to make it easier for everyone to undertand. Since I was the one who worked on this, I didnt follow all those rules, but people should typically. 

For example, the URL '/register' should simply run a feature called "Register" (again, features are just entries in the "addons" table.)

When the Register feature runs, 
it first determines which view needs to be displayed, 
then gets the layout for that view. The layout is just the part of the html page for this feature. 
then replaces all the mustache {{tags}} in the layout with real data
then (if the feature is marked as as 'html' type), it builds the entire html5 wrapper around the little layout provided.

So imagine if there was a dynamic menu at the top of this register page. There would be one feature for the dynamic menu and a different feature for the register. Totally different features. Perhaps they both need jquery (or any common 3rd party library). In this case, both of these elements are added to the page by the designer, so theoretically the designer could add them, then go setup the 3rd party libraries for this page. That is possible.

But the system is built do a content manager (customer) can add features to a page. For example, a feature might be an image, or a text box. Customers turn on the Widget tool and drag-drop widgets on the page. (A widget is a type of feature)

So to handle this case, we can configure each feature to tell it what other features have to run before (we call them dependencies).
then menu feature is marked so it needs the boostrap feature to run before it runs
the Register feature is also marked so it depends on bootstrap.

Since the menu appears first on the page, it runs bootstrap, then the menu, then the register. Bootstrap is run only once, and always in the right order.

