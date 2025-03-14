using System.Text;
using API.FFMPEG;
using OpenAI.Audio;
using OpenAI.Chat;
using Whisper.net;
using Whisper.net.Ggml;

namespace API.AI;
using OpenAI;

public class OpenAIService : IOpenAIService
{
  
    protected readonly string _apiKey;
    ChatClient _chatClient;
    AudioClient _audioClient;
    private string _modelName = "ggml-base.bin";

    public OpenAIService()
    {
       
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _chatClient = new(model: "gpt-4o", apiKey: _apiKey);
        _audioClient = new("whisper-1", apiKey: _apiKey);
    }

    public async Task<string> TranscribeAudioAPI(string filePath, bool videoPath = true)
    {
        
        string absolutePath = Path.GetFullPath(filePath);

        string audioFile = filePath;
        
        // only extract the audio if it isnt already an audio file
       
        if (videoPath)
        {
            audioFile = await FfmpegService.extractAudioFile(absolutePath);
        }
        
        AudioTranscriptionOptions options = new()
        {
            ResponseFormat = AudioTranscriptionFormat.Verbose,
            TimestampGranularities = AudioTimestampGranularities.Word | AudioTimestampGranularities.Segment,
        };
        AudioTranscription transcription = _audioClient.TranscribeAudio(audioFile, options);
        

        System.IO.File.Delete(audioFile);
        return transcription.Text;
    }


    public virtual async Task<string> MakeRequest(string question)
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

    public async Task<String> TranscribeAudio(string filePath, bool videoFile = true)
    {


        string audioFilePath = filePath;

        if (videoFile)
        {
            // gives a wav file,which is needed since mp4 video files not accepted by local whisper modal
            audioFilePath = await FfmpegService.extractAudioFile(filePath);
        }

        // download whisper model if it isnt already downloaded on local file system
        if (!File.Exists(_modelName))
        {
            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Base);
            using var fileWriter = File.OpenWrite(_modelName);
            await modelStream.CopyToAsync(fileWriter);
        }
        
        using var whisperFactory = WhisperFactory.FromPath("ggml-base.bin");

        using var processor = whisperFactory.CreateBuilder()
            .WithLanguage("auto")
            .Build();

        StringBuilder sb = new StringBuilder("");

        await using (var fileStream = File.OpenRead(audioFilePath)) 
        {
            await foreach (var result in processor.ProcessAsync(fileStream))
            {
                sb.Append(result.Text);
            }
        }
        //delete file after done
        System.IO.File.Delete(audioFilePath);

        return sb.ToString();

    }

    public async Task TaskDownloadModel()
    {
        var modelFileName = "ggml-base.bin";
        if (!File.Exists(modelFileName))
        {
            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Base);
            using var fileWriter = File.OpenWrite(modelFileName);
            await modelStream.CopyToAsync(fileWriter);
        }
    }
    
    

}
