namespace API.AI;

public interface IOpenAIService
{
    Task<string> TranscribeAudioAPI(string filePath, bool videoFile = true);
    
    Task <string> TranscribeAudio(string filePath, bool videoFile = true);

    Task<string> MakeRequest(string question);
    
    
}