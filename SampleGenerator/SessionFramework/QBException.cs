// Copyright 2007 by Intuit Inc.
// All rights reserved
// Usage governed by the QuickBooks SDK Developer's License Agreement

using System;

namespace SessionFramework
{
    public class QBException : Exception
    {
        private QBException() { }

        public QBException(string sMsg)
            : base(sMsg)
        {
        }

        public override string ToString()
        {
            return base.Message;
        }

    }
}
