CREATE OR ALTER PROCEDURE USP_CreateUser
	@roleId INT,
	@email NVARCHAR(150),
	@passwordHash NVARCHAR(255),
	@firstName NVARCHAR(100),
	@paternalLastName NVARCHAR(100),
	@maternalLastName NVARCHAR(100) = NULL,
	@birthDate DATE,
	@phoneNumber NVARCHAR(20) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @age INT;
	SET @age = DATEDIFF(YEAR, @birthDate, GETDATE());

	IF (MONTH(@birthDate) > MONTH(GETDATE())) OR (MONTH(@birthDate) = MONTH(GETDATE()) AND DAY(@birthDate) > DAY(GETDATE()))
	BEGIN
		SET @age = @age - 1;
	END

	IF @age < 18
	BEGIN
		;THROW 50001, 'Validation Error: User must be at least 18 years old.', 1;
	END

	IF EXISTS (SELECT 1 FROM [User] WHERE Email = @email)
	BEGIN
		;THROW 50002, 'Validation Error: The email address is already registered.', 1;
	END

	INSERT INTO [User] (
		RoleId, 
        Email, 
        PasswordHash, 
        FirstName, 
        PaternalLastName, 
        MaternalLastName, 
        BirthDate, 
        PhoneNumber
	)
	VALUES (
		@roleId, 
        @email, 
        @passwordHash, 
        @firstName, 
        @paternalLastName, 
        @maternalLastName, 
        @birthDate, 
        @phoneNumber
    );

	SELECT SCOPE_IDENTITY() AS NewUserId;
END
GO