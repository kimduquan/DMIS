﻿<%@ Page Language="C#" CompilationMode="Never" MasterPageFile="~/Root/Global/PageTemplates/sn-layout-inter-index.Master" %><asp:Content ID="Content_HeaderZone" ContentPlaceHolderID="CPHeaderZone" runat="server"><asp:WebPartZone ID="HeaderZone" name="HeaderZone" headertext="HeaderZone" partchrometype="None"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_Full" ContentPlaceHolderID="CPFull" runat="server"><asp:WebPartZone ID="Full" name="Full" headertext="Full" partchrometype="None"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_FullCol" ContentPlaceHolderID="CPFullCol" runat="server"><asp:WebPartZone ID="FullCol" name="FullCol" headertext="Full Column" partchrometype="None"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_LeftCol" ContentPlaceHolderID="CPLeftCol" runat="server"><asp:WebPartZone ID="LeftCol" name="LeftCol" headertext="Left Column" partchrometype="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_WideCol" ContentPlaceHolderID="CPWideCol" runat="server"><asp:WebPartZone ID="WideCol" name="WideCol" headertext="Wide Column" partchrometype="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_CenterCol" ContentPlaceHolderID="CPCenterCol" runat="server"><asp:WebPartZone ID="CenterCol" name="CenterCol" headertext="Center Column" partchrometype="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_CenterLeftCol" ContentPlaceHolderID="CPCenterLeftCol" runat="server"><asp:WebPartZone ID="CenterLeftCol" name="CenterLeftCol" headertext="Center / Left Column" partchrometype="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_CenterRightCol" ContentPlaceHolderID="CPCenterRightCol" runat="server"><asp:WebPartZone ID="CenterRightCol" name="CenterRightCol" headertext="Center / Right Column" partchrometype="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_RightCol" ContentPlaceHolderID="CPRightCol" runat="server"><asp:WebPartZone ID="RightCol" name="RightCol" headertext="Right Column" partchrometype="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_FooterLeft" ContentPlaceHolderID="CPFooterLeft" runat="server"><asp:WebPartZone ID="FooterLeft" name="FooterLeft" headertext="Footer" partchrometype="None"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_Footer" ContentPlaceHolderID="CPFooter" runat="server"><asp:WebPartZone ID="Footer" name="Footer" headertext="FooterRight" partchrometype="None"  runat="server"><ZoneTemplate><snpe:ContentCollectionPortlet Title="FooterMenu1" ChromeType="None" BindTarget="CustomRoot" CustomRootPath="/Root/Sites/Default_Site/workspaces" Renderer="/Root/Global/renderers/sitemapMenu.xslt" ID="FooterMenu1" runat="server" SkinPreFix="footer-ws" /><snpe:ContentCollectionPortlet Title="FooterMenu2" ChromeType="None" BindTarget="CustomRoot" CustomRootPath="/Root/Sites/Default_Site/NewsDemo" Renderer="/Root/Global/renderers/sitemapMenu.xslt" ID="FooterMenu2" runat="server" SkinPreFix="footer-newsdemo" /><snpe:ContentCollectionPortlet Title="FooterMenu3" ChromeType="None" BindTarget="CustomRoot" CustomRootPath="/Root/Sites/Default_Site/features" Renderer="/Root/Global/renderers/sitemapMenu.xslt" ID="FooterMenu3" runat="server" SkinPreFix="footer-features" /></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_FooterLink" ContentPlaceHolderID="CPFooterLink" runat="server"><asp:WebPartZone ID="FooterLink" name="FooterLink" headertext="FooterLinks" partchrometype="None"  runat="server"><ZoneTemplate><snpe:SingleContentPortlet UsedContentTypeName="HTMLContent" ContentPath="/Root/YourContents/footer-links" ID="FooterContent1" runat="server" CssClass="usefulLinks" /></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_Version" ContentPlaceHolderID="CPVersion" runat="server"><asp:WebPartZone ID="Version" name="Version" headertext="Version" partchrometype="None"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content>