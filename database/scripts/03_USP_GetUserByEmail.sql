CREATE OR ALTER PROCEDURE USP_GetUserByEmail
	@email NVARCHAR(150)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		Id,
		RoleId,
		PasswordHash,
		FirstName,
		PaternalLastName,
		MaternalLastName,
		ProfilePictureUrl,
		IsVerified
	FROM [User]
	WHERE Email = @email;
END
GO