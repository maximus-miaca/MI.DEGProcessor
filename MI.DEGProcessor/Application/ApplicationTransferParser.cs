using System.Xml;
using IGS.Models.Entities;
using MI.Common.Extensions;
using MI.DEGProcessor.Helpers;

namespace MI.DEGProcessor.Application;

public class ApplicationTransferParser : ApplicationTransfer
{
    private readonly XmlDocument                  _document;
    private          List<ApplicationIndexParser> _indexes;

    private List<TransitionParser> _transitions;

    public ApplicationTransferParser(XmlDocument document)
    {
        _document = document;
    }

    public new string ApplicationId
    {
        get =>
            ATXMLHelper.GetSingleNodeValueString("*[local-name()='ApplicationIdentification']/*[local-name()='IdentificationID']",
                                                 string.Empty,
                                                 InsuranceApplicationNode);
        set => throw new NotImplementedException();
    }

    public new bool? AnyoneHasHealthInsurance
    {
        get
        {
            var nodes =
                InsuranceApplicationNode
                   .SelectNodes("*[local-name()='InsuranceApplicant']/*[local-name()='InsuranceApplicantNonESICoverageIndicator']");
            var hasInsurance = false;
            foreach (XmlNode node in nodes)
            {
                try
                {
                    var value = bool.Parse(node.InnerText);
                    if (value)
                    {
                        hasInsurance = true;
                        break;
                    }
                }
                catch
                {
                    // ignore error means false
                }
            }

            return hasInsurance;
        }
        set => throw new NotImplementedException();
    }

    public string ATXML
    {
        get => _document.OuterXml;
        set => throw new NotImplementedException();
    }

    public new string CaseId
    {
        get => string.Empty;
        set => throw new NotImplementedException();
    }

    public new string CreatedBy
    {
        get => string.IsNullOrEmpty(base.CreatedBy) ? string.Empty : base.CreatedBy;
        set => base.CreatedBy = value;
    }

    public List<ApplicationIndexParser> ApplicationIndices
    {
        get
        {
            if (_indexes == null)
            {
                _indexes = new List<ApplicationIndexParser>();
                foreach (XmlNode personNode in _document.SelectNodes("//*[local-name()='Person']"))
                {
                    var id = ATXMLHelper.GetSingleNodeValueString("@*[local-name()='id']", string.Empty, personNode);
                    var applicantNode =
                        _document.SelectSingleNode($"//*[local-name()='InsuranceApplicant'][*[local-name()='RoleOfPersonReference'][@*[local-name()='ref']='{personNode.GetAttributeValueByLocalName("id")}']]");
                    var resultPerson =
                        _document.SelectSingleNode($"/EDResult/DeterminationsReasons/*[local-name()='assess-response']/*[local-name()='global-instance']/*[local-name()='list-person']/*[local-name()='person'][@*[local-name()='id']='{id}']");
                    _indexes.Add(new ApplicationIndexParser(personNode, applicantNode, resultPerson, SSFSignerID));
                }
            }

            return _indexes;
        }
    }

    public XmlNode InsuranceApplicationNode => _document.SelectNodes("//*[local-name()='InsuranceApplication']")[0];

    public new string OriginalFfmtransferId
    {
        get =>
            ATXMLHelper.GetSingleNodeValueString("/EDResult/ATXML/*[local-name()='AccountTransferRequest']/*[local-name()='InsuranceApplication']/*[local-name()='ApplicationUpdate'][@*[local-name()='id']='OriginalFFMTransferId']/*[local-name()='ActivityIdentification']/*[local-name()='IdentificationCategoryText']",
                                                 string.Empty,
                                                 _document);
        set => throw new NotImplementedException();
    }

    public new string Pend
    {
        get =>
            ATXMLHelper
               .MapSingleNodeValueBoolToString("//EDResult/DeterminationsReasons/*[local-name() = 'assess-response']/*[local-name() = 'global-instance']/*[local-name() = 'o_g_pend-application']/*[local-name()='boolean-val']",
                                               "Y",
                                               "N",
                                               "",
                                               _document);
        set => throw new NotImplementedException();
    }

    public new string PendInvalidRelationships
    {
        get =>
            ATXMLHelper.GetSingleNodeValueBool("//*[local-name()='assess-response']/*[local-name()='global-instance']/*[local-name()='o_g_application-pended-due-to-invalid-relationships']/*[local-name()='boolean-val']",
                                               false,
                                               _document)
                ? "Y"
                : "N";
        set => throw new NotImplementedException();
    }

    public new string PendSameSex
    {
        get =>
            ATXMLHelper.GetSingleNodeValueBool("//*[local-name()='assess-response']/*[local-name()='global-instance']/*[local-name()='o_g_pend-for-determination-of-same-sex-couple-eligibility']/*[local-name()='boolean-val']",
                                               false,
                                               _document)
                ? "Y"
                : "N";
        set => throw new NotImplementedException();
    }

    public new string Source
    {
        get => "SOM-HUB";
        set => throw new NotImplementedException();
    }

    public new string SourceXml
    {
        get => string.Empty;
        set => throw new NotImplementedException();
    }

    private string SSFSignerID =>
        ATXMLHelper.GetSingleNodeValueString("//*[local-name()='SSFSigner']/*[local-name()='RoleOfPersonReference']/@*[local-name()='ref']",
                                             string.Empty,
                                             InsuranceApplicationNode);

    public new DateTime TransferDate
    {
        get
        {
            var nowUtc      = DateTime.UtcNow;
            var nowMountain = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time"));
            var nowEastern  = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
            var valueEastern =
                ATXMLHelper
                   .GetSingleNodeValueString("//*[local-name()='TransferHeader']/*[local-name()='TransferActivity']/*[local-name()='ActivityDate']",
                                             nowEastern.ToString(),
                                             _document);
            if (string.IsNullOrEmpty(valueEastern))
            {
                return nowMountain;
            }

            var activityDateEastern = valueEastern.SafeDateTimeOffsetParse(TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).DateTime;
            var activityDateUtc = TimeZoneInfo.ConvertTimeToUtc(activityDateEastern, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
            var activityDateMountain =
                TimeZoneInfo.ConvertTimeFromUtc(activityDateUtc, TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time"));
            return activityDateMountain;
        }
        set => throw new NotImplementedException();
    }

    public new string TransferId
    {
        get =>
            ATXMLHelper.GetSingleNodeValueString("//*[local-name()='TransferHeader']/*[local-name()='TransferActivity']/*[local-name()='ActivityIdentification']/*[local-name()='IdentificationID']",
                                                 "",
                                                 _document);
        set => throw new NotImplementedException();
    }

    public new List<TransitionParser> Transitions
    {
        get
        {
            if (_transitions == null)
            {
                _transitions = new List<TransitionParser> { new("RECEIVED", "Added by DEG Processor") };
            }

            return _transitions;
        }
    }
}