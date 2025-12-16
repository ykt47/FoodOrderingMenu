-- ========================================
-- SEED DATA FOR RESTAURANT DINING SYSTEM
-- (Admin User + Dining Promo Codes Only)
-- ========================================
-- Run this in SQL Server Object Explorer
-- Right-click database → New Query → Paste → Execute

-- ========================================
-- 1. ADMIN USER
-- ========================================
-- Email: admin@local
-- Password: Admin123

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@local')
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, EmailConfirmed, Role, CreatedAt)
    VALUES (
        'Admin User', 
        'admin@local', 
        '0C52F1E0A6E6CBE0A39AE0D3C969928C7F6D02EE4B14F5E2B0F7F4B0E1B5D8F1',
        1, 
        'Admin', 
        GETUTCDATE()
    );
    PRINT '✅ Admin user created: admin@local / Admin123';
END
ELSE
BEGIN
    PRINT 'ℹ️ Admin user already exists';
END
GO

-- ========================================
-- 2. RESTAURANT DINE-IN PROMO CODES
-- ========================================

IF NOT EXISTS (SELECT 1 FROM DiscountCodes)
BEGIN
    INSERT INTO DiscountCodes (
        Code, 
        Description, 
        DiscountPercentage, 
        MaxDiscountAmount, 
        MinOrderAmount, 
        ExpiryDate, 
        MaxUses, 
        TimesUsed, 
        IsActive, 
        CreatedAt
    )
    VALUES 
        -- First-time diner discount
        (
            'WELCOME10', 
            'Welcome! 10% off your first dine-in order', 
            10, 
            20.00, 
            30.00, 
            DATEADD(MONTH, 6, GETUTCDATE()), 
            100, 
            0, 
            1, 
            GETUTCDATE()
        ),
        
        -- Large group discount
        (
            'GROUP20', 
            'Group dining discount - 20% off orders above RM 100', 
            20, 
            50.00, 
            100.00, 
            DATEADD(MONTH, 6, GETUTCDATE()), 
            50, 
            0, 
            1, 
            GETUTCDATE()
        ),
        
        -- Lunch special
        (
            'LUNCH15', 
            'Lunch special - 15% off between 11 AM - 3 PM', 
            15, 
            25.00, 
            40.00, 
            DATEADD(MONTH, 3, GETUTCDATE()), 
            NULL, 
            0, 
            1, 
            GETUTCDATE()
        ),
        
        -- Weekend special
        (
            'WEEKEND10', 
            'Weekend special - 10% off Saturday & Sunday', 
            10, 
            30.00, 
            50.00, 
            DATEADD(MONTH, 6, GETUTCDATE()), 
            NULL, 
            0, 
            1, 
            GETUTCDATE()
        ),
        
        -- Birthday special
        (
            'BIRTHDAY20', 
            'Birthday celebration - 20% off for birthday guests', 
            20, 
            40.00, 
            80.00, 
            NULL, 
            NULL, 
            0, 
            1, 
            GETUTCDATE()
        ),
        
        -- Student discount
        (
            'STUDENT5', 
            'Student discount - 5% off with valid student ID', 
            5, 
            15.00, 
            NULL, 
            NULL, 
            NULL, 
            0, 
            1, 
            GETUTCDATE()
        );
    
    PRINT '✅ Restaurant promo codes created';
END
ELSE
BEGIN
    PRINT 'ℹ️ Promo codes already exist';
END
GO

-- ========================================
-- VERIFICATION
-- ========================================
PRINT '';
PRINT '========================================';
PRINT '✅ DATABASE SEEDING COMPLETE!';
PRINT '========================================';
PRINT '';
PRINT '📧 ADMIN LOGIN:';
PRINT '   Email: admin@local';
PRINT '   Password: Admin123';
PRINT '';
PRINT '🎟️ PROMO CODES AVAILABLE:';
PRINT '   • WELCOME10  - 10% off first order (min RM 30)';
PRINT '   • GROUP20    - 20% off large groups (min RM 100)';
PRINT '   • LUNCH15    - 15% lunch special (min RM 40)';
PRINT '   • WEEKEND10  - 10% weekend special (min RM 50)';
PRINT '   • BIRTHDAY20 - 20% birthday celebration (min RM 80)';
PRINT '   • STUDENT5   - 5% student discount (no minimum)';
PRINT '';

-- Show what was created
SELECT 
    (SELECT COUNT(*) FROM Users WHERE Role = 'Admin') AS AdminUsers,
    (SELECT COUNT(*) FROM DiscountCodes WHERE IsActive = 1) AS ActivePromoCodes;

-- Show all promo codes
SELECT 
    Code,
    Description,
    DiscountPercentage AS [Discount %],
    ISNULL(CAST(MinOrderAmount AS VARCHAR), 'No min') AS [Min Order],
    ISNULL(CAST(MaxDiscountAmount AS VARCHAR), 'No max') AS [Max Discount],
    CASE 
        WHEN ExpiryDate IS NULL THEN 'No expiry'
        ELSE CONVERT(VARCHAR, ExpiryDate, 106)
    END AS [Expires],
    IsActive AS Active
FROM DiscountCodes
ORDER BY DiscountPercentage DESC;

PRINT '';
PRINT '========================================';
PRINT '🍽️ Restaurant Dining System Ready!';
PRINT '========================================';
GO