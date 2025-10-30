using Microsoft.AspNetCore.Mvc;
using AutoVendoringApi.Models;
using AutoVendoringApi.Data;

namespace AutoVendoringApi.Controllers
{
    [Route("api/auto-vendoring")]
    [ApiController]
    public class AutoVendoringController : ControllerBase
    {
        private readonly AutoVendoringRepository _repo;

        public AutoVendoringController(AutoVendoringRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("vendors")]
        public async Task<IActionResult> CreateVendor([FromBody] VendorDto vendor)
        {
            if (vendor == null || string.IsNullOrWhiteSpace(vendor.VendorName))
                return BadRequest(new { error = "vendor_name required" });

            try
            {
                var id = await _repo.CreateVendorAsync(vendor);
                return Ok(new { ok = true, vendor_id = id, message = "Vendor saved and sent for approval" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "server error", details = ex.Message });
            }
        }

        [HttpGet("approvals/pending")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var list = await _repo.GetPendingApprovalsAsync();
            return Ok(list);
        }

        [HttpPost("approvals/{approvalId}/action")]
        public async Task<IActionResult> ActionApproval(int approvalId, [FromBody] ApprovalActionDto body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Approver))
                return BadRequest(new { error = "approver required" });

            try
            {
                await _repo.ApproveAsync(approvalId, body.Approver, body.Approve, body.Comments);
                return Ok(new { ok = true, message = body.Approve ? "Approved" : "Rejected" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "server error", details = ex.Message });
            }
        }

        [HttpPost("bills/generate")]
        public async Task<IActionResult> GenerateBills()
        {
            try
            {
                var count = await _repo.GenerateBillsAsync();
                return Ok(new { ok = true, generated = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class ApprovalActionDto
        {
            public string Approver { get; set; }
            public bool Approve { get; set; }
            public string Comments { get; set; }
        }
    }
}
