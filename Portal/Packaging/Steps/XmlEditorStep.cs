using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Packaging.Steps
{
    public abstract class XmlEditorStep : Step
    {
        [XmlFragment]
        [Annotation("Content can be any xml elements")]
        public string Source { get; set; }
        public string Xpath { get; set; }

        public string File { get; set; }
        public string Content { get; set; }
        public string Field { get; set; }

        public override void Execute(ExecutionContext context)
        {
            if (!String.IsNullOrEmpty(File) && String.IsNullOrEmpty(Content) && String.IsNullOrEmpty(Field))
                ExecuteOnFile(context);
            else if (String.IsNullOrEmpty(File) && !String.IsNullOrEmpty(Content))
                ExecuteOnContent(context);
            else
                throw new PackagingException(SR.Errors.InvalidParameters);
        }
        private void ExecuteOnContent(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var content = ContentRepository.Content.Load(Content);
            var data = content[Field ?? "Binary"];
            ContentRepository.Storage.BinaryData binaryData = null;
            var xmlSrc = data as string;
            if (xmlSrc == null)
            {
                binaryData = data as ContentRepository.Storage.BinaryData;
                if (binaryData != null)
                {
                    using (var r = new System.IO.StreamReader(binaryData.GetStream()))
                        xmlSrc = r.ReadToEnd();
                }
                else
                {
                    //TODO: empty stream: handle by step config (default: throw)
                }
            }

            var doc = new XmlDocument();
            doc.LoadXml(xmlSrc);

            EditXml(doc, content.Path);

            if (binaryData != null)
                binaryData.SetStream(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(doc.OuterXml)));
            else
                content[Field] = doc.OuterXml;
            content.Save();
        }
        private void ExecuteOnFile(ExecutionContext context)
        {
            foreach (var path in ResolveAllTargets(File, context))
            {
                string xmlSrc = null;
                using (var reader = new System.IO.StreamReader(path))
                    xmlSrc = reader.ReadToEnd();

                var doc = new XmlDocument();
                doc.LoadXml(xmlSrc);

                EditXml(doc, path);

                using (var writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true }))
                    doc.Save(writer);
            }
        }

        protected abstract void EditXml(XmlDocument doc, string path);
    }

    public class AppendXmlFragment : XmlEditorStep
    {
        protected override void EditXml(XmlDocument doc, string path)
        {
            var edited = 0;
            var skipped = 0;

            foreach (var node in doc.SelectNodes(this.Xpath))
            {
                var element = node as XmlElement;
                if (element != null)
                {
                    element.InnerXml += this.Source;
                    edited++;
                }
                else
                {
                    skipped++;
                }
            }

            string msg;
            switch (edited)
            {
                case 0: msg = "No element"; break;
                case 1: msg = "One element"; break;
                default: msg = edited.ToString() + " elements are"; break;
            }
            Logger.LogMessage("{0} changed. XPath: {1}. Path: {2}", msg, this.Xpath, path);

            if (skipped != 0)
                Logger.LogMessage(
                    "{0} cannot be changed because {1} not element. XPath: {2}. Path: {3}",
                    skipped == 1 ? "One node" : (skipped.ToString() + " nodes"),
                    skipped == 1 ? "it is" : "they are",
                    this.Xpath, path);

        }
    }

    public class AppendXmlAttributes : XmlEditorStep
    {
        [DefaultProperty]
        [Annotation("Content can be any xml elements")]
        public new string Source { get; set; }

        protected override void EditXml(XmlDocument doc, string path)
        {
            var edited = 0;
            var skipped = 0;

            var attrs = ParseAttributes();

            foreach (var node in doc.SelectNodes(this.Xpath))
            {
                var element = node as XmlElement;
                if (element != null)
                {
                    foreach (var item in attrs)
                        element.SetAttribute(item.Key, item.Value);
                    edited++;
                }
                else
                {
                    skipped++;
                }
            }

            string msg;
            switch (edited)
            {
                case 0: msg = "No element"; break;
                case 1: msg = "One element"; break;
                default: msg = edited.ToString() + " elements are"; break;
            }
            Logger.LogMessage("{0} changed. XPath: {1}. Path: {2}", msg, this.Xpath, path);

            if (skipped != 0)
                Logger.LogMessage(
                    "{0} cannot be changed because {1} not element. XPath: {2}. Path: {3}",
                    skipped == 1 ? "One node" : (skipped.ToString() + " nodes"),
                    skipped == 1 ? "it is" : "they are",
                    this.Xpath, path);

        }

        private Dictionary<string, string> ParseAttributes()
        {
            var result = new Dictionary<string, string>();
            var srcObject = Newtonsoft.Json.Linq.JObject.Parse(this.Source);
            foreach (var item in srcObject.Properties())
                result.Add(item.Name, item.Value.ToString());

            return result;
        }
    }

    public class EditXmlNodes : XmlEditorStep
    {
        protected override void EditXml(XmlDocument doc, string path)
        {
            var edited = 0;
            var skipped = 0;

            var nodes = doc.SelectNodes(this.Xpath);
            foreach (XmlNode node in nodes)
            {
                var attr = node as XmlAttribute;
                if (attr != null)
                {
                    attr.Value = this.Source;
                    edited++;
                    continue;
                }

                var element = node as XmlElement;
                if (element != null)
                {
                    element.InnerXml = this.Source;
                    edited++;
                    continue;
                }

                skipped++;
            }

            string msg;
            switch (edited)
            {
                case 0: msg = "No node"; break;
                case 1: msg = "One node"; break;
                default: msg = edited.ToString() + " nodes are"; break;
            }
            Logger.LogMessage("{0} changed. XPath: {1}. Path: {2}", msg, this.Xpath, path);

            if(skipped != 0)
                Logger.LogMessage(
                    "{0} cannot be changed because {1} not attribute or element. XPath: {2}. Path: {3}",
                    skipped == 1 ? "One node" : (skipped.ToString() + " nodes"),
                    skipped == 1 ? "it is" : "they are",
                    this.Xpath, path);
        }
    }

    public class DeleteXmlNodes : XmlEditorStep
    {
        protected override void EditXml(XmlDocument doc, string path)
        {
            var deleted = 0;
            var skipped = 0;

            var nodes = doc.SelectNodes(this.Xpath);
            foreach (XmlNode node in nodes)
            {
                if (node.ParentNode != null)
                {
                    node.ParentNode.RemoveChild(node);
                    deleted++;
                }
                else
                {
                    skipped++;
                }
            }

            string msg;
            switch (deleted)
            {
                case 0: msg = "No node"; break;
                case 1: msg = "One node"; break;
                default: msg = deleted.ToString() + " nodes are"; break;
            }
            Logger.LogMessage("{0} deleted. XPath: {1}. Path: {2}", msg, this.Xpath, path);

            if (skipped != 0)
                Logger.LogMessage(
                    "{0} cannot be deleted. XPath: {1}. Path: {2}",
                    skipped == 1 ? "One node" : (skipped.ToString() + " nodes"),
                    this.Xpath, path);
        }
    }

}
