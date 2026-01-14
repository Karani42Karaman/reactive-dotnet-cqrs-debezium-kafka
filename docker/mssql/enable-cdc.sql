-- Database level
USE master;
GO
ALTER DATABASE payment SET RECOVERY FULL;
GO

USE payment;
GO

EXEC sys.sp_cdc_enable_db;
GO

-- Table level
EXEC sys.sp_cdc_enable_table
    @source_schema = 'dbo',
    @source_name   = 'Transactions',
    @role_name     = NULL;
GO
