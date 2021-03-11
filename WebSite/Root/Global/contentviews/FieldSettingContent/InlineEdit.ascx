<%@ Language="C#" AutoEventWireup="true" Inherits="Portal.UI.GenericContentView" %>
<%@ Import Namespace="Portal.UI" %>
<%@ Import Namespace="ContentRepository" %>
<%@ Import Namespace="ContentRepository.Versioning" %>

<div id="InlineViewContent" runat="server" class="sn-content sn-content-inlineview">
        [GENERIC CONTENT PLACEHOLDER]
</div>
<asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>

<div class="sn-panel sn-buttons">
  <sn:CommandButtons ID="CommandButtons1" runat="server" layoutControlPath="/Root/System/SystemPlugins/Controls/CommandButtons.ascx" />
</div>