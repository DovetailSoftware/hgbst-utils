Dovetail Software HGBST Utilities

ABOUT
=======================
The HGBST Utilities program is a collection of various read-only
HGBST (user-defined lists) visualization and troubleshooting 
features. 

REQUIREMENTS
=======================
Software:
	- Operating System:
		Microsoft™ Windows™ 2000 and later

	- Microsoft Data Access Components 2.7 SP1 or later (2.8 recommended) 

	- Microsoft™ .NET Framework 1.1 Runtime 
		Install through Windows Update 
		Or Download from: 
			http://msdn.microsoft.com/netframework/technologyinfo/howtoget/default.aspx 
			
			NOTE: Windows Server 2003 meets this requirement out of
			the box.
	
Clarify(tm) System Environment:
	- Clarify(tm) 7.0 or later 
	- Database:
		Microsoft™ SQL Server™ 2000 SP3 or later
			- OR – 
		Oracle™ Database Server 7.2.3 or later ([1]) 
	- A user with the System Administrator role which DOES NOT have an encrypted/obfuscated password (i.e. the 'sa' user)

INSTALLING
=======================

The distribution of HGBST Utilities you received should have contained
a single ZIP file which contains the hgbstutils.exe executable file 
and this document. To install, simply extract the ZIP file to a folder on
the hard drive.

GETTING STARTED
=======================

Open a command prompt (cmd.exe), navigate (using the 'cd' command) to 
the directory in which the HGBST Utilities were extracted.

To view the usage of the application, execute the command:

hgbstutils /?

This will give a usage summary of the application.

There are 4 operations that the HGBST Utilities can perform:
- Output a tree-view display of all HGBST LISTS or a specific LIST or set of LISTS to StdOut (the console)
- Output the LIST tree for a given Element, using the element's objid
- Output the LIST tree for a given Show, using the show's objid
- Output the LIST tree for a given LIST, using the LIST's objid

REVIEWING RESULTS
=======================

Each operation will output a tree-view display to the console. This shows how the list/show/element hierarchy
looks for the particular LIST or set of LISTS specified.

If there is corruption in the LIST structure, this will be noted in the output with a special flag (i.e. "*** ERROR ***").

If a particular list contains a loop condition, processing will stop after the 2nd time a particular SHOW is seen
in the same LIST hierarchy.


GETTING ASSISTANCE FROM DOVETAIL 
================================

If this program produces an error or returns unexpected results, please save the results (if possible) to a file (redirect StdOut to a file, see below)
and email them to Dovetail Software Support (support@dovetailsoftware.com) with any error messages, notes, extra info, and/or description of errant behavior.

Example:

hgbstutils /t "FAMILY" > familylist.txt

UNINSTALLING
=======================

Delete the hgbstutils.exe file and this document to uninstall this application.
Also, you may wish delete the original distribution ZIP file.

LEGAL
=======================
- HGBST Utilities (c) 2005 Dovetail Software

- Clarify, its product names, and tools are trademarks of Amdocs. 
  Amdocs trademarks may include the following: 
	ClearConfigurator 
	ClearConfigurator Workshop 
	eSupport 
	eOrder 
	eResponse Manager 
	e.link 
	Flexible Deployment 
	SalesBasic 
	Notifier 
	Classification Engine 
	Routing Server 
	ClearEnterprise Traveler 
	Diagnosis Engine 
	
- Microsoft, Windows, .NET and other Microsoft products are trademarks
  of Microsoft.