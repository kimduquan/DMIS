
    <%@ Control Language="C#" AutoEventWireup="false" Inherits="Portal.UI.ContentListViews.ListView" %>
    <%@ Import Namespace="SNCR=ContentRepository" %>
    <%@ Import Namespace="Portal.UI.ContentListViews" %>
    <%@ Import Namespace="System.Linq" %>
    <%@ Import Namespace="SCR=ContentRepository.Fields" %>
    
    <sn:CssRequest ID="gallerycss" runat="server" CSSPath="$skin/styles/sn-gallery.css" />
    <sn:CssRequest ID="gallerycss2" runat="server" CSSPath="/Root/Global/styles/prettyPhoto.css" />
    <sn:ScriptRequest runat="server" Path="$skin/scripts/jquery/plugins/jquery.prettyPhoto.js" />
    
    
    <div class="galleryContainer">
    <sn:ListGrid ID="ViewBody"
                  DataSourceID="ViewDatasource"
                  runat="server">
      <LayoutTemplate>
        <table class="sn-datagrid sn-gallery-table">
          <tbody>
          <tr>
            <asp:TableCell runat="server" id="itemPlaceHolder" />
            
            </tr>
          </tbody>
        </table>
      </LayoutTemplate>
      <ItemTemplate>
        <asp:TableCell runat="server" class="sn-gallery-cell">
          <a href="<%# (((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder"))? Eval("Path") : Eval("Path") %>" rel="<%# (((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder"))? " " : "prettyphoto[pp_gal]" %>" class="<%# (((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder"))? "sn-folder-img" : "sn-gallery-img" %>" title="<%# (((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder"))? Eval("DisplayName") : Eval("DisplayName") %>"><img src='<%# (((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder"))? "/Root/Global/images/icons/folder-icon-125.jpg" : Eval("Path") + "?action=Thumbnail" %>' title="<%# (((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder"))? Eval("DisplayName") : Eval("DisplayName") %>" /></a>
        
        
          <div class="sn-title"><%# Eval("DisplayName") %></div>
        
      

        <div class="sn-modifiedby"><span>Uploaded by:</span><sn:ActionLinkButton ID="ModifiedBy" runat='server' NodePath='<%# ((SNCR.Content)Container.DataItem).ContentHandler.ModifiedBy.Path%>' ActionName='Profile' IconVisible="false"
    Text='<%# ((SNCR.User)((SNCR.Content)Container.DataItem).ContentHandler.ModifiedBy).FullName %>'
ToolTip='<%# ((SNCR.User)((SNCR.Content)Container.DataItem).ContentHandler.ModifiedBy).Domain + "/" + ((SNCR.Content)Container.DataItem).ContentHandler.ModifiedBy.Name %>'  />​</div>

      <%# Portal.UI.UITools.GetFriendlyDate(Container.DataItem as SNCR.Content, "ModificationDate") %></asp:TableCell>
      </ItemTemplate>
      <EmptyDataTemplate>
        <table class="sn-listgrid ui-widget-content">
          <thead>
          <asp:TableRow runat="server"></asp:TableRow>
          </thead>
        </table>
        <div class="sn-warning-msg ui-widget-content ui-state-default"><%=GetGlobalResourceObject("List", "EmptyList")%></div>
      </EmptyDataTemplate>
    </sn:ListGrid>
    </div>
    <asp:Literal runat="server" id="ViewScript" />
    <sn:DataSource ID="ViewDatasource" runat="server" />
     <script>
      $(function ()
      {
          var items = $(".galleryContainer td");
          var newlist = '';
          var actlist = '<tr>';
          var size = 5;
          var currentsize = 1;
          var endclosed = false;
          for (var i = 0; i < items.length; i++)
          {
              actlist += items[i].outerHTML;
              endclosed = false;
              if (++currentsize > size)
              {
                  currentsize = 1;
                  newlist += actlist + '</tr>';
                  actlist = '<tr>';
                  endclosed = true;
              }
          }
          if (!endclosed)
              newlist += actlist + '</tr>';

          $(".galleryContainer tbody").html(newlist);
      });
      $(document).ready(function ()
      {
          $(".sn-gallery-table a[rel^='prettyPhoto']").prettyPhoto({
              theme: 'facebook',
              deeplinking: false,
              overlay_gallery: false
          });
      });
    </script>
    
    
  