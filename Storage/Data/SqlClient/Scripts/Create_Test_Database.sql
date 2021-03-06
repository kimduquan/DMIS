USE [master]
GO

/****** DROP DATABASE: [ContentRepository_test] ******/
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'ContentRepository_test')
BEGIN
	/****** Restricts access to this database to only one user at a time  ******/
	ALTER DATABASE [ContentRepository_test] SET SINGLE_USER WITH ROLLBACK IMMEDIATE 
	ALTER DATABASE [ContentRepository_test] SET MULTI_USER WITH ROLLBACK IMMEDIATE
	DROP DATABASE [ContentRepository_test]
END 
go
/****** CREATE DATABASE: [ContentRepository_test] ******/
CREATE DATABASE [ContentRepository_test] ON  PRIMARY 
( NAME = N'ContentRepository_test', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL.1\MSSQL\DATA\ContentRepository_test.mdf' , SIZE = 3072KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'ContentRepository_test_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL.1\MSSQL\DATA\ContentRepository_test_log.ldf' , SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
EXEC dbo.sp_dbcmptlevel @dbname=N'ContentRepository_test', @new_cmptlevel=90
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [ContentRepository_test].[dbo].[sp_fulltext_database] @action = 'disable'
end
GO
EXEC sp_fulltext_database enable
GO
ALTER DATABASE [ContentRepository_test] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [ContentRepository_test] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [ContentRepository_test] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [ContentRepository_test] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [ContentRepository_test] SET ARITHABORT OFF 
GO
ALTER DATABASE [ContentRepository_test] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [ContentRepository_test] SET AUTO_CREATE_STATISTICS ON 
GO
ALTER DATABASE [ContentRepository_test] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [ContentRepository_test] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [ContentRepository_test] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [ContentRepository_test] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [ContentRepository_test] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [ContentRepository_test] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [ContentRepository_test] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [ContentRepository_test] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [ContentRepository_test] SET  ENABLE_BROKER 
GO
ALTER DATABASE [ContentRepository_test] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [ContentRepository_test] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [ContentRepository_test] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [ContentRepository_test] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [ContentRepository_test] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [ContentRepository_test] SET  READ_WRITE 
GO
ALTER DATABASE [ContentRepository_test] SET RECOVERY FULL 
GO
ALTER DATABASE [ContentRepository_test] SET  MULTI_USER 
GO
ALTER DATABASE [ContentRepository_test] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [ContentRepository_test] SET DB_CHAINING OFF
