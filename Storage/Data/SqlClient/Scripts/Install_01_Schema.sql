--EXEC sp_fulltext_database enable
--GO


/****** Object:  FullTextCatalog [SnCrFullText]    Script Date: 11/26/2007 13:40:34 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
--ALTER FULLTEXT INDEX ON [dbo].[BinaryProperties] DISABLE
--GO

--/****** Object:  FullTextIndex     Script Date: 11/26/2007 13:40:34 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
--DROP FULLTEXT INDEX ON [dbo].[BinaryProperties]
--GO

--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[FlatProperties]'))
--ALTER FULLTEXT INDEX ON [dbo].[FlatProperties] DISABLE
--GO

--/****** Object:  FullTextIndex     Script Date: 11/26/2007 13:40:34 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[FlatProperties]'))
--DROP FULLTEXT INDEX ON [dbo].[FlatProperties]
--GO

--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[Nodes]'))
--ALTER FULLTEXT INDEX ON [dbo].[Nodes] DISABLE
--GO

--/****** Object:  FullTextIndex     Script Date: 11/26/2007 13:40:35 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[Nodes]'))
--DROP FULLTEXT INDEX ON [dbo].[Nodes]
--GO

--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[TextPropertiesNText]'))
--ALTER FULLTEXT INDEX ON [dbo].[TextPropertiesNText] DISABLE
--GO

--/****** Object:  FullTextIndex     Script Date: 11/26/2007 13:40:35 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[TextPropertiesNText]'))
--DROP FULLTEXT INDEX ON [dbo].[TextPropertiesNText]
--GO

--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[TextPropertiesNVarchar]'))
--ALTER FULLTEXT INDEX ON [dbo].[TextPropertiesNVarchar] DISABLE
--GO

--/****** Object:  FullTextIndex     Script Date: 11/26/2007 13:40:35 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[TextPropertiesNVarchar]'))
--DROP FULLTEXT INDEX ON [dbo].[TextPropertiesNVarchar]
--GO

--IF  EXISTS (SELECT * FROM sysfulltextcatalogs ftc WHERE ftc.name = N'SnCrFullText')
--DROP FULLTEXT CATALOG [SnCrFullText]
--GO



--CREATE FULLTEXT CATALOG SnCrFullText AS DEFAULT;
--GO


------------------------------------------------                        --------------------------------------------------------------
------------------------------------------------  DROP EXISTING TABLES  --------------------------------------------------------------
------------------------------------------------                        --------------------------------------------------------------


IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityMemberships_Nodes_ContainerId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityMemberships]'))
ALTER TABLE [dbo].[SecurityMemberships] DROP CONSTRAINT [FK_SecurityMemberships_Nodes_ContainerId]
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityMemberships_Nodes_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityMemberships]'))
ALTER TABLE [dbo].[SecurityMemberships] DROP CONSTRAINT [FK_SecurityMemberships_Nodes_UserId]
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_SecurityMemberships_ContainerType]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityMemberships]'))
ALTER TABLE [dbo].[SecurityMemberships] DROP CONSTRAINT [CK_SecurityMemberships_ContainerType]
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityEntries_DefinedOnNodeId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityEntries]'))
ALTER TABLE [dbo].[SecurityEntries] DROP CONSTRAINT [FK_SecurityEntries_DefinedOnNodeId]
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityEntries_PrincipalId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityEntries]'))
ALTER TABLE [dbo].[SecurityEntries] DROP CONSTRAINT [FK_SecurityEntries_PrincipalId]
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityCustomEntries_DefinedOnNodeId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityCustomEntries]'))
ALTER TABLE [dbo].[SecurityCustomEntries] DROP CONSTRAINT [FK_SecurityCustomEntries_DefinedOnNodeId]
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityCustomEntries_PrincipalId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityCustomEntries]'))
ALTER TABLE [dbo].[SecurityCustomEntries] DROP CONSTRAINT [FK_SecurityCustomEntries_PrincipalId]
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_SchemaPropertySets_ContentListTypeId]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_SchemaPropertySets_ContentListTypeId]
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_Nodes_ContentListId]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_Nodes_ContentListId]
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_Nodes_CreatedById]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_Nodes_CreatedById]
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_Nodes_ModifiedById]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_Nodes_ModifiedById]
GO

/****** Object:  ForeignKey [FK_BinaryProperties_SchemaPropertyTypes]    Script Date: 10/25/2007 15:49:18 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BinaryProperties_SchemaPropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
ALTER TABLE [dbo].[BinaryProperties] DROP CONSTRAINT [FK_BinaryProperties_SchemaPropertyTypes]
GO
/****** Object:  ForeignKey [FK_BinaryProperties_Versions]    Script Date: 10/25/2007 15:49:19 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BinaryProperties_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
ALTER TABLE [dbo].[BinaryProperties] DROP CONSTRAINT [FK_BinaryProperties_Versions]
GO

IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_StagingBinaryProperties_CreationDate]') AND parent_object_id = OBJECT_ID(N'[dbo].[StagingBinaryProperties]'))
Begin
	IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_StagingBinaryProperties_CreationDate]') AND type = 'D')
	BEGIN
		ALTER TABLE [dbo].[StagingBinaryProperties] DROP CONSTRAINT [DF_StagingBinaryProperties_CreationDate]
	END
End
GO
IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF__StagingBi__RowGu__42F95F9C]') AND parent_object_id = OBJECT_ID(N'[dbo].[StagingBinaryProperties]'))
Begin
	IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF__StagingBi__RowGu__42F95F9C]') AND type = 'D')
	BEGIN
		ALTER TABLE [dbo].[StagingBinaryProperties] DROP CONSTRAINT [DF__StagingBi__RowGu__42F95F9C]
	END
End
GO
/****** Object:  ForeignKey [FK_StagingBinaryProperties_SchemaPropertyTypes]    Script Date: 01/12/2012 04:45:07 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_StagingBinaryProperties_SchemaPropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[StagingBinaryProperties]'))
ALTER TABLE [dbo].[StagingBinaryProperties] DROP CONSTRAINT [FK_StagingBinaryProperties_SchemaPropertyTypes]
GO
/****** Object:  ForeignKey [FK_StagingBinaryProperties_Versions]    Script Date: 01/12/2012 04:45:07 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_StagingBinaryProperties_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[StagingBinaryProperties]'))
ALTER TABLE [dbo].[StagingBinaryProperties] DROP CONSTRAINT [FK_StagingBinaryProperties_Versions]
GO
/****** Object:  Table [dbo].[StagingBinaryProperties]    Script Date: 01/12/2012 04:45:07 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StagingBinaryProperties]') AND type in (N'U'))
DROP TABLE [dbo].[StagingBinaryProperties]
GO

/****** Object:  ForeignKey [FK_FlatProperties_Versions]    Script Date: 10/25/2007 15:50:10 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_FlatProperties_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[FlatProperties]'))
ALTER TABLE [dbo].[FlatProperties] DROP CONSTRAINT [FK_FlatProperties_Versions]
GO
/****** Object:  ForeignKey [FK_Nodes_LockedBy]    Script Date: 10/25/2007 15:50:16 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_LockedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_LockedBy]
GO
/****** Object:  ForeignKey [FK_Nodes_Parent]    Script Date: 10/25/2007 15:50:16 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_Parent]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_Parent]
GO
/****** Object:  ForeignKey [FK_Nodes_SchemaPropertySets]    Script Date: 10/25/2007 15:50:17 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_SchemaPropertySets]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes] DROP CONSTRAINT [FK_Nodes_SchemaPropertySets]
GO
/****** Object:  ForeignKey [FK_ReferenceProperties_SchemaPropertyTypes]    Script Date: 10/25/2007 15:50:19 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ReferenceProperties_SchemaPropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]'))
ALTER TABLE [dbo].[ReferenceProperties] DROP CONSTRAINT [FK_ReferenceProperties_SchemaPropertyTypes]
GO
/****** Object:  ForeignKey [FK_PropertySets_PropertySets]    Script Date: 10/25/2007 15:50:23 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PropertySets_PropertySets]') AND parent_object_id = OBJECT_ID(N'[dbo].[SchemaPropertySets]'))
ALTER TABLE [dbo].[SchemaPropertySets] DROP CONSTRAINT [FK_PropertySets_PropertySets]
GO
/****** Object:  ForeignKey [FK_PropertySets_PropertySetTypes]    Script Date: 10/25/2007 15:50:24 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PropertySets_PropertySetTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[SchemaPropertySets]'))
ALTER TABLE [dbo].[SchemaPropertySets] DROP CONSTRAINT [FK_PropertySets_PropertySetTypes]
GO
/****** Object:  ForeignKey [FK_PropertyTypes_PropertySets]    Script Date: 10/25/2007 15:50:25 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PropertyTypes_PropertySets]') AND parent_object_id = OBJECT_ID(N'[dbo].[SchemaPropertySetsPropertyTypes]'))
ALTER TABLE [dbo].[SchemaPropertySetsPropertyTypes] DROP CONSTRAINT [FK_PropertyTypes_PropertySets]
GO
/****** Object:  ForeignKey [FK_PropertyTypes_PropertySlots]    Script Date: 10/25/2007 15:50:25 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PropertyTypes_PropertySlots]') AND parent_object_id = OBJECT_ID(N'[dbo].[SchemaPropertySetsPropertyTypes]'))
ALTER TABLE [dbo].[SchemaPropertySetsPropertyTypes] DROP CONSTRAINT [FK_PropertyTypes_PropertySlots]
GO
/****** Object:  ForeignKey [FK_PropertySlots_DataTypes]    Script Date: 10/25/2007 15:50:28 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PropertySlots_DataTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[SchemaPropertyTypes]'))
ALTER TABLE [dbo].[SchemaPropertyTypes] DROP CONSTRAINT [FK_PropertySlots_DataTypes]
GO
/****** Object:  ForeignKey [FK_TextPropertiesNText_SchemaPropertyTypes]    Script Date: 10/25/2007 15:50:30 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_TextPropertiesNText_SchemaPropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[TextPropertiesNText]'))
ALTER TABLE [dbo].[TextPropertiesNText] DROP CONSTRAINT [FK_TextPropertiesNText_SchemaPropertyTypes]
GO
/****** Object:  ForeignKey [FK_TextPropertiesNText_Versions]    Script Date: 10/25/2007 15:50:30 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_TextPropertiesNText_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[TextPropertiesNText]'))
ALTER TABLE [dbo].[TextPropertiesNText] DROP CONSTRAINT [FK_TextPropertiesNText_Versions]
GO
/****** Object:  ForeignKey [FK_TextPropertiesNVarchar_SchemaPropertyTypes]    Script Date: 10/25/2007 15:50:32 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_TextPropertiesNVarchar_SchemaPropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[TextPropertiesNVarchar]'))
ALTER TABLE [dbo].[TextPropertiesNVarchar] DROP CONSTRAINT [FK_TextPropertiesNVarchar_SchemaPropertyTypes]
GO
/****** Object:  ForeignKey [FK_TextPropertiesNVarchar_Versions]    Script Date: 10/25/2007 15:50:32 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_TextPropertiesNVarchar_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[TextPropertiesNVarchar]'))
ALTER TABLE [dbo].[TextPropertiesNVarchar] DROP CONSTRAINT [FK_TextPropertiesNVarchar_Versions]
GO
/****** Object:  ForeignKey [FK_VersionExtensions_SchemaPropertySets]    Script Date: 10/25/2007 15:50:33 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VersionExtensions_SchemaPropertySets]') AND parent_object_id = OBJECT_ID(N'[dbo].[VersionExtensions]'))
ALTER TABLE [dbo].[VersionExtensions] DROP CONSTRAINT [FK_VersionExtensions_SchemaPropertySets]
GO
/****** Object:  ForeignKey [FK_VersionExtensions_Versions]    Script Date: 10/25/2007 15:50:33 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VersionExtensions_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[VersionExtensions]'))
ALTER TABLE [dbo].[VersionExtensions] DROP CONSTRAINT [FK_VersionExtensions_Versions]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes]    Script Date: 10/25/2007 15:50:36 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions] DROP CONSTRAINT [FK_Versions_Nodes]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes_CreatedBy]    Script Date: 10/25/2007 15:50:37 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes_CreatedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions] DROP CONSTRAINT [FK_Versions_Nodes_CreatedBy]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes_ModifiedBy]    Script Date: 10/25/2007 15:50:37 ******/
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes_ModifiedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions] DROP CONSTRAINT [FK_Versions_Nodes_ModifiedBy]
GO
/****** Object:  FullTextIndex [PK_BinaryProperties]    Script Date: 10/25/2007 15:49:19 ******/
/****** Object:  FullTextIndex     Script Date: 10/25/2007 15:49:19 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
--DROP FULLTEXT INDEX ON [dbo].[BinaryProperties]
--GO
--/****** Object:  FullTextIndex [PK_FlatProperties_1]    Script Date: 10/25/2007 15:50:10 ******/
--/****** Object:  FullTextIndex     Script Date: 10/25/2007 15:50:10 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[FlatProperties]'))
--DROP FULLTEXT INDEX ON [dbo].[FlatProperties]
--GO
--/****** Object:  FullTextIndex [PK_tblFpsNodes]    Script Date: 10/25/2007 15:50:17 ******/
--/****** Object:  FullTextIndex     Script Date: 10/25/2007 15:50:17 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[Nodes]'))
--DROP FULLTEXT INDEX ON [dbo].[Nodes]
--GO
--/****** Object:  FullTextIndex [PK_TextPropertiesNText]    Script Date: 10/25/2007 15:50:30 ******/
--/****** Object:  FullTextIndex     Script Date: 10/25/2007 15:50:30 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[TextPropertiesNText]'))
--DROP FULLTEXT INDEX ON [dbo].[TextPropertiesNText]
--GO
--/****** Object:  FullTextIndex [PK_TextPropertiesNVarchar]    Script Date: 10/25/2007 15:50:32 ******/
--/****** Object:  FullTextIndex     Script Date: 10/25/2007 15:50:32 ******/
--IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[TextPropertiesNVarchar]'))
--DROP FULLTEXT INDEX ON [dbo].[TextPropertiesNVarchar]
--GO

/****** Object:  View [dbo].[NodeInfoView]  ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[NodeInfoView]'))
DROP VIEW [dbo].[NodeInfoView]
GO
/****** Object:  View [dbo].[PropertyInfoView]  ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[PropertyInfoView]'))
DROP VIEW [dbo].[PropertyInfoView]
GO
/****** Object:  View [dbo].[PermissionInfoView]  ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[PermissionInfoView]'))
DROP VIEW [dbo].[PermissionInfoView]
GO
/****** Object:  View [dbo].[SysSearchWithFlatsView]    Script Date: 08/07/2007 14:50:18 ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[SysSearchWithFlatsView]'))
DROP VIEW [dbo].[SysSearchWithFlatsView]
GO
/****** Object:  View [dbo].[SysSearchView]    Script Date: 08/07/2007 14:50:18 ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[SysSearchView]'))
DROP VIEW [dbo].[SysSearchView]
GO
/****** Object:  View [dbo].[ReferencesInfoView]    Script Date: 08/07/2007 14:50:18 ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[ReferencesInfoView]'))
DROP VIEW [dbo].[ReferencesInfoView]
GO
/****** Object:  View [dbo].[PropertySetsInfoView]    Script Date: 08/13/2007 13:40:03 ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[PropertySetsInfoView]'))
DROP VIEW [dbo].[PropertySetsInfoView]
GO
/****** Object:  View [dbo].[MembershipInfoView]    ******/
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[MembershipInfoView]'))
DROP VIEW [dbo].[MembershipInfoView]
GO

/****** Object:  Table [dbo].[FlatProperties]    Script Date: 10/25/2007 15:50:10 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FlatProperties]') AND type in (N'U'))
DROP TABLE [dbo].[FlatProperties]
GO
/****** Object:  Table [dbo].[SchemaPropertySetTypes]    Script Date: 10/25/2007 15:50:26 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaPropertySetTypes]') AND type in (N'U'))
DROP TABLE [dbo].[SchemaPropertySetTypes]
GO
/****** Object:  Table [dbo].[SchemaDataTypes]    Script Date: 10/25/2007 15:50:20 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaDataTypes]') AND type in (N'U'))
DROP TABLE [dbo].[SchemaDataTypes]
GO
/****** Object:  Table [dbo].[SchemaPropertyTypes]    Script Date: 10/25/2007 15:50:28 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaPropertyTypes]') AND type in (N'U'))
DROP TABLE [dbo].[SchemaPropertyTypes]
GO
/****** Object:  Table [dbo].[SchemaPropertySets]    Script Date: 10/25/2007 15:50:23 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaPropertySets]') AND type in (N'U'))
DROP TABLE [dbo].[SchemaPropertySets]
GO
/****** Object:  Table [dbo].[SchemaPropertySetsPropertyTypes]    Script Date: 10/25/2007 15:50:25 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaPropertySetsPropertyTypes]') AND type in (N'U'))
DROP TABLE [dbo].[SchemaPropertySetsPropertyTypes]
GO
/****** Object:  Table [dbo].[ReferenceProperties]    Script Date: 10/25/2007 15:50:19 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]') AND type in (N'U'))
DROP TABLE [dbo].[ReferenceProperties]
GO
/****** Object:  Table [dbo].[TextPropertiesNText]    Script Date: 10/25/2007 15:50:30 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TextPropertiesNText]') AND type in (N'U'))
DROP TABLE [dbo].[TextPropertiesNText]
GO
/****** Object:  Table [dbo].[BinaryProperties]    Script Date: 10/25/2007 15:49:18 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BinaryProperties]') AND type in (N'U'))
DROP TABLE [dbo].[BinaryProperties]
GO
/****** Object:  Table [dbo].[TextPropertiesNVarchar]    Script Date: 10/25/2007 15:50:32 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TextPropertiesNVarchar]') AND type in (N'U'))
DROP TABLE [dbo].[TextPropertiesNVarchar]
GO
/****** Object:  Table [dbo].[VersionExtensions]    Script Date: 10/25/2007 15:50:33 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VersionExtensions]') AND type in (N'U'))
DROP TABLE [dbo].[VersionExtensions]
GO
/****** Object:  Table [dbo].[SchemaPermissionTypes]    Script Date: 10/25/2007 15:50:21 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaPermissionTypes]') AND type in (N'U'))
DROP TABLE [dbo].[SchemaPermissionTypes]
GO
/****** Object:  Table [dbo].[Nodes]    Script Date: 10/25/2007 15:50:16 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Nodes]') AND type in (N'U'))
DROP TABLE [dbo].[Nodes]
GO
/****** Object:  Table [dbo].[Versions]    Script Date: 10/25/2007 15:50:36 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Versions]') AND type in (N'U'))
DROP TABLE [dbo].[Versions]
GO
/****** Object:  Table [dbo].[SecurityEntries]    Script Date: 11/19/2007 11:48:06 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityEntries]') AND type in (N'U'))
DROP TABLE [dbo].[SecurityEntries]
GO
/****** Object:  Table [dbo].[SecurityCustomEntries] ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityCustomEntries]') AND type in (N'U'))
DROP TABLE [dbo].[SecurityCustomEntries]
GO
/****** Object:  Table [dbo].[SecurityMemberships]    Script Date: 05/13/2009 11:10:30 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityMemberships]') AND type in (N'U'))
DROP TABLE [dbo].[SecurityMemberships]
GO
/****** Object:  Table [dbo].[ApplicationMessagingInstances]    Script Date: 06/11/2008 13:58:03 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationMessagingInstances]') AND type in (N'U'))
DROP TABLE [dbo].[ApplicationMessagingInstances]
GO
/****** Object:  Table [dbo].[ApplicationMessages]    Script Date: 06/11/2008 13:58:34 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationMessages]') AND type in (N'U'))
DROP TABLE [dbo].[ApplicationMessages]
GO

/****** Object:  Table [dbo].[ApplicationMessagingUploadTokens]    Script Date: 08/07/2008 10:57:33 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationMessagingUploadTokens]') AND type in (N'U'))
DROP TABLE [dbo].[ApplicationMessagingUploadTokens]
GO

-- For in-memory security (postponed)
/****** Object:  Table [dbo].[SecurityExplicitEntries]    Script Date: 08/18/2008 22:08:02 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityExplicitEntries]') AND type in (N'U'))
DROP TABLE [dbo].[SecurityExplicitEntries]
GO

/****** Object:  Table [dbo].[SecurityInheritanceBreaks]    Script Date: 08/21/2008 16:09:05 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityInheritanceBreaks]') AND type in (N'U'))
DROP TABLE [dbo].[SecurityInheritanceBreaks]
GO

/****** Object:  Table [dbo].[LogCategoriesEntries]    Script Date: 10/09/2009 10:01:54 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogCategoriesEntries]') AND type in (N'U'))
DROP TABLE [dbo].[LogCategoriesEntries]
GO

/****** Object:  Table [dbo].[LogEntries]    Script Date: 10/09/2009 10:01:54 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogEntries]') AND type in (N'U'))
DROP TABLE [dbo].[LogEntries]
GO


/****** Object:  Table [dbo].[IndexingActivity]    Script Date: 09/01/2010 05:28:15 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IndexingActivity]') AND type in (N'U'))
DROP TABLE [dbo].[IndexingActivity]
GO

/****** [DF_IndexBackup_RowGuid] ******/
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_IndexBackup_RowGuid]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[IndexBackup] DROP CONSTRAINT [DF_IndexBackup_RowGuid]
END
GO
/****** [DF_IndexBackup_RowGuid] ******/
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_IndexBackup2_RowGuid]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[IndexBackup2] DROP CONSTRAINT [DF_IndexBackup2_RowGuid]
END
GO

/****** Object:  Table [dbo].[IndexBackup]    Script Date: 10/27/2010 20:58:01 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IndexBackup]') AND type in (N'U'))
DROP TABLE [dbo].[IndexBackup]
GO
/****** Object:  Table [dbo].[IndexBackup]    Script Date: 10/27/2010 20:58:01 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IndexBackup2]') AND type in (N'U'))
DROP TABLE [dbo].[IndexBackup2]
GO

/****** Object:  Table [dbo].[WorkflowNotification]   ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WorkflowNotification]') AND type in (N'U'))
DROP TABLE [dbo].[WorkflowNotification]
GO

/****** Object:  Table [dbo].SchemaModification]   ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaModification]') AND type in (N'U'))
DROP TABLE [dbo].[SchemaModification]
GO

/****** Object:  Table [dbo].[Messaging.Subscriptions]    Script Date: 03/11/2011 05:09:49 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messaging.Subscriptions]') AND type in (N'U'))
DROP TABLE [dbo].[Messaging.Subscriptions]
GO

/****** Object:  Table [dbo].[Messaging.Events]    Script Date: 03/11/2011 05:13:25 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messaging.Events]') AND type in (N'U'))
DROP TABLE [dbo].[Messaging.Events]
GO

/****** Object:  Table [dbo].[Messaging.Messages]    Script Date: 03/11/2011 05:15:42 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messaging.Messages]') AND type in (N'U'))
DROP TABLE [dbo].[Messaging.Messages]
GO

/****** Object:  Table [dbo].[Messaging.LastProcessTime]    Script Date: 03/11/2011 05:15:42 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messaging.LastProcessTime]') AND type in (N'U'))
DROP TABLE [dbo].[Messaging.LastProcessTime]
GO

/****** Object:  Table [dbo].[Messaging.Synchronization]    Script Date: 03/23/2011 14:45:30 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messaging.Synchronization]') AND type in (N'U'))
DROP TABLE [dbo].[Messaging.Synchronization]
GO

/****** Object:  Table [dbo].[Packages]  ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Packages]') AND type in (N'U'))
DROP TABLE [dbo].[Packages]
GO

------------------------------------------------                           --------------------------------------------------
------------------------------------------------ ENABLE SNAPSHOT ISOLATION --------------------------------------------------
------------------------------------------------                           --------------------------------------------------

DECLARE @dbName sysname,
        @cmd1 nvarchar(max),
        @cmd2 nvarchar(max);

SET @dbName = DB_NAME()

SET @cmd1 = N'ALTER DATABASE ' + quotename(@dbName) + N' SET ALLOW_SNAPSHOT_ISOLATION on'
SET @cmd2 = N'ALTER DATABASE ' + quotename(@dbName) + N' SET READ_COMMITTED_SNAPSHOT on'

BEGIN TRY
	EXEC(@cmd1)
	EXEC(@cmd2)
END TRY
BEGIN CATCH
   	print '!!! Can not enable snapshot isolation mode. (Warning).'
END CATCH;
GO

------------------------------------------------               --------------------------------------------------------------
------------------------------------------------ CREATE TABLES --------------------------------------------------------------
------------------------------------------------               --------------------------------------------------------------


/****** Object:  Table [dbo].[SchemaPermissionTypes]    Script Date: 10/25/2007 15:50:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaPermissionTypes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SchemaPermissionTypes](
	[PermissionId] [int] IDENTITY(1,1) NOT NULL,
	[PermissionIndex] [int] NULL,
	[Name] [nvarchar](450) NOT NULL,
 	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED 
(
	[PermissionId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[SchemaPropertySetTypes]    Script Date: 10/25/2007 15:50:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaPropertySetTypes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SchemaPropertySetTypes](
	[PropertySetTypeId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](450) NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_PropertySetTypes] PRIMARY KEY CLUSTERED 
(
	[PropertySetTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SchemaDataTypes]    Script Date: 10/25/2007 15:50:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaDataTypes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SchemaDataTypes](
	[DataTypeId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_DataTypes] PRIMARY KEY CLUSTERED 
(
	[DataTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[ReferenceProperties]    Script Date: 10/25/2007 15:50:19 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ReferenceProperties](
	[ReferencePropertyId] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NOT NULL,
	[PropertyTypeId] [int] NOT NULL,
	[ReferredNodeId] [int] NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_ReferenceProperties] PRIMARY KEY CLUSTERED 
(
	[ReferencePropertyId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]') AND name = N'IX_VersionIdPropertyTypeId')
CREATE NONCLUSTERED INDEX [IX_VersionIdPropertyTypeId] ON [dbo].[ReferenceProperties]
(
	[VersionId] ASC,
	[PropertyTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[VersionExtensions]    Script Date: 10/25/2007 15:50:33 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VersionExtensions]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[VersionExtensions](
	[VersionId] [int] NOT NULL,
	[ExtensionTypeId] [int] NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_VersionExtensionTypes] PRIMARY KEY CLUSTERED 
(
	[VersionId] ASC,
	[ExtensionTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[TextPropertiesNText]    Script Date: 10/25/2007 15:50:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TextPropertiesNText]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[TextPropertiesNText](
	[TextPropertyNTextId] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NOT NULL,
	[PropertyTypeId] [int] NOT NULL,
	[Value] [ntext] NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_TextPropertiesNText] PRIMARY KEY CLUSTERED 
(
	[TextPropertyNTextId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[TextPropertiesNVarchar]    Script Date: 10/25/2007 15:50:32 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TextPropertiesNVarchar]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[TextPropertiesNVarchar](
	[TextPropertyNVarcharId] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NOT NULL,
	[PropertyTypeId] [int] NOT NULL,
	[Value] [nvarchar](4000) NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_TextPropertiesNVarchar] PRIMARY KEY CLUSTERED 
(
	[TextPropertyNVarcharId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[BinaryProperties]    Script Date: 10/25/2007 15:49:18 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BinaryProperties]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[BinaryProperties](
	[BinaryPropertyId] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NULL,
	[PropertyTypeId] [int] NULL,
	[ContentType] [nvarchar](450) NOT NULL,
	[FileNameWithoutExtension] [nvarchar](450) NULL,
	[Extension] [nvarchar](50) NOT NULL,
	[Size] [bigint] NOT NULL,
	[Checksum] [varchar](200) NULL,
	[Stream] VARBINARY(MAX) NULL,
	[CreationDate] [datetime] NOT NULL CONSTRAINT [DF_BinaryProperties_CreationDate]  DEFAULT (getutcdate()),
	--[BinaryPropertyGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID (),
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL unique DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
	[Staging] bit NULL
 CONSTRAINT [PK_BinaryProperties] PRIMARY KEY CLUSTERED 
(
	[BinaryPropertyId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY];
END
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Nodes]    Script Date: 10/25/2007 15:50:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Nodes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Nodes](
	[NodeId] [int] IDENTITY(1,1) NOT NULL,
	[NodeTypeId] [int] NOT NULL,
	[ContentListTypeId] [int] NULL,
	[ContentListId] [int] NULL,
	[CreatingInProgress] [tinyint] NOT NULL,
	[IsDeleted] [tinyint] NOT NULL,
	[IsInherited] [tinyint] NOT NULL CONSTRAINT [DF_Nodes_IsInherited]  DEFAULT ((1)),
	[ParentNodeId] [int] NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Path] [nvarchar](450) COLLATE Latin1_General_CI_AS NOT NULL,
	[Index] [int] NOT NULL,
	[Locked] [tinyint] NOT NULL,
	[LockedById] [int] NULL,
	[ETag] [varchar](50) NOT NULL,
	[LockType] [int] NOT NULL,
	[LockTimeout] [int] NOT NULL,
	[LockDate] [datetime] NOT NULL,
	[LockToken] [varchar](50) NOT NULL,
	[LastLockUpdate] [datetime] NOT NULL,
	[LastMinorVersionId] [int] NULL,
	[LastMajorVersionId] [int] NULL,
	[CreationDate] [datetime] NOT NULL,
	[CreatedById] [int] NOT NULL,
	[ModificationDate] [datetime] NOT NULL,
	[ModifiedById] [int] NOT NULL,
	[DisplayName] [nvarchar](450) NULL,
	[IsSystem] [tinyint] NULL,
	[ClosestSecurityNodeId] [int] NULL,
	[SavingState] [int] NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_tblFpsNodes] PRIMARY KEY CLUSTERED 
(
	[NodeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
ALTER TABLE [dbo].[Nodes] ADD  CONSTRAINT [DF_Nodes_CreatingInProgress]  DEFAULT ((0)) FOR [CreatingInProgress]
GO
SET ANSI_PADDING OFF
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Nodes]') AND name = N'IX_Nodes_Path')
CREATE UNIQUE NONCLUSTERED INDEX [IX_Nodes_Path] ON [dbo].[Nodes] 
(
	[Path] ASC
) Include([NodeId])  WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FlatProperties]    Script Date: 10/25/2007 15:50:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FlatProperties]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[FlatProperties](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NOT NULL,
	[Page] [int] NOT NULL,
	[nvarchar_1] [nvarchar](450) NULL,
	[nvarchar_2] [nvarchar](450) NULL,
	[nvarchar_3] [nvarchar](450) NULL,
	[nvarchar_4] [nvarchar](450) NULL,
	[nvarchar_5] [nvarchar](450) NULL,
	[nvarchar_6] [nvarchar](450) NULL,
	[nvarchar_7] [nvarchar](450) NULL,
	[nvarchar_8] [nvarchar](450) NULL,
	[nvarchar_9] [nvarchar](450) NULL,
	[nvarchar_10] [nvarchar](450) NULL,
	[nvarchar_11] [nvarchar](450) NULL,
	[nvarchar_12] [nvarchar](450) NULL,
	[nvarchar_13] [nvarchar](450) NULL,
	[nvarchar_14] [nvarchar](450) NULL,
	[nvarchar_15] [nvarchar](450) NULL,
	[nvarchar_16] [nvarchar](450) NULL,
	[nvarchar_17] [nvarchar](450) NULL,
	[nvarchar_18] [nvarchar](450) NULL,
	[nvarchar_19] [nvarchar](450) NULL,
	[nvarchar_20] [nvarchar](450) NULL,
	[nvarchar_21] [nvarchar](450) NULL,
	[nvarchar_22] [nvarchar](450) NULL,
	[nvarchar_23] [nvarchar](450) NULL,
	[nvarchar_24] [nvarchar](450) NULL,
	[nvarchar_25] [nvarchar](450) NULL,
	[nvarchar_26] [nvarchar](450) NULL,
	[nvarchar_27] [nvarchar](450) NULL,
	[nvarchar_28] [nvarchar](450) NULL,
	[nvarchar_29] [nvarchar](450) NULL,
	[nvarchar_30] [nvarchar](450) NULL,
	[nvarchar_31] [nvarchar](450) NULL,
	[nvarchar_32] [nvarchar](450) NULL,
	[nvarchar_33] [nvarchar](450) NULL,
	[nvarchar_34] [nvarchar](450) NULL,
	[nvarchar_35] [nvarchar](450) NULL,
	[nvarchar_36] [nvarchar](450) NULL,
	[nvarchar_37] [nvarchar](450) NULL,
	[nvarchar_38] [nvarchar](450) NULL,
	[nvarchar_39] [nvarchar](450) NULL,
	[nvarchar_40] [nvarchar](450) NULL,
	[nvarchar_41] [nvarchar](450) NULL,
	[nvarchar_42] [nvarchar](450) NULL,
	[nvarchar_43] [nvarchar](450) NULL,
	[nvarchar_44] [nvarchar](450) NULL,
	[nvarchar_45] [nvarchar](450) NULL,
	[nvarchar_46] [nvarchar](450) NULL,
	[nvarchar_47] [nvarchar](450) NULL,
	[nvarchar_48] [nvarchar](450) NULL,
	[nvarchar_49] [nvarchar](450) NULL,
	[nvarchar_50] [nvarchar](450) NULL,
	[nvarchar_51] [nvarchar](450) NULL,
	[nvarchar_52] [nvarchar](450) NULL,
	[nvarchar_53] [nvarchar](450) NULL,
	[nvarchar_54] [nvarchar](450) NULL,
	[nvarchar_55] [nvarchar](450) NULL,
	[nvarchar_56] [nvarchar](450) NULL,
	[nvarchar_57] [nvarchar](450) NULL,
	[nvarchar_58] [nvarchar](450) NULL,
	[nvarchar_59] [nvarchar](450) NULL,
	[nvarchar_60] [nvarchar](450) NULL,
	[nvarchar_61] [nvarchar](450) NULL,
	[nvarchar_62] [nvarchar](450) NULL,
	[nvarchar_63] [nvarchar](450) NULL,
	[nvarchar_64] [nvarchar](450) NULL,
	[nvarchar_65] [nvarchar](450) NULL,
	[nvarchar_66] [nvarchar](450) NULL,
	[nvarchar_67] [nvarchar](450) NULL,
	[nvarchar_68] [nvarchar](450) NULL,
	[nvarchar_69] [nvarchar](450) NULL,
	[nvarchar_70] [nvarchar](450) NULL,
	[nvarchar_71] [nvarchar](450) NULL,
	[nvarchar_72] [nvarchar](450) NULL,
	[nvarchar_73] [nvarchar](450) NULL,
	[nvarchar_74] [nvarchar](450) NULL,
	[nvarchar_75] [nvarchar](450) NULL,
	[nvarchar_76] [nvarchar](450) NULL,
	[nvarchar_77] [nvarchar](450) NULL,
	[nvarchar_78] [nvarchar](450) NULL,
	[nvarchar_79] [nvarchar](450) NULL,
	[nvarchar_80] [nvarchar](450) NULL,
	[int_1] [int] NULL,
	[int_2] [int] NULL,
	[int_3] [int] NULL,
	[int_4] [int] NULL,
	[int_5] [int] NULL,
	[int_6] [int] NULL,
	[int_7] [int] NULL,
	[int_8] [int] NULL,
	[int_9] [int] NULL,
	[int_10] [int] NULL,
	[int_11] [int] NULL,
	[int_12] [int] NULL,
	[int_13] [int] NULL,
	[int_14] [int] NULL,
	[int_15] [int] NULL,
	[int_16] [int] NULL,
	[int_17] [int] NULL,
	[int_18] [int] NULL,
	[int_19] [int] NULL,
	[int_20] [int] NULL,
	[int_21] [int] NULL,
	[int_22] [int] NULL,
	[int_23] [int] NULL,
	[int_24] [int] NULL,
	[int_25] [int] NULL,
	[int_26] [int] NULL,
	[int_27] [int] NULL,
	[int_28] [int] NULL,
	[int_29] [int] NULL,
	[int_30] [int] NULL,
	[int_31] [int] NULL,
	[int_32] [int] NULL,
	[int_33] [int] NULL,
	[int_34] [int] NULL,
	[int_35] [int] NULL,
	[int_36] [int] NULL,
	[int_37] [int] NULL,
	[int_38] [int] NULL,
	[int_39] [int] NULL,
	[int_40] [int] NULL,
	[datetime_1] [datetime] NULL,
	[datetime_2] [datetime] NULL,
	[datetime_3] [datetime] NULL,
	[datetime_4] [datetime] NULL,
	[datetime_5] [datetime] NULL,
	[datetime_6] [datetime] NULL,
	[datetime_7] [datetime] NULL,
	[datetime_8] [datetime] NULL,
	[datetime_9] [datetime] NULL,
	[datetime_10] [datetime] NULL,
	[datetime_11] [datetime] NULL,
	[datetime_12] [datetime] NULL,
	[datetime_13] [datetime] NULL,
	[datetime_14] [datetime] NULL,
	[datetime_15] [datetime] NULL,
	[datetime_16] [datetime] NULL,
	[datetime_17] [datetime] NULL,
	[datetime_18] [datetime] NULL,
	[datetime_19] [datetime] NULL,
	[datetime_20] [datetime] NULL,
	[datetime_21] [datetime] NULL,
	[datetime_22] [datetime] NULL,
	[datetime_23] [datetime] NULL,
	[datetime_24] [datetime] NULL,
	[datetime_25] [datetime] NULL,
	[money_1] [money] NULL,
	[money_2] [money] NULL,
	[money_3] [money] NULL,
	[money_4] [money] NULL,
	[money_5] [money] NULL,
	[money_6] [money] NULL,
	[money_7] [money] NULL,
	[money_8] [money] NULL,
	[money_9] [money] NULL,
	[money_10] [money] NULL,
	[money_11] [money] NULL,
	[money_12] [money] NULL,
	[money_13] [money] NULL,
	[money_14] [money] NULL,
	[money_15] [money] NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_FlatProperties_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[FlatProperties]') AND name = N'IX_FlatProperties')
CREATE UNIQUE NONCLUSTERED INDEX [IX_FlatProperties] ON [dbo].[FlatProperties] 
(
	[Page] ASC,
	[VersionId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SchemaPropertySetsPropertyTypes]    Script Date: 10/25/2007 15:50:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaPropertySetsPropertyTypes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SchemaPropertySetsPropertyTypes](
	[PropertyTypeId] [int] NOT NULL,
	[PropertySetId] [int] NOT NULL,
	[IsDeclared] [tinyint] NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_SchemaPropertySetsPropertyTypes] PRIMARY KEY CLUSTERED 
(
	[PropertyTypeId] ASC,
	[PropertySetId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Versions]    Script Date: 10/25/2007 15:50:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Versions]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Versions](
	[VersionId] [int] IDENTITY(1,1) NOT NULL,
	[NodeId] [int] NOT NULL,
	[MajorNumber] [smallint] NOT NULL,
	[MinorNumber] [smallint] NOT NULL,
	[CreationDate] [datetime] NOT NULL,
	[CreatedById] [int] NOT NULL,
	[ModificationDate] [datetime] NOT NULL,
	[ModifiedById] [int] NOT NULL,
	[Status] [smallint] NOT NULL CONSTRAINT [DF_Versions_Status]  DEFAULT ((1)),
	[IndexDocument] VARBINARY(MAX) NULL,
	[ChangedData] NTEXT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_Versions] PRIMARY KEY CLUSTERED 
(
	[VersionId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[SchemaPropertySets]    Script Date: 10/25/2007 15:50:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaPropertySets]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SchemaPropertySets](
	[PropertySetId] [int] IDENTITY(1,1) NOT NULL,
	[ParentId] [int] NULL,
	[Name] [varchar](450) NOT NULL,
	[PropertySetTypeId] [int] NOT NULL,
	[ClassName] [varchar](450) NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_PropertySets] PRIMARY KEY CLUSTERED 
(
	[PropertySetId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SchemaPropertyTypes]    Script Date: 10/25/2007 15:50:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaPropertyTypes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SchemaPropertyTypes](
	[PropertyTypeId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](450) NOT NULL,
	[DataTypeId] [int] NOT NULL,
	[Mapping] [int] NOT NULL,
	[IsContentListProperty] [tinyint] NOT NULL DEFAULT 0,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_PropertySlots] PRIMARY KEY CLUSTERED 
(
	[PropertyTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[SecurityEntries]    Script Date: 11/22/2007 16:39:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityEntries]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SecurityEntries](
	[SecurityEntryId] [int] IDENTITY(1,1) NOT NULL,
	[DefinedOnNodeId] [int] NOT NULL,
	[PrincipalId] [int] NOT NULL,
	[IsInheritable] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_IsInheritable]  DEFAULT ((1)),
	[PermissionValue1] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue1]  DEFAULT ((0)),
	[PermissionValue2] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue2]  DEFAULT ((0)),
	[PermissionValue3] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue3]  DEFAULT ((0)),
	[PermissionValue4] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue4]  DEFAULT ((0)),
	[PermissionValue5] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue5]  DEFAULT ((0)),
	[PermissionValue6] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue6]  DEFAULT ((0)),
	[PermissionValue7] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue7]  DEFAULT ((0)),
	[PermissionValue8] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue8]  DEFAULT ((0)),
	[PermissionValue9] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue9]  DEFAULT ((0)),
	[PermissionValue10] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue10]  DEFAULT ((0)),
	[PermissionValue11] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue11]  DEFAULT ((0)),
	[PermissionValue12] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue12]  DEFAULT ((0)),
	[PermissionValue13] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue13]  DEFAULT ((0)),
	[PermissionValue14] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue14]  DEFAULT ((0)),
	[PermissionValue15] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue15]  DEFAULT ((0)),
	[PermissionValue16] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue16]  DEFAULT ((0)),

	[PermissionValue17] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue17]  DEFAULT ((0)),
	[PermissionValue18] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue18]  DEFAULT ((0)),
	[PermissionValue19] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue19]  DEFAULT ((0)),
	[PermissionValue20] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue20]  DEFAULT ((0)),
	[PermissionValue21] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue21]  DEFAULT ((0)),
	[PermissionValue22] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue22]  DEFAULT ((0)),
	[PermissionValue23] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue23]  DEFAULT ((0)),
	[PermissionValue24] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue24]  DEFAULT ((0)),
	[PermissionValue25] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue25]  DEFAULT ((0)),
	[PermissionValue26] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue26]  DEFAULT ((0)),
	[PermissionValue27] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue27]  DEFAULT ((0)),
	[PermissionValue28] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue28]  DEFAULT ((0)),
	[PermissionValue29] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue29]  DEFAULT ((0)),
	[PermissionValue30] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue30]  DEFAULT ((0)),
	[PermissionValue31] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue31]  DEFAULT ((0)),
	[PermissionValue32] [tinyint] NOT NULL CONSTRAINT [DF_SecurityEntries_PermissionValue32]  DEFAULT ((0)),
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_SecurityEntries] PRIMARY KEY CLUSTERED 
(
	[SecurityEntryId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/****** Object:  Table [dbo].[SecurityCustomEntries] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityCustomEntries]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SecurityCustomEntries](
	[SecurityEntryId] [int] IDENTITY(1,1) NOT NULL,
	[DefinedOnNodeId] [int] NOT NULL,
	[PrincipalId] [int] NOT NULL,
	[IsInheritable] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_IsInheritable]  DEFAULT ((1)),
	[PermissionValue1] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue1]  DEFAULT ((0)),
	[PermissionValue2] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue2]  DEFAULT ((0)),
	[PermissionValue3] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue3]  DEFAULT ((0)),
	[PermissionValue4] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue4]  DEFAULT ((0)),
	[PermissionValue5] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue5]  DEFAULT ((0)),
	[PermissionValue6] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue6]  DEFAULT ((0)),
	[PermissionValue7] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue7]  DEFAULT ((0)),
	[PermissionValue8] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue8]  DEFAULT ((0)),
	[PermissionValue9] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue9]  DEFAULT ((0)),
	[PermissionValue10] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue10]  DEFAULT ((0)),
	[PermissionValue11] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue11]  DEFAULT ((0)),
	[PermissionValue12] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue12]  DEFAULT ((0)),
	[PermissionValue13] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue13]  DEFAULT ((0)),
	[PermissionValue14] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue14]  DEFAULT ((0)),
	[PermissionValue15] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue15]  DEFAULT ((0)),
	[PermissionValue16] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue16]  DEFAULT ((0)),

	[PermissionValue17] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue17]  DEFAULT ((0)),
	[PermissionValue18] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue18]  DEFAULT ((0)),
	[PermissionValue19] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue19]  DEFAULT ((0)),
	[PermissionValue20] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue20]  DEFAULT ((0)),
	[PermissionValue21] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue21]  DEFAULT ((0)),
	[PermissionValue22] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue22]  DEFAULT ((0)),
	[PermissionValue23] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue23]  DEFAULT ((0)),
	[PermissionValue24] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue24]  DEFAULT ((0)),
	[PermissionValue25] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue25]  DEFAULT ((0)),
	[PermissionValue26] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue26]  DEFAULT ((0)),
	[PermissionValue27] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue27]  DEFAULT ((0)),
	[PermissionValue28] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue28]  DEFAULT ((0)),
	[PermissionValue29] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue29]  DEFAULT ((0)),
	[PermissionValue30] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue30]  DEFAULT ((0)),
	[PermissionValue31] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue31]  DEFAULT ((0)),
	[PermissionValue32] [tinyint] NOT NULL CONSTRAINT [DF_SecurityCustomEntries_PermissionValue32]  DEFAULT ((0)),
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_SecurityCustomEntries] PRIMARY KEY CLUSTERED 
(
	[SecurityEntryId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/****** Object:  Table [dbo].[SecurityMemberships]    Script Date: 05/13/2009 11:12:02 ******/
SET ANSI_NULLS ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SecurityMemberships]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[SecurityMemberships](
	[ContainerId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[ContainerType] [char](1) NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_SecurityMemberships] PRIMARY KEY CLUSTERED 
(
	[ContainerId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[ApplicationMessagingInstances]    Script Date: 06/11/2008 13:46:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationMessagingInstances]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ApplicationMessagingInstances](
	[ApplicationInstanceId] [int] IDENTITY(1,1) NOT NULL,
	[AppDomainFriendlyName] [nvarchar](450) NOT NULL,
	[MachineName] [nvarchar](450) NOT NULL,
	[RegisteredOn] [datetime] NOT NULL,
	[LastActivityOn] [datetime] NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_ApplicationMessagingInstances] PRIMARY KEY CLUSTERED 
(
	[ApplicationInstanceId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[ApplicationMessages]    Script Date: 06/11/2008 13:45:48 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationMessages]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ApplicationMessages](
	[ApplicationMessageId] [int] IDENTITY(1,1) NOT NULL,
	[SenderApplicationId] [int] NOT NULL,
	[TargetApplicationId] [int] NOT NULL,
	[MessageType] [int] NOT NULL,
	[Message] [nvarchar](450) NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_ApplicationMessages] PRIMARY KEY CLUSTERED 
(
	[ApplicationMessageId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO

/****** Object:  Table [dbo].[ApplicationMessagingUploadTokens]    Script Date: 08/07/2008 10:56:36 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationMessagingUploadTokens]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ApplicationMessagingUploadTokens](
	[Token] [uniqueidentifier] NOT NULL,
	[UserId] [int] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_ApplicationMessagingUploadTokens] PRIMARY KEY CLUSTERED 
(
	[Token] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO

------------------------------------------------              --------------------------------------------------------------
------------------------------------------------ CREATE VIEWS --------------------------------------------------------------
------------------------------------------------              --------------------------------------------------------------

/******  Object:  View [dbo].[NodeInfoView]  ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[NodeInfoView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[NodeInfoView]
AS
SELECT     N.NodeId, T.Name AS Type, N.Name, N.Path, N.LockedById, V.VersionId, CONVERT(Varchar, V.MajorNumber) + ''.'' + CONVERT(Varchar, V.MinorNumber) 
                      + ''.'' + CASE [Status] WHEN 1 THEN ''A'' WHEN 2 THEN ''L'' WHEN 4 THEN ''D'' WHEN 8 THEN ''R'' WHEN 16 THEN ''P'' ELSE '''' END AS Version, 
                      CASE V.VersionId WHEN N .LastMajorVersionId THEN ''TRUE'' ELSE ''false'' END AS LastPub, 
                      CASE V.VersionId WHEN N .LastMinorVersionId THEN ''TRUE'' ELSE ''false'' END AS LastWork, 
                      CASE F.int_4 WHEN 1 THEN ''off'' WHEN 2 THEN ''ON'' ELSE ''inh'' END AS AMode, 
                      CASE F.int_1 WHEN 1 THEN ''None'' WHEN 2 THEN ''Major'' WHEN 3 THEN ''Full'' ELSE ''inh'' END AS VMode
FROM         dbo.Versions AS V INNER JOIN
                      dbo.Nodes AS N ON V.NodeId = N.NodeId INNER JOIN
                      dbo.SchemaPropertySets AS T ON N.NodeTypeId = T.PropertySetId LEFT OUTER JOIN
                      dbo.FlatProperties AS F ON V.VersionId = F.VersionId AND F.Page = 0
'
GO
/******  Object:  View [dbo].[PropertyInfoView]  ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[PropertyInfoView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[PropertyInfoView]
AS
WITH PropInfo(PropId, Page, [Table], [Column]) AS (
	SELECT     PropertyTypeId AS PropId, 
		CASE PT.DataTypeId WHEN 1 THEN PT.Mapping / 80 WHEN 3 THEN PT.Mapping / 40 WHEN 5 THEN PT.Mapping / 25 WHEN 4 THEN PT.Mapping / 15 ELSE NULL END AS Page,
		CASE PT.DataTypeId WHEN 2 THEN ''Text'' WHEN 6 THEN ''Binary''  WHEN 7 THEN ''Reference''  ELSE ''Flat'' END AS [Table],
		CASE PT.DataTypeId WHEN 1 THEN ''nvarchar_'' + CONVERT(VARCHAR, PT.Mapping % 80 + 1) WHEN 2 THEN NULL WHEN 3 THEN ''int_'' + CONVERT(VARCHAR, PT.Mapping % 40 + 1) WHEN 4 THEN ''money_'' + CONVERT(VARCHAR, PT.Mapping % 15 + 1) WHEN 5 THEN ''datetime_'' + CONVERT(VARCHAR, PT.Mapping % 25 + 1) WHEN 6 THEN NULL WHEN 7 THEN NULL ELSE NULL END AS [Column]
    FROM SchemaPropertyTypes AS PT)
SELECT
	PT.PropertyTypeId AS Id, PT.Name, DT.Name AS [Type], PT.Mapping, PT.IsContentListProperty AS IsList, P.[Table], P.Page, P.[Column]
FROM
	SchemaPropertyTypes PT 
		INNER JOIN SchemaDataTypes DT ON DT.DataTypeId = PT.DataTypeId
			INNER JOIN PropInfo P ON P.PropId = PT.PropertyTypeId
'
GO
/****** Object:  View [dbo].[PropertySetsInfoView]    Script Date: 08/13/2007 13:40:03 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[PropertySetsInfoView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[PropertySetsInfoView]
AS
SELECT PS.PropertySetId, PS.ParentId, PS.Name AS PropertySet, PSP.Name AS Parent, 
	PST.Name AS SetType, PT.Name AS Property, DT.Name AS DataType, PT.Mapping, Link.IsDeclared
FROM SchemaPropertySets AS PS
	INNER JOIN SchemaPropertySetTypes PST ON PS.PropertySetTypeId = PST.PropertySetTypeId
	LEFT OUTER JOIN SchemaPropertySets PSP ON PS.ParentId = PSP.PropertySetId
	LEFT OUTER JOIN SchemaPropertyTypes PT
		INNER JOIN SchemaPropertySetsPropertyTypes Link ON PT.PropertyTypeId = Link.PropertyTypeId
		INNER JOIN SchemaDataTypes DT ON PT.DataTypeId = DT.DataTypeId
	ON PS.PropertySetId = Link.PropertySetId
'
GO
/****** Object:  View [dbo].[ReferencesInfoView]    Script Date: 08/07/2007 14:50:18 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[ReferencesInfoView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[ReferencesInfoView]
AS
--SELECT     Nodes.Name AS SrcName, ''V'' + CAST(Versions.MajorNumber AS nvarchar(20)) + ''.'' + CAST(Versions.MinorNumber AS nvarchar(20)) AS SrcVer, 
--                      Slots.Name AS RelType, RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, 
--                      RefNodes.Path AS TargetPath
--FROM         dbo.ReferenceProperties AS Refs INNER JOIN
--                      dbo.Versions AS Versions ON Refs.VersionId = Versions.VersionId INNER JOIN
--                      dbo.Nodes AS Nodes ON Versions.NodeId = Nodes.NodeId INNER JOIN
--                      dbo.Nodes AS RefNodes ON Refs.ReferredNodeId = RefNodes.NodeId INNER JOIN
--                      dbo.SchemaPropertyTypes AS Slots ON Refs.PropertyTypeId = Slots.PropertyTypeId

-- ReferenceProperties
	SELECT     Nodes.Name AS SrcName, ''V'' + CAST(Versions.MajorNumber AS nvarchar(20)) + ''.'' + CAST(Versions.MinorNumber AS nvarchar(20)) AS SrcVer, 
						  Slots.Name AS RelType, RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, 
						  RefNodes.Path AS TargetPath
	FROM         dbo.ReferenceProperties AS Refs INNER JOIN
						  dbo.Versions AS Versions ON Refs.VersionId = Versions.VersionId INNER JOIN
						  dbo.Nodes AS Nodes ON Versions.NodeId = Nodes.NodeId INNER JOIN
						  dbo.Nodes AS RefNodes ON Refs.ReferredNodeId = RefNodes.NodeId INNER JOIN
						  dbo.SchemaPropertyTypes AS Slots ON Refs.PropertyTypeId = Slots.PropertyTypeId
UNION ALL
-- Parent
	SELECT     Nodes.Name AS SrcName, ''V*.*'' AS SrcVer, ''Parent'' AS RelType, RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, 
						  RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, RefNodes.Path AS TargetPath
	FROM         dbo.Nodes AS Nodes INNER JOIN
						  dbo.Nodes AS RefNodes ON Nodes.ParentNodeId = RefNodes.NodeId
UNION ALL
-- LockedById
	SELECT     Nodes.Name AS SrcName, ''V*.*'' AS SrcVer, ''LockedById'' AS RelType, RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, 
						  RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, RefNodes.Path AS TargetPath
	FROM         dbo.Nodes AS Nodes INNER JOIN
						  dbo.Nodes AS RefNodes ON Nodes.LockedById = RefNodes.NodeId
UNION ALL
-- CreatedById
	SELECT     Nodes.Name AS SrcName, ''V'' + CAST(Versions.MajorNumber AS nvarchar(20)) + ''.'' + CAST(Versions.MinorNumber AS nvarchar(20)) AS SrcVer, 
						  ''CreatedById'', RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, 
						  RefNodes.Path AS TargetPath
	FROM         dbo.Nodes AS Nodes INNER JOIN
		                  dbo.Versions AS Versions ON Nodes.NodeId = Versions.NodeId INNER JOIN
			              dbo.Nodes AS RefNodes ON Versions.CreatedById = RefNodes.NodeId
UNION ALL
-- ModifiedById
	SELECT     Nodes.Name AS SrcName, ''V'' + CAST(Versions.MajorNumber AS nvarchar(20)) + ''.'' + CAST(Versions.MinorNumber AS nvarchar(20)) AS SrcVer, 
						  ''ModifiedById'', RefNodes.Name AS TargetName, Nodes.NodeId AS SrcId, RefNodes.NodeId AS TargetId, Nodes.Path AS SrcPath, 
						  RefNodes.Path AS TargetPath
	FROM         dbo.Nodes AS Nodes INNER JOIN
		                  dbo.Versions AS Versions ON Nodes.NodeId = Versions.NodeId INNER JOIN
			              dbo.Nodes AS RefNodes ON Versions.ModifiedById = RefNodes.NodeId
'
GO
/****** Object:  View [dbo].[PermissionInfoView]    Script Date: 08/13/2007 13:40:03 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[PermissionInfoView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[PermissionInfoView]
AS
SELECT 
	COALESCE(E.DefinedOnNodeId, C.DefinedOnNodeId) AS DefinedOn,
	DefinedOn.Name AS DefinedOnName, 
	DefinedOn.Path AS DefinedOnPath,
	CASE DefinedOn.IsInherited WHEN 0 THEN ''BREAK'' WHEN 1 THEN ''Inherited'' END AS Inheritance, 
	[Identity].NodeId AS IdentityId, [Identity].Name AS IdentityName, [Identity].Path AS IdentityPath,
	CASE COALESCE(E.IsInheritable, C.IsInheritable) WHEN 0 THEN ''LOCALONLY'' WHEN 1 THEN ''Propagated'' END AS Propagation, 
	CASE E.PermissionValue1 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS See, 
	CASE E.PermissionValue16 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Preview,
	CASE E.PermissionValue17 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS PWater,
	CASE E.PermissionValue18 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS PReda,
	CASE E.PermissionValue2 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS [Open], 
	CASE E.PermissionValue3 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS OpenMinor, 
	CASE E.PermissionValue4 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS [Save], 
	CASE E.PermissionValue5 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Publish, 
	CASE E.PermissionValue6 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Checkin, 
	CASE E.PermissionValue7 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS AddNew, 
	CASE E.PermissionValue8 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Approve, 
	CASE E.PermissionValue9 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS [Delete], 
	CASE E.PermissionValue10 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS RecallVer, 
	CASE E.PermissionValue11 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS DeleteVer, 
	CASE E.PermissionValue12 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS SeePerm, 
	CASE E.PermissionValue13 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS SetPerm,
	CASE E.PermissionValue14 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Run,
	CASE E.PermissionValue15 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS ManageLists,

	CASE C.PermissionValue1 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom01,
	CASE C.PermissionValue2 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom02,
	CASE C.PermissionValue3 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom03,
	CASE C.PermissionValue4 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom04,
	CASE C.PermissionValue5 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom05,
	CASE C.PermissionValue6 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom06,
	CASE C.PermissionValue7 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom07,
	CASE C.PermissionValue8 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom08,
	CASE C.PermissionValue9 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom09,
	CASE C.PermissionValue10 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom10,
	CASE C.PermissionValue11 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom11,
	CASE C.PermissionValue12 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom12,
	CASE C.PermissionValue13 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom13,
	CASE C.PermissionValue14 WHEN 0 THEN '''' WHEN 1 THEN ''Allow'' WHEN 2 THEN ''Deny'' END AS Custom14
FROM SecurityEntries E
	FULL JOIN SecurityCustomEntries C ON E.DefinedOnNodeId = C.DefinedOnNodeId AND E.PrincipalId = C.PrincipalId AND E.IsInheritable = C.IsInheritable
	INNER JOIN Nodes DefinedOn ON COALESCE(E.DefinedOnNodeId, C.DefinedOnNodeId) = DefinedOn.NodeId
	INNER JOIN Nodes AS [Identity] ON COALESCE(E.PrincipalId, C.PrincipalId) = [Identity].NodeId
'
GO
/****** Object:  View [dbo].[MembershipInfoView]    Script Date: 03/01/2012 04:03:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[MembershipInfoView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[MembershipInfoView]
AS
SELECT     m.ContainerId, c.Name AS ContainerName, c.Path AS ContainerPath, 
                      CASE m.ContainerType WHEN ''O'' THEN ''OU'' WHEN ''G'' THEN ''Group'' ELSE m.ContainerType END AS ContainerType, m.UserId, 
                      u.Name AS UserName, u.Path AS UserPath
FROM         dbo.SecurityMemberships AS m INNER JOIN
                      dbo.Nodes AS c ON c.NodeId = m.ContainerId INNER JOIN
                      dbo.Nodes AS u ON u.NodeId = m.UserId
'
GO

/****** Object:  View [dbo].[SysSearchView]    Script Date: 08/07/2007 14:50:18 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[SysSearchView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[SysSearchView]
AS
SELECT
	N.NodeId, 
	N.NodeTypeId, 
	N.ContentListId, 
	N.ContentListTypeId, 
	n.CreatingInProgress, 
	N.IsDeleted, 
	N.IsInherited, 
	N.ParentNodeId, 
	N.Name, 
	N.Path, 
	N.[Index],
	V.VersionId, 
	V.MajorNumber, 
	V.MinorNumber, 
	V.CreationDate, 
	V.CreatedById, 
	V.ModificationDate, 
	V.ModifiedById, 
	V.Status,
	N.Locked, 
	N.LockedById, 
	N.ETag, 
	N.LockType, 
	N.LockTimeout, 
	N.LockDate, 
	N.LockToken, 
	N.LastLockUpdate
FROM dbo.Nodes AS N 
	INNER JOIN dbo.Versions AS V ON N.LastMinorVersionId = V.VersionId OR N.LastMajorVersionId = V.VersionId
'
GO
/****** Object:  View [dbo].[SysSearchWithFlatsView]    Script Date: 08/07/2007 14:50:18 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[SysSearchWithFlatsView]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[SysSearchWithFlatsView]
AS
SELECT
N.NodeId, 
N.NodeTypeId, 
N.ContentListId, 
N.ContentListTypeId, 
N.CreatingInProgress,
N.IsDeleted, 
N.IsInherited,
N.ParentNodeId, 
N.Name, 
N.Path, 
V.VersionId, 
V.MajorNumber, 
V.MinorNumber, 
V.CreationDate, 
V.CreatedById, 
V.ModificationDate, 
V.ModifiedById, 
V.Status,
N.Locked, 
N.LockedById, 
N.ETag, 
N.LockType, 
N.LockTimeout, 
N.LockDate, 
N.LockToken, 
N.LastLockUpdate, 
N.[Index],
		F.Page, F.nvarchar_1, F.nvarchar_2, F.nvarchar_3, F.nvarchar_4, F.nvarchar_5, F.nvarchar_6, F.nvarchar_7, 
		F.nvarchar_8, F.nvarchar_9, F.nvarchar_10, F.nvarchar_11, F.nvarchar_12, F.nvarchar_13, F.nvarchar_14, F.nvarchar_15, F.nvarchar_16, 
		F.nvarchar_17, F.nvarchar_18, F.nvarchar_19, F.nvarchar_20, F.nvarchar_21, F.nvarchar_22, F.nvarchar_23, F.nvarchar_24, F.nvarchar_25, 
		F.nvarchar_26, F.nvarchar_27, F.nvarchar_28, F.nvarchar_29, F.nvarchar_30, F.nvarchar_31, F.nvarchar_32, F.nvarchar_33, F.nvarchar_34, 
		F.nvarchar_35, F.nvarchar_36, F.nvarchar_37, F.nvarchar_38, F.nvarchar_39, F.nvarchar_40, F.nvarchar_41, F.nvarchar_42, F.nvarchar_43, 
		F.nvarchar_44, F.nvarchar_45, F.nvarchar_46, F.nvarchar_47, F.nvarchar_48, F.nvarchar_49, F.nvarchar_50, F.nvarchar_51, F.nvarchar_52, 
		F.nvarchar_53, F.nvarchar_54, F.nvarchar_55, F.nvarchar_56, F.nvarchar_57, F.nvarchar_58, F.nvarchar_59, F.nvarchar_60, F.nvarchar_61, 
		F.nvarchar_62, F.nvarchar_63, F.nvarchar_64, F.nvarchar_65, F.nvarchar_66, F.nvarchar_67, F.nvarchar_68, F.nvarchar_69, F.nvarchar_70, 
		F.nvarchar_71, F.nvarchar_72, F.nvarchar_73, F.nvarchar_74, F.nvarchar_75, F.nvarchar_76, F.nvarchar_77, F.nvarchar_78, F.nvarchar_79, 
		F.nvarchar_80, F.int_1, F.int_2, F.int_3, F.int_4, F.int_5, F.int_6, F.int_7, F.int_8, F.int_9, F.int_10, F.int_11, F.int_12, F.int_13, F.int_14, F.int_15, 
		F.int_16, F.int_17, F.int_18, F.int_19, F.int_20, F.int_21, F.int_22, F.int_23, F.int_24, F.int_25, F.int_26, F.int_27, F.int_28, F.int_29, F.int_30, F.int_31, 
		F.int_32, F.int_33, F.int_34, F.int_35, F.int_36, F.int_37, F.int_38, F.int_39, F.int_40, F.datetime_1, F.datetime_2, F.datetime_3, F.datetime_4, 
		F.datetime_5, F.datetime_6, F.datetime_7, F.datetime_8, F.datetime_9, F.datetime_10, F.datetime_11, F.datetime_12, F.datetime_13, F.datetime_14, 
		F.datetime_15, F.datetime_16, F.datetime_17, F.datetime_18, F.datetime_19, F.datetime_20, F.datetime_21, F.datetime_22, F.datetime_23, 
		F.datetime_24, F.datetime_25, F.money_1, F.money_2, F.money_3, F.money_4, F.money_5, F.money_6, F.money_7, F.money_8, F.money_9, 
		F.money_10, F.money_11, F.money_12, F.money_13, F.money_14, F.money_15
FROM         dbo.Nodes AS N 
INNER JOIN dbo.SysSearchView AS S on N.NodeId = S.NodeId
INNER JOIN dbo.Versions AS V ON N.LastMinorVersionId = V.VersionId OR N.LastMajorVersionId = V.VersionId
LEFT OUTER JOIN dbo.FlatProperties AS F ON V.VersionId = F.VersionId
'
GO


------------------------------------------------                    --------------------------------------------------------------
------------------------------------------------ CREATE CONSTRAINTS --------------------------------------------------------------
------------------------------------------------                    --------------------------------------------------------------


/****** Object:  ForeignKey [FK_BinaryProperties_SchemaPropertyTypes]    Script Date: 10/25/2007 15:49:18 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BinaryProperties_SchemaPropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
ALTER TABLE [dbo].[BinaryProperties]  WITH CHECK ADD  CONSTRAINT [FK_BinaryProperties_SchemaPropertyTypes] FOREIGN KEY([PropertyTypeId])
REFERENCES [dbo].[SchemaPropertyTypes] ([PropertyTypeId])
GO
ALTER TABLE [dbo].[BinaryProperties] CHECK CONSTRAINT [FK_BinaryProperties_SchemaPropertyTypes]
GO
/****** Object:  ForeignKey [FK_BinaryProperties_Versions]    Script Date: 10/25/2007 15:49:19 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BinaryProperties_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
ALTER TABLE [dbo].[BinaryProperties]  WITH CHECK ADD  CONSTRAINT [FK_BinaryProperties_Versions] FOREIGN KEY([VersionId])
REFERENCES [dbo].[Versions] ([VersionId])
GO
ALTER TABLE [dbo].[BinaryProperties] CHECK CONSTRAINT [FK_BinaryProperties_Versions]
GO
/****** Object:  ForeignKey [FK_FlatProperties_Versions]    Script Date: 10/25/2007 15:50:10 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_FlatProperties_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[FlatProperties]'))
ALTER TABLE [dbo].[FlatProperties]  WITH CHECK ADD  CONSTRAINT [FK_FlatProperties_Versions] FOREIGN KEY([VersionId])
REFERENCES [dbo].[Versions] ([VersionId])
GO
ALTER TABLE [dbo].[FlatProperties] CHECK CONSTRAINT [FK_FlatProperties_Versions]
GO
/****** Object:  ForeignKey [FK_Nodes_LockedBy]    Script Date: 10/25/2007 15:50:16 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_LockedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_LockedBy] FOREIGN KEY([LockedById])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_LockedBy]
GO
/****** Object:  ForeignKey [FK_Nodes_Parent]    Script Date: 10/25/2007 15:50:16 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_Parent]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_Parent] FOREIGN KEY([ParentNodeId])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_Parent]
GO
/****** Object:  ForeignKey [FK_Nodes_SchemaPropertySets]    Script Date: 10/25/2007 15:50:17 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nodes_SchemaPropertySets]') AND parent_object_id = OBJECT_ID(N'[dbo].[Nodes]'))
ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_SchemaPropertySets] FOREIGN KEY([NodeTypeId])
REFERENCES [dbo].[SchemaPropertySets] ([PropertySetId])
GO
ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_SchemaPropertySets]
GO
/****** Object:  ForeignKey [FK_ReferenceProperties_SchemaPropertyTypes]    Script Date: 10/25/2007 15:50:19 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ReferenceProperties_SchemaPropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[ReferenceProperties]'))
ALTER TABLE [dbo].[ReferenceProperties]  WITH CHECK ADD  CONSTRAINT [FK_ReferenceProperties_SchemaPropertyTypes] FOREIGN KEY([PropertyTypeId])
REFERENCES [dbo].[SchemaPropertyTypes] ([PropertyTypeId])
GO
ALTER TABLE [dbo].[ReferenceProperties] CHECK CONSTRAINT [FK_ReferenceProperties_SchemaPropertyTypes]
GO
/****** Object:  ForeignKey [FK_PropertySets_PropertySets]    Script Date: 10/25/2007 15:50:23 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PropertySets_PropertySets]') AND parent_object_id = OBJECT_ID(N'[dbo].[SchemaPropertySets]'))
ALTER TABLE [dbo].[SchemaPropertySets]  WITH CHECK ADD  CONSTRAINT [FK_PropertySets_PropertySets] FOREIGN KEY([ParentId])
REFERENCES [dbo].[SchemaPropertySets] ([PropertySetId])
GO
ALTER TABLE [dbo].[SchemaPropertySets] CHECK CONSTRAINT [FK_PropertySets_PropertySets]
GO
/****** Object:  ForeignKey [FK_PropertySets_PropertySetTypes]    Script Date: 10/25/2007 15:50:24 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PropertySets_PropertySetTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[SchemaPropertySets]'))
ALTER TABLE [dbo].[SchemaPropertySets]  WITH CHECK ADD  CONSTRAINT [FK_PropertySets_PropertySetTypes] FOREIGN KEY([PropertySetTypeId])
REFERENCES [dbo].[SchemaPropertySetTypes] ([PropertySetTypeId])
GO
ALTER TABLE [dbo].[SchemaPropertySets] CHECK CONSTRAINT [FK_PropertySets_PropertySetTypes]
GO
/****** Object:  ForeignKey [FK_PropertyTypes_PropertySets]    Script Date: 10/25/2007 15:50:25 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PropertyTypes_PropertySets]') AND parent_object_id = OBJECT_ID(N'[dbo].[SchemaPropertySetsPropertyTypes]'))
ALTER TABLE [dbo].[SchemaPropertySetsPropertyTypes]  WITH CHECK ADD  CONSTRAINT [FK_PropertyTypes_PropertySets] FOREIGN KEY([PropertySetId])
REFERENCES [dbo].[SchemaPropertySets] ([PropertySetId])
GO
ALTER TABLE [dbo].[SchemaPropertySetsPropertyTypes] CHECK CONSTRAINT [FK_PropertyTypes_PropertySets]
GO
/****** Object:  ForeignKey [FK_PropertyTypes_PropertySlots]    Script Date: 10/25/2007 15:50:25 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PropertyTypes_PropertySlots]') AND parent_object_id = OBJECT_ID(N'[dbo].[SchemaPropertySetsPropertyTypes]'))
ALTER TABLE [dbo].[SchemaPropertySetsPropertyTypes]  WITH CHECK ADD  CONSTRAINT [FK_PropertyTypes_PropertySlots] FOREIGN KEY([PropertyTypeId])
REFERENCES [dbo].[SchemaPropertyTypes] ([PropertyTypeId])
GO
ALTER TABLE [dbo].[SchemaPropertySetsPropertyTypes] CHECK CONSTRAINT [FK_PropertyTypes_PropertySlots]
GO
/****** Object:  ForeignKey [FK_PropertySlots_DataTypes]    Script Date: 10/25/2007 15:50:28 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PropertySlots_DataTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[SchemaPropertyTypes]'))
ALTER TABLE [dbo].[SchemaPropertyTypes]  WITH CHECK ADD  CONSTRAINT [FK_PropertySlots_DataTypes] FOREIGN KEY([DataTypeId])
REFERENCES [dbo].[SchemaDataTypes] ([DataTypeId])
GO
ALTER TABLE [dbo].[SchemaPropertyTypes] CHECK CONSTRAINT [FK_PropertySlots_DataTypes]
GO
/****** Object:  ForeignKey [FK_TextPropertiesNText_SchemaPropertyTypes]    Script Date: 10/25/2007 15:50:30 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_TextPropertiesNText_SchemaPropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[TextPropertiesNText]'))
ALTER TABLE [dbo].[TextPropertiesNText]  WITH CHECK ADD  CONSTRAINT [FK_TextPropertiesNText_SchemaPropertyTypes] FOREIGN KEY([PropertyTypeId])
REFERENCES [dbo].[SchemaPropertyTypes] ([PropertyTypeId])
GO
ALTER TABLE [dbo].[TextPropertiesNText] CHECK CONSTRAINT [FK_TextPropertiesNText_SchemaPropertyTypes]
GO
/****** Object:  ForeignKey [FK_TextPropertiesNText_Versions]    Script Date: 10/25/2007 15:50:30 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_TextPropertiesNText_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[TextPropertiesNText]'))
ALTER TABLE [dbo].[TextPropertiesNText]  WITH CHECK ADD  CONSTRAINT [FK_TextPropertiesNText_Versions] FOREIGN KEY([VersionId])
REFERENCES [dbo].[Versions] ([VersionId])
GO
ALTER TABLE [dbo].[TextPropertiesNText] CHECK CONSTRAINT [FK_TextPropertiesNText_Versions]
GO
/****** Object:  ForeignKey [FK_TextPropertiesNVarchar_SchemaPropertyTypes]    Script Date: 10/25/2007 15:50:32 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_TextPropertiesNVarchar_SchemaPropertyTypes]') AND parent_object_id = OBJECT_ID(N'[dbo].[TextPropertiesNVarchar]'))
ALTER TABLE [dbo].[TextPropertiesNVarchar]  WITH CHECK ADD  CONSTRAINT [FK_TextPropertiesNVarchar_SchemaPropertyTypes] FOREIGN KEY([PropertyTypeId])
REFERENCES [dbo].[SchemaPropertyTypes] ([PropertyTypeId])
GO
ALTER TABLE [dbo].[TextPropertiesNVarchar] CHECK CONSTRAINT [FK_TextPropertiesNVarchar_SchemaPropertyTypes]
GO
/****** Object:  ForeignKey [FK_TextPropertiesNVarchar_Versions]    Script Date: 10/25/2007 15:50:32 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_TextPropertiesNVarchar_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[TextPropertiesNVarchar]'))
ALTER TABLE [dbo].[TextPropertiesNVarchar]  WITH CHECK ADD  CONSTRAINT [FK_TextPropertiesNVarchar_Versions] FOREIGN KEY([VersionId])
REFERENCES [dbo].[Versions] ([VersionId])
GO
ALTER TABLE [dbo].[TextPropertiesNVarchar] CHECK CONSTRAINT [FK_TextPropertiesNVarchar_Versions]
GO
/****** Object:  ForeignKey [FK_VersionExtensions_SchemaPropertySets]    Script Date: 10/25/2007 15:50:33 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VersionExtensions_SchemaPropertySets]') AND parent_object_id = OBJECT_ID(N'[dbo].[VersionExtensions]'))
ALTER TABLE [dbo].[VersionExtensions]  WITH CHECK ADD  CONSTRAINT [FK_VersionExtensions_SchemaPropertySets] FOREIGN KEY([ExtensionTypeId])
REFERENCES [dbo].[SchemaPropertySets] ([PropertySetId])
GO
ALTER TABLE [dbo].[VersionExtensions] CHECK CONSTRAINT [FK_VersionExtensions_SchemaPropertySets]
GO
/****** Object:  ForeignKey [FK_VersionExtensions_Versions]    Script Date: 10/25/2007 15:50:33 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_VersionExtensions_Versions]') AND parent_object_id = OBJECT_ID(N'[dbo].[VersionExtensions]'))
ALTER TABLE [dbo].[VersionExtensions]  WITH CHECK ADD  CONSTRAINT [FK_VersionExtensions_Versions] FOREIGN KEY([VersionId])
REFERENCES [dbo].[Versions] ([VersionId])
GO
ALTER TABLE [dbo].[VersionExtensions] CHECK CONSTRAINT [FK_VersionExtensions_Versions]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes]    Script Date: 10/25/2007 15:50:36 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions]  WITH CHECK ADD  CONSTRAINT [FK_Versions_Nodes] FOREIGN KEY([NodeId])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Versions] CHECK CONSTRAINT [FK_Versions_Nodes]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes_CreatedBy]    Script Date: 10/25/2007 15:50:37 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes_CreatedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions]  WITH CHECK ADD  CONSTRAINT [FK_Versions_Nodes_CreatedBy] FOREIGN KEY([CreatedById])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Versions] CHECK CONSTRAINT [FK_Versions_Nodes_CreatedBy]
GO
/****** Object:  ForeignKey [FK_Versions_Nodes_ModifiedBy]    Script Date: 10/25/2007 15:50:37 ******/
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Versions_Nodes_ModifiedBy]') AND parent_object_id = OBJECT_ID(N'[dbo].[Versions]'))
ALTER TABLE [dbo].[Versions]  WITH CHECK ADD  CONSTRAINT [FK_Versions_Nodes_ModifiedBy] FOREIGN KEY([ModifiedById])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Versions] CHECK CONSTRAINT [FK_Versions_Nodes_ModifiedBy]
GO
--/****** Object:  FullTextIndex [PK_BinaryProperties]    Script Date: 10/25/2007 15:49:19 ******/
--/****** Object:  FullTextIndex     Script Date: 10/25/2007 15:49:19 ******/
--IF not EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[BinaryProperties]'))
--CREATE FULLTEXT INDEX ON [dbo].[BinaryProperties](
--[ContentType], 
--[Extension], 
--[FileNameWithoutExtension], 
--[Stream] TYPE COLUMN [Extension])
--KEY INDEX [PK_BinaryProperties] ON [SnCrFullText]
--WITH CHANGE_TRACKING AUTO
--GO
/****** Object:  FullTextIndex [PK_FlatProperties_1]    Script Date: 10/25/2007 15:50:10 ******/
/****** Object:  FullTextIndex     Script Date: 10/25/2007 15:50:10 ******/
--IF not EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[FlatProperties]'))
--CREATE FULLTEXT INDEX ON [dbo].[FlatProperties](
--[nvarchar_1], 
--[nvarchar_10], 
--[nvarchar_11], 
--[nvarchar_12], 
--[nvarchar_13], 
--[nvarchar_14], 
--[nvarchar_15], 
--[nvarchar_16], 
--[nvarchar_17], 
--[nvarchar_18], 
--[nvarchar_19], 
--[nvarchar_2], 
--[nvarchar_20], 
--[nvarchar_21], 
--[nvarchar_22], 
--[nvarchar_23], 
--[nvarchar_24], 
--[nvarchar_25], 
--[nvarchar_26], 
--[nvarchar_27], 
--[nvarchar_28], 
--[nvarchar_29], 
--[nvarchar_3], 
--[nvarchar_30], 
--[nvarchar_31], 
--[nvarchar_32], 
--[nvarchar_33], 
--[nvarchar_34], 
--[nvarchar_35], 
--[nvarchar_36], 
--[nvarchar_37], 
--[nvarchar_38], 
--[nvarchar_39], 
--[nvarchar_4], 
--[nvarchar_40], 
--[nvarchar_41], 
--[nvarchar_42], 
--[nvarchar_43], 
--[nvarchar_44], 
--[nvarchar_45], 
--[nvarchar_46], 
--[nvarchar_47], 
--[nvarchar_48], 
--[nvarchar_49], 
--[nvarchar_5], 
--[nvarchar_50], 
--[nvarchar_51], 
--[nvarchar_52], 
--[nvarchar_53], 
--[nvarchar_54], 
--[nvarchar_55], 
--[nvarchar_56], 
--[nvarchar_57], 
--[nvarchar_58], 
--[nvarchar_59], 
--[nvarchar_6], 
--[nvarchar_60], 
--[nvarchar_61], 
--[nvarchar_62], 
--[nvarchar_63], 
--[nvarchar_64], 
--[nvarchar_65], 
--[nvarchar_66], 
--[nvarchar_67], 
--[nvarchar_68], 
--[nvarchar_69], 
--[nvarchar_7], 
--[nvarchar_70], 
--[nvarchar_71], 
--[nvarchar_72], 
--[nvarchar_73], 
--[nvarchar_74], 
--[nvarchar_75], 
--[nvarchar_76], 
--[nvarchar_77], 
--[nvarchar_78], 
--[nvarchar_79], 
--[nvarchar_8], 
--[nvarchar_80], 
--[nvarchar_9])
--KEY INDEX [PK_FlatProperties_1] ON [SnCrFullText]
--WITH CHANGE_TRACKING AUTO
--GO
/****** Object:  FullTextIndex [PK_tblFpsNodes]    Script Date: 10/25/2007 15:50:17 ******/
/****** Object:  FullTextIndex     Script Date: 10/25/2007 15:50:17 ******/
--IF not EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[Nodes]'))
--CREATE FULLTEXT INDEX ON [dbo].[Nodes](
--[Name], 
--[Path])
--KEY INDEX [PK_tblFpsNodes] ON [SnCrFullText]
--WITH CHANGE_TRACKING AUTO
--GO
--/****** Object:  FullTextIndex [PK_TextPropertiesNText]    Script Date: 10/25/2007 15:50:30 ******/
--/****** Object:  FullTextIndex     Script Date: 10/25/2007 15:50:30 ******/
--IF not EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[TextPropertiesNText]'))
--CREATE FULLTEXT INDEX ON [dbo].[TextPropertiesNText](
--[Value])
--KEY INDEX [PK_TextPropertiesNText] ON [SnCrFullText]
--WITH CHANGE_TRACKING AUTO
--GO
--/****** Object:  FullTextIndex [PK_TextPropertiesNVarchar]    Script Date: 10/25/2007 15:50:32 ******/
--/****** Object:  FullTextIndex     Script Date: 10/25/2007 15:50:32 ******/
--IF not EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[TextPropertiesNVarchar]'))
--CREATE FULLTEXT INDEX ON [dbo].[TextPropertiesNVarchar](
--[Value])
--KEY INDEX [PK_TextPropertiesNVarchar] ON [SnCrFullText]
--WITH CHANGE_TRACKING AUTO
--GO


--- Constraints for the Security: Entries table

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityEntries_DefinedOnNodeId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityEntries]'))
ALTER TABLE [dbo].[SecurityEntries]  WITH CHECK ADD  CONSTRAINT [FK_SecurityEntries_DefinedOnNodeId] FOREIGN KEY([DefinedOnNodeId])
REFERENCES [dbo].[Nodes] ([NodeId])
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityEntries_DefinedOnNodeId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityEntries]'))
ALTER TABLE [dbo].[SecurityEntries] CHECK CONSTRAINT [FK_SecurityEntries_DefinedOnNodeId]
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityEntries_PrincipalId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityEntries]'))
ALTER TABLE [dbo].[SecurityEntries]  WITH CHECK ADD  CONSTRAINT [FK_SecurityEntries_PrincipalId] FOREIGN KEY([PrincipalId])
REFERENCES [dbo].[Nodes] ([NodeId])
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityEntries_PrincipalId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityEntries]'))
ALTER TABLE [dbo].[SecurityEntries] CHECK CONSTRAINT [FK_SecurityEntries_PrincipalId]
GO

--- Constraints for the Security: Custom Entries table

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityCustomEntries_DefinedOnNodeId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityCustomEntries]'))
ALTER TABLE [dbo].[SecurityCustomEntries]  WITH CHECK ADD  CONSTRAINT [FK_SecurityCustomEntries_DefinedOnNodeId] FOREIGN KEY([DefinedOnNodeId])
REFERENCES [dbo].[Nodes] ([NodeId])
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityCustomEntries_DefinedOnNodeId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityCustomEntries]'))
ALTER TABLE [dbo].[SecurityCustomEntries] CHECK CONSTRAINT [FK_SecurityCustomEntries_DefinedOnNodeId]
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityCustomEntries_PrincipalId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityCustomEntries]'))
ALTER TABLE [dbo].[SecurityCustomEntries]  WITH CHECK ADD  CONSTRAINT [FK_SecurityCustomEntries_PrincipalId] FOREIGN KEY([PrincipalId])
REFERENCES [dbo].[Nodes] ([NodeId])
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityCustomEntries_PrincipalId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityCustomEntries]'))
ALTER TABLE [dbo].[SecurityCustomEntries] CHECK CONSTRAINT [FK_SecurityCustomEntries_PrincipalId]
GO

--- Constraints for the Security: SecurityMemberships table

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SecurityMemberships]') AND name = N'IX_SecurityMemberships_UserId')
CREATE NONCLUSTERED INDEX [IX_SecurityMemberships_UserId] ON [dbo].[SecurityMemberships] 
(
	[UserId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityMemberships_Nodes_ContainerId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityMemberships]'))
ALTER TABLE [dbo].[SecurityMemberships]  WITH CHECK ADD  CONSTRAINT [FK_SecurityMemberships_Nodes_ContainerId] FOREIGN KEY([ContainerId])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[SecurityMemberships] CHECK CONSTRAINT [FK_SecurityMemberships_Nodes_ContainerId]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SecurityMemberships_Nodes_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityMemberships]'))
ALTER TABLE [dbo].[SecurityMemberships]  WITH CHECK ADD  CONSTRAINT [FK_SecurityMemberships_Nodes_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[SecurityMemberships] CHECK CONSTRAINT [FK_SecurityMemberships_Nodes_UserId]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_SecurityMemberships_ContainerType]') AND parent_object_id = OBJECT_ID(N'[dbo].[SecurityMemberships]'))
ALTER TABLE [dbo].[SecurityMemberships]  WITH CHECK ADD  CONSTRAINT [CK_SecurityMemberships_ContainerType] CHECK  (([ContainerType]='G' OR [ContainerType]='O'))
GO
ALTER TABLE [dbo].[SecurityMemberships] CHECK CONSTRAINT [CK_SecurityMemberships_ContainerType]
GO
IF NOT EXISTS (SELECT * FROM ::fn_listextendedproperty(N'MS_Description' , N'SCHEMA',N'dbo', N'TABLE',N'SecurityMemberships', N'CONSTRAINT',N'CK_SecurityMemberships_ContainerType'))
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'ContainerType can be ''G'' (Group) or ''O'' (OrganizationalUnit)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SecurityMemberships', @level2type=N'CONSTRAINT',@level2name=N'CK_SecurityMemberships_ContainerType'
GO
-- Constraints for the Nodes table (ContentList, Created/Modified by)

--This constraint is obsolete because we can move an item out of the content list,
--to the trash, and delete the list, leaving the item in the trash

--ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_SchemaPropertySets_ContentListTypeId] FOREIGN KEY([ContentListTypeId])
--REFERENCES [dbo].[SchemaPropertySets] ([PropertySetId])
--GO
--ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_SchemaPropertySets_ContentListTypeId]
--GO

--ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_Nodes_ContentListId] FOREIGN KEY([ContentListId])
--REFERENCES [dbo].[Nodes] ([NodeId])
--GO
--ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_Nodes_ContentListId]
--GO

ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_Nodes_CreatedById] FOREIGN KEY([CreatedById])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_Nodes_CreatedById]
GO

ALTER TABLE [dbo].[Nodes]  WITH CHECK ADD  CONSTRAINT [FK_Nodes_Nodes_ModifiedById] FOREIGN KEY([ModifiedById])
REFERENCES [dbo].[Nodes] ([NodeId])
GO
ALTER TABLE [dbo].[Nodes] CHECK CONSTRAINT [FK_Nodes_Nodes_ModifiedById]
GO

/****** Object:  Table [dbo].[JournalItems]    Script Date: 07/08/2009 07:22:07 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[JournalItems]') AND type in (N'U'))
DROP TABLE [dbo].[JournalItems]
GO
/****** Object:  Table [dbo].[JournalItems]    Script Date: 07/08/2009 07:22:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JournalItems](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[When] [datetime] NOT NULL,
	[Wherewith] [nvarchar](450) NOT NULL,
	[What] [nvarchar](100) NOT NULL,
	[Who] [nvarchar](200) NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
	[NodeId] [int] NOT NULL,
	[DisplayName] [nvarchar](450) NOT NULL,
	[NodeTypeName] [nvarchar](100) NOT NULL,
	[SourcePath] [nvarchar](450) NULL,
	[TargetPath] [nvarchar](450) NULL,
	[TargetDisplayName] [nvarchar](450) NULL,
	[Hidden] [bit] NOT NULL,
	[Details] [nvarchar](450) NULL	
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_JournalItems] ON [dbo].[JournalItems] 
(
	[When] DESC,
	[Wherewith] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[LogEntries]    Script Date: 10/09/2009 10:01:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogEntries]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[LogEntries](
	[LogId] [int] IDENTITY(1,1) NOT NULL,
	[EventId] [int] NOT NULL,
	[Priority] [int] NOT NULL,
	[Severity] [varchar](30) NOT NULL,
	[Title] [nvarchar](256) NULL,
	[ContentId] [int] NULL,
	[ContentPath] [nvarchar](450) NULL,
	[UserName] [nvarchar](450) NULL,
	[LogDate] [datetime] NOT NULL,
	[MachineName] [varchar](32) NULL,
	[AppDomainName] [varchar](512) NULL,
	[ProcessID] [varchar](256) NULL,
	[ProcessName] [varchar](512) NULL,
	[ThreadName] [varchar](512) NULL,
	[Win32ThreadId] [varchar](128) NULL,
	[Message] [nvarchar](1500) NULL,
	[FormattedMessage] [ntext] NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_Log] PRIMARY KEY CLUSTERED 
(
	[LogId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[LogCategoriesEntries]    Script Date: 10/09/2009 10:01:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogCategoriesEntries]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[LogCategoriesEntries](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CategoryName] [nvarchar](50) NULL,
	[LogId] [int] NOT NULL,
	[RowGuid] UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL DEFAULT NEWID(),
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_LogCategoriesEntries] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO

/****** Object:  Table [dbo].[IndexingActivity]    Script Date: 08/27/2010 07:54:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[IndexingActivity](
	[IndexingActivityId] [int] IDENTITY(1,1) NOT NULL,
	[ActivityType] [varchar](50) NULL,
	[CreationDate] [datetime] NOT NULL,
	[NodeId] [int] NOT NULL,
	[VersionId] [int] NOT NULL,
	[SingleVersion] [bit] NULL,
	[MoveOrRename] [bit] NULL,
	[IsLastDraftValue] [bit] NULL,
	[Path] [nvarchar](450) NULL,
	[VersionTimestamp] [bigint] NULL,
	[Hash] [varbinary](50) NULL
 CONSTRAINT [PK_IndexingActivity] PRIMARY KEY CLUSTERED 
(
	[IndexingActivityId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[IndexBackup]    Script Date: 10/27/2010 20:59:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[IndexBackup](
	[IndexBackupId] [int] IDENTITY(1,1) NOT NULL,
	[BackupNumber] [int] NOT NULL,
	[IsActive] [tinyint] NOT NULL,
	[BackupDate] [datetime] NOT NULL,
	[ComputerName] [nvarchar](100) NOT NULL,
	[AppDomain] [nvarchar](500) NOT NULL,
	[BackupFile] [varbinary](max) NULL,
	[RowGuid] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_IndexBackup] PRIMARY KEY CLUSTERED 
(
	[IndexBackupId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[IndexBackup] ADD  CONSTRAINT [DF_IndexBackup_RowGuid]  DEFAULT (newid()) FOR [RowGuid]
GO
/****** Object:  Table [dbo].[IndexBackup2]    Script Date: 10/27/2010 20:59:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[IndexBackup2](
	[IndexBackupId] [int] IDENTITY(1,1) NOT NULL,
	[BackupNumber] [int] NOT NULL,
	[IsActive] [tinyint] NOT NULL,
	[BackupDate] [datetime] NOT NULL,
	[ComputerName] [nvarchar](100) NOT NULL,
	[AppDomain] [nvarchar](500) NOT NULL,
	[BackupFile] [varbinary](max) NULL,
	[RowGuid] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_IndexBackup2] PRIMARY KEY CLUSTERED 
(
	[IndexBackupId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
ALTER TABLE [dbo].[IndexBackup2] ADD  CONSTRAINT [DF_IndexBackup2_RowGuid]  DEFAULT (newid()) FOR [RowGuid]
GO
/****************/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[WorkflowNotification](
	[NotificationId] [int] IDENTITY(1,1) NOT NULL,
	[NodeId] [int] NOT NULL,
	[WorkflowInstanceId] [uniqueidentifier] NOT NULL,
	[WorkflowNodePath] [nvarchar](450) NOT NULL,
	[BookmarkName] [varchar](50) NOT NULL,
 CONSTRAINT [PK_WorkflowNotification] PRIMARY KEY CLUSTERED 
(
	[NotificationId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 50) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[SchemaModification]    Script Date: 01/06/2011 12:08:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SchemaModification](
	[SchemaModificationId] [int] IDENTITY(1,1) NOT NULL,
	[ModificationDate] [datetime] NOT NULL,
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_SchemaModification] PRIMARY KEY CLUSTERED 
(
	[SchemaModificationId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Messaging.Subscriptions]    Script Date: 03/11/2011 05:09:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Messaging.Subscriptions](
	[SubscriptionId] [int] IDENTITY(1,1) NOT NULL,
	[UserEmail] [nvarchar](250) NOT NULL,
	[UserPath] [nvarchar](450) NOT NULL,
	[UserId] [int] NOT NULL,
	[UserName] [nvarchar](250) NOT NULL,
	[ContentPath] [nvarchar](450) NOT NULL,
	[FrequencyId] [int] NOT NULL,
	[Language] [nvarchar](5) NOT NULL,
	[Active] [tinyint] NOT NULL,
	[SitePath] [nvarchar](450) NULL,
	[SiteUrl] [nvarchar](200) NULL,
 CONSTRAINT [PK_Messaging.Subscriptions] PRIMARY KEY CLUSTERED 
(
	[SubscriptionId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Messaging.Events]    Script Date: 03/11/2011 05:13:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Messaging.Events](
	[EventId] [int] IDENTITY(1,1) NOT NULL,
	[ContentPath] [nvarchar](450) NOT NULL,
	[CreatorId] [int] NOT NULL,
	[NotificationTypeId] [int] NOT NULL,
	[When] [datetime] NOT NULL,
	[LastModifierId] [int] NOT NULL,
	[Who] [nvarchar](250) NOT NULL,
 CONSTRAINT [PK_Messaging.Events] PRIMARY KEY CLUSTERED 
(
	[EventId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Messaging.Messages]    Script Date: 03/11/2011 05:16:03 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Messaging.Messages](
	[MessageId] [int] IDENTITY(1,1) NOT NULL,
	[Address] [nvarchar](250) NOT NULL,
	[Subject] [text] NOT NULL,
	[Body] [text] NOT NULL,
	[LockId] [nvarchar](500) NULL,
	[LockedUntil] [datetime] NULL,
 CONSTRAINT [PK_Messaging.Messages] PRIMARY KEY CLUSTERED 
(
	[MessageId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Messaging.LastProcessTime]    Script Date: 03/11/2011 05:16:03 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Messaging.LastProcessTime](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Immediately] [datetime] NULL,
	[Daily] [datetime] NULL,
	[Weekly] [datetime] NULL,
	[Monthly] [datetime] NULL,
 CONSTRAINT [PK_Messaging.LastProcessTime] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Messaging.Synchronization]    Script Date: 03/23/2011 14:40:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Messaging.Synchronization](
	[LockName] [nvarchar](200) NOT NULL,
	[Locked] [bit] NOT NULL,
	[LockedUntil] [datetime] NOT NULL,
	[ComputerName] [nvarchar](200) NULL,
	[LockId] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_Messaging.Synchronization] PRIMARY KEY CLUSTERED 
(
	[LockName] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Packages]  ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Packages](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[PackageType] [varchar](50) NOT NULL,
	[PackageLevel] [varchar](50) NOT NULL,
	[Version] [varchar](50) NOT NULL,
	[AppId] [varchar](50) NULL,
	[Edition] [nvarchar](450) NULL,
	[AppVersion] [varchar](50) NULL,
	[ReleaseDate] [datetime] NOT NULL,
	[ExecutionDate] [datetime] NOT NULL,
	[ExecutionResult] [varchar](50) NOT NULL,
	[ExecutionError] [nvarchar](max) NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Packages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[StagingBinaryProperties]    Script Date: 01/12/2012 04:46:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[StagingBinaryProperties](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NULL,
	[PropertyTypeId] [int] NULL,
	[ContentType] [varchar](450) NOT NULL,
	[FileNameWithoutExtension] [varchar](450) NULL,
	[Extension] [varchar](50) NOT NULL,
	[Size] [bigint] NOT NULL,
	[Checksum] [varchar](200) NULL,
	[Stream] [varbinary](max) NULL,
	[CreationDate] [datetime] NOT NULL,
	[RowGuid] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[Timestamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_StagingBinaryProperties] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[RowGuid] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Default [DF_StagingBinaryProperties_CreationDate]    Script Date: 01/12/2012 04:46:21 ******/
ALTER TABLE [dbo].[StagingBinaryProperties] ADD  CONSTRAINT [DF_StagingBinaryProperties_CreationDate]  DEFAULT (getutcdate()) FOR [CreationDate]
GO
/****** Object:  Default [DF__StagingBi__RowGu__42F95F9C]    Script Date: 01/12/2012 04:46:21 ******/
ALTER TABLE [dbo].[StagingBinaryProperties] ADD  DEFAULT (newid()) FOR [RowGuid]
GO
/****** Object:  ForeignKey [FK_StagingBinaryProperties_SchemaPropertyTypes]    Script Date: 01/12/2012 04:46:21 ******/
ALTER TABLE [dbo].[StagingBinaryProperties]  WITH CHECK ADD  CONSTRAINT [FK_StagingBinaryProperties_SchemaPropertyTypes] FOREIGN KEY([PropertyTypeId])
REFERENCES [dbo].[SchemaPropertyTypes] ([PropertyTypeId])
GO
ALTER TABLE [dbo].[StagingBinaryProperties] CHECK CONSTRAINT [FK_StagingBinaryProperties_SchemaPropertyTypes]
GO
/****** Object:  ForeignKey [FK_StagingBinaryProperties_Versions]    Script Date: 01/12/2012 04:46:21 ******/
ALTER TABLE [dbo].[StagingBinaryProperties]  WITH CHECK ADD  CONSTRAINT [FK_StagingBinaryProperties_Versions] FOREIGN KEY([VersionId])
REFERENCES [dbo].[Versions] ([VersionId])
GO
ALTER TABLE [dbo].[StagingBinaryProperties] CHECK CONSTRAINT [FK_StagingBinaryProperties_Versions]
GO




/****** Object: Index [ix_verionid] Script Date: 05/03/2011 15:21:41 ******/
CREATE NONCLUSTERED INDEX [ix_version_id] ON [dbo].[BinaryProperties] 
(
[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/****** Object: Index [ix_version_id] Script Date: 05/03/2011 15:22:03 ******/
CREATE NONCLUSTERED INDEX [ix_version_id] ON [dbo].[FlatProperties] 
(
[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/****** Object: Index [ix_versionid] Script Date: 05/03/2011 15:22:32 ******/
CREATE NONCLUSTERED INDEX [ix_version_id] ON [dbo].[ReferenceProperties] 
(
[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/****** Object: Index [ix_versionid] Script Date: 05/03/2011 15:23:06 ******/
CREATE NONCLUSTERED INDEX [ix_version_id] ON [dbo].[TextPropertiesNText] 
(
[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/****** Object: Index [ix_version] Script Date: 05/03/2011 15:23:25 ******/
CREATE NONCLUSTERED INDEX [ix_version_id] ON [dbo].[TextPropertiesNVarchar] 
(
[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

--/****** Object: Index [ix_nodeid] Script Date: 05/03/2011 15:23:45 ******/
--CREATE NONCLUSTERED INDEX [ix_nodeid] ON [dbo].[Versions] 
--(
--[NodeId] ASC,
--[MajorNumber] DESC,
--[ModificationDate] DESC,
--[MinorNumber] DESC,
--[Status] ASC
--)
--INCLUDE ( [VersionId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
--GO

CREATE NONCLUSTERED INDEX [ix_Versions_NodeId]
ON [dbo].[Versions] ([NodeId])
GO

CREATE NONCLUSTERED INDEX [ix_Versions_NodeId_MinorNumber_MajorNumber_Status]
ON [dbo].[Versions] ([NodeId],[MinorNumber],[Status])
GO


CREATE NONCLUSTERED INDEX [ix_name] ON [dbo].[SchemaPropertySets] 
(
	[Name] ASC
)
INCLUDE([PropertySetId])
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 50) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [ix_name] ON [dbo].[SchemaPropertyTypes] 
(
	[Name] ASC
)
INCLUDE([PropertyTypeId])
WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 50) ON [PRIMARY]
GO

--============================== Switch off the foreign keys ==============================

ALTER TABLE [BinaryProperties] NOCHECK CONSTRAINT ALL
ALTER TABLE [FlatProperties] NOCHECK CONSTRAINT ALL
ALTER TABLE [Nodes] NOCHECK CONSTRAINT ALL
ALTER TABLE [ReferenceProperties] NOCHECK CONSTRAINT ALL
ALTER TABLE [SecurityEntries] NOCHECK CONSTRAINT ALL
ALTER TABLE [SecurityCustomEntries] NOCHECK CONSTRAINT ALL
ALTER TABLE [SecurityMemberships] NOCHECK CONSTRAINT ALL
ALTER TABLE [TextPropertiesNText] NOCHECK CONSTRAINT ALL
ALTER TABLE [TextPropertiesNVarchar] NOCHECK CONSTRAINT ALL
ALTER TABLE [Versions] NOCHECK CONSTRAINT ALL
ALTER TABLE [StagingBinaryProperties] NOCHECK CONSTRAINT ALL
ALTER TABLE [VersionExtensions] NOCHECK CONSTRAINT ALL

--=========================================================================================
