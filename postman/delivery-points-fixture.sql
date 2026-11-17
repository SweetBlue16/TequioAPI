-- Run only on an isolated TEQUIO test database after scripts 15, 16, 17.
-- Supply an existing controlled TEST_ producer and TEST_ product; never reuse a real customer's data.
DECLARE @runId VARCHAR(32) = 'REPLACE_WITH_32_HEX_CHARACTERS';
DECLARE @producerId INT = 0;
DECLARE @baseProductId INT = 0;
IF LEN(@runId) <> 32 OR @runId LIKE '%[^0-9a-f]%' OR @producerId < 1 OR @baseProductId < 1
    THROW 50101, N'Configura runId, productor y producto de prueba.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE Id=@producerId AND RoleId=2
    AND FirstName LIKE N'TEST[_]%')
    THROW 50101, N'El productor debe ser un fixture TEST_.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.BaseProduct WHERE Id=@baseProductId
    AND ProducerId=@producerId AND Name LIKE N'TEST[_]%')
    THROW 50101, N'El producto debe ser un fixture TEST_ del productor.', 1;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @point TABLE (Id INT);
DECLARE @name NVARCHAR(150) = N'TEST_' + @runId + N'_linked';
INSERT @point EXEC dbo.USP_CreateDeliveryPoint @name=@name, @street=N'Enriquez', @number=N'1',
    @neighborhood=N'Centro', @zipCode=N'91000', @city=N'Xalapa', @businessHours=N'09:00 - 18:00',
    @latitude=19.53, @longitude=-96.92;
DECLARE @deliveryPointId INT = (SELECT Id FROM @point);
INSERT dbo.Batch (BaseProductId, ProducerId, DeliveryPointId, BatchName, Description, MinimumGoal, Deadline, Status)
VALUES (@baseProductId, @producerId, @deliveryPointId, @name, N'Fixture RN-07', 1,
    DATEADD(DAY, 7, GETUTCDATE()), N'Active');
DECLARE @batchId INT = SCOPE_IDENTITY();
COMMIT;
-- Copy only these nonsecret identifiers to the local Postman environment.
SELECT @deliveryPointId AS linkedDeliveryPointId, @batchId AS linkedBatchId,
    (SELECT Id FROM dbo.Municipality WHERE Name=N'Xalapa') AS municipalityId;
