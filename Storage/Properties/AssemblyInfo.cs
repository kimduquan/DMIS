using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Tests")]
#if DEBUG
[assembly: AssemblyTitle("Storage (Debug)")]
#elif DIAGNOSTIC
[assembly: AssemblyTitle("Storage (Diagnostic)")]
#else
[assembly: AssemblyTitle("Storage (Release)")]
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
