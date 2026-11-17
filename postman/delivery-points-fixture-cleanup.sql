-- After the runner: cancel only this execution's fixture batch and logically deactivate its point.
-- No physical deletion, no alteration of real users/products, all associations retained.
DECLARE @runId VARCHAR(32) = 'REPLACE_WITH_32_HEX_CHARACTERS';
DECLARE @deliveryPointId INT = 0, @batchId INT = 0;
IF LEN(@runId) <> 32 OR @runId LIKE '%[^0-9a-f]%' OR @deliveryPointId < 1 OR @batchId < 1
    THROW 50101, N'Configura los identificadores de esta ejecución.', 1;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.Batch b WITH (UPDLOCK, HOLDLOCK)
    JOIN dbo.DeliveryPoint d ON d.Id=b.DeliveryPointId
    WHERE b.Id=@batchId AND d.Id=@deliveryPointId
      AND b.BatchName=N'TEST_'+@runId+N'_linked' AND d.Name=b.BatchName)
    THROW 50101, N'Los datos no pertenecen al fixture indicado.', 1;
IF EXISTS (SELECT 1 FROM dbo.[Order] WHERE BatchId=@batchId)
    THROW 50101, N'El fixture tiene compras; no se modificará.', 1;
UPDATE dbo.Batch SET Status=N'Cancelled' WHERE Id=@batchId;
EXEC dbo.USP_SetDeliveryPointStatus @id=@deliveryPointId, @isActive=0;
COMMIT;
SELECT d.Id, d.IsActive, b.Id AS BatchId, b.Status FROM dbo.DeliveryPoint d
JOIN dbo.Batch b ON b.DeliveryPointId=d.Id WHERE d.Id=@deliveryPointId AND b.Id=@batchId;
