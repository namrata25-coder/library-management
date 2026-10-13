using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmLeadManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddBearerTokenAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WorkRecurringInvoices columns (safely check before adding to avoid column exists error)
            migrationBuilder.Sql(@"
                IF COL_LENGTH('WorkRecurringInvoices', 'TaxName') IS NULL
                    ALTER TABLE [WorkRecurringInvoices] ADD [TaxName] nvarchar(max) NULL;
                IF COL_LENGTH('WorkRecurringInvoices', 'TaxType') IS NULL
                    ALTER TABLE [WorkRecurringInvoices] ADD [TaxType] nvarchar(max) NULL;
            ");

            // WorkInvoices columns
            migrationBuilder.Sql(@"
                IF COL_LENGTH('WorkInvoices', 'TaxName') IS NULL
                    ALTER TABLE [WorkInvoices] ADD [TaxName] nvarchar(max) NULL;
                IF COL_LENGTH('WorkInvoices', 'TaxType') IS NULL
                    ALTER TABLE [WorkInvoices] ADD [TaxType] nvarchar(max) NULL;
            ");

            // Users columns (Bearer Token Auth + Permissions)
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Users', 'FailedLoginCount') IS NULL
                    ALTER TABLE [Users] ADD [FailedLoginCount] int NOT NULL DEFAULT 0;
                IF COL_LENGTH('Users', 'LastLoginAt') IS NULL
                    ALTER TABLE [Users] ADD [LastLoginAt] datetime2 NULL;
                IF COL_LENGTH('Users', 'LockoutUntil') IS NULL
                    ALTER TABLE [Users] ADD [LockoutUntil] datetime2 NULL;
                IF COL_LENGTH('Users', 'ModulePermissions') IS NULL
                    ALTER TABLE [Users] ADD [ModulePermissions] nvarchar(max) NULL;
                IF COL_LENGTH('Users', 'PasswordHash') IS NULL
                    ALTER TABLE [Users] ADD [PasswordHash] nvarchar(max) NULL;
            ");

            // Quotations columns
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Quotations', 'TaxName') IS NULL
                    ALTER TABLE [Quotations] ADD [TaxName] nvarchar(max) NULL;
                IF COL_LENGTH('Quotations', 'TaxRate') IS NULL
                    ALTER TABLE [Quotations] ADD [TaxRate] nvarchar(max) NULL;
                IF COL_LENGTH('Quotations', 'TaxType') IS NULL
                    ALTER TABLE [Quotations] ADD [TaxType] nvarchar(max) NULL;
            ");

            // Proposals columns
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Proposals', 'Discount') IS NULL
                    ALTER TABLE [Proposals] ADD [Discount] nvarchar(max) NULL;
                IF COL_LENGTH('Proposals', 'GrandTotal') IS NULL
                    ALTER TABLE [Proposals] ADD [GrandTotal] nvarchar(max) NULL;
                IF COL_LENGTH('Proposals', 'Subtotal') IS NULL
                    ALTER TABLE [Proposals] ADD [Subtotal] nvarchar(max) NULL;
                IF COL_LENGTH('Proposals', 'Tax') IS NULL
                    ALTER TABLE [Proposals] ADD [Tax] nvarchar(max) NULL;
                IF COL_LENGTH('Proposals', 'TaxName') IS NULL
                    ALTER TABLE [Proposals] ADD [TaxName] nvarchar(max) NULL;
                IF COL_LENGTH('Proposals', 'TaxRate') IS NULL
                    ALTER TABLE [Proposals] ADD [TaxRate] nvarchar(max) NULL;
                IF COL_LENGTH('Proposals', 'TaxType') IS NULL
                    ALTER TABLE [Proposals] ADD [TaxType] nvarchar(max) NULL;
            ");

            // Estimations columns
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Estimations', 'TaxName') IS NULL
                    ALTER TABLE [Estimations] ADD [TaxName] nvarchar(max) NULL;
                IF COL_LENGTH('Estimations', 'TaxRate') IS NULL
                    ALTER TABLE [Estimations] ADD [TaxRate] nvarchar(max) NULL;
                IF COL_LENGTH('Estimations', 'TaxType') IS NULL
                    ALTER TABLE [Estimations] ADD [TaxType] nvarchar(max) NULL;
            ");

            // AccessTokens table (Bearer Token Auth)
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'AccessTokens', N'U') IS NULL
                BEGIN
                    CREATE TABLE [AccessTokens] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [UserId] int NOT NULL,
                        [TokenHash] nvarchar(64) NOT NULL,
                        [DeviceId] nvarchar(max) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [ExpiresAt] datetime2 NOT NULL,
                        [LastUsedAt] datetime2 NULL,
                        [RevokedAt] datetime2 NULL,
                        CONSTRAINT [PK_AccessTokens] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_AccessTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AccessTokens_TokenHash' AND object_id = OBJECT_ID(N'AccessTokens'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_AccessTokens_TokenHash] ON [AccessTokens] ([TokenHash]);
                END;

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AccessTokens_UserId' AND object_id = OBJECT_ID(N'AccessTokens'))
                BEGIN
                    CREATE INDEX [IX_AccessTokens_UserId] ON [AccessTokens] ([UserId]);
                END;
            ");

            // RoleMasters table
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'RoleMasters', N'U') IS NULL
                BEGIN
                    CREATE TABLE [RoleMasters] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [RoleName] nvarchar(450) NOT NULL,
                        [Description] nvarchar(max) NULL,
                        [IsSystem] bit NOT NULL,
                        [DataAccess] nvarchar(max) NOT NULL,
                        [PermissionsJson] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_RoleMasters] PRIMARY KEY ([Id])
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RoleMasters_RoleName' AND object_id = OBJECT_ID(N'RoleMasters'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_RoleMasters_RoleName] ON [RoleMasters] ([RoleName]);
                END;
            ");

            // TaxMasters table
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'TaxMasters', N'U') IS NULL
                BEGIN
                    CREATE TABLE [TaxMasters] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [TaxName] nvarchar(max) NOT NULL,
                        [TaxType] nvarchar(max) NOT NULL,
                        [TaxRate] decimal(18,2) NOT NULL,
                        [Description] nvarchar(max) NULL,
                        [Status] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_TaxMasters] PRIMARY KEY ([Id])
                    );
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop Bearer token auth objects only, preserving existing business tables/columns
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'AccessTokens', N'U') IS NOT NULL
                    DROP TABLE [AccessTokens];

                IF COL_LENGTH('Users', 'PasswordHash') IS NOT NULL
                    ALTER TABLE [Users] DROP COLUMN [PasswordHash];

                IF COL_LENGTH('Users', 'LockoutUntil') IS NOT NULL
                    ALTER TABLE [Users] DROP COLUMN [LockoutUntil];

                IF COL_LENGTH('Users', 'LastLoginAt') IS NOT NULL
                    ALTER TABLE [Users] DROP COLUMN [LastLoginAt];

                IF COL_LENGTH('Users', 'FailedLoginCount') IS NOT NULL
                    ALTER TABLE [Users] DROP COLUMN [FailedLoginCount];
            ");
        }
    }
}
