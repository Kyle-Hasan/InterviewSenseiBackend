namespace API.Interviews;

public interface IinterviewService
{
    Task<RatingResponse> rateAnswer(string question,string videoPath);

    Task<string> generateQuestion(string prompt);
    
    
    
    
}