using API.FFMPEG;
using OpenAI.Audio;
using OpenAI.Chat;

namespace API.AI;
using OpenAI;

public class OpenAIService : IOpenAIService
{
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;
    ChatClient _chatClient;
    AudioClient _audioClient;

    public OpenAIService(IConfiguration configuration)
    {
        _configuration = configuration;
        _apiKey = _configuration.GetValue<string>("OPENAI_API_KEY");
        _chatClient = new(model: "gpt-4o", apiKey: _apiKey);
        _audioClient = new("whisper-1", apiKey: _apiKey);
    }

    public async Task<string> GetTranscript(String videoPath)
    {
        string absolutePath = Path.GetFullPath(videoPath);
        string audioFile = FfmpegService.extractAudioFile(absolutePath);
        AudioTranscriptionOptions options = new()
        {
            ResponseFormat = AudioTranscriptionFormat.Verbose,
            TimestampGranularities = AudioTimestampGranularities.Word | AudioTimestampGranularities.Segment,
        };
        AudioTranscription transcription = _audioClient.TranscribeAudio(audioFile, options);
        Console.WriteLine("Transcription:");
        Console.WriteLine($"{transcription.Text}");
        return transcription.Text;
    }


    public async Task<string> MakeRequest(String question)
    {
        ChatCompletionOptions options = new()
        {
            
            MaxOutputTokenCount = 1500, 
            Temperature = 0.9f
        };
        
        var message = ChatMessage.CreateUserMessage(question);
  
        ChatCompletion completion = await _chatClient.CompleteChatAsync(new ChatMessage[] {message}, options);
        return completion.Content[0].Text;
    }
    
    

}
