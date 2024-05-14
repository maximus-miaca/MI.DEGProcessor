namespace MI.DEGProcessor.Helpers;

public class Enums
{
    public enum SaveAtXmlToDatabaseResult
    {
        Success,
        S3FileMissing,
        XmlError,
        DatabaseError
    }
}