namespace API.Messages;

public class MessageResponse
{
    public string userMessage { get; set; }
    public string aiResponse { get; set; }
    public int interviewId { get; set; }
    
    public int userMessageId {get; set; }
    
    public int aiMessageId { get; set; }
    
    
    
}