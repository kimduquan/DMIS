﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Globalization;
using ApplicationModel;
using ContentRepository.Storage.Search;
using Portal.PortletFramework;
using System.Xml.XPath;
using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.Fields;
using System.Web;
using Portal.Virtualization;
using Search;
using ContentRepository.Storage.Schema;
using ContentRepository.i18n;

namespace Portal.UI
{
    public class ContentTools
    {
        private Dictionary<string, Content> _contents;

        public XPathNodeIterator GetContent(string path)
        {
            return GetContent(path, false);
        }
        public XPathNodeIterator GetContent(string path, bool withChildren)
        {
            var content = LoadContent(path);
            var stream = content.GetXml(withChildren);
            var doc = new XPathDocument(stream);
            var nav = doc.CreateNavigator();
            var iter = nav.Select("/*[1]");
            return iter;
        }

        public XPathNodeIterator SplitRate(string value, bool enableGrouping, int contentid, string propertyname)
        {
            if (String.IsNullOrEmpty(value))
            {
                if (contentid < 1 || string.IsNullOrEmpty(propertyname))
                    throw new NotSupportedException("You must set content id and rating property name");
                var c = Content.Load(contentid);
                var data = c.Fields[propertyname].GetData() as VoteData;
                value = data == null ? string.Empty : data.Serialize();
                if (string.IsNullOrEmpty(value))
                    value = c.Fields[propertyname].FieldSetting.DefaultValue;
                if (string.IsNullOrEmpty(value))
                    value = "0.0|1|0|0|0|0|0";
            }

            var stream = new MemoryStream();
            var xmlWriter = XmlWriter.Create(stream);

            var vd = VoteData.CreateVoteData(value);
            vd.EnableGrouping = enableGrouping;
            var x = new System.Xml.Serialization.XmlSerializer(vd.GetType());
            x.Serialize(xmlWriter, vd);
            xmlWriter.Flush();
            stream.Seek(0, 0);
            var doc = new XPathDocument(stream);
            var nav = doc.CreateNavigator();
            var iter = nav.Select(".");
            return iter;
        }
        
        public string Guid()
        {
            return System.Guid.NewGuid().ToString("N");
        }

        private Content LoadContent(string path)
        {
            if (_contents == null)
                _contents = new Dictionary<string, Content>();
            if (_contents.ContainsKey(path))
                return _contents[path];
            var content = Content.Load(path);
            _contents.Add(path, content);
            return content;
        }

        public XPathNodeIterator GetReferences(string path, string referenceName)
        {
            var content = LoadContent(path);
            var stream = content.GetXml(referenceName);
            var doc = new XPathDocument(stream);
            var nav = doc.CreateNavigator();
            var iter = nav.Select("/*[1]");
            return iter;
        }

        // path: original path (value of Image) generated by ImageField
        // imageModeStr: value of imageMode attribute generated by ImageField
        // width: desired width of image
        // height: desired height of image
        //  ie: <Image imageMode="UploadedThumbnail">/Root/Default_Site/Demo_Website/myContent/thumbnail.PNG</Image>
        //  <xsl:value-of select="snc:CreateThumbnailPath(/Content/Fields/Image, /Content/Fields/Image/@imageMode, 10, 20)"
        public string CreateThumbnailPath(string path, string imageModeStr, int width, int height)
        {
            var imageMode = (ImageRequestMode)Enum.Parse(typeof(ImageRequestMode), imageModeStr);
            return UITools.GetUrlWithParameters(path, ImageField.GetSizeUrlParams(imageMode, width, height));
        }

        public static string GetImageUrlFromImageField(string path)
        {
            var content = Content.Load(path);
            
            if (content != null)
            {
                var imageField = content["Avatar"] as ImageField.ImageFieldData;
                if (imageField != null)
                {
                    if (!imageField.ImgData.IsEmpty)
                    {
                        return GetBinaryUrl(content.Id, "ImageData", imageField.ImgData.Timestamp);
                    }
                    if (imageField.ImgRef != null)
                    {
                        return imageField.ImgRef.Path;
                    } 
                }
            }

            return string.Empty;
        }

        public static string GetBinaryUrl(Field field)
        {
            var binaryField = field as BinaryField;
            if (binaryField == null)
                return string.Empty;

            var binData = binaryField.GetData() as BinaryData;
            if (binData == null)
                return string.Empty;

            //in case of the default binary field (named 'Binary') we should use the content path
            if (field.Name.CompareTo(PortalContext.DefaultNodePropertyName) == 0)
                return field.Content.Path;

            return GetBinaryUrl(binaryField.Content.Id, binaryField.Name, binData.Timestamp);
        }

        public static string GetBinaryUrl(int contentId, string propertyName, long checksum = 0)
        {
            return checksum > 0
                ? String.Format("/binaryhandler.ashx?nodeid={0}&propertyname={1}&checksum={2}", contentId, HttpUtility.UrlEncode(propertyName), HttpUtility.UrlEncode(checksum.ToString()))
                : String.Format("/binaryhandler.ashx?nodeid={0}&propertyname={1}", contentId, HttpUtility.UrlEncode(propertyName));
        }

        public string CurrentUrlEncoded()
        {
            //AbsoluteUri: "http://localhost/Root/Default_Site/Apps/Folder/Edit/InRepositoryPage.aspx?action=Edit&back=%2ftargetfolder"
            //AbsolutePath: "/Root/Default_Site/Apps/Folder/Edit/InRepositoryPage.aspx"
            //Query: ?action=Edit&back=%2ftargetfolder
            //RawUrl: "/Root/Default_Site/targetfolder?action=Edit&back=%2ftargetfolder"
            return HttpUtility.UrlEncode(HttpContext.Current.Request.RawUrl);
        }

        public XPathNodeIterator GetContextNode()
        {
            return GetContent(PortalContext.Current.ContextNodePath, false);
        }

        public bool HasApprovingItems()
        {
            var hasPermission = false;
            var cq = new ContentQuery();
            cq.Text = "+InFolder:/Root/Sites/Default_Site/Book_Rental_Demo_Site/News* +Approvable:yes +Type:newsarticle";
            var result = cq.Execute(ExecutionHint.ForceIndexedEngine);
            int i = 0;
            var nodeList = result.Nodes.ToList();
            var count = nodeList.Count;
            while (!hasPermission && i<count)
            {
                hasPermission |= nodeList[i].Security.HasPermission(PermissionType.Approve);
                i++;
            }
            return hasPermission;
        }

        public string UserIsLoggedIn()
        {
            return (User.Current.Id != User.Visitor.Id).ToString().ToLower();
        }

        public static string GetUserNameByPath(string path)
        {
            var user = Node.LoadNode(path) as User;
            if (user == null)
            {
                return string.Empty;
            }
            return user.FullName ?? string.Empty;
        }

        public string GetParentFromPath(string path)
        {
            var temp = path.Split('/');
            return temp[temp.Length - 2];
        }

        public string GetParentPath(string path)
        {
            var temp = path.Split('/');
            var parentPath = "";
            for (int i = 4; i < temp.Length - 1; i++)
            {
                parentPath += "/";
                parentPath += temp[i];
            }
            return parentPath;
        }

        public string GetRatingUserGuid()
        {
            return System.Guid.NewGuid().ToString("N");
        }

        public string GetActionUrl(string url, string actionName, string backUrl)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(actionName))
                return string.Empty;
            
            Content content = null;

            try
            {
                content = Content.Load(RepositoryPath.Combine(PortalContext.Current.Site.Path, url)) ?? Content.Load(url);
            }
            catch (ContentRepository.Storage.Security.SecurityException)
            {
                return string.Empty;
            }

            var act = ActionFramework.GetAction(actionName, content, backUrl, null);
            var res = act == null ? string.Empty : act.Forbidden ? string.Empty : act.Uri;
            return res;

            //return ActionFramework.GetActionUrl(RepositoryPath.Combine(PortalContext.Current.Site.Path, url), actionName, backUrl);
        }

        public string GetCurrentUserPath()
        {
            return User.Current.Path;
        }

        public string GetResourceString(string res)
        {
            return ResourceManager.Current.GetString(res);
        }
        public string GetResourceString(string res, object arg0)
        {
            return String.Format(ResourceManager.Current.GetString(res), arg0);
        }
        public string GetResourceString(string res, object arg0, object arg1)
        {
            return String.Format(ResourceManager.Current.GetString(res), arg0, arg1);
        }
        public string GetResourceString(string res, object arg0, object arg1, object arg2)
        {
            return String.Format(ResourceManager.Current.GetString(res), arg0, arg1, arg2);
        }
        public string GetResourceString(string res, params object[] args)
        {
            return String.Format(ResourceManager.Current.GetString(res), args);
        }
    }
}
