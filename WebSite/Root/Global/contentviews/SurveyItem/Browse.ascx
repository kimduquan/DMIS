﻿<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.UI.SingleContentView" EnableViewState="false" %>
<%@ Register Assembly="Portal" Namespace="Portal.UI.Controls" TagPrefix="sn" %>

<div class="sn-content sn-survey-item">
    <h2 class="sn-content-title"><%= GetValue("DisplayName")%></h2>
    <div class="sn-lead"><%= GetValue("Description")%></div>
    <sn:GenericFieldControl ID="GenericField1" runat="server" ContentListFieldsOnly="True" />
</div>
