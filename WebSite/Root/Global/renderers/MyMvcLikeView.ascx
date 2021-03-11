<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="Portal.Portlets" %>
<%@ Import Namespace="Portal.Helpers" %>

<div class="sn-entries">
<% foreach (var content in this.Model.Items)
{
       if (content.Children.Count() > 0)
       {
           %><h3 style="color: red"><%
       }
       else
       {
           %><h3><%
       }%>
       <%= HttpUtility.HtmlEncode(content.DisplayName) %></h3>
       <p><%= content.Path %></p>
       <p><%= content.Description %></p>
<%} %>
</div>

