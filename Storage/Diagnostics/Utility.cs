using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ContentRepository.Storage.Security;
using System.Threading;

namespace Diagnostics
{
    public static class Utility
    {
        private const string LoggedUserNameKey = "UserName";
        private const string LoggedUserNameKey2 = "LoggedUserName";
        private const string SpecialUserNameKey = "SpecialUserName";

        private static string thisNameSpace = typeof(Utility).Namespace;

        internal static MethodInfo GetOriginalCaller(object handlerInstance)
        {
            //skip the last few method calls
            var stackTrace = new System.Diagnostics.StackTrace(3);
            MethodBase result = null;

            var i=0;
            while (true)
            {
                var sf = stackTrace.GetFrame(i++);
                if (sf == null)
                    break;

                result = sf.GetMethod();
                if (result == null)
                    break;

                //skip everything in Diagnostics namespace
                if (result.DeclaringType.Namespace != thisNameSpace)
                    break;
            }
            return result as MethodInfo;
        }

        internal static IDictionary<string, object> GetDefaultProperties(object target)
        {
            var n = target as ContentRepository.Storage.Node;
            if (n != null)
                return new Dictionary<string, object> { { "NodeId", n.Id }, { "Path", n.Path } };

            var e = target as Exception;
            if (e != null)
            {
                var props = new Dictionary<string, object>();
                props.Add("Messages", CollectExceptionMessages(e));
                var epath = string.Empty;
                while (e != null)
                {
                    epath += e.GetType().Name + "/";
                    var data = e.Data;
                    foreach (var key in data.Keys)
                        props.Add(epath + key.ToString(), data[key]);
                    e = e.InnerException;
                }
                return props;
            }

            var t = target as Type;
            if (t != null)
                return new Dictionary<string, object> { { "Type", t.FullName } };

            t = target.GetType();
            if (t.FullName == "Search.Indexing.Activities.DistributedLuceneActivity+LuceneActivityDistributor")
            {
                return new Dictionary<string, object> { { "LuceneActivity", target.ToString() } };
            }

            return new Dictionary<string, object> { { "Type", target.GetType().FullName }, { "Value", target.ToString() } };
        }

        internal static IDictionary<string, object> CollectAutoProperties(IDictionary<string, object> properties)
        {
            var props = properties;
            if (props == null)
                props = new Dictionary<string, object>();
            if (props.IsReadOnly)
                props = new Dictionary<string, object>(props);

            CollectUserProperties(props);
            CollectContextProperties(props);

            var nullNames = new List<string>();
            foreach (var key in props.Keys)
                if (props[key] == null)
                    nullNames.Add(key);
            foreach (var key in nullNames)
                props[key] = String.Empty;

            return props;
        }
        private static void CollectUserProperties(IDictionary<string, object> properties)
        {
            //if (!AccessProvider.IsInitialized)
            //    return;

            //IUser loggedUser = AccessProvider.Current.GetCurrentUser();
            IUser loggedUser = GetCurrentUser();

            if (loggedUser == null)
                return;

            IUser specialUser = null;

            if (loggedUser is StartupUser)
            {
                specialUser = loggedUser;
                loggedUser = null;
            }
            else
            {
                var systemUser = loggedUser as SystemUser;
                if (systemUser != null)
                {
                    specialUser = systemUser;
                    loggedUser = systemUser.OriginalUser;
                }
            }

            if (loggedUser != null)
            {
                if (properties.ContainsKey(LoggedUserNameKey))
                {
                    if (properties.ContainsKey(LoggedUserNameKey2))
                        properties[LoggedUserNameKey2] = loggedUser.Username ?? String.Empty;
                    else
                        properties.Add(LoggedUserNameKey2, loggedUser.Username ?? String.Empty);
                }
                else
                {
                    properties.Add(LoggedUserNameKey, loggedUser.Username ?? String.Empty);
                }
            }
            if (specialUser != null)
            {
                if (properties.ContainsKey(SpecialUserNameKey))
                    properties[SpecialUserNameKey] = specialUser.Username ?? String.Empty;
                else
                    properties.Add(SpecialUserNameKey, specialUser.Username ?? String.Empty);
            }
        }
        private static void CollectContextProperties(IDictionary<string, object> properties)
        {
            if (!properties.ContainsKey("WorkingMode"))
                properties.Add("WorkingMode", ContentRepository.Storage.Data.RepositoryConfiguration.WorkingMode.RawValue);

            if (!properties.ContainsKey("IsHttpContext"))
            {
                var ctx = System.Web.HttpContext.Current;
                properties.Add("IsHttpContext", ctx == null ? "no" : "yes");
                if (ctx != null)
                {
                    System.Web.HttpRequest req = null;
                    try
                    {
                        req = ctx.Request;
                    }
                    catch { }// does nothing
                    if (req != null)
                    {
                        if (!properties.ContainsKey("Url"))
                            properties.Add("Url", ctx.Request.Url);
                        if (!properties.ContainsKey("Referrer"))
                            properties.Add("Referrer", ctx.Request.UrlReferrer);
                    }
                    else
                    {
                        if (!properties.ContainsKey("Url"))
                            properties.Add("Url", "// not available //");
                    }
                }
            }
        }

        private static IUser GetCurrentUser()
        {
            if ((System.Web.HttpContext.Current != null) && (System.Web.HttpContext.Current.User != null))
                return System.Web.HttpContext.Current.User.Identity as IUser;
            return Thread.CurrentPrincipal.Identity as IUser;
        }

        public static string CollectExceptionMessages(Exception ex)
        {
            var sb = new StringBuilder();
            //var e = ex;
            //while (e != null)
            //{
            //    sb.AppendLine(e.Message).AppendLine(e.StackTrace).AppendLine("-----------------");
            //    e = e.InnerException;
            //}

            sb.Append(ex.GetType().Name).Append(": ").AppendLine(ex.Message);
            PrintTypeLoadError(ex as ReflectionTypeLoadException, sb);
            sb.AppendLine(ex.StackTrace);
            while ((ex = ex.InnerException) != null)
            {
                sb.AppendLine("---- Inner Exception:");
                sb.Append(ex.GetType().Name);
                sb.Append(": ");
                sb.AppendLine(ex.Message);
                PrintTypeLoadError(ex as ReflectionTypeLoadException, sb);
                sb.AppendLine(ex.StackTrace);
            }
            sb.AppendLine("=====================");

            return sb.ToString();
        }
        private static void PrintTypeLoadError(ReflectionTypeLoadException exc, StringBuilder sb)
        {
            if (exc == null)
                return;
            sb.AppendLine("LoaderExceptions:");
            foreach (var e in exc.LoaderExceptions)
            {
                sb.Append("-- ");
                sb.Append(e.GetType().FullName);
                sb.Append(": ");
                sb.AppendLine(e.Message);

                var fileNotFoundException = e as System.IO.FileNotFoundException;
                if (fileNotFoundException != null)
                {
                    sb.AppendLine("FUSION LOG:");
                    sb.AppendLine(fileNotFoundException.FusionLog);
                }
            }
        }
    }
}
