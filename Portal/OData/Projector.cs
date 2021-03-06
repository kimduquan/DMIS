using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Portal.Virtualization;
using ContentRepository;
using ApplicationModel;

namespace Portal.OData
{
    internal abstract class Projector
    {
        protected const string ACTIONSPROPERTY = "Actions";
        protected const string ICONPROPERTY = "Icon";
        protected const string ISFILEPROPERTY = "IsFile";

        protected ODataRequest Request { get; private set; }
        protected bool IsCollectionItem { get; private set; }

        internal static Projector Create(ODataRequest request, bool isCollectionItem, Content container = null)
        {
            Projector prj;
            if (request.HasExpand)
                if (request.HasSelect)
                    prj = new ExpanderProjector();
                else
                    prj = new SimpleExpanderProjector();
            else
                prj = new SimpleProjector();
            prj.Request = request;
            prj.IsCollectionItem = isCollectionItem;
            prj.Initialize(container);
            return prj;
        }
        internal abstract void Initialize(Content container);
        internal abstract Dictionary<string, object> Project(Content content);

        protected string GetSelfUrl(Content content)
        {
            //return string.Concat("/", ODataRequest.ODATA_SVC, ODataHandler.GetEntityUrl(PortalContext.GetSiteRelativePath(content.Path)));
            return string.Concat("/", ODataRequest.ODATA_SVC, ODataHandler.GetEntityUrl(content.Path));
        }
        protected ODataSimpleMeta GetMetadata(Content content, string selfurl, MetadataFormat format)
        {
            if (format == MetadataFormat.Minimal)
            {
                return new ODataSimpleMeta
                {
                    Uri = selfurl,
                    Type = content.ContentType.Name,
                };
            }

            var snActions = GetSnActions(content);
            //return new ODataFullMeta
            //{
            //    Uri = selfurl,
            //    Type = content.ContentType.Name,
            //    Actions = snActions.Where(a => a.CausesStateChange && a.IsODataOperation).Select(a => new ODataOperation
            //    {
            //        Title = SNSR.GetString(a.Text),
            //        Name = a.Name,
            //        Target = string.Concat(selfurl, "/", a.Name),
            //        Forbidden = a.Forbidden
            //    }).OrderBy(x => x.Title).ToArray(),
            //    Functions = snActions.Where(a => !a.CausesStateChange && a.IsODataOperation).Select(a => new ODataOperation
            //    {
            //        Title = SNSR.GetString(a.Text),
            //        Name = a.Name,
            //        Target = string.Concat(selfurl, "/", a.Name),
            //        Forbidden = a.Forbidden
            //    }).OrderBy(x => x.Title).ToArray(),
            //};
            return new ODataFullMeta
            {
                Uri = selfurl,
                Type = content.ContentType.Name,
                Actions = snActions.Where(a => a.CausesStateChange && a.IsODataOperation).Select(a => CreateOdataOperation(a, selfurl)).OrderBy(x => x.Title).ToArray(),
                Functions = snActions.Where(a => !a.CausesStateChange && a.IsODataOperation).Select(a => CreateOdataOperation(a, selfurl)).OrderBy(x => x.Title).ToArray(),
            };
        }
        private ODataOperation CreateOdataOperation(ActionBase a, string selfUrl)
        {
            return new ODataOperation
            {
                Title = SNSR.GetString(a.Text),
                Name = a.Name,
                Target = string.Concat(selfUrl, "/", a.Name),
                Forbidden = a.Forbidden,
                Parameters = a.ActionParameters.Select(p => new ODataOperationParameter
                    {
                        Name = p.Name,
                        Type = ResolveODataParameterType(p.Type),
                        Required = p.Required 
                    }).ToArray()
            };
        }
        private string ResolveODataParameterType(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int))
                return "int";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(DateTime))
                return "dateTime";

            if (type == typeof(string[]))
                return "string[]";
            if (type == typeof(int[]))
                return "int[]";

            return type.FullName;
        }

        private IEnumerable<ActionBase> GetSnActions(Content content)
        {
            //Get actions without back url: let the client append the back parameter,
            //as we are in a service here that does not know about the redirect url.
            return ODataHandler.ActionResolver.GetActions(content, this.Request.Scenario, null);
        }

        protected bool IsAllowedField(Content content, string fieldName)
        {
            switch (fieldName)
            {
                case ACTIONSPROPERTY:
                case ICONPROPERTY:
                case ISFILEPROPERTY:
                    return true;
                default:
                    return content.IsAllowedField(fieldName);
            }
        }

        protected ODataActionItem[] GetActions(Content content)
        {
            var snActions = GetSnActions(content);

            var actions = snActions.Where(a => a.IsHtmlOperation).Select(a => new ODataActionItem
            {
                Name = a.Name,
                DisplayName = SNSR.GetString(a.Text),
                Icon = a.Icon,
                Index = a.Index,
                Url = a.Uri,
                IncludeBackUrl = a.GetApplication() == null ? 0 : (int)a.GetApplication().IncludeBackUrl,
                ClientAction = a is ClientAction && !string.IsNullOrEmpty(((ClientAction)a).Callback),
                Forbidden = a.Forbidden
            });
            return actions.ToArray();
        }
    }
}
