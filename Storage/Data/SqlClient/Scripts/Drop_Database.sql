USE [master]
GO
/****** DROP DATABASE: [ContentRepository] ******/
IF  EXISTS (SELECT name FROM sys.databases WHERE name = N'ContentRepository')
DROP DATABASE [ContentRepository]
GO