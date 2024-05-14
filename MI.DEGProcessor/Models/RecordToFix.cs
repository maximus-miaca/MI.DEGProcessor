namespace MI.DEGProcessor.Models;

public class RecordToFix
{
    public int      ApplicationTransferId { get; set; }
    public string   Recipient             { get; set; }
    public string   ApplicationId         { get; set; }
    public string   TransferId            { get; set; }
    public string   AtXmlDownloadPath     { get; set; }
    public DateTime CreatedOn             { get; set; }
    public DateTime TransferDate          { get; set; }
    public bool     Fixed                 { get; set; } = false;
}

public class RecordsToUpdate
{
    public int ApplicationTransferId { get; set; }
    public string Path { get; set; }
}
