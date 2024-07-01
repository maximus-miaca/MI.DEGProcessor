using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using IGS.DataServices;
using MI.Common.Extensions;
using MI.Common.Helper;
using MI.Common.Models;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MI.DEGProcessor.Tests;

[TestClass]
public class DataServicesTests
{
    [TestMethod]
    public void TestGetNextTransferSequenceNumber()
    {
        var data =
            new DataServices(SqlHelper
                                .EnsureTrustedServerCertificate("Data Source=ucocdmmsql01mia;Initial Catalog=MIACA_IGS;Integrated Security=SSPI;"));

        var valueTask = data.ApplicationTransfers.GetNextTransferSequenceNumber("WorkerDCT");
        valueTask.Wait();
        var value = valueTask.Result;

        Assert.IsTrue(value > 0);
    }

    private List<int> GetFfmIds(string connectionString)
    {
        var cn  = new SqlConnection(SqlHelper.EnsureTrustedServerCertificate(connectionString));
        var cmd = cn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = "SELECT * FROM StagingAtXmlResubmit WHERE BatchId = 1";
        cn.Open();

        var ids = new List<int>();
        try
        {
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt32("StagingAtXmlId");
                ids.Add(id);
            }
        }
        finally
        {
            cn.Close();
        }

        return ids;
    }

    private void UpdateFfmRecord(string filename, int id, string status, string errorMessage)
    {
        var resultFile = File.AppendText(@$"c:\dev\{filename}");
        try
        {
            resultFile.WriteLine($"{id},{status},{DateTime.Now},{errorMessage}");
        }
        finally
        {
            resultFile.Close();
        }
    }

    [TestMethod]
    [Ignore("This test method is not intended to run with all tests")]
    public void ResubmitFfmApps()
    {
        try
        {
            var connectionString = "Data Source=uvaaummsql01mia.maxcorp.maximus;Initial Catalog=MIACA_IGS;Integrated Security=SSPI;";
            var region           = "us-east-1";
            var baseAddress      = new Uri("https://degp-uat.miaca.maximus.com/deg/");
            var outputFilename   = "ffmresults_uat.csv";

            try
            {
                var ids    = GetFfmIds(connectionString);
                var client = new HttpClient();
                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                foreach (var id in ids)
                {
                    var request = new DEGRecordReceivedNotification
                                  {
                                      AwsRegion      = region,
                                      ReceivedDate   = DateTimeOffset.Now,
                                      StagingAtXmlId = id
                                  };
                    try
                    {
                        var response = client.PostAsJsonAsync("ProcessFromDegService", request).GetAwaiter().GetResult();

                        if (response.IsSuccessStatusCode)
                        {
                            try
                            {
                                UpdateFfmRecord(outputFilename, id, "PROCESSED", string.Empty);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToFullBlownString());
                            }
                        }
                        else
                        {
                            try
                            {
                                UpdateFfmRecord(outputFilename, id, "ERROR", response.ReasonPhrase);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToFullBlownString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error submitting to DEG Processor. {ex.ToFullBlownString()}");

                        try
                        {
                            UpdateFfmRecord(outputFilename, id, "ERROR", ex.Message);
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine(ex2.ToFullBlownString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running query. {ex.ToFullBlownString()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled error: {ex.ToFullBlownString()}");
        }
    }
}