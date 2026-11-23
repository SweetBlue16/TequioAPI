CREATE OR ALTER PROCEDURE USP_DeactivateBaseProduct
    @productId INT,
    @producerId INT
AS
BEGIN
    SET NOCOUNT ON;

    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

            IF NOT EXISTS (
                SELECT 1
                FROM dbo.BaseProduct WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @productId AND ProducerId = @producerId
            )
            BEGIN
                ;THROW 50015, 'Validation Error: Product does not exist or does not belong to the specified producer.', 1;
            END

            IF EXISTS (
                SELECT 1
                FROM dbo.BaseProduct
                WHERE Id = @productId AND IsActive = 0
            )
            BEGIN
                ;THROW 50016, 'Validation Error: Product is already deactivated.', 1;
            END

            IF EXISTS (
                SELECT 1
                FROM dbo.Batch
                WHERE BaseProductId = @productId
                  AND Status IN ('Draft', 'Active', 'Confirmed')
            )
            BEGIN
                ;THROW 50017, 'Validation Error: Cannot deactivate product with active or confirmed batches.', 1;
            END

            UPDATE dbo.BaseProduct
            SET IsActive = 0
            WHERE Id = @productId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK;
        END;
        THROW;
    END CATCH;

    SELECT 1 AS IsSuccess;
END
GO