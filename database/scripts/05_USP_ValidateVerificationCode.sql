CREATE OR ALTER PROCEDURE USP_ValidateVerificationCode
	@userId INT,
	@code NVARCHAR(10),
	@type NVARCHAR(50)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @actualCode NVARCHAR(10);
    DECLARE @expirationDate DATETIME;
    DECLARE @remainingAttempts INT;
    DECLARE @codeId INT;

	SELECT TOP 1
		@codeId = Id,
		@actualCode = Code,
		@expirationDate = ExpirationDate,
		@remainingAttempts = RemainingAttempts
	FROM VerificationCode
	WHERE UserId = @userId AND Type = @type
	ORDER BY CreationDate DESC;

	IF @codeId IS NULL OR @remainingAttempts <= 0
	BEGIN
		;THROW 50003, 'Validation Error: No active verification code found or maximum attempts reached.', 1;
	END

	IF GETDATE() > @expirationDate
	BEGIN
		UPDATE VerificationCode SET RemainingAttempts = 0 WHERE Id = @codeId;
		;THROW 50004, 'Validation Error: The verification code has expired.', 1;
	END

	IF @actualCode <> @code
	BEGIN
		UPDATE VerificationCode
		SET RemainingAttempts = RemainingAttempts - 1
		WHERE Id = @codeId;

		;THROW 50005, 'Validation Error: Incorrect verification code.', 1;
	END

	UPDATE VerificationCode
	SET RemainingAttempts = 0
	WHERE Id = @codeId;

	IF @type = 'AccountVerification'
	BEGIN
		UPDATE [User]
		SET IsVerified = 1
		WHERE Id = @userId;
	END

	SELECT 1 AS IsSuccess;
END
GO