﻿using QBFC15Lib;
using QBSDKWrapper;
using SampleGenerator.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Utilities;

namespace SampleGenerator
{
    public class QBSDKWrapper : QBSDKWrapperBase
    {
        private string billIteratorID;
        private int iteratorRemainingCount = 0;
        public int ItemCount { get; private set; }
        public QBSDKWrapper() : base(SampleGenerator.Properties.Settings.Default.AppId.ToString(), SampleGenerator.Properties.Settings.Default.AppName)
        {
            billIteratorID = string.Empty;
        }

        /// <summary>
        /// Async Method to load bills after a specified date.  Meant to be called until the list returned is null;
        /// </summary>
        /// <param name="start">Start Date for Bills</param>
        /// <param name="chunksize">How many bills to return after each call</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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


    }
}
