<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="ContentRepository.Storage" %>
<%@ Import Namespace="ContentRepository.Storage.Schema" %>
<%@ Import Namespace="Portal.Helpers" %>
<%@ Import Namespace="Portal.UI" %>
<%@ Import Namespace="ContentRepository.i18n" %>

<div class="sn-contentlist sn-bloglist">
    <%
        if (this.Model.Items.Count() == 0)
        {%>
        <div class="sn-blogpost">
            <p class="sn-blogpost-missing"><%= ResourceManager.Current.GetString("Portal", "SnBlog_BlogPostsPortlet_NoPosts")%></p>
        </div>
        <%}
        else
        {
            var wsContent = ContentRepository.Content.Load(this.Model.Content.WorkspacePath);
            bool showAvatar = wsContent["ShowAvatar"].Equals(true);
            foreach (var content in this.Model.Items)
            {
    %>
    <div class="sn-blogpost">
        <div>
            <% if (showAvatar)
               { %>
                <div style="float: left; margin-right: 10px;">
                    <img class="sn-blogpost-avatar" style="margin-top: 5px" src='<%= UITools.GetAvatarUrl(content["CreatedBy"] as ContentRepository.User, 48, 48) %>' alt='<%# Eval("CreatedBy") %>' />
                </div>
            <%} %>            
            <div>
                <div class="sn-blogpost-admin">
                    <% var editUrl = Actions.ActionUrl(content, "Edit", true);
                       var deleteUrl = Actions.ActionUrl(content, "Delete", true); 
                       if(!String.IsNullOrEmpty(editUrl))
                       {%>
                          <div class="sn-blogpost-admin-item"><img src="/Root/Global/images/icons/16/edit.png" alt="<%=GetGlobalResourceObject("Content", "Edit")%>" /><a href="<%= editUrl %>"><%= ContentRepository.i18n.ResourceManager.Current.GetString("Portal", "SnBlog_BlogPostsPortlet_EditPost")%></a></div>
                       <%}
                       if (!String.IsNullOrEmpty(deleteUrl))
                       {%>
                           <div class="sn-blogpost-admin-item"><img src="/Root/Global/images/icons/16/delete.png" alt="<%=GetGlobalResourceObject("Content", "Delete")%>" />
                                <a href="javascript:void(0);" onclick="<%= deleteUrl %>" class="sn-actionlink"><%= ResourceManager.Current.GetString("Portal", "SnBlog_BlogPostsPortlet_DeletePost")%></a>
                            </div>
                       <%}
                    %>                    
                </div>
                <div class="sn-blogpost-info">
                    <h1 class="sn-blogpost-title">
                        <%=Actions.BrowseAction(content, true)%>
                    </h1>
                    <span class="sn-blogpost-createdby"><%=(content["CreatedBy"] as ContentRepository.User).FullName%></span> 
                    <span class="sn-blogpost-publishedon"><%=content["PublishedOn"]%></span>
                    <%  var i = 0;
                        foreach (string tag in content["Tags"].ToString().Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries)) {
                        i++;
                    %>
                        <%= i > 1 ? ", " : " - " %>
                        <a class="sn-blogpost-tag" href="<%= Actions.ActionUrl(wsContent, "Search", false) + "&text=" + ContentRepository.Security.Sanitizer.Sanitize(tag) %>"><%= ContentRepository.Security.Sanitizer.Sanitize(tag)%></a><% } %>
                 </div>
            </div>
            
        </div>
        <div class="sn-blogpost-lead">
            <%=content["LeadingText"]%>
            <% if (!String.IsNullOrEmpty(content["BodyText"].ToString()))
               {%>
                <a class="sn-blogpost-readmore" href="<%=Actions.BrowseUrl(content, true) %>"><%= ContentRepository.i18n.ResourceManager.Current.GetString("Portal", "SnBlog_BlogPostsPortlet_ReadMore")%></a>
            <%} %>
        </div>       
        
        <div class="sn-blogpost-footer">
            <div class="sn-blogpost-comments">
            <% 
                var commentCount = Portal.Wall.CommentInfo.GetCommentCount(content.Id);
                if (commentCount > 0) { 
                    %><strong><a href="<%= Actions.ActionUrl(content, "Browse", true) %>"><%= String.Format(ContentRepository.i18n.ResourceManager.Current.GetString("Portal", "SnBlog_BlogPostsPortlet_Comments"), commentCount) %></a></strong><%
                }
                else {                     
                    %><i><%= ContentRepository.i18n.ResourceManager.Current.GetString("Portal", "SnBlog_BlogPostsPortlet_NoComments")%></i><%
                }
            %>                
            </div>
        </div>
        <div class="sn-clearfix"></div>
    </div>
    <%} %>

    <% if (((Portal.Portlets.ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl && this.Model.Pager.Pagecount > 1)
       { %>
    <div class="sn-pager sn-blog-pager">
        <%foreach (var pageAction in this.Model.Pager.PagerActions)
          {

              if (pageAction.CurrentlyActive)
              {  %>
                <span class="sn-pager-item sn-pager-active"><%=pageAction.PageNumber%></span>
            <%}
              else
              { %>
                <a class="sn-pager-item" href="<%=pageAction.Url %>"><%=pageAction.PageNumber%></a>
            <%} %>
        
        <% } %>
    </div>
    <% }
        } %>

</div>

<script> $(function () {

     $('.sn-blogpost-publishedon').each(function () {
         var that = $(this);
         that.text =
         SN.Util.setFriendlyLocalDate(that, '<%= System.Globalization.CultureInfo.CurrentUICulture%>', that.text(), '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern %>', '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern %>');
     });
 });
</script>