CREATE OR ALTER PROCEDURE USP_UpdateUserProfilePicture
    @userId INT,
    @profilePictureUrl NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [User]
    SET ProfilePictureUrl = @profilePictureUrl
    WHERE Id = @userId;
    
    SELECT 1 AS IsSuccess;
END
GO