Add-on Collection files are zip files that install collections. They are created by zipping an xml collection file with all the individual resources files it references.

### XML Collection File

<Addon name="Personalization Default" guid="{FF864BEF-8021-4742-AFF0-4AB7D3CB4E9A}" type="Widget">
<DotNetClass><![CDATA[Contensive.Processor.Addons.PersonalizeDefaultAddon]]></DotNetClass>
</Addon>

<data>
<record content="sort methods" guid="{97128516-AEDF-4B6C-BC56-F6EAA4C3AA78}" name="By Name">
<field name="OrderByClause"><![CDATA[Name]]></field>
</record>
</data>

<NavigatorEntry Name="Trap Report" NameSpace="Reports" NavIconTitle="" NavIconType="Report" LinkPage="?af=12&amp;rid=36" ContentName="" AdminOnly="0" DeveloperOnly="0" NewWindow="" Active="0" AddonGuid="" guid="{018FA96F-43F3-4AAE-9794-0450CE2F9318}"/>

<ImportCollection name="Add-on Manager">{A3E58089-74FC-4FB3-B847-0AD22B51AA72}</ImportCollection>

<Resource name="baseassets.zip" type="www" path="baseAssets" />

<CDef Name="Languages" NavTypeId="System" addoncategoryid="" ActiveOnly="1" AdminOnly="0" AliasID="ID" AliasName="NAME" AllowAdd="1"  AllowContentTracking="0" AllowDelete="1" AllowTopicRules="0" AllowWorkflowAuthoring="0" AuthoringDataSourceName="DEFAULT" AuthoringTableName="ccLanguages" ContentDataSourceName="DEFAULT" ContentTableName="ccLanguages" DefaultSortMethod="By Name" DeveloperOnly="0" DropDownFieldList="NAME" EditorGroupName="" Parent="" AllowContentChildTool="0" NavIconType="Content" IconLink="" IconWidth="" IconHeight="" IconSprites="" guid="{46C9A881-3D07-4C03-917B-9F5F545E54C3}">
<Field Name="Name" active="1" AdminOnly="0" Authorable="1" Caption="Name" DeveloperOnly="0" EditSortPriority="110" FieldType="Text" HTMLContent="0" IndexColumn="0" IndexSortDirection="0" IndexSortOrder="0" IndexWidth="40" LookupContent="" NotEditable="0" Password="0" ReadOnly="0" RedirectContent="" RedirectID="" RedirectPath="" Required="0" TextBuffered="0" UniqueName="0" DefaultValue="" RSSTitle="" RSSDescription="" MemberSelectGroup="" EditTab="" Scramble="0" LookupList="" ManyToManyContent="" ManyToManyRuleContent="" ManyToManyRulePrimaryField="" ManyToManyRuleSecondaryField="">
<HelpDefault><![CDATA[ ]]></HelpDefault>
</Field>
<Field Name="HTTP_Accept_Language" active="1" AdminOnly="0" Authorable="1" Caption="Browser Abbreviation" DeveloperOnly="0" EditSortPriority="1010" FieldType="Text" HTMLContent="0" IndexColumn="1" IndexSortDirection="0" IndexSortOrder="1" IndexWidth="10" LookupContent="" NotEditable="0" Password="0" ReadOnly="0" RedirectContent="" RedirectID="" RedirectPath="" Required="0" TextBuffered="0" UniqueName="0" DefaultValue="" RSSTitle="" RSSDescription="" MemberSelectGroup="" EditTab="" Scramble="0" LookupList="" ManyToManyContent="" ManyToManyRuleContent="" ManyToManyRulePrimaryField="" ManyToManyRuleSecondaryField="">
<HelpDefault><![CDATA[ ]]></HelpDefault>
</Field>
</CDef>
