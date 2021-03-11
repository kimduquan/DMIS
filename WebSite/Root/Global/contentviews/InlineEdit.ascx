<%@ Language="C#" AutoEventWireup="true" Inherits="Portal.UI.GenericContentView" %>
<%@ Import Namespace="Portal.UI" %>
<%@ Import Namespace="ContentRepository" %>
<%@ Import Namespace="ContentRepository.Versioning" %>

<div class="sn-content-inlineview-header ui-helper-clearfix">
    <%= Portal.UI.IconHelper.RenderIconTag(Content.Icon, null, 32) %>
	<div class="sn-content-info">
        <h2 class="sn-view-title"><% = HttpUtility.HtmlEncode(DisplayName) %> (<%= ContentRepository.Content.Create(ContentType)["DisplayName"].ToString() %>)</h2>
        <strong><%=GetGlobalResourceObject("Content", "Path")%></strong> <%= ContentHandler.Path %>
    <% var gc = ContentHandler as GenericContent;
       if (gc.VersioningMode > VersioningType.None || gc.ApprovingMode == ApprovingType.True || gc.Locked || gc.Version.Major > 1) { %>
       <br /><strong><%=GetGlobalResourceObject("Content", "Version")%></strong> <%= ContentHandler.Version.ToDisplayText() %>
    <% } %>
    </div>
</div>
<div id="InlineViewContent" runat="server" class="sn-content sn-content-inlineview">
        [GENERIC CONTENT PLACEHOLDER]
</div>
<asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>

<div class="sn-panel sn-buttons">
  <sn:CommandButtons ID="CommandButtons1" runat="server" layoutControlPath="/Root/System/SystemPlugins/Controls/CommandButtons.ascx" />
</div>