using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Tests")]

#if DEBUG
[assembly: AssemblyTitle("Portal (Debug)")]
#elif DIAGNOSTIC
[assembly: AssemblyTitle("Portal (Diagnostic)")]
#else
[assembly: AssemblyTitle("Portal (Release)")]
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
[assembly: Guid("ae1a54ac-6441-4eac-b4be-8148541b6042")]