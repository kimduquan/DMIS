<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Portal.Portlets" %>
<%@ Import Namespace="Portal.UI.PortletFramework" %>
<%@ Import Namespace="Portal.Helpers" %>
<%@ Import Namespace="Portal.Virtualization" %>
<%@ Import Namespace="ContentRepository" %>
<%@ Import Namespace="ContentRepository.Fields" %>
<%@ Import Namespace="Portal.Workspaces" %>




<div class="sn-workspace-list">

    <% 
        var profile = PortalContext.Current.ContextWorkspace as UserProfile;
        var profileUser = profile.User as User;
        var editable = profileUser.Id == User.Current.Id;
        var user = (this.Parent as ContextBoundPortlet).ContextNode as User;
        var wsGroupLists = WorkspaceHelper.GetWorkspaceGroupLists(user);
        foreach (var wsg in wsGroupLists)
        {
            var content = ContentRepository.Content.Create(wsg.Workspace);
            var managerData = WorkspaceHelper.GetManagerData(null);
            var managers = content["Manager"] as List<ContentRepository.Storage.Node>;
            if (managers != null)
            {
                var manager = managers.FirstOrDefault() as ContentRepository.User;
                managerData = WorkspaceHelper.GetManagerData(manager);
            }
            
            %>
            
            <div class="sn-content ui-helper-clearfix" style="margin-bottom: 5px; background-color: #FFF; border: solid 1px #DDD; padding: 5px;">
                <a href='<%= managerData.ManagerUrl %>' title='<% = managerData.ManagerName %>'><img style="float:right;" src="<%= managerData.ManagerImgPath %>" title="<%= managerData.ManagerName %>" /></a>
                <div style="float:right;  margin-right: 20px; text-align: right;">
                    <%=GetGlobalResourceObject("KPIRenderers", "Deadline")%>
                    <big style="font-size: 12px; display: block;"><strong><%= ((DateTime)wsg.Workspace["Deadline"]).ToShortDateString()%></strong></big>
                </div>
                <div style="padding-right:110px">            
                    <div style="float:left;">
                        <%= Portal.UI.IconHelper.RenderIconTag(wsg.Workspace.Icon, null, 32)%>                    
                    </div>
                    <div style="float:left;">
                        <div class="sn-workspace-listitem-title"><a href="<%=Actions.BrowseUrl(content)%>"><%= HttpUtility.HtmlEncode(content.DisplayName)%></a></div>
                        <div class="sn-workspace-listitem-groupinfo">
                    <%
               
               var groupInfos = wsg.Groups.OrderBy(g => g.Group.DisplayName).ToList();
               for (var j = 0; j < groupInfos.Count; j++)
               {
                   var groupInfo = groupInfos[j];
                   var viaGroups = WorkspaceHelper.GetViaGroups(user, groupInfo).ToList();
                   var viaCount = viaGroups.Count;
                   
                   %><%= j > 0 ? "; " : "" %>in <a href="<%= ContentRepository.Content.Create(groupInfo.Group).Path%>"><%= ContentRepository.Content.Create(groupInfo.Group).DisplayName%></a><%= viaCount > 0 ? "" : "; "%><% 
                   
                    // if user is contained ONLY via a group or groups, list these groups. if user is contained explicitely, don't list groups
                    if (viaCount > 0 && !viaGroups.Select(g => g.Id).Contains(user.Id))
                    {
                       %> via <%
                        for (var i = 0; i < viaCount; i++)
                        {
                            var group = viaGroups[i];
                           %><a href="<%= ContentRepository.Content.Create(group).Path%>"><%= ContentRepository.Content.Create(group).DisplayName%></a><%= i == viaCount - 1 ? "; " : ", "%><%
                        }
                    }
               } %>
                         </div>
                      </div>
                      <div style="clear:both;"></div>
                 </div>
            </div>
       <% } %>
</div>
