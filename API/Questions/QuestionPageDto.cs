namespace API.Questions;

public class QuestionPageDto: QuestionDTO
{
    public int nextQuestionId {get;set;}
    
    public int previousQuestionId {get;set;}
    
    public int interviewId {get;set;}

    public int secondsPerAnswer { get; set; }
}