SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- Only explicitly terminal batches release a delivery point. Unknown states remain current.
-- English identifiers match the schema; Spanish aliases preserve the domain documentation.
CREATE OR ALTER FUNCTION dbo.UF_IsCurrentBatch(@status NVARCHAR(50))
RETURNS BIT
AS
BEGIN
    RETURN CASE WHEN UPPER(LTRIM(RTRIM(@status))) COLLATE Latin1_General_100_CI_AI
        IN (N'CANCELLED', N'CANCELED', N'CANCELADO', N'COMPLETED', N'FINALIZADO', N'ENTREGADO')
        THEN 0 ELSE 1 END;
END;
GO
CREATE OR ALTER PROCEDURE dbo.USP_SetDeliveryPointStatus @id INT, @isActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        -- Exclusive parent lock coordinates status changes with FK checks and the batch trigger.
        IF NOT EXISTS (SELECT 1 FROM dbo.DeliveryPoint WITH (XLOCK, HOLDLOCK) WHERE Id = @id)
            THROW 50104, N'No se encontró el punto de entrega.', 1;
        IF @isActive IS NULL THROW 50101, N'El estado es obligatorio.', 1;
        IF @isActive = 0 AND EXISTS (
            SELECT 1 FROM dbo.Batch WITH (UPDLOCK, HOLDLOCK, INDEX(IX_Batch_DeliveryPointId))
            WHERE DeliveryPointId = @id AND dbo.UF_IsCurrentBatch(Status) = 1)
            THROW 50103, N'El punto tiene lotes activos, confirmados o vigentes.', 1;
        UPDATE dbo.DeliveryPoint SET IsActive = @isActive WHERE Id = @id;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
-- All existing and future batch writers must preserve the invariant, including concurrent inserts/status changes.
CREATE OR ALTER TRIGGER dbo.TR_Batch_RequireActiveDeliveryPoint ON dbo.Batch
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.DeliveryPoint d WITH (UPDLOCK, HOLDLOCK) ON d.Id = i.DeliveryPointId
        WHERE dbo.UF_IsCurrentBatch(i.Status) = 1 AND d.IsActive = 0)
        THROW 50103, N'El lote vigente requiere un punto de entrega activo.', 1;
END;
GO
