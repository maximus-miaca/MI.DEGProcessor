using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using MI.Common.Configuration;
using MI.Common.Extensions;
using MI.Common.Helper;
using MI.DEGProcessor.Helpers;
using MI.DEGProcessor.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using ML.Common.Helper;
using ML.Common.Service;
using ML.Models.OtherModels;
using Newtonsoft.Json;
using NLog;

namespace MI.DEGProcessor;

public class AwsAtXmlTransferService : BackgroundService
{
	private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

	private readonly string             _baseConnectionString;
	private readonly string             _bucketName;
	private readonly RegionEndpoint     _bucketRegion;
	private readonly Guid               _lockingGuid;
	private readonly PerformanceTracker _trackerBatchRetrieve;
	private readonly PerformanceTracker _trackerBatchSet;
	private readonly PerformanceTracker _trackerProcess;

	private int _lastThreadCount;

	public AwsAtXmlTransferService()
	{
		_trackerBatchSet = new PerformanceTracker("S3 Archive Batch Set",
												  new List<PerformanceTrackerTask>
												  {
													  new(0, "00. Set Batch")
												  });
		_trackerBatchRetrieve = new PerformanceTracker("S3 Archive Batch Retrieval",
													   new List<PerformanceTrackerTask>
													   {
														   new(0, "00. Retrieve Batch")
													   });
		_trackerProcess = new PerformanceTracker("S3 Archive Process",
												 new List<PerformanceTrackerTask>
												 {
													 new(0, "00. Get Record"),
													 new(1, "01. Save to S3"),
													 new(2, "02. Update path")
												 });

		_bucketName   = GlobalAppSettings.Instance.AtXmlS3BucketName;
		_bucketRegion = RegionEndpoint.GetBySystemName(GlobalAppSettings.Instance.AwsRegion);

		_baseConnectionString = SqlHelper.EnsureTrustedServerCertificate(GlobalConnectionStrings.Instance.MIACA_IGS);
		_lockingGuid          = CalculateLockingGuid(Environment.MachineName);
	}

	private async Task<BucketAccelerateStatus> CheckS3Acceleration(AmazonS3Client s3Client, string bucketName)
	{
		var ret = BucketAccelerateStatus.Suspended;

		try
		{
			var getRequest = new GetBucketAccelerateConfigurationRequest
							 {
								 BucketName = bucketName
							 };
			var response = await s3Client.GetBucketAccelerateConfigurationAsync(getRequest);
			ret = response.Status;

			_logger.Info($"Acceleration state for S3 bucket {bucketName} = '{response.Status}' ");
		}
		catch (AmazonS3Exception amazonS3Exception)
		{
			_logger.Error(amazonS3Exception,
						  $"Error occurred retrieving S3 acceleration status for bucket {bucketName}");
		}
		catch (Exception ex)
		{
			_logger.Error(ex, $"Unexpected error occurred retrieving S3 acceleration status for bucket {bucketName}");
		}

		return ret;
	}

	private Guid CalculateLockingGuid(string name)
	{
		using var md5   = MD5.Create();
		var       hash1 = md5.ComputeHash(Encoding.Default.GetBytes(name));
		return new Guid(hash1);
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
					_logger.Info("AtXml Transfer Process Started.");
					try
					{
						await ProcessRecordsAsync(stoppingToken, _bucketName);
					}
					catch (TaskCanceledException tce)
					{
						_logger.Info($"Task was cancelled... {tce.Message}");
					}
					catch (Exception ex)
					{
						_logger.Error(ex, "Unknown error transfering ATXml records.");
					}
				}

				if (!stoppingToken.IsCancellationRequested)
				{
					await Task.Delay(120000, stoppingToken);
				}
			}
			catch (TaskCanceledException tce)
			{
				_logger.Info($"Task was cancelled... {tce.Message}");
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Unknown error transfering ATXml records.");
				await Task.Delay(120000, stoppingToken);
			}
		}

		_logger.Info("Exiting ExecuteAsync()");
	}

	private async Task ProcessRecordsAsync(CancellationToken stoppingToken, string bucketName)
	{
		try
		{
			var recordsProcessed = false;
			// Transfer any files left over from last run
			await TransferFilesAsync(_lockingGuid, stoppingToken, bucketName);
			do
			{
				if (AppSettings.Instance.GetAtXmlS3TransferServers()
							   .Any(s => s.Key.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)))
				{
					_trackerBatchSet.StartStep(0);
					var count = await LockApplicationTransferRecordsAsync(_lockingGuid, stoppingToken);
					_logger.Info($"Locked {count} records to be transferred.");
					recordsProcessed = count is > 0;
					_trackerBatchSet.EndStep(0);
					//_trackerBatchSet.LogPerformanceMetric(_logger);

					var transferred = await TransferFilesAsync(_lockingGuid, stoppingToken, bucketName);
					_logger.Info($"Transferred {transferred} records.");
					recordsProcessed |= transferred > 0;
				}
				else
				{
					recordsProcessed = false;
					_logger.Info($"Stopping S3 Transfer for {Environment.MachineName}, not found in AtXmlS3TransferServers list.");
					while (!recordsProcessed)
					{
						await Task.Delay(AppSettings.Instance.DelayMilliseconds, stoppingToken);
						if (AppSettings.Instance.GetAtXmlS3TransferServers()
									   .Any(s => s.Key.Equals(Environment.MachineName,
															  StringComparison.OrdinalIgnoreCase)))
						{
							recordsProcessed = true;
							_logger.Info($"Starting S3 Transfer for {Environment.MachineName}.");
						}
					}
				}
			} while (recordsProcessed && !stoppingToken.IsCancellationRequested);
		}
		catch (Exception ex)
		{
			_logger.Error(ex, "Unhandled exception in ProcessRecordsAsync.");
		}
	}

	private async Task<int> TransferFilesAsync(Guid lockingGuid, CancellationToken stoppingToken, string bucketName)
	{
		var count = 0;
		_trackerBatchRetrieve.StartStep(0);
		var recordsToTransfer = await GetRecordsToTransferAsync(lockingGuid, stoppingToken);
		_trackerBatchRetrieve.EndStep(0);
		//_trackerBatchRetrieve.LogPerformanceMetric(_logger);
		while (recordsToTransfer.Count > 0 && !stoppingToken.IsCancellationRequested)
		{
			var server = AppSettings.Instance.GetAtXmlS3TransferServers()
									.FirstOrDefault(s => s.Key.Equals(Environment.MachineName,
																	  StringComparison.OrdinalIgnoreCase));
			var threads = string.IsNullOrEmpty(server.Key) ? 1 : server.Value;
			if (threads != _lastThreadCount)
			{
				_logger.Info($"S3 Transfer thread count changed from {_lastThreadCount} to {threads}");
				_lastThreadCount = threads;
			}

			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
			ParallelOptions parallelOptions = new()
											  {
												  MaxDegreeOfParallelism = threads,
												  CancellationToken      = stoppingToken
											  };
			try
			{
				await Parallel.ForEachAsync(recordsToTransfer,
											parallelOptions,
											async (record, token) =>
											{
												//foreach (var record in recordsToTransfer)
												//{
												var attempt     = 0;
												var lastAttempt = DateTime.MinValue;
												if (record.TransferStatus == TransferStatus.TryAgain)
												{
													var parts = record.Message.Split('|');
													attempt     = int.Parse(parts[0]);
													lastAttempt = DateTime.Parse(parts[1]);
												}
												else
												{
													_trackerProcess.StartStep(0);
													//await GetAtXmlAsync(record, token);
													_trackerProcess.EndStep(0);
												}

												try
												{
													if (record.TransferStatus == TransferStatus.Retrieved ||
														(record.TransferStatus == TransferStatus.TryAgain &&
														 lastAttempt <=
														 DateTime.UtcNow.AddSeconds(0 - 5 * (2 ^ attempt))))
													{
														count++;
														record.AtXml = await record.AtXmlBin.DecompressAsync(token);
														_trackerProcess.StartStep(1);
														await ExportToFileAsync(record, token, bucketName);
														_trackerProcess.EndStep(1);
														_trackerProcess.StartStep(2);
														if (record.TransferStatus == TransferStatus.Exported)
														{
															await RecordPathAsync(record, token);
														}
													}
												}
												catch (Exception ex)
												{
													record.TransferStatus =
														++attempt >= 10
															? TransferStatus.Error
															: TransferStatus.TryAgain;
													record.Message =
														$"{attempt + 1}|{DateTime.UtcNow}|Error processing ATXMLbin. ApplicationTransferId: {record.ApplicationTransferId}";
													_logger.Error(ex, record.Message);
												}

												await RecordResultsAsync(record, token);
												_trackerProcess.EndStep(2);
												//_trackerProcess.LogPerformanceMetric(_logger);
												//if (stoppingToken.IsCancellationRequested)
												//{
												//    return count;
												//}
												//}
											});
			}
			catch (AggregateException ae)
			{
				_logger.Error($"{ae.InnerExceptions.Count} Aggregate Exceptions Occurred: ");
				foreach (var e in ae.InnerExceptions)
				{
					_logger.Error(e, "Unhandled exception");
				}
			}

			_trackerBatchRetrieve.StartStep(0);
			recordsToTransfer = await GetRecordsToTransferAsync(lockingGuid, stoppingToken);
			_trackerBatchRetrieve.EndStep(0);
			//_trackerBatchRetrieve.LogPerformanceMetric(_logger);
		}

		return count;
	}

	private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
	{
		_logger.Error(e.Exception, "Unobserved exception in Parallel.ForEach.");
		e.SetObserved();
	}

	private async Task RecordPathAsync(AwsAtXmlTransferRecord record, CancellationToken stoppingToken)
	{
		var con = new SqlConnection(_baseConnectionString);
		var sql = @"UPDATE ApplicationTransfer
                    SET
	                    AtXmlDownloadPath = @FinalPath
                    WHERE
	                    ApplicationTransferID = @ApplicationTransferId";
		var cmd = con.CreateCommand();
		cmd.CommandText = sql;
		cmd.CommandType = CommandType.Text;
		cmd.Parameters.AddWithValue("@FinalPath",             record.FinalPath);
		cmd.Parameters.AddWithValue("@ApplicationTransferId", record.ApplicationTransferId);

		try
		{
			await con.OpenAsync(stoppingToken);
			await cmd.ExecuteNonQueryAsync(stoppingToken);
			record.TransferStatus = TransferStatus.Complete;
		}
		catch (Exception ex)
		{
			throw new Exception($"Path could not be stored. Path: {record.FinalPath}", ex);
		}
		finally
		{
			await con.CloseAsync();
		}
	}

	private async Task RecordResultsAsync(AwsAtXmlTransferRecord record, CancellationToken stoppingToken)
	{
		var con = new SqlConnection(_baseConnectionString);
		var sql = @"UPDATE AwsAtXmlTransfer
                    SET
	                    TransferStatus = @TransferStatus,
                        TransferDate = SYSDATETIMEOFFSET(),
	                    [Message] = @Message
                    WHERE
	                    ApplicationTransferID = @ApplicationTransferId";
		var cmd = con.CreateCommand();
		cmd.CommandText = sql;
		cmd.CommandType = CommandType.Text;
		cmd.Parameters.AddWithValue("@TransferStatus", record.TransferStatus);
		cmd.Parameters.Add("@Message", SqlDbType.VarChar, -1).Value =
			string.IsNullOrEmpty(record.Message) ? DBNull.Value : record.Message;
		cmd.Parameters.AddWithValue("@ApplicationTransferId", record.ApplicationTransferId);

		try
		{
			await con.OpenAsync(stoppingToken);
			await cmd.ExecuteNonQueryAsync(stoppingToken);
		}
		catch (Exception ex)
		{
			string recordJson;
			try
			{
				recordJson = JsonConvert.SerializeObject(record);
			}
			catch
			{
				recordJson = record.ApplicationTransferId.ToString();
			}

			_logger.Error(ex, $"Error updating transfer status value. {recordJson}");
		}
		finally
		{
			await con.CloseAsync();
		}
	}

	private async Task ExportToFileAsync(AwsAtXmlTransferRecord record,
										 CancellationToken      stoppingToken,
										 string                 bucketName)
	{
		var relativePath = CalculateTransferPath(record.CreatedOn);
		var path         = $"{GlobalAppSettings.Instance.AtXmlRootPath}{relativePath}";
		var xmlName      = $"{record.ApplicationTransferId}.xml";
		var fileName     = $"{record.ApplicationTransferId}.zip";
		record.FinalPath = $"{path}/{fileName}";
		try
		{
			using var streamToUpload = new MemoryStream(await record.AtXml.ZipAsync(xmlName));

			var s3Client = new AmazonS3Client(_bucketRegion);
			//var status   = CheckS3Acceleration(s3Client, _bucketName).Result;
			//if (status == BucketAccelerateStatus.Enabled)
			//{
			//    s3Client = new AmazonS3Client(new AmazonS3Config
			//                                  { RegionEndpoint = _bucketRegion, UseAccelerateEndpoint = true });
			//}

			var fileTransferUtility = new TransferUtility(s3Client);
			await fileTransferUtility.UploadAsync(streamToUpload, bucketName, record.FinalPath, stoppingToken);
			record.TransferStatus = "Exported";
		}
		catch (Exception ex)
		{
			throw new Exception($"Could not export file {record.FinalPath}.", ex);
		}

		record.AtXml    = string.Empty;
		record.AtXmlBin = null;
	}

	private string CalculateTransferPath(DateTime createdOn)
	{
		return $"{createdOn.Year:D4}/{createdOn.Month:D2}/{createdOn.Day:D2}/{createdOn.Hour:D2}";
	}

	//private async Task GetAtXmlAsync(AwsAtXmlTransferRecord record, CancellationToken stoppingToken)
	//{
	//    byte[] ret = null;

	//    var connectionString = string.IsNullOrEmpty(record.ArchiveDBName)
	//                               ? _baseConnectionString
	//                               : _baseConnectionString.Replace("miaca_igs",
	//                                                               record.ArchiveDBName,
	//                                                               StringComparison.OrdinalIgnoreCase);
	//    var sql =
	//        $"SELECT ATXMLbin FROM ApplicationTransferXml WHERE ApplicationTransferId = {record.ApplicationTransferId}";
	//    var con = new SqlConnection(connectionString);
	//    var cmd = con.CreateCommand();
	//    cmd.CommandText = sql;
	//    cmd.CommandType = CommandType.Text;

	//    try
	//    {
	//        await con.OpenAsync(stoppingToken);
	//        var reader = await cmd.ExecuteReaderAsync(stoppingToken);
	//        if (reader.HasRows)
	//        {
	//            await reader.ReadAsync(stoppingToken);
	//            record.AtXmlBin = reader.IsDBNull(0) ? null : reader.GetBytes(0);
	//        }

	//        await reader.CloseAsync();
	//        if (record.AtXmlBin != null)
	//        {
	//            record.TransferStatus = "Retrieved";
	//        }
	//        else
	//        {
	//            record.TransferStatus = "Empty";
	//            record.Message        = "Xml record was empty or missing.";
	//        }
	//    }
	//    catch (Exception ex)
	//    {
	//        //_logger.Error(ex, "Error reading AWS ATXml value.");
	//        record.TransferStatus = "Missing";
	//        record.Message        = $"Could not retrieve AtXML. {ex.Message}";
	//    }
	//    finally
	//    {
	//        await con.CloseAsync();
	//    }
	//}

	private async Task<List<AwsAtXmlTransferRecord>> GetRecordsToTransferAsync(
		Guid              lockingGuid,
		CancellationToken stoppingToken)
	{
		var ret = new List<AwsAtXmlTransferRecord>();

		var sql = @$"
                    SELECT TOP {AppSettings.Instance.AwsS3SelectBatchSize}
	                    aat.ApplicationTransferId,
	                    a.ArchiveDBID,
	                    adb.Name,
	                    a.CreatedOn
                    FROM
	                    AwsAtXmlTransfer aat
                    INNER JOIN
	                    ApplicationTransfer a ON aat.ApplicationTransferID = a.ApplicationTransferID
                    LEFT OUTER JOIN
	                    ArchiveDB adb ON a.ArchiveDBID = adb.ArchiveDBID
                    WHERE
	                    aat.TransferGuid = @LockingGuid
                    AND aat.TransferStatus = 'Locked'
                    AND a.AtXmlDownloadPath IS NULL
                   ";
		var con = new SqlConnection(_baseConnectionString);
		var cmd = con.CreateCommand();
		cmd.CommandText                                                          = sql;
		cmd.CommandType                                                          = CommandType.Text;
		cmd.Parameters.Add("@LockingGuid", SqlDbType.UniqueIdentifier, 16).Value = lockingGuid;

		try
		{
			await con.OpenAsync(stoppingToken);

			var reader = await cmd.ExecuteReaderAsync(stoppingToken);
			if (reader.HasRows)
			{
				while (await reader.ReadAsync(stoppingToken))
				{
					ret.Add(new AwsAtXmlTransferRecord
							{
								ApplicationTransferId = reader.GetInt32(0),
								ArchiveDBID           = reader.IsDBNull(1) ? null : reader.GetInt32(1),
								ArchiveDBName =
									reader.IsDBNull(2) ? null : reader.GetString(2),
								CreatedOn = reader.GetDateTime(3)
							});
				}
			}

			await reader.CloseAsync();
		}
		catch (Exception ex)
		{
			_logger.Error(ex, "Error reading AWS ATXml transfer records.");
		}
		finally
		{
			await con.CloseAsync();
		}

		return ret;
	}

	private async Task<int> LockApplicationTransferRecordsAsync(Guid lockingGuid, CancellationToken stoppingToken)
	{
		object count = null;

		var sql = @$"
	                INSERT INTO AwsAtXmlTransfer (
		                ApplicationTransferID,
		                TransferGuid,
		                TransferDate,
		                TransferStatus
	                )
	                SELECT TOP {AppSettings.Instance.AwsS3InsertBatchSize}
		                a.ApplicationTransferId AS ApplicationTransferID,
		                @LockingGuid AS TransferGuid,
		                SYSDATETIMEOFFSET() AS TransferDate,
		                'Locked' AS TransferStatus
	                FROM ApplicationTransfer a
	                LEFT OUTER JOIN AwsAtXmlTransfer aat ON a.ApplicationTransferId = aat.ApplicationTransferId
	                WHERE
		                aat.ApplicationTransferId IS NULL
	                AND a.AtXmlDownloadPath IS NULL
	                ORDER BY
		                a.ApplicationTransferId DESC

	                SELECT @@ROWCOUNT
                ";
		var con = new SqlConnection(_baseConnectionString);
		var cmd = con.CreateCommand();
		cmd.CommandTimeout                                                       = 300;
		cmd.CommandText                                                          = sql;
		cmd.CommandType                                                          = CommandType.Text;
		cmd.Parameters.Add("@LockingGuid", SqlDbType.UniqueIdentifier, 16).Value = lockingGuid;

		do
		{
			try
			{
				await con.OpenAsync(stoppingToken);

				count = await cmd.ExecuteScalarAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Error inserting AWS ATXml transfer records. Trying again...");
				await Task.Delay(5000, stoppingToken);
			}
			finally
			{
				await con.CloseAsync();
			}
		} while (count == null);

		return int.TryParse(count.ToString(), out var ret) ? ret : 0;
	}

	private static class TransferStatus
	{
		public static readonly string Complete  = "Complete";
		public static readonly string Error     = "Error";
		public static readonly string Exported  = "Exported";
		public static readonly string Locked    = "Locked";
		public static readonly string Retrieved = "Retrieved";
		public static readonly string TryAgain  = "TryAgain";
	}
}