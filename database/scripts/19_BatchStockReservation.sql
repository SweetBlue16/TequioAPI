SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- Adds idempotent stock reservations without treating pending orders as paid.
IF COL_LENGTH(N'dbo.[Order]', N'RequestId') IS NULL
BEGIN
    ALTER TABLE dbo.[Order]
        ADD RequestId UNIQUEIDENTIFIER NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.[Order]') AND name = N'UX_Order_BuyerRequest'
)
BEGIN
    CREATE UNIQUE INDEX UX_Order_BuyerRequest
        ON dbo.[Order] (BuyerId, RequestId)
        WHERE RequestId IS NOT NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PublishBatch
    @producerId INT,
    @productId INT,
    @deliveryPointId INT,
    @name NVARCHAR(150),
    @description NVARCHAR(MAX),
    @minimumGoal INT,
    @deadline DATETIME,
    @packages NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @packages IS NULL OR ISJSON(@packages, ARRAY) <> 1
        OR COALESCE(TRIM(@name), N'') = N'' OR COALESCE(TRIM(@description), N'') = N''
        OR @minimumGoal IS NULL OR @minimumGoal < 1 OR @deadline IS NULL OR @deadline <= GETUTCDATE()
    BEGIN
        THROW 50201, N'Completa los datos, la meta y la fecha del lote.', 1;
    END;

    DECLARE @items TABLE (Name NVARCHAR(150), Content NVARCHAR(MAX), Price DECIMAL(18, 2), Quantity INT);
    INSERT INTO @items (Name, Content, Price, Quantity)
    SELECT TRIM(Name), TRIM(Content), TRY_CONVERT(DECIMAL(18, 2), Price), TRY_CONVERT(INT, Quantity)
    FROM OPENJSON(@packages)
    WITH (
        Name NVARCHAR(150) '$.name', Content NVARCHAR(MAX) '$.content',
        Price NVARCHAR(50) '$.price', Quantity NVARCHAR(50) '$.quantity'
    );

    IF NOT EXISTS (SELECT 1 FROM @items)
        OR EXISTS (
            SELECT 1
            FROM @items
            WHERE COALESCE(Name, N'') = N'' OR COALESCE(Content, N'') = N''
                OR Price IS NULL OR Price <= 0 OR Quantity IS NULL OR Quantity <= 0
        )
        OR (SELECT SUM(CONVERT(BIGINT, Quantity)) FROM @items) < @minimumGoal
    BEGIN
        THROW 50201, N'Agrega paquetes cerrados válidos y comprueba el inventario de la meta.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = @producerId AND RoleId = 2 AND IsVerified = 1
        )
        BEGIN
            THROW 50202, N'Necesitas una cuenta de productor verificada.', 1;
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.BaseProduct WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = @productId AND ProducerId = @producerId AND IsActive = 1
        )
        BEGIN
            THROW 50202, N'El producto debe estar activo y pertenecer a tu cuenta.', 1;
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.DeliveryPoint WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = @deliveryPointId AND IsActive = 1
        )
        BEGIN
            THROW 50203, N'Selecciona un punto de entrega activo.', 1;
        END;

        INSERT INTO dbo.Batch (
            BaseProductId, ProducerId, DeliveryPointId, BatchName, Description, MinimumGoal, Deadline, Status
        )
        VALUES (
            @productId, @producerId, @deliveryPointId, TRIM(@name), TRIM(@description), @minimumGoal,
            @deadline, N'Active'
        );

        DECLARE @batchId INT = SCOPE_IDENTITY();
        INSERT INTO dbo.Package (BatchId, PackageName, Content, Price, AvailableQuantity)
        SELECT @batchId, Name, Content, Price, Quantity
        FROM @items;

        COMMIT;
        SELECT @batchId AS BatchId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK;
        END;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ReserveBatchStock
    @buyerId INT,
    @batchId INT,
    @requestId UNIQUEIDENTIFIER,
    @packages NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @requestId IS NULL OR @batchId IS NULL OR @batchId < 1
        OR @packages IS NULL OR ISJSON(@packages, ARRAY) <> 1
    BEGIN
        THROW 50204, N'Selecciona los paquetes que quieres apartar.', 1;
    END;

    DECLARE @requested TABLE (PackageId INT, Quantity INT);
    INSERT INTO @requested (PackageId, Quantity)
    SELECT TRY_CONVERT(INT, PackageId), TRY_CONVERT(INT, Quantity)
    FROM OPENJSON(@packages)
    WITH (PackageId NVARCHAR(32) '$.packageId', Quantity NVARCHAR(32) '$.quantity');

    IF NOT EXISTS (SELECT 1 FROM @requested)
        OR EXISTS (
            SELECT 1
            FROM @requested
            WHERE PackageId IS NULL OR PackageId <= 0 OR Quantity IS NULL OR Quantity <= 0
        )
        OR EXISTS (SELECT 1 FROM @requested GROUP BY PackageId HAVING COUNT(*) > 1)
    BEGIN
        THROW 50204, N'Las cantidades y los paquetes seleccionados deben ser válidos y únicos.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Lock the buyer first so the same request cannot create competing reservations.
        IF NOT EXISTS (
            SELECT 1
            FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = @buyerId AND RoleId = 1 AND IsVerified = 1
        )
        BEGIN
            THROW 50205, N'Necesitas una cuenta de comprador verificada.', 1;
        END;

        DECLARE @orderId INT, @existingBatchId INT;
        SELECT @orderId = Id, @existingBatchId = BatchId
        FROM dbo.[Order]
        WHERE BuyerId = @buyerId AND RequestId = @requestId;

        IF @orderId IS NOT NULL
        BEGIN
            IF @existingBatchId <> @batchId OR EXISTS (
                SELECT PackageId, Quantity FROM @requested
                EXCEPT
                SELECT PackageId, Quantity FROM dbo.OrderDetail WHERE OrderId = @orderId
            ) OR EXISTS (
                SELECT PackageId, Quantity FROM dbo.OrderDetail WHERE OrderId = @orderId
                EXCEPT
                SELECT PackageId, Quantity FROM @requested
            )
            BEGIN
                THROW 50206, N'La referencia ya se usó con otro apartado.', 1;
            END;

            COMMIT;
            SELECT @orderId AS OrderId;
            RETURN;
        END;

        -- Serialize stock accounting, including reservations for overlapping packages.
        IF NOT EXISTS (
            SELECT 1
            FROM dbo.Batch WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = @batchId AND Status = N'Active' AND Deadline > GETUTCDATE()
        )
        BEGIN
            THROW 50207, N'El lote ya no está disponible.', 1;
        END;

        IF EXISTS (
            SELECT 1
            FROM @requested AS requested
            LEFT JOIN dbo.Package AS package ON package.Id = requested.PackageId AND package.BatchId = @batchId
            WHERE package.Id IS NULL OR package.AvailableQuantity < requested.Quantity OR package.Price <= 0
        )
        BEGIN
            THROW 50208, N'El inventario no alcanza para los paquetes solicitados.', 1;
        END;

        INSERT INTO dbo.[Order] (BuyerId, BatchId, TotalPaid, PaymentStatus, DeliveryStatus, RequestId)
        VALUES (@buyerId, @batchId, 0, N'Pending', N'Pending', @requestId);
        SET @orderId = SCOPE_IDENTITY();

        INSERT INTO dbo.OrderDetail (OrderId, PackageId, Quantity, UnitPrice)
        SELECT @orderId, package.Id, requested.Quantity, package.Price
        FROM @requested AS requested
        JOIN dbo.Package AS package ON package.Id = requested.PackageId;

        UPDATE package
        SET AvailableQuantity = package.AvailableQuantity - requested.Quantity
        FROM dbo.Package AS package
        JOIN @requested AS requested ON requested.PackageId = package.Id;

        COMMIT;
        SELECT @orderId AS OrderId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK;
        END;
        THROW;
    END CATCH;
END;
GO
