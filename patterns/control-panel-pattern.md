
# Control Panel Pattern

## Overview
The control panel is an addon called Admin Site, insalled with the base collection, aoBase51.xml. It is executed on the endpoint defined in the config.json file. 

- Access to the control site is limited to administrators and developers. 
- It provides access to edit all content. 
- It supports a UI pattern that is easy to create an run tools and reports.
- At the top of the UI is a navigation based on type of content and tool.
- On the left of th UI is a navigation based on installed portals

## AdminUI

The adminUI is a pattern used to create all tools and views and is created from methods available in the common progamming object used for object injection, the CP object.

## Top Navigation

The top nav is made of the following sections
- Help
- Warnings
- Content
- System
- Design
- Comm
- Reports
- Tools 
- Settings
- Users

Features selected from the nav can be content or addons. They are installed from collection xml files they are added to nav
- A content definition cdef node has an attribute NavTypeId set to the nav section
- The attribute addoncategoryid is used to group features within a drop-down. 
- Addons are added to the nav with the collection file addon node's type attribute.
- addons categories in the nav are created with the cdef node Category