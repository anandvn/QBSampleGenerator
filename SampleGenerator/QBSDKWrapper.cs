using QBFC15Lib;
using SampleGenerator.Model;
using SessionFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace SampleGenerator
{
    public class QBSDKWrapper : IDisposable
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly SessionManager sessionMgr;
        private string billIteratorID;
        private int iteratorRemainingCount = 0;
        public string AttachDir { get; private set; }
        public string CompanyFile { get; private set; }
        public int ItemCount { get; private set; }

        public QBSDKWrapper()
        {
            sessionMgr = SessionManager.getInstance();
            billIteratorID = string.Empty;
        }

        public Status Connect(string companyFile)
        {
            CompanyFile = companyFile;
            string companydir = Path.GetFileNameWithoutExtension(companyFile);
            string companyfilepath = Path.GetDirectoryName(companyFile);
            AttachDir = companyfilepath + @"\attach\" + companydir + @"\Txn\";
            // One Connect to rule them all
            string message = "Unknown Error Connecting to Quickbooks";
            ErrorCode code = ErrorCode.ConnectQBFailed;
            try
            {
                sessionMgr.beginSession(companyFile);
                message = "Connected to Quickbooks Successfully.";
                code = ErrorCode.ConnectQBOK;
            }
            catch (Exception except)
            {
                log.Info("Connect", except);
                if (except is COMException exception)
                {
                    code = ErrorCode.NoConnection;
                    switch (exception.ErrorCode)
                    {
                        case unchecked((int)0x8004040A):
                            message = exception.Message;
                            break;
                        case unchecked((int)0x80040410):
                            message = "Quickbooks is currently open in Single User Mode.  Either close the company file, or re-open it in multi-user mode.";
                            break;
                        case unchecked((int)0x80040414):
                            message = "There is a window open in Quickbooks preventing QBConnector from accessing it.  Please close the window or the company in Quickbooks.";
                            break;
                        case unchecked((int)0x8004041B):
                            message = exception.Message;
                            break;
                        case unchecked((int)0x80040422):
                            message = exception.Message;
                            break;
                        default:
                            message = exception.Message;
                            break;
                    }
                }
                else
                {
                    message = except.Message;
                    code = ErrorCode.NoConnection;
                }
            }
            return new Status(message, code, 0);
        }

        public async Task<Status> ConnectAsync(string companyFile)
        {
            return await Task.Run(() => Connect(companyFile));
        }

        public void Disconnect()
        {
            sessionMgr.endSession();
        }
        
        public async Task<ICollection<InventoryTransfer>> GetBillsAsync(DateTime start, int chunksize = 100)
        {
            if (iteratorRemainingCount < 0)
                return null;

            ICollection<InventoryTransfer> qbbilllist = new ObservableCollection<InventoryTransfer>();
            IMsgSetRequest requestSet = sessionMgr.getMsgSetRequest();
            bool firstiteration = (billIteratorID == string.Empty);
            // Get the RequestMsgSet based on the correct QB Version
            if (requestSet == null)
                throw new Exception("Unable to create the Session's message set request object");
            // Initialize the message set request object
            requestSet.Attributes.OnError = ENRqOnError.roeStop;

            // Add the request to the message set request object
            IBillQuery billquery = requestSet.AppendBillQueryRq();
            billquery.IncludeLineItems.SetValue(true);
            billquery.ORBillQuery.BillFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateFilter.FromTxnDate.SetValue(start);

            if (firstiteration)
            {
                billquery.ORBillQuery.BillFilter.MaxReturned.SetValue(chunksize);
                billquery.iterator.SetValue(ENiterator.itStart);
            }
            else
            {
                billquery.iteratorID.SetValue(billIteratorID);
                billquery.iterator.SetValue(ENiterator.itContinue);
                if (iteratorRemainingCount >= chunksize)
                    billquery.ORBillQuery.BillFilter.MaxReturned.SetValue(chunksize);
                else
                    billquery.ORBillQuery.BillFilter.MaxReturned.SetValue(iteratorRemainingCount);
            }

            bool result = await Task.Run(() =>
            {
                bool retval = true;
                try
                {
                    retval = sessionMgr.doRequests(ref requestSet);

                }
                catch (Exception except)
                {
                    log.Error("GetBillsAsync:doRequests", except);
                }
                return retval;
            });
            if (!result)
            {
                if (sessionMgr.getResponse(0) is IBillRetList retlist)
                {
                    billIteratorID = sessionMgr.getIteratorID();
                    iteratorRemainingCount = sessionMgr.getIteratorRemainingCount();

                    if (firstiteration)
                        ItemCount = iteratorRemainingCount + chunksize;
                    if (iteratorRemainingCount == 0)
                    {
                        billIteratorID = string.Empty;
                        iteratorRemainingCount = -1;
                        ItemCount = 0;
                    }
                    for (int i = 0; i < retlist.Count; i++)
                    {
                        IBillRet ret = retlist.GetAt(i);
                        InventoryTransfer itemreceipt = InventoryTransfer.Create(ret);
                        qbbilllist.Add(itemreceipt);
                    }
                }
            }
            return qbbilllist;
        }

        /// <summary>
        /// Load all active vendors from the Quickbooks Company file
        /// </summary>
        /// <returns>ObservableCollection of Names and ListIDs for each vendor</returns>
        public async Task<ObservableCollection<Business>> GetVendors()
        {
            ObservableCollection<Business> vendorList = new ObservableCollection<Business>();
            IMsgSetRequest requestSet = sessionMgr.getMsgSetRequest();
            requestSet.Attributes.OnError = ENRqOnError.roeStop;
            IVendorQuery vendorQuery = requestSet.AppendVendorQueryRq();
            vendorQuery.ORVendorListQuery.VendorListFilter.ActiveStatus.SetValue(ENActiveStatus.asActiveOnly);
            vendorQuery.IncludeRetElementList.Add("Name");
            vendorQuery.IncludeRetElementList.Add("ListID");

            bool result = await Task.Run(() =>
            {
                bool retval = true;
                try
                {
                    retval = sessionMgr.doRequests(ref requestSet);
                }
                catch (Exception except)
                {
                    log.Error("GetVendorListAsync:doRequests", except);
                }
                return retval;
            });
            if (!result)
            {
                IVendorRetList vendorretlist = sessionMgr.getResponse(0) as IVendorRetList;
                if (vendorretlist.Count != 0)
                {
                    for (int ndx = 0; ndx <= (vendorretlist.Count - 1); ndx++)
                    {
                        IVendorRet vendorRet = vendorretlist.GetAt(ndx);
                        if (vendorRet.ListID == null)
                            continue;

                        Business qbvendor = new Business()
                        {
                            Name = vendorRet.Name.GetValue(),
                            AccountNumber = vendorRet.AccountNumber.GetValue(),
                            CreditLimit = (decimal)vendorRet.CreditLimit.GetValue(),
                            Email = vendorRet.Email.GetValue(),
                            FirstName = vendorRet.FirstName.GetValue(),
                            LastName = vendorRet.LastName.GetValue(),
                            Phone = vendorRet.Phone.GetValue(),
                            TaxID = vendorRet.VendorTaxIdent.GetValue(),
                        };
                        vendorList.Add(qbvendor);
                    }
                }
            }
            return vendorList;
        }

        /// <summary>
        /// Get the list of all active terms.
        /// </summary>
        /// <returns>Descriptions and ListIDs for all active terms</returns>
        public async Task<ObservableCollection<NameListIDPair>> GetTermsListAsync()
        {
            ObservableCollection<NameListIDPair> retlist = new ObservableCollection<NameListIDPair>();
            IMsgSetRequest requestSet = sessionMgr.getMsgSetRequest();
            requestSet.Attributes.OnError = ENRqOnError.roeStop;
            ITermsQuery termsQuery = requestSet.AppendTermsQueryRq();
            termsQuery.ORListQuery.ListFilter.ActiveStatus.SetValue(ENActiveStatus.asActiveOnly);
            termsQuery.IncludeRetElementList.Add("Name");
            termsQuery.IncludeRetElementList.Add("ListID");
            bool result = await Task.Run(() =>
            {
                bool retval = true;
                try
                {
                    retval = sessionMgr.doRequests(ref requestSet);
                }
                catch (Exception except)
                {
                    log.Error("GetTermsList:doRequests", except);
                }
                return retval;
            });
            if (!result)
            {
                IORTermsRetList termsretlist = sessionMgr.getResponse(0) as IORTermsRetList;
                if ((termsretlist != null) && (termsretlist.Count != 0))
                {
                    for (int ndx = 0; ndx <= (termsretlist.Count - 1); ndx++)
                    {
                        IORTermsRet termsret = termsretlist.GetAt(ndx);
                        NameListIDPair terms = new NameListIDPair();
                        switch (termsret.ortype)
                        {
                            case ENORTermsRet.ortrDateDrivenTermsRet:
                                if (termsret.DateDrivenTermsRet.ListID != null)
                                {
                                    terms.ListID = termsret.DateDrivenTermsRet.ListID.GetValue();
                                    terms.Name = termsret.DateDrivenTermsRet.Name.GetValue();
                                }
                                break;
                            case ENORTermsRet.ortrStandardTermsRet:
                                if (termsret.StandardTermsRet.ListID != null)
                                {
                                    terms.ListID = termsret.StandardTermsRet.ListID.GetValue();
                                    terms.Name = termsret.StandardTermsRet.Name.GetValue();
                                }
                                break;
                            case ENORTermsRet.ortrNA:
                                break;
                        }
                        if (terms.ListID != "")
                            retlist.Add(terms);
                    }
                }
            }
            return retlist;
        }

        #region IDisposable Members
        // Flag: Has Dispose already been called?
        private bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any managed objects here.
                //
            }
            // Free any unmanaged resources here.
            Disconnect();
            disposed = true;
        }
        #endregion
    }
}
