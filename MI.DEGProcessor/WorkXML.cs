using System.Xml;
using Amazon;
using IGS.DataServices;
using IGS.Models.Entities;
using IGS.Models.Helpers;
using IGS.Models.Interfaces;
using MI.Common.Configuration;
using MI.Common.Extensions;
using MI.Common.Helper;
using MI.DEGProcessor.Application;
using MI.DEGProcessor.Helpers;
using ATXMLHelper = MI.DEGProcessor.Helpers.ATXMLHelper;

namespace MI.DEGProcessor;

public class WorkXml
{
    private RegionEndpoint _bucketRegion;
    private IDataServices  _data;
    private bool           _validateAtDocument;
    private bool           _validateDeterminationDocument;

    public WorkXml(string xmlString, DateTime dateReceived, bool validateAtDocument, bool validateDeterminationDocument)
    {
        _data = new DataServices(SqlHelper.EnsureTrustedServerCertificate(GlobalConnectionStrings.Instance.MIACA_IGS));
        Initialize(_data, xmlString, dateReceived, validateAtDocument, validateDeterminationDocument);
    }

    public WorkXml(string   xmlString,
                   int      applicationTransferId,
                   DateTime dateReceived,
                   bool     validateAtDocument,
                   bool     validateDeterminationDocument,
                   bool     suspendS3Writes)
    {
        _data           = new DataServices(SqlHelper.EnsureTrustedServerCertificate(GlobalConnectionStrings.Instance.MIACA_IGS));
        SuspendS3Writes = suspendS3Writes;

        var rc = _data.ApplicationTransfers.FirstOrDefaultAsync(x => x.ApplicationTransferId == applicationTransferId);
        rc.Wait();
        App = rc.Result;

        var rc2 = _data.ApplicationIndexs.FindAsync(x => x.ApplicationTransferId == applicationTransferId);
        rc2.Wait();
        Indexes = rc2.Result.ToList();

        var rc3 = _data.Transitions.FindAsync(x => x.ApplicationTransferId == applicationTransferId);
        rc3.Wait();
        Transitions = rc3.Result.ToList();
        BypassSave  = false;
        Valid       = false;

        if (Indexes.Count == 0)
        {
            Valid      = true;
            BypassSave = true;
        }

        foreach (var ai in Indexes)
        {
            if (ai.HomeAddress != null)
            {
                Valid      = true;
                BypassSave = true;
            }
        }

        if (!BypassSave)
        {
            SetExistingIndexInfo(_data, xmlString);
        }
    }

    public WorkXml(IDataServices data,
                   string        xmlString,
                   DateTime      dateReceived,
                   bool          validateAtDocument,
                   bool          validateDeterminationDocument)
    {
        Initialize(data, xmlString, dateReceived, validateAtDocument, validateDeterminationDocument);
    }

    public WorkXml(IDataServices data,
                   string        xmlString,
                   DateTime      dateReceived,
                   bool          validateAtDocument,
                   bool          validateDeterminationDocument,
                   string        awsRegion,
                   bool          suspendS3Writes)
    {
        SuspendS3Writes = suspendS3Writes;
        Initialize(data, xmlString, dateReceived, validateAtDocument, validateDeterminationDocument, awsRegion);
    }

    public bool SuspendS3Writes { get; }

    private void Initialize(IDataServices data,
                            string        xmlString,
                            DateTime      dateReceived,
                            bool          validateAtDocument,
                            bool          validateDeterminationDocument)
    {
        Initialize(data, xmlString, dateReceived, validateAtDocument, validateDeterminationDocument, GlobalAppSettings.Instance.AwsRegion);
    }

    private void Initialize(IDataServices data,
                            string        xmlString,
                            DateTime      dateReceived,
                            bool          validateAtDocument,
                            bool          validateDeterminationDocument,
                            string        awsRegion)
    {
        _data         = data;
        _bucketRegion = RegionEndpoint.GetBySystemName(awsRegion);

        _validateAtDocument            = validateAtDocument;
        _validateDeterminationDocument = validateDeterminationDocument;
        try
        {
            Valid = false;
            Document.LoadXml(xmlString);

            ATXMLHelper.RemoveSOAPEnvelope("//*[local-name()='assess-response']", "//DeterminationsReasons", Document);

            if (ValidateDocument())
            {
                AppParser = new ApplicationTransferParser(Document);
                PopulateApp();
                App.ReceivedDate = dateReceived;
                Valid            = true;
            }
        }
        catch (Exception ex)
        {
            Exception    = ex;
            ErrorMessage = "xmlString could not be parsed into a valid XmlDocument.";
        }
    }

    private void SetExistingIndexInfo(IDataServices data, string xmlString)
    {
        _data = data;

        try
        {
            Valid = false;
            Document.LoadXml(xmlString);

            //ATXMLHelper.RemoveSOAPEnvelope("//*[local-name()='assess-response']", "//DeterminationsReasons", Document);

            if (ValidateDocument())
            {
                AppParser = new ApplicationTransferParser(Document);
                PopulateIndexFields();
                Valid = true;
            }
        }
        catch (Exception ex)
        {
            Exception    = ex;
            ErrorMessage = "xmlString could not be parsed into a valid XmlDocument.";
        }
    }

    public async Task<ApplicationTransfer> SaveAsync(CancellationToken stoppingToken)
    {
        if (App.Source.Equals("WorkerDCT", StringComparison.OrdinalIgnoreCase))
        {
            App.TransferId = App.TransferId[..11] + "W" + _data.ApplicationTransfers.GetNextTransferSequenceNumber(App.Source);
        }

        await _data.ApplicationTransfers.AddAsync(App);
        try
        {
            await _data.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception("Error saving indexes and transitions.", ex);
        }

        try
        {
            if (!SuspendS3Writes)
            {
                App.AtXmlDownloadPath = await ExportToFileAsync(stoppingToken);
                await _data.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error saving AtXml to S3.", ex);
        }

        return App;
    }

    public async Task<ApplicationTransfer> SaveIndexesAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!BypassSave)
            {
                await _data.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error saving indexes and transitions.", ex);
        }

        return App;
    }

    public async Task<ApplicationTransfer> SaveTransferResults(int applicationTransferId, string status, CancellationToken stoppingToken)
    {
        var transferResult = await _data.AwsAtXmlTransfers.FirstOrDefaultAsync(x => x.ApplicationTransferId == applicationTransferId);
        if (transferResult == null)
        {
            var newTransfer = new AwsAtXmlTransfer
                              {
                                  ApplicationTransferId = applicationTransferId,
                                  TransferStatus        = status,
                                  TransferDate          = DateTime.Now,
                                  Message               = ErrorMessage
                              };
            _data.AwsAtXmlTransfers.Add(newTransfer);
            await _data.SaveChanges();
        }
        else
        {
            transferResult.TransferStatus = status;
            transferResult.TransferDate   = DateTime.Now;
            transferResult.Message        = ErrorMessage;
            await _data.SaveChanges();
        }

        return App;
    }

    #region Properties

    public ApplicationTransferParser AppParser { get; private set; }

    public ApplicationTransfer    App         { get; } = new();
    public List<ApplicationIndex> Indexes     { get; } = new();
    public List<Transition>       Transitions { get; } = new();

    public XmlDocument Document { get; } = new();

    public bool RedoStatus =>
        ATXMLHelper.GetSingleNodeValueBool("//*[local-name()='assess-response']/*[local-name()='global-instance']/*[local-name()='o_g_required-verification-data-received']",
                                           true,
                                           Document) ||
        string.IsNullOrWhiteSpace(TransferID);

    public string TransferID => AppParser == null ? string.Empty : AppParser.TransferId;

    public bool      Valid        { get; private set; }
    public bool      BypassSave   { get; }
    public string    ErrorMessage { get; private set; }
    public Exception Exception    { get; private set; }

    private string _atXmlFragment;

    private string ATXMLFragment
    {
        get
        {
            if (string.IsNullOrEmpty(_atXmlFragment))
            {
                _atXmlFragment = Document.SelectSingleNode("//ATXML").OuterXml;
            }

            return _atXmlFragment;
        }
    }

    private string _determinationsReasonsFragment;

    private string DeterminationsReasonsFragment
    {
        get
        {
            if (string.IsNullOrEmpty(_determinationsReasonsFragment))
            {
                _determinationsReasonsFragment = Document.SelectSingleNode("//*[local-name()='assess-response']").OuterXml;
            }

            return _determinationsReasonsFragment;
        }
    }

    #endregion

    #region Helper Methods

    private async Task<string> ExportToFileAsync(CancellationToken stoppingToken)
    {
        var relativePath = CalculateTransferPath(App.CreatedOn);
        var path         = $"{relativePath}";
        var xmlName      = $"{App.ApplicationTransferId}.xml";
        var fileName     = $"{App.ApplicationTransferId}.zip";
        var finalPath    = $"{path}/{fileName}";
        try
        {
            using var streamToUpload = new MemoryStream(await Document.OuterXml.ZipAsync(xmlName));

            var bucketName = Environment.GetEnvironmentVariable("ATXML_BUCKET");
            await AwsClientHelper.AwsTransferUtility.UploadAsync(streamToUpload, bucketName, finalPath, stoppingToken);
        }
        catch (Exception ex)
        {
            throw new Exception($"Could not export file {finalPath}.", ex);
        }

        return finalPath;
    }

    private string CalculateTransferPath(DateTime createdOn)
    {
        return $"{createdOn.Year:D4}/{createdOn.Month:D2}/{createdOn.Day:D2}/{createdOn.Hour:D2}";
    }

    private bool ValidateDocument()
    {
        var valid = true;

        if (_validateAtDocument)
        {
            valid = ValidateATXMLXSD();
        }

        if (valid && _validateDeterminationDocument)
        {
            valid = ValidateDeterminationsXSD();
        }

        return valid;
    }

    /// <summary>
    ///     This method check the ATXML part against the xsd
    /// </summary>
    /// <returns></returns>
    private bool ValidateATXMLXSD()
    {
        return ValidateXmlFragment(@"XMLSchema\AT-062113\XMLSchemas\ExchangeModel.xsd", ATXMLFragment);
    }

    /// <summary>
    ///     This method will validate the DeterminationsReasons section against the XSD
    /// </summary>
    /// <returns></returns>
    private bool ValidateDeterminationsXSD()
    {
        return ValidateXmlFragment(@"XMLSchema\AssessResponse_R18.03\AssessResponse_R18.03-Build1.xsd", DeterminationsReasonsFragment);
    }

    /// <summary>
    ///     This method check the xml fragment part against the xsd
    /// </summary>
    /// <returns></returns>
    private bool ValidateXmlFragment(string schemaPath, string xmlFragment)
    {
        var isValid = true;

        TextReader tr       = null;
        XmlReader  myReader = null;
        try
        {
            var settings = new XmlReaderSettings
                           {
                               ValidationType = ValidationType.Schema
                           };
            var xsdFilePath = Path.Combine(AppContext.BaseDirectory, schemaPath);
            settings.Schemas.Add(null, xsdFilePath);
            tr = new StringReader(xmlFragment);

            using (myReader = XmlReader.Create(tr, settings))
            {
                while (myReader.Read())
                {
                    ;
                }
            }
        }
        catch (Exception e)
        {
            ErrorMessage = "FAILED XSD VALIDATION: " + e.Message;
            Exception    = e;
            Valid        = false;
            isValid      = false;
        }
        finally
        {
            tr?.Close();
            myReader?.Close();
        }

        return isValid;
    }

    private void PopulateApp()
    {
        var nowMountain = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time"));
        AppParser.CopyPropertiesTo(App);
        App.CreatedOn = nowMountain;

        var atExtract = new ATExtract(Document);
        //App.ApplicationTransferXml = new ApplicationTransferXml
        //                             {
        //                                 Atxmlbin  = atExtract.ToATXmlBin(),
        //                                 CreatedOn = App.CreatedOn
        //                             };
        foreach (var indexParser in AppParser.ApplicationIndices)
        {
            var index = new ApplicationIndex();
            Indexes.Add(index);
            indexParser.CopyPropertiesTo(index);
            foreach (var personDenialReasonParser in indexParser.PersonDenialReasons)
            {
                var reasonCodes = personDenialReasonParser.ReasonCode.Split(',');
                foreach (var reasonCode in reasonCodes)
                {
                    if (!string.IsNullOrEmpty(reasonCode))
                    {
                        var denialReason =
                            _data.DenialReasons.FirstOrDefault(d => d.ProgramCode.ToUpper() == personDenialReasonParser.ProgramCode.ToUpper() &&
                                                                    d.ReasonCode.ToUpper()  == reasonCode.ToUpper()) ??
                            new DenialReason
                            {
                                ProgramCode             = personDenialReasonParser.ProgramCode,
                                ReasonCode              = reasonCode,
                                IneligibilityReason     = string.Empty,
                                IneligibilityReasonText = string.Empty,
                                CreatedBy               = "DEGProcessor",
                                CreatedOn               = nowMountain
                            };
                        index.DenialReasons.Add(denialReason);
                    }
                }
            }

            App.ApplicationIndices.Add(index);
        }

        foreach (var transitionParser in AppParser.Transitions)
        {
            var transition = new Transition
                             {
                                 StatusCode = transitionParser.StatusCode
                             };
            Transitions.Add(transition);
            transitionParser.CopyPropertiesTo(transition);
            App.Transitions.Add(transition);
        }

        // Set the Head of Household
        if (!Indexes.Any(i => i.Hoh))
        {
            var hoh = Indexes.MaxBy(i => i.Dob);
            if (hoh != null)
            {
                hoh.Hoh = true;
            }
        }
    }

    private void PopulateIndexFields()
    {
        var nowMountain = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time"));
        try
        {
            foreach (var indexParser in AppParser.ApplicationIndices)
            {
                foreach (var ai in App.ApplicationIndices)
                {
                    if (indexParser.PersonId == ai.PersonId)
                    {
                        indexParser.CopyIndexPropertiesTo(ai);
                        ai.ModifiedOn = nowMountain;
                        foreach (var personDenialReasonParser in indexParser.PersonDenialReasons)
                        {
                            var reasonCodes = personDenialReasonParser.ReasonCode.Split(',');
                            foreach (var reasonCode in reasonCodes)
                            {
                                if (!string.IsNullOrEmpty(reasonCode))
                                {
                                    var denialReason =
                                        _data.DenialReasons.FirstOrDefault(d => d.ProgramCode.ToUpper() ==
                                                                                personDenialReasonParser.ProgramCode.ToUpper() &&
                                                                                d.ReasonCode.ToUpper() == reasonCode.ToUpper()) ??
                                        new DenialReason
                                        {
                                            ProgramCode             = personDenialReasonParser.ProgramCode,
                                            ReasonCode              = reasonCode,
                                            IneligibilityReason     = string.Empty,
                                            IneligibilityReasonText = string.Empty,
                                            CreatedBy               = "DEGProcessor",
                                            CreatedOn               = nowMountain
                                        };
                                    var addReason = true;
                                    foreach (var reason in ai.DenialReasons)
                                    {
                                        if (reason.ReasonCode == reasonCode)
                                        {
                                            addReason = false;
                                        }
                                    }

                                    if (addReason)
                                    {
                                        ai.DenialReasons.Add(denialReason);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error Populating Fields", ex);
        }
    }

    #endregion
}