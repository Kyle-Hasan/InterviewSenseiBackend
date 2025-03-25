namespace API.Interviews;

public class InterviewQueryRequest
{
    public int id { get; set; }
    public List<string> fields { get; set; } = new List<string>();
}