using System.Net;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using IGS.DataServices;
using IGS.Models.Entities;
using MI.Common.Configuration;
using MI.Common.Helper;
using MI.Common.Models;
using MI.DEGProcessor.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ML.Models;
using ML.Models.Enums;
using NLog;

namespace MI.DEGProcessor.Controllers;

[Route("[controller]/[action]")]
[AllowAnonymous]
[ApiController]
public class DEGController : ControllerBase
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    [HttpGet]
    public async Task<HttpResponseMessage> TestGet()
    {
        _logger.Info("TestGet Ran");
        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    [HttpGet]
    public async Task<HttpResponseMessage> RetryBackfillMessagesAsync(string            region,
                                                                      string            environment,
                                                                      CancellationToken token)
    {
        var awsRegion = RegionEndpoint.GetBySystemName(region);
        var awsEnv    = environment.ToLower();
        var client    = new AmazonSQSClient(awsRegion);
        var request = new StartMessageMoveTaskRequest
                      {
                          SourceArn =
                              $"arn:aws:sqs:us-east-1:936161934601:xml-backfill-{awsEnv}-dlq",
                          DestinationArn =
                              $"arn:aws:sqs:us-east-1:936161934601:xml-backfill-{awsEnv}"
                      };

        var response = await client.StartMessageMoveTaskAsync(request, token);

        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    [HttpGet]
    public async Task<HttpResponseMessage> TestSaveAtXml()
    {
        return await SaveAtXmlToDatabaseAsync(new AWSBackfillReceivedNotification
                                              {
                                                  ApplicationTransferId = 201298,
                                                  S3Path =
                                                      "dev-major/atxml/2022/10/05/15/201298.zip",
                                                  AwsRegion    = "us-east-1",
                                                  ReceivedDate = DateTimeOffset.Now
                                              });
    }

    [HttpPost]
    public async Task<HttpResponseMessage> SaveAtXmlToDatabaseAsync(
        [FromBody] AWSBackfillReceivedNotification recordToUpdate)
    {
        try
        {
            var result =
                await ATXMLHelper.SaveAtXmlToDatabaseAsync(recordToUpdate.S3Path, recordToUpdate.ApplicationTransferId);

            switch (result)
            {
                case Enums.SaveAtXmlToDatabaseResult.DatabaseError:

                    throw new Exception("Error saving xml to database.");
                    break;
                default:
                    return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Error saving ApplicationTransferId {recordToUpdate.ApplicationTransferId}.");
            throw new Exception("Error saving ApplicationTransferId {recordToUpdate.ApplicationTransferId}.", ex);
        }
    }

    [HttpPost]
    public HttpResponseMessage ProcessFromDegServiceAsync([FromBody] DEGRecordReceivedNotification recordReceived)
    {
        _logger.Info($"recordReceived.StagingAtXmlId: {recordReceived.StagingAtXmlId}");
        try
        {
            var result = ProcessStagingAtXmlRecord(recordReceived.StagingAtXmlId);
            switch (result)
            {
                case ProcessingResult.Success:
                    _logger.Info($"Processed StagingAtXmlId {recordReceived.StagingAtXmlId}");
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                                   {
                                       Content =
                                           new
                                               StringContent($"Processed StagingAtXmlId {recordReceived.StagingAtXmlId}")
                                   };
                    return response;
                case ProcessingResult.Duplicate:
                    _logger.Info($"Skipping StagingAtXml record because already processed. StagingAtXmlId: {recordReceived.StagingAtXmlId}");
                    break;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing record {recordReceived.StagingAtXmlId}. Message: {ex.Message} - StackTrace: {ex.StackTrace}",
                          ex);
            throw new Exception($"Error processing record {recordReceived.StagingAtXmlId}.", ex);
            //var response = new HttpResponseMessage(HttpStatusCode.NotAcceptable)
            //               {
            //                   ReasonPhrase = ex.Message,
            //                   Content =
            //                       new
            //                           StringContent($"Error processing record {recordReceived.StagingAtXmlId}.")
            //               };
            //return response;
        }
    }

    private ProcessingResult ProcessStagingAtXmlRecord(int stagingAtXmlid)
    {
        var data =
            new DataServices(SqlHelper.EnsureTrustedServerCertificate(GlobalConnectionStrings.Instance.MIACA_IGS));
        var degRecord = data.StagingAtxmls.FirstOrDefault(s => s.StagingAtxmlid == stagingAtXmlid);

        // Check to see if the record was already processed
        if (degRecord.Status.Equals("RECEIVED", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info("Processing record:" + Environment.NewLine + degRecord.WriteDetails(),
                         new NLogExt { AcaId = degRecord.ApplicationId },
                         LogType.Workflow);

            var                 succeeded    = ProcessingResult.Success;
            ApplicationTransfer info         = null;
            var                 errorMessage = string.Empty;

            try
            {
                var workXml = new WorkXml(degRecord.Atxml,
                                          degRecord.CreatedOn,
                                          AppSettings.Instance.SwitchATXSDValidation,
                                          AppSettings.Instance.SwitchDeterminationXSDValidation);

                try
                {
                    if (workXml.Valid)
                    {
                        info = workXml.SaveAsync(new CancellationToken()).Result;
                    }
                    else
                    {
                        errorMessage = "Error processing XML: " +
                                       workXml.ErrorMessage     +
                                       Environment.NewLine      +
                                       degRecord.WriteDetails();
                        _logger.Error(workXml.Exception,
                                      errorMessage,
                                      new NLogExt
                                      {
                                          AcaId = degRecord.ApplicationId
                                      },
                                      LogType.Workflow);
                        succeeded = ProcessingResult.Error;
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = "Unknown Exception on AT Save." + Environment.NewLine + degRecord.WriteDetails();
                    _logger.Error(ex, errorMessage, new NLogExt { AcaId = degRecord.ApplicationId }, LogType.Workflow);
                    succeeded = ProcessingResult.Error;
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Error parsing xml.";
                _logger.Error(ex, errorMessage);
                succeeded = ProcessingResult.Error;
            }

            if (succeeded == ProcessingResult.Success)
            {
                degRecord.Status                = "PROCESSED";
                degRecord.ApplicationTransferId = info.ApplicationTransferId;
                degRecord.ErrorMessage          = string.Empty;
                degRecord.Atxml                 = string.Empty;
            }
            else
            {
                degRecord.Status       = "FAILED_PROCESSING";
                degRecord.ErrorMessage = errorMessage;
            }

            try
            {
                var saveTask = data.SaveChanges();
                saveTask.Wait();

                if (saveTask.Status == TaskStatus.Faulted)
                {
                    throw new Exception("Exception saving deg record.", saveTask.Exception);
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Exception updating Staging Table record:" +
                               Environment.NewLine                        +
                               degRecord.WriteDetails();
                errorMessage += "\n  Status: " + degRecord.Status;
                errorMessage += "\n  ApplicationTransferID: " +
                                (info == null ? string.Empty : info.ApplicationTransferId.ToString());
                errorMessage += "\n  ATXMLError: " + degRecord.ErrorMessage;
                _logger.Error(ex, errorMessage);
            }

            return succeeded;
        }

        return ProcessingResult.Duplicate;
    }

    private enum ProcessingResult
    {
        Success,
        Error,
        Duplicate
    }
}