SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.USP_ListDeliveryPoint
    @pageIndex INT = 1, @pageSize INT = 10, @totalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @pageIndex < 1 OR @pageSize < 1 OR @pageSize > 100
        THROW 50101, N'Paginación inválida.', 1;
    SELECT @totalCount = COUNT(*) FROM dbo.DeliveryPoint;
    SELECT Id, Name, Street, Number, Neighborhood, ZipCode, City, [References], BusinessHours, Latitude, Longitude, IsActive, MunicipalityId FROM dbo.DeliveryPoint
    ORDER BY Id DESC OFFSET (CONVERT(BIGINT, @pageIndex) - 1) * @pageSize ROWS
    FETCH NEXT @pageSize ROWS ONLY;
END;
GO
CREATE OR ALTER PROCEDURE dbo.USP_GetDeliveryPoint @id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Street, Number, Neighborhood, ZipCode, City, [References], BusinessHours, Latitude, Longitude, IsActive, MunicipalityId FROM dbo.DeliveryPoint WHERE Id = @id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.USP_CreateDeliveryPoint
    @name NVARCHAR(MAX),
    @street NVARCHAR(MAX),
    @number NVARCHAR(MAX),
    @neighborhood NVARCHAR(MAX),
    @zipCode NVARCHAR(MAX),
    @city NVARCHAR(MAX),
    @businessHours NVARCHAR(MAX),
    @latitude DECIMAL(18,8), @longitude DECIMAL(18,8),
    @references NVARCHAR(MAX) = NULL, @municipalityId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @name = LTRIM(RTRIM(@name));
    IF @name IS NULL OR LEN(@name) = 0 OR DATALENGTH(@name) / 2 > 150
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @street = LTRIM(RTRIM(@street));
    IF @street IS NULL OR LEN(@street) = 0 OR DATALENGTH(@street) / 2 > 150
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @number = LTRIM(RTRIM(@number));
    IF @number IS NULL OR LEN(@number) = 0 OR DATALENGTH(@number) / 2 > 50
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @neighborhood = LTRIM(RTRIM(@neighborhood));
    IF @neighborhood IS NULL OR LEN(@neighborhood) = 0 OR DATALENGTH(@neighborhood) / 2 > 100
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @zipCode = LTRIM(RTRIM(@zipCode));
    IF @zipCode IS NULL OR LEN(@zipCode) = 0 OR DATALENGTH(@zipCode) / 2 > 20
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @city = LTRIM(RTRIM(@city));
    IF @city IS NULL OR LEN(@city) = 0 OR DATALENGTH(@city) / 2 > 100
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @businessHours = LTRIM(RTRIM(@businessHours));
    IF @businessHours IS NULL OR LEN(@businessHours) = 0 OR DATALENGTH(@businessHours) / 2 > 200
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    IF @latitude IS NULL OR @longitude IS NULL OR @latitude NOT BETWEEN -90 AND 90
        OR @longitude NOT BETWEEN -180 AND 180 OR DATALENGTH(@references) / 2 > 400
        THROW 50101, N'Coordenadas o referencias inválidas.', 1;
    DECLARE @resolvedMunicipalityId INT;
    SELECT @resolvedMunicipalityId = Id, @city = Name FROM dbo.Municipality
    WHERE Name = @city COLLATE Latin1_General_100_CI_AI
        AND (@municipalityId IS NULL OR Id = @municipalityId);
    IF @resolvedMunicipalityId IS NULL
        THROW 50105, N'El municipio no existe o no corresponde a la ciudad.', 1;
    INSERT dbo.DeliveryPoint (Name, Street, Number, Neighborhood, ZipCode, City, BusinessHours, Latitude, Longitude, [References], MunicipalityId)
    VALUES (@name, @street, @number, @neighborhood, @zipCode, @city, @businessHours, @latitude, @longitude, @references, @resolvedMunicipalityId);
    SELECT CONVERT(INT, SCOPE_IDENTITY());
END;
GO
CREATE OR ALTER PROCEDURE dbo.USP_UpdateDeliveryPoint
    @id INT,
    @name NVARCHAR(MAX),
    @street NVARCHAR(MAX),
    @number NVARCHAR(MAX),
    @neighborhood NVARCHAR(MAX),
    @zipCode NVARCHAR(MAX),
    @city NVARCHAR(MAX),
    @businessHours NVARCHAR(MAX),
    @latitude DECIMAL(18,8), @longitude DECIMAL(18,8),
    @references NVARCHAR(MAX) = NULL, @municipalityId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @name = LTRIM(RTRIM(@name));
    IF @name IS NULL OR LEN(@name) = 0 OR DATALENGTH(@name) / 2 > 150
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @street = LTRIM(RTRIM(@street));
    IF @street IS NULL OR LEN(@street) = 0 OR DATALENGTH(@street) / 2 > 150
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @number = LTRIM(RTRIM(@number));
    IF @number IS NULL OR LEN(@number) = 0 OR DATALENGTH(@number) / 2 > 50
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @neighborhood = LTRIM(RTRIM(@neighborhood));
    IF @neighborhood IS NULL OR LEN(@neighborhood) = 0 OR DATALENGTH(@neighborhood) / 2 > 100
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @zipCode = LTRIM(RTRIM(@zipCode));
    IF @zipCode IS NULL OR LEN(@zipCode) = 0 OR DATALENGTH(@zipCode) / 2 > 20
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @city = LTRIM(RTRIM(@city));
    IF @city IS NULL OR LEN(@city) = 0 OR DATALENGTH(@city) / 2 > 100
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    SET @businessHours = LTRIM(RTRIM(@businessHours));
    IF @businessHours IS NULL OR LEN(@businessHours) = 0 OR DATALENGTH(@businessHours) / 2 > 200
        THROW 50101, N'Campo obligatorio o longitud inválida.', 1;
    IF @latitude IS NULL OR @longitude IS NULL OR @latitude NOT BETWEEN -90 AND 90
        OR @longitude NOT BETWEEN -180 AND 180 OR DATALENGTH(@references) / 2 > 400
        THROW 50101, N'Coordenadas o referencias inválidas.', 1;
    DECLARE @resolvedMunicipalityId INT;
    SELECT @resolvedMunicipalityId = Id, @city = Name FROM dbo.Municipality
    WHERE Name = @city COLLATE Latin1_General_100_CI_AI
        AND (@municipalityId IS NULL OR Id = @municipalityId);
    IF @resolvedMunicipalityId IS NULL
        THROW 50105, N'El municipio no existe o no corresponde a la ciudad.', 1;
    UPDATE dbo.DeliveryPoint SET
        Name = @name,
        Street = @street,
        Number = @number,
        Neighborhood = @neighborhood,
        ZipCode = @zipCode,
        City = @city,
        BusinessHours = @businessHours,
        Latitude = @latitude, Longitude = @longitude, [References] = @references,
        MunicipalityId = @resolvedMunicipalityId
    WHERE Id = @id;
    IF @@ROWCOUNT = 0 THROW 50104, N'No se encontró el punto de entrega.', 1;
    SELECT @id;
END;
GO
