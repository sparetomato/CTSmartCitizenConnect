using System;
using System.ComponentModel;
using System.Web.Services;
using System.Xml;
using warwickshire.gov.uk.CT_WS;
using log4net;
using System.Web;
using warwickshire.ConcessionaryTravel.Classes;
using System.Xml.Serialization;
using System.IO;
using CTSmartCitizenConnect.SmartConnect;

namespace CTSmartCitizenConnect
{    
    
    /// <summary>
    /// Summary description for CT_WS
    /// </summary>
    [WebService(Namespace = "http://warwickshire.gov.uk/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]


    public class CT_WS : System.Web.Services.WebService
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string appDataPath = HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/";

        [WebMethod]
        public XmlDocument issueNewPass(string CPICC, string firmstepCaseId, string title, string firstNameOrInitial, string surname, string houseOrFlatNameOrNumber,
            string buildingName, string street, string villageOrDistrict, string townCity, string county, string postcode,
            string dateOfBirth, string typeOfConcession, string disabilityPermanent, string evidenceExpiryDate, string passStartDate, string passImageString, string passPrintReason, string gender, string disabilityCategory, string UPRN, SmartCitizenConnector.Proof[] proofs, string homePhone, string mobilePhone, string emailAddress, string preferredContactMethod, string NINO)
        {
            return CT_WSBL.getInstance().IssuePass(CPICC, firmstepCaseId, firstNameOrInitial, surname, houseOrFlatNameOrNumber,
                buildingName, street, villageOrDistrict, townCity, county, postcode, title, dateOfBirth, typeOfConcession, disabilityPermanent, evidenceExpiryDate, passStartDate, passImageString, passPrintReason, gender, disabilityCategory, UPRN, proofs, homePhone, mobilePhone, emailAddress, preferredContactMethod.ToLower(), NINO);
        }

 
        [WebMethod]
        public XmlDocument QueryPass(string forename, string Surname, string postcode, string passNo)
        {
            return CT_WSBL.getInstance().queryPass(forename, Surname, postcode,String.Empty, passNo);
        }


        [WebMethod]
        public XmlDocument QueryPassFromPassHolderNumber(string CPICC, string passHolderNumber)
        {
            return CT_WSBL.getInstance().queryPassFromPassholderNumber(CPICC, passHolderNumber);
        }



        /// <summary>
        /// Used By WCC-CT Change pass
        /// </summary>
        /// <param name="ISRN"></param>
        /// <param name="passHolderNumber"></param>
        /// <param name="CPICC"></param>
        /// <param name="title"></param>
        /// <param name="firstNameOrInitial"></param>
        /// <param name="surname"></param>
        /// <param name="houseOrFlatNameOrNumber"></param>
        /// <param name="buildingName"></param>
        /// <param name="street"></param>
        /// <param name="villageOrDistrict"></param>
        /// <param name="townCity"></param>
        /// <param name="county"></param>
        /// <param name="postcode"></param>
        /// <param name="dateOfBirth"></param>
        /// <param name="typeOfConcession"></param>
        /// <param name="disabilityPermanent"></param>
        /// <param name="evidenceExpiryDate"></param>
        /// <param name="passStartDate"></param>
        /// <param name="reissuePass"></param>
        /// <param name="oldCPICC"></param>
        /// <param name="recalculateExpiryDate"></param>
        /// <param name="northgateCaseNumber"></param>
        /// <param name="printReason"></param>
        /// <param name="gender"></param>
        /// <param name="disabilityCategory"></param>
        /// <param name="UPRN"></param>
        /// <returns></returns>
        [WebMethod]
        public XmlDocument UpdatePassDetails(string ISRN, string passHolderNumber, string CPICC, string title, string firstNameOrInitial, string surname, string houseOrFlatNameOrNumber,
            string buildingName, string street, string villageOrDistrict, string townCity, string county, string postcode,
            string dateOfBirth, string typeOfConcession, string disabilityPermanent, string evidenceExpiryDate, string passStartDate, bool reissuePass, string oldCPICC, bool recalculateExpiryDate, string northgateCaseNumber, string printReason,
            string passStatusNotes, string gender, string disabilityCategory, string UPRN, string homePhone, string mobilePhone, string emailAddress, string preferredContactMethod, string oldPassStatus, string NINO, string authoriser, SmartCitizenConnector.Proof[] proofs)
        {
            int? oldPassStatusInt = null;
            if (!String.IsNullOrEmpty(oldPassStatus))
                oldPassStatusInt = Convert.ToInt16(oldPassStatus);
            return CT_WSBL.getInstance().updatePassDetails(ISRN, CPICC, passHolderNumber, firstNameOrInitial, surname, houseOrFlatNameOrNumber,
                buildingName, street, villageOrDistrict, townCity, county, postcode, title, dateOfBirth, typeOfConcession, disabilityPermanent,
                evidenceExpiryDate, passStartDate, reissuePass, oldCPICC, recalculateExpiryDate, northgateCaseNumber, printReason, passStatusNotes, 
                gender, disabilityCategory, UPRN,  homePhone,  mobilePhone,  emailAddress, preferredContactMethod.ToLower(),NINO, authoriser,proofs, oldPassStatusInt);
        }

        [WebMethod]
        public XmlDocument UpdatePassStatus(string passHolderNumber, string ISRN, int CardStatusCode, int cardLocationCode, string AdditionalInformation, bool Replace)
        {
            throw new NotImplementedException();

        }

        [WebMethod]
        public bool TestUpdatePassStatus(string ISRN)
        {
            return CT_WSBL.getInstance().updateCardStatus(ISRN, 3);
        }


        [WebMethod]
        public XmlDocument UpdateImage(string passHolderNumber, string CPICC, string passImageString)
        {
            return CT_WSBL.getInstance().updatePassImage(CPICC, passHolderNumber, passImageString);
            
        }

        /// <summary>
        /// Cancels the pass on SmartCitizen.
        /// </summary>
        /// <param name="ISRN">ISRN of pass to cancel.</param>
        /// <param name="reasonDescription">Reason description code as per the SmartCitizen Status list</param>
        /// <returns></returns>
        [WebMethod]
        public XmlDocument CancelPass(string ISRN, string reasonDescription, string authoriser)
        {
            return CT_WSBL.getInstance().cancelPass(ISRN, reasonDescription, authoriser);
        }

        [WebMethod]
        public XmlDocument RecordTransaction(int? cardHolderId, string ISRN, CardTransactionData transactionData, string locationSCName)
        {
            if (String.IsNullOrEmpty(ISRN))
            {
                return CT_WSBL.getInstance().recordTransaction(cardHolderId.Value, transactionData,locationSCName);
            }
            else
            {
                return CT_WSBL.getInstance().recordTransaction(ISRN, transactionData,locationSCName);
            }
        }



        #region Methods copied from SmartCitizenConnect WS Endpoint

        /// <summary>
        /// Checks SmartCitizen to verify that a passholder exists and returns their details
        /// Used by WCC-CT Self Only Renew Pass Search (Live & Test)
        /// Used By WCC-CT Self TEST ONLY - Renew Pass Search
        /// </summary>
        /// <param name="surname"></param>
        /// <param name="forename"></param>
        /// <param name="dateOfBirth"></param>
        /// <param name="postcode"></param>
        /// <param name="ISRN"></param>
        /// <returns>Pass Holder Details</returns>
        [WebMethod(Description = "Verifies that a passholder exists on SmartCitizen for a renewal")]
        public XmlDocument CheckPassHolderData(string surname, string forename, string dateOfBirth, string postcode, string ISRN)
        {
            if (log.IsInfoEnabled) log.Info("Received request to search for cardholder data. ISRN:" + ISRN);
            logParams(surname, forename, dateOfBirth, postcode, ISRN);

            if (log.IsDebugEnabled) log.Debug("Trimming spaces");
            surname = surname.Trim();
            forename = forename.Trim();
            dateOfBirth = dateOfBirth.Trim();
            postcode = postcode.Trim();
            ISRN = ISRN.Trim();
            if (log.IsDebugEnabled) log.Debug("Spaces Trimmed.");

            XmlDocument responseDoc = new XmlDocument();
            responseDoc.Load(appDataPath + "CTSelfPassSearchResponse.xml");
            //Parse the DoB String
            DateTime parsedDob;

            if (log.IsDebugEnabled) log.Debug("Parsing Date:" + dateOfBirth);
            if (DateTime.TryParse(dateOfBirth, out parsedDob) == false)
                throw new ArgumentException("Date: " + dateOfBirth + " is not a valid Date.");
            //Validating a passholder on SC takes five steps if any of the first three fail, return a 'not found' response.:
            //1. Check Cardholder Exists
            try
            {
                if (log.IsDebugEnabled) log.Debug("Checking Cardholder Exists");
                SmartCitizenConnector dataLayer = new SmartCitizenConnector();

                CTPassHolder[] searchResults;
                searchResults = dataLayer.SearchPassHolders(surname, forename, dateOfBirth, postcode,
                    ISRN);

                if (searchResults.Length == 0)
                {
                    //Search for an initial, but all other values match...
                    searchResults = dataLayer.SearchPassHolders(surname, forename[0].ToString(), dateOfBirth, postcode,
                        ISRN);
                }

                //No match found for initial. Search for forename, without DoB
                if (searchResults.Length == 0)
                {
                    searchResults = dataLayer.SearchPassHolders(surname, forename, String.Empty, postcode, ISRN);
                }

                // No match found for forename, no DoB. Search for initial without DoB
                if (searchResults.Length == 0)
                {
                    searchResults = dataLayer.SearchPassHolders(surname, forename[0].ToString(), String.Empty, postcode, ISRN);
                }

                //If we still have no unique matches, return a not found response.
                if (searchResults.Length == 0)
                    return responseDoc;

                if (log.IsDebugEnabled)
                {
                    if (log.IsDebugEnabled) log.Debug(searchResults.Length + " results returned");
                    if (log.IsDebugEnabled) log.Debug("Output of results:");
                    foreach (CTPassHolder searchResult in searchResults)
                    {
                        try
                        {
                            if (log.IsDebugEnabled) log.Debug(SerializeObj(searchResult));
                        }
                        catch (Exception ex)
                        {

                            if (log.IsErrorEnabled) log.Error("Could not serialize object:" + ex.Message);
                        }

                    }
                }

                CTPassHolder passHolder = new CTPassHolder();
                for (int i = 0; i < searchResults.Length; i++)
                {
                    if (searchResults[i].CtPass.ISRN == ISRN)
                        passHolder = searchResults[i];
                    break;
                }


                if (passHolder.RecordID > 0)
                {
                    if (log.IsInfoEnabled) log.Info("Result found for ISRN:" + ISRN);
                    if (log.IsDebugEnabled) log.Debug("Passholder data and ISRN match. Returning Customer Data.");
                    if (log.IsDebugEnabled) log.Debug("Passholder details:" + SerializeObj(passHolder));
                    //4. Return pass holder data:

                    responseDoc.SelectSingleNode("result/recordId").InnerText = passHolder.RecordID.ToString();
                    responseDoc.SelectSingleNode("result/title").InnerText = passHolder.Title;
                    responseDoc.SelectSingleNode("result/foreName").InnerText = passHolder.FirstNameOrInitial;
                    responseDoc.SelectSingleNode("result/resultsFound").InnerText = "1";
                    //Reformat Dob to Firmstep pattern
                    if (passHolder.DateOfBirth != null)
                        responseDoc.SelectSingleNode("result/dob").InnerText = passHolder.DateOfBirth.Value.ToString("yyyy-MM-dd");

                    switch (passHolder.CtPass.PassType)
                    {
                        case CTPassType.Age:
                            responseDoc.SelectSingleNode("result/passType").InnerText = "Age";
                            break;
                        case CTPassType.Disabled:
                            responseDoc.SelectSingleNode("result/passType").InnerText = "Disabled";
                            responseDoc.SelectSingleNode("result/disabledPassType").InnerText = "Permanent";


                            break;
                        case CTPassType.DisabledTemporary:
                            responseDoc.SelectSingleNode("result/passType").InnerText = "Disabled";
                            responseDoc.SelectSingleNode("result/disabledPassType").InnerText = "Temporary";

                            break;
                    }

                    if (passHolder.DisabilityCategory != '\0')
                        responseDoc.SelectSingleNode("result/disabilityCategory").InnerText =
                                passHolder.DisabilityCategory.ToString();


                    responseDoc.SelectSingleNode("result/expiryDate").InnerText =
                        passHolder.CtPass.ExpiryDate.ToString("yyyy-MM-dd");

                    responseDoc.SelectSingleNode("result/gender").InnerText = passHolder.Gender;

                    if (passHolder.PhotoAssociated == 'Y')
                    {
                        responseDoc.SelectSingleNode("result/hasPhoto").InnerText = "true";
                        responseDoc.SelectSingleNode("result/passHolderPhotograph").InnerText =
                            Convert.ToBase64String(passHolder.PhotographBytes);
                    }
                    else responseDoc.SelectSingleNode("result/hasPhoto").InnerText = "false";


                    if (passHolder.PostCode != String.Empty)
                        responseDoc.SelectSingleNode("result/SCpostcode").InnerText = passHolder.PostCode;

                    responseDoc.SelectSingleNode("result/searchISRN").InnerText = ISRN;
                    responseDoc.SelectSingleNode("result/searchSurname").InnerText = surname;
                    responseDoc.SelectSingleNode("result/searchPostcode").InnerText = postcode;


                }
                else
                {
                    if (log.IsDebugEnabled) log.Debug("Supplied pass number does not match passholder's latest pass number.");
                    if (log.IsInfoEnabled) log.Info("No results found for ISRN:" + ISRN);
                }

            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error(ex.Message);
                return responseDoc;
            }

            if (log.IsDebugEnabled) log.Debug("Returning Pass Data:" + responseDoc.OuterXml);
            if (log.IsDebugEnabled) log.Debug("Exiting");
            return responseDoc;

        }

        /// <summary>
        /// Used by CT Self - Save Pass
        /// </summary>
        /// <param name="cardHolderId"></param>
        /// <param name="ISRN"></param>
        /// <param name="title"></param>
        /// <param name="forename"></param>
        /// <param name="surname"></param>
        /// <param name="dateOfBirth"></param>
        /// <param name="gender"></param>
        /// <param name="disabilitycategory"></param>
        /// <param name="caseId"></param>
        /// <returns></returns>
        [WebMethod(
            Description =
                "Updates and renews a pass on SmartCitizen. This will place the pass in the 'Authorise Passes' list")]
        public XmlDocument UpdateAndRenewPass(int cardHolderId, string ISRN, string title, string forename, string surname,
            string dateOfBirth, string gender, string disabilitycategory, string caseId, string NINO)
        {

            return CT_WSBL.getInstance().UpdateAndRenewPass(cardHolderId, ISRN, title, forename, surname,
            dateOfBirth, gender, disabilitycategory, caseId, NINO);
        }



        #endregion

        #region Private Methods

        private void logParams(params object[] parms)
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            if (log.IsDebugEnabled) log.Debug("Output of parameters supplied for:" + stackTrace.GetFrame(1).GetMethod().Name);
            for (int i = 0; i < parms.Length; i++)
            {
                if (log.IsDebugEnabled) log.Debug("Parameter [" + i + "]: Name:" + stackTrace.GetFrame(1).GetMethod().GetParameters()[i].Name + " Value:[" + parms[i] + "]");
            }
        }

        private string SerializeObj<T>(T obj)
        {
            try
            {


                XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());
                using (StringWriter txtWriter = new StringWriter())
                {
                    xmlSerializer.Serialize(txtWriter, obj);
                    return txtWriter.ToString();
                }
            }
            catch (Exception ex)
            {

                if (log.IsErrorEnabled) log.Error("Could not serialize object of type: " + obj.GetType());
                if (log.IsErrorEnabled) log.Error(ex.Message);

            }
            return "";
        }

        #endregion



        #region Obsolete Methods

        /// <summary>
        /// Method has been superceded by new method allowing an array of Proof types.
        /// </summary>
        [WebMethod]
        public XmlDocument issuePass(string CPICC, string northgateCaseID, string title, string firstNameOrInitial, string surname, string houseOrFlatNameOrNumber,
            string buildingName, string street, string villageOrDistrict, string townCity, string county, string postcode,
            string dateOfBirth, string typeOfConcession, string disabilityPermanant, string evidenceExpiryDate, string passStartDate, string passImageString, string passPrintReason, string gender, string disabilityCategory, string UPRN, string addressProofId, string addressProofDate, string addressProofReference, string ageProofId, string ageProofDate, string ageProofReference, string disabilityProofId, string disabilityProofDate, string disabilityProofReference, string homePhone, string mobilePhone, string emailAddress, string preferredContactMethod)
        {
            if (log.IsWarnEnabled) log.Warn("Firmstep Case reference " + northgateCaseID + " is calling an obsolete method. Please update to use the new method.");
            throw new NotImplementedException();
            //SmartCitizenConnector.Proof addressProof = new SmartCitizenConnector.Proof(Convert.ToInt32(addressProofId), addressProofReference,null, DateTime.Parse(addressProofDate));
            //SmartCitizenConnector.Proof ageProof = null;
            // if(!String.IsNullOrEmpty(ageProofReference))
            //   ageProof = new SmartCitizenConnector.Proof(Convert.ToInt32(ageProofId),ageProofReference, DateTime.Parse(ageProofDate), DateTime.Now);
            //SmartCitizenConnector.Proof disabilityProof = null;
            //if(!String.IsNullOrEmpty(disabilityProofReference))
            //    disabilityProof = new SmartCitizenConnector.Proof(Convert.ToInt32(disabilityProofId), disabilityProofReference, DateTime.Parse(evidenceExpiryDate), DateTime.Parse(disabilityProofDate));
            //return CT_WSBL.getInstance().IssuePass(CPICC, northgateCaseID, firstNameOrInitial, surname, houseOrFlatNameOrNumber,
            //    buildingName, street, villageOrDistrict, townCity, county, postcode, title, dateOfBirth, typeOfConcession, disabilityPermanant, evidenceExpiryDate, passStartDate, passImageString, passPrintReason, gender, disabilityCategory, UPRN, addressProof, ageProof, disabilityProof, homePhone, mobilePhone, emailAddress, preferredContactMethod.ToLower());
        }



        /// <summary>
        /// Searches passholder data for current and previous values
        /// </summary>
        /// <param name="PassholderId">Record ID (Unique)</param>
        /// <param name="Surname">Current Surname</param>
        /// <param name="DateOfBirth">Date Of Birth</param>
        /// <param name="CurrentUPRN">Current Unique Property Reference</param>
        /// <param name="CurrentAddress">Current Address</param>
        /// <param name="PriorUPRN">Prevuious UPRN</param>
        /// <param name="PriorAddress">Previous Address</param>
        /// <param name="PriorSurname">Previous Surname</param>
        /// <returns></returns>
        //[WebMethod]
        public XmlDocument SearchCurrentAndPreviousPassholderData(string PassholderId, string Surname, string DateOfBirth, string CurrentUPRN, string CurrentAddress, string PriorUPRN, string PriorAddress, string PriorSurname)
        {
            if (log.IsErrorEnabled) log.Error("Attempt to call SearchCurrentAndPreviousPassholderData");
            throw new NotImplementedException();
            /*
            int? iCurrentUPRN, iPriorUPRN;
            if(!string.IsNullOrEmpty(CurrentUPRN))s
                iCurrentUPRN = Convert.ToInt32(CurrentUPRN);
            else
                iCurrentUPRN = null;

            if(!string.IsNullOrEmpty(PriorUPRN))
                iPriorUPRN = Convert.ToInt32(PriorUPRN);
            else
                iPriorUPRN = null;


            return CT_WSBL.getInstance().queryPassWithCurrentOrPreviousData(PassholderId, Surname, DateOfBirth, iCurrentUPRN, CurrentAddress, iPriorUPRN, PriorAddress, PriorSurname);
        
             */
        }


        //[WebMethod]
        //public XmlDocument GetPassInformation(string CPICC, string PassHolderNumber, string RequestIssedSince)
        //{
        //    if (log.IsErrorEnabled) log.Error("Attempt to call GetPassInformation.");
        //    throw new NotImplementedException();
        //    //return CT_WSBL.getInstance().GetPassInformation(CPICC, PassHolderNumber, RequestIssedSince);
        //}



        //[WebMethod]
        //public XmlDocument ReissuePass(string oldPassNumber, string CPICC, string passHolderNumber)
        //{
        //    if (log.IsErrorEnabled) log.Error("Attempt to call ReissuePass method.");
        //    throw new NotImplementedException();
        //    //return CT_WSBL.getInstance().ReissuePass(CPICC, passHolderNumber, oldPassNumber);
        //}


        //[WebMethod]
        //public XmlDocument GetPassStatus(string CPICC, string passHolderNumber, string requestIssuedSince)
        //{
        //    if (log.IsErrorEnabled) log.Error("Attempt to call GetPassStatus");
        //    throw new NotImplementedException();
        //    //return CT_WSBL.getInstance().queryPassStatus(CPICC, passHolderNumber, requestIssuedSince);
        //    //return CT_WSBL.getInstance().GetPassInformation(CPICC, passHolderNumber, requestIssuedSince);
        //}

        //[WebMethod]
        public XmlDocument FlagPass(string CPICC, string PassHolderNumber, string FlagDescription)
        {
            return CT_WSBL.getInstance().flagPass(CPICC, PassHolderNumber, FlagDescription);
        }


        //[WebMethod]
        public bool GetProofs()
        {
            SmartCitizenConnector conn = new SmartCitizenConnector();
            conn.GetProofList();
            return true;
        }

        #endregion

    }
}
