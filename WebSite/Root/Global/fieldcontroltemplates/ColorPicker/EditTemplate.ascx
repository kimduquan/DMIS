<%@  Language="C#" %>
<%@ Import Namespace="SNControls=Portal.UI.Controls" %>
<%@ Import Namespace="SNFields=ContentRepository.Fields" %>
<sn:ScriptRequest runat="server" Path="/Root/Global/scripts/kendoui/kendo.web.min.js" />
<sn:CssRequest runat="server" CSSPath="/Root/Global/styles/kendoui/kendo.common.min.css" />
<sn:CssRequest runat="server" CSSPath="/Root/Global/styles/kendoui/kendo.metro.min.css" />
<asp:TextBox ID="InnerShortText" CssClass="sn-ctrl sn-ctrl-text sn-ctrl-colorpicker" runat="server"></asp:TextBox>
<asp:HiddenField ID="paletteField" Value='<%# ((SNFields.ColorFieldSetting)((SNControls.ColorPicker)Container).Field.FieldSetting).Palette %>' runat="server" />
    <div id="colorPalette"></div>
<script>
    $(function () {
        var colors = $('.sn-ctrl-colorpicker').next('input').val().split(';');
        $colorPicker = $(".sn-ctrl-colorpicker");
        var p = [];
        if (colors.length > 1) {
            $.each(colors, function (i, item) {
                p.push(item);
            });
            $("#colorPalette").kendoColorPalette({
                columns: 12,
                palette: p,
                change: select
            });
        }
        else {
            $colorPicker.kendoColorPicker({
                buttons: false
            });
        }
        function select(e) {
            $colorPicker.val(e.value);
        }
    });
</script>