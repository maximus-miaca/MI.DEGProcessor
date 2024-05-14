using MI.Common.Attributes;
using MI.Common.Configuration;

namespace MI.DEGProcessor.Helpers;

public class AppSettings : GlobalAppSettings
{
    public new static     AppSettings Instance = new();
    public                double      HeartbeatMilliseconds            { get; set; }
    public                int         DelayMilliseconds                { get; set; }
    public                int         CommunicationPort                { get; set; }
    public                bool        SwitchATXSDValidation            { get; set; }
    public                bool        SwitchDeterminationXSDValidation { get; set; }
    public                string      ServiceName                      { get; set; }
    public                bool        EnableAWSATXmlTransfer           { get; set; }
    [DbAppSetting] public int         AwsS3InsertBatchSize             { get; set; } = 10000;
    [DbAppSetting] public int         AwsS3SelectBatchSize             { get; set; } = 1000;
    [DbAppSetting] public string      AtXmlS3TransferServers           { get; set; } = "";
    [DbAppSetting] public int         DEGProcessorBatchSize            { get; set; } = 10;
    [DbAppSetting] public int         DEGProcessorThreads              { get; set; } = 4;

    public Dictionary<string, int> GetAtXmlS3TransferServers()
    {
        var ret = new Dictionary<string, int>();

        if (!string.IsNullOrEmpty(AtXmlS3TransferServers))
        {
            var servers = AtXmlS3TransferServers.Split(';').ToList();
            foreach (var server in servers)
            {
                var values = server.Split('/');
                if (values.Length > 1 && int.TryParse(values[1], out var threads))
                {
                    ret.Add(values[0], threads);
                }
                else
                {
                    ret.Add(values[0], 1);
                }
            }
        }

        return ret;
    }
}