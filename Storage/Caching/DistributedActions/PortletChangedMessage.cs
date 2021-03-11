using System;
using System.Collections.Generic;
using System.Text;
using Communication.Messaging;

namespace ContentRepository.Storage.Caching.DistributedActions
{
    [Serializable]
    public class PortletChangedMessage : ClusterMessage
    {
        public string PortletID;

        public PortletChangedMessage() { }
        public PortletChangedMessage(string portletID) { PortletID = portletID; }
    }
}