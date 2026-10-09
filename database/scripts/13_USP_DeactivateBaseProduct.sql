CREATE OR ALTER PROCEDURE USP_DeactivateBaseProduct
    @productId INT,
    @producerId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 
        FROM BaseProduct 
        WHERE Id = @productId AND ProducerId = @producerId
    )
    BEGIN
        ;THROW 50015, 'Validation Error: Product does not exist or does not belong to the specified producer.', 1;
    END

    IF EXISTS (
        SELECT 1 
        FROM BaseProduct 
        WHERE Id = @productId AND IsActive = 0
    )
    BEGIN
        ;THROW 50016, 'Validation Error: Product is already deactivated.', 1;
    END

    IF EXISTS (
        SELECT 1 
        FROM Batch 
        WHERE BaseProductId = @productId 
          AND Status IN ('Draft', 'Active', 'Confirmed')
    )
    BEGIN
        ;THROW 50017, 'Validation Error: Cannot deactivate product with active or confirmed batches (RN-10).', 1;
    END

    UPDATE BaseProduct
    SET IsActive = 0
    WHERE Id = @productId;

    SELECT 1 AS IsSuccess;
END
GO