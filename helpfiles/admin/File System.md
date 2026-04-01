
The filesystem refers to content files. It is divided into four locations depending on the purpose of the file: webFiles, cdnFiles, privateFiles and tempFiles. In addition, the site can be set to store files on Amazon AWS and keep a mirror copy locally or store files locally, depending on the site's needs

### Local vs Remote Files

In local files mode, content files are stored on the local server, typically on d:\inetpub\applicationName\

In remote files mode, content files are stored in an Amazon AWS S3 bucket. The local file space is used as a working mirror of the files recently accessed.

### cdnFiles

cdnFiles are content uploaded by the user. Files in this storage location can be shared publically.

### privateFiles

privateFiles are files used by the application and are not publically available.

### tempFiles

tempFiles are ephemeral files that are located on the local physical server.

### webFiles

Webfiles access the folder with the application script files.

### Developers

To access files in any of the file storage areas, use cp.cdnFiles, cp.privateFiles, cp.tempFiles, and cp.wwwFiles



