<%@  Language="C#" EnableViewState="false" %>
<%@ Import Namespace="SNControls=Portal.UI.Controls" %>
<%@ Import Namespace="SNFields=ContentRepository.Fields" %>

<span class='<%# "sn-date-" + Portal.UI.UITools.GetFieldNameClass(Container) %>'><%# DataBinder.Eval(Container, "Data") %></span>
<script>
    $(function () {
        SN.Util.setFriendlyLocalDate('<%# "span.sn-date-" + Portal.UI.UITools.GetFieldNameClass(Container) %>', '<%= 
            System.Globalization.CultureInfo.CurrentUICulture %>', '<%# 
            ((DateTime)((SNControls.FieldControl)Container).GetData()).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-US")) %>', '<%= 
            System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern.ToUpper() %>', <%# 
            (((SNControls.DatePicker)Container).Mode != SNFields.DateTimeMode.Date).ToString().ToLower() %>);
    });
</script>
