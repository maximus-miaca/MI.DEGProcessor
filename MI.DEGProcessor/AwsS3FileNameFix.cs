using System.Data;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.SSO.Model;
using IGS.Models.Helpers;
using MI.Common.Configuration;
using MI.Common.Extensions;
using MI.Common.Helper;
using MI.DEGProcessor.Helpers;
using MI.DEGProcessor.Models;
using Microsoft.Data.SqlClient;
using ML.Common.Service;
using NLog;

namespace MI.DEGProcessor;

public class AwsS3FileNameFix : BackgroundService
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly string         _baseConnectionString;
    private readonly string         _bucketName;
    private readonly RegionEndpoint _bucketRegion;
    private readonly Guid           _lockingGuid;
    private readonly string         _logConnectionString;
    private          string         _lastPath;

    public AwsS3FileNameFix()
    {
        _bucketName   = Environment.GetEnvironmentVariable("ATXML_BUCKET");
        _bucketRegion = RegionEndpoint.GetBySystemName(GlobalAppSettings.Instance.AwsRegion);

        _baseConnectionString = SqlHelper.EnsureTrustedServerCertificate(GlobalConnectionStrings.Instance.MIACA_IGS);
        _logConnectionString  = SqlHelper.EnsureTrustedServerCertificate(GlobalConnectionStrings.Instance.MIACA_LOG);
        _lockingGuid          = CalculateLockingGuid(Environment.MachineName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            //var s3Item = await GetAwsS3Item("prod/atxml/2023/01/08/02/0.zip",
            //                                "a37r1jh1ilG_15jwvaJKRmVp1Qiibrzg",
            //                                stoppingToken);
            //var atXmlDocument = GetAtXmlDocument(s3Item);
            //var recordToFix   = await GetRecordToFixAsync(98213654, stoppingToken);
            //var newPath       = await SaveToCorrectPathAsync(atXmlDocument, recordToFix, stoppingToken);
            //await UpdateRecordWithCorrectPathAsync(98064906, newPath, stoppingToken);

            await InitializeLastPathAsync(stoppingToken);
            _lastPath = GlobalAppSettings.Instance.AtXmlFix0ZipLastPath;
            while (!stoppingToken.IsCancellationRequested)
            {
                while (string.IsNullOrEmpty(_lastPath))
                {
                    await Task.Delay(30000, stoppingToken);
                    _lastPath = GlobalAppSettings.Instance.AtXmlFix0ZipLastPath;
                }

                var isActive = await ApplicationActiveService.IsApplicationActiveAsync(stoppingToken);
                if (isActive)
                {
                    while (!GlobalAppSettings.Instance.AtXmlFix0ZipServer.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                    {
                        await Task.Delay(30000, stoppingToken);
                    }

                    _logger.Info("AtXml File Fix Started.");
                    try
                    {
                        await ProcessRecordsAsync(stoppingToken);
                        _logger.Info("AtXml File Fix Completed.");
                    }
                    catch (TaskCanceledException tce)
                    {
                        _logger.Info($"Task was cancelled... {tce.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Unknown error transfering fixing AtXml Files.");
                    }
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(AppSettings.Instance.DelayMilliseconds, stoppingToken);
                }
            }
        }
        catch (TaskCanceledException tce)
        {
            _logger.Info($"Task was cancelled... {tce.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unknown error fixing AtXml Files.");
            await Task.Delay(AppSettings.Instance.DelayMilliseconds, stoppingToken);
        }
    }

    private async Task ProcessRecordsAsync(CancellationToken stoppingToken)
    {
        try
        {
            var recordsProcessed = false;
            do
            {
                recordsProcessed = false;
                var pathsToFix = await GetPathsToFixAsync(_lockingGuid, stoppingToken);

                if (pathsToFix is { Count: > 0 })
                {
                    foreach (var atXmlDownloadPath in pathsToFix)
                    {
                        var pathRecords = await GetMatchingRecordsToFixAsync(atXmlDownloadPath, stoppingToken);

                        var versions = await GetS3VersionsAsync(atXmlDownloadPath, stoppingToken);
                        if (versions.Count == 0)
                        {
                            await LogFixNoVersionsAsync(atXmlDownloadPath, stoppingToken);
                        }

                        foreach (var version in versions)
                        {
                            var s3Item        = await GetAwsS3Item(atXmlDownloadPath, version, stoppingToken);
                            var atXmlDocument = GetAtXmlDocument(s3Item);
                            var utcTime       = DateTime.SpecifyKind(s3Item.LastModified, DateTimeKind.Utc);
                            var mountain      = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utcTime, "Mountain Standard Time");

                            var atExtract       = new ATExtract(atXmlDocument);
                            var matchingRecords = GetMatchingRecords(pathRecords, atExtract, mountain); // convert from utc to server time
                            // find closest record by date/time
                            RecordToFix matchingRecord;
                            if (matchingRecords.Count > 1)
                            {
                                var closestMillisecond = matchingRecords.Select(r => Math.Abs((r.CreatedOn - mountain).TotalMilliseconds)).Min();

                                matchingRecord =
                                    matchingRecords.FirstOrDefault(r => Math.Abs((r.CreatedOn - mountain).TotalMilliseconds)
                                                                            .Equals(closestMillisecond));
                            }
                            else
                            {
                                matchingRecord = matchingRecords.FirstOrDefault();
                            }

                            if (matchingRecord != null)
                            {
                                var path = await SaveToCorrectPathAsync(atXmlDocument, matchingRecord, stoppingToken);
                                if (GlobalAppSettings.Instance.AtXmlFix0ZipServerOn)
                                {
                                    await UpdateRecordWithCorrectPathAsync(matchingRecord, path, stoppingToken);
                                }

                                await LogFixResultsAsync(atXmlDownloadPath,
                                                         version,
                                                         atExtract,
                                                         utcTime,
                                                         mountain,
                                                         matchingRecord,
                                                         path,
                                                         stoppingToken);
                                matchingRecord.Fixed = true;
                            }
                            else
                            {
                                await LogFixResultsAsync(atXmlDownloadPath, version, atExtract, utcTime, mountain, null, null, stoppingToken);
                            }

                            if (!GlobalAppSettings.Instance.AtXmlFix0ZipServer.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidRequestException("Server setting does not match. Exiting out.");
                            }
                        }

                        _lastPath = await SaveLastPathAsync(atXmlDownloadPath, stoppingToken);
                    }

                    recordsProcessed = true;
                }
            } while (recordsProcessed && !stoppingToken.IsCancellationRequested);
        }
        catch (InvalidRequestException)
        {
            _logger.Info("Server setting does not match. Exiting out.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unhandled exception in ProcessRecordsAsync.");
        }
    }

    private async Task<bool> InitializeLastPathAsync(CancellationToken stoppingToken)
    {
        var ret = false;
        var con = new SqlConnection(_logConnectionString);
        var sql = @"IF NOT EXISTS (SELECT Code FROM SystemConfig WHERE [Code] = 'AtXmlFix0ZipLastPath')
                    BEGIN
                        INSERT INTO [dbo].[SystemConfig] ([Code], [Value], [CreatedBy], [ModifiedBy], [Description], [IsHidden], [IsReadOnly]) VALUES (N'AtXmlFix0ZipLastPath', N'', N'MAXCORP\135620', N'MAXCORP\135620', N'Latest path already completed for fix process.', 0, 0)
                    END

                    SELECT @@ROWCOUNT
                    ";
        var cmd = con.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText    = sql;
        cmd.CommandType    = CommandType.Text;

        try
        {
            await con.OpenAsync(stoppingToken);
            var count = await cmd.ExecuteScalarAsync(stoppingToken);
            if (!int.Parse(count.ToString()).Equals(0))
            {
                ret = true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Could not initialize last path.");
        }

        return ret;
    }

    private async Task<string> SaveLastPathAsync(string atXmlDownloadPath, CancellationToken stoppingToken)
    {
        var con = new SqlConnection(_logConnectionString);
        var sql = @"UPDATE SystemConfig
                    SET
                        Value = @PathValue
                    WHERE
                        [Code] = 'AtXmlFix0ZipLastPath'
                    ";
        var cmd = con.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText    = sql;
        cmd.CommandType    = CommandType.Text;
        cmd.Parameters.AddWithValue("@PathValue", atXmlDownloadPath);

        try
        {
            await con.OpenAsync(stoppingToken);
            await cmd.ExecuteNonQueryAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Could not save last path {atXmlDownloadPath}.");
        }

        return atXmlDownloadPath;
    }

    private async Task LogFixNoVersionsAsync(string atXmlDownloadPath, CancellationToken stoppingToken)
    {
        var con = new SqlConnection(_baseConnectionString);
        var sql = @"INSERT INTO Temp0ZipFix (
                        Environment,
                        PathFrom
                    )
                    VALUES (
                        @Environment,
                        @PathFrom
                    )";
        var cmd = con.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText    = sql;
        cmd.CommandType    = CommandType.Text;
        cmd.Parameters.AddWithValue("@Environment", GlobalAppSettings.Instance.Environment);
        cmd.Parameters.AddWithValue("@PathFrom",    atXmlDownloadPath);

        try
        {
            await con.OpenAsync(stoppingToken);
            await cmd.ExecuteNonQueryAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            throw new Exception("Error saving results to log table.", ex);
        }
    }

    private async Task LogFixResultsAsync(string            atXmlDownloadPath,
                                          S3ObjectVersion   version,
                                          ATExtract         atExtract,
                                          DateTime          utcTime,
                                          DateTime          mountain,
                                          RecordToFix       record,
                                          string            pathTo,
                                          CancellationToken stoppingToken)
    {
        var con = new SqlConnection(_baseConnectionString);
        var sql = @"INSERT INTO Temp0ZipFix (
                        Environment,
                        PathFrom,
                        Version,
                        ApplicationID,
                        TransferID,
                        TransferDate,
                        S3CreateDateUtc,
                        S3CreateDateMountain,
                        ApplicationTransferID,
                        CreatedOn,
                        PathTo
                    )
                    VALUES (
                        @Environment,
                        @PathFrom,
                        @Version,
                        @ApplicationID,
                        @TransferID,
                        @TransferDate,
                        @S3CreateDateUtc,
                        @S3CreateDateMountain,
                        @ApplicationTransferID,
                        @CreatedOn,
                        @PathTo
                    )";
        var cmd = con.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText    = sql;
        cmd.CommandType    = CommandType.Text;
        cmd.Parameters.AddWithValue("@Environment",           GlobalAppSettings.Instance.Environment);
        cmd.Parameters.AddWithValue("@PathFrom",              record == null ? atXmlDownloadPath : record.AtXmlDownloadPath);
        cmd.Parameters.AddWithValue("@Version",               version.VersionId);
        cmd.Parameters.AddWithValue("@ApplicationID",         atExtract.ApplicationId);
        cmd.Parameters.AddWithValue("@TransferId",            atExtract.TransferId);
        cmd.Parameters.AddWithValue("@TransferDate",          atExtract.ActivityDate);
        cmd.Parameters.AddWithValue("@S3CreateDateUtc",       utcTime);
        cmd.Parameters.AddWithValue("@S3CreateDateMountain",  mountain);
        cmd.Parameters.AddWithValue("@ApplicationTransferID", record == null ? DBNull.Value : record.ApplicationTransferId);
        cmd.Parameters.AddWithValue("@CreatedOn",             record == null ? DBNull.Value : record.CreatedOn);
        cmd.Parameters.AddWithValue("@PathTo",                string.IsNullOrEmpty(pathTo) ? DBNull.Value : pathTo);

        try
        {
            await con.OpenAsync(stoppingToken);
            await cmd.ExecuteNonQueryAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            throw new Exception("Error saving results to log table.", ex);
        }
    }

    private async Task UpdateRecordWithCorrectPathAsync(RecordToFix record, string path, CancellationToken stoppingToken)
    {
        await UpdateRecordWithCorrectPathAsync(record.ApplicationTransferId, path, stoppingToken);
    }

    private async Task UpdateRecordWithCorrectPathAsync(int applicationTransferId, string path, CancellationToken stoppingToken)
    {
        var con = new SqlConnection(_baseConnectionString);
        var sql = @"UPDATE ApplicationTransfer
                    SET
	                    AtXmlDownloadPath = @AtXmlDownloadPath,
                        Recipient = ''
                    WHERE
	                    ApplicationTransferID = @ApplicationTransferId";
        var cmd = con.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText    = sql;
        cmd.CommandType    = CommandType.Text;
        cmd.Parameters.AddWithValue("@AtXmlDownloadPath",     path);
        cmd.Parameters.AddWithValue("@ApplicationTransferId", applicationTransferId);

        try
        {
            await con.OpenAsync(stoppingToken);
            await cmd.ExecuteNonQueryAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Error updating transfer status value. {applicationTransferId}");
        }
        finally
        {
            await con.CloseAsync();
        }
    }

    private async Task<RecordToFix> GetRecordToFixAsync(int applicationTransferId, CancellationToken stoppingToken)
    {
        var ret = new RecordToFix();

        var sql = @"
                    SELECT 
	                    ApplicationTransferID,
                        Recipient,
                        ApplicationId,
                        TransferId,
                        AtXmlDownloadPath,
                        CreatedOn,
                        TransferDate
                    FROM
	                    ApplicationTransfer
                    WHERE
	                    ApplicationTransferId = @ApplicationTransferId
                   ";

        var con = new SqlConnection(_baseConnectionString);
        var cmd = con.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText    = sql;
        cmd.CommandType    = CommandType.Text;
        cmd.Parameters.AddWithValue("@ApplicationTransferId", applicationTransferId);

        try
        {
            await con.OpenAsync(stoppingToken);

            var reader = await cmd.ExecuteReaderAsync(stoppingToken);
            if (reader.HasRows)
            {
                while (await reader.ReadAsync(stoppingToken))
                {
                    ret = new RecordToFix
                          {
                              ApplicationTransferId = reader.GetInt32(0),
                              Recipient             = reader.IsDBNull(1) ? null : reader.GetString(1),
                              ApplicationId         = reader.IsDBNull(2) ? null : reader.GetString(2),
                              TransferId            = reader.IsDBNull(3) ? null : reader.GetString(3),
                              AtXmlDownloadPath     = reader.IsDBNull(4) ? null : reader.GetString(4),
                              CreatedOn             = reader.GetDateTime(5),
                              TransferDate          = reader.GetDateTime(6)
                          };
                    break;
                }
            }

            await reader.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error reading ATXml files to fix.");
        }
        finally
        {
            await con.CloseAsync();
        }

        return ret;
    }

    private async Task<List<RecordToFix>> GetMatchingRecordsToFixAsync(string pathToFix, CancellationToken stoppingToken)
    {
        var ret           = new List<RecordToFix>();
        var pathToFixLike = pathToFix.Substring(0, pathToFix.LastIndexOf('/') + 1) + '%';

        var sql = @"
                    SELECT 
	                    ApplicationTransferID,
                        Recipient,
                        ApplicationId,
                        TransferId,
                        AtXmlDownloadPath,
                        CreatedOn,
                        TransferDate
                    FROM
	                    ApplicationTransfer
                    WHERE
	                    AtXmlDownloadPath LIKE @PathToFix
                   ";

        var con = new SqlConnection(_baseConnectionString);
        var cmd = con.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText    = sql;
        cmd.CommandType    = CommandType.Text;
        cmd.Parameters.AddWithValue("@PathToFix", pathToFixLike);

        try
        {
            await con.OpenAsync(stoppingToken);

            var reader = await cmd.ExecuteReaderAsync(stoppingToken);
            if (reader.HasRows)
            {
                while (await reader.ReadAsync(stoppingToken))
                {
                    ret.Add(new RecordToFix
                            {
                                ApplicationTransferId = reader.GetInt32(0),
                                Recipient             = reader.IsDBNull(1) ? null : reader.GetString(1),
                                ApplicationId         = reader.IsDBNull(2) ? null : reader.GetString(2),
                                TransferId            = reader.IsDBNull(3) ? null : reader.GetString(3),
                                AtXmlDownloadPath     = reader.IsDBNull(4) ? null : reader.GetString(4),
                                CreatedOn             = reader.GetDateTime(5),
                                TransferDate          = reader.GetDateTime(6)
                            });
                }
            }

            await reader.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error reading ATXml files to fix.");
        }
        finally
        {
            await con.CloseAsync();
        }

        return ret;
    }

    private List<RecordToFix> GetMatchingRecords(List<RecordToFix> recordsToFix, ATExtract atExtract, DateTime s3CreatedDate)
    {
        var matchingRecords = recordsToFix.Where(r => !r.Fixed                                                                            &&
                                                      r.ApplicationId.Equals(atExtract.ApplicationId, StringComparison.OrdinalIgnoreCase) &&
                                                      r.TransferId.Equals(atExtract.TransferId, StringComparison.OrdinalIgnoreCase)       &&
                                                      s3CreatedDate < r.CreatedOn.AddMilliseconds(15000)                                  &&
                                                      s3CreatedDate > r.CreatedOn.AddMilliseconds(-15000)                                 &&
                                                      (string.IsNullOrEmpty(atExtract.ActivityDate) ||
                                                       (r.TransferDate < DateTime.Parse(atExtract.ActivityDate).AddMilliseconds(3) &&
                                                        r.TransferDate > DateTime.Parse(atExtract.ActivityDate).AddMilliseconds(-3))))
                                          .ToList();

        return matchingRecords;
    }

    private async Task<List<S3ObjectVersion>> GetS3VersionsAsync(string atXmlDownloadPath, CancellationToken stoppingToken)
    {
        try
        {
            var lastKey       = string.Empty;
            var lastVersionId = string.Empty;
            var ret           = new List<S3ObjectVersion>();
            var count         = 0;
            do
            {
                var s3Client = new AmazonS3Client(_bucketRegion);
                var request = new ListVersionsRequest
                              {
                                  BucketName      = _bucketName,
                                  Prefix          = atXmlDownloadPath,
                                  KeyMarker       = string.IsNullOrEmpty(lastKey) ? null : lastKey,
                                  VersionIdMarker = string.IsNullOrEmpty(lastVersionId) ? null : lastVersionId,
                                  MaxKeys         = 1000
                              };

                var versions = await s3Client.ListVersionsAsync(request, stoppingToken);
                count = versions.Versions.Count;
                if (count > 0)
                {
                    lastKey       = versions.Versions.Last().Key;
                    lastVersionId = versions.Versions.Last().VersionId;
                }

                ret.AddRange(versions.Versions);
            } while (count >= 1000);

            return ret;
        }
        catch (Exception ex)
        {
            throw new Exception($"Could not get versions for {atXmlDownloadPath}", ex);
        }
    }

    private async Task<string> SaveToCorrectPathAsync(XmlDocument atXmlDocument, RecordToFix record, CancellationToken stoppingToken)
    {
        return await SaveToCorrectPathAsync(atXmlDocument, record.CreatedOn, record.ApplicationTransferId, stoppingToken);
    }

    private async Task<string> SaveToCorrectPathAsync(XmlDocument       atXmlDocument,
                                                      DateTime          createdOn,
                                                      int               applicationTransferId,
                                                      CancellationToken stoppingToken)
    {
        try
        {
            var relativePath = CalculateTransferPath(createdOn);
            var path         = $"{relativePath}";
            var xmlName      = $"{applicationTransferId}.xml";
            var fileName     = $"{applicationTransferId}.zip";
            var finalPath    = $"{path}/{fileName}";

            using var streamToUpload = new MemoryStream(await atXmlDocument.OuterXml.ZipAsync(xmlName));

            if (GlobalAppSettings.Instance.AtXmlFix0ZipServerOn)
            {
                var s3Client            = new AmazonS3Client(_bucketRegion);
                var fileTransferUtility = new TransferUtility(s3Client);
                await fileTransferUtility.UploadAsync(streamToUpload, _bucketName, finalPath, stoppingToken);
            }

            return finalPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving xml file to the correct path. ID: {applicationTransferId}", ex);
        }
    }

    private string CalculateTransferPath(DateTime createdOn)
    {
        return $"{createdOn.Year:D4}/{createdOn.Month:D2}/{createdOn.Day:D2}/{createdOn.Hour:D2}";
    }

    private async Task<GetObjectResponse> GetAwsS3Item(string atXmlDownloadPath, S3ObjectVersion version, CancellationToken stoppingToken)
    {
        return await GetAwsS3Item(atXmlDownloadPath, version.VersionId, stoppingToken);
    }

    private async Task<GetObjectResponse> GetAwsS3Item(string atXmlDownloadPath, string versionId, CancellationToken stoppingToken)

    {
        try
        {
            var s3Client = new AmazonS3Client(_bucketRegion);
            var item     = await s3Client.GetObjectAsync(_bucketName, atXmlDownloadPath, versionId, stoppingToken);

            return item;
        }
        catch (Exception ex)
        {
            throw new Exception($"Could not retrieve S3 object {atXmlDownloadPath}.", ex);
        }
    }

    private XmlDocument GetAtXmlDocument(GetObjectResponse s3Item)
    {
        try
        {
            var zipArchive = new ZipArchive(s3Item.ResponseStream);
            var r          = zipArchive.GetEntry("0.xml");
            if (r != null)
            {
                var doc = new XmlDocument();
                doc.Load(r.Open());

                return doc;
            }

            throw new Exception($"Could not retrieve xml file out of zip. {s3Item.Key}:{s3Item.VersionId}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Could not retrieve file {s3Item.Key}:{s3Item.VersionId}.", ex);
        }
    }

    private async Task<List<string>> GetPathsToFixAsync(Guid lockingGuid, CancellationToken stoppingToken)
    {
        var ret = new List<string>();
        var sql = @$"
                    SELECT DISTINCT
	                    AtXmlDownloadPath
                    FROM
	                    ApplicationTransfer
                    WHERE
	                    AtXmlDownloadPath LIKE '%/0.zip'
                    AND AtXmlDownloadPath > '{_lastPath}'
                    ORDER BY
                        AtXmlDownloadPath
                   ";

        var con = new SqlConnection(_baseConnectionString);
        var cmd = con.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText    = sql;
        cmd.CommandType    = CommandType.Text;
        //cmd.Parameters.Add("@LockingGuid", SqlDbType.UniqueIdentifier, 16).Value = lockingGuid;

        try
        {
            await con.OpenAsync(stoppingToken);

            var reader = await cmd.ExecuteReaderAsync(stoppingToken);
            if (reader.HasRows)
            {
                while (await reader.ReadAsync(stoppingToken))
                {
                    ret.Add(reader.GetString(0));
                    //ret.Add(new RecordToFix
                    //        {
                    //            ApplicationTransferId = reader.GetInt32(0),
                    //            Recipient             = reader.IsDBNull(1) ? null : reader.GetString(1),
                    //            ApplicationId         = reader.IsDBNull(2) ? null : reader.GetString(2),
                    //            TransferId            = reader.IsDBNull(3) ? null : reader.GetString(3),
                    //            AtXmlDownloadPath     = reader.IsDBNull(4) ? null : reader.GetString(4),
                    //            CreatedOn             = reader.GetDateTime(5),
                    //            TransferDate          = reader.GetDateTime(6)
                    //        });
                }
            }

            await reader.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error reading ATXml files to fix.");
        }
        finally
        {
            await con.CloseAsync();
        }
        //}

        return ret;
    }

    private async Task<bool> LockRecordsToFixAsync(Guid lockingGuid, CancellationToken stoppingToken)
    {
        object count = null;
        var sql = @"
                    UPDATE ApplicationTransfer
                    SET
                        Recipient = @LockingGuid
                    WHERE
	                    AtXmlDownloadPath IN (
					                        SELECT TOP 10
						                        AtXmlDownloadPath
					                        FROM ApplicationTransfer
					                        WHERE
						                        AtXmlDownloadPath LIKE '%/0.zip'
					                        )

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
                _logger.Error(ex, "Error locking batch for AtXml files to fix. Trying again...");
                await Task.Delay(5000, stoppingToken);
            }
            finally
            {
                await con.CloseAsync();
            }
        } while (count == null);

        int.TryParse(count.ToString(), out var iCount);

        return iCount > 0;
    }

    private Guid CalculateLockingGuid(string name)
    {
        using var md5   = MD5.Create();
        var       hash1 = md5.ComputeHash(Encoding.Default.GetBytes(name));
        return new Guid(hash1);
    }
}