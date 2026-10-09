CREATE OR ALTER PROCEDURE USP_CreateBaseProduct
    @producerId INT,
    @categoryId INT,
    @name NVARCHAR(150),
    @shortDescription NVARCHAR(255),
    @imageUrl NVARCHAR(500) = NULL,
    @measurementUnit NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 
        FROM [User] 
        WHERE Id = @producerId AND RoleId = 2
    )
    BEGIN
        ;THROW 50010, 'Validation Error: Producer does not exist or lacks producer privileges.', 1;
    END

    IF NOT EXISTS (
        SELECT 1 
        FROM ProductCategory 
        WHERE Id = @categoryId
    )
    BEGIN
        ;THROW 50011, 'Validation Error: The specified category does not exist.', 1;
    END

    IF EXISTS (
        SELECT 1 
        FROM BaseProduct 
        WHERE ProducerId = @producerId 
          AND LOWER(TRIM(Name)) = LOWER(TRIM(@name))
          AND IsActive = 1
    )
    BEGIN
        ;THROW 50012, 'Validation Error: An active product with the same name already exists for this producer.', 1;
    END

    INSERT INTO BaseProduct (
        ProducerId,
        CategoryId,
        Name,
        ShortDescription,
        ImageUrl,
        MeasurementUnit,
        IsActive
    )
    VALUES (
        @producerId,
        @categoryId,
        TRIM(@name),
        TRIM(@shortDescription),
        @imageUrl,
        TRIM(@measurementUnit),
        1
    );

    SELECT SCOPE_IDENTITY() AS NewProductId;
END
GO