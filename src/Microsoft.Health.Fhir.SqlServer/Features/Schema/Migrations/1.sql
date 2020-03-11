-- NOTE: This script DROPS AND RECREATES all database objects.
-- Style guide: please see: https://github.com/ktaranov/sqlserver-kit/blob/master/SQL%20Server%20Name%20Convention%20and%20T-SQL%20Programming%20Style.md


/*************************************************************
    Drop existing objects
**************************************************************/

DECLARE @sql nvarchar(max) =''

SELECT @sql = @sql + 'DROP PROCEDURE '+ s.name + '.' + p.name +';'
FROM sys.procedures p
join sys.schemas s
on p.schema_id = s.schema_id

SELECT @sql = @sql + 'DROP TABLE '+ s.name + '.' + t.name +';'
from sys.tables t
join sys.schemas s
on t.schema_id = s.schema_id

SELECT @sql = @sql + 'DROP TYPE '+ s.name + '.' + tt.name +';'
FROM sys.table_types tt
join sys.schemas s
on tt.schema_id = s.schema_id

SELECT @sql = @sql + 'DROP SEQUENCE '+ s.name + '.' + sq.name +';'
FROM sys.sequences sq
join sys.schemas s
on sq.schema_id = s.schema_id

EXEC(@sql)

GO

DROP SCHEMA IF EXISTS dicom

GO

/*************************************************************
    Configure database
**************************************************************/

-- Enable RCSI
IF ((SELECT is_read_committed_snapshot_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON
END

-- Avoid blocking queries when statistics need to be rebuilt
IF ((SELECT is_auto_update_stats_async_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET AUTO_UPDATE_STATISTICS_ASYNC ON
END

-- Use ANSI behavior for null values
IF ((SELECT is_ansi_nulls_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET ANSI_NULLS ON
END

GO

/*************************************************************
    Schema bootstrap
**************************************************************/
CREATE SCHEMA dicom
GO

CREATE TABLE dicom.SchemaVersion
(
    Version int PRIMARY KEY,
    Status varchar(10)
)

INSERT INTO dicom.SchemaVersion
VALUES
    (1, 'started')

GO

--
--  STORED PROCEDURE
--      SelectCurrentSchemaVersion
--
--  DESCRIPTION
--      Selects the current completed schema version
--
--  RETURNS
--      The current version as a result set
--
CREATE PROCEDURE dicom.SelectCurrentSchemaVersion
AS
BEGIN
    SET NOCOUNT ON

    SELECT MAX(Version)
    FROM SchemaVersion
    WHERE Status = 'complete'
END
GO

--
--  STORED PROCEDURE
--      UpsertSchemaVersion
--
--  DESCRIPTION
--      Creates or updates a new schema version entry
--
--  PARAMETERS
--      @version
--          * The version number
--      @status
--          * The status of the version
--
CREATE PROCEDURE dicom.UpsertSchemaVersion
    @version int,
    @status varchar(10)
AS
    SET NOCOUNT ON

    IF EXISTS(SELECT *
        FROM dicom.SchemaVersion
        WHERE Version = @version)
    BEGIN
        UPDATE dicom.SchemaVersion
        SET Status = @status
        WHERE Version = @version
    END
    ELSE
    BEGIN
        INSERT INTO dicom.SchemaVersion
            (Version, Status)
        VALUES
            (@version, @status)
    END
GO
/*************************************************************
    Instance Table
**************************************************************/
--Mapping table for dicom retrieval
CREATE TABLE dicom.Instance (
	--instance keys
	StudyInstanceUid NVARCHAR(64) NOT NULL,
	SeriesInstanceUid NVARCHAR(64) NOT NULL,
	SopInstanceUid NVARCHAR(64) NOT NULL,
	--data consitency columns
	Watermark BIGINT NOT NULL,
	Status TINYINT NOT NULL,
    LastStatusUpdatesDate DATETIME2(7) NOT NULL,
    --audit columns
	CreatedDate DATETIME2(7) NOT NULL
)

/*************************************************************
    Study Table
**************************************************************/
--Table containing normalized standard Study tags
CREATE TABLE dicom.StudyMetadataCore (
	--Key
	ID BIGINT NOT NULL, --PK
	--instance keys
	StudyInstanceUid NVARCHAR(64) NOT NULL,
    Version INT NOT NULL,
	--patient and study core
	PatientID NVARCHAR(64) NOT NULL,
	PatientName NVARCHAR(320) NULL,
	--PatientNameIndex AS REPLACE(PatientName, '^', ' '), --FT index, TODO code gen not working 
	ReferringPhysicianName NVARCHAR(320) NULL,
	StudyDate DATE NULL,
	StudyDescription NVARCHAR(64) NULL,
	AccessionNumber NVARCHAR(16) NULL,
)

/*************************************************************
    Series Table
**************************************************************/
--Table containing normalized standard Series tags
CREATE TABLE dicom.SeriesMetadataCore (
	--Key
	ID BIGINT NOT NULL, --FK
	--instance keys
	SeriesInstanceUid NVARCHAR(64) NOT NULL,
    Version INT NOT NULL,
	--series core
	Modality NVARCHAR(16) NULL,
	PerformedProcedureStepStartDate DATE NULL
) 
GO
