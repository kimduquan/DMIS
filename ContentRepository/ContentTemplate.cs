using System;
using System.Collections.Generic;
using System.Linq;
using Preview;
using ContentRepository.Storage;
using ContentRepository.Storage.Search;
using ContentRepository.Storage.Security;
using ContentRepository.Versioning;
using Diagnostics;
using Search;
using ContentRepository.Storage.Schema;
using ContentRepository.Fields;

namespace ContentRepository
{
    public sealed class ContentTemplate
    {
        public static readonly string NOTIFOBSERVERNAME = "Messaging.NotificationObserver";
        public static readonly string WFOBSERVERNAME = "Workflow.WorkflowNotificationObserver";

        /// <summary>
        /// Gets a template instance node for the specified contenttype name. 
        /// </summary>
        /// <param name="contentTypeName">ContentTypeName that is searched for </param>
        /// <returns>First element of the list of available content templates.</returns>
        public static Node GetTemplate(string contentTypeName)
        {

            //
            //  TODO: must be extended for returning the Default template (if other templates exist) and not the first.
            //

            if (contentTypeName == null) throw new ArgumentNullException("contentTypeName");

            var templates = GetTemplatesForType<Node>(contentTypeName);

            return templates != null ? templates.FirstOrDefault() : null;
        }

        /// <summary>
        /// Returns a list of available content templates for the specified contenttype under the Global Templates folder.
        /// </summary>
        /// <typeparam name="T">Type that tells which types will be returned.</typeparam>
        /// <param name="contentTypeName">ContentTypeName that is searched for.</param>
        /// <returns>An IEnumerable list with the given types.</returns>
        public static IEnumerable<T> GetTemplatesForType<T>(string contentTypeName) where T : Node
        {
            return GetTemplatesForType<T>(contentTypeName, Repository.ContentTemplateFolderPath);
        }
        /// <summary>
        /// Returns a list of available content templates for the specified contenttype under a given repository path.
        /// </summary>
        /// <typeparam name="T">Type that tells which types will be returned.</typeparam>
        /// <param name="contentTypeName">ContentTypeName that is searched for.</param>
        /// <param name="contextPath">A repository where templates are searched.</param>
        /// <returns>An IEnumerable list with the given types.</returns>
        public static IEnumerable<T> GetTemplatesForType<T>(string contentTypeName, string contextPath) where T : Node
        {
            var path = RepositoryPath.Combine(contextPath, contentTypeName);
            if (RepositoryInstance.ContentQueryIsAllowed)
            {
                return
                    ContentQuery.Query(SafeQueries.InFolder,
                        new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled },
                        path).Nodes.Where(ct => ct is T).Cast<T>();
            }
            else
            {
                return NodeQuery.QueryChildren(path).Nodes.Where(ct => ct is T).Cast<T>();
            }
        }

        /// <summary>
        /// Creates a template with the specified parameter based upon the contenttypename and
        /// </summary>
        /// <param name="contentTypeName">Specifies a CTD name.</param>
        /// <param name="templateName">Specified a template name for the CTD.</param>
        /// <param name="targetPath">Specifies a path of the target location where the template is copied to.</param>
        /// <returns></returns>
        public static Content CreateTemplated(string contentTypeName, string templateName, string targetPath)
        {
            if (String.IsNullOrEmpty(contentTypeName)) throw new ArgumentException("contentTypeName");
            if (String.IsNullOrEmpty(templateName)) throw new ArgumentException("templateName");
            if (String.IsNullOrEmpty(targetPath)) throw new ArgumentException("targetPath");

            var targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new InvalidOperationException(String.Format("{0} node couldn't be loaded.", targetPath));

            var templatesForContentType = GetTemplatesForType<Node>(contentTypeName);
            var template = from t in templatesForContentType
                           where t.Name.Equals(templateName)
                           select t;
            if (template.Count() < 1)
                throw new InvalidOperationException(String.Format("There is no template with {0} name for {1} contentType.", templateName, contentTypeName));

            var templateNode = template.FirstOrDefault();

            return ContentTemplate.CreateTemplated(targetNode, templateNode, null);
        }

        /// <summary>
        /// Copies the contents of the Template node which is specified in the Template property of the given content.
        /// </summary>
        /// <param name="content">Source content which will be processed.</param>
        /// <exception cref="InvalidOperationException">Thrown when a Template isn't specified.</exception>
        public static void CopyContents(Content content)
        {
            if (content == null) throw new ArgumentNullException("content");
            CopyChildren(content);
        }

        /// <summary>
        /// Creates a template under the specified path using the given template.
        /// </summary>
        /// <param name="parentPath">Target location where the templates are copied to.</param>
        /// <param name="templatePath">The template is to be copied.</param>
        /// <returns></returns>
        public static Content CreateTemplated(string parentPath, string templatePath)
        {
            if (parentPath == null) throw new ArgumentNullException("parentPath");
            if (templatePath == null) throw new ArgumentNullException("templatePath");

            var parentNode = Node.LoadNode(parentPath);
            if (parentNode == null)
                throw new InvalidOperationException(String.Format("Couldn't create a content from a template. ParentNode couldn't be loaded from {0} path.", parentPath));

            var templateNode = Node.LoadNode(templatePath);
            if (templateNode == null)
                throw new InvalidOperationException(String.Format("Couldn't create a content from a template. TemplateNode couldn't be loaded from {0} path.", templatePath));

            return ContentTemplate.CreateTemplated(parentNode, templateNode, null);
        }

        public static Content CreateTemplated(Node parent, Node template, string nameBase)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            if (template == null) throw new ArgumentNullException("template");
            //if (nameBase == null) throw new ArgumentNullException("nameBase");

            using (var traceOperation = Logger.TraceOperation("Content.CreateTemplated"))
            {
                var name = ContentNamingHelper.GetNewName(nameBase, ((GenericContent)template).GetContentType(), parent);
                var content = Content.Create(ContentTemplate.CreateFromTemplate(parent, template, name));
                var realUser = (User)User.LoggedInUser;

                var now = DateTime.UtcNow;
                var node = content.ContentHandler;
                node.CreatedBy = realUser;
                node.VersionCreatedBy = realUser;
                node.NodeOperation = NodeOperation.TemplateCreation;

                traceOperation.IsSuccessful = true;
                return content;
            }
        }

        public static Content CreateTemplatedAndParse(Node parent, Node template, string nameBase, Dictionary<string, string> fieldData)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            if (template == null) throw new ArgumentNullException("template");
            if (nameBase == null) throw new ArgumentNullException("nameBase");

            using (var traceOperation = Logger.TraceOperation("Content.CreateTemplatedAndParse"))
            {

                var content = CreateTemplated(parent, template, nameBase);
                Content.Modify(content, fieldData);

                traceOperation.IsSuccessful = true;
                return content;
            }
        }

        public static Node CreateFromTemplate(Node target, Node template, string name)
        {
            var newNode = template.MakeTemplatedCopy(target, name);
            VersionNumber version = null;

            //compute new version number according to the versioning 
            //and approving settings in the target folder
            var gc = newNode as GenericContent;
            if (gc != null)
            {
                version = SavingAction.ComputeNewVersion(gc.ApprovingMode == ApprovingType.True, gc.VersioningMode);
            }
            else
            {
                //TODO: handle non-GenericContent scenarios
            }

            if (version != null)
                newNode.Version = version;

            newNode.Template = template;

            return newNode;
        }

        public static Node GetNamedTemplate(string contentTypeName, string templateName)
        {
            var templates = GetTemplatesForType<Node>(contentTypeName);

            var sel = from Node n in templates
                      where (n.Name == templateName)
                      select n;

            return sel.FirstOrDefault();
        }

        /// <summary>
        /// Returns true if contenttemplate is found under the configured Repository.ContentTemplatesPath.
        /// </summary>
        /// <param name="contentTypeName">The contentype that is searched for.</param>
        /// <returns>True, if at least one template found otherwise returns false.</returns>
        public static bool HasTemplate(string contentTypeName)
        {
            return HasTemplate(contentTypeName, Repository.ContentTemplateFolderPath);
        }

        /// <summary>
        /// Returns true if contenttemplate is found under the given path.
        /// </summary>
        /// <param name="contentTypeName">The contentype that is searched for.</param>
        /// <param name="path">Templates are to be searched under this path.</param>
        /// <returns>True, if template is found otherwise returns false.</returns>
        public static bool HasTemplate(string contentTypeName, string path)
        {
            var templatePath = RepositoryPath.Combine(path, contentTypeName);

            if (RepositoryInstance.ContentQueryIsAllowed)
            {
                return ContentQuery.Query(SafeQueries.InFolderCountOnly,
                    new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled },
                    templatePath).Count > 0;
            }
            else
            {
                return NodeQuery.QueryChildren(path).Nodes.Where(n => n.NodeType.Name == contentTypeName).Count() > 0;
            }
        }

        // internals ///////////////////////////////////////////////////////////////////////////////

        private static void CopyChildren(Content source)
        {
            if (source == null) throw new ArgumentNullException("source");
            var template = source.ContentHandler.Template;
            if (template == null)
                throw new InvalidOperationException(String.Format("Couldn't be copied contents 'cause Template is not specified for the given content. {0}", source.Path));
            var templateContent = Content.Create(template);
            if (templateContent == null)
                throw new InvalidOperationException(String.Format("Couldn't create a Content from {0} node.", template.Path));

            using (var traceOperation = Logger.TraceOperation("ContentTemplate.CopyChildren"))
            {
                using (new SystemAccount())
                {
                    //This is to make sure that only those children are copied
                    //that are really under this content. E.g. SmartFolder may
                    //contain real children and queried children too.
                    foreach (var child in templateContent.Children.DisableAutofilters().DisableLifespan().Where(c => c.InFolder(templateContent.Path)))
                        CreateContentRecursive(child, source.ContentHandler);
                }

                //set permissions on newly created content (copy from the template)
                SetPermissions(template, source.ContentHandler);

                //update references on target content that point to template content
                UpdateLocalReferences(template, source.ContentHandler);

                traceOperation.IsSuccessful = true;
            }
        }
        private static void CreateContentRecursive(Content sourceContent, Node targetContainer)
        {
            var sourceNode = sourceContent.ContentHandler;
            var alreadyExisting = Node.LoadNode(targetContainer.Path + "/" + sourceNode.Name);
            if (alreadyExisting != null && alreadyExisting.NodeType.Name != sourceNode.NodeType.Name)
                throw new Exception("The target node already exists with a different type.");

            var target = alreadyExisting ?? NodeType.CreateInstance(sourceNode.NodeType.Name, targetContainer);
            target.Name = sourceNode.Name;
            target.DisplayName = sourceNode.DisplayName;
            target.Index = sourceNode.Index;
            target.NodeOperation = NodeOperation.TemplateChildCopy;

            var targetContent = Content.Create(target);

            //We do not want to set the current user here but the user we 'inherited'
            //from the parent (the template). In case of user profiles (my site) the
            //creator will be the user itself.
            var realCreatorUser = targetContainer.CreatedBy;
            var realModifierUser = targetContainer.ModifiedBy;

            var now = DateTime.UtcNow;
            target.CreatedBy = realCreatorUser;
            target.VersionCreatedBy = realCreatorUser;
            target.ModificationDate = now;
            target.ModifiedBy = realModifierUser;
            target.VersionModificationDate = now;
            target.VersionModifiedBy = realModifierUser;

            foreach (var propertyType in sourceNode.PropertyTypes.Where(pt => !pt.IsContentListProperty))
            {
                switch (propertyType.DataType)
                {
                    case DataType.Binary:
                        var sourceBinaryData = sourceNode.GetBinary(propertyType);
                        var targetBinaryData = new BinaryData();
                        targetBinaryData.CopyFrom(sourceBinaryData);
                        target.SetBinary(propertyType.Name, targetBinaryData);
                        break;
                    //case DataType.Reference:
                    //    break;
                    default:
                        target[propertyType] = sourceNode[propertyType];
                        break;
                }
            }

            // Iterate through content list fields and set their value on the target node separately.
            // This is needed because the same content list fields may have a different property name
            // (e.g. #String_0 and #String_1) in the source and target content lists.
            foreach (var field in sourceContent.Fields.Values.Where(f => f.Name.StartsWith("#")))
            {
                // this should give us the name of the source property (e.g. #String_0)
                var sourcePropertyName = field.FieldSetting.Bindings.FirstOrDefault();
                if (string.IsNullOrEmpty(sourcePropertyName))
                    continue;

                // this should give us the name of the target property (e.g. #String_1)
                var targetPropertyName = targetContent.Fields[field.Name].FieldSetting.Bindings.FirstOrDefault();
                if (string.IsNullOrEmpty(targetPropertyName))
                    continue;
                
                if (field is BinaryField)
                {
                    var sourceBinaryData = sourceNode.GetBinary(sourcePropertyName);
                    var targetBinaryData = new BinaryData();
                    targetBinaryData.CopyFrom(sourceBinaryData);
                    target.SetBinary(targetPropertyName, targetBinaryData);
                }
                else
                {
                    target[targetPropertyName] = sourceNode[sourcePropertyName];
                }
            }

            //workaround for content lists: content list type needs to be refreshed
            var contentList = target as ContentList;
            if (contentList != null)
            {
                contentList.ContentListDefinition = contentList.ContentListDefinition;
            }

            target.Save();

            // inherit explicit permissions from template
            using (new SystemAccount())
            {
                foreach (SecurityEntry se in sourceNode.Security.GetExplicitEntries())
                {
                    target.Security.SetPermissions(se.PrincipalId, se.Propagates, se.PermissionValues);
                }
            }

            //This is to make sure that only those children are copied
            //that are really under this content. E.g. SmartFolder may
            //contain real children and queried children too.
            foreach (var childNode in sourceNode.PhysicalChildArray.Where(ch => ch.InFolder(sourceNode.Path)))
                CreateContentRecursive(Content.Create(childNode), target);
        }

        private static void SetPermissions(Node templateRoot, Node targetRoot)
        {
            if (templateRoot == null)
                throw new ArgumentNullException("templateRoot");
            if (targetRoot == null)
                throw new ArgumentNullException("targetRoot");

            using (new SystemAccount())
            {
                IEnumerable<Node> sourceNodes;

                if (RepositoryInstance.ContentQueryIsAllowed)
                {
                    sourceNodes = ContentQuery.Query(SafeQueries.InTreeOrderByPath,
                        new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled },
                        templateRoot.Path).Nodes;
                }
                else
                {
                    var sourceNodeList = NodeQuery.QueryNodesByPath(templateRoot.Path, true).Nodes.ToList();
                    var first = sourceNodeList.FirstOrDefault();

                    //we have to add the tenplate node manually because the query above does not return the root itself
                    if (first == null || first.Id != templateRoot.Id)
                        sourceNodeList.Insert(0, templateRoot);

                    sourceNodes = sourceNodeList;
                }

                foreach (var sourceNode in sourceNodes)
                {
                    var expEntries = sourceNode.Security.GetExplicitEntries();

                    //no need to do anyithing: no explicit entries and no inheritance break here
                    if (expEntries.Length == 0 && sourceNode.IsInherited)
                        continue;

                    var targetNodePath = sourceNode.Path.Replace(templateRoot.Path, targetRoot.Path);
                    var targetNode = Node.LoadNode(targetNodePath);
                    if (!sourceNode.IsInherited)
                    {
                        targetNode.Security.BreakInheritance();
                        targetNode.Security.RemoveExplicitEntries();
                    }

                    foreach (var se in expEntries)
                    {
                        var sourcePrincHead = NodeHead.Get(se.PrincipalId);

                        if (sourcePrincHead.Path.StartsWith(templateRoot.Path))
                        {
                            //local groups: we need to change the permission 
                            //setting to map to the newly created local group
                            var targetGroupPath = sourcePrincHead.Path.Replace(templateRoot.Path, targetRoot.Path);
                            var targetGroup = NodeHead.Get(targetGroupPath);
                            if (targetGroup == null)
                                throw new InvalidOperationException("Target local group was not found: " + targetGroupPath);

                            //set permissions for target local group
                            targetNode.Security.SetPermissions(targetGroup.Id, se.Propagates, se.PermissionValues);

                            //clear original permissions
                            targetNode.Security.SetPermissions(se.PrincipalId, se.Propagates, Enumerable.Repeat(PermissionValue.NonDefined, PermissionType.NumberOfPermissionTypes).ToArray());
                        }
                        else
                        {
                            //normal groups or users
                            targetNode.Security.SetPermissions(se.PrincipalId, se.Propagates, se.PermissionValues);
                        }
                    }
                }
            }
        }

        private static void UpdateLocalReferences(Node templateRoot, Node targetRoot)
        {
            if (templateRoot == null)
                throw new ArgumentNullException("templateRoot");
            if (targetRoot == null)
                throw new ArgumentNullException("targetRoot");

            using (new SystemAccount())
            {
                IEnumerable<Content> targetContentList;

                if (RepositoryInstance.ContentQueryIsAllowed)
                {
                    targetContentList = Content.All.DisableAutofilters().DisableLifespan().Where(c => c.InTree(targetRoot)).OrderBy(c => c.Path).ToList();
                }
                else
                {
                    var targetCts = NodeQuery.QueryNodesByPath(targetRoot.Path, true).Nodes.Select(Content.Create).ToList();
                    var first = targetCts.FirstOrDefault();

                    //we have to add the tenplate node manually because the query above does not return the root itself
                    if (first == null || first.Id != targetRoot.Id)
                        targetCts.Insert(0, Content.Create(targetRoot));

                    targetContentList = targetCts;
                }

                //iterate through the newly created content
                foreach (var content in targetContentList)
                {
                    var needToSave = false;

                    //iterate through all writable reference fields
                    foreach (var field in content.Fields.Values.Where(f => f is ReferenceField && !f.ReadOnly && content.ContentHandler.HasProperty(f.Name)))
                    {
                        var refData = field.GetData();
                        var refNodes = refData as IEnumerable<Node>;
                        if (refNodes == null)
                        {
                            var refNode = refData as Node;
                            if (refNode != null)
                                refNodes = new List<Node> { refNode };
                        }

                        if (refNodes == null)
                            continue;

                        var targetRefIds = new List<int>();

                        //iterate through all referenced content and choose 
                        //the ones that point to the template subtree
                        foreach (var refNode in refNodes.Where(rn => rn.InTree(templateRoot)))
                        {
                            //remove the reference to the template sub element
                            content.ContentHandler.RemoveReference(field.Name, refNode);

                            //find the corresponding new content in the target tree (e.g. a local group)
                            var targetHead = NodeHead.Get(refNode.Path.Replace(templateRoot.Path, targetRoot.Path));
                            if (targetHead != null)
                                targetRefIds.Add(targetHead.Id);
                        }

                        if (targetRefIds.Count <= 0)
                            continue;

                        //add the new references to the property
                        content.ContentHandler.AddReferences(field.Name, Node.LoadNodes(targetRefIds));
                        needToSave = true;
                    }

                    if (!needToSave)
                        continue;

                    content.ContentHandler.NodeOperation = NodeOperation.TemplateCreation;
                    content.ContentHandler.DisableObserver(typeof(DocumentPreviewObserver));
                    content.ContentHandler.DisableObserver(TypeHandler.GetType(WFOBSERVERNAME));
                    content.ContentHandler.DisableObserver(TypeHandler.GetType(NOTIFOBSERVERNAME));

                    content.SaveSameVersion();
                }
            }
        }
    }
}
