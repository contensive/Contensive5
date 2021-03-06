'
' documentation should be in a new project that inherits these classes. The class names should be the object names in the actual cp project
'
Namespace Contensive.BaseClasses
    ''' <summary>
    ''' CP.Addon - The Addon class represents the instance of an add-on. To use this class, use its constructor and open an cpcore.addon. 
    ''' Use these properties to retrieve it's configuration
    ''' </summary>
    ''' <remarks></remarks>
    Public MustInherit Class CPAddonBaseClass
        '
        '====================================================================================================
        ''' <summary>
        ''' When true, this add-on is displayed on and can be used from the admin navigator.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property Admin() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' A crlf delimited list of name=value pairs. These pairs create an options dialog available to administrators in advance edit mode. When the addon is executed, the values selected are available through the cp.doc.var("name") method.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property ArgumentList() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' When true, this addon returns the javascript code necessary to implement this object as ajax.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property AsAjax() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' When true, the system only uses the custom styles field when building the page. This field is not updated with add-on updates.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property BlockDefaultStyles() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' The guid used to uniquely identify the add-on
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property ccGuid() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' The ID local to this site of the collection which installed this cpcore.addon.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property CollectionID() As Integer
        '
        '====================================================================================================
        ''' <summary>
        ''' When true, this addon can be placed in the content of pages.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property Content() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' text copy is added to the addon content during execution.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks>
        ''' Addon content is assembled in the following order: TextContent + HTMLContent + IncludeContent + ScriptCallbackContent + FormContent + RemoteAssetContent + ScriptContent + ObjectContent + AssemblyContent.
        ''' </remarks>
        Public MustOverride ReadOnly Property Copy() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' text copy is added to the addon content during execution.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks>
        ''' Addon content is assembled in the following order: TextContent + HTMLContent + IncludeContent + ScriptCallbackContent + FormContent + RemoteAssetContent + ScriptContent + ObjectContent + AssemblyContent.
        ''' </remarks>
        Public MustOverride ReadOnly Property CopyText() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' Styles that are rendered on the page when the addon is executed. Custom styles are editable and are not modified when the add-on is updated.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property CustomStyles() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' Styles that are included with the add-on and are updated when the add-on is updated. See BlockdefaultStyles to block these.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property DefaultStyles() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' The add-on description is displayed in the addon manager
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property Description() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' When present, the system calls the execute method of an objected created from this dot net class namespace.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks>
        ''' Addon content is assembled in the following order: TextContent + HTMLContent + IncludeContent + ScriptCallbackContent + FormContent + RemoteAssetContent + ScriptContent + ObjectContent + AssemblyContent.
        ''' </remarks>
        Public MustOverride ReadOnly Property DotNetClass() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' This is an xml stucture that the system executes to create an admin form. See the support.contensive.com site for more details.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property FormXML() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' This copy is displayed when the help icon for this addon is clicked.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property Help() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' If present, this link is displayed when the addon icon is clicked.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property HelpLink() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' When present, this icon will be used when the add-on is displayed in the addon manager and when edited. The height, width and sprites must also be set.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property IconFilename() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' The height in pixels of the icon referenced by the iconfilename.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property IconHeight() As Integer
        '
        '====================================================================================================
        ''' <summary>
        ''' The number of images in the icon. There can be multiple images stacked top-to-bottom in the file. The first is the normal image. the second is the hover-over image. The third is the clicked image.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property IconSprites() As Integer
        '
        '====================================================================================================
        ''' <summary>
        ''' The width of the icon referenced by the iconfilename
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property IconWidth() As Integer
        '
        '====================================================================================================
        ''' <summary>
        ''' The local ID of this addon on this site.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property ID() As Integer
        '
        '====================================================================================================
        ''' <summary>
        ''' When true, this addon will be displayed in an html iframe.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property InFrame() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' When true, the system will assume the addon returns html that is inline, as opposed to block. This is used to vary the edit icon behaviour.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property IsInline() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' Javascript code that will be placed in the document right before the end-body tag. Do not include script tags.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property JavaScriptBodyEnd() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' Javascript code that will be placed in the head of the document. Do no include script tags.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property JavascriptInHead() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' Javascript that will be executed in the documents onload event.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <Obsolete("Create onready or onload events within your javascript. This method will be deprecated.", False)> Public MustOverride ReadOnly Property JavaScriptOnLoad() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' A URL to a webserver that returns javascript. This URL will be added as the src attribute of a script tag, and placed in the content where this Add-on is inserted. This URL can be to any server-side program on any server, provided it returns javascript.
        ''' For instance, if you have a script page that returns javascript,put the URL of that page here. The addon can be dropped on any page and will execute the script. Your script can be from any site. This technique is used in widgets and avoids the security issues with ajaxing from another site.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property Link() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' Text here will be added to the meta description section of the document head.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property MetaDescription() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' This is a comma or crlf delimited list of phrases that will be added to the document's meta keyword list
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property MetaKeywordList() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' The name of the cpcore.addon.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property Name() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' The type of navigator entry to be made. Choices are: Add-on,Report,Setting,Tool
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property NavIconType() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' If present, this string will be used as an activex programid to create an object and call it's execute method.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property ObjectProgramID() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' If true, this addon will be execute at the end of every page and its content added to right before the end-body tag
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property OnBodyEnd() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' If true, this addon will be execute at the start of every page and it's content added to right after the body tag
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property OnBodyStart() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' if true, this add-on will be executed on every page and its content added right after the content box.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property OnContentEnd() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' If true, this add-on will be executed on every page and its content added right before the content box
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property OnContentStart() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' Open an add-on with it's local id before accessing its properties
        ''' </summary>
        ''' <param name="AddonId"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride Function Open(ByVal AddonId As Integer) As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' open an add-on with its name or guid before accessing its properties
        ''' </summary>
        ''' <param name="AddonNameOrGuid"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride Function Open(ByVal AddonNameOrGuid As String) As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' All content in the field will be added directly, as-is to the document head.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property OtherHeadTags() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' All content in the field will be added to the documents title tag
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property PageTitle() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' When present, this add-on will be executed stand-alone without a webpage periodically at this interval (in minutes).
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property ProcessInterval() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' The next time this add-on is scheduled to run as a processs
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property ProcessNextRun() As Date
        '
        '====================================================================================================
        ''' <summary>
        ''' Check true, this addon will be run once within the next minute as a stand-alone process.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property ProcessRunOnce() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property RemoteAssetLink() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' if true, this add-on can be executed as a remote method. The name of the addon is used as the url.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property RemoteMethod() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' When present, this text will be added to the robots.txt content for the site. This content is editable through the preferences page
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property RobotsTxt() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' When present, the first routine of this script will be executed when the add-on is executed and its return added to the add-ons return
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property ScriptCode() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' if the ScriptCode has more than one routine and you want to run one other than the first, list is here.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property ScriptEntryPoint() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' The script language selected for this script.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property ScriptLanguage() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' A comma delimited list of the local id values of shared style record that will display with this add-on
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property SharedStyles() As String
        '
        '====================================================================================================
        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property Template() As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="privatePathFilename"></param>
        ''' <param name="returnUserError"></param>
        ''' <returns></returns>
        Public MustOverride Function installCollectionFile(privatePathFilename As String, ByRef returnUserError As String) As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="collectionGuid"></param>
        ''' <param name="returnUserError"></param>
        ''' <returns></returns>
        Public MustOverride Function installCollectionFromLibrary(collectionGuid As String, ByRef returnUserError As String) As Boolean
    End Class
End Namespace
