-- ========================================
-- Auto Vendoring - Database Tables
-- ========================================

CREATE TABLE dbo.Vendors (
    VendorId INT IDENTITY(1,1) PRIMARY KEY,
    VendorName NVARCHAR(200) NOT NULL,
    ContactPerson NVARCHAR(100),
    Phone NVARCHAR(50),
    Email NVARCHAR(100),
    Address NVARCHAR(300),
    GSTIN NVARCHAR(50),
    CreatedBy NVARCHAR(50),
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE dbo.VendorPrices (
    PriceId INT IDENTITY(1,1) PRIMARY KEY,
    VendorId INT NOT NULL,
    ItemCode NVARCHAR(100),
    Unit NVARCHAR(50),
    Price DECIMAL(18,2),
    Currency NVARCHAR(10),
    EffectiveFrom DATE,
    EffectiveTo DATE,
    Remarks NVARCHAR(200),
    FOREIGN KEY (VendorId) REFERENCES dbo.Vendors(VendorId)
);

CREATE TABLE dbo.ApprovalQueue (
    ApprovalId INT IDENTITY(1,1) PRIMARY KEY,
    EntityType NVARCHAR(50),
    EntityId INT,
    RequestedBy NVARCHAR(50),
    Approver NVARCHAR(50),
    ApproverComments NVARCHAR(500),
    Status NVARCHAR(20) DEFAULT 'pending',
    CreatedAt DATETIME DEFAULT GETDATE(),
    ActedAt DATETIME
);

CREATE TABLE dbo.VendorBillRequests (
    BillId INT IDENTITY(1,1) PRIMARY KEY,
    VendorId INT,
    BillDate DATETIME DEFAULT GETDATE(),
    Amount DECIMAL(18,2),
    Status NVARCHAR(20) DEFAULT 'pending',
    RequestedBy NVARCHAR(50),
    ApprovedAt DATETIME,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (VendorId) REFERENCES dbo.Vendors(VendorId)
);

-- Optional: Preload sample data
INSERT INTO dbo.Vendors (VendorName, ContactPerson, Phone, Email, GSTIN, CreatedBy)
VALUES ('ABC Supplies', 'Ravi Mehta', '9876543210', 'ravi@abc.com', '27ABCDE1234F2Z5', 'system');

PRINT 'Auto Vendoring tables created successfully.';
