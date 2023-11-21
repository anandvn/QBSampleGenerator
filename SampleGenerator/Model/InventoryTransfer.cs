using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleGenerator.Model
{
    public enum TransferType { Bill, Credit }

    public class InventoryTransfer 
    {
        public Business Vendor { get; set; }
        public string QBEntityID { get; set; }
        public string QBTermsID { get; set; }
        public DateTime Date { get; set; }
        public decimal SalesTax { get; set; }
        public decimal Shipping { get; set; }
        public string ReferenceNum { get; set; }
        public string PONumber { get; set; }
        public string Memo { get; set; }
        public bool ToBePrinted { get; set; }
        public bool ToBeEmailed { get; set; }
        public ICollection<ItemEntry> Items { get; set; }
        public ICollection<ExpenseEntry> Expenses { get; set; }
        public string EditSequence { get; set; }
        public string TxnID { get; set; }
        public DateTime DueDate { get; set; }

        public InventoryTransfer()
        {
            Items = new HashSet<ItemEntry>();
            Expenses = new HashSet<ExpenseEntry>();
        }
    }
}
