<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.UI.SingleContentView" %>
<%@ Import Namespace="Portal.Virtualization" %>
<%@ Import Namespace="ContentRepository.Storage" %>
<div>
    <span><%=GetGlobalResourceObject("Survey", "AfterSubmitPage")%></span>
    <span><a href='<%= GetValue("LandingPage.Path") %>?<%= PortalContext.Current.GeneratedBackUrl %>'>Browse</a></span>
    <span><a href='<%= GetValue("LandingPage.Path") %>?action=EditSurveyTemplate&<%= PortalContext.Current.GeneratedBackUrl %>'><%=GetGlobalResourceObject("Content", "Edit")%></a></span>
</div>

<div>
    <span><%=GetGlobalResourceObject("Survey", "InvalidSurveyPage")%></span>
    <span><a href='<%= GetValue("InvalidSurveyPage.Path") %>?<%= PortalContext.Current.GeneratedBackUrl %>'>Browse</a></span>
    <span><a href='<%= GetValue("InvalidSurveyPage.Path") %>?action=EditSurveyTemplate&<%= PortalContext.Current.GeneratedBackUrl %>'><%=GetGlobalResourceObject("Content", "Edit")%></a></span>
</div>

<div>
    <span><%=GetGlobalResourceObject("Survey", "MailtTemplatePage")%></span>
    <span><a href='<%= GetValue("MailTemplate.Path") %>?<%= PortalContext.Current.GeneratedBackUrl %>'>Browse</a></span>
    <span><a href='<%= GetValue("MailTemplate.Path") %>?action=EditSurveyTemplate&<%= PortalContext.Current.GeneratedBackUrl %>'><%=GetGlobalResourceObject("Content", "Edit")%></a></span>
</div>