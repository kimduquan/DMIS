USE [master]
GO
/****** DROP DATABASE: [ContentRepository_test] ******/
IF  EXISTS (SELECT name FROM sys.databases WHERE name = N'ContentRepository_test')
DROP DATABASE [ContentRepository_test]
GO