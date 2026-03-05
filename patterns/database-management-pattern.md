
# Database Management Pattern

## Overview
The Contensive architecture provides a database api and 

## AdminUI Pattern

The adminUI is a pattern used to create all tools and views and is created from methods available in the common progamming object used for object injection, the CP object.

For details about how to use the AdminUI pattern see the adminui-pattern.md document in the patterns folder of the github contensive account, contensive5 repo

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