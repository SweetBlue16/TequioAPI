CREATE OR ALTER PROCEDURE USP_UpdateUserPassword
	@userId INT,
	@newPasswordHash NVARCHAR(255)
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE [User]
	SET PasswordHash = @newPasswordHash
	WHERE Id = @userId;

	SELECT 1 AS IsSuccess;
END
GO