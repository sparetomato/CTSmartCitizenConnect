using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Services;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
//using CTBusinessProcessManager.CTDataV2_WS;
using CTSmartCitizenConnect;
using CTSmartCitizenConnect.SmartConnect;
using ExtensionMethods;
using log4net;
using Microsoft.Win32;
using warwickshire.ConcessionaryTravel.Classes;
using warwickshire.gov.uk.CT_WS;
using MyExtensions = ExtensionMethods.MyExtensions;

namespace CTSmartCitizenConnect
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class CTSmartCitizen : System.Web.Services.WebService
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private SmartConnect.CardManagerClient _cmClient;
        private readonly string appDataPath = HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data") + "/";

        // To Do - we need to have two CardManagerClients, or possibly just two ClientCredential objects. These need to pick up from the 
        // custom config section as per the "SmartCitizenSettings" example on line 55-57
        // Consider actually loading these into an array? multiple system configs?



        public CTSmartCitizen()
        {
            if (log.IsInfoEnabled) log.Info("Initialising CardManager Client");
            _cmClient = new CardManagerClient("WSHttpBinding_ICardManager1");
            _cmClient.Endpoint.Address = new EndpointAddress(new Uri(ConfigurationManager.AppSettings["SCUrl"]), _cmClient.Endpoint.Address.Identity, _cmClient.Endpoint.Address.Headers);
            if (log.IsDebugEnabled) log.Debug("Initialising Client credentials");


            // liveCredentials.UserName.UserName = smartCitizenConfig.LiveConfigSettings.SCUserId;
            // liveCredentials.UserName.Password = smartCitizenConfig.LiveConfigSettings.SCPassword;
            //_cmClient.ClientCredentials.UserName.UserName = smartCitizenConfig.LiveConfigSettings.SCUserId;
            _cmClient.ClientCredentials.UserName.UserName = ConfigurationManager.AppSettings["SCUserId"];
            _cmClient.ClientCredentials.UserName.Password = ConfigurationManager.AppSettings["SCPassword"];
            //_cmClient.ClientCredentials.UserName = SmartCitizenCredentialsManager.Live.UserName;

            //.ClientCredentials = SmartCitizenCredentialsManager.Live;

            if (log.IsDebugEnabled) log.Debug("Client Credentials initialised.");
            if (log.IsDebugEnabled) log.Debug("Bypassing certificate validation.");
            _cmClient.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode =
                System.ServiceModel.Security.X509CertificateValidationMode.None;
            if (log.IsInfoEnabled) log.Info("CardManager Client initialised.");

        }

        private static class CMClient
        {
            // This class manages the four different CardManaher Clients. Access each one from this class.

            private static readonly Dictionary<string, CardManagerClient> _cardManagerClients = new Dictionary<string, CardManagerClient>();

            static CMClient()
            {
                SmartCitizenSystemSettings smartCitizenConfig =
                ConfigurationManager.GetSection("SmartCitizenSystemSettingsSection") as SmartCitizenSystemSettings;

                //To Do - maybe see if we can get away with not having to create four objects and loop using one instance...?

                SmartConnect.CardManagerClient cmLiveClient = new CardManagerClient("WSHttpBinding_ICardManager1");
                SmartConnect.CardManagerClient cmTestClient = new CardManagerClient("WSHttpBinding_ICardManager1");
                SmartConnect.CardManagerClient cmLiveSelfClient = new CardManagerClient("WSHttpBinding_ICardManager1");
                SmartConnect.CardManagerClient cmTestSelfClient = new CardManagerClient("WSHttpBinding_ICardManager1");

                cmLiveClient.Endpoint.Address = new EndpointAddress(new Uri(ConfigurationManager.AppSettings["SCUrl"]), cmLiveClient.Endpoint.Address.Identity, cmLiveClient.Endpoint.Address.Headers);
                cmTestClient.Endpoint.Address = new EndpointAddress(new Uri(ConfigurationManager.AppSettings["SCUrl"]), cmTestClient.Endpoint.Address.Identity, cmTestClient.Endpoint.Address.Headers);
                cmLiveSelfClient.Endpoint.Address = new EndpointAddress(new Uri(ConfigurationManager.AppSettings["SCUrl"]), cmLiveSelfClient.Endpoint.Address.Identity, cmLiveSelfClient.Endpoint.Address.Headers);
                cmTestSelfClient.Endpoint.Address = new EndpointAddress(new Uri(ConfigurationManager.AppSettings["SCUrl"]), cmTestSelfClient.Endpoint.Address.Identity, cmTestSelfClient.Endpoint.Address.Headers);

                cmLiveClient.ClientCredentials.UserName.UserName = smartCitizenConfig.LiveConfigSettings.SCUserId;
                cmLiveClient.ClientCredentials.UserName.Password = smartCitizenConfig.LiveConfigSettings.SCPassword;
                _cardManagerClients.Add("Live", cmLiveClient);

                cmLiveSelfClient.ClientCredentials.UserName.UserName = smartCitizenConfig.LiveConfigSettings.SCSelfUserId;
                cmLiveSelfClient.ClientCredentials.UserName.Password =
                    smartCitizenConfig.LiveConfigSettings.SCSelfUserPassword;

                _cardManagerClients.Add("Live Self", cmLiveSelfClient);

                cmTestClient.ClientCredentials.UserName.UserName = smartCitizenConfig.TestConfigSettings.SCUserId;
                cmTestClient.ClientCredentials.UserName.Password = smartCitizenConfig.TestConfigSettings.SCPassword;

                _cardManagerClients.Add("Test", cmTestClient);

                cmTestSelfClient.ClientCredentials.UserName.UserName =
                    smartCitizenConfig.TestConfigSettings.SCSelfUserId;
                cmTestSelfClient.ClientCredentials.UserName.Password =
                    smartCitizenConfig.TestConfigSettings.SCSelfUserPassword;
                _cardManagerClients.Add("Test Self", cmTestSelfClient);

                // set each of the clients to ignore certificate errors.
                _cardManagerClients.ToDictionary(x => x.Key,
                    x => x.Value.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode =
                        System.ServiceModel.Security.X509CertificateValidationMode.None);

            }

            public static CardManagerClient Live
            {
                get { return _cardManagerClients["Live"]; }
            }

            public static CardManagerClient Test
            {
                get { return _cardManagerClients["Test"]; }
            }

            public static CardManagerClient Live_Self
            {
                get { return _cardManagerClients["Live Self"]; }
            }

            public static CardManagerClient Test_Self
            {
                get { return _cardManagerClients["Test Self"]; }
            }
        }



        [WebMethod]
        public bool TestConnection(string surname, string forename, string dateOfBirth, string postcode)
        {
            //SmartConnect.CardManagerClient client = new SmartConnect.CardManagerClient("WSHttpBinding_ICardManager1");
            //client.Endpoint.Address = new EndpointAddress(new Uri("https://warwickshiretest.smartcitizen.net/cardmanager3/cardmanager.svc"), client.Endpoint.Address.Identity, client.Endpoint.Address.Headers);
            //client.ClientCredentials.UserName.UserName = "9826212601000031";
            //client.ClientCredentials.UserName.Password = "f1rm5t3p!";
            //client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode =
            //    System.ServiceModel.Security.X509CertificateValidationMode.None;

            CardholderExistsData data = new CardholderExistsData();
            CardholderExistsResponse response;
            /*data.Surname = "Test";
            data.Forename = "Andy";
            data.DateOfBirth = new DateTime(1945,09,05);
            data.Postcode = "CV34 4TG";*/
            data.Surname = surname;
            data.Forename = forename;
            data.DateOfBirth = DateTime.Parse(dateOfBirth);
            data.Postcode = postcode;
            try
            {
                string serialized = SerializeObj(data);
                response = CMClient.Test.CheckCardholderExists(data);
                if (log.IsDebugEnabled)
                {
                    log.Debug("Cardholder Found:");
                    log.Debug("Cardholder Exists:" + response.RecordExists.ToString());
                    log.Debug("CardholderId:" + response.UniqueMatchIdentifier.CardholderID);
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }


            return true;

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
            if (log.IsInfoEnabled) log.Info("Updating and renewing pass details");
            logParams(cardHolderId, ISRN, title, forename, surname, dateOfBirth, gender, disabilitycategory, caseId);
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

            UpdatePassHolderDetails(cardHolderId, title, forename, surname, dateOfBirth, gender, disabilitycategory);
            return ReplacePass(cardHolderId, ISRN, 17, caseId);
        }

        [WebMethod(
            Description =
                "Updates Passholder Details on SmartCitizen. Note that this does not automatically issue a new pass.")] 
        public XmlDocument UpdatePassHolderDetails(int cardHolderId, string title, string forename, string surname, string dateOfBirth, string gender, string disabilitycategory)
        {
            if (log.IsInfoEnabled) log.Info("Updating Passholder Details for cardholder ID [" + cardHolderId + "]");
            logParams(cardHolderId, title, forename, surname, dateOfBirth, gender, disabilitycategory);
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
                                
                                //MyExtensions.ToTitleCase(title);
                        break;
                    case "3": //Forename
                        if (String.IsNullOrEmpty(forename))
                            itemElement.Remove();
                        else
                            itemElement.Value = forename.ToTitleCase();
                                //MyExtensions.ToTitleCase(forename);
                        break;
                    case "4": //Surname
                        if (String.IsNullOrEmpty(surname))
                            itemElement.Remove();
                        else
                            itemElement.Value = surname.ToSurnameTitleCase();
                                //MyExtensions.ToTitleCase(surname);
                        break;
                    case "21": //Display Name
                        if (String.IsNullOrEmpty(forename) || String.IsNullOrEmpty(surname))
                            itemElement.Remove();
                        else
                            itemElement.Value = forename.ToTitleCase() + " " + surname.ToSurnameTitleCase();
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

        [WebMethod(Description = "Gets cardholder information from SmartCitizen based on their unique ID.")]
        public XmlDocument GetCardHolderData(int uniqueId)
        {
            if (log.IsInfoEnabled) log.Info("Getting Cardholder data using uniqueID");
            if (log.IsDebugEnabled) log.Debug("Cardholder ID [" + uniqueId + "]");
            GetCardholderData getCardHolderRequest = new GetCardholderData();
            getCardHolderRequest.CardholderIdentifier = new RecordIdentifier() { CardholderID = uniqueId };
            try
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug("Making request to SmartCitizen.");
                    log.Debug("Raw XML:" + SerializeObj(getCardHolderRequest));
                }
                GetCardholderResponse cardHolderData = _cmClient.GetCardholder(getCardHolderRequest);
                if (log.IsDebugEnabled)
                {
                    log.Debug("Received Response.");
                    log.Debug("Raw XML:" + SerializeObj(cardHolderData));

                }


            }
            catch (Exception ex)
            {

                if (log.IsErrorEnabled) log.Error(ex.Message);
            }

            return new XmlDocument();
        }

        [WebMethod(Description = "Search for a pass on SmartCitizen by pass number")]
        public XmlDocument GetPassData(string PassId)
        {
            CheckCardData cardData = new CheckCardData() { CardIdentifier = PassId };
            try
            {
                CheckCardResponse cardResponse = _cmClient.CheckCard(cardData);
                if (log.IsDebugEnabled) log.Debug(SerializeObj(cardResponse));
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error(ex.Message);
                throw;
            }

            return new XmlDocument();

        }

        [WebMethod(Description = "Check the Hotlist Data")]
        public string CheckHotlist(string PassId)
        {
            GetHotlistData hotlistData = new GetHotlistData();
            hotlistData.MaximumRecords = 1000;
            //hotlistData.IssuerID = 28992;
            //hotlistData.IssuerID = 29040;
            //hotlistData.ApplicationID = 605802;

            CardListEntry[] cardListEntries = _cmClient.GetHotlist(hotlistData);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < cardListEntries.Length; i++)
                sb.Append(cardListEntries[i].SmartCardID + " " + cardListEntries[i].Status + Environment.NewLine);

            return sb.ToString();
        }


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
            // Call old method for now...
            //CTSelf_WS self = new CTSelf_WS();
            //return self.CheckPassHolderData(surname, forename, dateOfBirth, postcode, ISRN);

            // Redundant code here to be implemented with SmartCitizen switch.

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

        [WebMethod]
        public string AgeNextBirthday(string DoB)
        {
            DateTime dateOfBirth;
            if (DateTime.TryParse(DoB, out dateOfBirth) == false)
                return "Could not calculate age next birthday";

            int age = DateTime.Now.Year - dateOfBirth.Year;

            bool hadBirthday = false;

            if (DateTime.Now > dateOfBirth.AddYears(age))
            {
                age++;
                hadBirthday = true;
            }

            int yearOfNextBirthday;
            if (hadBirthday)
                yearOfNextBirthday = DateTime.Now.Year + 1;
            else
                yearOfNextBirthday = DateTime.Now.Year;

            string response;

            if (dateOfBirth.Day == 29 && dateOfBirth.Month == 2) // Check to see if next year is a Leap year...
            {
                DateTime tester;
                if (DateTime.TryParse("29/02" + yearOfNextBirthday, out tester))
                    response = "You will be " + age + " on " + dateOfBirth.Day + GetDaySuffix(dateOfBirth.Day) + " " + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateOfBirth.Month) + " " + yearOfNextBirthday;
                else
                    response = "You will be " + age + " on 1st March " + yearOfNextBirthday;

            }
            else
            {
                response = "You will be " + age + " on " + dateOfBirth.Day + GetDaySuffix(dateOfBirth.Day) + " " + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateOfBirth.Month) + " " + yearOfNextBirthday;
            }




            return response;
        }

        private string GetDaySuffix(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }



        private SmartCitizenCard getSmartCitizenCardForPerson(RecordIdentifier personIdentifier)
        {
            if (log.IsDebugEnabled) log.Debug("Getting Smart Citizen Card for Person");
            logParams(personIdentifier);
            SmartCitizenCard cardForPerson = new SmartCitizenCard();
            GetCardholderResponse cardHolderDetails = _cmClient.GetCardholder(new GetCardholderData() { CardholderIdentifier = personIdentifier });
            if (cardHolderDetails.Identifier.CardID != null)
            {
                CheckCardResponse cardCheckResponse =
                    _cmClient.CheckCard(new CheckCardData() { CardIdentifier = cardHolderDetails.Identifier.CardID });
                EntityDetailsListResponse[] entityDetailsListResponse = _cmClient.GetEntityList(personIdentifier);
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

        public class ScValidationException : Exception
        {
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


        private class SmartCitizenCard
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



        /*[WebMethod(Description = "Search for a cardholder on SmartCitizen and return their details")]
        public XmlDocument SearchSmartCitizen()
        {
            //GetCardholderData getCardholderRequest = new GetCardholderData();
            return new XmlDocument();
        }*/



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

        private void logParams(params object[] parms)
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            if (log.IsDebugEnabled) log.Debug("Output of parameters supplied for:" + stackTrace.GetFrame(1).GetMethod().Name);
            for (int i = 0; i < parms.Length; i++)
            {
                if (log.IsDebugEnabled) log.Debug("Parameter [" + i + "]: Name:" + stackTrace.GetFrame(1).GetMethod().GetParameters()[i].Name + " Value:[" + parms[i] + "]");
            }
        }
    }

    public class SmartCitizenSettings : ConfigurationElement
    {
        [ConfigurationProperty("SCUserId")]
        public string SCUserId
        {
            get { return this["SCUserId"].ToString(); }
        }

        [ConfigurationProperty("SCPassword")]
        public string SCPassword
        {
            get { return this["SCPassword"].ToString(); }
        }

        [ConfigurationProperty("SCSelfUserId")]
        public string SCSelfUserId
        {
            get { return this["SCSelfUserId"].ToString(); }
        }

        [ConfigurationProperty("SCSelfPassword")]
        public string SCSelfUserPassword
        {
            get { return this["SCSelfPassword"].ToString(); }
        }
    }

    public class SmartCitizenSystemSettings : ConfigurationSection
    {

        [ConfigurationProperty("Live")]
        public SmartCitizenSettings LiveConfigSettings
        {
            get { return (SmartCitizenSettings)this["Live"]; }
        }
        [ConfigurationProperty("Test")]
        public SmartCitizenSettings TestConfigSettings
        {
            get { return (SmartCitizenSettings)this["Test"]; }
        }

    }






    //public class SmartCitizenSettingsHandler : IConfigurationSectionHandler
    //{
    //    private SmartCitizenSettingsHandler()
    //    {
    //    }

    //    public object Create(object parent, object configContext, XmlNode section)
    //    {
    //        SmartCitizenSystemSettings objSettings = new SmartCitizenSystemSettings();
    //        objSettings.SCUserId = section.Attributes["SCUserId"].InnerText;
    //        return objSettings;
    //    }
    //}
}


namespace ExtensionMethods
{
    public static class MyExtensions
    {
        //public static string ToTitleCase(this string str)
        //{
        //    CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
        //    TextInfo textInfo = cultureInfo.TextInfo;
        //    return textInfo.ToTitleCase(str);
        //}

        public static string ToSurnameTitleCase(this string str)
        {
            str = str.ToTitleCase();
            if (str.Substring(0, 3).ToLower() == "mac")
                str = str.Substring(0, 3) + str.Substring(3, 1).ToUpper() + str.Substring(4);
            if (str.Substring(0, 2).ToLower() == "mc")
                str = str.Substring(0, 2) + str.Substring(2, 1).ToUpper() + str.Substring(3);
            if (str.Substring(0, 2).ToLower() == "o'")
                str = str.Substring(0, 2) + str.Substring(2, 1).ToUpper() + str.Substring(3);

            return str;

        }
    }
}