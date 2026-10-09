CREATE OR ALTER PROCEDURE [dbo].[USP_GetProductsByCategory]
    @categoryId INT,
    @pageIndex INT,
    @pageSize INT,
    @totalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @pageIndex < 0
BEGIN
        ;THROW 50009, 'Validation Error: Page index cannot be negative.', 1;
END

    IF @pageSize <= 0
BEGIN
        ;THROW 50010, 'Validation Error: Page size must be greater than zero.', 1;
END

    IF @pageSize > 100
BEGIN
        ;THROW 50011, 'Validation Error: Page size cannot exceed 100 products.', 1;
END

SELECT
    @totalCount = COUNT([Id])
FROM
    [dbo].[BaseProduct]
WHERE
    [CategoryId] = @categoryId;

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
    [CategoryId] = @categoryId
ORDER BY
    [Id] ASC
OFFSET (@pageIndex * @pageSize) ROWS
    FETCH NEXT @pageSize ROWS ONLY;
END
GO