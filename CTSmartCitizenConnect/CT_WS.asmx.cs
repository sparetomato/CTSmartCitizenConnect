﻿using System;
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
            string dateOfBirth, string typeOfConcession, string disabilityPermanent, string evidenceExpiryDate, string passStartDate, string passImageString, string passPrintReason, string gender, string disabilityCategory, string UPRN, SmartCitizenConnector.Proof[] proofs, string homePhone, string mobilePhone, string emailAddress, string preferredContactMethod)
        {
            return CT_WSBL.getInstance().IssuePass(CPICC, firmstepCaseId, firstNameOrInitial, surname, houseOrFlatNameOrNumber,
                buildingName, street, villageOrDistrict, townCity, county, postcode, title, dateOfBirth, typeOfConcession, disabilityPermanent, evidenceExpiryDate, passStartDate, passImageString, passPrintReason, gender, disabilityCategory, UPRN, proofs, homePhone, mobilePhone, emailAddress, preferredContactMethod.ToLower());
        }
        /// <summary>
        /// Issues a new Concessionary Travel Pass.
        /// </summary>
        /// <param name="CPICC">CPICC code for the pass</param>
        /// <param name="northgateCaseID">Northgate Case Reference Number</param>
        /// <param name="title">Applicant Title</param>
        /// <param name="firstNameOrInitial">Applicant forename</param>
        /// <param name="surname">Applicant surname</param>
        /// <param name="houseOrFlatNameOrNumber">House Number</param>
        /// <param name="buildingName">Building Name</param>
        /// <param name="street">Street name</param>
        /// <param name="villageOrDistrict">Village Name</param>
        /// <param name="townCity">Town Name</param>
        /// <param name="county">County</param>
        /// <param name="postcode">Postcode</param>
        /// <param name="dateOfBirth">Date of Birth dd/mm/yyyy as a string</param>
        /// <param name="typeOfConcession">A - Age, D - Disabled</param>
        /// <param name="disabilityPermanant">YES or NO (only applicable for Disabled Passes)</param>
        /// <param name="evidenceExpiryDate">dd/mm/yyyy (only applicable for temporary disabled passes</param>
        /// <param name="passStartDate">dd/mm/yyyy - for passes to be issued in the future</param>
        /// <param name="passImageString">Photograph encoded as Base64.</param>
        /// <returns></returns>
        [WebMethod]
        public XmlDocument issuePass(string CPICC, string northgateCaseID, string title, string firstNameOrInitial, string surname, string houseOrFlatNameOrNumber,
            string buildingName, string street, string villageOrDistrict, string townCity, string county, string postcode,
            string dateOfBirth, string typeOfConcession, string disabilityPermanant, string evidenceExpiryDate, string passStartDate, string passImageString, string passPrintReason, string gender, string disabilityCategory, string UPRN, string addressProofId, string addressProofDate, string addressProofReference, string ageProofId, string ageProofDate, string ageProofReference, string disabilityProofId, string disabilityProofDate, string disabilityProofReference, string homePhone, string mobilePhone, string emailAddress, string preferredContactMethod)
        {
            if (log.IsWarnEnabled) log.Warn("Firmstep Case reference " + northgateCaseID + " is calling an obsolete method. Please update to use the new method.");
            SmartCitizenConnector.Proof addressProof = new SmartCitizenConnector.Proof(Convert.ToInt32(addressProofId), addressProofReference,null, DateTime.Parse(addressProofDate));
            SmartCitizenConnector.Proof ageProof = null;
             if(!String.IsNullOrEmpty(ageProofReference))
               ageProof = new SmartCitizenConnector.Proof(Convert.ToInt32(ageProofId),ageProofReference, DateTime.Parse(ageProofDate), DateTime.Now);
            SmartCitizenConnector.Proof disabilityProof = null;
            if(!String.IsNullOrEmpty(disabilityProofReference))
                disabilityProof = new SmartCitizenConnector.Proof(Convert.ToInt32(disabilityProofId), disabilityProofReference, DateTime.Parse(evidenceExpiryDate), DateTime.Parse(disabilityProofDate));
            return CT_WSBL.getInstance().IssuePass(CPICC, northgateCaseID, firstNameOrInitial, surname, houseOrFlatNameOrNumber,
                buildingName, street, villageOrDistrict, townCity, county, postcode, title, dateOfBirth, typeOfConcession, disabilityPermanant, evidenceExpiryDate, passStartDate, passImageString, passPrintReason, gender, disabilityCategory, UPRN, addressProof, ageProof, disabilityProof, homePhone, mobilePhone, emailAddress, preferredContactMethod.ToLower());
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
            string gender, string disabilityCategory, string UPRN, string homePhone, string mobilePhone, string emailAddress, string preferredContactMethod, string oldPassStatus)//, byte[] imageFile)
        {
            int? oldPassStatusInt = null;
            if (!String.IsNullOrEmpty(oldPassStatus))
                oldPassStatusInt = Convert.ToInt16(oldPassStatus);
            return CT_WSBL.getInstance().updatePassDetails(ISRN, CPICC, passHolderNumber, firstNameOrInitial, surname, houseOrFlatNameOrNumber,
                buildingName, street, villageOrDistrict, townCity, county, postcode, title, dateOfBirth, typeOfConcession, disabilityPermanent,
                evidenceExpiryDate, passStartDate, reissuePass, oldCPICC, recalculateExpiryDate, northgateCaseNumber, printReason, gender, disabilityCategory, UPRN,  homePhone,  mobilePhone,  emailAddress, preferredContactMethod.ToLower(),oldPassStatusInt);
        }

        [WebMethod]
        public XmlDocument UpdatePassStatus(string passHolderNumber, string ISRN, int CardStatusCode, int cardLocationCode, string AdditionalInformation, bool Replace)
        {
            throw new NotImplementedException();
        }


        [WebMethod]
        public XmlDocument UpdateImage(string passHolderNumber, string CPICC, string passImageString)
        {
            return CT_WSBL.getInstance().updatePassImage(CPICC, passHolderNumber, passImageString);
            
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
            string dateOfBirth, string gender, string disabilitycategory, string caseId)
        {

            return CT_WSBL.getInstance().UpdateAndRenewPass(cardHolderId, ISRN, title, forename, surname,
            dateOfBirth, gender, disabilitycategory, caseId);
            //if (log.IsInfoEnabled) log.Info("Updating and renewing pass details");
            //logParams(cardHolderId, ISRN, title, forename, surname, dateOfBirth, gender, disabilitycategory, caseId);
            //if (log.IsDebugEnabled) log.Debug("Checking pass number against the supplied pass number");
            //SmartCitizenCard currentSmartCitizenCard =
            //     getSmartCitizenCardForPerson(new RecordIdentifier() { CardholderID = cardHolderId });

            //if (currentSmartCitizenCard.ISRN != ISRN)
            //    throw new ScValidationException(ScValidationException.ScValidationReason.CardNotMatched);

            //if (!currentSmartCitizenCard.CanBeRenewed)
            //    throw new ScValidationException(
            //        ScValidationException.ScValidationReason.CardOutsideRenewalWindow);

            ///*if(!currentSmartCitizenCard.IsValid)
            //    throw new ScValidationException(
            //           ScValidationException.ScValidationReason.CardNotValid);*/

            //UpdatePassHolderDetails(cardHolderId, title, forename, surname, dateOfBirth, gender, disabilitycategory);
            //return ReplacePass(cardHolderId, ISRN, 17, caseId);
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

        //private SmartCitizenCard getSmartCitizenCardForPerson(RecordIdentifier personIdentifier)
        //{
        //    if (log.IsDebugEnabled) log.Debug("Getting Smart Citizen Card for Person");
        //    logParams(personIdentifier);
        //    SmartCitizenCard cardForPerson = new SmartCitizenCard();
        //    GetCardholderResponse cardHolderDetails = _cmClient.GetCardholder(new GetCardholderData() { CardholderIdentifier = personIdentifier });
        //    if (cardHolderDetails.Identifier.CardID != null)
        //    {
        //        CheckCardResponse cardCheckResponse =
        //            _cmClient.CheckCard(new CheckCardData() { CardIdentifier = cardHolderDetails.Identifier.CardID });
        //        cardForPerson.IsValid = cardCheckResponse.CardValid;
        //        cardForPerson.ISRN = cardHolderDetails.Identifier.CardID;
        //        DateTime expiryDate;
        //        DateTime.TryParse(
        //            cardHolderDetails.CitizenData.XPathSelectElement("Services/Service/Item[@name='EXPIRY DATE']")
        //                .Value, out expiryDate);
        //        cardForPerson.ExpiryDate = expiryDate;

        //    }
        //    if (log.IsDebugEnabled) log.Debug("Got Card.");
        //    return cardForPerson;
        //}

        #endregion



        #region Obsolete Methods

        //[WebMethod]
        //public XmlDocument addRecord(string CPICC, string northgateCaseID, string passHolderNumber, string title, string firstNameOrInitial, string surname, string houseOrFlatNameOrNumber,
        //    string buildingName, string street, string villageOrDistrict, string townCity, string county, string postcode,
        //    string dateOfBirth, string typeOfConcession, string disabilityPermanant, string evidenceExpiryDate, string passStartDate)//, byte[] imageFile)
        //{
        //    throw new System.NotImplementedException("Add Record method is obsolete. Please do not use.");
        //    //return CT_WSBL.getInstance().AddRecord(CPICC, northgateCaseID, passHolderNumber, firstNameOrInitial, surname, houseOrFlatNameOrNumber,
        //    //    buildingName, street, villageOrDistrict, townCity, county, postcode, title, dateOfBirth, typeOfConcession, disabilityPermanant, evidenceExpiryDate, passStartDate);//, imageFile);
        //}
        //[WebMethod]
        public bool GetPassesForPrint()
        {
            throw new NotImplementedException();
            //return CT_WSBL.getInstance().getPassesForPrint();
        }

        //[WebMethod]
        public bool GetPrintedPasses()
        {
            throw new NotImplementedException();
            //return CT_WSBL.getInstance().getPrintedPasses();
        }

        //[WebMethod]
        public bool ProcessESPReports()
        {
            throw new NotImplementedException();
            //return CT_WSBL.getInstance().processESPReports();
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
        public XmlDocument GetPassInformation(string CPICC, string PassHolderNumber, string RequestIssedSince)
        {
            if (log.IsErrorEnabled) log.Error("Attempt to call GetPassInformation.");
            throw new NotImplementedException();
            //return CT_WSBL.getInstance().GetPassInformation(CPICC, PassHolderNumber, RequestIssedSince);
        }



        //[WebMethod]
        public XmlDocument ReissuePass(string oldPassNumber, string CPICC, string passHolderNumber)
        {
            if (log.IsErrorEnabled) log.Error("Attempt to call ReissuePass method.");
            throw new NotImplementedException();
            //return CT_WSBL.getInstance().ReissuePass(CPICC, passHolderNumber, oldPassNumber);
        }


        //[WebMethod]
        public XmlDocument GetPassStatus(string CPICC, string passHolderNumber, string requestIssuedSince)
        {
            if (log.IsErrorEnabled) log.Error("Attempt to call GetPassStatus");
            throw new NotImplementedException();
            //return CT_WSBL.getInstance().queryPassStatus(CPICC, passHolderNumber, requestIssuedSince);
            //return CT_WSBL.getInstance().GetPassInformation(CPICC, passHolderNumber, requestIssuedSince);
        }

        //[WebMethod]
        public XmlDocument FlagPass(string CPICC, string PassHolderNumber, string FlagDescription)
        {
            return CT_WSBL.getInstance().flagPass(CPICC, PassHolderNumber, FlagDescription);
        }

        /// <summary>
        /// Used By WCC-CT Cancel Pass > BROKEN
        /// </summary>
        /// <param name="ISRN"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        //[WebMethod]
        public XmlDocument CancelPass(string ISRN, string delay)
        {
            return CT_WSBL.getInstance().cancelPass(ISRN, delay);
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
