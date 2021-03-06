using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.Storage.Security;
using Portal.UI.ContentListViews;

namespace ApplicationModel
{
    public class ManageViewsAction : UrlAction
    {
        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            if (context == null)
            {
                this.Visible = false;
                return;
            }

            //if the Views folder does not exist, we have to create it
            //(but only in case of a content list)
            var cl = ContentList.GetContentListByParentWalk(context.ContentHandler);
            if (cl != null)
            {
                var viewsFolderPath = RepositoryPath.Combine(cl.Path, ViewManager.VIEWSFOLDERNAME);
                if (!Node.Exists(viewsFolderPath))
                {
                    using (new SystemAccount())
                    {
                        Tools.CreateStructure(viewsFolderPath, "SystemFolder");
                    }
                }
            }
        }
    }
}
