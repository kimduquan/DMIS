using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using ContentRepository.Storage.Caching.Dependency;
using Communication.Messaging;

namespace ContentRepository.Storage.Caching.DistributedActions
{
    [Serializable]
    public class PortletChangedAction : DistributedAction
    {
        public string PortletID;

        public PortletChangedAction() { }
        public PortletChangedAction(string portletID) 
        {
            PortletID = portletID;
        }

        public override void DoAction(bool onRemote, bool isFromMe)
        {
            if (!(onRemote && isFromMe))
                PortletDependency.FireChanged(this.PortletID);
        }

        public override string ToString()
        {
            return "Portlet changed: " + this.PortletID;
        }
    }
}