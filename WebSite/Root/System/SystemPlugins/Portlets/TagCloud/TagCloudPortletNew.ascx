<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="~/Controls/TagCloudPortlet.ascx.cs"
    Inherits="Portal.Portlets.Controls.TagCloudControl" %>

<style>
    #snCloudTags {
        display: none;
    }
    .sn-tag {
        color: #007dc2;
    }
</style>

<%var url = Portal.Virtualization.PortalContext.Current.RequestedUri.ToString(); %>
<script runat="server" type="text/C#">
    protected string GetCurrentUrl()
    {
        var url = Portal.Virtualization.PortalContext.Current.RequestedUri.ToString();
        var query = Portal.Virtualization.PortalContext.Current.RequestedUri.Query;
        if (!string.IsNullOrEmpty(query))
        {
            return url.Replace(query, "");
        }
        return url;
    }
</script>
<script>
    $(function () {
        var requestUrl = ' <%=url%>';
        var removeableString = window.location.protocol + '//' + window.location.host;
        requestUrl = requestUrl.replace(removeableString, '').trim();
        $('.sn-tag').on('click', function () {
            var tag = $(this).text().trim();
            var tableBody = '';
            var itemList = $.ajax({
                url: 'odata.svc' + '/Root' + '?$select=Path,DisplayName,Description,CreatedBy/FullName,CreationDate,ModificationDate&$expand=CreatedBy&query=Tags:*' + tag + '*&metadata=no',
                dataType: 'json',
                success: function (data) {
                    $.each(data.d.results, function (i, item) {
                        var desc = item.Description;
                        if (desc === null)
                            desc = '';
                        tableBody += '<tr><td><a href="' + item.Path + '">' + item.DisplayName + '</a></td><td>' + desc + '</td><td>' + item.CreatedBy.FullName + '</td><td>' + item.CreationDate + '</td><td>' + item.ModificationDate + '</td></tr>'
                    });
                    $('#snCloudTags tbody').html('');
                    $('#snCloudTags tbody').html(tableBody);
                    $('#snCloudTags').show();
                }
            });
        });
    });
</script>
<asp:Repeater ID="TagCloudRepeater" runat="server">
    <HeaderTemplate>
        <div class="sn-tags">
            <ul>
    </HeaderTemplate>
    <ItemTemplate>
        <li class="sn-tag sn-tag<%# Eval("Value") %>">
            <%# HttpUtility.UrlEncode(Eval("Key").ToString()) %></li>
    </ItemTemplate>
    <FooterTemplate>
        </ul> </div>
    </FooterTemplate>
</asp:Repeater>
<div class="sn-tags-table" id="snCloudTags">
    <table>
        <thead>
            <tr id="ctl00_wpm_TagSearchPortlet732414790_ctl00_TagSearchListView_Tr3">
                <th><%=GetGlobalResourceObject("TagAdmin", "DisplayName")%></th>        
                    <th><%=GetGlobalResourceObject("TagAdmin", "Description")%></th>
                    <th><%=GetGlobalResourceObject("TagAdmin", "CreatedBy")%></th>        
                    <th><%=GetGlobalResourceObject("TagAdmin", "CreationDate")%></th>
                    <th><%=GetGlobalResourceObject("TagAdmin", "ModificationDate")%></th>
            </tr>
        </thead>
        <tbody>

        </tbody>
    </table>
</div>
