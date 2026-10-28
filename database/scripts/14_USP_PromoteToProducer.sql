USE TequioDB;
GO
CREATE OR ALTER PROCEDURE USP_PromoteToProducer
    @userId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 
        FROM [dbo].[User] 
        WHERE [Id] = @userId
    )
    BEGIN
        ;THROW 50018, 'Validation Error: User does not exist.', 1;
    END

    IF EXISTS (
        SELECT 1 
        FROM [dbo].[User] 
        WHERE [Id] = @userId AND [RoleId] = 2
    )
    BEGIN
        ;THROW 50019, 'Validation Error: User already has producer privileges.', 1;
    END

    UPDATE [dbo].[User]
    SET [RoleId] = 2
    WHERE [Id] = @userId;

    SELECT 1 AS IsSuccess;
END
GO