using System;
using System.Collections.Generic;

namespace AutoVendoringApi.Models
{
    public class VendorDto
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Gstin { get; set; }
        public string CreatedBy { get; set; }
        public List<VendorPriceDto> Prices { get; set; }
    }

    public class VendorPriceDto
    {
        public int PriceId { get; set; }
        public string ItemCode { get; set; }
        public string Unit { get; set; }
        public decimal? Price { get; set; }
        public string Currency { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string Remarks { get; set; }
    }
}
