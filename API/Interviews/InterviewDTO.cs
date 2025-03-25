using API.InteractiveInterviewFeedback;
using API.Messages;
using API.Questions;

namespace API.Interviews;

public record InterviewDTO
{
    public List<QuestionPageDto>? questions { get; set; }
    
    public int id {get; set;}
    public string name {get; set;} = string.Empty;
    
    public string jobDescription {get; set;} = string.Empty;
    
    public string resumeLink {get; set;} = string.Empty;
    
    public string createdDate {get; set;} = string.Empty;
    
    public int secondsPerAnswer {get; set;}
    
    public string additionalDescription {get; set;} = string.Empty;
    
    public string type {get; set;} = string.Empty;
    
    public string? userCode {get; set;}
    
    public string? codeLanguageName { get; set; }
    
    public InterviewFeedbackDTO? feedback {get; set;}
    
    public List<MessageDTO>? messages {get; set;}
    
    
    
}