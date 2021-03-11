<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Portal.Helpers" %>
<%@ Import Namespace="Portal.UI" %>
<%@ Import Namespace="Portal.Virtualization" %>
   
<ul class="sn-menu">
<% var index = 1;
  foreach (var content in this.Model.Items.Where(item => !(bool)item["Hidden"]))
  { %>
          <li class='<%="sn-menu-" + index++ %>'>
            <% var displayName = UITools.GetSafeText(content.DisplayName);
               
               if (PortalContext.Current.IsResourceEditorAllowed)
               { %>
            <% = displayName %>
            <% } else { %>
            <a href="<%= Actions.BrowseUrl(content) %>"><%= displayName %></a>
            <% } %>
          </li>
<%} %>
</ul>