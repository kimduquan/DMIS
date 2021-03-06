<%@ Control Language="C#"%>
<%@ Import Namespace="Portal.Virtualization" %>
<%@ Import Namespace="ApplicationModel" %>
<%@ Import Namespace="System.Linq" %>

<sn:CssRequest ID="css1" runat="server" Path="$skin/styles/icons.css" />
<sn:CssRequest ID="css2" runat="server" Path="$skin/styles/jqueryui/jquery-ui.css" />
<sn:CssRequest ID="css4" runat="server" Path="$skin/scripts/jquery/plugins/tree/themes/default/style.css" />
<sn:CssRequest ID="css5" runat="server" Path="$skin/scripts/jquery/plugins/grid/themes/ui.jqgrid.css" />
<sn:CssRequest ID="css6" runat="server" Path="$skin/styles/widgets/jqueryui/jquery-ui.css" />
<sn:CssRequest ID="css3" runat="server" Path="$skin/styles/widgets.css" />
<sn:CssRequest ID="css8" runat="server" Path="$skin/styles/jqueryui/jquery-ui-sntheme.css" />

<sn:ScriptRequest ID="js1" runat="server" Path="$skin/scripts/sn/SN.PortalRemoteControl.Application.js" />


<script runat="server">
    void ResourceEditorMode_Click(object sender, EventArgs args)
    {
        if (Request.Cookies["AllowResourceEditorCookie"] != null)
        {
            Response.Cookies.Set(new HttpCookie("AllowResourceEditorCookie") { Expires = DateTime.UtcNow.AddDays(-1d) });
            Response.Redirect(Request.RawUrl);
        }
        else
        {
            Response.Cookies.Set(new HttpCookie("AllowResourceEditorCookie", "Allow"));
        }
    }
</script>

<div class="sn-portalremotecontrol">
        <span style="cursor: pointer;" runat="server" id="PRCIcon" title="<%$ Resources:PortalRemoteControl,OpenPRC %>" class="sn-prc-dock"><%=GetGlobalResourceObject("PortalRemoteControl", "OpenPRC")%></span>
        <span style="cursor: pointer;" runat="server" id="prctoolbarmenu" visible="false" class="sn-actionlinkbutton icon sn-prc-toolbar sn-prc-toolbar-open" title="<%$ Resources:PortalRemoteControl,OpenPRC %>"><%=GetGlobalResourceObject("PortalRemoteControl", "PageActions")%></span>

	    <snpe:SNUpdatePanel ID="_updatePageDetails" runat="server" >
	        <ContentTemplate>
	        </ContentTemplate>
		</snpe:SNUpdatePanel>
		
        <div class="sn-prc" id="PortalRemoteControl" title="Portal Remote Control">

		    <div class="sn-prc-body sn-admin-content">
	            

	            <sn:ContextInfo runat="server" ID="ContextInfoPage" Selector="CurrentPage" />
			    <sn:ContextInfo runat="server" ID="ContextInfoContent" Selector="CurrentContent" />
			    <sn:ContextInfo runat="server" ID="ContextInfoAppOrContent" Selector="CurrentApplicationContext" ReplaceNullWithContext="true" />
			    <sn:ContextInfo runat="server" ID="ContextInfoAppOnly" Selector="CurrentApplicationContext" />
			    <sn:ContextInfo runat="server" ID="ContextInfoUrlContent" Selector="CurrentUrlContent" />
			            
		        <asp:Panel id="panelPrcTop" runat="server">
			        <dl class="sn-prc-properties">
			            <dt><%=GetGlobalResourceObject("PortalRemoteControl", "NameLabel")%></dt><dd><asp:Label ID="ContentNameLabel" runat="server" /></dd>
			            <dt><%=GetGlobalResourceObject("PortalRemoteControl", "TypeLabel")%></dt><dd><asp:Label ID="ContentTypeLabel" runat="server" /></dd><!--<asp:Image runat="server" ID="ContentTypeImage" />)-->
			            
			            <dt><%=GetGlobalResourceObject("PortalRemoteControl", "VersionLabel")%></dt>
                        <dd><asp:Label ID="VersionLabel" runat="server" ></asp:Label></dd>
			            <dt><%=GetGlobalResourceObject("PortalRemoteControl", "StatusLabel")%></dt>
			            <dd>
			                <asp:Label ID="CheckedOutByLabel" runat="server" /><br />
			                <asp:HyperLink runat="server" ID="SendMessageLink" Visible="false" CssClass="sn-prc-sendmsg" ToolTip='<%=GetGlobalResourceObject("PortalRemoteControl", "SendMessage")%>'><%=GetGlobalResourceObject("PortalRemoteControl", "SendMessage")%></asp:HyperLink><asp:HyperLink runat="server" ID="CheckedOutLink" Target="_blank" Visible="false" /> 
                            
			            </dd>
			            <dt><%=GetGlobalResourceObject("PortalRemoteControl", "LastModifiedLabel")%></dt>
			            <dd><asp:Label ID="LastModifiedLabel" runat="server" /><br /><asp:HyperLink runat="server" ID="LastModifiedLink" Target="_blank" /></dd>
			            
			            <dt><%=GetGlobalResourceObject("PortalRemoteControl", "PageTemplateLabel")%></dt>
			            <dd><asp:Label ID="PageTemplateLabel" runat="server" /></dd>

			            <dt><%=GetGlobalResourceObject("PortalRemoteControl", "SkinLabel")%></dt>
			            <dd><asp:Label ID="SkinLabel" runat="server" /></dd>
			        </dl>
			    </asp:Panel>
			       
	            <snpe:SNUpdatePanel ID="UpdatePortlet" runat="server" >
                    <Triggers>
                        <asp:PostBackTrigger ControlID="EditModeButton" />
                        <asp:PostBackTrigger ControlID="BrowseModeButton" />
                        <asp:PostBackTrigger ControlID="ResourceEditorMode" />
                    </Triggers>
			        <ContentTemplate>
                        <div id="sn-prc-states">
                            <% var urlNodePath = HttpContext.Current.Request.Params[PortalContext.ContextNodeParamName];
                               if (!string.IsNullOrEmpty(urlNodePath))
                               {
                                   var backUrl = PortalContext.Current.BackUrl;
                                   if (!string.IsNullOrEmpty(backUrl))
                                   { %>
                                    <a href='<%= backUrl %>' title='<%=GetGlobalResourceObject("PortalRemoteControl", "BackToContent")%>' class="sn-prc-button sn-prc-tocontent"><%=GetGlobalResourceObject("PortalRemoteControl", "ApplicationMode")%></a>
                                <% }
                                   else
                                   { %>
                                <sn:ActionLinkButton ID="BrowseOriginalContent" runat="server" ActionName="Browse" ContextInfoID="ContextInfoUrlContent" IncludeBackUrl="false" CssClass="sn-prc-button sn-prc-tocontent" ToolTip="<%$ Resources:PortalRemoteControl,BackToContent %>" IconVisible="false"><%=GetGlobalResourceObject("PortalRemoteControl", "ApplicationMode")%></sn:ActionLinkButton>
                                <% } %>
                            <% } %>
                            <sn:ActionLinkButton ID="BrowseApp" runat="server" ActionName="Browse" ContextInfoID="ContextInfoPage" ParameterString="context={CurrentContextPath}" IncludeBackUrl="true" CssClass="sn-prc-button sn-prc-toapplication" ToolTip="<%$ Resources: PortalRemoteControl, BrowseApp %>" IconVisible="false"><%=GetGlobalResourceObject("PortalRemoteControl", "ContentMode")%></sn:ActionLinkButton>
                            <asp:LinkButton Visible="false" ID="EditModeButton" CommandName="entereditmode" runat="server" CssClass="sn-prc-button sn-prc-editmode" ToolTip="<%$ Resources: PortalRemoteControl, EditMode %>"><%=GetGlobalResourceObject("PortalRemoteControl", "EditMode")%></asp:LinkButton>
		                    <asp:LinkButton Visible="false" ID="BrowseModeButton" CommandName="enterbrowsemode" runat="server" CssClass="sn-prc-button sn-prc-browsemode" ToolTip="<%$ Resources: PortalRemoteControl, PreviewMode %>"><%=GetGlobalResourceObject("PortalRemoteControl", "PreviewMode")%></asp:LinkButton>                            
    		                <sn:ActionLinkButton ID="ActionListLink" runat="server" ActionName="ActionList" ContextInfoID="ContextInfoContent" CssClass="sn-prc-button sn-prc-actionlist"  IconVisible="false" Text="<%$ Resources: PortalRemoteControl, AllActions %>" ToolTip="<%$ Resources: PortalRemoteControl, ListAvailableActions %>" />
		                </div>

				        <div id="sn-prc-actions" class="ui-helper-clearfix">
				        <% if (PortalContext.Current.ActionName != null && PortalContext.Current.ActionName.ToLower() == "explore") { %>
                            <sn:ActionLinkButton ActionName="Explore" ID="ExploreRootLink" runat="server" NodePath="/Root" CssClass="sn-prc-button sn-prc-root" IconSize="32" IconName="sn-prc-root"><%=GetGlobalResourceObject("PortalRemoteControl", "RootConsole")%></sn:ActionLinkButton>
				        <% } else {%>
                            <sn:ActionLinkButton ActionName="OpenInContentExplorer" ID="BrowseRoot" runat="server" NodePath="/Root" CssClass="sn-prc-button sn-prc-root" IconSize="32" IconName="sn-prc-root" ><%=GetGlobalResourceObject("PortalRemoteControl", "RootConsole")%></sn:ActionLinkButton>
                            <sn:ActionLinkButton ActionName="OpenInContentExplorer" ID="ExploreAdvancedLink" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button sn-prc-explore" IconSize="32" IconName="sn-prc-explore" ><%=GetGlobalResourceObject("Action", "OpenInContentExplorer")%></sn:ActionLinkButton>
			            <% }%>   
  					       <sn:ActionLinkButton ID="BrowseLink" runat="server" ActionName="Browse" ContextInfoID="ContextInfoContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-browse" Text="Browse" />
					    
                            <sn:ActionLinkButton ActionName="Versions" ID="Versions" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-versions" ><%=GetGlobalResourceObject("PortalRemoteControl", "Versions")%></sn:ActionLinkButton>
                            <sn:ActionLinkButton ActionName="Edit" ID="EditPage" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32"  IconName="sn-prc-edit-properties"><%=GetGlobalResourceObject("PortalRemoteControl", "Edit")%></sn:ActionLinkButton>
                            <sn:ActionLinkButton ActionName="SetPermissions" ID="SetPermissions" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-set-permissions"  ><%=GetGlobalResourceObject("PortalRemoteControl", "SetPermissions")%></sn:ActionLinkButton>

                            <sn:ActionLinkButton ActionName="Add" ID="AddLinkButton" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-add-new" ><%=GetGlobalResourceObject("PortalRemoteControl", "CreateNewPageTitle")%></sn:ActionLinkButton>
                            <sn:ActionLinkButton ActionName="Rename" ID="Rename" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-rename" ><%=GetGlobalResourceObject("PortalRemoteControl", "Rename")%></sn:ActionLinkButton>
                            <sn:ActionLinkButton ActionName="CopyTo" ID="CopyTo" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-copy" ><%=GetGlobalResourceObject("PortalRemoteControl", "CopyTo")%></sn:ActionLinkButton>
                            <sn:ActionLinkButton ActionName="MoveTo" ID="MoveTo" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-move" ><%=GetGlobalResourceObject("PortalRemoteControl", "MoveTo")%></sn:ActionLinkButton>
                            <sn:ActionLinkButton ActionName="Delete" ID="DeletePage" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-delete"><%=GetGlobalResourceObject("PortalRemoteControl", "DeletePageTitle")%></sn:ActionLinkButton>

                            <sn:ActionLinkButton ActionName="CheckOut" ID="CheckoutButton" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-checkout"><%=GetGlobalResourceObject("PortalRemoteControl", "CheckOut")%></sn:ActionLinkButton>
                            <sn:ActionLinkButton ActionName="CheckIn" ID="CheckinButton" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-checkin"><%=GetGlobalResourceObject("PortalRemoteControl", "CheckIn")%></sn:ActionLinkButton>
                            <sn:ActionLinkButton ActionName="Publish" ID="PublishButton" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-publish"><%=GetGlobalResourceObject("PortalRemoteControl", "Publish")%></sn:ActionLinkButton>
				            <sn:ActionLinkButton ActionName="Approve" ID="Approve" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-approve"><%=GetGlobalResourceObject("PortalRemoteControl", "Approving")%></sn:ActionLinkButton>
                            <sn:ActionLinkButton ActionName="UndoCheckOut" ID="UndoCheckoutButton" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-undo"><%=GetGlobalResourceObject("PortalRemoteControl", "UndoCheckOut")%></sn:ActionLinkButton>
				            <sn:ActionLinkButton ActionName="ForceUndoCheckOut" ID="ForceUndoCheckOut" runat="server" ContextInfoID="ContextInfoAppOrContent" CssClass="sn-prc-button" IconSize="32" IconName="sn-prc-forceundo"><%=GetGlobalResourceObject("PortalRemoteControl", "ForceUndoCheckOut")%></sn:ActionLinkButton>
			                
                            <asp:LinkButton ID="ResourceEditorMode" runat="server" OnClick="ResourceEditorMode_Click" CssClass="sn-prc-button"><sn:SNIcon runat="server" Size="32" Icon="sn-prc-resource-editor" /><%=GetGlobalResourceObject("PortalRemoteControl", "ResourceEditorMode")%></asp:LinkButton>

			                <asp:Panel id="panelPrcPortletButtons" runat="server">
                                <asp:LinkButton Visible="false" ID="ModifyPortletsButton" CommandName="modifyportlets" runat="server" CssClass="sn-prc-button"><sn:SNIcon ID="SNIcon2" runat="server" Icon="prc-editportlets" /><%=GetGlobalResourceObject("PortalRemoteControl", "ModifyPortlets")%></asp:LinkButton>
			                </asp:Panel>

					    </div>
					    
					    <div id="sn-prc-statusbar" class="ui-corner-all"><span></span><strong id="sn-prc-statusbar-text"></strong></div>
                        <%
                            var context = PortalContext.Current.GetApplicationContext() ?? PortalContext.Current.ContextNode;
                            if (context != null && ActionFramework.GetActions(ContentRepository.Content.Create(context), "Prc", null).Count() != 0)
                        { %>
                            <h3><%=GetGlobalResourceObject("PortalRemoteControl", "CustomActionsTitle")%>:</h3>
                        <% } %>
	                    
	                    <sn:ActionList runat="server" ID="ActionListScenario" Scenario="Prc" ContextInfoID="ContextInfoAppOrContent" WrapperCssClass="sn-prc-customactions" />		        


			        </ContentTemplate>
			    </snpe:SNUpdatePanel>

                <span style="display:none;">
		            <asp:LinkButton ID="AddPortletButton" runat="server" CommandName="addportlet" CssClass="sn-prc-button sn-prc-hiddenaddportlet"><%=HttpContext.GetGlobalResourceObject("PortalRemoteControl", "AddPortlet")%></asp:LinkButton>
		            <asp:TextBox ID="AddPortletButtonTextBox" runat="server" CssClass="sn-prc-button sn-prc-hiddenaddportlettb" />
		        </span>

            </div>
        </div>
</div>
<div id="Message" runat="server" visible="false"><%=HttpContext.GetGlobalResourceObject("PortalRemoteControl", "PRCError")%></div>




