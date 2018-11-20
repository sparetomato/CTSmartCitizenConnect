using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CTSmartCitizenConnect
{
    public class SmartCitizenCard
    {
        private DateTime _expiryDate;
        internal string ISRN { get; set; }
        internal string ExpiryDateString { get { return ExpiryDate.ToShortDateString(); } }
        internal bool IsExpired
        {
            get
            {
                if (DateTime.Now > _expiryDate) return true;
                return false;
            }
        }
        internal bool IsValid { get; set; }

        internal DateTime ExpiryDate
        {
            get { return _expiryDate; }
            set { _expiryDate = value; }
        }

        internal bool CanBeRenewed
        {
            get
            {
                if (DateTime.Now.Date.AddYears(-1) > _expiryDate.Date)
                    return false;
                if (_expiryDate.Date > DateTime.Now.AddMonths(1).Date)
                    return false;

                return true;
            }
        }

    }
}