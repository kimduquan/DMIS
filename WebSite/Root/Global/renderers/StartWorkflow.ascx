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
       <p>There are no workflows that you can start.</p>
    <%
        }
       else
       {
           
           var contextNode = PortalContext.Current.ContextNode;
           var contentList = ContentList.GetContentListByParentWalk(this.Model.Content.ContentHandler) as ContentList;
           var backUrl = PortalContext.Current.BackUrl;

           foreach (var content in this.Model.Items)
           {
               var addAction = ActionFramework.GetAction("StartWorkflow", ContentRepository.Content.Create(contentList), backUrl, new { ContentTypeName = content.Path, RelatedContent = contextNode.Path });
               
               %>

        <div class="sn-content sn-workflow ui-helper-clearfix">
            <a class="sn-actionlinkbutton" href="<%=addAction == null ? string.Empty : addAction.Uri %>">
                <img class="sn-icon sn-icon16" title="" alt="[start]" src="/Root/Global/images/icons/16/startworkflow.png" />
                <%=GetGlobalResourceObject("Renderers", "StartWorkflow")%>
            </a>
            <h2 class="sn-content-title">
                <%= Portal.UI.IconHelper.RenderIconTag(content.Icon, null, 32)%>
                <%=HttpUtility.HtmlEncode(content.DisplayName)%> <br />
            </h2>
            <div class="sn-content-lead"><%=content.Description%></div>
        </div>
        
    <%}
       } %>
</div>
