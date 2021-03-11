using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Web.Hosting;
using ContentRepository;
using ContentRepository.Storage.Security;
using Diagnostics;
using System.Threading;

namespace Portal.Virtualization
{
    public static class HttpHeaderTools
    {
        private static readonly string HEADER_CONTENTDISPOSITION_NAME = "Content-Disposition";
        private static readonly string HEADER_CONTENTDISPOSITION_VALUE = "Attachment";
        private static readonly string HEADER_ACESSCONTROL_ALLOWORIGIN_NAME = "Access-Control-Allow-Origin";
        private static readonly string HEADER_ACESSCONTROL_ALLOWCREDENTIALS_NAME = "Access-Control-Allow-Credentials";
        private static readonly string HEADER_ACESSCONTROL_ALLOWCREDENTIALS_ALL = "*";
        private static readonly string HEADER_ACESSCONTROL_ORIGIN_NAME = "Origin";

        private delegate void PurgeDelegate(IEnumerable<string> urls);


        // ============================================================================================ Private methods
        private static bool IsClientCached(DateTime contentModified)
        {
            var modifiedSinceHeader = HttpContext.Current.Request.Headers["If-Modified-Since"];
            if (modifiedSinceHeader != null)
            {
                DateTime isModifiedSince;
                if (DateTime.TryParse(modifiedSinceHeader, out isModifiedSince))
                    return isModifiedSince - contentModified > TimeSpan.FromSeconds(-1);    // contentModified is more precise
            }
            return false;
        }
        private static string[] PurgeUrlFromProxy(string url, bool async)
        {
            // PURGE /contentem/maicontent.jpg HTTP/1.1
            // Host: myhost.hu

            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException("url");

            if (PortalContext.ProxyIPs.Count == 0)
                return null;

            string contentPath;
            string host;

            var slashIndex = url.IndexOf("/");
            if (slashIndex >= 0)
            {
                contentPath = url.Substring(slashIndex);
                host = url.Substring(0, slashIndex);
            }
            else
            {
                contentPath = "/";
                host = url;
            }

            if (string.IsNullOrEmpty(host) && HttpContext.Current != null)
                host = HttpContext.Current.Request.Url.Host;

            string[] result = null;
            if (!async)
                result = new string[PortalContext.ProxyIPs.Count];

            var proxyIndex = 0;
            foreach (var proxyIP in PortalContext.ProxyIPs)
            {
                var proxyUrl = string.Concat("http://", proxyIP, contentPath);

                try
                {
                    var request = WebRequest.Create(proxyUrl) as HttpWebRequest;
                    if (request == null)
                        break;

                    request.Method = "PURGE";
                    request.Host = host;

                    if (!async)
                    {
                        using (request.GetResponse())
                        {
                            //we do not need to read the request here, just the status code
                            result[proxyIndex] = "OK";
                        }
                    }
                    else
                    {
                        request.BeginGetResponse(null, null);
                    }
                }
                catch (WebException wex)
                {
                    var wr = wex.Response as HttpWebResponse;
                    if (wr != null && !async)
                    {
                        switch (wr.StatusCode)
                        {
                            case HttpStatusCode.NotFound:
                                result[proxyIndex] = "MISS";
                                break;
                            case HttpStatusCode.OK:
                                result[proxyIndex] = "OK";
                                break;
                            default:
                                Logger.WriteException(wex);
                                result[proxyIndex] = wex.Message;
                                break;
                        }
                    }
                    else
                    {
                        Logger.WriteException(wex);
                        if (!async)
                            result[proxyIndex] = wex.Message;
                    }
                }

                proxyIndex++;
            }

            return result;
        }
        private static void PurgeUrlsFromProxyAsyncWithDelay(IEnumerable<string> urls) 
        {
            if (PortalContext.PurgeUrlDelayInMilliSeconds.HasValue)
            {
                Thread.Sleep(PortalContext.PurgeUrlDelayInMilliSeconds.Value);
            }
            var distinctUrls = urls.Distinct().Where(url => !string.IsNullOrEmpty(url));
            foreach (var url in distinctUrls)
            {
                PurgeUrlFromProxyAsync(url);
            }
        }


        // ============================================================================================ Public methods
        public static void SetCacheControlHeaders(int cacheForSeconds)
        {
            SetCacheControlHeaders(cacheForSeconds, HttpCacheability.Public);
        }
        public static void SetCacheControlHeaders(int cacheForSeconds, HttpCacheability httpCacheability)
        {
            HttpContext.Current.Response.Cache.SetCacheability(httpCacheability);
            HttpContext.Current.Response.Cache.SetMaxAge(new TimeSpan(0, 0, cacheForSeconds));
            HttpContext.Current.Response.Cache.SetSlidingExpiration(true);  // max-age does not appear in response header without this...
        }
        public static void SetCacheControlHeaders(HttpCacheability? httpCacheability = null, DateTime? lastModified = null, TimeSpan? maxAge = null)
        {
            var cache = HttpContext.Current.Response.Cache;

            try
            {
                if (httpCacheability.HasValue)
                {
                    cache.SetCacheability(httpCacheability.Value);
                }

                if (lastModified.HasValue)
                {
                    var t = lastModified.Value;
                    if (t > DateTime.UtcNow)
                        t = DateTime.UtcNow;
                    cache.SetLastModified(t);
                }

                if (maxAge.HasValue)
                {
                    // max-age does not appear in response header without this
                    cache.SetMaxAge(maxAge.Value);
                    cache.SetSlidingExpiration(true);
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Logger.WriteError(EventId.SetCacheControlHeaders, string.Format("Exception in SetCacheControlHeaders. " +
                    "Parameter values: httpCacheability:'{0}' lastModified:'{1}' maxAge:'{2}'", httpCacheability, lastModified, maxAge));
                Diagnostics.Logger.WriteException(ex);
            }
        }

        public static void SetContentDispositionHeader(string fileName)
        {
            if (HttpContext.Current == null)
                return;

            var cdHeader = HEADER_CONTENTDISPOSITION_VALUE;
            if (!string.IsNullOrEmpty(fileName))
            {
                cdHeader += "; filename=\"" + fileName; 

                // According to MSDN UrlPathEncode should not be used, so we need to replace '+' signs manually to 
                // let browsers interpret the file name correctly. Otherwise 'foo bar.docx' would become 'foo+bar.docx'.
                var encoded = HttpUtility.UrlEncode(fileName).Replace("+", "%20");

                // If the encoded name is different, add the UTF-8 version too. Note that this will be executed
                // even if the only difference is that the space characters were encoded.
                if (string.CompareOrdinal(fileName, encoded) != 0)
                    cdHeader += "\"; filename*=UTF-8''" + encoded;
            }

            // cannot use AppendHeader, because there must be only one header entry with this name
            HttpContext.Current.Response.Headers.Set(HEADER_CONTENTDISPOSITION_NAME, cdHeader);
        }

        /// <summary>
        /// Set Cross-Origin Request Sharing (CORS) headers.
        /// </summary>
        /// <param name="domain">The domain that will be written to the response as allowed origin.</param>
        public static void SetAccessControlHeaders(string domain = null)
        {
            // Set headers only in a real-world environment, not in case of test/mock requests.
            if (HttpContext.Current == null || !HttpRuntime.UsingIntegratedPipeline)
                return;

            // Use the current domain if it was not provided by the caller.
            var allowedDomain = string.IsNullOrEmpty(domain)
                ? HttpContext.Current.Request.Url.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped)
                : domain;

            // Set the allowed origin. This will prevent unauthorized external sites from
            // accessing this resource from the client side (using Javascript ajax request).
            HttpContext.Current.Response.Headers.Set(HEADER_ACESSCONTROL_ALLOWORIGIN_NAME, allowedDomain);

            // Set Credentials header only if the domain is a real one, not a wildcard ('*').
            // FUTURE: set this header based on a more granular setting (by domain?)
            if (allowedDomain != HEADER_ACESSCONTROL_ALLOWCREDENTIALS_ALL)
                HttpContext.Current.Response.Headers.Set(HEADER_ACESSCONTROL_ALLOWCREDENTIALS_NAME, "true");
        }

        /// <summary>
        /// Check if the origin header sent by the client is a known domain. It has to be the 
        /// same that the request was sent to, OR it has to be among the whitelisted external
        /// domains that are allowed to access the Content Repository.
        /// </summary>
        public static void AssertOriginHeader()
        {
            if (HttpContext.Current == null)
                return;
            
            // Get the Origin header from the request, if it was sent by the browser.
            // Command-line tools or local html files will not send this.
            var originHeader = HttpContext.Current.Request.Headers[HEADER_ACESSCONTROL_ORIGIN_NAME];
            if (string.IsNullOrEmpty(originHeader) || string.Compare(originHeader, "null", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                SetAccessControlHeaders();
                return;
            }

            // We compare only the domain parts of the two urls, because interim servers
            // may change the scheme and port of the url (e.g. from https to http).
            var currentDomain = HttpContext.Current.Request.Url.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped);
            var originDomain = string.Empty;
            var error = false;

            try
            {
                var origin = new Uri(originHeader.Trim(' '));
                originDomain = origin.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped);
            }
            catch (Exception)
            {
                Logger.WriteWarning(EventId.Security.HttpHeaderError, "Unknown or incorrectly formatted origin header: " + originHeader);
                error = true;
            }

            if (!error)
            {
                // check if the request arrived from an external domain
                if (string.Compare(currentDomain, originDomain, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    // We allow requests from external domains only if they are registered in this whitelist.
                    var corsDomains = Settings.GetValue<IEnumerable<string>>(PortalSettings.SETTINGSNAME, PortalSettings.SETTINGS_ALLOWEDORIGINDOMAINS, 
                        PortalContext.Current.ContextNodePath, new string[0]);

                    // try to find the domain in the whitelist (or the '*')
                    var externalDomain = corsDomains.FirstOrDefault( d => d == HEADER_ACESSCONTROL_ALLOWCREDENTIALS_ALL ||
                        string.Compare(d, originDomain, StringComparison.InvariantCultureIgnoreCase) == 0);

                    if (!string.IsNullOrEmpty(externalDomain))
                    {
                        // Set the desired domain as allowed (or '*' if it is among the whitelisted domains). We cannot use 
                        // the value from the whitelist (e.g. 'example.com'), because the browser expects the full origin 
                        // (with schema and port, e.g. 'http://example.com:80').
                        SetAccessControlHeaders(externalDomain == HEADER_ACESSCONTROL_ALLOWCREDENTIALS_ALL ? HEADER_ACESSCONTROL_ALLOWCREDENTIALS_ALL : originHeader);
                        return;
                    }

                    // not found in the whitelist
                    error = true;
                }
            }

            SetAccessControlHeaders();

            if (error)
                throw new SecurityException(string.Format("Unknown origin: {0}. Compared origin domain: {1}. Current domain: {2}", originHeader, originDomain, currentDomain));
        }

        public static void EndResponseForClientCache(DateTime lastModificationDate)
        {
            //
            //  http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html
            //  14.25 If-Modified-Since
            //  14.29 Last-Modified
            //

            var context = HttpContext.Current;
            if (IsClientCached(lastModificationDate))
            {
                context.Response.StatusCode = 304;
                context.Response.SuppressContent = true;
                context.Response.Flush();
                context.Response.End();
                // thread exits here
            }
            else
            {
                // make sure that the date is in the past
                var localDate = DateTime.Compare(lastModificationDate, DateTime.UtcNow) <= 0 ? lastModificationDate : DateTime.UtcNow;

                context.Response.Cache.SetLastModified(localDate);
            }
        }

        /// <summary>
        /// Sends a PURGE request to all of the configured proxy servers for the given urls. Purge requests are synchronous.
        /// </summary>
        /// <param name="urls">Urls of the content that needs to be purged. The urls must start with the host name (e.g. www.example.com/mycontent/myimage.jpg).</param>
        /// <returns>A Dictionary with the given urls and the purge result Dictionaries.</returns>
        public static Dictionary<string, string[]> PurgeUrlsFromProxy(IEnumerable<string> urls)
        {
            return urls.Distinct().Where(url => !string.IsNullOrEmpty(url)).ToDictionary(url => url, PurgeUrlFromProxy);
        }

        /// <summary>
        /// Sends a PURGE request to all of the configured proxy servers for the given url. Purge request is synchronous and result is processed.
        /// </summary>
        /// <param name="url">Url of the content that needs to be purged. It must start with the host name (e.g. www.example.com/mycontent/myimage.jpg).</param>
        /// <returns>A Dictionary with the result of each proxy request. Possible values: OK, MISS, {error message}.</returns>
        public static string[] PurgeUrlFromProxy(string url)
        {
            return PurgeUrlFromProxy(url, false);
        }

        /// <summary>
        /// Sends a PURGE request to all of the configured proxy servers for the given url. Purge request is asynchronous and result is not processed.
        /// </summary>
        /// <param name="url">Url of the content that needs to be purged. It must start with the host name (e.g. www.example.com/mycontent/myimage.jpg).</param>
        public static void PurgeUrlFromProxyAsync(string url)
        {
            PurgeUrlFromProxy(url, true);
        }

        /// <summary>
        /// Starts an async thread that will start purging urls after a specified delay. Delay is configured with PurgeUrlDelayInSeconds key in web.config.
        /// </summary>
        /// <param name="urls"></param>
        public static void BeginPurgeUrlsFromProxyWithDelay(IEnumerable<string> urls)
        {
            var purgeDelegate = new PurgeDelegate(PurgeUrlsFromProxyAsyncWithDelay);
            purgeDelegate.BeginInvoke(urls, null, null);
        }
    }
}
