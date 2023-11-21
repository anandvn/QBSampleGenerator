using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleGenerator.Model
{
    public class ItemEntry
    {
        public string ItemLookupCode { get; set; }
        public string VendorSKU { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string TxnID { get; set; }
    }
}
