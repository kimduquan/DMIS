using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Configuration;
using System.Xml;

namespace Packaging.Steps
{
    /// <summary>Represents one activity in the package execution sequence</summary>
    public abstract class Step
    {
        private static Dictionary<string, Type> _stepTypes;
        internal static Dictionary<string, Type> StepTypes { get { return _stepTypes; } }

        static Step()
        {
            var stepTypes = new Dictionary<string,Type>();
            foreach (var stepType in ContentRepository.Storage.TypeHandler.GetTypesByBaseType(typeof(Step)))
            {
                if (!stepType.IsAbstract)
                {
                    var step = (Step)Activator.CreateInstance(stepType);
                    stepTypes[step.ElementName] = stepType;
                    stepTypes[stepType.FullName] = stepType;
                }
            }
            _stepTypes = stepTypes;
        }

        internal static Step Parse(XmlElement stepElement, int index)
        {
            var parameters = new Dictionary<string, XmlNode>();

            // attribute model
            foreach (XmlAttribute attr in stepElement.Attributes)
                parameters.Add(PackageManager.ToPascalCase(attr.Name), attr);

            var children = stepElement.SelectNodes("*");
            if (children.Count == 0 && stepElement.InnerXml != null && stepElement.InnerXml.Trim().Length > 0)
            {
                // default property model
                parameters.Add("", stepElement);
            }
            else
            {
                // element model
                foreach (XmlElement childElement in children)
                {
                    var name = childElement.Name;
                    if (parameters.ContainsKey(name))
                        throw new InvalidPackageException(String.Format(SR.Errors.StepParsing.AttributeAndElementNameCollision_2, stepElement.Name, name));
                    parameters.Add(name, childElement);
                }
            }

            return Step.BuildStep(index, stepElement.Name, parameters);
        }

        internal static Step BuildStep(int stepId, string stepName, Dictionary<string, XmlNode> parameters)
        {
            Type stepType;
            if (!_stepTypes.TryGetValue(stepName, out stepType))
                throw new InvalidPackageException(String.Format(SR.Errors.StepParsing.UnknownStep_1, stepName));

            var step = (Step)Activator.CreateInstance(stepType);
            step.StepId = stepId;
            foreach (var item in parameters)
                step.SetProperty(item.Key, item.Value);

            return step;
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------*/
        internal void SetProperty(string name, string value)
        {
            SetProperty(GetProperty(name), value);
        }
        internal void SetProperty(string name, XmlNode value)
        {
            var prop = GetProperty(name);
            if (prop.PropertyType == typeof(IEnumerable<XmlElement>))
                SetPropertyFromNestedElement(prop, value);
            else
                SetProperty(prop, (value is XmlAttribute) ? value.Value : value.InnerXml);
        }
        private void SetPropertyFromNestedElement(PropertyInfo prop, XmlNode value)
        {
            var element = value as XmlElement;
            if (element == null)
                return;

            try
            {
                var val = element.SelectNodes("*").Cast<XmlElement>().ToList();
                var setter = prop.GetSetMethod();
                setter.Invoke(this, new object[] { val });
            }
            catch (Exception e)
            {
                throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.CannotConvertToPropertyType_3, this.GetType().FullName, prop.Name, prop.PropertyType), e);
            }
        }
        private void SetProperty(PropertyInfo prop, string value)
        {

            if (!(prop.PropertyType.GetInterfaces().Any(x => x == typeof(IConvertible))))
                throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.PropertyTypeMustBeConvertible_2, this.GetType().FullName, prop.Name));

            var formatProvider = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                var val = prop.PropertyType.IsEnum
                        ? Enum.Parse(prop.PropertyType, value, true)
                        : ((IConvertible)(value)).ToType(prop.PropertyType, formatProvider);
                var setter = prop.GetSetMethod();
                setter.Invoke(this, new object[] { val });
            }
            catch (Exception e)
            {
                throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.CannotConvertToPropertyType_3, this.GetType().FullName, prop.Name, prop.PropertyType), e);
            }
        }
        private PropertyInfo GetProperty(string name)
        {
            var stepType = this.GetType();
            PropertyInfo prop = null;
            var propertyName = name;
            if (propertyName == string.Empty)
            {
                prop = GetDefaultProperty(stepType);
                if (prop == null)
                    throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.DefaultPropertyNotFound_1, stepType.FullName));
            }
            else
            {
                prop = stepType.GetProperty(propertyName);
                if (prop == null)
                    throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.UnknownProperty_2, stepType.FullName, propertyName));
            }
            return prop;
        }

        private static PropertyInfo GetDefaultProperty(Type stepType)
        {
            var props = stepType.GetProperties();
            foreach (var prop in props)
                if (prop.GetCustomAttributes(true).Any(x => x is DefaultPropertyAttribute))
                    return prop;
            return null;
        }

        /*=========================================================== Public instance part ===========================================================*/

        /// <summary>Returns the XML name of the step element in the manifest. Default: simple or fully qualified name of the class.</summary>
        public virtual string ElementName { get { return this.GetType().Name; } }
        /// <summary>Order number in the phase.</summary>
        public int StepId { get; private set; }
        /// <summary>The method that executes the activity. Called by packaging framework.</summary>
        public abstract void Execute(ExecutionContext context);

        /*=========================================================== Common tools ===========================================================*/

        /// <summary>Returns with a full path under the package if the path is relative.</summary>
        protected static string ResolvePackagePath(string path, ExecutionContext context)
        {
            return ResolvePath(context.PackagePath, path);
        }
        /// <summary>Returns with a full path under the target directory on the local server if the path is relative.</summary>
        protected static string ResolveTargetPath(string path, ExecutionContext context)
        {
            return ResolvePath(context.TargetPath, path);
        }
        private static string ResolvePath(string basePath, string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
                return relativePath;
            var path = Path.Combine(basePath, relativePath);
            var result = Path.GetFullPath(path);
            return result;
        }
        /// <summary>Returns with a full paths under the target directories on the network servers if the path is relative.</summary>
        protected static string[] ResolveNetworkTargets(string path, ExecutionContext context)
        {
            if (Path.IsPathRooted(path))
                return new string[0];
            var resolved = context.NetworkTargets.Select(x => Path.GetFullPath(Path.Combine(x, path))).ToArray();
            return resolved;
        }
        /// <summary>Returns with a full paths under the target directories on all servers if the path is relative.</summary>
        protected static string[] ResolveAllTargets(string path, ExecutionContext context)
        {
            var allTargets = new List<string>(context.NetworkTargets.Length + 1);
            allTargets.Add(ResolveTargetPath(path, context));
            allTargets.AddRange(ResolveNetworkTargets(path, context));
            return allTargets.ToArray();
        }
    }
}
