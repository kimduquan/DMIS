<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Portal.Helpers" %>
   
<div class="sn-contentlist">
 
<%foreach (var content in this.Model.Items)
  { %>
    <div class="sn-content sn-contentlist-item">
        <h1 class="sn-content-title">
            <%=Actions.BrowseAction(content)%>
            <%if (Security.IsInRole("SmartEditors"))%>
                <%= Actions.ActionMenu(content.Path, "[SN]", "ListItem") %>
        </h1>
    </div>
<%} %>

</div>




