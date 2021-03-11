<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="~/Controls/TagCloudPortlet.ascx.cs" Inherits="Portal.Portlets.Controls.TagCloudControl" %>

<script runat="server" type="text/C#">
protected string GetCurrentUrl()
{
    if (Portal.Virtualization.PortalContext.Current != null && Portal.Virtualization.PortalContext.Current.ContextWorkspace != null)
    {
        return Portal.Helpers.Actions.ActionUrl(ContentRepository.Content.Load(Portal.Virtualization.PortalContext.Current.ContextWorkspace.Path), "Search", false);
    }
    else return String.Empty;
}    
</script>
<% if((this.FindControl("TagCloudRepeater") as Repeater).Items.Count == 0){%>
    <div class="sn-tags-notags"><span><%= ContentRepository.i18n.ResourceManager.Current.GetString("Portal", "SnBlog_TagCloudPortlet_NoTags") %></span></div>
<%}
else{%>
    <asp:Repeater ID="TagCloudRepeater" runat="server">
        <HeaderTemplate>
            <div class="sn-tags">
                <ul>
        </HeaderTemplate>
        <ItemTemplate>
            <li class="sn-tag<%# Eval("Value") %>">           
                <a title='<%# System.Web.HttpUtility.HtmlEncode(Eval("Key")) %>' href='<%# GetCurrentUrl() + "&text=" + System.Web.HttpUtility.HtmlEncode(Eval("Key").ToString()) %>'><%# System.Web.HttpUtility.HtmlEncode(Eval("Key"))%></a>
            </li>
        </ItemTemplate>
        <FooterTemplate>
            </ul> </div>
        </FooterTemplate>
    </asp:Repeater>
<%} %>
