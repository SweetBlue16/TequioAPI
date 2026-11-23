-- Incremental installation on the configured TEQUIO database; never rerun 01 on an existing database.
-- Required session settings for the persisted computed unique index.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.Municipality', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Municipality (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Municipality PRIMARY KEY,
        Name NVARCHAR(100) COLLATE Latin1_General_100_CI_AI NOT NULL,
        CONSTRAINT UQ_Municipality_Name UNIQUE (Name),
        CONSTRAINT UQ_Municipality_IdName UNIQUE (Id, Name)
    );
END;
INSERT dbo.Municipality (Name)
SELECT name FROM (VALUES
    (N'Xalapa'), (N'Coatepec'), (N'Xico'), (N'Emiliano Zapata'), (N'Banderilla'),
    (N'Tlalnelhuayocan'), (N'Teocelo'), (N'Rafael Lucio'), (N'Naolinco')
) AS catalog(name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Municipality m WHERE m.Name = catalog.name);
-- Stop rather than invent municipalities or silently repair existing team data.
IF EXISTS (SELECT 1 FROM dbo.DeliveryPoint d WHERE NOT EXISTS
    (SELECT 1 FROM dbo.Municipality m WHERE m.Name = d.City COLLATE Latin1_General_100_CI_AI))
    THROW 50105, N'Hay municipios existentes sin correspondencia. Revisar antes de instalar.', 1;
DECLARE @deliveryPointObjectId INT = OBJECT_ID(N'dbo.DeliveryPoint');
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = @deliveryPointObjectId AND name = N'MunicipalityId'
)
BEGIN
    ALTER TABLE dbo.DeliveryPoint ADD MunicipalityId INT NULL;
END;
DECLARE @updateMunicipality NVARCHAR(MAX) =
    N'UPDATE point SET MunicipalityId = municipality.Id '
    + N'FROM dbo.DeliveryPoint AS point '
    + N'JOIN dbo.Municipality AS municipality ON municipality.Name = point.City COLLATE Latin1_General_100_CI_AI '
    + N'WHERE point.MunicipalityId IS NULL;';
EXEC sys.sp_executesql @updateMunicipality;

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = @deliveryPointObjectId AND name = N'MunicipalityId' AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.DeliveryPoint ALTER COLUMN MunicipalityId INT NOT NULL;
END;
-- City is retained for the current frontend contract and tied to the same municipality.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = @deliveryPointObjectId
    AND name = 'City' AND collation_name <> 'Latin1_General_100_CI_AI')
    ALTER TABLE dbo.DeliveryPoint ALTER COLUMN City NVARCHAR(100) COLLATE Latin1_General_100_CI_AI NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DeliveryPoint_Municipality')
BEGIN
    ALTER TABLE dbo.DeliveryPoint WITH CHECK ADD CONSTRAINT FK_DeliveryPoint_Municipality
        FOREIGN KEY (MunicipalityId, City) REFERENCES dbo.Municipality (Id, Name);
END;
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = @deliveryPointObjectId AND name = N'NormalizedName'
)
BEGIN
    ALTER TABLE dbo.DeliveryPoint ADD NormalizedName AS
        (UPPER(LTRIM(RTRIM(Name))) COLLATE Latin1_General_100_CI_AI) PERSISTED;
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = @deliveryPointObjectId
    AND name = 'UX_DeliveryPoint_MunicipalityName')
BEGIN
    CREATE UNIQUE INDEX UX_DeliveryPoint_MunicipalityName
        ON dbo.DeliveryPoint(MunicipalityId, NormalizedName);
END;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DeliveryPoint_Valid')
    ALTER TABLE dbo.DeliveryPoint WITH CHECK ADD CONSTRAINT CK_DeliveryPoint_Valid CHECK (
        Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180
        AND LEN(LTRIM(RTRIM(Name))) > 0 AND LEN(LTRIM(RTRIM(Street))) > 0
        AND LEN(LTRIM(RTRIM(Number))) > 0 AND LEN(LTRIM(RTRIM(Neighborhood))) > 0
        AND LEN(LTRIM(RTRIM(ZipCode))) > 0 AND LEN(LTRIM(RTRIM(BusinessHours))) > 0
        AND ([References] IS NULL OR DATALENGTH([References]) <= 800));
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Batch')
    AND name = 'IX_Batch_DeliveryPointId')
    CREATE INDEX IX_Batch_DeliveryPointId ON dbo.Batch(DeliveryPointId);
COMMIT;
GO
