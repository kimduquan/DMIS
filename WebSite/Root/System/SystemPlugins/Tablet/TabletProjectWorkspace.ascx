<%@ Control Language="C#" AutoEventWireup="false" Inherits="System.Web.UI.UserControl" %>
<%@ Import Namespace="Portal.Virtualization" %> 
<%@ Import Namespace="ContentRepository" %> 
<%@ Import Namespace="ContentRepository.Workspaces" %> 
<%@ Import Namespace="Portal.Helpers" %>
<%@ Import Namespace="Search" %> 
<% 
    var cc = PortalContext.Current.ContextNode;
    var manager = cc.GetReference<User>("Manager");
    
%>

<article class="snm-tile bg-zero" id="reload">
    <a href="javascript:location.reload(true)" class="snm-link-tile bg-zero clr-text">
        <span class="snm-lowertext snm-fontsize3">Refresh</span>
    </a>
</article>
<article class="snm-tile" id="backtile">
    <a href="javascript:window.history.back()" class="snm-link-tile bg-semitransparent clr-text">
        <span class="snm-lowertext snm-fontsize3">Back</span>
    </a>
</article>
<div id="snm-container">
    <div id="page1" class="snm-page">
        <div class="snm-pagecontent">
       	    <div class="snm-col">
    		    <h1><%= HttpUtility.HtmlEncode(cc.DisplayName) %></h1>

                <article class="snm-tile snm-clip bg-primary clr-text">
                    <span class="snm-progress"><span class="snm-progress-bar" style="width:<%= (int)Math.Floor(Convert.ToSingle(cc["Completion"])) %>%"></span></span>
                    <span class="snm-bigtext"><%= (int)Math.Floor(Convert.ToSingle(cc["Completion"])) %>%</span>
                </article>

                <% if (cc["Deadline"] != null) { %>
                <article class="snm-tile snm-calendar bg-primary clr-text">
                    <span class="snm-month"><%= ((DateTime)cc["Deadline"]).ToString("MMM")%></span> <span class="snm-day"><%= ((DateTime)cc["Deadline"]).ToString("%d") %></span>
                </article>
                <% } %>

                <% if (manager != null) { %>
                <article class="snm-tile snm-flip">
                    <section class="snm-front">
                        <span class="snm-background"><img src="<%= Portal.UI.UITools.GetAvatarUrl(manager, 80, 80) %>" alt="" title="<%= manager["FullName"] %>" /></span>
                    </section>
                    <section class="snm-back bg-primary">
                        <span class="snm-lowertext snm-fontsize3"><a href="mailto:<%= manager["Email"] %>"><%= manager["FullName"] %></a></span>
                    </section>
                </article>
                <% } %>

                <article class="snm-tile snm-tile-wide2 snm-flip snm-clock">
                    <section class="snm-front snm-state-highlight">
                        <span class="snm-middletext snm-fontsize2"><%=GetGlobalResourceObject("Tablet", "FlipFront")%></span>
                    </section>
                    <section class="snm-back bg-primary">
                        <span class="snm-middletext snm-fontsize2"><%=GetGlobalResourceObject("Tablet", "FlipBack")%></span>
                    </section>
                </article>


            </div>
        </div>
    </div>
</div>
