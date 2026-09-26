CREATE OR ALTER PROCEDURE USP_GenerateVerificationCode
	@userId INT,
	@code NVARCHAR(10),
	@type NVARCHAR(50)
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE VerificationCode
	SET RemainingAttempts = 0
	WHERE UserId = @userId AND Type = @type AND ExpirationDate > GETDATE() AND RemainingAttempts > 0;

	INSERT INTO VerificationCode (
        UserId,
        Code,
        Type,
        CreationDate,
        ExpirationDate,
        RemainingAttempts
    )
    VALUES (
        @userId,
        @code,
        @type,
        GETDATE(),
        DATEADD(MINUTE, 15, GETDATE()),
        3
    );
END
GO