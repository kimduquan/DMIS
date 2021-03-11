using System.Reflection;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyTitle("WebSite (Debug)")]
#elif DIAGNOSTIC
[assembly: AssemblyTitle("WebSite (Diagnostic)")]
#else
[assembly: AssemblyTitle("WebSite (Release)")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(" Inc.")]
[assembly: AssemblyCopyright("Copyright © Inc.")]
[assembly: AssemblyProduct("DMIS")]
[assembly: AssemblyTrademark(" Inc.")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: ComVisible(false)]

