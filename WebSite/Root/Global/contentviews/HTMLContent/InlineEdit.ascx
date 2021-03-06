<%@ Language="C#" AutoEventWireup="true" Inherits="Portal.UI.SingleContentView" %>

<div id="InlineViewContent" class="sn-content sn-content-inlineview">

    <sn:LongText ID="HTMLFragment" runat="server" FieldName="HTMLFragment" FullScreenText="true" FrameMode="NoFrame"/>

</div>

<div id="InlineViewProperties" class="sn-content-meta">
        <sn:ShortText ID="Name" runat="server" RenderMode="Edit" FieldName="Name" />
        <sn:ShortText ID="Path" runat="server" RenderMode="Edit" FieldName="Path" ReadOnly="true" />
        <sn:Version id="Version1" runat="server" fieldname="Version" />
        <sn:WholeNumber ID="Index" runat="server" RenderMode="Edit" FieldName="Index" />
        <sn:DatePicker ID="ValidFrom1" runat="server" RenderMode="Edit" FieldName="ValidFrom" />
        <sn:DatePicker ID="ReviewDate1" runat="server" RenderMode="Edit" FieldName="ReviewDate" />
        <sn:DatePicker ID="ArchiveDate1" runat="server" RenderMode="Edit" FieldName="ArchiveDate" />
        <sn:DatePicker ID="ValidTill1" runat="server" RenderMode="Edit" FieldName="ValidTill" />
</div>

<div class="sn-panel sn-buttons">
  <sn:CommandButtons ID="CommandButtons1" runat="server" HideButtons="Save CheckoutSave" />
</div>
