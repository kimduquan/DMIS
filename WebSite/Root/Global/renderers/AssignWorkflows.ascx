<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="ApplicationModel" %>
<%@ Import Namespace="Portal.Portlets" %>
<%@ Import Namespace="Portal.Helpers" %>
<%@ Import Namespace="Portal.Virtualization" %>
<%@ Import Namespace="ContentRepository" %>
<%@ Import Namespace="ContentRepository.Fields" %>
<%@ Import Namespace="ContentRepository.Storage.Schema" %>

<%
    var contentList = PortalContext.Current.ContextNode as ContentList;
    var backUrl = PortalContext.Current.BackUrl;
%>

<div class="sn-workflow-list">

    <%foreach (var content in this.Model.Items)
      { %>
        <%
          var typeName = content.Name.Replace(".xaml", "").Replace(".XAML", "");
          var addAction = contentList == null ? null : ActionFramework.GetAction("AssignWorkflow", ContentRepository.Content.Create(contentList), backUrl, new { ContentTypeName = typeName });
          var wfDefType = ContentRepository.Schema.ContentType.GetByName(typeName);
        %>

        <div class="sn-content sn-workflow ui-helper-clearfix">
            <div>
                <a class="sn-actionlinkbutton" href="<%=addAction == null ? string.Empty : addAction.Uri %>">
                    <img class="sn-icon sn-icon16" title="" alt="[add]" src="/Root/Global/images/icons/16/add.png" /> <%=GetGlobalResourceObject("Workflow", "AssignToList")%>
                </a>
                <h2 class="sn-content-title">
                    <%= Portal.UI.IconHelper.RenderIconTag(wfDefType.Icon, null, 32)%>
                    <%=HttpUtility.HtmlEncode(content.DisplayName) %>                    
                </h2>
                <div class="sn-content-lead"><%= content["Description"] %></div>

            </div>
        </div>
        
    <%} %>
</div>
