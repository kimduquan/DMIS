using ContentRepository.Storage.Security;
using ContentRepository.Storage;
using Diagnostics;
using ContentRepository;

namespace Services.WebDav
{
    public class UnLock : IHttpMethod
    {
        private WebDavHandler _handler;

        public UnLock(WebDavHandler handler)
        {
            _handler = handler;
        }

        public void HandleMethod()
        {
            if (Config.AutoCheckoutFiles)
            {
                var gc = Node.LoadNode(_handler.GlobalPath) as GenericContent;
                if (gc != null && gc.Locked && gc.LockedById == User.Current.Id)
                    gc.CheckIn();
            }

            _handler.Context.Response.Clear();
            _handler.Context.Response.StatusCode = 204;
        }
    }
}
