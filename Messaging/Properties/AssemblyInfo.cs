﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Tests")]
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if DEBUG
[assembly: AssemblyTitle("Messaging (Debug)")]
#elif DIAGNOSTIC
[assembly: AssemblyTitle("Messaging (Diagnostic)")]
#else
[assembly: AssemblyTitle("Messaging (Release)")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(" Inc.")]
[assembly: AssemblyCopyright("Copyright © Inc.")]
[assembly: AssemblyProduct("DMIS")]
[assembly: AssemblyTrademark(" Inc.")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("fcc6395c-7fd0-400e-a01c-274e2a54bdcb")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]