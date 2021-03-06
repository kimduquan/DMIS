using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using ContentRepository.Schema;
using ContentRepository.Storage;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Search;

namespace ContentRepository.Tests.Schema
{
	[TestClass()]
    public class ContentTypeInheritanceTest : TestBase
	{
		#region test infrastructure
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

        [ClassCleanup()]
        public static void RemoveContentTypes()
        {
            //-- T1 torli a tobbit is
            var nodeTypeName = "T1";
            var nodeType = ActiveSchema.NodeTypes[nodeTypeName];
            if (nodeType != null)
            {
                NodeQuery query = new NodeQuery();
                query.Add(new TypeExpression(nodeType));
                foreach (var nodeId in query.Execute().Identifiers)
                    Node.ForceDelete(nodeId);
            }
            ContentType ct = ContentType.GetByName(nodeType.Name);
            if (ct != null)
                ct.Delete();
        }

		private string _baseCtd = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='{0}' parentType='{1}' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
	<DisplayName>{0}</DisplayName>
	<Fields></Fields>
</ContentType>";

		private string _withFieldCtd = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='{0}' parentType='{1}' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
	<DisplayName>{0}</DisplayName>
	<Fields>
		<Field name='F1' type='ShortText'>
			<DisplayName>F1</DisplayName>
		</Field>
	</Fields>
</ContentType>";
//        private string _withFieldCtd = @"<?xml version='1.0' encoding='utf-8'?>
//<ContentType name='{0}' parentType='{1}' handler='ContentRepository.GenericContent' xmlns='http://schemas.com/ContentRepository/ContentTypeDefinition'>
//	<DisplayName>{0}</DisplayName>
//	<Fields>
//		<Field name='F1' override='{2}' type='ShortText'>
//			<DisplayName>F1</DisplayName>
//		</Field>
//	</Fields>
//</ContentType>";

		[TestInitialize]
		public void CreateStartStructure()
		{
			var ct = ContentType.GetByName("T1");
			if (ct != null)
			{
				ct.Delete();
				ContentTypeManager.Reset();
			}

			ContentTypeInstaller installer = ContentTypeInstaller.CreateBatchContentTypeInstaller();
			for (int level = 1; level < 6; level++)
				installer.AddContentType(CreateCtd(level, false, false));
			installer.ExecuteBatch();
		}

		[TestMethod]
		public void ContentType_Inheritance_T2add()
		{
			//  Index   Type  OwnerIndex  ParentIndex
			//  0:      T1	  -			  -
			//  1:      T2	  1           -
			//  2:      T3    1           -
			//  3:      T4    1           -
			//  4:      T5    1           -

			ContentTypeInstaller.InstallContentType(CreateCtd(2, true, false)); //T2+
			FieldSetting[] fields;
			ContentType[] types = GetTestContentTypes(out fields);

			Assert.IsNull(fields[0], "#01");
			Assert.IsNotNull(fields[1], "#02");
			Assert.IsNotNull(fields[2], "#03");
			Assert.IsNotNull(fields[3], "#04");
			Assert.IsNotNull(fields[4], "#05");

			Assert.IsTrue(ReferenceEquals(fields[1].Owner, types[1]), "#10");

			Assert.IsTrue(ReferenceEquals(fields[2], fields[1]), "#20");
			Assert.IsTrue(ReferenceEquals(fields[3], fields[1]), "#21");
			Assert.IsTrue(ReferenceEquals(fields[4], fields[1]), "#22");
		}
		[TestMethod]
		public void ContentType_Inheritance_T2addT4add()
		{
			//  Index   Type  OwnerIndex  ParentIndex
			//  0:      T1	  -			  -
			//  1:      T2	  1           -
			//  2:      T3    1           -
			//  3:      T4    3           1
			//  4:      T5    3           -

			ContentTypeInstaller.InstallContentType(CreateCtd(2, true, false), //T2+
													CreateCtd(4, true, true)); //T4+
			FieldSetting[] fields;
			ContentType[] types = GetTestContentTypes(out fields);

			Assert.IsNull(fields[0], "#01");
			Assert.IsNotNull(fields[1], "#02");
			Assert.IsNotNull(fields[2], "#03");
			Assert.IsNotNull(fields[3], "#04");
			Assert.IsNotNull(fields[4], "#05");

			Assert.IsTrue(ReferenceEquals(fields[1].Owner, types[1]), "#10");
			Assert.IsTrue(ReferenceEquals(fields[3].Owner, types[3]), "#11");

			Assert.IsTrue(ReferenceEquals(fields[2], fields[1]), "#20");
			Assert.IsTrue(ReferenceEquals(fields[3].ParentFieldSetting, fields[1]), "#21");
			Assert.IsTrue(ReferenceEquals(fields[4], fields[3]), "#22");
		}
		[TestMethod]
		public void ContentType_Inheritance_T2addT4addT1add()
		{
			//  Index   Type  OwnerIndex  ParentIndex
			//  0:      T1	  0			  -
			//  1:      T2	  1           0
			//  2:      T3    1           -
			//  3:      T4    3           1
			//  4:      T5    3           -

			ContentTypeInstaller.InstallContentType(CreateCtd(2, true, true),   //T2+
			                                        CreateCtd(4, true, true),   //T4+
			                                        CreateCtd(1, true, false)); //T1+
			ContentTypeManager.Reset();

			FieldSetting[] fields;
			ContentType[] types = GetTestContentTypes(out fields);

			Assert.IsNotNull(fields[0], "#01");
			Assert.IsNotNull(fields[1], "#02");
			Assert.IsNotNull(fields[2], "#03");
			Assert.IsNotNull(fields[3], "#04");
			Assert.IsNotNull(fields[4], "#05");

			Assert.IsTrue(ReferenceEquals(fields[0].Owner, types[0]), "#10");
			Assert.IsTrue(ReferenceEquals(fields[1].Owner, types[1]), "#11");
			Assert.IsTrue(ReferenceEquals(fields[3].Owner, types[3]), "#12");

			Assert.IsTrue(ReferenceEquals(fields[1].ParentFieldSetting, fields[0]), "#20");
			Assert.IsTrue(ReferenceEquals(fields[2], fields[1]), "#21");
			Assert.IsTrue(ReferenceEquals(fields[3].ParentFieldSetting, fields[1]), "#22");
			Assert.IsTrue(ReferenceEquals(fields[4], fields[3]), "#23");
		}
		[TestMethod]
		public void ContentType_Inheritance_T2addT4addT1addT2del()
		{
			//  Index   Type  OwnerIndex  ParentIndex
			//  0:      T1	  0			  -
			//  1:      T2	  0           -
			//  2:      T3    0           -
			//  3:      T4    3           0
			//  4:      T5    3           -

			ContentTypeInstaller.InstallContentType(CreateCtd(2, true, true),    //T2+
			                                        CreateCtd(4, true, true),    //T4+
			                                        CreateCtd(1, true, false));  //T1+
			ContentTypeManager.Reset();
			ContentTypeInstaller.InstallContentType(CreateCtd(2, false, false)); //T2-

			FieldSetting[] fields;
			ContentType[] types = GetTestContentTypes(out fields);

			Assert.IsNotNull(fields[0], "#01");
			Assert.IsNotNull(fields[1], "#02");
			Assert.IsNotNull(fields[2], "#03");
			Assert.IsNotNull(fields[3], "#04");
			Assert.IsNotNull(fields[4], "#05");

			Assert.IsTrue(ReferenceEquals(fields[0].Owner, types[0]), "#10");
			Assert.IsTrue(ReferenceEquals(fields[3].Owner, types[3]), "#11");

			Assert.IsTrue(ReferenceEquals(fields[1], fields[0]), "#20");
			Assert.IsTrue(ReferenceEquals(fields[2], fields[0]), "#21");
			Assert.IsTrue(ReferenceEquals(fields[3].ParentFieldSetting, fields[0]), "#22");
			Assert.IsTrue(ReferenceEquals(fields[4], fields[3]), "#23");
		}
		[TestMethod]
		public void ContentType_Inheritance_T2addT4addT1addT2delT1del()
		{
			//  Index   Type  OwnerIndex  ParentIndex
			//  0:      T1	  -			  -
			//  1:      T2	  -           -
			//  2:      T3    -           -
			//  3:      T4    3           -
			//  4:      T5    3           -

			ContentTypeInstaller.InstallContentType(CreateCtd(2, true, true),    //T2+
													CreateCtd(4, true, true),    //T4+
													CreateCtd(1, true, false));  //T1+
			ContentTypeInstaller.InstallContentType(CreateCtd(2, false, false),  //T2-
													CreateCtd(1, false, false),  //T1-
													CreateCtd(4, true, false));

			FieldSetting[] fields;
			ContentType[] types = GetTestContentTypes(out fields);

			Assert.IsNull(fields[0], "#01");
			Assert.IsNull(fields[1], "#02");
			Assert.IsNull(fields[2], "#03");
			Assert.IsNotNull(fields[3], "#04");
			Assert.IsNotNull(fields[4], "#05");

			Assert.IsTrue(ReferenceEquals(fields[3].Owner, types[3]), "#10");

			Assert.IsTrue(ReferenceEquals(fields[4], fields[3]), "#20");
		}

		private string CreateCtd(int level, bool withField, bool withOverride)
		{
			string format = withField ? _withFieldCtd : _baseCtd;
			string name = "T" + level;
			string parent = level > 1 ? "T" + (level - 1) : "GenericContent";
			return withField ?
				String.Format(_withFieldCtd, name, parent) :
				//String.Format(_withFieldCtd, name, parent, withOverride.ToString().ToLower()) :
				String.Format(_baseCtd, name, parent);

		}
		private ContentType[] GetTestContentTypes(out FieldSetting[] fields)
		{
			ContentType[] types = new ContentType[5];
			fields = new FieldSetting[5];
			for (int level = 1; level < 6; level++)
			{
				ContentType ct = ContentTypeManager.Current.GetContentTypeByName("T" + level);
				types[level - 1] = ct;
				foreach (FieldSetting ft in ct.FieldSettings)
				{
					if (ft.Name == "F1")
					{
						fields[level - 1] = ft;
						break;
					}
				}
			}
			return types;
		}

	}
}