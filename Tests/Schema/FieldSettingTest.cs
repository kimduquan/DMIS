using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using ContentRepository.Schema;
using ContentRepository.Storage;
using ContentRepository.Storage.Schema;
using System.Collections;
using ContentRepository.Fields;
using SNC = ContentRepository;
using ContentRepository.Storage.Search;
using System.Web.UI.WebControls;
using System.Xml.XPath;
using System.Xml;
using System.Linq;
using ContentRepository.Tests.ContentHandlers;
using Microsoft.Practices.Unity;
using System.Configuration;
using Microsoft.Practices.Unity.Configuration;

namespace ContentRepository.Tests.Schema
{
    public class CustomFieldSetting : IntegerFieldSetting
    {
        public const string EvenOnlyName = "EvenOnly";

        private bool? _evenOnly;

        public bool? EvenOnly
        {
            get
            {
                if (_evenOnly != null)
                    return (bool)_evenOnly;
                if (this.ParentFieldSetting == null)
                    return null;
                CustomFieldSetting parentCustom = this.ParentFieldSetting as CustomFieldSetting;
                if (parentCustom == null)
                    return null;
                return parentCustom.EvenOnly;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            //<MinValue>-6</MinValue>
            //<MaxValue>42</MaxValue>
            foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (node.LocalName)
                {
                    case EvenOnlyName:
                        bool evenOnlyValue;
                        if (Boolean.TryParse(node.InnerXml, out evenOnlyValue))
                            _evenOnly = evenOnlyValue;
                        break;
                }
            }
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            _evenOnly = GetConfigurationNullableValue<bool>(info, EvenOnlyName, null);
        }
        protected override Dictionary<string,object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(EvenOnlyName, _evenOnly);
            return result;
        }
        protected override void SetDefaults()
        {
            _evenOnly = null;
            base.SetDefaults();
        }

        public override FieldValidationResult ValidateData(object value, Field field)
        {
            int intValue = (int)value;
            if (this.EvenOnly != null)
                if (intValue % 2 != 0)
                    return new FieldValidationResult(EvenOnlyName);
            return base.ValidateData(value, field);
        }
    }

    [TestClass()]
    public class FieldSettingTest : TestBase
    {
        #region Test Infrastructure
        private TestContext testContextInstance;

        public override TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion
        #endregion

        private static string _testRootName = "_FieldSettingTest_ContentTest";
        private static string __testRootPath = String.Concat("/Root/", _testRootName);
        private Folder __testRoot;
        private Folder _testRoot
        {
            get
            {
                if (__testRoot == null)
                {
                    __testRoot = (Folder)Node.LoadNode(__testRootPath);
                    if (__testRoot == null)
                    {
                        Folder folder = new Folder(Repository.Root);
                        folder.Name = _testRootName;
                        folder.Save();
                        __testRoot = (Folder)Node.LoadNode(__testRootPath);
                    }
                }
                return __testRoot;
            }
        }

        [ClassCleanup]
        public static void RemoveContentTypes()
        {
            if (Node.Exists(__testRootPath))
                Node.ForceDelete(__testRootPath);
            ContentType ct;
            ct = ContentType.GetByName("FieldSetting_Structure");
            if (ct != null)
                ct.Delete();
            ct = ContentType.GetByName("FieldSetting_Structure2");
            if (ct != null)
                ct.Delete();
            ct = ContentType.GetByName("FieldSetting_Structure1");
            if (ct != null)
                ct.Delete();
            ct = ContentType.GetByName("ValidatedContent");
            if (ct != null)
                ct.Delete();
            ct = ContentType.GetByName("ReferredContent");
            if (ct != null)
                ct.Delete();
            ct = ContentType.GetByName("ContentWithXmlField");
            if (ct != null)
                ct.Delete();
        }

        [TestMethod]
        public void FieldSetting_Structure()
        {
            string ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='TestString' type='ShortText'>
							<Configuration>
								<ReadOnly>false</ReadOnly>
								<Compulsory>true</Compulsory>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            ContentTypeInstaller.InstallContentType(ctd);
            SNC.Content content = SNC.Content.CreateNew("FieldSetting_Structure", _testRoot, "FieldSetting1");
            content["TestString"] = "TestValue";
            bool valid = content.IsValid;
            Assert.IsTrue(valid);
        }
        [TestMethod]
        public void FieldSetting_Structure1()
        {
            string ctd1 = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='FieldSetting_Structure1' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='TestString' type='ShortText'>
							<Configuration>
								<MinLength>2</MinLength>
								<MaxLength>20</MaxLength>
							</Configuration>
						</Field>
						<Field name='TestText' type='LongText'>
							<Configuration>
								<MinLength>2</MinLength>
								<MaxLength>20</MaxLength>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            string ctd2 = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='FieldSetting_Structure2' parentType='FieldSetting_Structure1' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='TestString' type='ShortText'>
							<Configuration>
								<MinLength>4</MinLength>
							</Configuration>
						</Field>
						<Field name='TestText' type='LongText'>
							<Configuration>
								<MinLength>4</MinLength>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            ContentTypeInstaller.InstallContentType(ctd1, ctd2);

            SNC.Content content1 = SNC.Content.CreateNew("FieldSetting_Structure1", _testRoot, "FieldSetting1");
            content1["TestString"] = "TestValue";
            content1["TestText"] = "TestValue";
            bool valid1 = content1.IsValid;

            SNC.Content content2 = SNC.Content.CreateNew("FieldSetting_Structure2", _testRoot, "FieldSetting2");
            content2["TestString"] = "TestValue";
            content2["TestText"] = "TestValue";
            bool valid2 = content2.IsValid;

            Assert.IsTrue(valid1, "#1");
            Assert.IsTrue(valid2, "#2");
        }

        //======================================================================================= Abstract FieldSetting tests

        [TestMethod]
        public void FieldSetting_Inheritance_ModifySettingType()
        {
            ContentType c = ContentType.GetByName("CT_Root");
            if (c != null)
                ContentTypeInstaller.RemoveContentType(c);
            ContentTypeManager.Reset();

            ContentTypeInstaller installer = ContentTypeInstaller.CreateBatchContentTypeInstaller();

            installer.AddContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='CT_Root' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='InheritanceTest' type='Integer'>
							<Configuration>
								<MinValue>-5</MinValue>
								<MaxValue>7</MaxValue>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");

            //CT_ROOT
            //    CT_A
            //        CT_A_A
            //    CT_B
            //        CT_B_A
            //            CT_B_A_A
            //        CT_B_B               <=== change
            //            CT_B_B_A
            //                CT_B_B-A-A
            //                CT_B_B-A-B
            //            CT_B_B_B
            //                CT_B_B_B_A
            //                CT_B_B_B_B
            //            CT_B_B_C
            //                CT_B_B_C_A
            //                CT_B_B_C_B
            //        CT_B_C
            //            CT_B_C_A
            //    CT_C
            //        CT_C_A

            installer.AddContentType("<ContentType name='CT_B' parentType='CT_Root' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'><Fields><Field name='InheritanceTest' type='Integer'></Field></Fields></ContentType>");
            installer.AddContentType("<ContentType name='CT_B_B' parentType='CT_B' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'><Fields><Field name='InheritanceTest' type='Integer'></Field></Fields></ContentType>");
            installer.AddContentType("<ContentType name='CT_B_B_B' parentType='CT_B_B' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'><Fields><Field name='InheritanceTest' type='Integer'></Field></Fields></ContentType>");
            installer.AddContentType("<ContentType name='CT_B_B_B_B' parentType='CT_B_B_B' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'><Fields><Field name='InheritanceTest' type='Integer'></Field></Fields></ContentType>");
            //installer.AddContentType("<ContentType name='CT_B' parentType='CT_Root' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'><Fields><Field name='InheritanceTest' override='true' type='Integer'></Field></Fields></ContentType>");
            //installer.AddContentType("<ContentType name='CT_B_B' parentType='CT_B' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'><Fields><Field name='InheritanceTest' override='true' type='Integer'></Field></Fields></ContentType>");
            //installer.AddContentType("<ContentType name='CT_B_B_B' parentType='CT_B_B' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'><Fields><Field name='InheritanceTest' override='true' type='Integer'></Field></Fields></ContentType>");
            //installer.AddContentType("<ContentType name='CT_B_B_B_B' parentType='CT_B_B_B' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'><Fields><Field name='InheritanceTest' override='true' type='Integer'></Field></Fields></ContentType>");

            installer.ExecuteBatch();

            bool[] b = new bool[21];

            ContentType CT_Root = ContentType.GetByName("CT_Root");
            FieldSetting FS_Root = CT_Root.FieldSettings[0];
            ContentType CT_B = ContentType.GetByName("CT_B");
            FieldSetting FS_B = CT_B.FieldSettings[0];
            ContentType CT_B_B = ContentType.GetByName("CT_B_B");
            FieldSetting FS_B_B = CT_B_B.FieldSettings[0];
            ContentType CT_B_B_B = ContentType.GetByName("CT_B_B_B");
            FieldSetting FS_B_B_B = CT_B_B_B.FieldSettings[0];
            ContentType CT_B_B_B_B = ContentType.GetByName("CT_B_B_B_B");
            FieldSetting FS_B_B_B_B = CT_B_B_B_B.FieldSettings[0];
            b[0] = FS_Root.ParentFieldSetting == null;
            b[1] = FS_B.ParentFieldSetting != null;
            b[2] = FS_B_B.ParentFieldSetting != null;
            b[3] = FS_B_B_B.ParentFieldSetting != null;
            b[4] = FS_B_B_B_B.ParentFieldSetting != null;
            b[5] = FS_B.ParentFieldSetting == FS_Root;
            b[6] = FS_B_B.ParentFieldSetting == FS_B;
            b[7] = FS_B_B_B.ParentFieldSetting == FS_B_B;
            b[8] = FS_B_B_B_B.ParentFieldSetting == FS_B_B_B;

            //------------------------------------- Change CT_B_B.InheritanceTest.FieldType from default setting to custom field setting

            ContentTypeInstaller.InstallContentType(@"<ContentType name='CT_B_B' parentType='CT_B' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<!--<Field name='InheritanceTest' override='true' type='Integer'>-->
						<Field name='InheritanceTest' type='Integer'>
							<Configuration handler='ContentRepository.Tests.Schema.CustomFieldSetting'>
								<EvenOnly>true</EvenOnly>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");

            CT_Root = ContentType.GetByName("CT_Root");
            FS_Root = CT_Root.FieldSettings[0];
            CT_B = ContentType.GetByName("CT_B");
            FS_B = CT_B.FieldSettings[0];
            CT_B_B = ContentType.GetByName("CT_B_B");
            FS_B_B = CT_B_B.FieldSettings[0];
            CT_B_B_B = ContentType.GetByName("CT_B_B_B");
            FS_B_B_B = CT_B_B_B.FieldSettings[0];
            CT_B_B_B_B = ContentType.GetByName("CT_B_B_B_B");
            FS_B_B_B_B = CT_B_B_B_B.FieldSettings[0];
            b[9] = FS_Root.ParentFieldSetting == null;
            b[10] = FS_B.ParentFieldSetting != null;
            b[11] = FS_B_B.ParentFieldSetting != null;
            b[12] = FS_B_B_B.ParentFieldSetting != null;
            b[13] = FS_B_B_B_B.ParentFieldSetting != null;
            b[14] = FS_B.ParentFieldSetting == FS_Root;
            b[15] = FS_B_B.ParentFieldSetting == FS_B;
            b[16] = FS_B_B_B.ParentFieldSetting == FS_B_B;
            b[17] = FS_B_B_B_B.ParentFieldSetting == FS_B_B_B;

            b[18] = FS_B.GetType().FullName == "ContentRepository.Fields.IntegerFieldSetting";
            b[19] = FS_B_B.GetType().FullName == "ContentRepository.Tests.Schema.CustomFieldSetting";
            b[20] = FS_B_B_B.GetType().FullName == "ContentRepository.Fields.IntegerFieldSetting";

            //----------------------

            ContentTypeInstaller.RemoveContentType(ContentType.GetByName("CT_Root"));
            ContentTypeManager.Reset();

            //----------------------

            for (int i = 0; i < b.Length; i++)
                Assert.IsTrue(b[i], "#" + i);
        }

        //======================================================================================= Abstract FieldSetting tests

        [TestMethod]
        public void FieldSetting_ReadOnlyAndCompulsoryOnGenericProperty()
        {
            bool readOnly;
            bool compulsory;

            string ctd = @"<?xml version='1.0' encoding='utf-8'?>
							<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
								<Fields>
									<Field name='TestString' type='ShortText'>
										<Configuration>
											<ReadOnly>false</ReadOnly>
											<Compulsory>false</Compulsory>
										</Configuration>
									</Field>
								</Fields>
							</ContentType>";
            InstallContentType(ctd, null);
            GetReadOnlyAndCompulsory("FieldSetting_Structure", out readOnly, out compulsory);
            Assert.IsFalse(readOnly, "#1");
            Assert.IsFalse(compulsory, "#2");

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
							<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
								<Fields>
									<Field name='TestString' type='ShortText'>
										<Configuration>
											<ReadOnly>true</ReadOnly>
											<Compulsory>false</Compulsory>
										</Configuration>
									</Field>
								</Fields>
							</ContentType>";
            InstallContentType(ctd, null);
            GetReadOnlyAndCompulsory("FieldSetting_Structure", out readOnly, out compulsory);
            Assert.IsTrue(readOnly, "#3");
            Assert.IsFalse(compulsory, "#4");

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
							<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
								<Fields>
									<Field name='TestString' type='ShortText'>
										<Configuration>
											<ReadOnly>false</ReadOnly>
											<Compulsory>true</Compulsory>
										</Configuration>
									</Field>
								</Fields>
							</ContentType>";
            InstallContentType(ctd, null);
            GetReadOnlyAndCompulsory("FieldSetting_Structure", out readOnly, out compulsory);
            Assert.IsFalse(readOnly, "#5");
            Assert.IsTrue(compulsory, "#6");

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
							<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
								<Fields>
									<Field name='TestString' type='ShortText'>
										<Configuration>
											<ReadOnly>true</ReadOnly>
											<Compulsory>true</Compulsory>
										</Configuration>
									</Field>
								</Fields>
							</ContentType>";
            InstallContentType(ctd, null);
            GetReadOnlyAndCompulsory("FieldSetting_Structure", out readOnly, out compulsory);
            Assert.IsTrue(readOnly, "#7");
            Assert.IsTrue(compulsory, "#8");
        }
        [TestMethod]
        public void FieldSetting_ReadWriteOnReadOnlyProperty()
        {
            bool readOnly;
            bool compulsory;

            string ctd = @"<?xml version='1.0' encoding='utf-8'?>
							<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
								<Fields>
									<Field name='Id' type='Number'>
										<Configuration>
											<ReadOnly>true</ReadOnly>
										</Configuration>
									</Field>
								</Fields>
							</ContentType>";
            InstallContentType(ctd, null);
            GetReadOnlyAndCompulsory("FieldSetting_Structure", out readOnly, out compulsory);
            Assert.IsTrue(readOnly, "#9");
        }
        [TestMethod]
        public void FieldSetting_ReadOnlyAndCompulsory_Reset()
        {
            bool readOnly;
            bool compulsory;

            string ctd = @"<?xml version='1.0' encoding='utf-8'?>
							<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
								<Fields>
									<Field name='TestString' type='ShortText'>
										<Configuration>
											<ReadOnly>true</ReadOnly>
											<Compulsory>true</Compulsory>
										</Configuration>
									</Field>
								</Fields>
							</ContentType>";
            InstallContentType(ctd, null);
            GetReadOnlyAndCompulsory("FieldSetting_Structure", out readOnly, out compulsory);
            Assert.IsTrue(readOnly, "#1");
            Assert.IsTrue(compulsory, "#2");

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
							<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
								<Fields>
									<Field name='TestString' type='ShortText' />
								</Fields>
							</ContentType>";
            InstallContentType(ctd, null);
            GetReadOnlyAndCompulsory("FieldSetting_Structure", out readOnly, out compulsory);
            Assert.IsFalse(readOnly, "#3");
            Assert.IsFalse(compulsory, "#4");

        }
        private void GetReadOnlyAndCompulsory(string contentTypeName, out bool readOnly, out bool compulsory)
        {
            ContentType contentType = ContentTypeManager.Current.GetContentTypeByName(contentTypeName);
            PrivateObject obj = new PrivateObject(contentType);
            object o = obj.GetProperty("FieldSettings");
            PrivateObject setting = new PrivateObject(((IList)o)[0]);
            readOnly = (bool)setting.GetProperty("ReadOnly", new object[0]);
            compulsory = (bool)setting.GetProperty("Compulsory", new object[0]);
        }

        //======================================================================================= Derived FieldSetting tests

        [TestMethod]
        public void FieldSetting_Reference()
        {
            string ctd;
            ContentType contentType;
            ReferenceFieldSetting fieldSetting;

            //====

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='RefTest' type='Reference'>
							<Configuration />
						</Field>
					</Fields>
				</ContentType>";
            InstallContentType(ctd, null);
            contentType = ContentType.GetByName("FieldSetting_Structure");
            fieldSetting = contentType.GetFieldSettingByName("RefTest") as ReferenceFieldSetting;
            Assert.IsFalse(fieldSetting.AllowMultiple == true, "#01");
            Assert.IsTrue(fieldSetting.AllowedTypes == null, "#02");
            Assert.IsTrue(fieldSetting.SelectionRoots == null, "#03");
            Assert.IsFalse(fieldSetting.Compulsory == true, "#04");
            Assert.IsFalse(fieldSetting.ReadOnly, "#05");

            //====

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='RefTest' type='Reference'>
							<Configuration>
								<AllowMultiple>false</AllowMultiple>
								<Compulsory>true</Compulsory>
								<ReadOnly>true</ReadOnly>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            InstallContentType(ctd, null);
            contentType = ContentType.GetByName("FieldSetting_Structure");
            fieldSetting = contentType.GetFieldSettingByName("RefTest") as ReferenceFieldSetting;
            Assert.IsFalse(fieldSetting.AllowMultiple == true, "#11");
            Assert.IsTrue(fieldSetting.AllowedTypes == null, "#12");
            Assert.IsTrue(fieldSetting.SelectionRoots == null, "#13");
            Assert.IsTrue(fieldSetting.Compulsory == true, "#14");
            Assert.IsTrue(fieldSetting.ReadOnly, "#15");

            //====

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='RefTest' type='Reference'>
							<Configuration>
								<AllowMultiple>true</AllowMultiple>
								<AllowedTypes>
									<Type>Folder</Type>
									<Type>File</Type>
								</AllowedTypes>
								<SelectionRoot>
									<Path>/Root/1</Path>
									<Path>/Root/2</Path>
								</SelectionRoot>
								<Query>
									<q:And xmlns:q='http://schemas.com/ContentRepository/SearchExpression'>
									  <q:String op='StartsWith' property='Path'>.</q:String>
									  <q:String op='NotEqual' property='Name'>Restricted</q:String>
									</q:And>
								</Query>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            InstallContentType(ctd, null);
            contentType = ContentType.GetByName("FieldSetting_Structure");
            fieldSetting = contentType.GetFieldSettingByName("RefTest") as ReferenceFieldSetting;
            Assert.IsTrue(fieldSetting.AllowMultiple == true, "#21");
            Assert.IsTrue(fieldSetting.AllowedTypes.Count == 2, "#22");
            Assert.IsTrue(fieldSetting.AllowedTypes[0] == "Folder", "#23");
            Assert.IsTrue(fieldSetting.AllowedTypes[1] == "File", "#24");
            Assert.IsTrue(fieldSetting.SelectionRoots.Count == 2, "#25");
            Assert.IsTrue(fieldSetting.SelectionRoots[0] == "/Root/1", "#26");
            Assert.IsTrue(fieldSetting.SelectionRoots[1] == "/Root/2", "#27");
            Assert.IsFalse(fieldSetting.Compulsory == true, "#28");
            Assert.IsFalse(fieldSetting.ReadOnly, "#29");

        }

        //======================================================================================= Validation tests

        [TestMethod]
        public void FieldValidation_ShortText()
        {
            //Assert.Inconclusive("Missing validation message");

            string ctd;
            bool isValid;
            FieldValidationResult validationResult;

            PrivateType fieldSettingAccessor = new PrivateType(typeof(ShortTextFieldSetting));
            string minLengthError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("MinLengthName");
            string maxLengthError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("MaxLengthName");
            string regexError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("RegexName");
            //string compulsoryError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("CompulsoryName");

            //====

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ShortTextTest' type='ShortText'>
							<Configuration>
								<MinLength>3</MinLength>
								<MaxLength>8</MaxLength>
								<Regex>^[a-zA-Z0-9]*$</Regex>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            ContentTypeInstaller.InstallContentType(ctd);

            SNC.Content content = SNC.Content.CreateNew("ValidatedContent", _testRoot, "abc");


            content["ShortTextTest"] = "";
            isValid = content.IsValid;
            validationResult = content.Fields["ShortTextTest"].ValidationResult;
            Assert.IsFalse(isValid, "#1");
            Assert.IsTrue(validationResult.Category == TextFieldSetting.MinLengthName, "#2");
            Assert.IsTrue((int)content.Fields["ShortTextTest"].ValidationResult.GetParameter(TextFieldSetting.MinLengthName) == 3, "#3");

            content["ShortTextTest"] = "ab";
            isValid = content.IsValid;
            validationResult = content.Fields["ShortTextTest"].ValidationResult;
            Assert.IsFalse(isValid, "#4");
            Assert.IsTrue(validationResult.Category == TextFieldSetting.MinLengthName, "#5");
            Assert.IsTrue((int)content.Fields["ShortTextTest"].ValidationResult.GetParameter(TextFieldSetting.MinLengthName) == 3, "#6");

            content["ShortTextTest"] = "abcdefgh01";
            isValid = content.IsValid;
            Assert.IsFalse(isValid, "#7");
            Assert.IsTrue(content.Fields["ShortTextTest"].ValidationResult.Category == TextFieldSetting.MaxLengthName, "#8");
            Assert.IsTrue((int)content.Fields["ShortTextTest"].ValidationResult.GetParameter(TextFieldSetting.MaxLengthName) == 8, "#9");

            content["ShortTextTest"] = "!@#$%^";
            isValid = content.IsValid;
            Assert.IsFalse(isValid, "#10");
            Assert.IsTrue(content.Fields["ShortTextTest"].ValidationResult.Category == ShortTextFieldSetting.RegexName, "#11");

            content["ShortTextTest"] = "Correct";
            isValid = content.IsValid;
            Assert.IsTrue(isValid, "#12");
        }
        [TestMethod]
        public void FieldValidation_ShortText_Compulsory()
        {
            string ctd;
            bool isValid;
            FieldValidationResult validationResult;

            PrivateType fieldSettingAccessor = new PrivateType(typeof(ShortTextFieldSetting));
            string compulsoryError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("CompulsoryName");

            //====

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
                <ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
                    <Fields>
                        <Field name='ShortTextTest' type='ShortText'>
                            <Configuration>
                                <Compulsory>true</Compulsory>
                                <MinLength>3</MinLength>
                                <MaxLength>8</MaxLength>
                                <Regex>^[a-zA-Z0-9]*$</Regex>
                            </Configuration>
                        </Field>
                    </Fields>
                </ContentType>";
            ContentTypeInstaller.InstallContentType(ctd);

            SNC.Content content = SNC.Content.CreateNew("ValidatedContent", _testRoot, "abc");


            content["ShortTextTest"] = null;
            isValid = content.IsValid;
            validationResult = content.Fields["ShortTextTest"].ValidationResult;
            Assert.IsFalse(isValid, "#1");
            Assert.IsTrue(validationResult.Category == compulsoryError, "#2");

            content["ShortTextTest"] = "";
            isValid = content.IsValid;
            validationResult = content.Fields["ShortTextTest"].ValidationResult;
            Assert.IsFalse(isValid, "#3");
            Assert.IsTrue(validationResult.Category == compulsoryError, "#4");

            content["ShortTextTest"] = "Correct";
            isValid = content.IsValid;
            Assert.IsTrue(isValid, "#5");
        }

        [TestMethod]
        public void FieldValidation_Xml()
        {
            string ctd;
            bool isValid;
            FieldValidationResult validationResult;

            PrivateType fieldSettingAccessor = new PrivateType(typeof(XmlFieldSetting));
            string compulsoryError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("CompulsoryName");
            string namespaceError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("ExpectedXmlNamespaceName");
            string notWellformedXmlError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("NotWellformedXmlName");
            //====

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
                <ContentType name='ContentWithXmlField' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
                    <Fields>
                        <Field name='XmlTest' type='Xml'>
                            <Configuration>
                                <ExpectedXmlNamespace>htp://example.com/namespace</ExpectedXmlNamespace>
                            </Configuration>
                        </Field>
                    </Fields>
                </ContentType>";
            ContentTypeInstaller.InstallContentType(ctd);

            SNC.Content content = SNC.Content.CreateNew("ContentWithXmlField", _testRoot, null);

            //----

            content["XmlTest"] = null;

            Assert.IsTrue(content.IsValid, "#1");

            //----

            content["XmlTest"] = "<a xmlns='htp://example.com/namespace'><b>content</b></a>";

            Assert.IsTrue(content.IsValid, "#2");

            //----

            content["XmlTest"] = "";

            isValid = content.IsValid;
            validationResult = content.Fields["XmlTest"].ValidationResult;
            Assert.IsFalse(isValid, "#3");
            Assert.IsTrue(validationResult.Category == notWellformedXmlError, "#4");

            //----

            content["XmlTest"] = "<a xmlns='htp://example.com/namespace'><b>content<b></a>";

            isValid = content.IsValid;
            validationResult = content.Fields["XmlTest"].ValidationResult;
            Assert.IsFalse(isValid, "#5");
            Assert.IsTrue(validationResult.Category == notWellformedXmlError, "#6");

            //----

            content["XmlTest"] = "<a><b>content</b></a>";

            isValid = content.IsValid;
            validationResult = content.Fields["XmlTest"].ValidationResult;
            Assert.IsFalse(isValid, "#7");
            Assert.IsTrue(validationResult.Category == namespaceError, "#8");

            //=======================

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
                <ContentType name='ContentWithXmlField' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
                    <Fields>
                        <Field name='XmlTest' type='Xml'>
                            <Configuration>
                                <Compulsory>true</Compulsory>
                                <ExpectedXmlNamespace>htp://example.com/namespace</ExpectedXmlNamespace>
                            </Configuration>
                        </Field>
                    </Fields>
                </ContentType>";
            ContentTypeInstaller.InstallContentType(ctd);

            content = SNC.Content.CreateNew("ContentWithXmlField", _testRoot, null);
            content["XmlTest"] = null;

            isValid = content.IsValid;
            validationResult = content.Fields["XmlTest"].ValidationResult;
            Assert.IsFalse(isValid, "#9");
            Assert.IsTrue(validationResult.Category == compulsoryError, "#10");

            //=======================

            content["XmlTest"] = "<a xmlns='htp://example.com/namespace'><b>content</b></a>";

            var sb = new StringBuilder();
            var writer = XmlWriter.Create(sb);
            writer.WriteStartDocument();
            writer.WriteStartElement("root");
            content.Fields["XmlTest"].Export(writer, new ExportContext("/Root", "c:\\"));
            writer.WriteEndElement();
            writer.Flush();
            writer.Close();

            Assert.IsTrue(sb.ToString() == "<?xml version=\"1.0\" encoding=\"utf-16\"?><root><XmlTest><a xmlns='htp://example.com/namespace'><b>content</b></a></XmlTest></root>", "#11");
        }

        [TestMethod]
        public void FieldValidation_Password()
        {
            string ctd;
            bool isValid;
            FieldValidationResult validationResult;

            PrivateType fieldSettingAccessor = new PrivateType(typeof(PasswordFieldSetting));
            string minLengthError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("MinLengthName");
            string maxLengthError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("MaxLengthName");
            string regexError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("RegexName");
            //string compulsoryError = (string)fieldSettingAccessor.GetStaticFieldOrProperty("CompulsoryName");

            //====

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='PasswordTest' type='Password'>
							<Configuration>
								<MinLength>3</MinLength>
								<MaxLength>8</MaxLength>
								<Regex>^[a-zA-Z0-9]*$</Regex>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            ContentTypeInstaller.InstallContentType(ctd);

            SNC.Content content = SNC.Content.CreateNew("ValidatedContent", _testRoot, "abc");

            content["PasswordTest"] = new PasswordField.PasswordData { Text = "ab" };
            isValid = content.IsValid;
            validationResult = content.Fields["PasswordTest"].ValidationResult;
            Assert.IsFalse(isValid, "#1");
            Assert.IsTrue(validationResult.Category == ShortTextFieldSetting.MinLengthName, "#2");
            Assert.IsTrue((int)content.Fields["PasswordTest"].ValidationResult.GetParameter(ShortTextFieldSetting.MinLengthName) == 3, "#3");

            content["PasswordTest"] = new PasswordField.PasswordData { Text = "abcdefgh01" };
            isValid = content.IsValid;
            Assert.IsFalse(isValid, "#4");
            Assert.IsTrue(content.Fields["PasswordTest"].ValidationResult.Category == ShortTextFieldSetting.MaxLengthName, "#5");
            Assert.IsTrue((int)content.Fields["PasswordTest"].ValidationResult.GetParameter(ShortTextFieldSetting.MaxLengthName) == 8, "#6");

            content["PasswordTest"] = new PasswordField.PasswordData { Text = "!@#$%^" };
            isValid = content.IsValid;
            Assert.IsFalse(isValid, "#7");
            Assert.IsTrue(content.Fields["PasswordTest"].ValidationResult.Category == ShortTextFieldSetting.RegexName, "#8");

            content["PasswordTest"] = new PasswordField.PasswordData { Text = "Correct" };
            isValid = content.IsValid;
            Assert.IsTrue(isValid, "#9");
        }


        [TestMethod]
        public void FieldValidation_Reference()
        {
            //Assert.Inconclusive("Missing validation message");

            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ReferredContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ReferenceTest' type='Reference'>
							<Configuration>
								<AllowMultiple>true</AllowMultiple>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");

            var c = SNC.Content.CreateNew("Folder", _testRoot, "RefFieldTests");
            c.Save();
            var testFolder = Node.Load<Folder>(c.Id);
            SNC.Content.CreateNew("ReferredContent", testFolder, "Referred1").Save();
            SNC.Content.CreateNew("ReferredContent", testFolder, "Referred2").Save();
            SNC.Content referrer;

            //==== one, null, no type, no path, no query

            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition' xmlns:q='http://schemas.com/ContentRepository/SearchExpression'>
					<Fields>
						<Field name='ReferenceTest' type='Reference'></Field>
					</Fields>
				</ContentType>");
            referrer = SNC.Content.CreateNew("ValidatedContent", testFolder, "Referrer");
            Assert.IsTrue(referrer.IsValid, "one, null, no type, no path, no query #101");
            referrer["ReferenceTest"] = testFolder.Children.ToArray<Node>()[0];
            Assert.IsTrue(referrer.IsValid, "one, null, no type, no path, no query #102");
            referrer["ReferenceTest"] = testFolder.Children;
            Assert.IsFalse(referrer.IsValid, "one, null, no type, no path, no query #103");

            //==== one, not null, no type, no path, no query

            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition' xmlns:q='http://schemas.com/ContentRepository/SearchExpression'>
					<Fields>
						<Field name='ReferenceTest' type='Reference'>
							<Configuration>
								<Compulsory>true</Compulsory>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            referrer = SNC.Content.CreateNew("ValidatedContent", testFolder, "Referrer");
            Assert.IsFalse(referrer.IsValid, "one, not null, no type, no path, no query #111");
            referrer["ReferenceTest"] = testFolder.Children.ToArray<Node>()[0];
            Assert.IsTrue(referrer.IsValid, "one, not null, no type, no path, no query #112");
            referrer["ReferenceTest"] = testFolder.Children;
            Assert.IsFalse(referrer.IsValid, "one, not null, no type, no path, no query #113");

            //==== multi, null, no type, no path, no query

            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition' xmlns:q='http://schemas.com/ContentRepository/SearchExpression'>
					<Fields>
						<Field name='ReferenceTest' type='Reference'>
							<Configuration>
								<AllowMultiple>true</AllowMultiple>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            referrer = SNC.Content.CreateNew("ValidatedContent", testFolder, "Referrer");
            Assert.IsTrue(referrer.IsValid, "multi, null, no type, no path, no query #121");
            referrer["ReferenceTest"] = testFolder.Children.ToArray<Node>()[0];
            Assert.IsTrue(referrer.IsValid, "multi, null, no type, no path, no query #122");
            referrer["ReferenceTest"] = testFolder.Children;
            Assert.IsTrue(referrer.IsValid, "multi, null, no type, no path, no query #123");

            //==== multi, not null, no type, no path, no query

            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition' xmlns:q='http://schemas.com/ContentRepository/SearchExpression'>
					<Fields>
						<Field name='ReferenceTest' type='Reference'>
							<Configuration>
								<AllowMultiple>true</AllowMultiple>
								<Compulsory>true</Compulsory>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            referrer = SNC.Content.CreateNew("ValidatedContent", testFolder, "Referrer");
            Assert.IsFalse(referrer.IsValid, "multi, not null, no type, no path, no query #131");
            referrer["ReferenceTest"] = testFolder.Children.ToArray<Node>()[0];
            Assert.IsTrue(referrer.IsValid, "multi, not null, no type, no path, no query #132");
            referrer["ReferenceTest"] = testFolder.Children;
            Assert.IsTrue(referrer.IsValid, "multi, not null, no type, no path, no query #133");

            //==== multi, null, type, no path, no query

            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition' xmlns:q='http://schemas.com/ContentRepository/SearchExpression'>
					<Fields>
						<Field name='ReferenceTest' type='Reference'>
							<Configuration>
								<AllowMultiple>true</AllowMultiple>
								<AllowedTypes>
									<Type>ReferredContent</Type>
								</AllowedTypes>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            referrer = SNC.Content.CreateNew("ValidatedContent", testFolder, "Referrer");
            Assert.IsTrue(referrer.IsValid, "multi, null, type, no path, no query #141");
            referrer["ReferenceTest"] = testFolder.Children.ToArray<Node>()[0];
            Assert.IsTrue(referrer.IsValid, "multi, null, type, no path, no query #142");
            referrer["ReferenceTest"] = testFolder.Children;
            Assert.IsTrue(referrer.IsValid, "multi, null, type, no path, no query #143");

            //-- create content with restricted type
            SNC.Content.CreateNew("ValidatedContent", testFolder, "AnotherReferrer").Save();
            referrer["ReferenceTest"] = testFolder.Children;
            Assert.IsFalse(referrer.IsValid, "multi, null, type, no path, no query #144");

            //-- allow ValidatedContent type
            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition' xmlns:q='http://schemas.com/ContentRepository/SearchExpression'>
					<Fields>
						<Field name='ReferenceTest' type='Reference'>
							<Configuration>
								<AllowMultiple>true</AllowMultiple>
								<AllowedTypes>
									<Type>ReferredContent</Type>
									<Type>ValidatedContent</Type>
								</AllowedTypes>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            referrer = SNC.Content.CreateNew("ValidatedContent", testFolder, "Referrer");
            referrer["ReferenceTest"] = testFolder.Children;
            Assert.IsTrue(referrer.IsValid, "multi, null, type, no path, no query #145");

            //==== multi, null, no type, path, no query

            Folder subFolder = new Folder(testFolder);
            subFolder.Name = "SubFolder";
            subFolder.Save();
            string subFolderPath = subFolder.Path;

            SNC.Content.CreateNew("ReferredContent", subFolder, "Referred3").Save();
            SNC.Content.CreateNew("ReferredContent", subFolder, "Referred4").Save();

            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition' xmlns:q='http://schemas.com/ContentRepository/SearchExpression'>
					<Fields>
						<Field name='ReferenceTest' type='Reference'>
							<Configuration>
								<AllowMultiple>true</AllowMultiple>
								<SelectionRoot>
									<Path>" + subFolderPath + @"</Path>
								</SelectionRoot>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            referrer = SNC.Content.CreateNew("ValidatedContent", testFolder, "Referrer");
            Assert.IsTrue(referrer.IsValid, "multi, null, no type, path, no query #151");
            referrer["ReferenceTest"] = testFolder.Children.ToArray<Node>()[0];
            Assert.IsFalse(referrer.IsValid, "multi, null, no type, path, no query #152");
            referrer["ReferenceTest"] = testFolder.Children;
            Assert.IsFalse(referrer.IsValid, "multi, null, no type, path, no query #153");
            referrer["ReferenceTest"] = subFolder.Children.ToArray<Node>()[0];
            Assert.IsTrue(referrer.IsValid, "multi, null, no type, path, no query #154");
            referrer["ReferenceTest"] = subFolder.Children;
            Assert.IsTrue(referrer.IsValid, "multi, null, no type, path, no query #155");

            //==== multi, null, no type, no path, query

            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition' xmlns:q='http://schemas.com/ContentRepository/SearchExpression'>
					<Fields>
						<Field name='ReferenceTest' type='Reference'>
							<Configuration>
								<AllowMultiple>true</AllowMultiple>
								<Query>
									<q:And>
										<q:String op='EndsWith' property='Name'>2</q:String>
									</q:And>
								</Query>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");

            var goodList = NodeQuery.Parse(@"<q:SearchExpression xmlns:q='http://schemas.com/ContentRepository/SearchExpression'>
					<q:And>
						<q:String op='EndsWith' property='Name'>2</q:String>
					</q:And>
				</q:SearchExpression>").Execute().Nodes;
            referrer = SNC.Content.CreateNew("ValidatedContent", testFolder, "Referrer");
            Assert.IsTrue(referrer.IsValid, "multi, null, no type, path, no query #161");
            referrer["ReferenceTest"] = testFolder.Children;
            Assert.IsFalse(referrer.IsValid, "multi, null, no type, path, no query #162");
            referrer["ReferenceTest"] = subFolder.Children;
            Assert.IsFalse(referrer.IsValid, "multi, null, no type, path, no query #163");
            referrer["ReferenceTest"] = goodList;
            Assert.IsTrue(referrer.IsValid, "multi, null, no type, path, no query #164");

            Node node = Node.LoadNode(123);

        }

        [TestMethod]
        public void FieldValidation_Choice()
        {
            string ctd; string loadValue; string setExtraValue;
            ListItemCollection loadedList;
            string result;
            bool isValid;

            //---------------------------------------------- multiple and extra

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ChoiceTest' type='Choice'>
							<Configuration>
								<AllowExtraValue>true</AllowExtraValue>
								<AllowMultiple>true</AllowMultiple>
								<Options>
									<Option value='1'>text1</Option>
									<Option value='2'>text2</Option>
									<Option value='3'>text3</Option>
									<Option value='4'>text4</Option>
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            loadValue = "2;3;" + ChoiceField.EXTRAVALUEPREFIX + "TwentySeven";
            setExtraValue = "SeventyTwo";

            ChoiceTestOnString(ctd, loadValue, setExtraValue, out loadedList, out result, out isValid);

            Assert.IsTrue(!loadedList[0].Selected && loadedList[1].Selected && loadedList[2].Selected && !loadedList[3].Selected, "#1");
            Assert.IsTrue(result == "1;4;" + ChoiceField.EXTRAVALUEPREFIX + "SeventyTwo", "#2");
            Assert.IsTrue(isValid, "#3");

            //---------------------------------------------- multiple and denied extra

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ChoiceTest' type='Choice'>
							<Configuration>
								<AllowExtraValue>true</AllowExtraValue>
								<Options>
									<Option value='1'>text1</Option>
									<Option value='2'>text2</Option>
									<Option value='3'>text3</Option>
									<Option value='4'>text4</Option>
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            loadValue = "2;3;TwentySeven";
            setExtraValue = "SeventyTwo";

            ChoiceTestOnString(ctd, loadValue, setExtraValue, out loadedList, out result, out isValid);

            Assert.IsTrue(result == "1;4;" + ChoiceField.EXTRAVALUEPREFIX + "SeventyTwo", "#2");
            Assert.IsFalse(isValid, "#6");

            //---------------------------------------------- multiple without extra

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ChoiceTest' type='Choice'>
							<Configuration>
								<AllowMultiple>true</AllowMultiple>
								<Options>
									<Option value='1'>text1</Option>
									<Option value='2'>text2</Option>
									<Option value='3'>text3</Option>
									<Option value='4'>text4</Option>
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            loadValue = "2;3";
            setExtraValue = null;

            ChoiceTestOnString(ctd, loadValue, setExtraValue, out loadedList, out result, out isValid);

            Assert.IsTrue(result == "1;4", "#8");
            Assert.IsTrue(isValid, "#9");

            //---------------------------------------------- multiple value but multiple is denied

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ChoiceTest' type='Choice'>
							<Configuration>
								<AllowExtraValue>true</AllowExtraValue>
								<Options>
									<Option value='1'>text1</Option>
									<Option value='2'>text2</Option>
									<Option value='3'>text3</Option>
									<Option value='4'>text4</Option>
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            loadValue = "2;3;" + ChoiceField.EXTRAVALUEPREFIX + "TwentySeven";
            setExtraValue = null;

            ChoiceTestOnString(ctd, loadValue, setExtraValue, out loadedList, out result, out isValid);

            Assert.IsTrue(result == "1;4", "#11");
            Assert.IsFalse(isValid, "#12");

            //---------------------------------------------- single extra

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ChoiceTest' type='Choice'>
							<Configuration>
								<AllowExtraValue>true</AllowExtraValue>
								<Options>
									<Option value='1'>text1</Option>
									<Option value='2'>text2</Option>
									<Option value='3'>text3</Option>
									<Option value='4'>text4</Option>
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            loadValue = "1;2;3;4";
            setExtraValue = "SeventyTwo";

            ChoiceTestOnString(ctd, loadValue, setExtraValue, out loadedList, out result, out isValid);

            Assert.IsTrue(result == ChoiceField.EXTRAVALUEPREFIX + "SeventyTwo", "#13");
            Assert.IsTrue(isValid, "#14");

            //---------------------------------------------- single 

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ChoiceTest' type='Choice'>
							<Configuration>
								<Options>
									<Option value='1'>text1</Option>
									<Option value='2'>text2</Option>
									<Option value='3'>text3</Option>
									<Option value='4'>text4</Option>
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            loadValue = "1;2;3";
            setExtraValue = null;

            ChoiceTestOnString(ctd, loadValue, setExtraValue, out loadedList, out result, out isValid);

            Assert.IsTrue(result == "4", "#15");
            Assert.IsTrue(isValid, "#16");

            //---------------------------------------------- compulsory 

            ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ChoiceTest' type='Choice'>
							<Configuration>
								<Compulsory>true</Compulsory>
								<Options>
									<Option value='1'>text1</Option>
									<Option value='2'>text2</Option>
									<Option value='3'>text3</Option>
									<Option value='4'>text4</Option>
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            loadValue = "1;2;3;4";
            setExtraValue = null;

            ChoiceTestOnString(ctd, loadValue, setExtraValue, out loadedList, out result, out isValid);

            Assert.IsTrue(result == "", "#15");
            Assert.IsFalse(isValid, "#16");

        }
        private void ChoiceTestOnString(string ctd, string loadValue, string setExtraValue, out ListItemCollection loadedList, out string result, out bool isValid)
        {
            ContentTypeInstaller.InstallContentType(ctd);

            SNC.Content content = SNC.Content.CreateNew("ValidatedContent", _testRoot, "ChoiceTest");
            content.ContentHandler["ChoiceTest"] = loadValue;

            //-- simulate ContentView
            ChoiceField field = (ChoiceField)content.Fields["ChoiceTest"];
            Portal.UI.Controls.CheckBoxGroup chk = new Portal.UI.Controls.CheckBoxGroup();
            PrivateObject chkAcc = new PrivateObject(chk);
            chkAcc.Invoke("set_Field", content.Fields["ChoiceTest"]);
            chk.SetData(field.GetData());
            ListItemCollection items = (ListItemCollection)chkAcc.GetProperty("InnerListItemCollection");

            loadedList = new ListItemCollection();
            foreach (ListItem item in items)
            {
                ListItem newItem = new ListItem(item.Text, item.Value, item.Enabled);
                newItem.Selected = item.Selected;
                loadedList.Add(newItem);
            }

            var extraOptionValue = (string)chkAcc.Invoke("get_ExtraOptionValue");
            ListItem extraItem = null;

            //-- return inverted selection
            foreach (ListItem item in items)
            {
                item.Selected = !item.Selected;
                if (item.Value == extraOptionValue)
                    extraItem = item;
            }
            if (setExtraValue != null)
            {
                chkAcc.Invoke("SetExtraValue", setExtraValue);
                if (extraItem != null)
                    extraItem.Selected = true;
            }
            field.SetData(chk.GetData());
            new PrivateObject(field).Invoke("Save", false);

            result = (string)content.ContentHandler["ChoiceTest"];
            isValid = field.IsValid;
        }

        [TestMethod]
        public void FieldValidation_ChoiceOnEnum()
        {
            string ctd = @"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='EnumTestNode' parentType='GenericContent' handler='ContentRepository.Tests.ContentHandlers.EnumTestNode' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='TestProperty' type='Choice'>
							<Configuration>
								<Compulsory>true</Compulsory>
								<Options>
									<Enum type='ContentRepository.Tests.ContentHandlers.EnumTestNode+TestEnum' />
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>";
            var loadValue = EnumTestNode.TestEnum.Value1;
            var selectValue = EnumTestNode.TestEnum.Value3;
            ListItemCollection loadedList;
            EnumTestNode.TestEnum result;
            bool isValid;

            ChoiceTestOnEnum(ctd, loadValue, selectValue, out loadedList, out result, out isValid);

            Assert.IsTrue(result == selectValue, "#1");
            Assert.IsTrue(isValid, "#2");
        }
        private void ChoiceTestOnEnum(string ctd, EnumTestNode.TestEnum loadValue, EnumTestNode.TestEnum selectValue, out ListItemCollection loadedList, out EnumTestNode.TestEnum result, out bool isValid)
        {
            ContentTypeInstaller.InstallContentType(ctd);

            SNC.Content content = SNC.Content.CreateNew("EnumTestNode", _testRoot, "ChoiceTest");
            var contentHandler = (EnumTestNode)content.ContentHandler;
            contentHandler.TestProperty = loadValue;

            //-- simulate ContentView
            ChoiceField field = (ChoiceField)content.Fields["TestProperty"];
            Portal.UI.Controls.CheckBoxGroup chk = new Portal.UI.Controls.CheckBoxGroup();
            PrivateObject chkAcc = new PrivateObject(chk);
            chkAcc.Invoke("set_Field", content.Fields["TestProperty"]);

            chk.SetData(field.GetData());
            ListItemCollection items = (ListItemCollection)chkAcc.GetProperty("InnerListItemCollection");

            loadedList = new ListItemCollection();
            foreach (ListItem item in items)
            {
                ListItem newItem = new ListItem(item.Text, item.Value, item.Enabled);
                newItem.Selected = item.Selected;
                loadedList.Add(newItem);
            }

            var extraOptionValue = (string)chkAcc.Invoke("get_ExtraOptionValue");

            //-- select item
            var selectedEnumValue = ((int)selectValue).ToString();
            foreach (ListItem item in items)
                item.Selected = item.Value == selectedEnumValue;

            field.SetData(chk.GetData());
            new PrivateObject(field).Invoke("Save", false);

            result = (EnumTestNode.TestEnum)contentHandler.TestProperty;
            isValid = field.IsValid;
        }

        [TestMethod]
        public void FieldValidation_Integer()
        {
            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='IntegerTest' type='Integer'>
							<Configuration>
								<MinValue>-6</MinValue>
								<MaxValue>42</MaxValue>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");

            SNC.Content content = SNC.Content.CreateNew("ValidatedContent", _testRoot, "abc");
            Assert.IsTrue((int)content["IntegerTest"] == 0, "#1");
            content["IntegerTest"] = -7;
            Assert.IsFalse(content.IsValid, "#2");
            content["IntegerTest"] = -6;
            Assert.IsTrue(content.IsValid, "#3");
            content["IntegerTest"] = 0;
            Assert.IsTrue(content.IsValid, "#4");
            content["IntegerTest"] = 41;
            Assert.IsTrue(content.IsValid, "#5");
            content["IntegerTest"] = 42;
            Assert.IsTrue(content.IsValid, "#6");
            content["IntegerTest"] = 43;
            Assert.IsFalse(content.IsValid, "#7");

            //-- simulate ContentView
            content = SNC.Content.CreateNew("ValidatedContent", _testRoot, "abc");
            content.ContentHandler["IntegerTest"] = 7;

            IntegerField field = (IntegerField)content.Fields["IntegerTest"];
            Portal.UI.Controls.WholeNumber intEd = new Portal.UI.Controls.WholeNumber();
            PrivateObject intEdAcc = new PrivateObject(intEd);
            intEdAcc.Invoke("set_Field", content.Fields["IntegerTest"]);
            intEd.SetData(field.GetData());

            TextBox textBox = (TextBox)intEdAcc.GetField("_inputTextBox");
            Assert.IsTrue(textBox.Text == "7", "#8");
            //--
            textBox.Text = "14";
            field.SetData(intEd.GetData());
            new PrivateObject(field).Invoke("Save", false);
            int result = (int)content.ContentHandler["IntegerTest"];
            bool isValid = field.IsValid;
            Assert.IsTrue(result == 14, "#9");
            Assert.IsTrue(isValid, "#10");
            //--
            textBox.Text = "50";
            field.SetData(intEd.GetData());
            new PrivateObject(field).Invoke("Save", false);
            result = (int)content.ContentHandler["IntegerTest"];
            isValid = field.IsValid;
            Assert.IsTrue(result == 50, "#9");
            Assert.IsFalse(isValid, "#10");
            //--
            textBox.Text = "";
            field.SetData(intEd.GetData());
            new PrivateObject(field).Invoke("Save", false);
            result = (int)content.ContentHandler["IntegerTest"];
            isValid = field.IsValid;
            Assert.IsTrue(result == 0, "#11");
            Assert.IsTrue(isValid, "#12");
        }

        [TestMethod]
        public void FieldValidation_Number()
        {
            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='NumberTest' type='Number'>
							<Configuration>
								<MinValue>-6.0</MinValue>
								<MaxValue>42.1</MaxValue>
								<Digits>1</Digits>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");

            SNC.Content content = SNC.Content.CreateNew("ValidatedContent", _testRoot, "abc");
            Assert.IsTrue((decimal)content["NumberTest"] == 0.0m, "#1");
            content["NumberTest"] = -6.99m;
            Assert.IsFalse(content.IsValid, "#2");
            content["NumberTest"] = -6.0m;
            Assert.IsTrue(content.IsValid, "#3");
            content["NumberTest"] = 0.0m;
            Assert.IsTrue(content.IsValid, "#4");
            content["NumberTest"] = 41.0m;
            Assert.IsTrue(content.IsValid, "#5");
            content["NumberTest"] = 42.0m;
            Assert.IsTrue(content.IsValid, "#6");
            content["NumberTest"] = 42.11m;
            Assert.IsFalse(content.IsValid, "#7");

            //-- simulate ContentView
            content = SNC.Content.CreateNew("ValidatedContent", _testRoot, "abc");
            content.ContentHandler["NumberTest"] = 6.999m;

            NumberField field = (NumberField)content.Fields["NumberTest"];
            Portal.UI.Controls.Number numberEd = new Portal.UI.Controls.Number();
            PrivateObject numberEdAcc = new PrivateObject(numberEd);
            numberEdAcc.Invoke("set_Field", content.Fields["NumberTest"]);
            numberEd.SetData(field.GetData());

            TextBox textBox = (TextBox)numberEdAcc.GetField("_inputTextBox");
            Assert.IsTrue(textBox.Text.Replace(",", ".") == "7.0", "#8");
            //--
            textBox.Text = "14";
            field.SetData(numberEd.GetData());
            new PrivateObject(field).Invoke("Save", false);
            decimal result = (decimal)content.ContentHandler["NumberTest"];
            bool isValid = field.IsValid;
            Assert.IsTrue(result == 14m, "#9");
            Assert.IsTrue(isValid, "#10");
            //--
            textBox.Text = "50";
            field.SetData(numberEd.GetData());
            new PrivateObject(field).Invoke("Save", false);
            result = (decimal)content.ContentHandler["NumberTest"];
            isValid = field.IsValid;
            Assert.IsTrue(result == 50, "#11");
            Assert.IsFalse(isValid, "#12");
            //--
            textBox.Text = "";
            field.SetData(numberEd.GetData());
            new PrivateObject(field).Invoke("Save", false);
            result = (decimal)content.ContentHandler["NumberTest"];
            isValid = field.IsValid;
            Assert.IsTrue(result == 0, "#13");
            Assert.IsTrue(isValid, "#14");
        }

        [TestMethod]
        public void FieldValidation_Inheritance()
        {
            //Assert.Inconclusive("Missing validation message");

            ContentType c = ContentType.GetByName("CT_Root");
            if (c != null)
                ContentTypeInstaller.RemoveContentType(c);
            ContentTypeManager.Reset();

            ContentTypeInstaller installer = ContentTypeInstaller.CreateBatchContentTypeInstaller();

            installer.AddContentType(@"<ContentType name='CT_Root' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='InheritanceTest' type='Integer'>
							<Configuration>
								<MinValue>1</MinValue>
								<MaxValue>7</MaxValue>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            installer.AddContentType(@"<ContentType name='CT_A' parentType='CT_Root' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='InheritanceTest' type='Integer'><!-- override='true' -->
							<Configuration>
								<MinValue>4</MinValue>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            installer.AddContentType(@"<ContentType name='CT_A_A' parentType='CT_A' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='InheritanceTest' type='Integer'><!-- override='true' -->
							<Configuration handler='ContentRepository.Tests.Schema.CustomFieldSetting'>
								<EvenOnly>true</EvenOnly>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            installer.AddContentType(@"<ContentType name='CT_A_A_A' parentType='CT_A_A' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
				</ContentType>");
            installer.AddContentType(@"<ContentType name='CT_A_A_A_A' parentType='CT_A_A_A' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='InheritanceTest' type='Integer'><!-- override='true' -->
							<Configuration>
								<MinValue>-5</MinValue>
								<MaxValue>-1</MaxValue>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            installer.ExecuteBatch();

            //                             Configuration                    Assert
            //Type                         min        max        even       -6 -5 -4 -3 -2 -1  0  1  2  3  4  5  6  7  8
            //CT_ROOT                      1          7          -           F  F  F  F  F  F  F  T  T  T  T  T  T  T  F
            //    CT_A                     4          null       -           F  F  F  F  F  F  F  F  F  F  T  T  T  T  F
            //        CT_A_A               null       null       true        F  F  F  F  F  F  F  F  F  F  T  F  T  F  F
            //            CT_A_A_A         null       null       null        F  F  F  F  F  F  F  F  F  F  T  F  T  F  F
            //                CT_A_A_A_A   -5         -1         -           F  T  T  T  T  T  F  F  F  F  F  F  F  F  F

            string[] typeNames = new string[] { "CT_Root", "CT_A", "CT_A_A", "CT_A_A_A", "CT_A_A_A_A" };
            bool[][] expected = new bool[][]{
			    new bool[] {false,  false,  false,  false,  false,  false,  false,  true,  true,  true,  true,  true,  true,  true, false},
			    new bool[] {false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true,  true,  true,  true,  false},
			    new bool[] {false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true,  false,  true,  false,  false},
			    new bool[] {false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true,  false,  true,  false,  false},
			    new bool[] {false,  true,  true,  true,  true,  true,  false,  false,  false,  false,  false,  false,  false,  false,  false}
			};
            bool[][] validity = new bool[][] { new bool[15], new bool[15], new bool[15], new bool[15], new bool[15] };

            SNC.Content content = null;
            for (int i = 0; i < typeNames.Length; i++)
            {
                content = SNC.Content.CreateNew(typeNames[i], _testRoot, typeNames[i] + "_Instance");
                for (int value = -6; value < 9; value++)
                {
                    content["InheritanceTest"] = value;
                    validity[i][value + 6] = content.IsValid;
                }
            }

            //----------------------

            ContentTypeInstaller.RemoveContentType(ContentType.GetByName("CT_Root"));
            ContentTypeManager.Reset();

            //----------------------

            for (int i = 0; i < typeNames.Length; i++)
                for (int j = 0; j < 15; j++)
                    Assert.IsTrue(validity[i][j] == expected[i][j], String.Concat("#type: ", typeNames[i], ", value: ", j - 6));
        }

        //======================================================================================= Custom Validation tests

        [TestMethod]
        public void FieldValidation_Custom()
        {
            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='IntegerTest' type='Integer'>
							<Configuration>
								<MinValue>-5</MinValue>
								<MaxValue>7</MaxValue>
								<EvenOnly>true</EvenOnly>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");
            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ValidatedContent' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='IntegerTest' type='Integer'>
							<Configuration handler='ContentRepository.Tests.Schema.CustomFieldSetting'>
								<MinValue>-5</MinValue>
								<MaxValue>7</MaxValue>
								<EvenOnly>true</EvenOnly>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");

            SNC.Content content = SNC.Content.CreateNew("ValidatedContent", _testRoot, "abc");
            Assert.IsTrue((int)content["IntegerTest"] == 0, "#1");
            content["IntegerTest"] = -6; Assert.IsFalse(content.IsValid, "#-6");
            content["IntegerTest"] = -5; Assert.IsFalse(content.IsValid, "#-5");
            content["IntegerTest"] = -4; Assert.IsTrue(content.IsValid, "#-4");
            content["IntegerTest"] = -3; Assert.IsFalse(content.IsValid, "#-3");
            content["IntegerTest"] = -2; Assert.IsTrue(content.IsValid, "#-2");
            content["IntegerTest"] = -1; Assert.IsFalse(content.IsValid, "#-1");
            content["IntegerTest"] = 0; Assert.IsTrue(content.IsValid, "#0");
            content["IntegerTest"] = 1; Assert.IsFalse(content.IsValid, "#1");
            content["IntegerTest"] = 2; Assert.IsTrue(content.IsValid, "#2");
            content["IntegerTest"] = 3; Assert.IsFalse(content.IsValid, "#3");
            content["IntegerTest"] = 4; Assert.IsTrue(content.IsValid, "#4");
            content["IntegerTest"] = 5; Assert.IsFalse(content.IsValid, "#5");
            content["IntegerTest"] = 6; Assert.IsTrue(content.IsValid, "#6");
            content["IntegerTest"] = 7; Assert.IsFalse(content.IsValid, "#7");
            content["IntegerTest"] = 8; Assert.IsFalse(content.IsValid, "#8");

        }

        //======================================================================================= ToXml tests

        [TestMethod]
        public void FieldSetting_ToXml_Integer()
        {
            var field1 =
                @"<Field name=""TestField1"" type=""Integer"">
<DisplayName>Test name</DisplayName>
<Description>Test name for the content field</Description>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration>
<ReadOnly>true</ReadOnly>
<DefaultValue>33</DefaultValue>
</Configuration>
</Field>";
            var field2 =
                @"<Field name=""TestField2"" type=""Integer"">
<DisplayName>Test name #2</DisplayName>
<Description></Description>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration>
<MinValue>10</MinValue>
<MaxValue>99</MaxValue>
</Configuration>
</Field>";

            var contentType = CreateContentType(new [] { field1, field2 });
            var fs1 = contentType.GetFieldSettingByName("TestField1");
            var fs2 = contentType.GetFieldSettingByName("TestField2");
            var xml1 = fs1.ToXml();
            var xml2 = fs2.ToXml();

            Assert.IsTrue(CompareFieldXmls(field1, xml1));
            Assert.IsTrue(CompareFieldXmls(field2, xml2));
        }

        [TestMethod]
        public void FieldSetting_ToXml_Number()
        {
            var field1 =
                @"<Field name=""TestField1"" type=""Number"">
<DisplayName>Test name</DisplayName>
<Description>Test name for the content field</Description>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration>
<Compulsory>true</Compulsory>
</Configuration>
</Field>";
            var field2 =
                @"<Field name=""TestField2"" type=""Number"">
<DisplayName>Test name #2</DisplayName>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration>
<MinValue>10.0</MinValue>
<MaxValue>99.0</MaxValue>
<Digits>2</Digits>
</Configuration>
</Field>";

            var contentType = CreateContentType(new [] { field1, field2 });
            var fs1 = contentType.GetFieldSettingByName("TestField1");
            var fs2 = contentType.GetFieldSettingByName("TestField2");
            var xml1 = fs1.ToXml();
            var xml2 = fs2.ToXml();

            Assert.IsTrue(CompareFieldXmls(field1, xml1));
            Assert.IsTrue(CompareFieldXmls(field2, xml2));
        }

        [TestMethod]
        public void FieldSetting_ToXml_Reference()
        {
            var field1 = @"<Field name=""TestField1"" type=""Reference"">
<DisplayName>Test name</DisplayName>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration />
</Field>";
            var field2 =
                @"<Field name=""TestField2"" type=""Reference"">
<DisplayName>Test name #2</DisplayName>
<Description>Test name for the content field</Description>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration>
<AllowMultiple>false</AllowMultiple>
<AllowedTypes>
<Type>PageTemplate</Type>
</AllowedTypes>
<SelectionRoot>
<Path>/Root/System/Schema/ContentTypes</Path>
</SelectionRoot>
</Configuration>
</Field>";

            var contentType = CreateContentType(new[] { field1, field2 });
            var fs1 = contentType.GetFieldSettingByName("TestField1");
            var fs2 = contentType.GetFieldSettingByName("TestField2");
            var xml1 = fs1.ToXml();
            var xml2 = fs2.ToXml();

            Assert.IsTrue(CompareFieldXmls(field1, xml1));
            Assert.IsTrue(CompareFieldXmls(field2, xml2));
        }

        [TestMethod]
        public void FieldSetting_ToXml_ShortText()
        {
            var field1 =
                @"<Field name=""TestField1"" type=""ShortText"">
<DisplayName>Test name</DisplayName>
<Description>Test name for the content field</Description>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration />
</Field>";
            var field2 =
                @"<Field name=""TestField2"" type=""ShortText"">
<DisplayName>Test name #2</DisplayName>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration>
<MinLength>5</MinLength>
<MaxLength>300</MaxLength>
<Regex>d+</Regex>
</Configuration>
</Field>";

            var contentType = CreateContentType(new[] { field1, field2 });
            var fs1 = contentType.GetFieldSettingByName("TestField1");
            var fs2 = contentType.GetFieldSettingByName("TestField2");
            var xml1 = fs1.ToXml();
            var xml2 = fs2.ToXml();

            Assert.IsTrue(CompareFieldXmls(field1, xml1));
            Assert.IsTrue(CompareFieldXmls(field2, xml2));
        }

        [TestMethod]
        public void FieldSetting_ToXml_Choice()
        {
            var field1 =
                @"<Field name=""TestField1"" type=""Choice"">
<DisplayName>Test name #1</DisplayName>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration>
<AllowMultiple>true</AllowMultiple>
<AllowExtraValue>false</AllowExtraValue>
<Options>
<Option value=""opt1"" selected=""true"">Text1</Option>
<Option value=""opt2"" enabled=""false"">Text2</Option>
</Options>
</Configuration>
</Field>";

            var contentType = CreateContentType(new[] { field1 });
            var fs1 = contentType.GetFieldSettingByName("TestField1");
            var xml1 = fs1.ToXml();

            Assert.IsTrue(CompareFieldXmls(field1, xml1));
        }

        [TestMethod]
        public void FieldSetting_ToXml_Password()
        {
            var field1 =
                @"<Field name=""TestField1"" type=""Password"">
<DisplayName>Test name</DisplayName>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration>
<ReenterTitle>Re-enter password</ReenterTitle>
<ReenterDescription>Re-enter password</ReenterDescription>
</Configuration>
</Field>";

            var contentType = CreateContentType(new[] { field1 });
            var fs1 = contentType.GetFieldSettingByName("TestField1");
            var xml1 = fs1.ToXml();

            Assert.IsTrue(CompareFieldXmls(field1, xml1));
        }

        [TestMethod]
        public void FieldSetting_ToXml_Binary()
        {
            var field1 =
                @"<Field name=""TestField1"" type=""Binary"">
<DisplayName>Test name</DisplayName>
<Indexing><IndexHandler>Search.Indexing.LowerStringIndexHandler</IndexHandler></Indexing>
<Configuration>
<IsText>true</IsText>
</Configuration>
</Field>";

            var contentType = CreateContentType(new [] { field1 });
            var fs1 = contentType.GetFieldSettingByName("TestField1");
            var xml1 = fs1.ToXml();

            Assert.IsTrue(CompareFieldXmls(field1, xml1));
        }

        private ContentType CreateContentType(string[] fieldXmlFragments)
        {
            var ctd = string.Format(@"<?xml version='1.0' encoding='utf-8'?>
							<ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
								<Fields>
									{0}
								</Fields>
							</ContentType>", string.Join("", fieldXmlFragments));

            InstallContentType(ctd, null);
            return ContentTypeManager.Current.GetContentTypeByName("FieldSetting_Structure");
        }

        private bool CompareFieldXmls(string fieldXml1, string fieldXml2)
        {
            return fieldXml1.Replace(Environment.NewLine, "").CompareTo(fieldXml2.Replace(Environment.NewLine, "")) == 0;
        }

        //======================================================================================= Analyzer definition tests

        [TestMethod]
        public void FieldSetting_Analyzer()
        {
            var start = ContentTypeManager.Current;

            var globalAnalyzersBefore = StorageContext.Search.SearchEngine.GetAnalyzers();

            string ctd = @"<?xml version='1.0' encoding='utf-8'?>
                <ContentType name='FieldSetting_Analyzer' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
                    <Fields>
                        <Field name='TestString1' type='ShortText'>
                            <Indexing>
                                <Analyzer>Lucene.Net.Analysis.Standard.StandardAnalyzer</Analyzer>
                            </Indexing>
                        </Field>
                        <Field name='TestString2' type='ShortText'>
                            <Indexing>
                                <Analyzer>Lucene.Net.Analysis.WhitespaceAnalyzer</Analyzer>
                            </Indexing>
                        </Field>
                    </Fields>
                </ContentType>";
            ContentTypeInstaller.InstallContentType(ctd);

            var restart = ContentTypeManager.Current;

            var globalAnalyzersAfter = StorageContext.Search.SearchEngine.GetAnalyzers();

            var contentType = ContentType.GetByName("FieldSetting_Analyzer");
            var analyzer0 = contentType.GetFieldSettingByName("TestString1").IndexingInfo.Analyzer;
            var analyzer1 = contentType.GetFieldSettingByName("TestString2").IndexingInfo.Analyzer;
            Assert.IsTrue(analyzer0 == "Lucene.Net.Analysis.Standard.StandardAnalyzer", "#01");
            Assert.IsTrue(analyzer1 == "Lucene.Net.Analysis.WhitespaceAnalyzer", "#02");

            var analyzerInfo = StorageContext.Search.SearchEngine.GetAnalyzers();
            Assert.IsTrue(analyzerInfo.ContainsKey("TestString1"), "#03");
            Assert.IsTrue(analyzerInfo["TestString1"].FullName == "Lucene.Net.Analysis.Standard.StandardAnalyzer", "#04");
            Assert.IsTrue(analyzerInfo.ContainsKey("TestString2"), "#05");
            Assert.IsTrue(analyzerInfo["TestString2"].FullName == "Lucene.Net.Analysis.WhitespaceAnalyzer", "#06");

            var content = Content.CreateNew("FieldSetting_Analyzer", _testRoot, "test");
            content["TestString1"] = "asdf,qwer,zxcv";
            content.Save();

            var query = new NodeQuery(new StringExpression(ActiveSchema.PropertyTypes["TestString1"], StringOperator.Equal, "qwer"));
            var result = query.Execute();

            Assert.IsTrue(result.Count > 0, "#11");
        }
        [TestMethod]
        public void FieldSetting_Analyzer_TryOverride()
        {
            string badFieldName = null;
            try
            {
                string ctd = @"<?xml version='1.0' encoding='utf-8'?>
                    <ContentType name='FieldSetting_Structure1' parentType='GenericContent' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
                        <Fields>
                            <Field name='TestString1' type='ShortText'>
                                <Indexing>
                                    <Analyzer>Lucene.Net.Analysis.Standard.StandardAnalyzer</Analyzer>
                                </Indexing>
                            </Field>
                            <Field name='TestString2' type='ShortText'>
                                <Indexing>
                                    <Analyzer>Lucene.Net.Analysis.WhitespaceAnalyzer</Analyzer>
                                </Indexing>
                            </Field>
                        </Fields>
                    </ContentType>";
                string ctd2 = @"<?xml version='1.0' encoding='utf-8'?>
                    <ContentType name='FieldSetting_Structure2' parentType='FieldSetting_Structure1' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
                        <Fields>
                            <Field name='TestString1' type='ShortText'>
                                <Indexing>
                                    <Analyzer>Lucene.Net.Analysis.KeywordAnalyzer</Analyzer>
                                </Indexing>
                            </Field>
                            <Field name='TestString2' type='ShortText'>
                                <Indexing>
                                    <Analyzer>Lucene.Net.Analysis.WhitespaceAnalyzer</Analyzer>
                                </Indexing>
                            </Field>
                        </Fields>
                    </ContentType>";
                ContentTypeInstaller.InstallContentType(ctd);
                ContentTypeInstaller.InstallContentType(ctd2);
                //var analyzerInfo = StorageContext.Search.SearchEngine.GetAnalyzers();
                var contentType = ContentType.GetByName("FieldSetting_Structure1");
            }
            catch (ContentRegistrationException e)
            {
                badFieldName = e.FieldName;
            }

            var typeSystemIsCorrupt = false;
            try
            {
                ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
                    <ContentType name='FieldSetting_Structure2' parentType='FieldSetting_Structure1' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
                        <Fields>
                            <Field name='TestString1' type='ShortText'/>
                            <Field name='TestString2' type='ShortText'/>
                        </Fields>
                    </ContentType>");
            }
            catch
            {
                typeSystemIsCorrupt = true;
            }

            Assert.IsTrue(badFieldName != null, "Expected ContentRegistrationException was not thrown.");
            Assert.IsTrue(badFieldName == "TestString1", String.Concat("badFieldName is ", badFieldName, ". Expected: TestString1"));
            Assert.IsFalse(typeSystemIsCorrupt, "FieldSetting_Structure1 CTD is corrupt");
        }
        [TestMethod]
        public void FieldSetting_IndexingInfo_TryOverride()
        {
            bool exceptionWasThrown = false;
            try
            {
                string ctd = @"<?xml version='1.0' encoding='utf-8'?>
                <ContentType name='FieldSetting_Structure' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
                    <Fields>
                        <Field name='Id' type='Integer'>
                            <Indexing>
                                <!--<Mode>Analyzed</Mode>-->
                                <Store>No</Store>
                            </Indexing>
                        </Field>
                    </Fields>
                </ContentType>";
                ContentTypeInstaller.InstallContentType(ctd);
            }
            catch (ContentRegistrationException e)
            {
                exceptionWasThrown = true;
            }

            //var idSettings = ContentType.GetByName("FieldSetting_Structure").GetFieldSettingByName("Id");
            //var im = idSettings.IndexingInfo.IndexingMode;
            //var ism = idSettings.IndexingInfo.IndexStoringMode;
            //var itv = idSettings.IndexingInfo.TermVectorStoringMode;

            Assert.IsTrue(exceptionWasThrown, "Expected ContentRegistrationException was not thrown.");

        }

        //======================================================================================= Tools

        private string InstallContentType(string contentTypeDefInstall, string contentTypeDefModify)
        {
            SchemaEditor ed1 = new SchemaEditor();
            SchemaEditor ed2 = new SchemaEditor();

            ContentTypeManagerAccessor ctmAcc = new ContentTypeManagerAccessor(ContentTypeManager.Current);
            ContentType cts = ctmAcc.LoadOrCreateNew(contentTypeDefInstall);
            ctmAcc.ApplyChangesInEditor(cts, ed2);
            cts.Save(false);
            ContentTypeManager.Current.AddContentType(cts);

            SchemaEditorAccessor ed2Acc = new SchemaEditorAccessor(ed2);
            TestSchemaWriter wr = new TestSchemaWriter();
            ed2Acc.RegisterSchema(ed1, wr);

            if (contentTypeDefModify != null)
            {
                //-- Id-k beallitasa es klonozas
                SchemaEditor ed3 = new SchemaEditor();
                SchemaEditorAccessor ed3Acc = new SchemaEditorAccessor(ed3);
                SchemaItemAccessor schItemAcc;
                int id = 1;
                foreach (PropertyType pt in ed2.PropertyTypes)
                {
                    PropertyType clone = ed3.CreatePropertyType(pt.Name, pt.DataType, pt.Mapping);
                    schItemAcc = new SchemaItemAccessor(pt);
                    schItemAcc.Id = id++;
                    schItemAcc = new SchemaItemAccessor(clone);
                    schItemAcc.Id = pt.Id;
                }
                id = 1;
                foreach (NodeType nt in ed2.NodeTypes)
                {
                    NodeType clone = ed3.CreateNodeType(nt.Parent, nt.Name, nt.ClassName);
                    foreach (PropertyType pt in nt.PropertyTypes)
                        ed3.AddPropertyTypeToPropertySet(ed3.PropertyTypes[pt.Name], clone);
                    schItemAcc = new SchemaItemAccessor(nt);
                    schItemAcc.Id = id++;
                    schItemAcc = new SchemaItemAccessor(clone);
                    schItemAcc.Id = nt.Id;
                }

                cts = ctmAcc.LoadOrCreateNew(contentTypeDefModify);
                ctmAcc.ApplyChangesInEditor(cts, ed3);
                wr = new TestSchemaWriter();
                ed3Acc.RegisterSchema(ed2, wr);
            }

            return wr.Log;
        }
    }
}
