using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IGS.DataServices;
using IGS.Models.Entities;
using MI.Common.Configuration;
using MI.Common.Helper;
using MI.DEGProcessor.Helpers;
using Microsoft.Extensions.Hosting;
using ML.Common.Service;
using ML.Models;
using ML.Models.Entities;
using NLog;
using Environment = System.Environment;
using LogType = ML.Models.Enums.LogType;
using PerformanceMetricType = ML.Models.Enums.PerformanceMetricType;

namespace MI.DEGProcessor;

public class DEGProcessorService : BackgroundService
{
	private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

	private readonly Guid _lockingGuid;

	public DEGProcessorService()
	{
		using var md5   = MD5.Create();
		var       hash1 = md5.ComputeHash(Encoding.Default.GetBytes(Environment.MachineName));
		_lockingGuid = new Guid(hash1);

		////testing: delete when done
		//var path = "dev-major/atxml/2018/04/06/15/23.zip";
		//var id = 23;
		//var rc = ATXMLHelper.SaveAtXmlToDatabase(path, id);
		//rc.Wait();
	}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var isActive = await ApplicationActiveService.IsApplicationActiveAsync(stoppingToken);
				if (isActive)
				{
					try
					{
						var processed = await ProcessDEGRecordsAsync(stoppingToken);
						if (processed > 0)
						{
							var nLogExt = new NLogExt();
							nLogExt.PerformanceMetrics.Add(new PerformanceMetric
														   {
															   Name  = "RecordsProcessed",
															   Value = processed.ToString(),
															   PerformanceMetricTypeId =
																   (int)PerformanceMetricType
																	  .RecordsProcessed
														   });
							_logger.Info("Processed " + processed + " record(s).", nLogExt, LogType.PerformanceMetric);
						}
						else
						{
							_logger.Debug("Processed " + processed + " record(s).");
						}
					}
					catch (TaskCanceledException tce)
					{
						_logger.Info($"Task was cancelled... {tce.Message}");
					}
					catch (Exception ex)
					{
						_logger.Error(ex, "Unknown error processing DEG records.");
					}
				}

				if (!stoppingToken.IsCancellationRequested)
				{
					await Task.Delay(AppSettings.Instance.DelayMilliseconds, stoppingToken);
				}
			}
			catch (TaskCanceledException tce)
			{
				_logger.Info($"Task was cancelled... {tce.Message}");
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Unknown error processing DEG records.");
				await Task.Delay(AppSettings.Instance.DelayMilliseconds, stoppingToken);
			}
		}
	}

	private async Task<int> ProcessDEGRecordsAsync(CancellationToken stoppingToken)
	{
		var processed    = 0;
		var recordsFound = true;

		while (recordsFound && !stoppingToken.IsCancellationRequested)
		{
			var data1 = new DataServices(SqlHelper.EnsureTrustedServerCertificate(GlobalConnectionStrings
																				 .Instance.MIACA_IGS));
			var degRecords = await GetNextUnprocessedRecordsAsync(data1, stoppingToken);

			if (degRecords.Count > 0)
			{
				TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
				ParallelOptions parallelOptions = new()
												  {
													  MaxDegreeOfParallelism =
														  AppSettings.Instance.DEGProcessorThreads,
													  CancellationToken = stoppingToken
												  };
				await Parallel.ForEachAsync(degRecords.Select(r => r.StagingAtxmlid),
											parallelOptions,
											async (stagingAtxmlId, token) =>
											{
												var data =
													new DataServices(SqlHelper
																		.EnsureTrustedServerCertificate(GlobalConnectionStrings
																									   .Instance
																									   .MIACA_IGS));
												var degRecord =
													await data.StagingAtxmls
															  .FirstOrDefaultAsync(s => s.StagingAtxmlid ==
																						stagingAtxmlId);
												if (processed == 0)
												{
													_logger.Info("DEGProcessor Starting Batch...");
												}

												_logger.Info("Processing record:" +
															 Environment.NewLine  +
															 degRecord.WriteDetails(),
															 new NLogExt { AcaId = degRecord.ApplicationId },
															 LogType.Workflow);

												var                 succeeded    = true;
												ApplicationTransfer info         = null;
												var                 errorMessage = string.Empty;

												try
												{
													var workXml = new WorkXml(degRecord.Atxml,
																			  degRecord.CreatedOn,
																			  AppSettings.Instance
																						 .SwitchATXSDValidation,
																			  AppSettings.Instance
																						 .SwitchDeterminationXSDValidation);

													try
													{
														if (workXml.Valid)
														{
															info = await workXml.SaveAsync(token);
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
																			  AcaId = degRecord
																				 .ApplicationId
																		  },
																		  LogType.Workflow);
															succeeded = false;
														}
													}
													catch (Exception ex)
													{
														errorMessage =
															"Unknown Exception on AT Save." +
															Environment.NewLine             +
															degRecord.WriteDetails();
														_logger.Error(ex,
																	  errorMessage,
																	  new NLogExt { AcaId = degRecord.ApplicationId },
																	  LogType.Workflow);
														succeeded = false;
													}
												}
												catch (Exception ex)
												{
													errorMessage = "Error parsing xml.";
													_logger.Error(ex, errorMessage);
													succeeded = false;
												}

												if (succeeded)
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
													await saveTask.WaitAsync(token);

													if (saveTask.Status == TaskStatus.Faulted)
													{
														throw new Exception("Exception saving deg record.",
																			saveTask.Exception);
													}
												}
												catch (Exception ex)
												{
													errorMessage = "Exception updating Staging Table record:" +
																   Environment.NewLine                        +
																   degRecord.WriteDetails();
													errorMessage += "\n  Status: " + degRecord.Status;
													errorMessage += "\n  ApplicationTransferID: " +
																	(info == null
																		 ? string.Empty
																		 : info.ApplicationTransferId.ToString());
													errorMessage += "\n  ATXMLError: " + degRecord.ErrorMessage;
													_logger.Error(ex, errorMessage);
												}

												processed++;
											});
			}
			else
			{
				recordsFound = false;
			}
		}

		return processed;
	}
	//private async Task<int> ProcessDEGRecordsAsync(CancellationToken stoppingToken)
	//{
	//    var processed   = 0;
	//    var recordFound = true;

	//    while (recordFound && !stoppingToken.IsCancellationRequested)
	//    {
	//        var data = new DataServices(SqlHelper.EnsureTrustedServerCertificate(GlobalConnectionStrings
	//                                                                            .Instance.MIACA_IGS));
	//        var degRecord = await GetNextUnprocessedRecordAsync(data, stoppingToken);

	//        if (degRecord is { StagingAtxmlid: > 0 })
	//        {
	//            if (processed == 0)
	//            {
	//                _logger.Info("DEGProcessor Starting Batch...");
	//            }

	//            _logger.Info("Processing record:" + Environment.NewLine + degRecord.WriteDetails(),
	//                         new NLogExt { AcaId = degRecord.ApplicationId },
	//                         LogType.Workflow);

	//            var                 succeeded    = true;
	//            ApplicationTransfer info         = null;
	//            var                 errorMessage = string.Empty;

	//            try
	//            {
	//                var workXml = new WorkXml(degRecord.Atxml,
	//                                          degRecord.CreatedOn,
	//                                          AppSettings.Instance.SwitchATXSDValidation,
	//                                          AppSettings.Instance.SwitchDeterminationXSDValidation);

	//                try
	//                {
	//                    if (workXml.Valid)
	//                    {
	//                        info = await workXml.SaveAsync(stoppingToken);
	//                    }
	//                    else
	//                    {
	//                        errorMessage = "Error processing XML: " +
	//                                       workXml.ErrorMessage     +
	//                                       Environment.NewLine      +
	//                                       degRecord.WriteDetails();
	//                        _logger.Error(workXml.Exception,
	//                                      errorMessage,
	//                                      new NLogExt { AcaId = degRecord.ApplicationId },
	//                                      LogType.Workflow);
	//                        succeeded = false;
	//                    }
	//                }
	//                catch (Exception ex)
	//                {
	//                    errorMessage = "Unknown Exception on AT Save." + Environment.NewLine + degRecord.WriteDetails();
	//                    _logger.Error(ex,
	//                                  errorMessage,
	//                                  new NLogExt { AcaId = degRecord.ApplicationId },
	//                                  LogType.Workflow);
	//                    succeeded = false;
	//                }
	//            }
	//            catch (Exception ex)
	//            {
	//                errorMessage = "Error parsing xml.";
	//                _logger.Error(ex, errorMessage);
	//                succeeded = false;
	//            }

	//            if (succeeded)
	//            {
	//                degRecord.Status                = "PROCESSED";
	//                degRecord.ApplicationTransferId = info.ApplicationTransferId;
	//                degRecord.ErrorMessage          = string.Empty;
	//                degRecord.Atxml                 = string.Empty;
	//            }
	//            else
	//            {
	//                degRecord.Status       = "FAILED_PROCESSING";
	//                degRecord.ErrorMessage = errorMessage;
	//            }

	//            try
	//            {
	//                var saveTask = data.SaveChanges();
	//                await saveTask.WaitAsync(stoppingToken);

	//                if (saveTask.Status == TaskStatus.Faulted)
	//                {
	//                    throw new Exception("Exception saving deg record.", saveTask.Exception);
	//                }
	//            }
	//            catch (Exception ex)
	//            {
	//                errorMessage = "Exception updating Staging Table record:" +
	//                               Environment.NewLine                        +
	//                               degRecord.WriteDetails();
	//                errorMessage += "\n  Status: " + degRecord.Status;
	//                errorMessage += "\n  ApplicationTransferID: " +
	//                                (info == null ? string.Empty : info.ApplicationTransferId.ToString());
	//                errorMessage += "\n  ATXMLError: " + degRecord.ErrorMessage;
	//                _logger.Error(ex, errorMessage);
	//            }

	//            processed++;
	//        }
	//        else
	//        {
	//            recordFound = false;
	//        }
	//    }

	//    return processed;
	//}

	private async Task<StagingAtxml> GetNextUnprocessedRecordAsync(DataServices data, CancellationToken stoppingToken)
	{
		StagingAtxml at = null;
		try
		{
			at = await data.StagingAtxmls.GetNextRecordAsync(_lockingGuid, stoppingToken);
		}
		catch (Exception ex)
		{
			_logger.Error(ex, "Error retrieving next record to be processed.");
		}

		return at;
	}

	private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
	{
		_logger.Error(e.Exception, "Unobserved exception in Parallel.ForEach.");
		e.SetObserved();
	}

	private async Task<List<StagingAtxml>> GetNextUnprocessedRecordsAsync(
		DataServices      data,
		CancellationToken stoppingToken)
	{
		var ats = new List<StagingAtxml>();
		try
		{
			ats = await data.StagingAtxmls.GetNextRecordsAsync(_lockingGuid,
															   AppSettings.Instance.DEGProcessorBatchSize,
															   stoppingToken);
		}
		catch (Exception ex)
		{
			_logger.Error(ex, "Error retrieving next batch to be processed.");
		}

		return ats;
	}
}