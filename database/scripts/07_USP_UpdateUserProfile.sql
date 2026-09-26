CREATE OR ALTER PROCEDURE USP_UpdateUserProfile
	@userId INT,
	@phoneNumber NVARCHAR(20) = NULL,
	@locality NVARCHAR(100) = NULL,
	@biography NVARCHAR(MAX) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE [User]
	SET
		PhoneNumber = ISNULL(@phoneNumber, PhoneNumber),
		Locality = ISNULL(@locality, Locality),
		Biography = ISNULL(@biography, Biography)
	WHERE Id = @userId;

	SELECT 1 AS IsSuccess;
END
GO