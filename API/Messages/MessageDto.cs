namespace API.Messages;

public class MessageDTO
{
    public string content{ get; set; }
    
    public int id{ get; set; }
    
    public int interviewId { get; set; }
    
    public bool fromAI { get; set; }

    public MessageDTO()
    {
        
    }
    public MessageDTO(Message message)
    {
        id = message.Id;
        content = message.Content;
        interviewId = message.InterviewId;
        fromAI = message.FromAI;
    }
    
    
    
    
    
}