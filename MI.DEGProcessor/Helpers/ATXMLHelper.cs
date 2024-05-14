using System.Text;
using System.Xml;
using IGS.Models.Entities;
using IGS.Models.Helpers;
using NLog;
using static MI.DEGProcessor.Helpers.Enums;

namespace MI.DEGProcessor.Helpers;

public static class ATXMLHelper
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public static string WriteDetails(this StagingAtxml at)
    {
        var sb = new StringBuilder();

        sb.AppendLine("StagingATXMLID: " + at.StagingAtxmlid);
        sb.AppendLine("ApplicationID: "  + at.ApplicationId);
        sb.AppendLine("TransferID: "     + at.TransferId);

        return sb.ToString();
    }

    public static string GetSingleNodeValueString(string xpath, string defaultValue, XmlNode root)
    {
        try
        {
            var node = root.SelectSingleNode(xpath);
            if (node != null)
            {
                return node.InnerText;
            }

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public static bool GetSingleNodeValueBool(string xpath, bool defaultValue, XmlNode root)
    {
        try
        {
            var value = GetSingleNodeValueString(xpath, "", root);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return bool.Parse(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    public static DateTime? GetSingleNodeValueNullableDateTime(string xpath, XmlNode root)
    {
        try
        {
            var value = GetSingleNodeValueString(xpath, "", root);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.Parse(value);
        }
        catch
        {
            return null;
        }
    }

    public static bool? GetSingleNodeValueNullableBool(string xpath, XmlNode root)
    {
        try
        {
            var value = GetSingleNodeValueString(xpath, "", root);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return bool.Parse(value);
        }
        catch
        {
            return null;
        }
    }

    public static string MapSingleNodeValueBoolToString(string  xpath,
                                                        string  trueValue,
                                                        string  falseValue,
                                                        string  emptyValue,
                                                        XmlNode root)
    {
        var value = GetSingleNodeValueString(xpath, emptyValue, root);
        switch (value)
        {
            case "true":
                value = trueValue;
                break;
            case "false":
                value = falseValue;
                break;
            default:
                value = emptyValue;
                break;
        }

        return value;
    }

    public static void RemoveSOAPEnvelope(string sourceXpath, string targetXpath, XmlDocument document)
    {
        var xFragment = document.CreateDocumentFragment();
        xFragment.InnerXml = document.SelectSingleNode(sourceXpath).OuterXml;
        ReplaceFragment(targetXpath, xFragment, document);
    }

    public static void ReplaceFragment(string xpath, XmlDocumentFragment fragment, XmlDocument document)
    {
        document.SelectSingleNode(xpath).RemoveAll();
        document.SelectSingleNode(xpath).AppendChild(fragment);
    }

    public static async Task<SaveAtXmlToDatabaseResult> SaveAtXmlToDatabaseAsync(string path, int applicationTransferId)
    {
        try
        {
            // get file from S3
            string atXml;
            try
            {
                atXml = await S3Utils.RetrieveAtXmlAsync(path, CancellationToken.None);
                if (string.IsNullOrEmpty(atXml))
                {
                    _logger.Error($"Could not find S3 document at {path}");
                    var backFill = new LogBackfillError();
                    var results = backFill.SaveTransferResults(applicationTransferId, "Error", $"Could not find S3 document at {path}", CancellationToken.None);
                    return SaveAtXmlToDatabaseResult.S3FileMissing;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Could not find S3 document at {path}", ex);
                var backFill = new LogBackfillError();
                var results = backFill.SaveTransferResults(applicationTransferId, "Error", $"Could not find S3 document at {path}, {ex.Message}", CancellationToken.None);
                return SaveAtXmlToDatabaseResult.S3FileMissing;
            }

            WorkXml workXml;
            try
            {
                //extract info
                var doc = new XmlDocument();
                doc.LoadXml(atXml);

                //save to database
                workXml = new WorkXml(atXml,
                                      applicationTransferId,
                                      DateTime.Now,
                                      AppSettings.Instance.SwitchATXSDValidation,
                                      AppSettings.Instance.SwitchDeterminationXSDValidation,
                                      true);
            }
            catch (Exception ex)
            {
                _logger.Error("Error converting atXml string to xml document.", ex);
                var backFill = new LogBackfillError();
                var results = backFill.SaveTransferResults(applicationTransferId, "Error", $"Error converting atXml string to xml document: {applicationTransferId}", CancellationToken.None);
                return SaveAtXmlToDatabaseResult.XmlError;
            }

            ApplicationTransfer info         = null;
            var                 errorMessage = "";
            try
            {
                if (workXml.Valid)
                {
                    info = await workXml.SaveIndexesAsync(CancellationToken.None);
                }
                else
                {
                    errorMessage = "Error saving Additional Fields: " +
                                   workXml.ErrorMessage               +
                                   Environment.NewLine                +
                                   "ApplicationTransferId:"           +
                                   applicationTransferId;
                    _logger.Error(errorMessage);
                    var backFill = new LogBackfillError();
                    var results = backFill.SaveTransferResults(applicationTransferId, "Error", errorMessage, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Unknown Exception saving Additional Fields." +
                               Environment.NewLine                           +
                               "ApplicationTransferId:"                      +
                               applicationTransferId;
                _logger.Error(ex, errorMessage);
                var backFill = new LogBackfillError();
                var results = backFill.SaveTransferResults(applicationTransferId, "Error", errorMessage + " " + ex.Message, CancellationToken.None);

            }
        }

        catch (Exception ex)
        {
            _logger.Error("Error saving ATXML record:", ex);
            return SaveAtXmlToDatabaseResult.DatabaseError;
        }

        return SaveAtXmlToDatabaseResult.Success;
    }
}