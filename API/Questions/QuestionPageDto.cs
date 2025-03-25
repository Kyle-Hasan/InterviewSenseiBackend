using System.Diagnostics.CodeAnalysis;

namespace API.Questions;

public record QuestionPageDto: QuestionDTO
{
    public int nextQuestionId {get;set;}
    
    public int previousQuestionId {get;set;}
    
    public int interviewId {get;set;}

    public int secondsPerAnswer { get; set; }

    public QuestionPageDto(Question question, int nextQuestionId, int previousQuestionId, int secondsPerAnswer):base(question)
    {
       
        this.nextQuestionId = nextQuestionId;
        this.previousQuestionId = previousQuestionId;
        this.secondsPerAnswer = secondsPerAnswer;
    }
  
    public QuestionPageDto(Question question):base(question) { }
}