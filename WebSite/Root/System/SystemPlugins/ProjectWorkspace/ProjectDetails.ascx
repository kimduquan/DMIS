<%@ Language="C#" AutoEventWireup="true" Inherits="Portal.UI.SingleContentView" %>
<%@ Import Namespace="System.Collections.Generic"%>
<%@ Import namespace="ContentRepository.Storage" %>
<%@ Import Namespace="System.Web.UI.WebControls" %>
<% string user = (ContentRepository.User.Current).ToString(); %>


<div class="sn-content">
    <%= this.Content.Fields["Description"].GetData() %>
</div>
<div class="sn-panel ui-widget-content">
    <a class="sn-floatright" href='<%= Portal.PortletFramework.PortalActionLinkResolver.Instance.ResolveRelative(this.Content.Path, "RSS") %>'>
        <sn:SNIcon ID="SNIcon1" Icon="rss" runat="server" /><%=GetGlobalResourceObject("ProjectDetails", "RSSfeed")%>
    </a>
    <%=GetGlobalResourceObject("ProjectDetails", "ProjectIsActive-" + this.Content["IsActive"].ToString())%> <span class="sn-separator">|</span>
    <strong><%=GetGlobalResourceObject("ProjectDetails", "Completition")%></strong> <%= (int)Math.Floor(Convert.ToSingle(this.Content["Completion"])) %>%  
</div>