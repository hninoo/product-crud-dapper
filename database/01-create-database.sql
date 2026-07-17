IF DB_ID(N'ProductCrudDb') IS NULL
BEGIN
    CREATE DATABASE ProductCrudDb;
END;
GO

USE ProductCrudDb;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductId     INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Products PRIMARY KEY,
        Name          NVARCHAR(100) NOT NULL,
        Description   NVARCHAR(500) NULL,
        Price         DECIMAL(18,2) NOT NULL,
        StockQuantity INT NOT NULL
            CONSTRAINT DF_Products_StockQuantity DEFAULT (0),
        IsActive      BIT NOT NULL
            CONSTRAINT DF_Products_IsActive DEFAULT (1),
        CreatedAt     DATETIME2(0) NOT NULL
            CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt     DATETIME2(0) NULL,

        CONSTRAINT UQ_Products_Name UNIQUE (Name),
        CONSTRAINT CK_Products_Price CHECK (Price >= 0),
        CONSTRAINT CK_Products_StockQuantity CHECK (StockQuantity >= 0)
    );
END;
GO

-- Full-Text Search index for the product search box .
-- Guarded so the script still succeeds on servers without FTS installed;
-- the application falls back to LIKE search in that case.
IF ISNULL(CAST(SERVERPROPERTY('IsFullTextInstalled') AS int), 0) = 1
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'ProductsCatalog')
    BEGIN
        EXEC (N'CREATE FULLTEXT CATALOG ProductsCatalog AS DEFAULT;');
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.fulltext_indexes
        WHERE object_id = OBJECT_ID(N'dbo.Products')
    )
    BEGIN
        EXEC (N'CREATE FULLTEXT INDEX ON dbo.Products
                (
                    Name        LANGUAGE 1033,
                    Description LANGUAGE 1033
                )
                KEY INDEX PK_Products
                ON ProductsCatalog
                WITH CHANGE_TRACKING AUTO;');
    END;
END;
GO

-- Seed data: inserts any of the 20 sample products that are not already
-- present (matched by name), so re-running the script is always safe.
INSERT INTO dbo.Products (Name, Description, Price, StockQuantity, IsActive)
SELECT v.Name, v.Description, v.Price, v.StockQuantity, v.IsActive
FROM (VALUES
    (N'Wireless Mouse', N'Ergonomic 2.4 GHz wireless mouse with USB receiver.', CAST(19.99 AS DECIMAL(18,2)), 120, 1),
    (N'Mechanical Keyboard', N'87-key mechanical keyboard with blue switches.', 59.50, 45, 1),
    (N'USB-C Hub', N'7-in-1 USB-C hub with HDMI, USB 3.0, and SD card reader.', 34.00, 80, 1),
    (N'HD Webcam', N'1080p webcam with built-in microphone.', 42.75, 0, 0),
    (N'Laptop Stand', N'Adjustable aluminium laptop stand for 10-17 inch laptops.', 27.50, 60, 1),
    (N'Bluetooth Speaker', N'Portable speaker with 12-hour battery and IPX5 rating.', 45.00, 35, 1),
    (N'Noise-Cancelling Headphones', N'Over-ear wireless headphones with active noise cancelling.', 129.99, 25, 1),
    (N'27-inch Monitor', N'27-inch QHD IPS monitor with 75 Hz refresh rate.', 219.00, 18, 1),
    (N'Ergonomic Office Chair', N'Mesh-back office chair with lumbar support.', 189.99, 12, 1),
    (N'Standing Desk', N'Electric height-adjustable standing desk, 120x60 cm.', 349.00, 8, 1),
    (N'USB Microphone', N'Cardioid condenser USB microphone for streaming and calls.', 79.99, 40, 1),
    (N'Ring Light', N'10-inch LED ring light with tripod and phone holder.', 24.99, 55, 1),
    (N'External SSD 1TB', N'Portable 1 TB USB-C solid state drive, 1050 MB/s.', 99.50, 30, 1),
    (N'Wireless Charger', N'15 W fast wireless charging pad with USB-C cable.', 18.75, 90, 1),
    (N'Smart Power Strip', N'Wi-Fi power strip with 4 outlets and 4 USB ports.', 32.40, 42, 1),
    (N'Cable Organizer Kit', N'Cable management kit with sleeves, clips, and ties.', 12.99, 150, 1),
    (N'Laptop Sleeve 15 inch', N'Water-resistant neoprene sleeve for 15-inch laptops.', 21.00, 70, 1),
    (N'Graphics Tablet', N'10x6 inch pen tablet with 8192 pressure levels.', 89.00, 15, 1),
    (N'LED Desk Lamp', N'Dimmable LED desk lamp with USB charging port.', 26.30, 48, 1),
    (N'Portable Projector', N'1080p mini projector with HDMI and USB-C input.', 259.99, 0, 0)
) AS v (Name, Description, Price, StockQuantity, IsActive)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products p WHERE p.Name = v.Name);
GO
