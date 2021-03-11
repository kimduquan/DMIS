<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Portal.Portlets" %>
<%@ Import Namespace="Portal.Helpers" %>

<asp:Panel ID="pnlSearchControls" runat="server" DefaultButton="SearchButton" >
    <table class="sn-search">
    <tbody>
         <tr>
            <td><asp:TextBox CssClass="sn-ctrl sn-ctrl-text" ID="SearchBox" runat="server" Width="200px"/></td>
            <td>
                <asp:Button CssClass="sn-submit" ID="SearchButton" Text='<%$ Resources: Portal, SnBlog_SearchPortlet_SearchButtonTitle %>' runat="server" />
             </td>
         </tr>
    </tbody>
    </table>
    <% if(Portal.Virtualization.PortalContext.Current.RequestedUri.Query.Contains("&text=")){ %>
    <asp:Panel runat="server" ID="ErrorPanel" Visible="false" CssClass="sn-error-msg">
        <asp:Label runat="server" ID="ErrorLabel"></asp:Label><br />
    </asp:Panel>
    <%} %>
</asp:Panel> 

<% if (Portal.Virtualization.PortalContext.Current.RequestedUri.Query.Contains("&text="))
{ %> 
    <div class="sn-search-result-count">  
         <span><%= String.Format(ContentRepository.i18n.ResourceManager.Current.GetString("Portal", "SnBlog_SearchPortlet_SearchResults"), this.Model.Items.Count())%></span>
    </div>
<%} %>


<% if ((((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl && this.Model.Pager.Pagecount > 1) || this.Model.VisibleFieldNames.Length > 0)
   { %>
<div class="sn-list-navigation ui-widget-content ui-corner-all ui-helper-clearfix">
    
    <% if (this.Model.VisibleFieldNames.Length > 0) { %>
    <select class="sn-sorter sn-ctrl-select ui-widget-content ui-corner-all" onchange="if (this.value!='') window.location.href=this.value;">
        <option value=""><%=GetGlobalResourceObject("Content", "SelectOrdering")%></option>   
        <%foreach (var field in this.Model.VisibleFieldNames) { %>
            <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == false).Url %>"><%=field %> <%=GetGlobalResourceObject("Content", "Ascending")%></option>
            <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == true).Url %>"><%=field %> <%=GetGlobalResourceObject("Content", "Descending")%></option>
        <% } %>
    </select>
    <%} %>
    
    <% if (((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl && this.Model.Pager.Pagecount > 1)
       { %>
    <div class="sn-pager">
        <%foreach (var pageAction in this.Model.Pager.PagerActions) {
        
            if (pageAction.CurrentlyActive) {  %>
                <span class="sn-pager-item sn-pager-active"><%=pageAction.PageNumber%></span>
            <%} else { %>
                <a class="sn-pager-item" href="<%=pageAction.Url %>"><%=pageAction.PageNumber%></a>
            <%} %>
        
        <% } %>
    </div>
    <% } %>
</div>
<% } %>

<% if(this.Model.Items.Count() > 0){ %>
    <div class="sn-search-results">
        <%foreach (var content in this.Model.Items)
          { %>
      
            <div class="sn-search-result ui-helper-clearfix sn-blogpost">
                <h2 class="sn-title sn-blogpost-title"><%= Actions.BrowseAction(content) %></h2>
                <div>
                    <span class="sn-blogpost-createdby"><%= (content.ContentHandler as Portal.BlogPost).CreatedBy["FullName"] %></span> - 
                    <span class="sn-blogpost-publishedon"><%= content["PublishedOn"] %></span>
                    <% var i = 0;
                       foreach (string tag in content["Tags"].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) { 
                        var wsContent = ContentRepository.Content.Load(content.WorkspacePath);
                        i++;
                    %>
                    <%= i > 1 ? ", " : " - " %><a class="sn-blogpost-tag" href="<%= Actions.ActionUrl(wsContent, "Search", false) + "&text=" + ContentRepository.Security.Sanitizer.Sanitize(tag) %>"><%= ContentRepository.Security.Sanitizer.Sanitize(tag)%></a>
                    <% } %>
                </div>
                <div class="sn-content-body"><%= content["LeadingText"]%></div>
            </div> 
        
        <%} %>
    </div>
<% } %>

<% if ((((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl && this.Model.Pager.Pagecount > 1) || this.Model.VisibleFieldNames.Length > 0)
   { %>
<div class="sn-list-navigation ui-widget-content ui-corner-all ui-helper-clearfix">
    
    <% if (this.Model.VisibleFieldNames.Length > 0) { %>
    <select class="sn-sorter sn-ctrl-select ui-widget-content ui-corner-all" onchange="if (this.value!='') window.location.href=this.value;">
        <option value=""><%=GetGlobalResourceObject("PortalRemoteControl", "SelectOrdering")%></option>   
        <%foreach (var field in this.Model.VisibleFieldNames) { %>
            <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == false).Url %>"><%=field %> <%=GetGlobalResourceObject("Content", "Ascending")%></option>
            <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == true).Url %>"><%=field %> <%=GetGlobalResourceObject("Content", "Descending")%></option>
        <% } %>
    </select>
    <%} %>
    
    <% if (((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl && this.Model.Pager.Pagecount > 1)
       { %>
    <div class="sn-pager">
        <%foreach (var pageAction in this.Model.Pager.PagerActions) {
        
            if (pageAction.CurrentlyActive) {  %>
                <span class="sn-pager-item sn-pager-active"><%=pageAction.PageNumber%></span>
            <%} else { %>
                <a class="sn-pager-item" href="<%=pageAction.Url %>"><%=pageAction.PageNumber%></a>
            <%} %>
        
        <% } %>
    </div>
    <% } %>
</div>
<% } %> ​