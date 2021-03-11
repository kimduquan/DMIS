<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Portal.Helpers" %>
<%
    var siteLang = Portal.Virtualization.PortalContext.Current.Site.Language;
    var ci = System.Globalization.CultureInfo.GetCultureInfo(siteLang);
%>
<div class="sn-contentlist sn-blogarchive">
    <%
        var archivesList = new Dictionary<ContentRepository.Content, int>();
        foreach (var content in this.Model.Items)
        {
            if (!String.IsNullOrEmpty(content.Path) && content.Children.Any())
            {
                var count = Search.ContentQuery.Query(String.Format(@"+TypeIs:BlogPost +InTree:'{0}' +IsPublished:True +PublishedOn:<@@CURRENTTIME@@", content.Path)).Count;
                if (count > 0) {
                    archivesList.Add(content, count);
                }
            }                        
        }
        if (archivesList.Count() == 0)
        {%>
            <span class="sn-blogarchive-missing"><%= ContentRepository.i18n.ResourceManager.Current.GetString("Portal", "SnBlog_ArchivePortlet_NoArchives") %></span>
        <%}
        else
        {%>
            <ul class="sn-blogarchive-list">
                <%
                    foreach (ContentRepository.Content content in archivesList.Keys)
                    {%>
                            <li class="sn-blogarchive-listitem">
                                <a class="sn-blogarchive-listitem-folder" href="<%=Actions.BrowseUrl(content)%>"><%= content["DisplayName"]%></a>
                                <span><%= String.Format(" ({0})", archivesList[content])%></span>
                            </li>
                    <%}%>
            </ul>
        <%} %>
</div>
