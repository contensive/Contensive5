<?xml version="1.0" encoding="utf-8"?>
<!--
  WiX Heat transform: excludes files from auto-harvested components that are
  declared explicitly in Components.wxs.
  - ContensiveMcpServer.exe: declared with ServiceInstall/ServiceControl
  - appsettings.json: declared with NeverOverwrite to preserve user edits

  Both the Component definitions and their ComponentRef entries are removed
  so the linker does not encounter unresolved references.
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

  <!-- Key: look up Component IDs by the source filename they contain -->
  <xsl:key name="ExcludedComponents"
           match="wix:Component[wix:File[contains(@Source, 'ContensiveMcpServer.exe')] or wix:File[contains(@Source, 'appsettings.json')]]"
           use="@Id" />

  <!-- Remove the Component elements that contain the excluded files -->
  <xsl:template match="wix:Component[wix:File[contains(@Source, 'ContensiveMcpServer.exe')]]" />
  <xsl:template match="wix:Component[wix:File[contains(@Source, 'appsettings.json')]]" />

  <!-- Remove the ComponentRef elements that reference the excluded components -->
  <xsl:template match="wix:ComponentRef[key('ExcludedComponents', @Id)]" />

</xsl:stylesheet>
