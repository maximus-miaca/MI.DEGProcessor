using System.Xml;
using IGS.Models.Entities;
using MI.Common.Extensions;
using MI.DEGProcessor.Helpers;

namespace MI.DEGProcessor.Application;

public class ApplicationIndexParser : ApplicationIndex
{
    private readonly string _xPathPersonMailingCodeIndicator =
        "*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation']/*[local-name()='ContactInformationCategoryCode']";

    private readonly XmlNode applicantNode;

    private readonly XmlNode                        personNode;
    private readonly XmlNode?                       resultPersonNode;
    private readonly string                         ssfSignerID;
    private          List<PersonDenialReasonParser> _personDenialReasons;

    public ApplicationIndexParser(XmlNode personNode, XmlNode applicantNode, XmlNode? resultPerson, string ssfSignerID)
    {
        this.personNode    = personNode;
        this.applicantNode = applicantNode;
        resultPersonNode   = resultPerson;
        this.ssfSignerID   = ssfSignerID;
    }

    public List<PersonDenialReasonParser> PersonDenialReasons
    {
        get
        {
            if (_personDenialReasons == null)
            {
                _personDenialReasons = new List<PersonDenialReasonParser>();
                if (resultPersonNode != null)
                {
                    foreach (XmlNode programNode in
                             resultPersonNode.SelectNodes("*[local-name()='list-program']/*[local-name()='program']"))
                    {
                        _personDenialReasons.Add(new PersonDenialReasonParser(personNode, programNode));
                    }
                }
            }

            return _personDenialReasons;
        }
    }

    public new string Address
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Mailing']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStreet']/*[local-name()='StreetFullText'][1]",
                                         string.Empty,
                                         personNode)
               .NullIfEmpty() ??
            HomeAddress;
        set => throw new NotImplementedException();
    }

    public new bool? EligibleImmigrationStatus
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='InsuranceApplicantLawfulPresenceStatus']/*[local-name()='LawfulPresenceStatusEligibility']/*[local-name()='EligibilityIndicator']",
                                               applicantNode);
        set => throw new NotImplementedException();
    }

    public new bool? EligibleTribalBenefits
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='InsuranceApplicantEligibleITUServicesIndicator']",
                                               applicantNode);
        set => throw new NotImplementedException();
    }

    public new string Email
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation']/*[local-name()='ContactInformation']/*[local-name()='ContactEmailID'][1]",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    public new bool? EmploymentStatus
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='PersonAugmentation']/*[local-name()='PersonEmploymentAssociation']/*[local-name()='EmploymentStatus']/*[local-name()='StatusIndicator']",
                                               personNode);
        set => throw new NotImplementedException();
    }

    public new string EmploymentStatusCode
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonEmploymentAssociation']/*[local-name()='EmploymentStatus']/*[local-name()='EmploymentStatusCode']",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    public new bool? EsienrolledIndicator
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='InsuranceApplicantESIAssociation']/*[local-name()='InsuranceApplicantESIEnrolledIndicator']",
                                               applicantNode);
        set => throw new NotImplementedException();
    }

    public new string Ethnicity
    {
        get =>
            ATXMLHelper.GetSingleNodeValueString("*[local-name()='PersonEthnicityText']", string.Empty, personNode);
        set => throw new NotImplementedException();
    }

    public new string HomeAddress
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Home']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStreet']/*[local-name()='StreetFullText'][1]",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    private string AddressXPath
    {
        get => AddressXPaths[AddressXPathIndex];
        set => throw new NotImplementedException();
    }

    private int AddressXPathIndex
    {
        get
        {
            var xpath = string.Empty;
            for (var i = 0; i < AddressXPaths.Length; i++)
            {
                xpath = AddressXPaths[i];
                if (!string.IsNullOrEmpty(ATXMLHelper.GetSingleNodeValueString(xpath, string.Empty, personNode)))
                {
                    return i;
                }
            }

            return AddressXPaths.Length - 1;
        }
    }

    public new bool? AmericanIndianOrAlaskanNative
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='TribalAugmentation']/*[local-name()='PersonAmericanIndianOrAlaskaNativeIndicator']",
                                               personNode);
        set => throw new NotImplementedException();
    }

    public new string ApplicantId
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("//*[local-name()='InsuranceApplicant'][*[local-name()='RoleOfPersonReference']/@*[local-name()='ref']='" +
                                         PersonId +
                                         "']/@*[local-name()='id']",
                                         string.Empty,
                                         personNode.OwnerDocument
                                                   .SelectNodes("//*[local-name()='InsuranceApplication']")[0]);
        set => throw new NotImplementedException();
    }

    public new string CreatedBy
    {
        get => string.IsNullOrEmpty(base.CreatedBy) ? string.Empty : base.CreatedBy;
        set => base.CreatedBy = value;
    }

    public new bool? CitizenIndicator
    {
        get =>
            ATXMLHelper.GetSingleNodeValueNullableBool("*[local-name()='PersonUSCitizenIndicator']", personNode);
        set => throw new NotImplementedException();
    }

    public new string HomeCity
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Home']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCityName'][1]",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    public new string City
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Mailing']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCityName'][1]",
                                         string.Empty,
                                         personNode)
               .NullIfEmpty() ??
            HomeCity;
        set => throw new NotImplementedException();
    }

    private string CityXPath => CityXPaths[AddressXPathIndex];

    public new string HomeCountyCode
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Home']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyCode'][1]",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    public new string MailingCountyCode
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Mailing']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyCode'][1]",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    private string CountyCodeXPath => CountyCodeXPaths[AddressXPathIndex];

    public new string HomeCounty
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Home']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyName'][1]",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    public new string MailingCounty
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Mailing']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyName'][1]",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    private string CountyXPath => CountyXPaths[AddressXPathIndex];

    public new bool? DisabilityIndicator
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='InsuranceApplicantBlindnessOrDisabilityIndicator']",
                                               applicantNode);
        set => throw new NotImplementedException();
    }

    public new DateTime? Dob
    {
        get
        {
            var dob = ATXMLHelper.GetSingleNodeValueString("*[local-name()='PersonBirthDate']/*[local-name()='Date']",
                                                           string.Empty,
                                                           personNode);
            if (DateTime.TryParse(dob, out var ret))
            {
                return ret;
            }

            return null;
        }
        set => throw new NotImplementedException();
    }

    public new string FirstName
    {
        get =>
            ATXMLHelper.GetSingleNodeValueString("*[local-name()='PersonName']/*[local-name()='PersonGivenName']",
                                                 string.Empty,
                                                 personNode);
        set => throw new NotImplementedException();
    }

    public new bool? ParentOfUnder19Child
    {
        get =>
            ATXMLHelper.GetSingleNodeValueNullableBool("*[local-name()='InsuranceApplicantParentCaretakerIndicator']",
                                                       applicantNode);
        set => throw new NotImplementedException();
    }

    public new bool? FlintWaterVerification
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='PersonAugmentation']/*[local-name()='PersonIdentification'][*[local-name()='IdentificationID']='HasAdditionalFlintAddress']/*[local-name()='IdentificationCategoryText']",
                                               personNode);
        set => throw new NotImplementedException();
    }

    public new bool? FosterCareIndicator
    {
        get =>
            ATXMLHelper.GetSingleNodeValueNullableBool("*[local-name()='InsuranceApplicantFosterCareIndicator']",
                                                       applicantNode);
        set => throw new NotImplementedException();
    }

    public new bool? FulltimeStudent
    {
        get =>
            ATXMLHelper.GetSingleNodeValueNullableBool("*[local-name()='InsuranceApplicantStudentIndicator']",
                                                       applicantNode);
        set => throw new NotImplementedException();
    }

    public new bool? HasNonEsicoverage
    {
        get =>
            ATXMLHelper.GetSingleNodeValueNullableBool("*[local-name()='InsuranceApplicantNonESICoverageIndicator']",
                                                       applicantNode);
        set => throw new NotImplementedException();
    }

    public new bool? HelpPayingMapremiums
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='PersonAugmentation']/*[local-name()='PersonIdentification'][*[local-name()='IdentificationID']='HelpPayingMAPremiums']/*[local-name()='IdentificationCategoryText']",
                                               personNode);
        set => throw new NotImplementedException();
    }

    public new bool Hoh
    {
        get => Ssfsigner;
        set => throw new NotImplementedException();
    }

    public new string Sex
    {
        get =>
            ATXMLHelper.GetSingleNodeValueString("*[local-name()='PersonSexText']", string.Empty, personNode);
        set => throw new NotImplementedException();
    }

    public new bool Ssfsigner
    {
        get => PersonId == ssfSignerID;
        set => throw new NotImplementedException();
    }

    public new string LastName
    {
        get => ATXMLHelper.GetSingleNodeValueString("*[local-name()='PersonName']/*[local-name()='PersonSurName']",
                                                    string.Empty,
                                                    personNode);
        set => throw new NotImplementedException();
    }

    public new string Magimcdetermination
    {
        get =>
            ATXMLHelper
               .MapSingleNodeValueBoolToString("//*[local-name()='assess-response']/*[local-name()='global-instance']/*[local-name()='list-person']/*[local-name()='person'][@id='" +
                                               PersonId +
                                               "']/*[local-name()='o_per_elig-attested-MAGI-Medicaid']/*[local-name()='boolean-val']",
                                               "APPROVED",
                                               "DENIED",
                                               "NONE",
                                               personNode.OwnerDocument);
        set => throw new NotImplementedException();
    }

    public new string MagimichildDetermination
    {
        get =>
            ATXMLHelper
               .MapSingleNodeValueBoolToString("//*[local-name()='assess-response']/*[local-name()='global-instance']/*[local-name()='list-person']/*[local-name()='person'][@id='" +
                                               PersonId +
                                               "']/*[local-name()='o_per_elig-attested-MAGI-MIChild']/*[local-name()='boolean-val']",
                                               "APPROVED",
                                               "DENIED",
                                               "NONE",
                                               personNode.OwnerDocument);
        set => throw new NotImplementedException();
    }

    public new string MiddleName
    {
        get =>
            ATXMLHelper.GetSingleNodeValueString("*[local-name()='PersonName']/*[local-name()='PersonMiddleName']",
                                                 string.Empty,
                                                 personNode);
        set => throw new NotImplementedException();
    }

    public new string Pend
    {
        get =>
            ATXMLHelper
               .MapSingleNodeValueBoolToString("//*[local-name()='assess-response']/*[local-name()='global-instance']/*[local-name()='list-person']/*[local-name()='person'][@id='" +
                                               PersonId +
                                               "']/*[local-name()='o_per_pend-person']/*[local-name()='boolean-val']",
                                               "Y",
                                               "N",
                                               string.Empty,
                                               personNode.OwnerDocument);
        set => throw new NotImplementedException();
    }

    public new string PreferredSpokenLanguage
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonPreferredLanguage']/*[local-name()='LanguageName'][1]",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    public new string PersonId
    {
        get => ATXMLHelper.GetSingleNodeValueString("@*[local-name()='id']", string.Empty, personNode);
        set => throw new NotImplementedException();
    }

    public new string Relationship
    {
        get
        {
            int testInt;
            var testIntString =
                ATXMLHelper
                   .GetSingleNodeValueString("//*[local-name()='Person'][1]/*[local-name()='PersonAugmentation']/*[local-name()='PersonAssociation'][*[local-name()='PersonReference']/@*[local-name()='ref']='" +
                                             PersonId +
                                             "']/*[local-name()='FamilyRelationshipCode']",
                                             string.Empty,
                                             personNode);
            if (int.TryParse(testIntString.Replace("-", ""), out testInt))
            {
                return testIntString;
            }

            return "18";
        }
        set => throw new NotImplementedException();
    }

    public new string Ssn
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonSSNIdentification']/*[local-name()='IdentificationID']",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    public new string HomeState
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Home']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStateUSPostalServiceCode'][1]",
                                         string.Empty,
                                         personNode);
        set => throw new NotImplementedException();
    }

    public new string State
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Mailing']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStateUSPostalServiceCode'][1]",
                                         string.Empty,
                                         personNode)
               .NullIfEmpty() ??
            HomeState;
        set => throw new NotImplementedException();
    }

    private string StateXPath => StateXPaths[AddressXPathIndex];

    public new string Phone
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationIsPrimaryIndicator']/text()='true']/*[local-name()='ContactInformation']/*[local-name()='ContactTelephoneNumber']/*[local-name()='FullTelephoneNumber']/*[local-name()='TelephoneNumberFullID'][1]",
                                         string.Empty,
                                         personNode)
               .Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "");
        set => throw new NotImplementedException();
    }

    public new bool? PregnancyIndicator
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='PersonAugmentation']/*[local-name()='PersonPregnancyStatus']/*[local-name()='StatusIndicator']",
                                               personNode);
        set => throw new NotImplementedException();
    }

    public new DateTime? PregnancyDueDate
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableDateTime("*[local-name()='PersonAugmentation']/*[local-name()='PersonIdentification'][*[local-name()='IdentificationID']='i_per_pregnancy_due_date']/*[local-name()='IdentificationCategoryText']",
                                                   personNode);
        set => throw new NotImplementedException();
    }

    public new string OtherPhone
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationIsPrimaryIndicator']/text()='false']/*[local-name()='ContactInformation']/*[local-name()='ContactTelephoneNumber']/*[local-name()='FullTelephoneNumber']/*[local-name()='TelephoneNumberFullID'][1]",
                                         string.Empty,
                                         personNode)
               .Replace("-", "").Replace("(","").Replace(")","").Replace(" ","");
        set => throw new NotImplementedException();
    }

    public new string Race
    {
        get =>
            ATXMLHelper.GetSingleNodeValueString("*[local-name()='PersonRaceText']", string.Empty, personNode);
        set => throw new NotImplementedException();
    }

    public new bool? ReceivedItuservices
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='InsuranceApplicantReceivedITUServicesIndicator']",
                                               applicantNode);
        set => throw new NotImplementedException();
    }

    public new bool? RecentMedicalBillsIndicator
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueNullableBool("*[local-name()='InsuranceApplicantRecentMedicalBillsIndicator']",
                                               applicantNode);
        set => throw new NotImplementedException();
    }

    public new string HomeZip
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Home']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationPostalCode'][1]",
                                         string.Empty,
                                         personNode)
               .Replace("-", "");
        set => throw new NotImplementedException();
    }

    public new string InsurancePolicySourceCode
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='InsuranceApplicantNonESIPolicy']/*[local-name()='InsurancePolicySourceCode']",
                                         string.Empty,
                                         applicantNode);
        set => throw new NotImplementedException();
    }

    public new string Zip
    {
        get =>
            ATXMLHelper
               .GetSingleNodeValueString("*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][*[local-name()='ContactInformationCategoryCode']/text()='Mailing']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationPostalCode'][1]",
                                         string.Empty,
                                         personNode)
               .Replace("-", "")
               .NullIfEmpty() ??
            HomeZip;
        set => throw new NotImplementedException();
    }

    private string ZipXPath => ZipXPaths[AddressXPathIndex];

    private int GetAddressIndicatorXPathIndex(string addressType)
    {
        var nodes = personNode.SelectNodes(_xPathPersonMailingCodeIndicator);
        if (nodes != null)
        {
            for (var i = 0; i < nodes.Count - 1; i++)
            {
                if (nodes[i].InnerText.Equals(addressType, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1;
                }
            }
        }

        return -1;
    }

    #region xpath arrays

    private string[] AddressXPaths
    {
        get
        {
            return new string[3]
                   {
                       //$"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Home")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStreet']/*[local-name()='StreetFullText'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Mailing")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStreet']/*[local-name()='StreetFullText'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Self")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStreet']/*[local-name()='StreetFullText'][1]",
                       "*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStreet']/*[local-name()='StreetFullText'][1]"
                   };
        }
    }

    private string[] CityXPaths
    {
        get
        {
            return new string[3]
                   {
                       //$"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Home")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStreet']/*[local-name()='LocationCityName'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Mailing")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCityName'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Self")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCityName'][1]",
                       "*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCityName'][1]"
                   };
        }
    }

    private string[] StateXPaths
    {
        get
        {
            return new string[3]
                   {
                       //$"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Home")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStreet']/*[local-name()='LocationStateUSPostalServiceCode'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Mailing")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStateUSPostalServiceCode'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Self")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStateUSPostalServiceCode'][1]",
                       "*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStateUSPostalServiceCode'][1]"
                   };
        }
    }

    private string[] ZipXPaths
    {
        get
        {
            return new string[3]
                   {
                       //$"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Home")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStreet']/*[local-name()='LocationPostalCode'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Mailing")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationPostalCode'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Self")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationPostalCode'][1]",
                       "*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationPostalCode'][1]"
                   };
        }
    }

    private string[] CountyCodeXPaths
    {
        get
        {
            return new string[3]
                   {
                       //$"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Home")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationStreet']/*[local-name()='LocationCountyCode'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Mailing")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyCode'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Self")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyCode'][1]",
                       "*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyCode'][1]"
                   };
        }
    }

    private string[] CountyXPaths
    {
        get
        {
            return new string[3]
                   {
                       //"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation']/*[local-name()='ContactInformation'][local-name=ContactInformationCategoryCode/text()=Home]/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyName'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Mailing")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyName'][1]",
                       $"*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation'][{GetAddressIndicatorXPathIndex("Self")}]/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyName'][1]",
                       "*[local-name()='PersonAugmentation']/*[local-name()='PersonContactInformationAssociation']/*[local-name()='ContactInformation']/*[local-name()='ContactMailingAddress']/*[local-name()='StructuredAddress']/*[local-name()='LocationCountyName'][1]"
                   };
        }
    }

    #endregion
}