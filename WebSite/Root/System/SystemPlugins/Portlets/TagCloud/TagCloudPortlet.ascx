<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="~/Controls/TagCloudPortlet.ascx.cs"
    Inherits="Portal.Portlets.Controls.TagCloudControl" %>

<script runat="server" type="text/C#">
protected string GetCurrentUrl()
{
    var url = Portal.Virtualization.PortalContext.Current.RequestedUri.ToString();
    var query = Portal.Virtualization.PortalContext.Current.RequestedUri.Query;
    if (!string.IsNullOrEmpty(query))
	{
        return url.Replace(query, "");
	}
    return url;
    
    
    
}
    
</script>

<asp:Repeater ID="TagCloudRepeater" runat="server">
    <HeaderTemplate>
        <div class="sn-tags">
            <ul>
    </HeaderTemplate>
    <ItemTemplate>
        <li class="sn-tag<%# Eval("Value") %>">
           
            <a href='<%# SearchPortletPath == "" ? GetCurrentUrl() + "?action=SearchTag&" : SearchPortletPath + "?" %>defaultInput=<%# HttpUtility.UrlEncode(Eval("Key").ToString()) %>'><%# HttpUtility.HtmlEncode(Eval("Key"))%></a> </li>
    </ItemTemplate>
    <FooterTemplate>
        </ul> </div>
    </FooterTemplate>
</asp:Repeater>
