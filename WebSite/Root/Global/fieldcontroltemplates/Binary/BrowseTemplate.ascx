<%@  Language="C#" EnableViewState="false" %>
<%@ Import Namespace="Portal.UI" %>
<%@ Import Namespace="Portal.UI.Controls" %>
<%@ Import Namespace="Portal.Virtualization" %>

<a href='<%# ContentTools.GetBinaryUrl(((Binary)Container).Field) %>'><%# ((Binary)Container).Field.Name == PortalContext.DefaultNodePropertyName ? ((Binary)Container).Field.Content.DisplayName : string.Concat(((Binary)Container).Field.Content.DisplayName, " (", ((Binary)Container).Field.DisplayName, ")") %></a>