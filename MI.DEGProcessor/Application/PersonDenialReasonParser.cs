using System.Xml;
using MI.DEGProcessor.Helpers;

namespace MI.DEGProcessor.Application;

public class PersonDenialReasonParser
{
    private readonly XmlNode personNode;
    private readonly XmlNode programNode;

    public PersonDenialReasonParser(XmlNode personNode, XmlNode programNode)
    {
        this.personNode  = personNode;
        this.programNode = programNode;
    }

    public string PersonId
    {
        get => ATXMLHelper.GetSingleNodeValueString("@*[local-name()='id']", string.Empty, personNode);
        set => throw new NotImplementedException();
    }

    public string ProgramCode
    {
        get => ATXMLHelper.GetSingleNodeValueString("*[local-name()='the_program']/*[local-name()='text-val']",
                                                    string.Empty,
                                                    programNode);
        set => throw new NotImplementedException();
    }

    public string ReasonCode
    {
        get =>
            ATXMLHelper.GetSingleNodeValueString("*[local-name()='o_p_ineligible-reasons']/*[local-name()='text-val']",
                                                 string.Empty,
                                                 programNode);
        set => throw new NotImplementedException();
    }
}