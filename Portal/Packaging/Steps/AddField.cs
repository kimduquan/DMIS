using ContentRepository.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SnSchema = ContentRepository.Schema;

namespace Packaging.Steps
{
    public class AddField : Step
    {
        public string ContentType { get; set; }

        [DefaultProperty]
        public string FieldXml { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            // blocked if property is not a valid name.
            if (String.IsNullOrEmpty(ContentType))
                throw new PackagingException(SR.Errors.ContentTypeSteps.InvalidContentTypeName);

            // blocked if property content type does not exist.
            var contentType = SnSchema.ContentType.GetByName(this.ContentType);
            if (contentType == null)
                throw new PackagingException(SR.Errors.ContentTypeSteps.ContentTypeNotFound);

            // blocked if the field exists.
            string fieldName;
            var fieldXml = ParseField(out fieldName);
            if (contentType.FieldSettings.FirstOrDefault(f => f.Name == fieldName) != null)
                throw new PackagingException(SR.Errors.ContentTypeSteps.FieldExists);

            // load the CTD as xml
            string ctd = null;
            using (var reader = new StreamReader(contentType.Binary.GetStream()))
                ctd = reader.ReadToEnd();

            // append the field
            var p = ctd.IndexOf("</Fields>");
            if (p < 0)
                throw new PackagingException(SR.Errors.ContentTypeSteps.InvalidFieldXml);

            // the action
            ctd = ctd.Insert(p, fieldXml);
            SnSchema.ContentTypeInstaller.InstallContentType(ctd);

            Logger.LogMessage("The content type '{0}' is extended with the '{1}' field.", this.ContentType, fieldName);
        }

        private string ParseField(out string fieldName)
        {
            var fieldXml = this.FieldXml.Trim();
            if (fieldXml.StartsWith("<![CDATA[") && fieldXml.EndsWith("]]>"))
                fieldXml = fieldXml.Substring(9, fieldXml.Length - 12).Trim();

            var xml = new XmlDocument();
            xml.LoadXml(fieldXml);

            var nameAttr = xml.SelectSingleNode("/Field/@name") as XmlAttribute;
            if (nameAttr == null)
                throw new PackagingException(SR.Errors.ContentTypeSteps.InvalidField_NameNotFound);

            fieldName = nameAttr.Value;
            if (String.IsNullOrEmpty(fieldName))
                throw new PackagingException(SR.Errors.ContentTypeSteps.FieldNameCannotBeNullOrEmpty);

            return fieldXml;
        }
    }
}
