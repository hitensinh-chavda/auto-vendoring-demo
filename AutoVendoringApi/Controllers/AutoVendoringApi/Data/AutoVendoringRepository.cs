using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using AutoVendoringApi.Models;

namespace AutoVendoringApi.Data
{
    public class AutoVendoringRepository
    {
        private readonly string _connStr;
        public AutoVendoringRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("ErpDb");
        }

        public async Task<int> CreateVendorAsync(VendorDto vendor)
        {
            using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            try
            {
                var insertVendorSql = @"
                    INSERT INTO dbo.Vendors (VendorName, ContactPerson, Phone, Email, Address, GSTIN, CreatedBy)
                    VALUES (@VendorName, @ContactPerson, @Phone, @Email, @Address, @GSTIN, @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() as int);
                ";
                var vendorId = await conn.QuerySingleAsync<int>(insertVendorSql, new {
                    vendor.VendorName, vendor.ContactPerson, vendor.Phone, vendor.Email, vendor.Address, GSTIN = vendor.Gstin, CreatedBy = vendor.CreatedBy
                }, transaction: tran);

                if (vendor.Prices != null && vendor.Prices.Count > 0)
                {
                    var insertPriceSql = @"
                        INSERT INTO dbo.VendorPrices (VendorId, ItemCode, Unit, Price, Currency, EffectiveFrom, EffectiveTo, Remarks)
                        VALUES (@VendorId, @ItemCode, @Unit, @Price, @Currency, @EffectiveFrom, @EffectiveTo, @Remarks);
                    ";
                    foreach (var p in vendor.Prices)
                    {
                        await conn.ExecuteAsync(insertPriceSql, new {
                            VendorId = vendorId,
                            ItemCode = p.ItemCode,
                            Unit = p.Unit,
                            Price = p.Price ?? 0,
                            Currency = p.Currency ?? "INR",
                            EffectiveFrom = p.EffectiveFrom,
                            EffectiveTo = p.EffectiveTo,
                            Remarks = p.Remarks
                        }, transaction: tran);
                    }
                }

                var insertApproval = @"
                    INSERT INTO dbo.ApprovalQueue (EntityType, EntityId, RequestedBy, [Status], CreatedAt)
                    VALUES ('vendor', @VendorId, @RequestedBy, 'pending', GETDATE());
                ";
                await conn.ExecuteAsync(insertApproval, new { VendorId = vendorId, RequestedBy = vendor.CreatedBy ?? "web_user" }, transaction: tran);

                await tran.CommitAsync();
                return vendorId;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> GetPendingApprovalsAsync()
        {
            using var conn = new SqlConnection(_connStr);
            var sql = @"
                SELECT a.ApprovalId, a.EntityType, a.EntityId, a.RequestedBy, a.CreatedAt,
                       v.VendorName, v.ContactPerson, v.Phone, v.Email
                FROM dbo.ApprovalQueue a
                LEFT JOIN dbo.Vendors v ON a.EntityType = 'vendor' AND a.EntityId = v.VendorId
                WHERE a.[Status] = 'pending'
                ORDER BY a.CreatedAt DESC;
            ";
            return await conn.QueryAsync(sql);
        }

        public async Task ApproveAsync(int approvalId, string approver, bool approve, string comments)
        {
            using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            try
            {
                var status = approve ? "approved" : "rejected";
                var updateSql = @"
                    UPDATE dbo.ApprovalQueue
                    SET [Status] = @Status, Approver = @Approver, ApproverComments = @Comments, ActedAt = GETDATE()
                    WHERE ApprovalId = @ApprovalId;
                ";
                await conn.ExecuteAsync(updateSql, new { Status = status, Approver = approver, Comments = comments, ApprovalId = approvalId }, transaction: tran);

                if (approve)
                {
                    var entitySql = "SELECT EntityType, EntityId FROM dbo.ApprovalQueue WHERE ApprovalId = @ApprovalId";
                    var e = await conn.QuerySingleAsync<dynamic>(entitySql, new { ApprovalId = approvalId }, transaction: tran);

                    if (e.EntityType == "vendor")
                    {
                        var createBillSql = @"
                            INSERT INTO dbo.VendorBillRequests (VendorId, BillDate, Amount, [Status], RequestedBy, CreatedAt)
                            VALUES (@VendorId, GETDATE(), 0, 'pending', @RequestedBy, GETDATE());
                        ";
                        await conn.ExecuteAsync(createBillSql, new { VendorId = (int)e.EntityId, RequestedBy = approver }, transaction: tran);
                    }
                }

                await tran.CommitAsync();
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task<int> GenerateBillsAsync()
        {
            using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            try
            {
                var pickSql = @"
                    SELECT BillId, VendorId, BillDate, Amount
                    FROM dbo.VendorBillRequests
                    WHERE [Status] = 'approved' AND ApprovedAt IS NOT NULL AND BillId NOT IN (SELECT BillId FROM dbo.VendorBillRequests WHERE [Status] = 'generated')
                ";
                var rows = await conn.QueryAsync<dynamic>(pickSql, transaction: tran);
                int generatedCount = 0;
                foreach (var r in rows)
                {
                    var upd = "UPDATE dbo.VendorBillRequests SET [Status] = 'generated', ApprovedAt = GETDATE() WHERE BillId = @BillId";
                    await conn.ExecuteAsync(upd, new { BillId = (int)r.BillId }, transaction: tran);
                    generatedCount++;
                }

                await tran.CommitAsync();
                return generatedCount;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
    }
}
