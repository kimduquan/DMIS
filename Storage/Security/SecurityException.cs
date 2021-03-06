using System;
using System.Runtime.Serialization;
using ContentRepository.Storage.Schema;
using System.Text;

namespace ContentRepository.Storage.Security
{
    [Serializable]
    public class SecurityException : Exception
    {
        private const string ACCESSDENIED = "Access denied.";

        public SecurityException(string path, PermissionType permissionType) : this(path, permissionType, null) { }
        public SecurityException(string path, PermissionType permissionType, IUser user) : this(path, permissionType, user, null) { }
        public SecurityException(string path, string message) : this(path, null, null, message) { }
        public SecurityException(string path, PermissionType permissionType, IUser user, string message)
            : base(ACCESSDENIED)
        {
            Initialize(path: path, permissionType: permissionType, user: user, msg: message);
        }

        public SecurityException(int nodeId, PermissionType permissionType) : this(nodeId, permissionType, null) { }
        public SecurityException(int nodeId, PermissionType permissionType, IUser user) : this(nodeId, permissionType, user, null) { }
        public SecurityException(int nodeId, string message) : this(nodeId, null, null, message) { }
        public SecurityException(int nodeId, PermissionType permissionType, IUser user, string message)
            : base(ACCESSDENIED)
        {
            Initialize(nodeId: nodeId, permissionType: permissionType, user: user, msg: message);
        }

        public SecurityException(string message, Exception innerException = null)
            : base(ACCESSDENIED, innerException)
        {
            Initialize(msg: message);
        }

        protected SecurityException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        private void Initialize(string msg = null, string path = null, int nodeId = 0, PermissionType permissionType = null, IUser user = null)
        {
            this.Data.Add("FormattedMessage", GetMessage(msg, path, nodeId, permissionType, user));
            this.Data.Add("EventId", EventId.Error.SecurityError);
            if (msg != null)
                this.Data.Add("Message", msg);
            if (path != null)
                this.Data.Add("Path", path);
            if (nodeId != 0)
                this.Data.Add("NodeId", nodeId);
            if (permissionType != null)
                this.Data.Add("PermissionType", permissionType.Name);
            if (user != null)
                this.Data.Add("User", user.Username);
        }

        private static string GetMessage(string msg, string path, int nodeId, PermissionType permissionType, IUser user)
        {
            var sb = new StringBuilder(msg ?? "Access denied.");
            if (path != null)
                sb.Append(" Path: ").Append(path);
            if (nodeId != 0)
                sb.Append(" NodeId: ").Append(nodeId);
            if (permissionType != null)
                sb.Append(" PermissionType: ").Append(permissionType.Name);
            if (user != null)
                sb.Append(" User: ").Append(user.Username).Append(" UserId: ").Append(user.Id);
            return sb.ToString();
        }
    }
}