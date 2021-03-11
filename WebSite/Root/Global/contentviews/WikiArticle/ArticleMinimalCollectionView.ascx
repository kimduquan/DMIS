<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" EnableViewState="false" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Portal.Helpers" %>
    
<div class="sn-contentlist">
 
<% var index = 0;
    foreach (var content in this.Model.Items)
  { %>
      <% if (index > 0) { %>, <% } %>
      <%=Actions.BrowseAction(HttpUtility.HtmlEncode(content))%>
<% index++;
    } %>

</div>




