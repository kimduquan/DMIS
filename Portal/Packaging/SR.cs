using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Packaging
{
    internal static class SR
    {
        internal static class Errors
        {
            internal static readonly string ManifestNotFound = "Manifest not found.";
            internal static readonly string PackageCanContainOnlyOneFileInTheRoot = "Package can contain only one file in the root.";
            internal static readonly string InvalidPhaseIndex_2 = "Phase index out of range. Count of phases: {0}, requested index: {1}.";
            internal static readonly string InvalidParameters = "Invalid parameters";

            internal static readonly string PhaseFinishedWithError_1 = "Phase terminated with error: {0}";

            internal static class Manifest
            {
                internal static readonly string WrongRootName = @"Invalid manifest: root element must be ""Package"".";
                internal static readonly string MissingType = @"Invalid manifest: missing ""type"" attribute.";
                internal static readonly string InvalidType = @"Invalid manifest: invalid ""type"" attribute. The value must be ""product"" or ""application""";
                internal static readonly string MissingLevel = @"Invalid manifest: missing ""level"" attribute.";
                internal static readonly string InvalidLevel = @"Invalid manifest: invalid ""level"" attribute. The value must be ""tool"", ""patch"", ""servicepack"" or ""upgrade""";
                internal static readonly string MissingName = @"Invalid manifest: missing ""Name"" element.";
                internal static readonly string InvalidName = @"Invalid manifest: invalid ""Name"" element. Value cannot be empty.";
                internal static readonly string MissingEdition = @"Invalid manifest: missing ""Edition"" element.";
                internal static readonly string InvalidEdition = @"Invalid manifest: invalid ""Edition"" element. Value cannot be empty.";
                internal static readonly string MissingAppId = @"Invalid manifest: missing ""AppId"" element.";
                internal static readonly string InvalidAppId = @"Invalid manifest: invalid ""AppId"" element. Value cannot be empty.";
                internal static readonly string MissingReleaseDate = @"Invalid manifest: missing ""ReleaseDate"" element.";
                internal static readonly string InvalidReleaseDate = @"Invalid manifest: invalid ""ReleaseDate"" element.";
                internal static readonly string MissingVersionControl = @"Invaid manifest: missing ""VersionControl"" element.";
                internal static readonly string MissingVersionAttribute_1 = @"Invalid manifest: missing ""{0}"" VersionControl attribute.";
                internal static readonly string InvalidVersionAttribute_1 = @"Invalid manifest: invalid ""{0}"" VersionControl attribute.";
                internal static readonly string UnexpectedAppId = @"Invalid manifest: ""ApplicationIdentifier"" cannot be defined if the package type is ""product"".";
                internal static readonly string UnexpectedTarget = @"Invalid manifest: the ""target"" VersionControl attribute cannot be defined if the package level is ""tool"".";
                internal static readonly string UnexpectedExpectedVersion = @"Invalid manifest: the ""expected"" VersionControl attribute cannot be defined if the ""expectedMin"" or ""expectedMax"" exist.";
            }

            internal static class Precondition
            {
                internal static readonly string AppIdDoesNotMatch = "Package cannot be executed: Application identifier mismatch.";
                internal static readonly string MinimumVersion_1 = "Package cannot be executed: the {0} version is smaller than permitted.";
                internal static readonly string MaximumVersion_1 = "Package cannot be executed: the {0} version is greater than permitted.";
                internal static readonly string TargetVersionTooSmall_3 = @"Invalid manifest: the target version ({1}) must be greater than the current {0} version ({2}).";
                internal static readonly string EditionMismatch_2 = "Package cannot be executed: Edition mismatch. Installed: {0}, in package: {1}.";
                internal static readonly string CannotInstallExistingApp = "Cannot install existing application.";
            }

            internal static class StepParsing
            {
                internal static readonly string AttributeAndElementNameCollision_2 = "Attribute and element name must be unique. Step: {0}, name: {1}.";
                internal static readonly string UnknownStep_1 = "Unknown Step: {0}";
                internal static readonly string UnknownProperty_2 = "Unknown property. Step: {0}, name: {1}.";
                internal static readonly string DefaultPropertyNotFound_1 = "Default property not found. Step: {0}.";
                internal static readonly string PropertyTypeMustBeConvertible_2 = "The type of the property must be IConvertible. Step: {0}, property: {1}.";
                internal static readonly string CannotConvertToPropertyType_3 = "Cannot convert a string value to target type. Step: {0}, property: {1}, property type: {2}.";
            }

            internal static class Import
            {
                internal static readonly string SourceNotFound = "Source not found.";
                internal static readonly string TargetNotFound = "Target not found.";
                internal static readonly string InvalidTarget = "Invalid target. See inner exception.";
            }

            internal static class ContentTypeSteps
            {
                internal static readonly string InvalidContentTypeName = "Invalid content type name.";
                internal static readonly string ContentTypeNotFound = "Content type not found.";
                internal static readonly string FieldExists = "Cannot add the field if it exists.";
                internal static readonly string InvalidField_NameNotFound = "Invalid field xml: name not found";
                internal static readonly string FieldNameCannotBeNullOrEmpty = "Field name cannot be null or empty.";
                internal static readonly string InvalidFieldXml = "Invalid field xml.";
            }
        }
    }
}
