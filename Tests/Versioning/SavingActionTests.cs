using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using ContentRepository.Storage;
using ContentRepository.Versioning;
using System.Reflection;
using ContentRepository.Storage.Data;
using ContentRepository.Storage.Security;
using ContentRepository.Storage.Schema;

namespace ContentRepository.Tests.Versioning
{
    [TestClass]
    public class SavingActionTests : TestBase
    {
        #region test infrastructure

        public override TestContext TestContext { get; set; }

        #endregion

        #region Content create action tests

        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void SavingAction_Default_Default_Create_SaveAndCheckIn()
        {
            var content = Content.CreateNew("Car", Repository.Root, "car");
            var action = SavingAction.Create(content.ContentHandler);
            action.CheckOut();
        }
        [TestMethod]
        public void SavingAction_Off_None_Create()
        {
            CreatingTest(false, VersioningType.None, "V1.0.A");
        }
        [TestMethod]
        public void SavingAction_Off_MajorOnly_Create()
        {
            CreatingTest(false, VersioningType.MajorOnly, "V1.0.A");
        }
        [TestMethod]
        public void SavingAction_Off_MajorAndMinor_Create()
        {
            CreatingTest(false, VersioningType.MajorAndMinor, "V0.1.D");
        }
        [TestMethod]
        public void SavingAction_On_None_Create()
        {
            CreatingTest(true, VersioningType.None, "V1.0.P");
        }
        [TestMethod]
        public void SavingAction_On_MajorOnly_Create()
        {
            CreatingTest(true, VersioningType.MajorOnly, "V1.0.P");
        }
        [TestMethod]
        public void SavingAction_On_MajorAndMinor_Create()
        {
            CreatingTest(true, VersioningType.MajorAndMinor, "V0.1.D");
        }
        
        private static void CreatingTest(bool hasApproving, VersioningType mode, string expectedVersion)
        {
            var gc = CreateNodeInMemory(hasApproving ? ApprovingType.True : ApprovingType.False, mode);

            var action = SavingAction.Create(gc);
            action.CheckOutAndSave();

            Assert.IsTrue(action.CurrentVersion == null, "action.CurrentVersion is not null");
            Assert.IsTrue(action.CurrentVersionId == 0, String.Concat("action.CurrentVersionId is ", action.CurrentVersionId, ". Expected: 0"));
            Assert.IsTrue(action.ExpectedVersion != null, "action.ExpectedVersion is null");
            Assert.IsTrue(action.ExpectedVersion.ToString() == expectedVersion, String.Concat("action.CurrentVersion is ", action.ExpectedVersion, ". Expected: ", expectedVersion));
            Assert.IsTrue(action.ExpectedVersionId == 0, String.Concat("action.ExpectedVersionId is ", action.ExpectedVersionId, ". Expected: 0"));
            Assert.IsTrue(action.LockerUserId == null, "action.LockerUserId not null");
            Assert.IsTrue(action.DeletableVersionIds.Count == 0, String.Concat("action..DeletableVersions.Count is ", action.DeletableVersionIds.Count, ". Expected: 0"));
        }

        #endregion

        #region Content checkout and save action tests

        [TestMethod]
        public void SavingAction_Off_None_Approved_Save()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.None);

            InitializeAsExisting(gc, 123, 456, "V1.0.A");
            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 456)
            });

            action.CheckOutAndSave();
            SavingAssert(action, "V1.0.A", 456, "V2.0.L", 0, 1, new List<int>());
        }

        [TestMethod]
        public void SavingAction_Off_None_Locked_Save()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.None);

            InitializeAsExisting(gc, 123, 456, "V2.0.L");
            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 123),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.L"), 456)
            });

            action.CheckOutAndSave();

            SavingAssert(action, "V2.0.L", 456, "V2.0.L", 0, null, new List<int>());
        }

        #endregion

        #region Content checkin action tests

        [TestMethod]
        public void SavingAction_Off_None_Locked_CheckIn1()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.None);

            InitializeAsExisting(gc, 123, 113, "V1.2.L");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.2.L"), 113)
            });
            action.CheckIn();

            Assert.IsTrue(action.ExpectedVersion != null, "ExpectedVersion is null");
            Assert.IsTrue(action.ExpectedVersion.ToString() == "V1.0.A", String.Concat("ExpectedVersion is ", action.ExpectedVersion.ToString(), ". Expected: V1.0.A"));
            Assert.IsTrue(action.ExpectedVersionId == 111, String.Concat("ExpectedVersionId is ", action.ExpectedVersionId, ". Expected:111"));
            Assert.IsTrue(action.DeletableVersionIds.Count == 2, String.Concat("DeletableVersionIds contains ", action.DeletableVersionIds.Count, "items. Expected: 2"));
            Assert.IsTrue(action.DeletableVersionIds.Contains(113), String.Concat("DeletableVersionIds does not contain 113"));
            Assert.IsTrue(action.DeletableVersionIds.Contains(112), String.Concat("DeletableVersionIds does not contain 112"));
        }
        [TestMethod]
        public void SavingAction_Off_None_Locked_CheckIn2()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.None);

            InitializeAsExisting(gc, 100, 115, "V2.2.L");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.A"), 113),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.1.D"), 114),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.2.L"), 115)
            });
            action.CheckIn();

            Assert.IsTrue(action.ExpectedVersion != null, "ExpectedVersion is null");
            Assert.IsTrue(action.ExpectedVersion.ToString() == "V2.0.A", String.Concat("ExpectedVersion is ", action.ExpectedVersion.ToString(), ". Expected: V2.0.A"));
            Assert.IsTrue(action.ExpectedVersionId == 113, String.Concat("ExpectedVersionId is ", action.ExpectedVersionId, ". Expected:113"));
            Assert.IsTrue(action.DeletableVersionIds.Count == 2, String.Concat("DeletableVersionIds contains ", action.DeletableVersionIds.Count, "items. Expected: 2"));
            Assert.IsTrue(action.DeletableVersionIds.Contains(115), String.Concat("DeletableVersionIds does not contain 115"));
            Assert.IsTrue(action.DeletableVersionIds.Contains(114), String.Concat("DeletableVersionIds does not contain 114"));
        }
        [TestMethod]
        public void SavingAction_On_None_Locked_CheckIn1()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot

            //1.0A	1.0A
            //1.1D	1.1D
            //2.0A	2.0A
            //2.1D	2.2P <--
            //2.2L <--

            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.None);

            InitializeAsExisting(gc, 100, 115, "V2.2.L");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.A"), 113),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.1.D"), 114),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.2.L"), 115)
            });
            action.CheckIn();

            Assert.IsTrue(action.ExpectedVersion != null, "ExpectedVersion is null");
            Assert.IsTrue(action.ExpectedVersion.ToString() == "V3.0.P", String.Concat("ExpectedVersion is ", action.ExpectedVersion.ToString(), ". Expected: V2.1.P"));
            Assert.IsTrue(action.ExpectedVersionId == 115, String.Concat("ExpectedVersionId is ", action.ExpectedVersionId, ". Expected:115"));
            Assert.IsTrue(action.DeletableVersionIds.Count == 1, String.Concat("DeletableVersionIds contains ", action.DeletableVersionIds.Count, "items. Expected: 1"));
            Assert.IsTrue(action.DeletableVersionIds.Contains(114), String.Concat("DeletableVersionIds does not contain 114"));
        }
        [TestMethod]
        public void SavingAction_On_None_Locked_CheckIn2()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.None);

            InitializeAsExisting(gc, 100, 113, "V3.0.L");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.P"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V3.0.L"), 113)
            });
            action.CheckIn();

            Assert.IsTrue(action.ExpectedVersion != null, "ExpectedVersion is null");
            Assert.IsTrue(action.ExpectedVersion.ToString() == "V2.0.P", String.Concat("ExpectedVersion is ", action.ExpectedVersion.ToString(), ". Expected: V2.0.P"));
            Assert.IsTrue(action.ExpectedVersionId == 113, String.Concat("ExpectedVersionId is ", action.ExpectedVersionId, ". Expected:113"));
            Assert.IsTrue(action.DeletableVersionIds.Count == 1, String.Concat("DeletableVersionIds contains ", action.DeletableVersionIds.Count, "items. Expected: 1"));
            Assert.IsTrue(action.DeletableVersionIds.Contains(112), String.Concat("DeletableVersionIds does not contain 112"));
        }
        [TestMethod]
        public void SavingAction_Off_Major_Locked_CheckIn_Repair1()
        {
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.MajorOnly);

            InitializeAsExisting(gc, 123, 116, "V3.2.L");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.2.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.R"), 113),
                new NodeHead.NodeVersion(VersionNumber.Parse("V3.0.P"), 114),
                new NodeHead.NodeVersion(VersionNumber.Parse("V3.1.P"), 115),
                new NodeHead.NodeVersion(VersionNumber.Parse("V3.2.L"), 116)
            });

            action.CheckIn();

            SavingAssert(action, "V3.2.L", 116, "V2.0.A", 116, 0, new List<int> { 112, 113, 114, 115 });
        }

        #endregion

        #region Content checkout action tests

        [TestMethod]
        public void SavingAction_Off_None_CheckOut1()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.None);

            InitializeAsExisting(gc, 100, 111, "V2.0.A");
            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 110),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.A"), 111)
            });
            action.CheckOut();

            SavingAssert(action, "V2.0.A", 111, "V3.0.L", 0, 1, new List<int>());
        }
        [TestMethod]
        public void SavingAction_Off_None_CheckOut2()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.None);

            InitializeAsExisting(gc, 100, 111, "V2.0.A");
            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 110),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.A"), 111)
            });
            action.CheckOut();

            SavingAssert(action, "V2.0.A", 111, "V3.0.L", 0, 1, new List<int>());
        }

        #endregion

        #region Content undo checkout action tests

        [TestMethod]
        public void SavingAction_On_None_Locked_UndoCheckOut1()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.None);

            InitializeAsExisting(gc, 100, 113, "V3.0.L");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.P"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V3.0.L"), 113)
            });
            action.UndoCheckOut();

            Assert.IsTrue(action.ExpectedVersion != null, "ExpectedVersion is null");
            Assert.IsTrue(action.ExpectedVersion.ToString() == "V2.0.P", String.Concat("ExpectedVersion is ", action.ExpectedVersion.ToString(), ". Expected: V2.0.P"));
            Assert.IsTrue(action.ExpectedVersionId == 112, String.Concat("ExpectedVersionId is ", action.ExpectedVersionId, ". Expected:112"));
            Assert.IsTrue(action.DeletableVersionIds.Count == 1, String.Concat("DeletableVersionIds contains ", action.DeletableVersionIds.Count, "items. Expected: 1"));
            Assert.IsTrue(action.DeletableVersionIds.Contains(113), String.Concat("DeletableVersionIds does not contain 113"));
        }

        [TestMethod]
        public void SavingAction_On_None_Locked_UndoCheckOut_Bug1814()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.None);

            InitializeAsExisting(gc, 100, 111, "V1.0.A", true, 1, DateTime.UtcNow);

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.L"), 112)
            });
            action.UndoCheckOut();

            Assert.IsTrue(action.ExpectedVersion != null, "ExpectedVersion is null");
            Assert.IsTrue(action.ExpectedVersion.ToString() == "V1.0.A", String.Concat("ExpectedVersion is ", action.ExpectedVersion.ToString(), ". Expected: V1.0.A"));
            Assert.IsTrue(action.ExpectedVersionId == 111, String.Concat("ExpectedVersionId is ", action.ExpectedVersionId, ". Expected:111"));
            Assert.IsTrue(action.DeletableVersionIds.Count == 1, String.Concat("DeletableVersionIds contains ", action.DeletableVersionIds.Count, "items. Expected: 1"));
            Assert.IsTrue(action.DeletableVersionIds.Contains(112), String.Concat("DeletableVersionIds does not contain 112"));
        }

        #endregion

        #region Content publish action tests

        //[TestMethod]
        //public void SavingAction_Off_Major_Locked_Publish1()
        //{
        //    // do not save the content in this test
        //    // do not change the parent to TestRoot
        //    var content = Content.CreateNew("Car", Repository.Root, "car");
        //    var node = content.ContentHandler as GenericContent;
        //    node.ApprovingMode = ApprovingType.False;
        //    node.VersioningMode = VersioningType.MajorOnly;

        //    InitializeAsExisting(node, 123, 456, "V1.2.L");

        //    var action = SavingAction.Create(content.ContentHandler);
        //    SetVersionHistory(action, node, new[]
        //    {
        //        new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
        //        new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.D"), 112),
        //        new NodeHead.NodeVersion(VersionNumber.Parse("V1.2.L"), 113)
        //    });

        //    action.Publish();

        //    Assert.IsTrue(action.ExpectedVersion != null, "ExpectedVersion is null");
        //    Assert.IsTrue(action.ExpectedVersion.ToString() == "V2.0.A", String.Concat("ExpectedVersion is ", action.ExpectedVersion.ToString(), ". Expected: V2.0.A"));
        //    Assert.IsTrue(action.ExpectedVersionId == 0, String.Concat("ExpectedVersionId is ", action.ExpectedVersionId, ". Expected:0"));
        //    Assert.IsTrue(action.DeletableVersionIds.Count == 0, String.Concat("DeletableVersionIds contains ", action.DeletableVersionIds.Count, "items. Expected: 0"));
        //}

        [TestMethod]
        public void SavingAction_Off_Full_Locked_Publish1()
        {
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.MajorAndMinor);

            InitializeAsExisting(gc, 123, 113, "V1.2.L");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.2.L"), 113)
            });

            action.Publish();

            SavingAssert(action, "V1.2.L", 113, "V2.0.A", 113, 0, new List<int>());
        }

        [TestMethod]
        public void SavingAction_Off_Full_Draft_Publish1()
        {
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.MajorAndMinor);

            InitializeAsExisting(gc, 123, 112, "V1.1.D");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.D"), 112)
            });

            action.Publish();

            SavingAssert(action, "V1.1.D", 112, "V2.0.A", 112, 0, new List<int>());
        }

        [TestMethod]
        public void SavingAction_On_Full_Locked_Publish1()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.MajorAndMinor);

            InitializeAsExisting(gc, 123, 113, "V1.2.L");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.2.L"), 113)
            });

            action.Publish();

            SavingAssert(action, "V1.2.L", 113, "V1.2.P", 113, 0, new List<int>());
        }

        [TestMethod]
        public void SavingAction_On_Full_Draft_Publish1()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.MajorAndMinor);

            InitializeAsExisting(gc, 123, 114, "V1.1.D");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V0.1.P"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V0.2.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 113),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.D"), 114)
            });

            action.Publish();

            SavingAssert(action, "V1.1.D", 114, "V1.1.P", 114, 0, new List<int>());
        }

        [TestMethod]
        public void SavingAction_On_Full_Draft_Publish2()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.MajorAndMinor);

            InitializeAsExisting(gc, 123, 115, "V1.2.R");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V0.1.P"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V0.2.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 113),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.D"), 114),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.2.R"), 115)
            });

            action.Publish();

            SavingAssert(action, "V1.2.R", 115, "V1.3.P", 0, 0, new List<int>());
        }

        #endregion

        #region Content approve action tests

        [TestMethod]
        public void SavingAction_On_None_Pending_Approve1()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.None);

            InitializeAsExisting(gc, 123, 111, "V1.0.P");

            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.P"), 111)
            });

            action.Approve();

            SavingAssert(action, "V1.0.P", 111, "V1.0.A", 111, null, new List<int>());
        }

        [TestMethod]
        public void SavingAction_On_None_Pending_Approve2()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.None);

            InitializeAsExisting(gc, 123, 112, "V2.0.P");

            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.R"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.P"), 112)
            });

            action.Approve();

            SavingAssert(action, "V2.0.P", 112, "V1.0.A", 112, null, new List<int> { 111 });
        }

        [TestMethod]
        public void SavingAction_On_None_Pending_Approve3()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.None);

            InitializeAsExisting(gc, 123, 113, "V3.0.P");

            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.R"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V3.0.P"), 113)
            });

            action.Approve();

            SavingAssert(action, "V3.0.P", 113, "V1.0.A", 111, null, new List<int> { 112, 113 });
        }

        [TestMethod]
        public void SavingAction_On_Major_Pending_Approve1()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.MajorOnly);

            InitializeAsExisting(gc, 123, 112, "V2.0.P");

            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.R"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.P"), 112)
            });

            action.Approve();

            SavingAssert(action, "V2.0.P", 112, "V1.0.A", 112, null, new List<int> { 111 });
        }

        [TestMethod]
        public void SavingAction_On_Major_Pending_Approve2()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.MajorOnly);

            InitializeAsExisting(gc, 123, 114, "V4.0.P");

            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.P"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V3.0.P"), 113),
                new NodeHead.NodeVersion(VersionNumber.Parse("V4.0.P"), 114)
            });

            action.Approve();

            SavingAssert(action, "V4.0.P", 114, "V2.0.A", 114, null, new List<int> { 112, 113 });
        }

        [TestMethod]
        public void SavingAction_On_Full_Pending_Approve1()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.MajorAndMinor);

            InitializeAsExisting(gc, 123, 115, "V1.2.P");

            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V0.1.P"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V0.2.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 113),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.D"), 114),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.2.P"), 115)
            });

            action.Approve();

            SavingAssert(action, "V1.2.P", 115, "V2.0.A", 115, null, new List<int>());
        }

        [TestMethod]
        public void SavingAction_On_Full_Pending_Approve2()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.MajorAndMinor);

            InitializeAsExisting(gc, 123, 113, "V0.3.P");

            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V0.1.P"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V0.2.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V0.3.P"), 113)
            });

            action.Approve();

            SavingAssert(action, "V0.3.P", 113, "V1.0.A", 113, null, new List<int>());
        }

        #endregion

        #region Content reject action tests

        [TestMethod]
        public void SavingAction_On_None_Pending_Reject1()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.None);

            InitializeAsExisting(gc, 123, 111, "V1.0.P");

            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.P"), 111)
            });

            action.Reject();

            SavingAssert(action, "V1.0.P", 111, "V1.0.R", 111, null, new List<int>());
        }

        [TestMethod]
        public void SavingAction_On_None_Pending_Reject2()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.None);

            InitializeAsExisting(gc, 123, 112, "V2.0.P");

            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.R"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.P"), 112)
            });

            action.Reject();

            SavingAssert(action, "V2.0.P", 112, "V2.0.R", 112, null, new List<int>());
        }

        [TestMethod]
        public void SavingAction_On_None_Pending_Reject3()
        {
            var gc = CreateNodeInMemory(ApprovingType.True, VersioningType.None);

            InitializeAsExisting(gc, 123, 113, "V3.0.P");

            var action = SavingAction.Create(gc);

            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.R"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V3.0.P"), 113)
            });

            action.Reject();

            SavingAssert(action, "V3.0.P", 113, "V3.0.R", 113, null, new List<int>());
        }

        #endregion

        #region Content repair action tests

        [TestMethod]
        public void SavingAction_Off_Major_SaveAndCheckIn_Repair1()
        {
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.MajorOnly);

            InitializeAsExisting(gc, 123, 114, "V3.0.P");

            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 111),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.2.D"), 112),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.R"), 113),
                new NodeHead.NodeVersion(VersionNumber.Parse("V3.0.P"), 114)
            });

            action.CheckOutAndSaveAndCheckIn();

            SavingAssert(action, "V3.0.P", 114, "V2.0.A", 114, 0, new List<int> { 112, 113 });
        }

        #endregion

        #region Content start multistep save tests

        [TestMethod]
        public void SavingAction_Off_None_StartMultistepSave_01()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.None);

            InitializeAsExisting(gc, 100, 111, "V2.0.A");
            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 110),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.A"), 111)
            });
            action.StartMultistepSave();

            SavingAssert(action, "V2.0.A", 111, "V3.0.L", 0, 1, new List<int>());
        }

        [TestMethod]
        public void SavingAction_Off_MajorMinor_StartMultistepSave_02()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.MajorAndMinor);

            InitializeAsExisting(gc, 100, 111, "V2.0.A");
            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 110),
                new NodeHead.NodeVersion(VersionNumber.Parse("V2.0.A"), 111)
            });
            action.StartMultistepSave();

            SavingAssert(action, "V2.0.A", 111, "V2.1.L", 0, 1, new List<int>());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void SavingAction_Off_MajorMinor_StartMultistepSave_03()
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.MajorAndMinor);

            InitializeAsExisting(gc, 100, 111, "V1.1.L", true, 100, DateTime.Today);
            var action = SavingAction.Create(gc);
            SetVersionHistory(action, gc, new[]
            {
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.0.A"), 110),
                new NodeHead.NodeVersion(VersionNumber.Parse("V1.1.L"), 111)
            });
            action.StartMultistepSave();
        }

        #endregion

        #region Content action with different user tests

        [TestMethod]
        public void SavingAction_Off_None_DifferentUsers_CheckOut1()
        {
            var gc = CreateNodeInMemory(ApprovingType.False, VersioningType.None);

            InitializeAsExisting(gc, 100, 111, "V2.0.L", true, RepositoryConfiguration.VisitorUserId, DateTime.Today.AddDays(-1));

            Assert.IsFalse(SavingAction.HasCheckOut(gc), "Node is locked by another user but has checkout!");
            Assert.IsFalse(SavingAction.HasCheckIn(gc), "Node is locked by another user but has checkin!");
            Assert.IsFalse(SavingAction.HasUndoCheckOut(gc), "Node is locked by another user but has undo checkout!");
        }

        #endregion

        //====================================================================== Helper methods

        private static void SavingAssert(NodeSaveSettings action, string currentversion, int currentVersionId,
            string expectedVersion, int expectedVersionId, int? lockerId,
            ICollection<int> deletableVersionIds)
        {
            Assert.IsTrue(action.CurrentVersion != null, String.Concat("action.CurrentVersion is null"));
            Assert.IsTrue(action.CurrentVersion.ToString() == currentversion, String.Concat("action.CurrentVersion is ", action.CurrentVersion, ". Expected: ", currentversion));
            Assert.IsTrue(action.CurrentVersionId == currentVersionId, String.Concat("action.CurrentVersion is ", action.CurrentVersionId, ". Expected: ", currentVersionId));
            Assert.IsTrue(action.ExpectedVersion != null, String.Concat("action.ExpectedVersion is null"));
            Assert.IsTrue(action.ExpectedVersion.ToString() == expectedVersion, String.Concat("action.CurrentVersion is ", action.ExpectedVersion, ". Expected: ", expectedVersion));
            Assert.IsTrue(action.ExpectedVersionId == expectedVersionId, String.Concat("action.ExpectedVersionId is ", action.ExpectedVersionId, ". Expected: ", expectedVersionId));
            Assert.IsTrue(action.LockerUserId == lockerId, String.Concat("action.LockerUserId is '", action.LockerUserId, "', Expected: ", lockerId));
            Assert.IsTrue(action.DeletableVersionIds.Count == deletableVersionIds.Count, String.Concat("action..DeletableVersions.Count is ", action.DeletableVersionIds.Count, ". Expected: ", deletableVersionIds.Count));

            foreach (var versionId in deletableVersionIds)
            {
                Assert.IsTrue(action.DeletableVersionIds.Contains(versionId), String.Concat("DeletableVersionIds does not contain ", versionId));
            }
        }

        private static GenericContent CreateNodeInMemory(ApprovingType approvingType, VersioningType versioningType)
        {
            // do not save the content in this test
            // do not change the parent to TestRoot
            var content = Content.CreateNew("Car", Repository.Root, "car");
            var gc = content.ContentHandler as GenericContent;
            gc.ApprovingMode = approvingType;
            gc.VersioningMode = versioningType;

            return gc;
        }

        private static void InitializeAsExisting(Node node, int nodeId, int versionId, string version)
        {
            InitializeAsExisting(node, nodeId, versionId, version, false, 0, null);
        }

        private static void InitializeAsExisting(Node node, int nodeId, int versionId, string version, bool? locked, int? lockedById, DateTime? lockDate)
        {
            var nodeData = node.Data;
            nodeData.Id = nodeId;
            nodeData.VersionId = versionId;
            nodeData.Version = VersionNumber.Parse(version);

            if (locked.HasValue) nodeData.Locked = locked.Value;
            if (lockedById.HasValue) nodeData.LockedById = lockedById.Value;
            if (lockDate.HasValue) nodeData.LockDate = lockDate.Value;
        }
        private static void SetVersionHistory(SavingAction action, Node node, IEnumerable<NodeHead.NodeVersion> versionHistory)
        {
            var lastMajorVer = versionHistory.Where(v => v.VersionNumber.Minor == 0 && v.VersionNumber.Status == VersionStatus.Approved).LastOrDefault();
            var lastMinorVer = versionHistory.LastOrDefault();

            var head = NodeHead.CreateFromNode(node, lastMinorVer.VersionId, lastMajorVer == null ? 0 : lastMajorVer.VersionId);
            var fieldInfo = head.GetType().GetField("_versions", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(head, versionHistory);

            var methodInfo = action.GetType().GetMethod("SetNodeHead", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(NodeHead) }, null);
            methodInfo.Invoke(action, new object[] { head });
        }
    }
}
