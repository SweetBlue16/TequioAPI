-- Incremental OTP correction. No existing account is marked verified by installation.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.USP_GenerateVerificationCode
    @userId INT, @code NVARCHAR(10), @type NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @roleId INT, @isVerified BIT, @now DATETIME = GETDATE(), @attempts INT = 3;
        SELECT @roleId = RoleId, @isVerified = IsVerified FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @userId;
        IF @roleId IS NULL OR @code IS NULL OR LEN(@code) <> 6 OR @code LIKE N'%[^0-9]%'
            THROW 50003, N'La solicitud de código no es válida.', 1;
        IF @type = N'AccountVerification' AND @roleId IN (3,4)
            THROW 50003, N'La cuenta administrativa requiere autorización protegida.', 1;
        IF @type = N'AdminAuthorization'
        BEGIN
            IF @roleId <> 3 OR @isVerified = 1
                THROW 50003, N'No hay una solicitud pendiente de administrador.', 1;
            DECLARE @latestDate DATETIME, @latestExpiration DATETIME, @latestAttempts INT;
            SELECT TOP (1) @latestDate = CreationDate, @latestExpiration = ExpirationDate,
                @latestAttempts = RemainingAttempts
            FROM dbo.VerificationCode WHERE UserId = @userId AND Type = @type ORDER BY Id DESC;
            IF @latestExpiration > @now AND @latestAttempts <= 0
                THROW 50021, N'Espera a que venza el código sin intentos disponibles.', 1;
            IF @latestDate > DATEADD(SECOND, -60, @now)
                THROW 50020, N'Espera antes de solicitar otro código.', 1;
            IF (SELECT COUNT(*) FROM dbo.VerificationCode WHERE UserId = @userId AND Type = @type
                AND CreationDate > DATEADD(MINUTE, -15, @now)) >= 3
                THROW 50021, N'Se alcanzó el límite de solicitudes.', 1;
            -- A resend must not replenish guesses while the previous challenge is still live.
            IF @latestExpiration > @now SET @attempts = @latestAttempts;
        END;
        UPDATE dbo.VerificationCode SET RemainingAttempts = 0
        WHERE UserId = @userId AND Type = @type AND RemainingAttempts > 0;
        INSERT dbo.VerificationCode (UserId, Code, Type, CreationDate, ExpirationDate, RemainingAttempts)
        VALUES (@userId, @code, @type, @now, DATEADD(MINUTE, 15, @now), @attempts);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.USP_ValidateVerificationCode
    @userId INT, @code NVARCHAR(10), @type NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @error INT = 0;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @roleId INT, @actualCode NVARCHAR(10), @expirationDate DATETIME;
        DECLARE @remainingAttempts INT, @codeId INT;
        SELECT @roleId = RoleId FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK) WHERE Id = @userId;
        IF @roleId IS NULL OR (@type = N'AccountVerification' AND @roleId IN (3,4))
            OR (@type = N'AdminAuthorization' AND @roleId <> 3)
            SET @error = 50003;
        ELSE
        BEGIN
            SELECT TOP (1) @codeId = Id, @actualCode = Code, @expirationDate = ExpirationDate,
                @remainingAttempts = RemainingAttempts
            FROM dbo.VerificationCode WITH (UPDLOCK, HOLDLOCK)
            WHERE UserId = @userId AND Type = @type ORDER BY Id DESC;
            IF @codeId IS NULL OR @remainingAttempts <= 0 SET @error = 50003;
            ELSE IF @expirationDate <= GETDATE()
            BEGIN
                UPDATE dbo.VerificationCode SET RemainingAttempts = 0 WHERE Id = @codeId;
                SET @error = 50004;
            END
            ELSE IF @code IS NULL OR @actualCode <> @code
            BEGIN
                UPDATE dbo.VerificationCode SET RemainingAttempts = RemainingAttempts - 1 WHERE Id = @codeId;
                SET @error = 50005;
            END
            ELSE
            BEGIN
                UPDATE dbo.VerificationCode SET RemainingAttempts = 0 WHERE Id = @codeId;
                IF @type = N'AccountVerification' AND @roleId NOT IN (3,4)
                    UPDATE dbo.[User] SET IsVerified = 1 WHERE Id = @userId;
                IF @type = N'AdminAuthorization' AND @roleId = 3
                    UPDATE dbo.[User] SET IsVerified = 1 WHERE Id = @userId;
            END;
        END;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
    -- Throw after committing so failed guesses remain consumed in normal (non-ambient) API requests.
    IF @error = 50003 THROW 50003, N'No hay código utilizable o se agotaron los intentos.', 1;
    IF @error = 50004 THROW 50004, N'El código venció.', 1;
    IF @error = 50005 THROW 50005, N'El código no es válido.', 1;
    SELECT 1 AS IsSuccess;
END;
GO
