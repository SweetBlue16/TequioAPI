-- ==============================================================================
-- DATABASE CREATION
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'TequioDB')
BEGIN
    CREATE DATABASE TequioDB;
END
GO

USE TequioDB;
GO

-- ==============================================================================
-- DDL SCRIPT: Tequio Database
-- ==============================================================================

CREATE TABLE [Role] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE [User] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoleId INT NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    PaternalLastName NVARCHAR(100) NOT NULL,
    MaternalLastName NVARCHAR(100) NULL,
    BirthDate DATE NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    ProfilePictureUrl NVARCHAR(500) NULL,
    Locality NVARCHAR(100) NULL,
    Biography NVARCHAR(MAX) NULL,
    IsVerified BIT NOT NULL DEFAULT 0,
    RegistrationDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_User_Role FOREIGN KEY (RoleId) REFERENCES [Role](Id)
);

CREATE TABLE VerificationCode (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Code NVARCHAR(10) NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    CreationDate DATETIME NOT NULL DEFAULT GETDATE(),
    ExpirationDate DATETIME NOT NULL,
    RemainingAttempts INT NOT NULL DEFAULT 3,
    CONSTRAINT FK_VerificationCode_User FOREIGN KEY (UserId) REFERENCES [User](Id)
);

CREATE TABLE ProductCategory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ParentCategoryId INT NULL,
    Name NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_ProductCategory_ProductCategory FOREIGN KEY (ParentCategoryId) REFERENCES ProductCategory(Id)
);

CREATE TABLE BaseProduct (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProducerId INT NOT NULL,
    CategoryId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    ShortDescription NVARCHAR(255) NOT NULL,
    ImageUrl NVARCHAR(500) NULL,
    MeasurementUnit NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_BaseProduct_User FOREIGN KEY (ProducerId) REFERENCES [User](Id),
    CONSTRAINT FK_BaseProduct_ProductCategory FOREIGN KEY (CategoryId) REFERENCES ProductCategory(Id)
);

CREATE TABLE DeliveryPoint (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Street NVARCHAR(150) NOT NULL,
    Number NVARCHAR(50) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    ZipCode NVARCHAR(20) NOT NULL,
    Neighborhood NVARCHAR(100) NOT NULL,
    [References] NVARCHAR(MAX) NULL,
    BusinessHours NVARCHAR(200) NOT NULL,
    Latitude DECIMAL(10, 8) NOT NULL,
    Longitude DECIMAL(11, 8) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE Batch (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BaseProductId INT NOT NULL,
    ProducerId INT NOT NULL,
    DeliveryPointId INT NOT NULL,
    BatchName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    MinimumGoal INT NOT NULL,
    Deadline DATETIME NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    PublicationDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Batch_BaseProduct FOREIGN KEY (BaseProductId) REFERENCES BaseProduct(Id),
    CONSTRAINT FK_Batch_User FOREIGN KEY (ProducerId) REFERENCES [User](Id),
    CONSTRAINT FK_Batch_DeliveryPoint FOREIGN KEY (DeliveryPointId) REFERENCES DeliveryPoint(Id)
);

CREATE TABLE Package (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BatchId INT NOT NULL,
    PackageName NVARCHAR(150) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Price DECIMAL(18, 2) NOT NULL,
    AvailableQuantity INT NOT NULL,
    CONSTRAINT FK_Package_Batch FOREIGN KEY (BatchId) REFERENCES Batch(Id)
);

CREATE TABLE [Order] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BuyerId INT NOT NULL,
    BatchId INT NOT NULL,
    TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalPaid DECIMAL(18, 2) NOT NULL,
    PaymentStatus NVARCHAR(50) NOT NULL,
    PaymentReference NVARCHAR(255) NULL,
    DeliveryCode NVARCHAR(50) NULL,
    DeliveryStatus NVARCHAR(50) NOT NULL,
    DeliveryValidationDate DATETIME NULL,
    CONSTRAINT FK_Order_User FOREIGN KEY (BuyerId) REFERENCES [User](Id),
    CONSTRAINT FK_Order_Batch FOREIGN KEY (BatchId) REFERENCES Batch(Id)
);

CREATE TABLE OrderDetail (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    PackageId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    CONSTRAINT FK_OrderDetail_Order FOREIGN KEY (OrderId) REFERENCES [Order](Id),
    CONSTRAINT FK_OrderDetail_Package FOREIGN KEY (PackageId) REFERENCES Package(Id)
);

-- ==============================================================================
-- DML SCRIPT: Seed Data (Roles y Categorías)
-- ==============================================================================

INSERT INTO [Role] (Name) VALUES 
    ('Comprador'), 
    ('Productor'), 
    ('Administrador'), 
    ('Administrador maestro');

DECLARE @CurrentParentId INT;

INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES (NULL, 'Alimentos Frescos y Agroecológicos');
SET @CurrentParentId = SCOPE_IDENTITY();
INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES 
    (@CurrentParentId, 'Frutas y Cítricos'),
    (@CurrentParentId, 'Hortalizas y Verduras'),
    (@CurrentParentId, 'Hongos y Setas');

INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES (NULL, 'Café y Derivados');
SET @CurrentParentId = SCOPE_IDENTITY();
INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES 
    (@CurrentParentId, 'Café en grano'),
    (@CurrentParentId, 'Café molido'),
    (@CurrentParentId, 'Licores y cremas de café');

INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES (NULL, 'Lácteos y Embutidos');
SET @CurrentParentId = SCOPE_IDENTITY();
INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES 
    (@CurrentParentId, 'Quesos'),
    (@CurrentParentId, 'Embutidos y carnes ahumadas');

INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES (NULL, 'Conservas, Salsas y Endulzantes');
SET @CurrentParentId = SCOPE_IDENTITY();
INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES 
    (@CurrentParentId, 'Salsas y chiles secos'),
    (@CurrentParentId, 'Miel y derivados'),
    (@CurrentParentId, 'Dulces típicos y mermeladas');

INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES (NULL, 'Abarrotes y Granos');
SET @CurrentParentId = SCOPE_IDENTITY();
INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES 
    (@CurrentParentId, 'Frijol, arroz y semillas'),
    (@CurrentParentId, 'Piloncillo y especias'),
    (@CurrentParentId, 'Panadería tradicional');

INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES (NULL, 'Artesanías y Manufactura Local');
SET @CurrentParentId = SCOPE_IDENTITY();
INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES 
    (@CurrentParentId, 'Alfarería y cerámica'),
    (@CurrentParentId, 'Talabartería y cuero'),
    (@CurrentParentId, 'Textiles y ropa'),
    (@CurrentParentId, 'Juguetes tradicionales');

INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES (NULL, 'Viveros y Floricultura');
SET @CurrentParentId = SCOPE_IDENTITY();
INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES 
    (@CurrentParentId, 'Plantas de ornato'),
    (@CurrentParentId, 'Árboles frutales y maderables'),
    (@CurrentParentId, 'Tierra y abonos orgánicos');

INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES (NULL, 'Cuidado Personal y Herbolaria');
SET @CurrentParentId = SCOPE_IDENTITY();
INSERT INTO ProductCategory (ParentCategoryId, Name) VALUES 
    (@CurrentParentId, 'Jabones artesanales'),
    (@CurrentParentId, 'Extractos y aceites esenciales');
