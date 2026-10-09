CREATE OR ALTER PROCEDURE [dbo].[USP_GetProductsByProducer]
    @producerId INT,
    @pageIndex INT,
    @pageSize INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @pageIndex < 0
BEGIN
        ;THROW 50006, 'Validation Error: Page index cannot be negative.', 1;
END

    IF @pageSize <= 0
BEGIN
        ;THROW 50007, 'Validation Error: Page size must be greater than zero.', 1;
END

    IF @pageSize > 100
BEGIN
        ;THROW 50008, 'Validation Error: Page size cannot exceed 100 products.', 1;
END

SELECT
    [Id],
    [ProducerId],
    [CategoryId],
    [Name],
    [ShortDescription],
    [ImageUrl],
    [MeasurementUnit],
    [IsActive]
FROM
    [dbo].[BaseProduct]
WHERE
    [ProducerId] = @producerId
ORDER BY
    [Id] ASC
OFFSET (@pageIndex * @pageSize) ROWS
    FETCH NEXT @pageSize ROWS ONLY;
END
GO