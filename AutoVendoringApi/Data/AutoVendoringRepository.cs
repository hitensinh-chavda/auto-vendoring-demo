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
            _connStr = config.GetConnectionString("ErpDb") ?? "";
        }

        public Task<int> CreateVendorAsync(VendorDto vendor)
        {
            // demo: return fake id
            return Task.FromResult(new Random().Next(1000, 9999));
        }

        public Task<IEnumerable<dynamic>> GetPendingApprovalsAsync()
        {
            return Task.FromResult<IEnumerable<dynamic>>(new List<dynamic>());
        }

        public Task ApproveAsync(int approvalId, string approver, bool approve, string comments)
        {
            return Task.CompletedTask;
        }

        public Task<int> GenerateBillsAsync()
        {
            return Task.FromResult(0);
        }
    }
}
