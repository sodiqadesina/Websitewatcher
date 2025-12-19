/* =========================================================
   WebsiteWatcher - Fresh setup for a new developer
   SQL Server / LocalDB compatible
   ========================================================= */

-- (Optional) Create DB if it doesn't exist
IF DB_ID(N'WebsiteWatcher') IS NULL
BEGIN
    CREATE DATABASE WebsiteWatcher;
END
GO

USE WebsiteWatcher;
GO

/* 1) Enable Change Tracking at the database level */
ALTER DATABASE WebsiteWatcher
SET CHANGE_TRACKING = ON
(CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);
GO

/* 2) Create dbo.Websites */
IF OBJECT_ID(N'dbo.Websites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Websites (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Url] NVARCHAR(MAX) NOT NULL,
        [XPathExpression] NVARCHAR(MAX) NULL,
        [Timestamp] DATETIME2 NOT NULL CONSTRAINT DF_Websites_Timestamp DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Websites PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

/* 3) Enable Change Tracking on dbo.Websites */
ALTER TABLE dbo.Websites
ENABLE CHANGE_TRACKING
WITH (TRACK_COLUMNS_UPDATED = OFF);
GO

/* 4) Create dbo.Snapshots (composite PK so multiple snapshots per website) */
IF OBJECT_ID(N'dbo.Snapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Snapshots (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [Timestamp] DATETIME2 NOT NULL CONSTRAINT DF_Snapshots_Timestamp DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Snapshots PRIMARY KEY CLUSTERED ([Id] ASC, [Timestamp] ASC)
    );
END
GO
