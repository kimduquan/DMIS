using System;
using System.Collections.Generic;
using System.Linq;
using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Security;

namespace Packaging.Steps
{
    public class BreakPermissionInheritance : Step
    {
        /// <summary>Repository path of the target content.</summary>
        [DefaultProperty]
        [Annotation("Repository path of the target content.")]
        public string Path { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if (string.IsNullOrEmpty(Path))
                throw new PackagingException(SR.Errors.InvalidParameters);

            using (new SystemAccount())
            {
                var content = Content.Load(Path);
                if (content == null)
                {
                    Logger.LogWarningMessage("Content not found: " + Path);
                    return;
                }
                ChangeInheritance(content);
            }
        }
        protected virtual void ChangeInheritance(Content content)
        {
            if (content.ContentHandler.IsInherited)
            {
                content.Security.BreakInheritance();
                Logger.LogMessage("Permission inheritance break successfully performed on " + content.Path);
            }
            else
            {
                Logger.LogMessage("Permission inheritance did not change because of a previous inheritance break on " + content.Path);
            }
        }
    }
    public class RemoveBreakPermissionInheritance : BreakPermissionInheritance
    {
        protected override void ChangeInheritance(Content content)
        {
            if (!content.ContentHandler.IsInherited)
            {
                content.Security.RemoveBreakInheritance();
                Logger.LogMessage("Permission inheritance break is removed from " + content.Path);
            }
            else
            {
                Logger.LogMessage("Permission inheritance did not change because '" + content.Path + "' already inherits its permissions.");
            }
        }
    }

    public class SetPermissions : Step
    {
        /// <summary>Repository path of the content to change permissions for.</summary>
        [DefaultProperty]
        [Annotation("Repository path of the content to change permissions on.")]
        public string Path { get; set; }

        /// <summary>"Repository path of the user or group to set permissions for." </summary>
        [Annotation("Repository path of the user or group to set permissions for.")]
        public string Identity { get; set; }

        /// <summary>"Comma-separated list of permissions to allow."</summary>
        [Annotation("Comma-separated list of permissions to allow.")]
        public string Allow { get; set; }

        /// <summary>"Comma-separated list of permissions to deny."</summary>
        [Annotation("Comma-separated list of permissions to deny.")]
        public string Deny { get; set; }

        /// <summary>Permission entry should be local only (not inherited).</summary>
        [Annotation("Permission entry should be local only (not inherited).")]
        public bool LocalOnly { get; set; }

        /// <summary>Clear permissions on the target content before settings.</summary>
        [Annotation("Clear permissions on the target content before settings.")]
        public bool Clear { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if (string.IsNullOrEmpty(Path) || string.IsNullOrEmpty(Identity) || (string.IsNullOrEmpty(Allow) && string.IsNullOrEmpty(Deny)))
                throw new PackagingException(SR.Errors.InvalidParameters);

            using (new SystemAccount())
            {
                var content = Content.Load(Path);
                if (content == null)
                {
                    Logger.LogWarningMessage("Content not found: " + Path);
                    return;
                }

                var identity = Node.LoadNode(Identity) as ISecurityMember;
                if (identity == null)
                {
                    Logger.LogWarningMessage("Identity not found: " + Identity);
                    return;
                }

                var aclEditor = content.Security.GetAclEditor();

                if (Clear)
                    DeleteAllExplicitSetting(aclEditor, content);

                // set allow permissions
                foreach (var permissionType in GetPermissionTypes(Allow))
                {
                    aclEditor.SetPermission(identity, !LocalOnly, permissionType, PermissionValue.Allow);
                }

                // set deny permissions
                foreach (var permissionType in GetPermissionTypes(Deny))
                {
                    aclEditor.SetPermission(identity, !LocalOnly, permissionType, PermissionValue.Deny);
                }

                aclEditor.Apply(); 
            }
        }

        private void DeleteAllExplicitSetting(AclEditor aclEditor, Content content)
        {
            var explicitEntries = content.ContentHandler.Security.GetExplicitEntries();
            foreach (var explicitEntry in explicitEntries)
            {
                foreach (var permType in ActiveSchema.PermissionTypes)
                {
                    var ident = (ISecurityMember)Node.LoadNode(explicitEntry.PrincipalId);
                    aclEditor.SetPermission(ident, explicitEntry.Propagates, permType, PermissionValue.NonDefined);
                }
            }
        }

        private static IEnumerable<PermissionType> GetPermissionTypes(string permissionNames)
        {
            if (string.IsNullOrEmpty(permissionNames))
                return new PermissionType[0];

            var permissionTypeNames = permissionNames.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var permissionTypes = permissionTypeNames.Select(ptn => PermissionType.GetByName(ptn))
                .Where(pt => pt != null).ToArray();

            if (permissionTypeNames.Length > permissionTypes.Length)
                throw new PackagingException("Invalid permission types: " + permissionNames);

            return permissionTypes;
        }
    }
}
