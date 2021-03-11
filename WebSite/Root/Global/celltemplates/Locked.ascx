﻿<sn:ActionLinkButton ID="ActionLinkButton1" runat='server' NodePath='<%# Eval("Path") %>' ActionName='checkin' Tooltip='<%$ Resources: Portal, CheckIn %>' />    
<sn:ActionLinkButton ID="ActionLinkButton2" runat='server' NodePath='<%# Eval("Path") %>' ActionName='checkout' Tooltip='<%$ Resources: Portal, CheckOut %>' />    
<sn:ActionLinkButton ID="ActionLinkButton3" runat='server' NodePath='<%# Eval("Path") %>' ActionName='undocheckout' Tooltip='<%$ Resources: Portal, UndoCheckOut %>' />
<sn:ActionLinkButton ID="ActionLinkButton4" runat='server' NodePath='<%# Eval("Path") %>' ActionName='forceundocheckout' Tooltip='<%# ((SNCR.Content)Container.DataItem).ContentHandler.Locked ? HttpContext.GetGlobalResourceObject("Portal", "ForceUndo") + ((SNCR.Content)Container.DataItem).ContentHandler.LockedBy.Name : string.Empty %>' />
<asp:PlaceHolder runat="server" id="plcLocked" Visible="<%# ((SNCR.Content)Container.DataItem).ContentHandler.Locked && !SNCR.SavingAction.HasUndoCheckOut(((SNCR.Content)Container.DataItem).ContentHandler as SNCR.GenericContent) && !SNCR.SavingAction.HasForceUndoCheckOutRight(((SNCR.Content)Container.DataItem).ContentHandler as SNCR.GenericContent) %>">
<span disabled="disabled" class="sn-actionlinkbutton sn-disabled"><img class="sn-icon sn-icon16" title="<%# ((SNCR.Content)Container.DataItem).ContentHandler.Locked ? HttpContext.GetGlobalResourceObject("Portal", "LockedBy") + ((SNCR.Content)Container.DataItem).ContentHandler.LockedBy.Name : string.Empty %>" src="/Root/Global/images/icons/16/checkin.png"></span>
</asp:PlaceHolder>