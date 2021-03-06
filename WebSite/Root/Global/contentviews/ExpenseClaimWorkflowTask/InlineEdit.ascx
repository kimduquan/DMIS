<%@  Language="C#" AutoEventWireup="true" Inherits="Portal.UI.SingleContentView" EnableViewState="false" %>
<%@ Import Namespace="ContentRepository" %>
<%@ Import Namespace="ContentRepository.Fields" %>
<%@ Import Namespace="Portal.UI" %>
<%@ Import Namespace="Portal.UI.Controls" %>
<%@ Import Namespace="Portal.Helpers" %>

<sn:ErrorView ID="ErrorView1" runat="server" />

<% var eClaim = this.Content.ContentHandler.GetReference<ExpenseClaim>("ContentToApprove");
   var assignee = ((IEnumerable<ContentRepository.Storage.Node>)this.Content["AssignedTo"]).FirstOrDefault();
    if (eClaim != null) { %>

<div class="sn-inputunit ui-helper-clearfix" id="InputUnitPanel1">
    <div class="sn-iu-label">
        <asp:Label CssClass="sn-iu-title" ID="LabelForTitle" runat="server"><%=this.Content.Fields["ContentToApprove"].DisplayName %></asp:Label>
    </div>
    <div class="sn-iu-control">
        <%= IconHelper.RenderIconTag(eClaim.Icon)%>
        <a href='<%= Actions.BrowseUrl(ContentRepository.Content.Create(eClaim)) %>' title='<%=eClaim.DisplayName %>'><%=eClaim.DisplayName %></a> (in 
        <a href='<%= Actions.BrowseUrl(ContentRepository.Content.Create(eClaim.Parent)) %>' title='<%=eClaim.ParentPath %>' target="_blank"><%=eClaim.Parent.DisplayName%></a>)     
    </div>
</div>
   <% } %>

<div class="sn-inputunit ui-helper-clearfix">
	<div class=sn-iu-label>
		<span class=sn-iu-title><%=GetGlobalResourceObject("Content", "ExpanseClaimItems")%></span> <br/>
		<span class=sn-iu-desc></span> 
	</div>
	<div class=sn-iu-control>
        <table class="sn-listgrid ui-widget-content">
            <thead>
                <tr>  
                    <th class="sn-lg-col-1 ui-state-default"><%=GetGlobalResourceObject("Content", "DisplayName")%></th>
                    <th class="sn-lg-col-2 ui-state-default"><%=GetGlobalResourceObject("Content", "Amount")%></th>
                </tr>
            </thead>
            <tbody>
        <% foreach (var ecItemNode in eClaim.Children) { %>            
            <tr class="sn-lg-row0 ui-widget-content">
                <td><%= ecItemNode.DisplayName %></td>
                <td><%= ecItemNode["Amount"] %> <%=GetGlobalResourceObject("Content", "Currency")%></td>
            </tr>
        <% } %>
            </tbody>
        </table>
        <br />
        <span><%=GetGlobalResourceObject("Content", "Sum")%>: </span><strong><%= eClaim.Sum %> <%=GetGlobalResourceObject("Content", "Currency")%></strong>
	</div>
</div>

<sn:DatePicker ID="DueDate" runat="server" FieldName="DueDate" ControlMode="Browse" />

<% var ass = this.Content.ContentHandler.GetReference<User>("AssignedTo");
   if (ass != null)
   { %>
   <div class="sn-inputunit ui-helper-clearfix" id="Div1">
    <div class="sn-iu-label">
        <span class="sn-iu-title"><%=this.Content.Fields["AssignedTo"].DisplayName%></span>
    </div>
    <div class="sn-iu-control">
        
     <a href='<%= Actions.ActionUrl(ContentRepository.Content.Create(ass), "Profile") %>' title='<%=ass.FullName %>'><%=ass.FullName%></a>
    </div>
</div>
   <% } %>

   <% var rejResult = this.Content["Result"] as List<string>;
      if (rejResult != null && rejResult.FirstOrDefault() == "no")
   { %>
   <div class="sn-inputunit ui-helper-clearfix" id="Div2">
    <div class="sn-iu-label">
        <span class="sn-iu-title"><%=GetGlobalResourceObject("Workflow", "RejectReason")%></span>
    </div>

    <div class="sn-iu-control">
       <sn:LongText ID="RejectReason" runat="server" FieldName="RejectReason"/>
    </div>
</div>

<% } %>

<div class="sn-panel sn-buttons">
<% var status = this.Content.ContentHandler.GetProperty<string>("Result");
   if (status == null)
   { 
       var gc = this.Content.ContentHandler.GetReference<GenericContent>("ContentToApprove");
       if (gc != null && SavingAction.HasApprove(gc))
       {%>
    <asp:Button CssClass="sn-submit" Text="<%$ Resources:Workflow,Approve %>" ID="Approve" runat="server" OnClick="Click" CommandName="Approve" />
    <sn:RejectButton ID="RejectButton" runat="server"/>

    <sn:BackButton CssClass="sn-submit" Text="<%$ Resources:Workflow,Cancel %>" ID="BackButton1" runat="server" />
    <% } else { %>
           <span><%=GetGlobalResourceObject("Content", "DoNotHavePermission")%></span>
           <%
       }
   }
   else
   { %>
   <%=GetGlobalResourceObject("Workflow", "TaskAlredyCompleted")%><strong><%= ((ChoiceFieldSetting)(this.Content.Fields["Result"].FieldSetting)).Options.SingleOrDefault(opt => opt.Value == GetValue("Result")).Text%></strong>
   <sn:BackButton CssClass="sn-submit" Text="<%$ Resources:Content,Done %>" ID="BackButton2" runat="server" />
    <% } %>
    
</div>

<script runat="server">

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        this.RejectButton.OnReject += OnReject;
    }
    
    protected virtual void Click(object sender, EventArgs e)
    {
        string actionName = "";
        IButtonControl button = sender as IButtonControl;
        if (button != null)
            actionName = button.CommandName;

        if (!string.IsNullOrEmpty(actionName))
        {
            switch (actionName)
            {
                case "Approve": 
                    this.Content["Result"] = "yes"; 
                    this.Content["Status"] = "completed";
                    this.Content.Save(); 
                    this.RedirectToParent(); 
                    return;
            }
        }

        base.Click(sender, e);
    }

    protected void OnReject(object sender, VersioningActionEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Comments))
            return;

        this.Content["Result"] = "no";
        this.Content["Status"] = "completed";
        this.Content["RejectReason"] = e.Comments;
        this.Content.Save();
        this.RedirectToParent();
    }

</script>