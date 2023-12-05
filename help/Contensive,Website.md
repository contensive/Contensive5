
Favicon is the term used to describe the image used to represent the website by services like Google, Slack, Facebook, etc. 
In the simplest case, favicon refers to a file uploaded to the site named favicon.ico. A more complete implementation of Favicon can include many other files and settings.

## Adding an image as a simple Favicon

If no actual favicon.ico file has been uploaded directly to the website, an simpler alernate method is to upload a file to the Favicon setting in the Website tab of Site Settings.

There are many modern variations of the Favicon image(s). These can include other image and data files. In those cases, upload the files manually to the website, and add meta tags to templates related.

## Developers

When a file is uploaded to the Site Settings Favicon setting, the system adds a metatag to the website. 

<link rel=icon type=image/png href=/mysite/files/Settings/FaviconFilename/My-Uploaded-File-32x32.png>

Many services ignore this tag if an actual Favicon.ico file is present so verify there is no favicon.ico file in web-file space.

## References

[Google Developer details for Favicon](https://developers.google.com/search/docs/appearance/favicon-in-search)

