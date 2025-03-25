using API.Base;
using API.Interviews;

namespace API.CodeRunner;

public class CodeSubmission:BaseEntity
{
    public int InterviewId { get; set; }
    public virtual Interview Interview { get; set; }
    public string Token { get; set; }
    public string SourceCode { get; set; }
    public string LanguageName { get; set; }
}