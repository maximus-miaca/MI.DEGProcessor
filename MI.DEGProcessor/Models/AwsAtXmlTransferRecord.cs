using System;

namespace MI.DEGProcessor.Models;

public class AwsAtXmlTransferRecord
{
    public int      ApplicationTransferId { get; set; }
    public int?     ArchiveDBID           { get; set; }
    public string   ArchiveDBName         { get; set; }
    public DateTime CreatedOn             { get; set; }
    public byte[]   AtXmlBin              { get; set; }
    public string   AtXml                 { get; set; }
    public string   TransferStatus        { get; set; }
    public string   Message               { get; set; }
    public string   FinalPath             { get; set; }
}