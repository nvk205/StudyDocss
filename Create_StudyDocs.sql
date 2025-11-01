-- Tạo DB (nếu chưa có) – bạn có thể tự tạo trước rồi chỉ cần CREATE TABLES
IF DB_ID('StudyDocs') IS NULL
BEGIN
  CREATE DATABASE StudyDocs;
END
GO
USE StudyDocs;
GO

-- Bảng người dùng
IF OBJECT_ID('dbo.[User]','U') IS NULL
BEGIN
  CREATE TABLE dbo.[User](
    UserId        INT IDENTITY PRIMARY KEY,
    Username      NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash  VARBINARY(64) NOT NULL,   -- PBKDF2-SHA256 output 32 bytes (64 varbinary for safety)
    PasswordSalt  VARBINARY(16) NOT NULL,   -- 16 bytes salt
    DisplayName   NVARCHAR(100) NULL,
    Role          NVARCHAR(20)  NOT NULL DEFAULT N'User', -- 'User' | 'Admin'
    IsActive      BIT NOT NULL DEFAULT 1,
    CreatedAt     DATETIME NOT NULL DEFAULT GETDATE()
  );
END
GO

-- Bảng môn học
IF OBJECT_ID('dbo.[Subject]','U') IS NULL
BEGIN
  CREATE TABLE dbo.[Subject](
    SubjectId INT IDENTITY PRIMARY KEY,
    [Name]    NVARCHAR(100) NOT NULL UNIQUE
  );
END
GO

-- Bảng tài liệu
IF OBJECT_ID('dbo.[Document]','U') IS NULL
BEGIN
  CREATE TABLE dbo.[Document](
    DocumentId  INT IDENTITY PRIMARY KEY,
    Title       NVARCHAR(255) NOT NULL,
    SubjectId   INT NULL REFERENCES dbo.[Subject](SubjectId) ON DELETE SET NULL,
    [Type]      NVARCHAR(20) NOT NULL,              -- pdf/docx/pptx/txt/link
    FileData    VARBINARY(MAX) NULL,                -- NULL nếu Type='link'
    FileName    NVARCHAR(255) NULL,
    FilePath    NVARCHAR(500) NULL,                 -- dùng cho link (URL)
    Notes       NVARCHAR(MAX) NULL,
    [Status]    BIT NOT NULL DEFAULT 0,             -- đã học?
    CreatedBy   INT NOT NULL REFERENCES dbo.[User](UserId) ON DELETE CASCADE,
    CreatedAt   DATETIME NOT NULL DEFAULT GETDATE(),
    LastOpened  DATETIME NULL
  );
END
GO

-- Giới hạn kích thước file <= 50MB (khoảng 52,428,800 bytes)
-- CHECK sẽ bỏ qua NULL (trường hợp link)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Document_FileSize_50MB')
BEGIN
  ALTER TABLE dbo.[Document] WITH NOCHECK
  ADD CONSTRAINT CK_Document_FileSize_50MB
  CHECK (FileData IS NULL OR DATALENGTH(FileData) <= 52428800);
END
GO

-- Seed vài môn học mẫu (idempotent)
IF NOT EXISTS (SELECT 1 FROM dbo.[Subject])
BEGIN
  INSERT INTO dbo.[Subject]([Name]) VALUES (N'Toán'),(N'Lý'),(N'Hóa'),(N'Tiếng Anh'),(N'Lập trình');
END
GO

-- Không seed user ở SQL (vì app dùng PBKDF2). Tạo user đầu tiên bằng "Đăng ký" trong ứng dụng.
PRINT 'StudyDocs schema ready. Use the app to create the first user (admin if no users yet).';
GO
