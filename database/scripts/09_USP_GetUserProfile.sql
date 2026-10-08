CREATE OR ALTER PROCEDURE [dbo].[USP_GetUserProfile]
    @userId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        [Id],
        [Email],
        [FirstName],
        [PaternalLastName],
        [MaternalLastName],
        [BirthDate],
        [PhoneNumber],
        [Locality],
        [Biography],
        [ProfilePictureUrl],
        [IsVerified],
        [RegistrationDate]
    FROM 
        [dbo].[User]
    WHERE 
        [Id] = @userId;
END
GO