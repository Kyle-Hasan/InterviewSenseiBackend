namespace API.AI;

public interface IOpenAIService
{
    Task<string> GetTranscript(String videoPath);

    Task<string> MakeRequest(String question);
    
    
}