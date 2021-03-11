<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="ApplicationModel" %>
<%@ Import Namespace="Portal.Portlets" %>
<%@ Import Namespace="Portal.Helpers" %>
<%@ Import Namespace="Portal.Virtualization" %>
<%@ Import Namespace="ContentRepository" %>
<%@ Import Namespace="ContentRepository.Fields" %>


<div class="sn-workflow-list">

    <% if (this.Model.Content == null || !this.Model.Content.Children.Any())
       { %>
       <%=GetGlobalResourceObject("Workflow", "NoWorkflows")%>
    <%
        }
       else
       {
           foreach (var content in this.Model.Items)
           { 
              var typeName = content.Name.Replace(".xaml", "").Replace(".XAML", "");
              var deleteAction = ActionFramework.GetAction("Delete", content, new { RedirectToBackUrl = true });
            %>


        <div class="sn-content sn-workflow ui-helper-clearfix">
                <% if (deleteAction != null) { %>
                <span style="cursor:pointer;" class="sn-actionlinkbutton" onclick="<%=deleteAction.Uri %>">
                    <%= Portal.UI.IconHelper.RenderIconTag("delete", null, 16)%><%=GetGlobalResourceObject("Workflow", "RemoveFromList")%>
                </span>
                <% } %>
                <h2 class="sn-content-title">
                    <%= Portal.UI.IconHelper.RenderIconTag(content.Icon, null, 32)%>
                    <%= Actions.BrowseAction(content) %>    
                </h2>
                <div class="sn-content-lead"><%= content["Description"] %></div>
        </div>
        
    <%}
       } %>
</div>
