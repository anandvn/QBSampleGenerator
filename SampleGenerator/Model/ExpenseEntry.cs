using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleGenerator.Model
{
    public class ExpenseEntry
    {
        public string QBAcctID { get; set; }
        public string Description { get; set; }
        public string Memo { get; set; }
        public decimal Amount { get; set; }
        public string TxnID { get; set; }
    }
}
