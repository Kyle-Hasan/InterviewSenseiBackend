namespace API.Messages;

public class CreateUserMessageDto
{
    public IFormFile? audio  { get; set; }
    
    public int interviewId { get; set; }
    
    public string? textMessage { get; set; }
    
    public string? code { get; set; }

    public string messageType { get; set; } = "Text";



}