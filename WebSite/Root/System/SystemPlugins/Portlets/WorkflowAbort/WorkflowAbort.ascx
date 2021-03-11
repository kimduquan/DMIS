<%@ Control Language="C#" AutoEventWireup="false" Inherits="System.Web.UI.UserControl" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="ApplicationModel" %>
<%@ Import Namespace="Portal.Portlets" %>
<%@ Import Namespace="Portal.Helpers" %>
<%@ Import Namespace="Portal.Virtualization" %>
<%@ Import Namespace="ContentRepository" %>
<%@ Import Namespace="ContentRepository.Fields" %>
<%@ Import Namespace="Workflow" %>

 <div class="sn-pt-body-border ui-widget-content">
     <div class="sn-pt-body">
         <div class="sn-dialog-lead">
             <sn:SNIcon ID="SNIcon1" Icon="warning" Size="32" runat="server" /><%=GetGlobalResourceObject("WorkflowAbort", "AboutToAbort")%> <strong><asp:Label ID="ContentName" runat="server" /></strong>
         </div>
         <div style="padding-left: 45px;">
            <% var context = ContextBoundPortlet.GetContextNodeForControl(this) as WorkflowHandlerBase;
               var relatedContent = context.GetReference<ContentRepository.Storage.Node>("RelatedContent"); 
               
               if (relatedContent != null) {
            %>
                <%=GetGlobalResourceObject("WorkflowAbort", "RelatedContent")%>: <strong><%= HttpUtility.HtmlEncode(relatedContent.DisplayName)%></strong>
            <% } %>
         </div>
         <asp:PlaceHolder runat="server" ID="ErrorPanel">
             <div class="sn-error-msg">
                <asp:Label runat="server" ID="ErrorLabel" />
             </div>
         </asp:PlaceHolder>
     </div>
</div>   
        
<div class="sn-pt-body-border ui-widget-content sn-dialog-buttons">
    <div class="sn-pt-body">
        <asp:Button ID="Abort" runat="server" Text="<%$ Resources:WorkflowAbort,Abort %>" CommandName="Abort" CssClass="sn-submit" />
        <sn:BackButton Text="<%$ Resources:WorkflowAbort,Cancel %>" ID="BackButton1" runat="server" CssClass="sn-submit" />
    </div>
</div>