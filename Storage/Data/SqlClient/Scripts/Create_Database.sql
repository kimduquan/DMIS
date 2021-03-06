USE [master]
GO

/****** DROP DATABASE: [ContentRepository] ******/
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'ContentRepository')
BEGIN
	/****** Restricts access to this database to only one user at a time  ******/
	ALTER DATABASE [ContentRepository] SET SINGLE_USER WITH ROLLBACK IMMEDIATE 
	
	USE [master]
	DROP DATABASE [ContentRepository]
END 

/****** CREATE DATABASE: [ContentRepository] ******/
CREATE DATABASE [ContentRepository]
GO
EXEC dbo.sp_dbcmptlevel @dbname=N'ContentRepository' --, @new_cmptlevel=100
GO
--IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
--begin
--EXEC [ContentRepository].[dbo].[sp_fulltext_database] @action = 'disable'
--end
--GO
--EXEC sp_fulltext_database enable
--GO
ALTER DATABASE [ContentRepository] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [ContentRepository] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [ContentRepository] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [ContentRepository] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [ContentRepository] SET ARITHABORT OFF 
GO
ALTER DATABASE [ContentRepository] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [ContentRepository] SET AUTO_CREATE_STATISTICS ON 
GO
ALTER DATABASE [ContentRepository] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [ContentRepository] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [ContentRepository] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [ContentRepository] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [ContentRepository] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [ContentRepository] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [ContentRepository] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [ContentRepository] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [ContentRepository] SET  ENABLE_BROKER 
GO
ALTER DATABASE [ContentRepository] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [ContentRepository] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [ContentRepository] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [ContentRepository] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [ContentRepository] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [ContentRepository] SET  READ_WRITE 
GO
ALTER DATABASE [ContentRepository] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [ContentRepository] SET  MULTI_USER 
GO
ALTER DATABASE [ContentRepository] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [ContentRepository] SET DB_CHAINING OFF
GO