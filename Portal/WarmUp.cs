using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web.Compilation;
using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.Storage.Search;
using ContentRepository.Storage.Security;
using Diagnostics;
using Search;

namespace Portal
{
    public class WarmUp // : IProcessHostPreloadClient
    {
        //============================================================ Properties

        #region Type names for preload
        private static string[] _typesToPreloadByName = new[]
                                                            {
                                                               "ContentRepository.Storage.Events.NodeObserver",
"ContentRepository.Field",
"ContentRepository.Schema.FieldSetting",
"Search.Parser.LucQueryTemplateReplacer",
"ContentRepository.TemplateReplacerBase",
"Portal.PortletTemplateReplacer",
"Portal.UI.Controls.FieldControl",
"ContentRepository.Storage.ISnService",
"ContentRepository.Storage.Search.IIndexDocumentProvider",
"ContentRepository.Storage.Scripting.IEvaluator",
"ContentRepository.Security.UserAccessProvider",
"ContentRepository.Schema.ContentType",
"Search.Indexing.ExclusiveTypeIndexHandler",
"Search.Indexing.TypeTreeIndexHandler",
"Lucene.Net.Analysis.KeywordAnalyzer",
"Search.Indexing.DepthIndexHandler",
"Search.Indexing.InTreeIndexHandler",
"Search.Indexing.InFolderIndexHandler",
"Lucene.Net.Analysis.Standard.StandardAnalyzer",
"Search.Indexing.SystemContentIndexHandler",
"Search.Indexing.TagIndexHandler",
"ContentRepository.GenericContent",
"ApplicationModel.IncludeBackUrlMode",
"ApplicationModel.Application",
"Portal.Handlers.BackupIndexHandler",
"Portal.UI.Controls.Captcha.CaptchaImageApplication",
"Services.ExportToCsvApplication",
"ContentRepository.HttpEndpointDemoContent",
"Portal.AppModel.HttpStatusApplication",
"Portal.ApplicationModel.ImgResizeApplication",
"Services.RssApplication",
"Lucene.Net.Analysis.WhitespaceAnalyzer",
"Portal.Page",
"Portal.Handlers.XsltApplication",
"ContentRepository.ContentLink",
"ContentRepository.Schema.FieldSettingContent",
"ContentRepository.File",
"ContentRepository.Image",
"ContentRepository.ApplicationCacheFile",
"Portal.MasterPage",
"Portal.PageTemplate",
"ContentRepository.i18n.Resource",
"Portal.UI.ContentListViews.Handlers.ViewBase",
"Portal.UI.ContentListViews.Handlers.ListView",
"Workflow.WorkflowDefinitionHandler",
"ContentRepository.Folder",
"ContentRepository.Security.ADSync.ADFolder",
"ContentRepository.ContentList",
"Portal.Portlets.ContentHandlers.Form",
"ContentRepository.Survey",
"ContentRepository.Voting",
"ApplicationModel.Device",
"ContentRepository.Domain",
"ContentRepository.ExpenseClaim",
"ContentRepository.KPIDatasource",
"ContentRepository.OrganizationalUnit",
"ContentRepository.PortalRoot",
"ContentRepository.RuntimeContentContainer",
"ContentRepository.SmartFolder",
"ContentRepository.ContentRotator",
"ContentRepository.SystemFolder",
"ContentRepository.TrashBag",
"ContentRepository.Workspaces.Workspace",
"Portal.Site",
"ContentRepository.TrashBin",
"ContentRepository.UserProfile",
"ContentRepository.Group",
"Portal.BlogPost",
"ContentRepository.CalendarEvent",
"Portal.Portlets.ContentHandlers.FormItem",
"Portal.Portlets.ContentHandlers.EventRegistrationFormItem",
"Portal.DiscussionForum.ForumEntry",
"ContentRepository.SurveyItem",
"ContentRepository.Task",
"ContentRepository.VotingItem",
"Messaging.NotificationConfig",
"ContentRepository.User",
"Search.Indexing.WikiReferencedTitlesIndexHandler",
"Portal.WikiArticle",
"Workflow.WorkflowStatusEnum",
"Workflow.WorkflowHandlerBase",
"Workflow.ApprovalWorkflow",
"Workflow.RegistrationWorkflow",
"Portal.Workspaces.JournalNode",
"Workflow.InstanceManager",
"Portal.UI.PathTools",
"Portal.Portlets.ContentListPortlet",
"Portal.UI.Controls.DisplayName",
"Portal.UI.PortletFramework.DisplayName",
"Portal.UI.ContentListViews.DisplayName",
"Portal.UI.ContentListViews.FieldControls.DisplayName",
"Portal.UI.Controls.Name",
"Portal.UI.PortletFramework.Name",
"Portal.UI.ContentListViews.Name",
"Portal.UI.ContentListViews.FieldControls.Name",
"Portal.UI.Controls.RichText",
"Portal.UI.PortletFramework.RichText",
"Portal.UI.ContentListViews.RichText",
"Portal.UI.ContentListViews.FieldControls.RichText",
"ContentRepository.Schema.OutputMethod",
"ContentRepository.Schema.FieldVisibility",
"ContentRepository.Fields.TextType",
"ContentRepository.Fields.DateTimeMode",
"Portal.UI.Controls.ShortText",
"Portal.UI.PortletFramework.ShortText",
"Portal.UI.ContentListViews.ShortText",
"Portal.UI.ContentListViews.FieldControls.ShortText",
"ContentRepository.Fields.UrlFormat",
"Portal.UI.Controls.HyperLink",
"Portal.UI.PortletFramework.HyperLink",
"Portal.UI.ContentListViews.HyperLink",
"Portal.UI.ContentListViews.FieldControls.HyperLink",
"ContentRepository.Fields.DisplayChoice",
"Portal.UI.Controls.ColumnSelector",
"Portal.UI.PortletFramework.ColumnSelector",
"Portal.UI.ContentListViews.ColumnSelector",
"Portal.UI.ContentListViews.FieldControls.ColumnSelector",
"Portal.UI.Controls.SortingEditor",
"Portal.UI.PortletFramework.SortingEditor",
"Portal.UI.ContentListViews.SortingEditor",
"Portal.UI.ContentListViews.FieldControls.SortingEditor",
"Portal.UI.Controls.GroupingEditor",
"Portal.UI.PortletFramework.GroupingEditor",
"Portal.UI.ContentListViews.GroupingEditor",
"Portal.UI.ContentListViews.FieldControls.GroupingEditor",
"Portal.UI.Controls.VersioningModeChoice",
"Portal.UI.PortletFramework.VersioningModeChoice",
"Portal.UI.ContentListViews.VersioningModeChoice",
"Portal.UI.ContentListViews.FieldControls.VersioningModeChoice",
"Portal.UI.Controls.ApprovingModeChoice",
"Portal.UI.PortletFramework.ApprovingModeChoice",
"Portal.UI.ContentListViews.ApprovingModeChoice",
"Portal.UI.ContentListViews.FieldControls.ApprovingModeChoice",
"Portal.UI.Controls.SiteRelativeUrl",
"Portal.UI.PortletFramework.SiteRelativeUrl",
"Portal.UI.ContentListViews.SiteRelativeUrl",
"Portal.UI.ContentListViews.FieldControls.SiteRelativeUrl",
"Portal.Portlets.SingleContentPortlet",
"Portal.UI.ContentListViews.ListHelper",
"Portal.UI.Controls.EducationEditor",
"Portal.UI.PortletFramework.EducationEditor",
"Portal.UI.ContentListViews.EducationEditor",
"Portal.UI.ContentListViews.FieldControls.EducationEditor"
                                                            };

        private static string[] _typesToPreloadByBase = new[]
                                                            {
"ContentRepository.Storage.Events.NodeObserver",
"ContentRepository.Field",
"ContentRepository.Schema.FieldSetting",
"Search.Parser.LucQueryTemplateReplacer",
"ContentRepository.TemplateReplacerBase",
"Portal.PortletTemplateReplacer",
"Portal.UI.Controls.FieldControl" 
                                                            };

        private static string[] _typesToPreloadByInterface = new[]
                                                            {
"ContentRepository.Storage.ISnService",
"ContentRepository.Storage.Search.IIndexDocumentProvider",
"ContentRepository.Storage.Scripting.IEvaluator"
                                                            };

        #endregion

        private static IEnumerable<string> TypesToPreloadByName
        {
            get { return _typesToPreloadByName; }
        }

        private static IEnumerable<string> TypesToPreloadByBase
        {
            get { return _typesToPreloadByBase; }
        }

        private static IEnumerable<string> TypesToPreloadByInterface
        {
            get { return _typesToPreloadByInterface; }
        }

        //============================================================ Interface

        public static void Preload()
        {
            if (!Repository.WarmupEnabled)
            {
                Logger.WriteInformation(Logger.EventId.NotDefined, "***** Warmup is not enabled, skipped.");
                return;
            }

            //types
            ThreadPool.QueueUserWorkItem(delegate { PreloadTypes(); });
            
            //template replacers and resolvers
            ThreadPool.QueueUserWorkItem(delegate { TemplateManager.Init(); });
            ThreadPool.QueueUserWorkItem(delegate { NodeQuery.InitTemplateResolvers(); });

            //jscript evaluator
            ThreadPool.QueueUserWorkItem(delegate { JscriptEvaluator.Init(); });

            //xslt
            ThreadPool.QueueUserWorkItem(delegate { PreloadXslt(); });

            //content templates
            ThreadPool.QueueUserWorkItem(delegate { PreloadContentTemplates(); });

            //preload controls
            ThreadPool.QueueUserWorkItem(delegate { PreloadControls(); });

            //preload security items
            ThreadPool.QueueUserWorkItem(delegate { PreloadSecurity(); });
        }

        //============================================================ Helper methods

        private static void PreloadTypes()
        {
            using (var optrace = new OperationTrace("PreloadTypes"))
            {
                try
                {
                    //preload types by name
                    foreach (var typeName in TypesToPreloadByName)
                    {
                        TypeHandler.GetType(typeName);
                    }

                    //preload types by base
                    foreach (var typeName in TypesToPreloadByBase)
                    {
                        TypeHandler.GetTypesByBaseType(TypeHandler.GetType(typeName));
                    }

                    //preload types by interface
                    foreach (var typeName in TypesToPreloadByInterface)
                    {
                        TypeHandler.GetTypesByInterface(TypeHandler.GetType(typeName));
                    }

                    optrace.IsSuccessful = true;
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                }
            }
        }

        private static void PreloadControls()
        {
            try
            {
                QueryResult controlResult;
                var cc = 0;

                var timer = new Stopwatch();
                timer.Start();

                using (new SystemAccount())
                {
                    var query = ContentQuery.CreateQuery(SafeQueries.PreloadControls);
                    if (!string.IsNullOrEmpty(Repository.WarmupControlQueryFilter))
                        query.AddClause(Repository.WarmupControlQueryFilter);

                    controlResult = query.Execute();

                    foreach (var controlId in controlResult.Identifiers)
                    {
                        var head = NodeHead.Get(controlId);
                        try
                        {
                            if (head != null)
                            {
                                var pct = BuildManager.GetCompiledType(head.Path);

                                //if (pct != null)
                                //    Trace.WriteLine(">>>>Precompiled control: " + pct.FullName);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteException(new Exception("Error during control load: " + (head == null ? controlId.ToString() : head.Path), ex));
                            //Trace.WriteLine(">>>>Precompiled error during control load: " + (head == null ? controlId.ToString() : head.Path) + " ERROR: " + ex);
                        }

                        cc++;
                    }
                }

                timer.Stop();

                Logger.WriteInformation(Logger.EventId.NotDefined, string.Format("***** Control preload time: {0} ******* Count: {1} ({2})", timer.Elapsed, cc, controlResult.Count));
                //Trace.WriteLine(string.Format(">>>>Precompiled preload time: {0} ******* Count: {1} ({2})", timer.Elapsed, cc, controlResult.Count));
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }
        }

        private static void PreloadXslt()
        {
            try
            {
                QueryResult queryResult;
                var cc = 0;

                var timer = new Stopwatch();
                timer.Start();

                using (new SystemAccount())
                {
                    queryResult = ContentQuery.Query(SafeQueries.PreloadXslt);

                    foreach (var nodeId in queryResult.Identifiers)
                    {
                        var head = NodeHead.Get(nodeId);
                        try
                        {
                            if (head != null)
                            {
                                var xslt = UI.PortletFramework.Xslt.GetXslt(head.Path, true);
                                //Trace.WriteLine(">>>>Preload (xslt): " + head.Path);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteException(new Exception("Error during xlst load: " + (head == null ? nodeId.ToString() : head.Path), ex));
                            //Trace.WriteLine(">>>>Precompiled error during control load: " + (head == null ? nodeId.ToString() : head.Path) + " ERROR: " + ex);
                        }

                        cc++;
                    }
                }

                timer.Stop();

                Logger.WriteInformation(Logger.EventId.NotDefined, string.Format("***** XSLT preload time: {0} ******* Count: {1} ({2})", timer.Elapsed, cc, queryResult.Count));
                //Trace.WriteLine(string.Format(">>>>Preload XSLT preload time: {0} ******* Count: {1} ({2})", timer.Elapsed, cc, queryResult.Count));
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }
        }

        private static void PreloadContentTemplates()
        {
            try
            {
                QueryResult queryResult;

                var timer = new Stopwatch();
                timer.Start();

                using (new SystemAccount())
                {
                    queryResult = ContentQuery.Query(SafeQueries.PreloadContentTemplates, null,
                        Repository.ContentTemplateFolderPath, RepositoryPath.GetDepth(Repository.ContentTemplateFolderPath) + 2);

                    var templates = queryResult.Nodes.ToList();
                }

                timer.Stop();

                Logger.WriteInformation(Logger.EventId.NotDefined, string.Format("***** Content template preload time: {0} ******* Count: {1}", timer.Elapsed, queryResult.Count));
                //Trace.WriteLine(string.Format(">>>>Preload: Content template preload time: {0} ******* Count: {1}", timer.Elapsed, queryResult.Count));
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }
        }

        private static void PreloadSecurity()
        {
            try
            {
                var timer = new Stopwatch();
                timer.Start();

                //preload special groups
                var g1 = Group.Everyone;
                var g2 = Group.Administrators;
                var g3 = Group.LastModifiers;
                var g4 = Group.Creators;

                timer.Stop();

                Logger.WriteInformation(Logger.EventId.NotDefined, string.Format("***** Security preload time: {0} *******", timer.Elapsed));
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }
        }
    }
}
