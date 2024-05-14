using IGS.Models.Entities;

namespace MI.DEGProcessor.Application;

public class TransitionParser : Transition
{
    public TransitionParser(string statusCode, string comments)
    {
        StatusCode = statusCode;
        Comments   = comments;
    }

    public new string CreatedBy
    {
        get => string.IsNullOrEmpty(base.CreatedBy) ? string.Empty : base.CreatedBy;
        set => base.CreatedBy = value;
    }

    public new string Successful
    {
        get => string.IsNullOrEmpty(base.Successful) ? string.Empty : base.Successful;
        set => base.Successful = value;
    }

    public new string Comments { get; set; }

    public new string StatusCode { get; set; }
}