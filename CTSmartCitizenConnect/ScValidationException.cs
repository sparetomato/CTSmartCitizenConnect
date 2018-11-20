using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CTSmartCitizenConnect
{

    
    public class ScValidationException : Exception
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public enum ScValidationReason
        {
            CitizenDataNotFound,
            CardDataNotFound,
            CardNotMatched,
            MultiplePotentialMatches,
            CardNotExpired,
            CardNotValid,
            CardOutsideRenewalWindow
        };

        public ScValidationException(ScValidationReason reason)
        {
            if (log.IsErrorEnabled) log.Error("Validation Error:" + reason);
        }
        public ScValidationException(string message)
            : base(message)
        {
        }
    }
}