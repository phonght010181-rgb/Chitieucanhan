/*
    Du lieu mau cho ung dung Quan Ly Chi Tieu Ca Nhan - ASP.NET Core MVC .NET 10.

    Chay sau khi database da duoc tao migration. Neu database tren may khach
    co ten khac, sua dong USE ben duoi cho trung voi appsettings.json.

    Tai khoan duoc tao neu chua ton tai:
      Email: phonght010181@tvu-onschool.edu.vn
      Mat khau: 123456

    Neu email da ton tai, script giu nguyen mat khau va thong tin tai khoan.
    Giao dich mau co the nhan biet qua hau to "(du lieu mau)".
*/
USE [QuanLyChiTieuCaNhan_Core];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Categories', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Wallets', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Transactions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Budgets', N'U') IS NULL
BEGIN
    THROW 51000, N'Khong tim thay schema ung dung. Hay chay migration .NET 10 truoc khi nap du lieu mau.', 1;
END;

DECLARE @Email nvarchar(200) = N'phonght010181@tvu-onschool.edu.vn';
DECLARE @PasswordHash nvarchar(255) = N'$2a$12$oZP3oI/0Thy6MCVv.FqKiuJEjM1YTYbI/mePMLZeBJYt/dHpwaU2O';
DECLARE @Now datetime = GETDATE();
DECLARE @Today date = CONVERT(date, @Now);
DECLARE @UserId int;
DECLARE @WalletId int;
DECLARE @ExpectedTransactionCount int;
DECLARE @InsertedTransactionCount int;

BEGIN TRY
    BEGIN TRANSACTION;

    SELECT @UserId = UserId
    FROM dbo.Users
    WHERE Email = @Email;

    IF @UserId IS NULL
    BEGIN
        INSERT dbo.Users
            (FullName, Email, PasswordHash, PhoneNumber, Role, IsLocked,
             LoginFailedCount, CreatedAt, IsDeleted)
        VALUES
            (N'Phong HT', @Email, @PasswordHash, NULL, N'User', 0,
             0, @Now, 0);

        SET @UserId = CONVERT(int, SCOPE_IDENTITY());
    END
    ELSE IF EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @UserId AND IsDeleted = 1)
    BEGIN
        THROW 51001, N'Email mau da ton tai nhung tai khoan dang bi xoa mem. Hay xu ly tai khoan nay truoc khi nap du lieu.', 1;
    END;

    DECLARE @DefaultCategories table
    (
        Name nvarchar(100) NOT NULL,
        CategoryType tinyint NOT NULL
    );

    INSERT @DefaultCategories (Name, CategoryType)
    VALUES
        (N'Ăn uống', 2),
        (N'Đi lại', 2),
        (N'Nhà ở', 2),
        (N'Giải trí', 2),
        (N'Sức khỏe', 2),
        (N'Mua sắm', 2),
        (N'Lương', 1),
        (N'Kinh doanh', 1),
        (N'Khác', 1);

    INSERT dbo.Categories (Name, CategoryType, IsSystem, IsDeleted)
    SELECT d.Name, d.CategoryType, 1, 0
    FROM @DefaultCategories AS d
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Categories AS c
        WHERE c.UserId IS NULL
          AND c.IsSystem = 1
          AND c.Name = d.Name
          AND c.CategoryType = d.CategoryType
    );

    INSERT dbo.Categories (UserId, ParentCategoryId, Name, CategoryType, IsSystem, IsDeleted)
    SELECT @UserId, NULL, c.Name, c.CategoryType, 0, 0
    FROM dbo.Categories AS c
    WHERE c.UserId IS NULL
      AND c.IsSystem = 1
      AND c.IsDeleted = 0
      AND c.ParentCategoryId IS NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.Categories AS uc
          WHERE uc.UserId = @UserId
            AND uc.Name = c.Name
            AND uc.CategoryType = c.CategoryType
            AND uc.IsDeleted = 0
      );

    SELECT TOP (1) @WalletId = WalletId
    FROM dbo.Wallets
    WHERE UserId = @UserId
      AND IsDeleted = 0
    ORDER BY IsDefault DESC, WalletId;

    IF @WalletId IS NULL
    BEGIN
        INSERT dbo.Wallets
            (UserId, Name, WalletType, Balance, IsDefault, CreatedAt, IsDeleted)
        VALUES
            (@UserId, N'Tiền mặt', 1, 0, 1, @Now, 0);

        SET @WalletId = CONVERT(int, SCOPE_IDENTITY());
    END;

    DECLARE @SampleTransactions table
    (
        CategoryName nvarchar(100) NOT NULL,
        CategoryType tinyint NOT NULL,
        TransactionType tinyint NOT NULL,
        Amount decimal(18, 2) NOT NULL,
        DaysAgo int NOT NULL,
        Description nvarchar(300) NOT NULL
    );

    INSERT @SampleTransactions
        (CategoryName, CategoryType, TransactionType, Amount, DaysAgo, Description)
    VALUES
        (N'Lương',       1, 1, 18000000, 15, N'Lương tháng (dữ liệu mẫu)'),
        (N'Ăn uống',     2, 2,    85000,  1, N'Bữa sáng (dữ liệu mẫu)'),
        (N'Ăn uống',     2, 2,    65000,  2, N'Cà phê (dữ liệu mẫu)'),
        (N'Đi lại',      2, 2,    60000,  3, N'Đổ xăng (dữ liệu mẫu)'),
        (N'Nhà ở',       2, 2,  4500000,  4, N'Tiền thuê nhà (dữ liệu mẫu)'),
        (N'Giải trí',    2, 2,   280000,  6, N'Xem phim (dữ liệu mẫu)'),
        (N'Sức khỏe',    2, 2,   185000,  8, N'Thuốc và vitamin (dữ liệu mẫu)'),
        (N'Mua sắm',     2, 2,  1250000,  9, N'Mua đồ gia dụng (dữ liệu mẫu)'),
        (N'Ăn uống',     2, 2,    95000, 11, N'Ăn trưa (dữ liệu mẫu)'),
        (N'Đi lại',      2, 2,    35000, 14, N'Gửi xe (dữ liệu mẫu)'),
        (N'Ăn uống',     2, 2,   520000, 16, N'Đi chợ (dữ liệu mẫu)'),
        (N'Giải trí',    2, 2,   150000, 19, N'Xem phim cuối tuần (dữ liệu mẫu)'),
        (N'Nhà ở',       2, 2,   850000, 23, N'Tiền điện nước (dữ liệu mẫu)'),
        (N'Mua sắm',     2, 2,   460000, 28, N'Mua đồ cá nhân (dữ liệu mẫu)');

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions
        WHERE UserId = @UserId
          AND Description LIKE N'% (dữ liệu mẫu)'
    )
    BEGIN
        SELECT @ExpectedTransactionCount = COUNT(*)
        FROM @SampleTransactions;

        INSERT dbo.Transactions
            (UserId, CategoryId, WalletId, ToWalletId, Amount, TransactionType,
             TransactionDate, Description, CreatedAt, IsDeleted)
        SELECT
            @UserId,
            c.CategoryId,
            @WalletId,
            NULL,
            s.Amount,
            s.TransactionType,
            DATEADD(DAY, -s.DaysAgo, CONVERT(datetime, @Today)),
            s.Description,
            @Now,
            0
        FROM @SampleTransactions AS s
        INNER JOIN dbo.Categories AS c
            ON c.UserId = @UserId
           AND c.Name = s.CategoryName
           AND c.CategoryType = s.CategoryType
           AND c.IsDeleted = 0;

        SET @InsertedTransactionCount = @@ROWCOUNT;
        IF @InsertedTransactionCount <> @ExpectedTransactionCount
        BEGIN
            THROW 51002, N'Thieu danh muc mau. Khong co giao dich nao duoc nap; transaction da duoc rollback.', 1;
        END;

        UPDATE dbo.Wallets
        SET Balance = Balance +
        (
            SELECT SUM(CASE WHEN TransactionType = 1 THEN Amount ELSE -Amount END)
            FROM @SampleTransactions
        )
        WHERE WalletId = @WalletId;

        DECLARE @SampleBudgets table
        (
            CategoryName nvarchar(100) NOT NULL,
            LimitAmount decimal(18, 2) NOT NULL
        );

        INSERT @SampleBudgets (CategoryName, LimitAmount)
        VALUES
            (N'Ăn uống', 3000000),
            (N'Đi lại',  1200000),
            (N'Nhà ở',   6000000),
            (N'Mua sắm', 2500000);

        INSERT dbo.Budgets
            (UserId, CategoryId, LimitAmount, SpentAmount, Month, Year, CreatedAt, IsDeleted)
        SELECT
            @UserId,
            c.CategoryId,
            b.LimitAmount,
            COALESCE(SUM(t.Amount), 0),
            MONTH(@Today),
            YEAR(@Today),
            @Now,
            0
        FROM @SampleBudgets AS b
        INNER JOIN dbo.Categories AS c
            ON c.UserId = @UserId
           AND c.Name = b.CategoryName
           AND c.CategoryType = 2
           AND c.IsDeleted = 0
        LEFT JOIN dbo.Transactions AS t
            ON t.UserId = @UserId
           AND t.CategoryId = c.CategoryId
           AND t.TransactionType = 2
           AND t.IsDeleted = 0
           AND t.TransactionDate >= DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1)
           AND t.TransactionDate < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1))
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.Budgets AS existingBudget
            WHERE existingBudget.UserId = @UserId
              AND existingBudget.CategoryId = c.CategoryId
              AND existingBudget.Month = MONTH(@Today)
              AND existingBudget.Year = YEAR(@Today)
              AND existingBudget.IsDeleted = 0
        )
        GROUP BY c.CategoryId, b.LimitAmount;
    END;

    COMMIT TRANSACTION;

    SELECT
        @Email AS Email,
        N'123456' AS PasswordForNewAccount,
        @UserId AS UserId,
        @WalletId AS WalletId,
        (SELECT COUNT(*) FROM dbo.Transactions WHERE UserId = @UserId AND Description LIKE N'% (dữ liệu mẫu)') AS SampleTransactionCount,
        (SELECT COUNT(*) FROM dbo.Budgets WHERE UserId = @UserId AND Month = MONTH(@Today) AND Year = YEAR(@Today) AND IsDeleted = 0) AS CurrentMonthBudgetCount;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
