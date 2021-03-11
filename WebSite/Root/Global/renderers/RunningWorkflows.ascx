<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="ApplicationModel" %>
<%@ Import Namespace="Portal.Portlets" %>
<%@ Import Namespace="Portal.Helpers" %>
<%@ Import Namespace="Portal.Virtualization" %>
<%@ Import Namespace="ContentRepository" %>
<%@ Import Namespace="ContentRepository.Storage" %>
<%@ Import Namespace="ContentRepository.Fields" %>
<%@ Import Namespace="Workflow" %>


<% if (this.Model.Content == null || !this.Model.Content.Children.Any())
   { %>

<div class="sn-workflow-list">
    <%=GetGlobalResourceObject("Workflow", "NoRunningWorkflows")%>
</div>

<% } else { %>

<div class="sn-workflow-filters">
    <%=GetGlobalResourceObject("Workflow", "FilterWorkflows")%>
    <input type="radio" name="sn-workflow-filter" class="sn-workflow-filter" id="wfFilterAll" value="-1" checked="checked" /><label for="wfFilterAll"><%=GetGlobalResourceObject("Workflow", "All")%></label>
    <% 
        var backUrl = PortalContext.Current.BackUrl;

        foreach(var name in Enum.GetNames(typeof(Workflow.WorkflowStatusEnum))) {
        string statusID = ((int)Enum.Parse(typeof(Workflow.WorkflowStatusEnum), name)).ToString();
        if (statusID != "0") {
    %>
        <input type="radio" name="sn-workflow-filter" class="sn-workflow-filter" id="wfFilter<%= statusID %>" value="<%= statusID %>" /><label for="wfFilter<%= statusID %>"><%= name %></label>
    <%
            }
        } 
    %>
</div>

<div id="sn-current-workflows" class="sn-workflow-list">
<% foreach (var content in this.Model.Items) {
    var actionName = ( (content.ContentHandler as WorkflowHandlerBase).WorkflowStatus == WorkflowStatusEnum.Running ) ? "Abort" : "Delete";
    var action = ActionFramework.GetAction(actionName, content, new { RedirectToBackUrl = true });
    var clientAction = action as ClientAction;
    var relatedContent = (Node)content["RelatedContent"];
    var status = (content.ContentHandler as WorkflowHandlerBase).WorkflowStatus.ToString();
%>
    <div class="sn-content sn-workflow ui-helper-clearfix sn-wf-state-<%= ((int)Enum.Parse(typeof(Workflow.WorkflowStatusEnum), status)).ToString() %>">
        <% if (action != null) { %>
        <% if (clientAction != null)
           { %>
           <span style="cursor:pointer;" class="sn-actionlinkbutton" onclick="<%= clientAction.Callback %>">
           <%= Portal.UI.IconHelper.RenderIconTag("delete", null, 16)%>
            <%= actionName %>
            <%=GetGlobalResourceObject("Workflow", "Workflow")%>
            </span>
        <% }
           else
           { %>
           <a class="sn-actionlinkbutton" href="<%=action.Uri %>">
           <%= Portal.UI.IconHelper.RenderIconTag("delete", null, 16)%>
            <%= actionName %>
            <%=GetGlobalResourceObject("Workflow", "Workflow")%></a>
        <% } %>
            
        <% } %>
        <h2 class="sn-content-title">
            <%= Portal.UI.IconHelper.RenderIconTag(content.Icon, null, 32)%>
            <%= Actions.BrowseAction(content) %>    
        </h2>
        <div class="sn-content-lead">
            <% if (relatedContent != null && relatedContent.Id != PortalContext.Current.ContextNode.Id) { %>
            <%=GetGlobalResourceObject("Workflow", "Content")%><strong>
                <%= HttpUtility.HtmlEncode(relatedContent.DisplayName) %></strong><br />
            <% } %>
            <%=GetGlobalResourceObject("Workflow", "Status")%><strong>
                <%= status %></strong>
        </div>
    </div>
<%  } %>
</div>

<sn:InlineScript runat="server">
<script type="text/javascript">
    $(function() {

        //Initialize workflow-list filters
        $(".sn-workflow-filters").each(function () { 
            
            var $workflowFilters = $(".sn-workflow-filter",this);
            var $workflowList = $(this).next(".sn-workflow-list");
            var $workflowItems = $(".sn-workflow", $workflowList);

            $workflowFilters.each(function () {
                var $this = $(this);
                var value = $this.val();

                if (value != -1) var $relatedWorkflows = $(".sn-wf-state-" + value, $workflowList);

                $this.button().click(function () {
                    if (value != -1) {
                        $workflowItems.slideUp(200);
                        $relatedWorkflows.slideDown(200);
                    } else {
                        $workflowItems.slideDown(200);
                    }
                });

            });
        });

    });
</script>
</sn:InlineScript>

<% } %>
