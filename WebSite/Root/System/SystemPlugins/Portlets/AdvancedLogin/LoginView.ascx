<%@ Control Language="C#" AutoEventWireup="true" Inherits="Portal.Portlets.Controls.LoginView" %>
<%@ Register Src="~/Root/System/SystemPlugins/Portlets/AdvancedLogin/LoginDemo.ascx" TagPrefix="sn" TagName="LoginDemo" %>

<sn:ContextInfo runat="server" Selector="CurrentUser" UsePortletContext="false" ID="myContext" />
<style>
    .sn-login-demo 
    {
        background: none;
        box-shadow: none;
        margin-top: 10px;
        }
    .sn-link 
    {
        display: block;
        }
</style>
<asp:LoginView ID="LoginViewControl" runat="server">
    <AnonymousTemplate>
         <asp:Login ID="LoginControl" Width="100%" runat="server" DisplayRememberMe="false" RememberMeSet="false" FailureText='<%$ Resources:LoginPortlet, FailureText %>'>
            <LayoutTemplate>
                <asp:Panel DefaultButton="Login" runat="server">
                    <div class="sn-login">
                        <div class="sn-login-text"><%= HttpContext.GetGlobalResourceObject("LoginPortlet","LoginText") %></div>
                        <asp:Label AssociatedControlID="UserName" CssClass="sn-iu-label" ID="UsernameLabel" runat="server" Text="<%$ Resources:LoginPortlet, UsernameLabel %>"></asp:Label> 
                        <asp:TextBox ID="UserName" CssClass="sn-ctrl sn-login-username" runat="server"></asp:TextBox><br />                
                        <asp:Label AssociatedControlID="Password" CssClass="sn-iu-label" ID="PasswordLabel" runat="server" Text="<%$ Resources:LoginPortlet, PasswordLabel %>"></asp:Label> 
                        <asp:TextBox ID="Password" CssClass="sn-ctrl sn-login-password" runat="server" TextMode="Password"></asp:TextBox><br />
                        <%-- <asp:CheckBox ID="RememberMe" runat="server" Text='<%$ Resources:LoginPortlet,RememberMe %>'></asp:CheckBox> --%>
                        <asp:Button ID="Login" CssClass="sn-submit" CommandName="Login" runat="server" Text='<%$ Resources:LoginPortlet,LoginButtonTitle %>'></asp:Button>&#160;
                        
                        <div class="sn-login-links">
							<strong><a class="sn-link sn-link-forgotpass" href="/login/forgottenpassword"><%= HttpContext.GetGlobalResourceObject("LoginPortlet","ForgotPass") %></a></strong>
							<br />
						</div>

                        <div class="sn-error-msg">
                            <asp:Label ID="FailureText" runat="server"></asp:Label>
                        </div>
                    </div>
                 </asp:Panel>
            </LayoutTemplate>
        </asp:Login>
    </AnonymousTemplate>
    <LoggedInTemplate>
        <div class="sn-loggedin">
            <%= HttpContext.GetGlobalResourceObject("LoginPortlet","LoggedIn") %>
            <div class="sn-panel">
                <div class="sn-avatar sn-floatleft"><img class="sn-icon sn-icon32" src="<%= Portal.UI.UITools.GetAvatarUrl(32, 32) %>" alt="" title="<%= ContentRepository.User.Current.FullName %>" /></div>
                <strong><%= ContentRepository.User.Current.FullName %></strong><br />
                <sn:ActionLinkButton ID="ProfileLink" IconVisible="false" runat="server" ActionName="Profile" Text="<%$ Resources:Action,Profile %>" ContextInfoID="myContext" /> 
            </div>
            <hr />
            <asp:LoginStatus ID="LoginStatusControl" LogoutText="<%$ Resources:LoginPortlet,Logout %>" LogoutPageUrl="/" LogoutAction="Redirect" runat="server" CssClass="sn-link sn-logout" />
        </div>
    </LoggedInTemplate>
</asp:LoginView>