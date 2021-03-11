﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ContentRepository;
using System.IO;
using ContentRepository.i18n;
using System.Globalization;
using ContentRepository.Storage.Security;
using Search;
using System.Text.RegularExpressions;
using Portal.Virtualization;

namespace Portal.Resources
{
    /// <summary>
    /// When /Resource.ashx?class=xy is requested, a javascript variable is defined
    /// </summary>
    public class ResourceHandler : IHttpHandler
    {
        private static readonly string REGEX_RESOURCES = "(?<prev>[^/]*?)/" + UrlPart + "/(?<lang>[^/]+?)/(?<class>[^/]+)";

        public static string UrlPart
        {
            get { return "sn-resources"; }
        }

        //================================================================================= Helper methods

        private void DenyRequest(HttpContext context)
        {
            context.Response.Clear();
            context.Response.StatusCode = 404;
            context.Response.Flush();
            context.Response.End();
        }

        public static DateTime GetLastResourceModificationDate(DateTime? modifiedSince)
        {
            // return the cached value
            if (!modifiedSince.HasValue) 
                return ResourceManager.Current.LastResourceModificationDate;

            // do not return an invalid value
            if (modifiedSince.Value > DateTime.Now)
                return DateTime.Now;

            // return the bigger value (the date that occured later)
            return modifiedSince.Value > ResourceManager.Current.LastResourceModificationDate
                ? modifiedSince.Value
                : ResourceManager.Current.LastResourceModificationDate;
        }

        protected internal static Tuple<string, string> ParseUrl(string url)
        {
            var regex = new Regex(REGEX_RESOURCES);
            var match = regex.Match(url);

            return match.Success ? Tuple.Create(match.Groups["lang"].ToString(), match.Groups["class"].ToString()) : null;
        }

        //================================================================================= IHttpHandler

        public void ProcessRequest(HttpContext context)
        {
            // Handling If-Modified-Since

            var modifiedSinceHeader = HttpContext.Current.Request.Headers["If-Modified-Since"];

            if (modifiedSinceHeader != null)
            {
                DateTime ifModifiedSince;

                if (DateTime.TryParse(modifiedSinceHeader, out ifModifiedSince) && ifModifiedSince != DateTime.MinValue)
                {
                    // convert the client local time to UTC
                    var utcIfModifiedSince = ifModifiedSince.ToUniversalTime();
                    var lastModificationDate = GetLastResourceModificationDate(utcIfModifiedSince);

                    if (lastModificationDate <= utcIfModifiedSince)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = 304;
                        context.Response.Flush();
                        context.Response.End();
                    }
                }
            }

            // Handling the rest of the request

            var shouldDeny = true;

            try
            {
                var parsedUrl = ParseUrl(context.Request.RawUrl);

                if (parsedUrl != null)
                {
                    var cultureName = parsedUrl.Item1;
                    var className = parsedUrl.Item2;
                    CultureInfo culture = null;

                    if (!string.IsNullOrEmpty(cultureName))
                        culture = CultureInfo.GetCultureInfo(cultureName);

                    if (culture != null && !string.IsNullOrEmpty(className))
                    {
                        var script = ResourceScripter.RenderResourceScript(className, culture);
                        var lastModificationDate = GetLastResourceModificationDate(null);

                        HttpHeaderTools.SetCacheControlHeaders(lastModified: lastModificationDate);

                        // TODO: add an expires header when appropriate, but without clashing with the resource editor

                        context.Response.ContentType = "text/javascript";
                        context.Response.Write(script);
                        context.Response.Flush();

                        shouldDeny = false;
                    }
                }
            }
            catch
            {
                shouldDeny = true;
            }

            // If it failed for some reason, deny it

            if (shouldDeny)
                DenyRequest(context);

        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}
