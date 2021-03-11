<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.UI.SurveyContentView" %>
<%@ Register Assembly="Portal" Namespace="Portal.UI.Controls" TagPrefix="sn" %>
<%@ Import Namespace="ContentRepository.Storage" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Portal.Virtualization" %>
<%@ Register TagPrefix="sn" Assembly="CorePortlets" Namespace="Portal.Portlets" %>

<sn:Toolbar runat="server">
    <asp:LinkButton ID="VotingAndResult" runat="server" Text="Link" Visible="false"/>
</sn:Toolbar>

<asp:Panel runat="server" ID="QuestionPanel">
    <h2 class="sn-content-title"><%= HttpUtility.HtmlEncode(GetValue("DisplayName")) %></h2>
    <div class="sn-lead"><%= GetValue("Description") %></div>
    <asp:PlaceHolder runat="server" ID="QuestionPlaceHolder"></asp:PlaceHolder>
</asp:Panel>

