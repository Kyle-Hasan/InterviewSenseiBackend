namespace API.Messages;

public class MessageResponse
{
    public string userMessage { get; set; }
    public string aiResponse { get; set; }
    public int interviewId { get; set; }
}