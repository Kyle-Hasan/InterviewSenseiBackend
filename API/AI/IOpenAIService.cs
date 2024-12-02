namespace API.AI;

public interface IOpenAIService
{
    Task<string> TranscribeAudioAPI(String videoPath);
    
    Task <string> TranscribeAudio(String videoPath);

    Task<string> MakeRequest(String question);
    
    
}