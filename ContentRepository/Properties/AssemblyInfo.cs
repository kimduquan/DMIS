using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Tests")]
#if DEBUG
[assembly: AssemblyTitle("ContentRepository (Debug)")]
#elif DIAGNOSTIC
[assembly: AssemblyTitle("ContentRepository (Diagnostic)")]
#else
[assembly: AssemblyTitle("ContentRepository (Release)")]
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
