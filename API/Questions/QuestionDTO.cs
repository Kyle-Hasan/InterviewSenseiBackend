namespace API.Questions;

public class QuestionDTO
{
    public int id { get; set; }
    public string body {get; set;}
    public string type {get; set;}
    public string response { get; set; }
    public string videoLink {get; set;}
    
    public string feedback {get; set;}
}