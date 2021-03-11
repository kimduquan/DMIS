﻿using System;
using System.Linq;
using ContentRepository;
using ContentRepository.Storage.Events;
using ContentRepository.Storage;
using BackgroundOperations;
using System.Web;

namespace Preview
{
    public class DocumentPreviewObserver : NodeObserver
    {
        private static readonly string[] MONITORED_FIELDS = new[] { "Binary", "Version", "Locked", "SavingState" };

        // ================================================================================= Observer methods

        protected override void OnNodeCreated(object sender, NodeEventArgs e)
        {
            base.OnNodeCreated(sender, e);
            if (e.SourceNode.CopyInProgress)
                return;

            DocumentPreviewProvider.StartPreviewGeneration(e.SourceNode, GetPriority(e.SourceNode as File));
        }
        protected override void OnNodeModified(object sender, NodeEventArgs e)
        {
            base.OnNodeModified(sender, e);

            // check: fire only when the relevant fields had been modified (binary, version, ...)
            if (!e.ChangedData.Any(d => MONITORED_FIELDS.Contains(d.Name)))
                return;

            var versionData = e.ChangedData.FirstOrDefault(d => d.Name.Equals("Version", StringComparison.OrdinalIgnoreCase));
            if (versionData != null)
            {
                var originalVersion = VersionNumber.Parse(versionData.Original.ToString());

                // if the status changed from Locked to not locked, and the version number has been decreesed: undo or checkin
                if (originalVersion.Status == VersionStatus.Locked &&
                    e.SourceNode.Version.Status != VersionStatus.Locked &&
                    originalVersion > e.SourceNode.Version &&
                    DocumentPreviewProvider.Current.IsContentSupported(e.SourceNode))
                {
                    // Undo or Checkin: we will start to delete unnecessary images for the
                    // removed version (w/o waiting for the delete operations to complete).
                    DocumentPreviewProvider.Current.RemovePreviewImagesAsync(e.SourceNode.Id, originalVersion);

                    // This was an UNDO operation, it is unnecessary to start a preview generator process
                    if (string.Compare(NodeOperation.UndoCheckOut, e.SourceNode.NodeOperation, StringComparison.OrdinalIgnoreCase) == 0)
                        return;
                }
            }

            DocumentPreviewProvider.StartPreviewGeneration(e.SourceNode, GetPriority(e.SourceNode as File));
        }

        private static TaskPriority GetPriority(File file)
        {
            if (file != null)
                return file.PreviewGenerationPriority;

            return TaskPriority.Normal;
        }
    }
}
