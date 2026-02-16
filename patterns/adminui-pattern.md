
# AdminUI Pattern

The adminUI pattern is used to create tools and reports that display in the contensive control panel with a consistent look and feel.

## Overview

Admin UI is implemented with LayoutBuilder classes that create the html for common Admin UI cases.

LayoutBuilder classes are created using one of the following factory methods. The primary adminUI workflow is
- the client program creates a layoutbuilder instance by calling one of the cp.AdminUI methods
- the client populates its properties
- calls the getHtml method and returns its response. it is imporatant that the client program return the unmodified complete response from the the getHtml method.

The resulting string includes the entire view, including possible pagination, filters, refresh etc. If those features require ajax data, the layout build handles this by
- calling a layoutbuilder endpoint
- the endpoint executes the client program (which initially called the cpAdminUI interface)
- the LayoutBuilder then returns in the getHtml() methods the expected response for the ajac call, which the client program should return without modification.

These are the layout builder options

### CreateLayoutBuilder

creates an instance of LayoutBuilderBaseClass for basic forms

Examples of this layoutbuilder
- \source\Processor\Addons\Tools\ContentSchemaToolClass.cs
- \source\Processor\Addons\Tools\ConfigureEditClass.cs
- \source\Processor\Addons\Tools\ContentChildToolClass.cs
- \source\Processor\Addons\Tools\CreateGUIDToolClass.cs

### CreateLayoutBuilderList

creates an instance of LayoutBuilderListBaseClass object for tabular lists of data rows with filters

Examples of this layoutbuilder
- \source\Processor\Addons\LayoutBuilder\SampleLayoutBuilderList.cs
- \examples\LayoutBuilderListExample\README.md

### CreateLayoutBuilderNameValue

creates an instance of LayoutBuilderNameValueBaseClass for forms with lists of input boxes

Examples of this layoutbuilder
- \examples\LayoutBuilderNameValueExample\README.md

### CreateLayoutBuilderTabbedBody

creates an instance of LayoutBuilderTabbedBodyBaseClass for a layout that includes navigation accross the top and a body of content

### CreateLayoutBuilderToolForm

creates an instance of LayoutBuilderToolFormBaseClass for the structure of a tool that the client populates the body

### CreateLayoutBuilderTwoColumnLeft

creates an instance of LayoutBuilderTwoColumnLeftBaseClass for a simple 2 column layout populated by other layoutbuilders. The left column is wider than the right.

### CreateLayoutBuilderTwoColumnRight

creates an instance of LayoutBuilderTwoColumnRightBaseClass for a simple 2 column layout populated by other layoutbuilders. The right column is wider than the left.

