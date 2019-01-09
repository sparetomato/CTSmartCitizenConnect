using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Web;
using System.Xml;
using CTBusinessProcessManager.Classes;
using CTSmartCitizenConnect;
using CTSmartCitizenConnect.SmartConnect;
using log4net;
using System.Text;
using warwickshire.ConcessionaryTravel.Classes;
using System.Xml.XPath;
using System.Xml.Linq;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using ExtensionMethods;

namespace warwickshire.gov.uk.CT_WS
{

    internal class CT_WSBL
    {
        private static CT_WSBL _instance;

        private Dictionary<int, string> ctPassIssueStatus = new Dictionary<int, string>();
        private Dictionary<int, string> ctErrors = new Dictionary<int, string>();
        private Dictionary<string, string> warwickshireCPICCs = new Dictionary<string, string>();
        private int _temporaryDisabledPassStandardEligibility, _agePassStandardEligibility, _temporaryDisabledPassMaximumEligibility;
        private List<string> spuriousDates;
        private readonly string appDataPath = HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/";


        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);





        public static CT_WSBL getInstance()
        {
            if (_instance == null)
                _instance = new CT_WSBL();

            return _instance;
        }

        private CT_WSBL()
        {
            initialiseInternalCollections();
        }

        private void initialiseInternalCollections()
        {
            ctPassIssueStatus.Add(0, "Successful");
            ctErrors.Add(0, "Invalid Pass Number");
            ctErrors.Add(1, "Pass number supplied is not a number");
            ctErrors.Add(2, "Pass number must be 18 characters long");
            ctErrors.Add(3, "Pass number not found in the database");
            ctErrors.Add(4, "Evidence exipry date is not a valid date");
            ctErrors.Add(5, "Evidence expires in less than 2 months. Pass cannot be issued");
            ctErrors.Add(6, "Pass type not recognised.");
            ctErrors.Add(7, "Pass start date is not a valid Date");
            ctErrors.Add(8, "Cancel pass date is not a valid Date");
            ctErrors.Add(9, "Date of birth is not a valid Date");
            ctErrors.Add(10, "Base64 String could not be converted to an image");
            ctErrors.Add(11, "Could not convert image string to a Byte Array");
            ctErrors.Add(12, "Could not flag record");
            ctErrors.Add(13, "CPICC not supplied");
            ctErrors.Add(14, "Passholder number not supplied");
            ctErrors.Add(15, "Passholder image not supplied");
            ctErrors.Add(16, "Could not invoke Asynchronous Process");
            ctErrors.Add(17, "Pass Record does not exist");
            ctErrors.Add(18, "CPICC supplied is not a valid Warwickshire CPICC");
            ctErrors.Add(19, "Pass type not present");
            ctErrors.Add(20, "Age related pass must have a date of birth supplied");
            ctErrors.Add(21, "Permanent disabled pass must have a date of birth supplied");
            ctErrors.Add(22, "No Print Reason Specified");
            ctErrors.Add(23, "At least one of ISRN, Surname or postcode must be supplied.");
            ctErrors.Add(24, "Error returned from SmartCitizen");
            ctErrors.Add(99, "Internal Application Error");
            ctErrors.Add(9999, "Demonstration of an Error");

         


        }



        #region Synchronous Processing

        /// <summary>
        /// Gets the last birthday for a person. If their birthday has happened this year than their last birthday is this year. 
        /// If their birthday has not happened this year, their last birthday will be last year
        /// </summary>
        /// <param name="dateOfBirth">Date of Birth</param>
        /// <returns>The date that they celebrated their last birthday</returns>
        private DateTime getLastBirthday(DateTime dateOfBirth)
        {
            // Get their birth date for this year
            // Firstly, if their birthday is 29th February, set their dob to 28th February
            DateTime thisYearBirthday;
            if (dateOfBirth.Day == 29 && dateOfBirth.Month == 2)
                thisYearBirthday = new DateTime(DateTime.Now.Year, dateOfBirth.Month, 28);
            else
                thisYearBirthday = new DateTime(DateTime.Now.Year, dateOfBirth.Month, dateOfBirth.Day);

            if (thisYearBirthday.CompareTo(DateTime.Now) <= 0)
                return thisYearBirthday;
            else
                return new DateTime(DateTime.Now.Year - 1, thisYearBirthday.Month, thisYearBirthday.Day); // Need to use 'ThisYearBirthday' as we have already accounted for the 29th Feb

        }

        internal XmlDocument UpdateAndRenewPass(int cardHolderId, string ISRN, string title, string forename, string surname,
            string dateOfBirth, string gender, string disabilitycategory, string caseId, string NINO)
        {
            if (log.IsInfoEnabled) log.Info("Updating and renewing pass details");
            logParams(cardHolderId, ISRN, title, forename, surname, dateOfBirth, gender, disabilitycategory, caseId, NINO);
            if (log.IsDebugEnabled) log.Debug("Checking pass number against the supplied pass number");
            SmartCitizenCard currentSmartCitizenCard =
                 getSmartCitizenCardForPerson(new RecordIdentifier() { CardholderID = cardHolderId });

            if (currentSmartCitizenCard.ISRN != ISRN)
                throw new ScValidationException(ScValidationException.ScValidationReason.CardNotMatched);

            if (!currentSmartCitizenCard.CanBeRenewed)
                throw new ScValidationException(
                    ScValidationException.ScValidationReason.CardOutsideRenewalWindow);

            /*if(!currentSmartCitizenCard.IsValid)
                throw new ScValidationException(
                       ScValidationException.ScValidationReason.CardNotValid);*/

            UpdatePassHolderDetails(cardHolderId, title, forename, surname, dateOfBirth, gender, disabilitycategory, NINO);
            return ReplacePass(cardHolderId, ISRN, 17, caseId);
        }

        private XmlDocument ReplacePass(int cardHolderId, string ISRN, int cardStatus, string caseNumber) //, string title, string forename, string dateOfBirth, string gender, string disabilityCategory, string caseId)
        {
            if (log.IsDebugEnabled) log.Debug("Entering");
            if (log.IsInfoEnabled) log.Info("Replacing pass for recordID:" + cardHolderId);
            logParams(cardHolderId, ISRN, cardStatus, caseNumber);//, title, forename, dateOfBirth, gender, disabilityCategory, caseId);


            // code below here for SmartCitizen connection.
            if (log.IsDebugEnabled) log.Debug("Loading Response XML file");
            XmlDocument responseDoc = new XmlDocument();
            try
            {
                responseDoc.Load(appDataPath + "CTSelfPassRenewalResponse.xml");
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Could not load Response XML file:" + appDataPath + "CTSelfPassRenewalResponse.xml");
                throw ex;
            }

            if (log.IsDebugEnabled) log.Debug("Response Document Loaded.");




            RecordIdentifier cardHolderRecordId = new RecordIdentifier() { CardholderID = cardHolderId, CardID = ISRN };
            //IssuerId is hard-coded to 2 - renew for this service as it is only for renewals.
            // CardLocation is hard-coded to 3: 'unknown' - not sure if this needs to be parameterised.
            UpdateCardData cardDataToUpdate = new UpdateCardData() { Identifier = cardHolderRecordId, CardLocation = 3, CardStatus = cardStatus, AdditionalInformation = "Renewal requested through CRM. Case Referece Number:" + caseNumber, ReplaceCard = true, IssuerId = 2 };

            RecordIdentifier responseIdentifier = null;
            try
            {
                if (log.IsDebugEnabled) log.Debug("Updating Card.");
                if (log.IsDebugEnabled) log.Debug(SerializeObj(cardDataToUpdate));
                SmartCitizenConnector _cmClient = new SmartCitizenConnector();
                responseIdentifier = _cmClient.UpdateCard(cardDataToUpdate);
                if (log.IsDebugEnabled) log.Debug(SerializeObj(responseIdentifier));
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Error:" + ex.Message);
                var requestStatusNode = responseDoc.SelectSingleNode("PassRenewal/RequestStatus");
                if (requestStatusNode != null)
                    requestStatusNode.InnerText = "Failure";
                return responseDoc;
            }

            if (responseIdentifier != null)
            {
                if (log.IsDebugEnabled) log.Debug("Pass Successfully renewed. Getting details of new card");
                SmartCitizenCard cardForPerson = getSmartCitizenCardForPerson(responseIdentifier);
                responseDoc.SelectSingleNode("PassRenewal/CardNumber").InnerText = cardForPerson.ISRN;
                responseDoc.SelectSingleNode("PassRenewal/ExpiryDate").InnerText = cardForPerson.ExpiryDateString;
                responseDoc.SelectSingleNode("PassRenewal/RequestStatus").InnerText = "Success";
            }
            else
            { responseDoc.SelectSingleNode("PassRenewal/RequestStatus").InnerText = "Failure"; }


            if (log.IsDebugEnabled) log.Debug("Exiting");
            return responseDoc;


        }

        internal XmlDocument UpdatePassHolderDetails(int cardHolderId, string title, string forename, string surname, string dateOfBirth, string gender, string disabilitycategory, string NINO)
        {
            if (log.IsInfoEnabled) log.Info("Updating Passholder Details for cardholder ID [" + cardHolderId + "]");
            logParams(cardHolderId, title, forename, surname, dateOfBirth, gender, disabilitycategory, NINO);
            XmlDocument responseDoc = new XmlDocument();
            try
            {
                responseDoc.Load(appDataPath + "CTSelfPassHolderUpdateResponse.xml");
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Could not load Response XML file:" + appDataPath + "CTSelfPassHolderUpdateResponse.xml");
                throw ex;
            }
            UpdateCardholderData cardholderData = new UpdateCardholderData();
            cardholderData.Identifier = new RecordIdentifier() { CardholderID = cardHolderId };
            // map the input fields to the Citizen XML fragment...
            XElement citizenDataXElement = LoadXmlFragment("WCCSelfUpdateCardholderFragment.xml");
            /*
             * Item IDs:
             * 68: Title
             * 3: Forename
             * 4: Surname
             * 5: Date of Birth (yyyy-mm-dd hh:mm:ss)
             * 7: Gender (1=M, 2=F, 0=Trans, 9=Unknown)
             * 21: Display Name
             */
            DateTime parsedDateTime = DateTime.Parse(dateOfBirth);
            DateTime testingTryParse;
            DateTime.TryParse(dateOfBirth, out testingTryParse);

            if (log.IsDebugEnabled) log.Debug("Populating update data and removing nodes that are unused.");

            foreach (XElement itemElement in citizenDataXElement.XPathSelectElements("/Services/Service[@name='CCDA']/Item").ToList())
            {
                switch (itemElement.Attribute("itemId").Value)
                {
                    case "68": //Title
                        if (String.IsNullOrEmpty(title))
                            itemElement.Remove();
                        else
                            itemElement.Value = title.ToTitleCase();
                          
                        break;
                    case "3": //Forename
                        if (String.IsNullOrEmpty(forename))
                            itemElement.Remove();
                        else
                            itemElement.Value = forename.ToTitleCase();
                        break;
                    case "4": //Surname
                        if (String.IsNullOrEmpty(surname))
                            itemElement.Remove();
                        else
                            itemElement.Value = surname.ToSurnameTitleCase();
                        break;
                    case "21": //Display Name
                        if (String.IsNullOrEmpty(forename) || String.IsNullOrEmpty(surname))
                            itemElement.Remove();
                        else
                            itemElement.Value = forename.ToTitleCase() + " " + surname.ToSurnameTitleCase();
                        break;
                    case "70": //Nino
                        if (String.IsNullOrEmpty(NINO))
                            itemElement.Remove();
                        else
                            itemElement.Value = NINO.ToUpper();
                        break;
                }
            }



            citizenDataXElement.XPathSelectElement("/Services/Service[@name='CCDA']/Item[@itemId='5']").Value
                = parsedDateTime.ToString("yyyy-MM-dd 00:00:00");

            int genderInt;
            switch (gender.ToUpper()[0])
            {
                case 'M':
                    genderInt = 1;
                    break;
                case 'F':
                    genderInt = 2;
                    break;
                default:
                    genderInt = 9;
                    break;
            }

            citizenDataXElement.XPathSelectElement("/Services/Service[@name='CCDA']/Item[@itemId='7']").Value
                = genderInt.ToString();
            cardholderData.CitizenData = citizenDataXElement;

            if (!String.IsNullOrEmpty(disabilitycategory))
            {
                XElement disabilityServiceXElement =
                    XElement.Parse(
                        "<Service name=\"\" serviceId=\"\"><Item itemId=\"\" dtype=\"lookup\"></Item></Service>");
                XElement disabilityLookupFragment = LoadXmlFragment("WCC_SC_DisabilityServiceXRef.xml");
                XElement selectedDisabilityElement = disabilityLookupFragment.XPathSelectElement("/DisabilityFragment[@disabilityCategory='" + disabilitycategory + "']");
                disabilityServiceXElement.SetAttributeValue("name", selectedDisabilityElement.XPathSelectElement("ServiceName").Value);
                disabilityServiceXElement.SetAttributeValue("serviceId", selectedDisabilityElement.XPathSelectElement("ServiceId").Value);
                disabilityServiceXElement.XPathSelectElement("Item").SetAttributeValue("itemId", selectedDisabilityElement.XPathSelectElement("ItemId").Value);
                disabilityServiceXElement.XPathSelectElement("Item").Value = "Renew";
                //selectedDisabilityElement.XPathSelectElement("PermanentLookupId").Value;
                citizenDataXElement.XPathSelectElement("/Services").Add(disabilityServiceXElement);
            }

            try
            {
                if (log.IsDebugEnabled) log.Debug("Update Pass Data Request:");
                if (log.IsDebugEnabled) log.Debug(SerializeObj(cardholderData));
                SmartCitizenConnector _cmClient = new SmartCitizenConnector();
                _cmClient.UpdateCardholder(cardholderData);
                if (log.IsInfoEnabled) log.Info("Passholder ID:" + cardHolderId + "updated.");
                responseDoc.SelectSingleNode("PassHolderUpdate/RequestStatus").InnerText = "Success";
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Error updating Card Holder Data for cardholder ID:" + cardHolderId + " - " + ex.Message);
                responseDoc.SelectSingleNode("PassHolderUpdate/RequestStatus").InnerText = "Failure";

            }

            if (log.IsInfoEnabled) log.Info("Update Complete");
            return responseDoc;
        }

        internal SmartCitizenCard getSmartCitizenCardForPerson(RecordIdentifier CardholderID)
        {

            if (log.IsDebugEnabled) log.Debug("Getting Smart Citizen Card for Person");
            logParams(CardholderID.CardholderID);
            SmartCitizenCard cardForPerson = new SmartCitizenCard();
            SmartCitizenConnector _cmClient = new SmartCitizenConnector();

            GetCardholderResponse cardHolderDetails = _cmClient.GetCardholder(CardholderID);
            if (cardHolderDetails.Identifier.CardID != null)
            {
                CheckCardResponse cardCheckResponse =
                    _cmClient.CheckCard(new CheckCardData() { CardIdentifier = cardHolderDetails.Identifier.CardID });
                cardForPerson.IsValid = cardCheckResponse.CardValid;
                cardForPerson.ISRN = cardHolderDetails.Identifier.CardID;
                DateTime expiryDate;
                DateTime.TryParse(
                    cardHolderDetails.CitizenData.XPathSelectElement("Services/Service/Item[@name='EXPIRY DATE']")
                        .Value, out expiryDate);
                cardForPerson.ExpiryDate = expiryDate;

            }
            if (log.IsDebugEnabled) log.Debug("Got Card.");
            return cardForPerson;
        }

        private DateTime calculateNewExpiryDate(CTPassType typeOfPass, string dateOfBirth, string evidenceExpiryDate)
        {
            DateTime expiryDate = DateTime.Now;
            DateTime evidenceExpiry = DateTime.Now;
            TimeSpan dateDiff;

            if (typeOfPass == CTPassType.Age)
            {
                if (dateOfBirth == null)
                    throw new CTDataException(20);
                expiryDate = getLastBirthday(Convert.ToDateTime(dateOfBirth)).AddYears(5);
            }

            if (typeOfPass == CTPassType.Disabled)
            {
                if (dateOfBirth == null)
                    throw new CTDataException(21);
                expiryDate = getLastBirthday(Convert.ToDateTime(dateOfBirth)).AddYears(3);
            }

            if (typeOfPass == CTPassType.DisabledTemporary)
            {
                // For temporary disabled passes we must have an evidence expiry date.
                if (!String.IsNullOrEmpty(evidenceExpiryDate))
                {
                    try
                    {
                        evidenceExpiry = Convert.ToDateTime(evidenceExpiryDate);
                    }
                    catch (FormatException ex)
                    {
                        if (log.IsErrorEnabled) log.Error("Could not convert:[" + evidenceExpiryDate + "] to a DateTime object");
                        if (log.IsDebugEnabled) log.Debug("Inner Exception:" + ex.Message);
                        throw new CTDataException(4);
                    }
                    // if the evidence expires in less than 4 months, we set the pass for
                    // 6 months. If it is more than 4 months, we set the pass for 2 months after the date
                    // the evidence expires.
                    dateDiff = evidenceExpiry.Subtract(DateTime.Now);

                    if (dateDiff.TotalDays < 60)
                    {
                        throw new CTDataException(5);
                    }

                    if (dateDiff.TotalDays < 120)
                        expiryDate = DateTime.Now.AddMonths(6);

                    if (dateDiff.TotalDays >= 120 && dateDiff.TotalDays <= 1035)
                        expiryDate = evidenceExpiry.AddMonths(2);


                    if (dateDiff.TotalDays > 1035)
                    {
                        expiryDate = DateTime.Now.AddYears(3);
                    }

                }
                else
                {
                    expiryDate = getLastBirthday(Convert.ToDateTime(dateOfBirth)).AddYears(3);
                }
            }
            return expiryDate;
        }



        #endregion


        #region Asynchronous properties & methods


        internal XmlDocument IssuePass(string CPICC, string FirmstepCaseId, string firstNameOrInitial, string surname, string houseOrFlatNumberOrName,
            string buildingName, string street, string villageOrDistrict, string townCity, string county, string postcode, string title,
            string dateOfBirth, string typeOfConcession, string disabilityPermanent, string evidenceExpiryDate, string passStartDate, string imageAsBase64String, string passPrintReason, string gender, string disabilityCategory, string UPRN, SmartCitizenConnector.Proof[] proofs, string homePhone, string mobilePhone, string emailAddress, string preferredContactMethod, string NINO)
        {
            if (log.IsInfoEnabled) log.Info("Request to issue pass received from Firmstep Case ID:" + FirmstepCaseId);
            logParams(CPICC, FirmstepCaseId, firstNameOrInitial, surname, houseOrFlatNumberOrName, buildingName, street, villageOrDistrict, townCity, county, postcode, title,
                dateOfBirth, typeOfConcession, disabilityPermanent, evidenceExpiryDate, passStartDate, imageAsBase64String, passPrintReason, gender, disabilityCategory, UPRN, homePhone, mobilePhone, emailAddress, preferredContactMethod, NINO);

            XmlDocument response = new XmlDocument();
            response.Load(HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/CTIssuePassResponse.xml");
            SmartCitizenConnector dataLayer = new SmartCitizenConnector();

            CTPassType typeOfPass = CTPassType.NotSet;
            if (log.IsDebugEnabled) log.Debug("Setting Type of Concession") ;
            if (!String.IsNullOrEmpty(typeOfConcession))
            {
                if (typeOfConcession.ToUpper() == "AGE")
                {
                    typeOfPass = CTPassType.Age;
                }

                else if (typeOfConcession.ToUpper() == "ELIGIBLE DISABLED")
                {
                    if (disabilityPermanent.ToUpper() == "YES")
                        typeOfPass = CTPassType.Disabled;
                    else
                        typeOfPass = CTPassType.DisabledTemporary;
                }
                else
                {
                    processError(ref response, 6);
                    return response;
                }
            }
            else
            {
                processError(ref response, 19);
            }
            if (log.IsDebugEnabled) log.Debug("Type of concession set. Calling Data Layer");
            CTPass newPass;
            try
            {
                newPass = dataLayer.IssuePass(title, firstNameOrInitial, surname, dateOfBirth, gender, emailAddress, homePhone, mobilePhone, buildingName, houseOrFlatNumberOrName, street, villageOrDistrict, townCity, county, UPRN, CPICC, postcode, imageAsBase64String, FirmstepCaseId, typeOfPass, disabilityCategory, proofs, preferredContactMethod,NINO);
            }
            catch(SmartCitizenException ex)
            {
                processError(ref response, 24, ex.Message);
                return response;
            }

            response.SelectSingleNode("issuePassResponse/status").InnerText = "SUCCESS";
            response.SelectSingleNode("issuePassResponse/statusMessage").InnerText = "New Pass issued with ISRN:" + newPass.ISRN;

            response.SelectSingleNode("issuePassResponse/passExpiryDate").InnerText =
                newPass.ExpiryDate.ToShortDateString();
            if (log.IsDebugEnabled) log.Debug("Returned XML:" + response.OuterXml);
            if (log.IsDebugEnabled) log.Debug("Exiting Method.");
            return response;
        }


        #endregion

        #region Private Methods

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

        private void initSpuriousDates()
        {
            if(spuriousDates == null)
            {
                spuriousDates = new List<string>();
                string[] spuriousDateArray = ConfigurationManager.AppSettings["spuriousDates"].Split(',');
                foreach(string date in spuriousDateArray)
                {
                    spuriousDates.Add(date);
                }    
            }
        }

        private bool isSpuriousDate(string dateToCheck)
        {
        if(spuriousDates == null || spuriousDates.Count == 0)
            initSpuriousDates();

        return spuriousDates.Contains(dateToCheck);
        }

        #endregion


        private string formatPostcode(string postcode)
        {
            postcode = postcode.ToUpper().Trim().Replace(" ", "").Replace("  ", "");
            if (postcode.Length == 6)
                postcode = postcode.Substring(0, 3) + " " + postcode.Substring(3);
            else if (postcode.Length == 7)
                postcode = postcode.Substring(0, 4) + " " + postcode.Substring(4);

            return postcode;
        }

        private void buildJobData(ref SmartCitizenCTPassholder existingPassHolder, string CPICC, string NGCaseID, string passHolderNumber, string firstNameOrInitial, string surname, string houseOrFlatNumberOrName,
            string buildingName, string street, string villageOrDistrict, string townCity, string county, string postcode, string title,
            string dateOfBirth, string typeOfConcession, string disabilityPermanent, string evidenceExpiryDate, byte[] imageAsBytes, string UPRN, string homePhone, string mobilePhone, string emailAddress, string preferredContactMethod, string NINO)
        {
            if (log.IsDebugEnabled) log.Debug("Updating Existing Passholder Details");

            bool updateAddress = false;
            if ((!String.IsNullOrEmpty(houseOrFlatNumberOrName) || !String.IsNullOrEmpty(buildingName)) && !String.IsNullOrEmpty(postcode)) updateAddress = true;


            // only update those fields provided.
            if(!String.IsNullOrEmpty(firstNameOrInitial)) existingPassHolder.FirstNameOrInitial = firstNameOrInitial.ToTitleCase();
            if(!String.IsNullOrEmpty(surname)) existingPassHolder.Surname = surname.ToTitleCase();
            if (updateAddress)
            {
                existingPassHolder.HouseOrFlatNumberOrName = houseOrFlatNumberOrName.ToTitleCase();
               existingPassHolder.BuildingName = buildingName.ToTitleCase();
                existingPassHolder.Street = street.ToTitleCase();
                existingPassHolder.VillageOrDistrict = villageOrDistrict.ToTitleCase();
                existingPassHolder.TownCity = townCity.ToTitleCase();
                existingPassHolder.County = county.ToTitleCase();
                existingPassHolder.PostCode = formatPostcode(postcode);
            }
            if(!String.IsNullOrEmpty(title)) existingPassHolder.Title = title.ToTitleCase();
            if(!String.IsNullOrEmpty(CPICC)) existingPassHolder.CPICC = CPICC;
            existingPassHolder.RecordID = Convert.ToInt32(passHolderNumber);
            if(!String.IsNullOrEmpty(UPRN)) existingPassHolder.UPRN = UPRN;
            existingPassHolder.HomePhone = homePhone;
            existingPassHolder.MobilePhone = mobilePhone;
            existingPassHolder.Email = emailAddress;
            if(!string.IsNullOrEmpty(preferredContactMethod))
            {
                existingPassHolder.PreferredContactMethod = preferredContactMethod.ToLower();
            }

            //NINO to be cleared if it isn't provided.
            if (String.IsNullOrEmpty(NINO)) existingPassHolder.NINO = String.Empty;
            else existingPassHolder.NINO = NINO;

            
            CTPassType typeOfPass = CTPassType.NotSet;
            if (!String.IsNullOrEmpty(typeOfConcession))
            {
                if (typeOfConcession.ToUpper() == "AGE")
                {
                    typeOfPass = CTPassType.Age;
                }

                else if (typeOfConcession.ToUpper() == "ELIGIBLE DISABLED")
                {
                    if (disabilityPermanent.ToUpper() == "YES")
                        typeOfPass = CTPassType.Disabled;
                    else
                        typeOfPass = CTPassType.DisabledTemporary;
                }
                else
                {
                    throw new CTDataException(6);
                }
            }
            else
            {
                throw new CTDataException(19);
            }

            if (typeOfPass != CTPassType.NotSet)
            {
                existingPassHolder.CtPass.PassType = typeOfPass;  
            }

            if (!String.IsNullOrEmpty(NGCaseID))
                existingPassHolder.CtPass.NorthgateCaseNumber = NGCaseID;

            if (!String.IsNullOrEmpty(dateOfBirth))
                existingPassHolder.DateOfBirth = Convert.ToDateTime(dateOfBirth);



            if (imageAsBytes != null && imageAsBytes.Length > 0)
                existingPassHolder.PhotographBytes = imageAsBytes;



            if (log.IsDebugEnabled) log.Debug("Existing Pass Data updated with new values");


        }

        internal XmlDocument updatePassImage(string CPICC, string passHolderNumber, string passImageString)
        {
            //if (log.IsDebugEnabled) log.Debug("Updating Pass Image for CPICC:" + CPICC + " - PassholderNumber " + passHolderNumber);
            //validateCPICC(CPICC);
            if (log.IsDebugEnabled) log.Debug("Updating pass image for passholder:" + passHolderNumber);

            if (log.IsDebugEnabled) log.Debug("Pass Image String:" + passImageString);
            XmlDocument response = new XmlDocument();
            response.Load(HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/CTUpdateImageResponse.xml");

            // verify that the pass holder exists in the database
            SmartCitizenConnector dataLayer = new SmartCitizenConnector();
            CTPassHolder existingPassHolderDetails = dataLayer.GetCtPassHolder(passHolderNumber);

            if(existingPassHolderDetails == null)
            {
                if (log.IsErrorEnabled) log.Error("Could not locate Pass Holder Number:[" + passHolderNumber + "] with CPICC:[" + CPICC + "]");
                processError(ref response, 17);
                return response;
            }

            byte[] passImageAsBytes;
            try
            {
                if (log.IsDebugEnabled) log.Debug("Attempting to convert to an image from a byte array");
                passImageAsBytes = Convert.FromBase64String(passImageString);
                ImageConverter ic = new ImageConverter();
                Image passImage = (Image)ic.ConvertFrom(passImageAsBytes);
                if (log.IsDebugEnabled) log.Debug("Converted OK.");
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Could not update pass image:" + ex.Message + "for CPICC:" + CPICC + " - PassHolderNumber: " + passHolderNumber);
                processError(ref response, 10);
                return response;
            }

            try
            {
                if (log.IsDebugEnabled) log.Debug("Calling Method on Data Layer");
                if (log.IsDebugEnabled) log.Debug("Pass Image as Bytes:" + Convert.ToBase64String(passImageAsBytes));
                dataLayer.UpdatePassImage(passHolderNumber, passImageString);
                //datalayer.UpdateImage(CPICC, passHolderNumber, passImageAsBytes);

            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Debug("Could not update image in database:" + ex.Message);
                if (log.IsErrorEnabled) log.Error("CPICC:" + CPICC + " Passholdernumber:" + passHolderNumber + "Pass Image:" + Convert.ToBase64String(passImageAsBytes));
                processError(ref response, 10);
                return response;
            }

            response.SelectSingleNode("updateImageResponse/status").InnerText = "success";
            return response;
        }

        internal XmlDocument flagPass(string CPICC, string passHolderNumber, string FlagDescription)
        {

            logParams(CPICC, passHolderNumber, FlagDescription);
            XmlDocument response = new XmlDocument();
            response.Load(HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/CTFlagRecordResponse.xml");

            //CT_DataLayer dataLayer = new CT_DataLayer();

            SmartCitizenConnector dataLayer = new SmartCitizenConnector();



            if (log.IsDebugEnabled) log.Debug("Flagging pass in Data Layer");
            try
            {
                dataLayer.FlagPassHolder(passHolderNumber, FlagDescription);
            }
            catch(Exception)
            {
                processError(ref response, 12);
                return response;
            }

                response.SelectSingleNode("//status").InnerText = "success";
                return response;
            
        }


        /*
        internal XmlDocument ReissuePass(string CPICC, string passHolderNumber, string oldPassNumber)
        {
            if (log.IsDebugEnabled) log.Debug("Reissue Pass request received");
            XmlDocument response = new XmlDocument();
            response.Load(HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/CTIssuePassResponse.xml");
            // trim any spaces
            if (!String.IsNullOrEmpty(oldPassNumber))
            {
                oldPassNumber = oldPassNumber.Replace(" ", "");
                try
                {
                    validateISRN(oldPassNumber);
                }
                catch (CTDataException ex)
                {
                    processError(ref response, ex.ErrorCode);
                }
            }
            CTData jobData = new CTData();
            jobData.ISRN = oldPassNumber;
            jobData.PassHolderNumber = passHolderNumber;
            jobData.CPICC = CPICC;

            jobData.PrintReason = "Replacement";

            //get the previous print reason.
            //CTDataV2_WS.CT_DataLayer dataLayer = new CTDataV2_WS.CT_DataLayer();
            //dataLayer.GetPassFromPrintQueue(CPICC, passHolderNumber, new DateTime(2011,4,1));



            try
            {
                startAsynchronousPassRequest(jobData, CTJob.CTJobType.ReissuePass);
                response.SelectSingleNode("issuePassResponse/status").InnerText = "success";
            }
            catch (CTDataException ex)
            {
                processError(ref response, ex.ErrorCode);
            }
            return response;
        }*/


        //02/06/2014 - added spurious date check. If any date is in our list of spurious dates (in web.config) we return an empty node. JC Requirement.
        //04/06/2014 - added UPRN and RecordID into return values.
        //04/06/2014 - Refactored code to re-use buildCustomerResponse to provide consistent responses from queries.
        internal XmlDocument queryPass(string forename, string surname, string postcode, string dateOfBirth, string passNo)
        {
            
            if (log.IsInfoEnabled) log.Info("Query Pass request received");
            logParams(forename, surname, postcode, passNo);
            XmlDocument response = new XmlDocument();
            response.Load(HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/CTQueryPassSummary.xml");

            if ((surname + postcode + passNo).Length == 0)
            {
                processError(ref response, 23);
                return response;
            }

            

            //CTDataV2_WS.CT_DataLayer dataLayer = new CTDataV2_WS.CT_DataLayer();
            SmartCitizenConnector dataLayer = new SmartCitizenConnector();
            if (String.IsNullOrEmpty(passNo))
            {
                //response.Load(HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/CTQueryPassSummary.xml");
                XmlNode customerTemplate = response.SelectSingleNode("//customer").CloneNode(true);
                response.DocumentElement.RemoveChild(response.SelectSingleNode("//customer"));
                SmartCitizenCTPassholderSummary[] summaryResults;
                try
                {
                    summaryResults = dataLayer.GetCTPassholderSummary(surname, forename, dateOfBirth, postcode);
                }
                catch(SmartCitizenException ex)
                {
                    processError(ref response, 24,  ex.Message);
                    return response;
                }
                catch(Exception ex)
                {
                    processError(ref response, 99, ex.Message);
                    return response;
                }

                //SmartCitizenCTPassholder[] searchResults = dataLayer.SearchPassHolders(surname, forename, dateOfBirth, postcode, passNo);
                //CTPassHolder[] searchResults = dataLayer.SearchPassHolders(surname, forename, dateOfBirth, postcode, passNo);

                if (log.IsInfoEnabled) log.Info("Processing Search Result(s)");
                foreach (SmartCitizenCTPassholderSummary searchResult in summaryResults)
                {

                    XmlNode customer = customerTemplate.CloneNode(true);
                    buildCustomerSummaryResponse(ref customer, searchResult);
                    response.DocumentElement.AppendChild(customer);
                }

                // Firmstep 19-03-2014. Added an Empty Customer node if searching by surname and postcode CS Request
                if (String.IsNullOrEmpty(passNo))
                {
                    XmlNode blankCustomerNode = customerTemplate.CloneNode(true);
                    blankCustomerNode.SelectSingleNode("//PassHolderNumber").InnerText = "No Match Found";
                    response.DocumentElement.AppendChild(blankCustomerNode);
                    response.SelectSingleNode("//statusMessage").InnerText = "Search complete, but no match found in SmartCitizen.";
                }

            }
            else
            {
                //response.Load(HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/CTQueryPassResponse.xml");
                XmlNode customerTemplate = response.SelectSingleNode("//customer").CloneNode(true);
                response.DocumentElement.RemoveChild(response.SelectSingleNode("//customer"));
                SmartCitizenCTPassholder[] searchResults;
                try
                {
                 searchResults = dataLayer.SearchPassHolders(surname, forename, dateOfBirth, postcode, passNo);
                }
                catch (SmartCitizenException ex)
                {
                    processError(ref response, 24, ex.Message);
                    return response;
                }
                if (log.IsInfoEnabled) log.Info("Processing Search Result(s)");
                foreach (SmartCitizenCTPassholder searchResult in searchResults)
                {

                    XmlNode customer = customerTemplate.CloneNode(true);
                    buildCustomerSearchResponse(ref customer, searchResult);
                    response.DocumentElement.AppendChild(customer);
                }
            }


            if (log.IsDebugEnabled) log.Debug("Response:" + response.OuterXml);
            if (log.IsInfoEnabled) log.Info("Returning Search Response.");
            //response.LoadXml(dataLayer.SearchData(surname, postcode, passNo, false).OuterXml);
            return response;
        }

        internal XmlDocument queryPassFromPassholderNumber(string CPICC, string passHolderNumber)
        {
            logParams(CPICC, passHolderNumber);
            XmlDocument response = new XmlDocument();
            if (log.IsDebugEnabled) log.Debug("Loading response XML");
            response.Load(HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/CTQueryPassResponse.xml");
            if(log.IsDebugEnabled) log.Debug("Response XML Loaded.");

            if(log.IsDebugEnabled) log.Debug("Cloning the Customer node.");
            XmlNode customer = response.SelectSingleNode("//customer").CloneNode(true);
            response.DocumentElement.RemoveAll();
            if(log.IsDebugEnabled) log.Debug("Customer node cloned and document cleared.");

            //CTDataV2_WS.CT_DataLayer dataLayer = new CTDataV2_WS.CT_DataLayer();
            SmartCitizenConnector dataLayer = new SmartCitizenConnector();
            if(log.IsDebugEnabled) log.Debug("Calling Data Layer.");
            SmartCitizenCTPassholder searchResult;
            try
            {
                searchResult = dataLayer.GetCtPassHolder(passHolderNumber);
            }
            catch (SmartCitizenException ex)
            {
                processError(ref response, 24, ex.Message);
                return response;
            }
            if (searchResult == null)
            {

                processError(ref response, 3);
                return response;
            }
            if(log.IsDebugEnabled)log.Debug("Data Layer successfully called and a result returned.");

            buildCustomerSearchResponse(ref customer, searchResult);
            response.DocumentElement.AppendChild(customer);

            return response;
        }


        private void buildCustomerSearchResponse(ref XmlNode customerNode, SmartCitizenCTPassholder passHolder)
        {
                // match up the values we can automatically.
            foreach (var prop in passHolder.GetType().GetProperties())
            {
                try
                {
                    if (log.IsDebugEnabled) log.Debug("Property Name:" + prop.Name);
                    if (log.IsDebugEnabled) log.Debug("Property Type:" + prop.PropertyType);
                    //XmlElement element; 
                    if (prop.PropertyType == typeof (SmartCitizenCTPass))
                    {

                        foreach (var subprop in passHolder.CtPass.GetType().GetProperties())
                        {
                            if (log.IsDebugEnabled) log.Debug("Sub Property Name:" + subprop.Name);
                            if (log.IsDebugEnabled) log.Debug("Sub Property Type:" + subprop.PropertyType);
                            if (subprop.GetValue(passHolder.CtPass, null) != null)
                            {
                                if (log.IsDebugEnabled) log.Debug("Sub Property Value:" + subprop.GetValue(passHolder.CtPass, null).ToString());
                            }
                            if (customerNode.SelectSingleNode("//" + subprop.Name) != null)
                            {
                                if (subprop.GetValue(passHolder.CtPass, null) != null)
                                {
                                    if (subprop.PropertyType == typeof (DateTime))
                                    {
                                        string dateString =
                                            ((DateTime) subprop.GetValue(passHolder.CtPass, null)).ToShortDateString();
                                        if (!isSpuriousDate(dateString))
                                            customerNode.SelectSingleNode("//" + subprop.Name).InnerText = dateString;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            customerNode.SelectSingleNode("//" + subprop.Name).InnerText =
                                                subprop.GetValue(passHolder.CtPass, null).ToString();
                                        }
                                        catch (NullReferenceException ex)
                                        {
                                            if (log.IsErrorEnabled) log.Error("Could not find node with name:" + subprop.Name + "Please check XML file.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        //if (log.IsDebugEnabled) log.Debug("Property Value:" + prop.GetValue(passHolder, null).ToString());
                        if (customerNode.SelectSingleNode("//" + prop.Name) != null)
                        {
                            if (prop.GetValue(passHolder, null) != null)
                            {
                                if (prop.PropertyType == typeof (byte[]))
                                {
                                    customerNode.SelectSingleNode("//" + prop.Name).InnerText =
                                        convertImage((byte[]) prop.GetValue(passHolder, null));
                                }
                                else if (prop.PropertyType == typeof (DateTime))
                                {
                                    string dateString = ((DateTime) prop.GetValue(passHolder, null)).ToShortDateString();
                                    if (!isSpuriousDate(dateString))
                                        customerNode.SelectSingleNode("//" + prop.Name).InnerText =
                                            ((DateTime) prop.GetValue(passHolder, null)).ToShortDateString();
                                }
                                else if (prop.PropertyType == typeof (System.Nullable<DateTime>))
                                {
                                    //if (log.IsDebugEnabled) log.Debug("Property is Nullable DateTime");
                                    if (prop.GetValue(passHolder, null) != null)
                                    {
                                        string dateString =
                                            ((DateTime) prop.GetValue(passHolder, null)).ToShortDateString();
                                        if (!isSpuriousDate(dateString))
                                        {
                                            customerNode.SelectSingleNode("//" + prop.Name).InnerText = dateString;
                                        }
                                    }
                                }
                                else if (prop.PropertyType == typeof (char))
                                {
                                    if (Convert.ToChar(prop.GetValue(passHolder, null)) != '\0')
                                        customerNode.SelectSingleNode("//" + prop.Name).InnerText =
                                            Convert.ToString(prop.GetValue(passHolder, null));
                                }
                                else
                                {
                                    customerNode.SelectSingleNode("//" + prop.Name).InnerText =
                                        prop.GetValue(passHolder, null).ToString();
                                }
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    if (log.IsErrorEnabled) log.Error(e.Message);

                }

                // modify the nodes we need for the customer.
                if (customerNode.SelectSingleNode("//Deleted") != null)
                {
                    if (customerNode.SelectSingleNode("//Deleted").InnerText.ToLower() == "true")
                        customerNode.SelectSingleNode("//Deleted").InnerText = "Y";
                    else
                        customerNode.SelectSingleNode("//Deleted").InnerText = "N";
                }

                if (customerNode.SelectSingleNode("//Photograph").InnerText.Length > 10)
                    customerNode.SelectSingleNode("//PhotoAssociated").InnerText = "Y";

                customerNode.SelectSingleNode("//DaysToExpiry").InnerText.PadLeft(4, '0');
                //customer.SelectSingleNode("//DaysSincePhotoUpdated").InnerText.PadLeft(5, '0');
                customerNode.SelectSingleNode("//RemainingTime").InnerText =
                    calculateTimeLeftString(passHolder.CtPass.DaysToExpiry);


                switch (passHolder.CtPass.PassType)
                {
                    case CTPassType.Age:
                        customerNode.SelectSingleNode("//TypeOfConcession").InnerText = "A";
                        customerNode.SelectSingleNode("//TypeOfConcessionLong").InnerText = "Age";
                        break;
                    case CTPassType.Disabled:
                        customerNode.SelectSingleNode("//TypeOfConcession").InnerText = "D";
                        customerNode.SelectSingleNode("//TypeOfConcessionLong").InnerText = "Eligible Disabled";
                        customerNode.SelectSingleNode("//DisabilityPermanent").InnerText = "Yes";
                        customerNode.SelectSingleNode("//DisabilityType").InnerText = "Permanent";
                        break;
                    case CTPassType.DisabledTemporary:
                        customerNode.SelectSingleNode("//TypeOfConcession").InnerText = "D";
                        customerNode.SelectSingleNode("//TypeOfConcessionLong").InnerText = "Eligible Disabled";
                        customerNode.SelectSingleNode("//DisabilityPermanent").InnerText = "No";
                        customerNode.SelectSingleNode("//DisabilityType").InnerText = "Temporary";
                        break;
                }

                
            }

            if (passHolder.PhotographBytes != null)
            {

                if (log.IsDebugEnabled) log.Debug("Photo associated. Attaching.");
                if (log.IsDebugEnabled) log.Debug("Photo Length:" + passHolder.PhotographBytes.Length);
                customerNode.SelectSingleNode("//Photograph").InnerText =
                    Convert.ToBase64String(passHolder.PhotographBytes);
            }
            else
            {
                if (log.IsDebugEnabled) log.Debug("No Photograph associated with this record.");
            }



            customerNode.SelectSingleNode("//FormattedExpiryDate").InnerText =
                passHolder.CtPass.ExpiryDate.ToString("s");

            if (passHolder.DateOfBirth.HasValue && !isSpuriousDate(passHolder.DateOfBirth.Value.ToShortDateString()))
            {
                customerNode.SelectSingleNode("//FormattedDateOfBirth").InnerText =
                    passHolder.DateOfBirth.Value.ToString("s");
            }


        }

        private void buildCustomerSummaryResponse(ref XmlNode customerNode, SmartCitizenCTPassholderSummary passHolder)
        {
            // match up the values we can automatically.
            foreach (var prop in passHolder.GetType().GetProperties())
            {
                try
                {
                    if (log.IsDebugEnabled) log.Debug("Property Name:" + prop.Name);
                    if (log.IsDebugEnabled) log.Debug("Property Type:" + prop.PropertyType);
                    //XmlElement element; 
                    //if (log.IsDebugEnabled) log.Debug("Property Value:" + prop.GetValue(passHolder, null).ToString());
                    if (customerNode.SelectSingleNode("//" + prop.Name) != null)
                    {
                        if (prop.GetValue(passHolder, null) != null)
                        {
                            if (prop.PropertyType == typeof(DateTime))
                            {
                                string dateString = ((DateTime)prop.GetValue(passHolder, null)).ToShortDateString();
                                if (!isSpuriousDate(dateString))
                                    customerNode.SelectSingleNode("//" + prop.Name).InnerText =
                                        ((DateTime)prop.GetValue(passHolder, null)).ToShortDateString();
                            }
                            else if (prop.PropertyType == typeof(System.Nullable<DateTime>))
                            {
                                //if (log.IsDebugEnabled) log.Debug("Property is Nullable DateTime");
                                if (prop.GetValue(passHolder, null) != null)
                                {
                                    string dateString =
                                        ((DateTime)prop.GetValue(passHolder, null)).ToShortDateString();
                                    if (!isSpuriousDate(dateString))
                                    {
                                        customerNode.SelectSingleNode("//" + prop.Name).InnerText = dateString;
                                    }
                                }
                            }
                            else if (prop.PropertyType == typeof(char))
                            {
                                if (Convert.ToChar(prop.GetValue(passHolder, null)) != '\0')
                                    customerNode.SelectSingleNode("//" + prop.Name).InnerText =
                                        Convert.ToString(prop.GetValue(passHolder, null));
                            }
                            else
                            {
                                customerNode.SelectSingleNode("//" + prop.Name).InnerText =
                                    prop.GetValue(passHolder, null).ToString();
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    if (log.IsErrorEnabled) log.Error(e.Message);

                }
            }
        }



        internal XmlDocument updatePassDetails(string ISRN, string CPICC, string passHolderNumber, string firstNameOrInitial, string surname, string houseOrFlatNumberOrName,
            string buildingName, string street, string villageOrDistrict, string townCity, string county, string postcode, string title,
            string dateOfBirth, string typeOfConcession, string disabilityPermanent, string evidenceExpiryDate, string passStartDate, bool reissuePass, string oldCPICC, bool recalculateExpiryDate, string achieveServiceCaseNumber,
            string printReason, string gender, string disabilityCategory, string UPRN, string homePhone, string mobilePhone, string emailAddress, string preferredContactMethod, string NINO, int? oldPassStatus)
        {
            if (log.IsDebugEnabled) log.Debug("Update Pass Request Received");

            logParams(ISRN, CPICC, passHolderNumber, firstNameOrInitial, surname, houseOrFlatNumberOrName,
             buildingName, street, villageOrDistrict, townCity, county, postcode, title,
             dateOfBirth, typeOfConcession, disabilityPermanent, evidenceExpiryDate, passStartDate, reissuePass.ToString(), oldCPICC, recalculateExpiryDate.ToString(), achieveServiceCaseNumber, printReason, gender, disabilityCategory, homePhone, mobilePhone, emailAddress, preferredContactMethod, NINO);

            if (log.IsDebugEnabled) log.Debug("Loading Response XML Document");
            XmlDocument response = new XmlDocument();
            response.Load(HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/CTUpdatePassResponse.xml");
            if (log.IsDebugEnabled) log.Debug("Response XML loaded.");

            if (ISRN != String.Empty)
            {
                if (!validateISRN(ISRN))
                {
                    processError(ref response, 0);
                    return response;
                }
            }

            if (log.IsDebugEnabled) log.Debug("Getting Existing Passholder data from SmartCitizen.");
            SmartCitizenConnector dataLayer = new SmartCitizenConnector();
            SmartCitizenCTPassholder existingPassHolder;
            try
            {
                existingPassHolder = dataLayer.GetCtPassHolder(passHolderNumber);
            }
            catch (SmartCitizenException ex)
            {
                processError(ref response, 24, ex.Message);
                return response;
            }
            //CTData jobData = null;
            try
            {
                if (log.IsDebugEnabled) log.Debug("Building Job Data.");
                //CTData existingData = new CTData(queryPassFromPassholderNumber(oldCPICC, passHolderNumber));

                if (String.IsNullOrEmpty(passStartDate)) passStartDate = DateTime.Now.ToShortDateString();
                buildJobData(ref existingPassHolder, CPICC, null, passHolderNumber, firstNameOrInitial, surname, houseOrFlatNumberOrName,
                    buildingName, street, villageOrDistrict, townCity, county, postcode, title, dateOfBirth, typeOfConcession,
                    disabilityPermanent, evidenceExpiryDate, null, UPRN, homePhone, mobilePhone, emailAddress, preferredContactMethod, NINO);


                if (!String.IsNullOrEmpty(gender))
                    existingPassHolder.Gender = gender;

                if ((existingPassHolder.CtPass.PassType == CTPassType.Disabled || existingPassHolder.CtPass.PassType == CTPassType.DisabledTemporary)
                    && !String.IsNullOrEmpty(disabilityCategory))
                    existingPassHolder.DisabilityCategory = disabilityCategory[0];
                try
                {
                    existingPassHolder.CtPass.NorthgateCaseNumber = achieveServiceCaseNumber;
                }
                catch (System.FormatException)
                {
                    if (log.IsDebugEnabled) log.Debug("Could not set AchieveService Case Number to:[" + achieveServiceCaseNumber + "]. Setting to -1");
                    existingPassHolder.CtPass.NorthgateCaseNumber = "-1";
                }



            }

            catch (CTDataException ex)
            {
                if (log.IsErrorEnabled) log.Error(ex.Message);
                processError(ref response, 99, ex.Message);
                return response;
            }

            // If we are renewing, set OldPassStatus to "Expired"
            if (printReason.ToLower() == "renew")
            {
                oldPassStatus = 17;
            }

            SmartCitizenCTPassholder updatedPassHolder = dataLayer.UpdatePassHolderDetails(existingPassHolder);
            if (reissuePass)
            { 
                if(printReason.ToLower().Contains("renewal"))
                { // Set the existing pass to an "Expired" configuration
                    oldPassStatus = 17;
                }

            dataLayer.ReplacePass(updatedPassHolder.RecordID, updatedPassHolder.CtPass.ISRN, oldPassStatus != null ? oldPassStatus.Value : Convert.ToInt16(existingPassHolder.CtPass.PassStatusID),
                achieveServiceCaseNumber);

            }

            if (log.IsDebugEnabled) log.Debug("Asynchronous process started, returning successful response.");
            response.SelectSingleNode("updatePassResponse/status").InnerText = "success";

            response.SelectSingleNode("updatePassResponse/passExpiryDate").InnerText = updatedPassHolder.CtPass.ExpiryDate.ToShortDateString();
            response.SelectSingleNode("updatePassResponse/reissue").InnerText = reissuePass.ToString();
            if (log.IsDebugEnabled) log.Debug("Update Pass Request Complete");
            return response;
        }

        private bool validateISRN(string ISRN)
        {
            try
            {
                Convert.ToInt64(ISRN);
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Could not convert ISRN to a number:[" + ISRN + "]");
                if (log.IsDebugEnabled) log.Debug("Inner Exception:" + ex.Message);
                return false;
            }

            if (ISRN.Length != 18)
                return false;



            return true;
        }

        /// <summary>
        /// Checks the supplied CPICC code against the list of valid Warwickshire CPICCs
        /// </summary>
        /// <param name="CPICC">5 digit CPICC code</param>
        /// <returns>true if CPICC is  </returns>
        /// <exception>CTDataException</exception>
        private bool validateCPICC(string CPICC)
        {
            if (log.IsDebugEnabled) log.Debug("Validating CPICC:" + CPICC);
            if (String.IsNullOrEmpty(CPICC))
                throw new CTDataException(13);
            if (warwickshireCPICCs.ContainsKey(CPICC))
                return true;
            else
                throw new CTDataException(18);
        }

        internal XmlDocument cancelPass(string ISRN, string reason)
        {
            XmlDocument response = new XmlDocument();
            response.Load(HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/CTCancelPassResponse.xml");
            SmartCitizenConnector conn = new SmartCitizenConnector();
            bool successfullyCancelled;
            try
            {
                SmartCitizenCTPassholder ctPassholder = conn.GetCTPassholderForPass(ISRN);
                successfullyCancelled =  conn.cancelPassforPassHolder(ctPassholder, reason);
            }
            catch (SmartCitizenException ex)
            {
                processError(ref response, 24, ex.Message);
                return response;
            }

            if (successfullyCancelled)
                response.SelectSingleNode("//statusMessage").InnerText = "Pass with ID " + ISRN + " Sucessfully Cancelled";
            else {
                response.SelectSingleNode("//statusMessage").InnerText = "Pass with ID " + ISRN + " Not Cancelled";
            }
            return response;

        }







        private void processError(ref XmlDocument response, int errorCode)
        {
            processError(ref response, errorCode, String.Empty);
        }

        private void processError(ref XmlDocument response, int errorCode, string message)
        {
            if (log.IsErrorEnabled) log.Error("Error received.");
            if (log.IsErrorEnabled) log.Error("Error code:" + errorCode);

            response.SelectSingleNode("//status").InnerText = "ERROR";
            response.SelectSingleNode("//statusMessage").InnerText = ctErrors.First(x => x.Key == errorCode).Value;
            if (!String.IsNullOrEmpty(message)) response.SelectSingleNode("//statusMessage").InnerText += " : " + message;
        }

        /// <summary>
        /// Logs all the supplied parameter names and values out to the debug log
        /// </summary>
        /// <param name="parms">List of parameter names to log. Values are automatically passed.</param>
        private void logParams(params object[] parms)
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            if (log.IsDebugEnabled) log.Debug("Output of parameters supplied for:" + stackTrace.GetFrame(1).GetMethod().Name);
            for (int i = 0; i < parms.Length; i++)
            {
                if (log.IsDebugEnabled) log.Debug("Parameter [" + i + "]: Name:" + stackTrace.GetFrame(1).GetMethod().GetParameters()[i].Name + " Value:[" + parms[i] + "]");
            }
        }

        /*internal bool getPassesForPrint()
        {
            try
            {
                CTBPMEngine.getInstance().buildZipFile();
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Error getting passes for print:" + ex.Message);
                return false;
            }
            return true;
        }

        internal bool getPrintedPasses()
        {
            try
            {
                CTBPMEngine.getInstance().downloadSFTP();
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Error downloading passes: " + ex.Message);
                return false;
            }
            return true;
        }

        internal bool processESPReports()
        {
            try
            {
                CTBPMEngine.getInstance().processESPReports();
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Could not process reports:" + ex.Message);
                return false;
            }
            return true;
        }*/

        private byte[] convertImage(string imageAsBase64String)
        {
            if (log.IsDebugEnabled) log.Debug("Attempting to convert the image string.");
            try
            {
                byte[] imageAsBytes = Convert.FromBase64String(imageAsBase64String);
                if (log.IsDebugEnabled) log.Debug("Image String converted to Byte array.");
                return imageAsBytes;
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Could not convert image string:" + ex.Message);
                throw new CTDataException(11);
            }
        }

        private string convertImage(byte[] imageAsBytes)
        {
            string imageAsBase64String = String.Empty;
            try
            {
                imageAsBase64String = Convert.ToBase64String(imageAsBytes);
            }
            catch (ArgumentNullException)
            {
                if (log.IsErrorEnabled) log.Error("No value supplied to convert to Base64 string.");
            }
            return imageAsBase64String;
        }

        /// <summary>
        /// Loads a predefined XML Fragment
        /// </summary>
        /// <param name="xmlFragmentFileName">Fragment Filename. File needs to exist in app_data</param>
        /// <returns>XElement</returns>
        private XElement LoadXmlFragment(string xmlFragmentFileName)
        {
            XElement xmlFragment = null;
            using (var txtReader = new XmlTextReader(appDataPath + xmlFragmentFileName))
            {
                xmlFragment = XElement.Load(txtReader);
            }

            return xmlFragment;

        }

        /*private void startAsynchronousPassRequest(CTData jobData, CTJob.CTJobType jobType)
        {
            if (log.IsDebugEnabled) log.Debug("Starting Asynchronous Operation");
            try
            {
                CTBPMWorkerDelegate worker = new CTBPMWorkerDelegate(CTBPMWorker);
                worker.BeginInvoke(jobData, jobType, null, null, null);
                if (log.IsDebugEnabled) log.Debug("Asynchronous Process Started, returning from initial call.");
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Could not invoke asynchronous job request: " + ex.Message);
                throw new CTDataException(16);
            }
        }*/

        internal string calculateTimeLeftString(int days)
        {
            //int time = int.Parse(_timeLeft);
            StringBuilder sb = new StringBuilder();
            int years = 0;
            int months = 0;

            if (days < 0)
            {
                return "Expired";
            }

            //Find out how many years
            if (days >= 365)
            {
                //Need to divide time by 365 to find the number of years
                years = days / 365;
                int yearsToRemove = years * 365;
                days = days - yearsToRemove;
                if (years > 1)
                {
                    sb.Append(years + " years");
                }
                else
                {
                    sb.Append(years + " year");
                }

            }
            if (days >= 30)
            {
                if (years >= 1)
                {
                    sb.Append(", ");
                }

                //Need to divide time by 30 to get the number of months
                months = days / 30;
                int monthsToRemove = months * 30;
                days = days - monthsToRemove;
                if (months > 1)
                {
                    sb.Append(months + " months");
                }
                else
                {
                    sb.Append(months + " month");
                }
            }

            if (days > 0)
            {
                if (years > 0 || months > 0)
                {
                    sb.Append(" and ");
                }
                if (days > 1)
                {
                    sb.Append(days + " days");
                }
                else
                {
                    sb.Append(days + " day");
                }
            }
            return sb.ToString();
        }




        #region Obsolete Methods
        [Obsolete("Please use the ValidateISRN method", true)]
        private bool validatePassNumber(string passNumberToValidate)
        {
            return false;
        }
        #endregion




    }

    #region Exception classes
    internal class DateException : Exception
    {
        internal DateException(string message)
            : base(message)
        {
        }
    };
    #endregion

}
