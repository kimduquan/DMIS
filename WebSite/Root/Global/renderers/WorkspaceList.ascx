<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Portal.Portlets" %>
<%@ Import Namespace="Portal.Helpers" %>
<%@ Import Namespace="ContentRepository.Fields" %>

<sn:ContextInfo runat="server" ID="newWS" />

<% string user = (ContentRepository.User.Current).ToString(); %>
<%if (user == "Visitor")
  {%>
   <div class="sn-pt-body-border ui-widget-content ui-corner-all">
	<div class="sn-pt-body ui-corner-all">
		<%=GetGlobalResourceObject("Portal", "WSContentList_Visitor")%>
	</div>
</div>
<% }%>
<%else
  {%>

<div class="sn-workspace-list">
    <sn:Toolbar runat="server">
        <sn:ToolbarItemGroup Align="left" runat="server">
            <sn:ActionMenu ID="ActionMenu1" runat="server" Scenario="New" ContextInfoID="myContext" RequiredPermissions="AddNew" CheckActionCount="True">
                <sn:ActionLinkButton ID="ActionLinkButton1" runat="server" ActionName="Add" IconUrl="/Root/Global/images/icons/16/newfile.png" ContextInfoID="newWS" Text='<%$ Resources: Scenario, New %>' CheckActionCount="True"  ParameterString="backtarget=newcontent"/>
            </sn:ActionMenu>
        </sn:ToolbarItemGroup>
    </sn:Toolbar>


    <%foreach (var content in this.Model.Items)
      { %>
      
            <% 
          var managers = content["Manager"] as List<ContentRepository.Storage.Node>;
          var imgSrc = "/Root/Global/images/orgc-missinguser.png?width=64&height=64";
          var managerName = GetGlobalResourceObject("Workspace", "NoManagerAssociated");
          if (managers != null) {
              var manager = managers.FirstOrDefault() as ContentRepository.User;
              if (manager != null) {
                  var managerC = ContentRepository.Content.Create(manager);
                  managerName = manager.FullName;

                  var imgUrl = Portal.UI.UITools.GetAvatarUrl(manager, 64, 64);
                  if (!string.IsNullOrEmpty(imgUrl))
                      imgSrc = imgUrl;
              }
          }
          %>
      
        <div style="margin-bottom: 10px; background-color: #FFF; border: solid 1px #DDD; padding: 10px;">
            <img style="float:right;" src="<%= imgSrc %>" title="<%= managerName %>" />
            <div style="float:right;  margin-right: 40px; text-align: right;">
                <%=GetGlobalResourceObject("KPIRenderers", "Deadline")%>
                <big style="font-size: 18px; display: block;"><strong><%= ((DateTime)content["Deadline"]).ToShortDateString()%></strong></big>
            </div>
            <div style="padding-right:170px">
                <h2 class="sn-content-title">
                    <%= Portal.UI.IconHelper.RenderIconTag(content.Icon, null, 32) %>
                    <a href="<%=Actions.BrowseUrl(content)%>"><%=HttpUtility.HtmlEncode(content.DisplayName) %></a>
                </h2>
                <div class="sn-content-lead"><%= content["Description"] %></div>
            </div>
        </div>
        
    <%} %>
</div>

<%} %>