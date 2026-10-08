CREATE OR ALTER PROCEDURE [dbo].[USP_UpdateUserProfile]
    @userId INT,
    @firstName NVARCHAR(100),
    @paternalLastName NVARCHAR(100),
    @maternalLastName NVARCHAR(100) = NULL,
    @birthDate DATE,
    @phoneNumber NVARCHAR(20) = NULL,
    @locality NVARCHAR(100) = NULL,
    @biography NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[User]
    SET 
        [FirstName] = @firstName,
        [PaternalLastName] = @paternalLastName,
        [MaternalLastName] = @maternalLastName,
        [BirthDate] = @birthDate,
        [PhoneNumber] = @phoneNumber,
        [Locality] = @locality,
        [Biography] = @biography
    WHERE 
        [Id] = @userId;
END
GO