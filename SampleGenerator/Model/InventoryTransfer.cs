using log4net.Util;
using Microsoft.SqlServer.Server;
using QBFC15Lib;
using System;
using System.Collections.Generic;
using System.IO;
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

        public static InventoryTransfer Create(IBillRet billret)
        {
            var bill = new InventoryTransfer()
            {
                Items = new List<ItemEntry>(),
                Expenses = new List<ExpenseEntry>(),
                Date = billret.TxnDate.GetValue(),
                DueDate = billret.DueDate != null ? billret.DueDate.GetValue() : billret.TxnDate.GetValue(),
                TxnID = billret.TxnID.GetValue(),
                EditSequence = billret.EditSequence.GetValue(),
                ReferenceNum = billret.RefNumber != null ? billret.RefNumber.GetValue() : string.Empty,
                Memo = billret.Memo != null ? billret.Memo.GetValue() : string.Empty,
                QBTermsID = billret.TermsRef != null ? billret.TermsRef.ListID.GetValue() : string.Empty,
                QBEntityID = billret.VendorRef.ListID.GetValue(),
            };
            if ((billret.ORItemLineRetList != null) && (billret.ORItemLineRetList.Count > 0))
            {
                for (int i = 0; i <= billret.ORItemLineRetList.Count - 1; i++)
                {
                    IORItemLineRet line = billret.ORItemLineRetList.GetAt(i);
                    ItemEntry entry = new ItemEntry()
                    {
                        Price = line.ItemLineRet.Cost != null ? (decimal)line.ItemLineRet.Cost.GetValue() : (decimal)line.ItemLineRet.Amount.GetValue(),
                        Quantity = line.ItemLineRet.Quantity != null ? (decimal)line.ItemLineRet.Quantity.GetValue() : 1.0M,
                        TxnID = line.ItemLineRet.TxnLineID.GetValue(),
                        Description = line.ItemLineRet.Desc != null ? line.ItemLineRet.Desc.GetValue() : string.Empty,
                        ItemLookupCode = line.ItemLineRet.ItemRef != null ? line.ItemLineRet.ItemRef.FullName.GetValue() : string.Empty,
                    };
                    bill.Items.Add(entry);
                }
            }

            if ((billret.ExpenseLineRetList != null) && (billret.ExpenseLineRetList.Count > 0))
            {
                for (int i = 0; i <= billret.ExpenseLineRetList.Count - 1; i++)
                {
                    IExpenseLineRet line = billret.ExpenseLineRetList.GetAt(i);

                    ExpenseEntry entry = new ExpenseEntry()
                    {
                        Amount = line.Amount != null ? (decimal)line.Amount.GetValue() : 0.0M,
                        Memo = line.Memo != null ? line.Memo.GetValue() : string.Empty,
                        Description = line.AccountRef != null ? line.AccountRef.FullName.GetValue() : string.Empty,
                        QBAcctID = line.AccountRef != null ? line.AccountRef.ListID.GetValue() : string.Empty,
                        TxnID = line.TxnLineID.GetValue()
                    };
                    bill.Expenses.Add(entry);
                }
            }
            return bill;
        }

        public string GetAttachedDocumentName(string basePath)
        {
            string path = basePath + "800" + TxnID;
            if (!System.IO.Directory.Exists(path))
                return string.Empty;
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            FileInfo[] files = directoryInfo.GetFiles();
            string filename = string.Empty;
            FileInfo file = files.FirstOrDefault(x => x.Extension == ".pdf");
            if (file != null)
                filename = file.FullName;
            return filename;
        }
    }
}
