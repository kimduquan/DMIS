using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Storage.Events;

namespace ContentRepository.Storage.AppModel
{
    internal class RepositoryEventRouter : NodeObserver
    {

        protected override void OnNodeCopied(object sender, NodeOperationEventArgs e)
        {
            RouteEvent(RepositoryEvent.Copied, sender, e.SourceNode);
        }
        protected override void OnNodeCreated(object sender, NodeEventArgs e)
        {
            RouteEvent(RepositoryEvent.Created, sender, e.SourceNode);
        }
        protected override void OnNodeDeleted(object sender, NodeEventArgs e)
        {
            RouteEvent(RepositoryEvent.Deleted, sender, e.SourceNode);
        }
        protected override void OnNodeDeletedPhysically(object sender, NodeEventArgs e)
        {
            RouteEvent(RepositoryEvent.DeletedPhysically, sender, e.SourceNode);
        }
        protected override void OnNodeModified(object sender, NodeEventArgs e)
        {
            RouteEvent(RepositoryEvent.Modified, sender, e.SourceNode);
        }
        protected override void OnNodeMoved(object sender, NodeOperationEventArgs e)
        {
            RouteEvent(RepositoryEvent.Moved, sender, e.SourceNode);
        }

        protected override void OnNodeCopying(object sender, CancellableNodeOperationEventArgs e)
        {
            RouteCancelEvent(RepositoryEvent.Copying, sender, e.SourceNode, e);
        }
        protected override void OnNodeCreating(object sender, CancellableNodeEventArgs e)
        {
            RouteCancelEvent(RepositoryEvent.Creating, sender, e.SourceNode, e);
        }
        protected override void OnNodeDeleting(object sender, CancellableNodeEventArgs e)
        {
            RouteCancelEvent(RepositoryEvent.Deleting, sender, e.SourceNode, e);
        }
        protected override void OnNodeDeletingPhysically(object sender, CancellableNodeEventArgs e)
        {
            RouteCancelEvent(RepositoryEvent.DeletingPhysically, sender, e.SourceNode, e);
        }
        protected override void OnNodeModifying(object sender, CancellableNodeEventArgs e)
        {
            RouteCancelEvent(RepositoryEvent.Modifying, sender, e.SourceNode, e);
        }
        protected override void OnNodeMoving(object sender, CancellableNodeOperationEventArgs e)
        {
            RouteCancelEvent(RepositoryEvent.Moving, sender, e.SourceNode, e);
        }

        //========================================================================================

        private void RouteEvent(RepositoryEvent @event, object sender, Node contextNode)
        {
            var args = new RepositoryEventArgs(contextNode);
            @event.FireEvent(sender, args);
        }
        private void RouteCancelEvent(RepositoryCancelEvent @event, object sender, Node contextNode, CancellableNodeEventArgs originalArgs)
        {
            var args = new RepositoryCancelEventArgs(contextNode);
            @event.FireEvent(sender, args);
            originalArgs.Cancel = args.Cancel;
            originalArgs.CancelMessage = args.CancelMessage;
        }
    }
}
