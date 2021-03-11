<span class='<%# "sn-date-" + Portal.UI.UITools.GetClassForField(Container.DataItem, "@@fieldName@@") %>'>
</span>
<script> $(function () { SN.Util.setFriendlyLocalDate('<%# "span.sn-date-" +
    Portal.UI.UITools.GetClassForField(Container.DataItem, "@@fieldName@@")%>', '<%= 
    System.Globalization.CultureInfo.CurrentUICulture %>', '<%# 
    (Container.DataItem as SNCR.Content).Fields.ContainsKey("@@fieldName@@") &&
        ((Container.DataItem as SNCR.Content).Fields["@@fieldName@@"].FieldSetting as SNCR.Fields.DateTimeFieldSetting).DateTimeMode == SNCR.Fields.DateTimeMode.Date 
            ? ((DateTime)Eval("@@bindingName@@")).ToString("M/d/yyyy", SNCR.Fields.DateTimeField.DefaultUICulture) 
            : ((DateTime)Eval("@@bindingName@@")).ToString(SNCR.Fields.DateTimeField.DefaultUICulture) %>', '<%=
    System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern.ToUpper() %>', <%# 
    (((Container.DataItem as SNCR.Content).Fields["@@fieldName@@"].FieldSetting as SNCR.Fields.DateTimeFieldSetting).DateTimeMode != SNCR.Fields.DateTimeMode.Date).ToString().ToLower() %>); }); </script>