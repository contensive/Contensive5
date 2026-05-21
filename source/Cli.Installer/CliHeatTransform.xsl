<?xml version="1.0" encoding="utf-8"?>
<!--
  WiX Heat transform: excludes TaskService.exe from auto-harvested components
  because it is declared explicitly in Components.wxs with ServiceInstall/ServiceControl.
  Both the Component definitions and their ComponentRef entries are removed
  so the linker does not encounter duplicate or unresolved references.
-->
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">

  <!-- Identity transform: copy everything by default -->
  <xsl:output method="xml" indent="yes" />
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
    </xsl:copy>
  </xsl:template>

  <!-- Key: look up Component IDs by the source filename they contain.
       Use substring() to match only filenames that END with 'TaskService.exe'
       so that 'TaskService.exe.config' is not excluded. -->
  <xsl:key name="ExcludedComponents"
           match="wix:Component[wix:File[
             substring(@Source, string-length(@Source) - string-length('TaskService.exe') + 1) = 'TaskService.exe'
           ]]"
           use="@Id" />

  <!-- Remove the Component elements that contain TaskService.exe (but not TaskService.exe.config) -->
  <xsl:template match="wix:Component[wix:File[
    substring(@Source, string-length(@Source) - string-length('TaskService.exe') + 1) = 'TaskService.exe'
  ]]" />

  <!-- Remove the ComponentRef elements that reference the excluded components -->
  <xsl:template match="wix:ComponentRef[key('ExcludedComponents', @Id)]" />

</xsl:stylesheet>
