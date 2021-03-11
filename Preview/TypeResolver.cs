using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Preview
{
    internal class TypeResolver
    {
        private static Dictionary<Type, Type[]> _typecacheByBase = new Dictionary<Type, Type[]>();
        private static object _typeCacheSync = new object();
        private static bool _loaded = false;

        public static Assembly[] GetAssemblies()
        {
            if (!_loaded)
                LoadAssemblies();
            return AppDomain.CurrentDomain.GetAssemblies();
        }
        private static string[] LoadAssemblies()
        {
            string[] result = null;
            if (!_loaded)
            {
                lock (_typeCacheSync)
                {
                    if (!_loaded)
                    {
                        result = LoadAssembliesFrom(AppDomain.CurrentDomain.BaseDirectory);
                        _loaded = true;
                    }
                }
            }
            return result ?? new string[0];
        }
        private static string[] LoadAssembliesFrom(string path)
		{
			if (path == null)
				throw new ArgumentNullException("path");
			if (path.Length == 0)
				throw new ArgumentException("Path cannot be empty.", "path");

			List<string> assemblyNames = new List<string>();
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
				assemblyNames.Add(new AssemblyName(asm.FullName).Name);

			List<string> loaded = new List<string>();
			string[] dllPaths = Directory.GetFiles(path, "*.dll");
            var badImageFormatMessages = new List<string>();
			foreach (string dllPath in dllPaths)
			{
                try
                {
                    string asmName = AssemblyName.GetAssemblyName(dllPath).Name;
                    if (!assemblyNames.Contains(asmName))
                    {
                        Assembly.LoadFrom(dllPath);
                        assemblyNames.Add(asmName);
                        loaded.Add(Path.GetFileName(dllPath));
                    }
                }
                catch (BadImageFormatException e) //logged
                {
                    badImageFormatMessages.Add(e.Message);
                }
			}
            if (badImageFormatMessages.Count > 0)
                //Logger.WriteInformation(Logger.EventId.NotDefined, String.Format("Skipped assemblies from {0} on start: {1}{2}", path, Environment.NewLine, String.Join(Environment.NewLine, badImageFormatMessages)));
                throw new ApplicationException(String.Format("Skipped assemblies from {0} on start: {1}{2}", path, Environment.NewLine, String.Join(Environment.NewLine, badImageFormatMessages)));

			return loaded.ToArray();
		}

        public static Type[] GetTypesByInterface(Type interfaceType)
        {
            Type[] temp;
            if (!_typecacheByBase.TryGetValue(interfaceType, out temp))
            {
                lock (_typeCacheSync)
                {
                    if (!_typecacheByBase.TryGetValue(interfaceType, out temp))
                    {
                        var list = new List<Type>();
                        foreach (Assembly asm in GetAssemblies())
                        {
                            try
                            {
                                var types = asm.GetTypes();
                                foreach (Type type in types)
                                    foreach (var interf in type.GetInterfaces())
                                        if (interf == interfaceType)
                                            list.Add(type);
                            }
                            catch (Exception e)
                            {
                                if (!IgnorableException(e))
                                    throw;
                            }
                        }
                        temp = list.ToArray();

                        if (!_typecacheByBase.ContainsKey(interfaceType))
                            _typecacheByBase.Add(interfaceType, temp);
                    }
                }
            }
            var result = new Type[temp.Length];
            temp.CopyTo(result, 0);
            return result;
        }

        private static bool IgnorableException(Exception e)
        {
            if (!Debugger.IsAttached)
                return false;
            var rte = e as ReflectionTypeLoadException;
            if (rte != null)
            {
                if (rte.LoaderExceptions.Length == 2)
                {
                    var te0 = rte.LoaderExceptions[0] as TypeLoadException;
                    var te1 = rte.LoaderExceptions[1] as TypeLoadException;
                    if (te0 != null && te1 != null)
                    {
                        if (te0.TypeName == "System.Web.Mvc.CompareAttribute" && te1.TypeName == "System.Web.Mvc.RemoteAttribute")
                            return true;
                    }
                }
            }
            return false;
        }
        private static Exception TypeDiscoveryError(Exception innerEx, string typeName, Assembly asm)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var duplicates = assemblies.GroupBy(f => f.ToString()).Where(g => g.Count() > 1).ToArray();

            //--
            var msg = new StringBuilder();
            msg.Append("Type discovery error. Assembly: ").Append(asm);
            if (typeName != null)
                msg.Append(", type: ").Append(typeName).Append(".");
            if (duplicates.Count() > 0)
            {
                msg.AppendLine().AppendLine("DUPLICATED ASSEMBLIES:");
                var count = 0;
                foreach (var item in duplicates)
                    msg.Append("    #").Append(count++).Append(": ").AppendLine(item.Key);
            }
            return new ApplicationException(msg.ToString(), innerEx);
        }

    }
}
