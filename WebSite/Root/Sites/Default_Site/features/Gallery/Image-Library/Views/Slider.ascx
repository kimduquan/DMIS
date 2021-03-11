﻿
    <%@ Control Language="C#" AutoEventWireup="false" Inherits="Portal.UI.ContentListViews.ListView" %>
    <%@ Import Namespace="SNCR=ContentRepository" %>
    <%@ Import Namespace="Portal.UI.ContentListViews" %>
    <%@ Import Namespace="System.Linq" %>
    <%@ Import Namespace="SCR=ContentRepository.Fields" %>
    
    <sn:CssRequest ID="gallerycss" runat="server" CSSPath="/Root/Global/styles/slides.css" />
     <sn:CssRequest ID="gallerycss2" runat="server" CSSPath="/Root/Global/styles/prettyPhoto.css" />
    <sn:ScriptRequest runat="server" Path="/Root/Global/scripts/jquery/plugins/jquery.easing.1.3.js" />
    <sn:ScriptRequest runat="server" Path="/Root/Global/scripts/jquery/plugins/slides.min.jquery.js" />
    <sn:ScriptRequest runat="server" Path="$skin/scripts/jquery/plugins/jquery.prettyPhoto.js" />
    
    <script>
        $(function ()
        {
            $('#slides').slides({
                preload: true,
                preloadImage: '/Root/Global/images/slides/loading.gif',
                play: 5000,
                pause: 2500,
                autoHeight: true,
                hoverPause: true
            });
        });
        $(document).ready(function ()
        {
            $(".slides_container a[rel^='prettyPhoto']").prettyPhoto({
                theme: 'facebook',
                deeplinking: false,
                overlay_gallery: false
            });
        });
    </script>
    <script runat="server">
        string[] extensions = new string[] { ".jpg", ".jpeg", ".png", ".gif" };
    </script>
    <div id="slides">
        
                 <div class="slides_container">
     
    <asp:ListView ID="ViewBody"
                  DataSourceID="ViewDatasource"
                  runat="server">
      

      <LayoutTemplate>      
      
          <div runat="server" id="itemPlaceHolder" />
          
          
   
    </LayoutTemplate>
    <ItemTemplate>
                <div class='<%# !(((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder"))? "cr-content-container" : "sn-hide" %>'>
          <a href="<%# Eval("Path") %>" 
          rel="<%# !(((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder")) && extensions.Contains(System.IO.Path.GetExtension(((SNCR.Content)Container.DataItem).Name)) || !(((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder")) && System.IO.Path.GetExtension(((SNCR.Content)Container.DataItem).Name) == ".svg"? "prettyphoto[pp_gal]" : " " %>" 
          class="<%# (((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder"))? "sn-folder-img" : "sn-gallery-img" %>" 
          title="<%# (((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder"))? Eval("DisplayName") : Eval("DisplayName") %>">
                <asp:Image ID="Folder" Visible='<%# (((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder"))%>' runat="server" ImageUrl="/Root/Global/images/icons/folder-icon-125.jpg" AlternateText='<%# Eval("DisplayName") %>' ToolTip='<%# Eval("DisplayName") %>'/>
                <asp:Image ID="RegularImage" Visible='<%# !(((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder")) && extensions.Contains(System.IO.Path.GetExtension(((SNCR.Content)Container.DataItem).Name)) %>' runat="server" ImageUrl='<%# Eval("Path") %>' AlternateText='<%# Eval("DisplayName") %>' ToolTip='<%# Eval("DisplayName") %>'/>
                <asp:Image ID="SvgImage" Visible='<%# !(((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder")) && System.IO.Path.GetExtension(((SNCR.Content)Container.DataItem).Name) == ".svg" %>' runat="server" ImageUrl='<%# Eval("Path") %>' AlternateText='<%# Eval("DisplayName") %>' CssClass="svgimage" ToolTip='<%# Eval("DisplayName") %>'/>
              <asp:Image ID="OtherImage" 
              Visible='<%# !(((SNCR.Content)Container.DataItem).ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("Folder")) && !extensions.Contains(System.IO.Path.GetExtension(((SNCR.Content)Container.DataItem).Name)) && System.IO.Path.GetExtension(((SNCR.Content)Container.DataItem).Name) != ".svg" %>' CssClass="other" runat="server" 
              ImageUrl="/Root/Global/images/icons/image-icon-125.jpg" AlternateText='<%# Eval("DisplayName") %>' 
              ToolTip='<%# Eval("DisplayName") %>'/>


          </a>

          </div>
    </ItemTemplate>
    <EmptyDataTemplate>
    </EmptyDataTemplate>

  
      
      
    </asp:ListView>
    
          </div>
          <span class="prev" style="cursor:pointer;"><img src="/Root/Global/images/slides/arrow-prev.png" width="24" height="43" alt="<%=GetGlobalResourceObject("ContentView", "ArrowPrev")%>"></span>
        <span class="next" style="cursor:pointer;"><img src="/Root/Global/images/slides/arrow-next.png" width="24" height="43" alt="<%=GetGlobalResourceObject("ContentView", "ArrowNext")%>"></span>          
          <div class="cr-thumbs">
          </div>
    </div>
    <asp:Literal runat="server" id="ViewScript" />
    <sn:DataSource ID="ViewDatasource" runat="server" />
  