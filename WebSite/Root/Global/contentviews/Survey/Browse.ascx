<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.UI.SurveyContentView" EnableViewState="false" %>
<%@ Register Assembly="Portal" Namespace="Portal.UI.Controls" TagPrefix="sn" %>
<%@ Import Namespace="ContentRepository.Storage" %>
<%@ Import Namespace="Portal.Virtualization" %>

<div class="sn-survey sn-content">
    <h3 class="sn-content-title"><%= GetValue("DisplayName")%></h3>
    <div class="sn-content-lead"><%= GetValue("Description")%></div>

    <div>
            <sn:ActionLinkButton ID="ActionLinkButton1" NodePath='<%#Eval("Path") %>' ActionName="Add" IconName="edit" Text="<%$ Resources: Survey, FillOut %>" ParameterString='<%# "ContentTypeName=SurveyItem" %>' runat="server" />
    </div>
    <div><asp:Label CssClass="sn-error" runat="server" ID="LiteralMessage" Visible="false"></asp:Label></div>
    <div><asp:PlaceHolder runat="server" ID="phInvalidPage" /></div>
</div>
