using System.Text;
using System.Text.Json;
using API.Interviews;
using API.Users;

namespace API.CodeRunner;

public class JudgeZeroService: ICodeRunnerService
{
    private readonly string _rapidUrl;
    
    private readonly HttpClient _client;
    
    private readonly IinterviewService _interviewService;
    private readonly ICodeSubmissionRepository _codeSubmissionRepository;
    public JudgeZeroService(HttpClient httpClient,IinterviewService interviewService, ICodeSubmissionRepository codeSubmissionRepository)
    {
        _interviewService = interviewService;
        _codeSubmissionRepository = codeSubmissionRepository;
        _client = httpClient;
        string apiKey= Environment.GetEnvironmentVariable("RAPID_API_KEY");
        
        _rapidUrl = Environment.GetEnvironmentVariable("RAPID_URL");
        
        _client.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey.Trim());
        string _rapidHost = Environment.GetEnvironmentVariable("RAPID_HOST");
        _client.DefaultRequestHeaders.Add("x-rapidapi-host", _rapidHost.Trim());
        
    }

    private async Task SaveCode(RunCodeRequest request,AppUser user)
    {
        Interview interview = await _interviewService.GetInterview(request.interviewId,user);
        interview.UserCode = request.sourceCode;
        interview.CodeLanguageName = request.languageName;
        await _interviewService.UpdateInterview(interview,user);
    }

    private async Task<CodeSubmission> CreateCodeSubmission(RunCodeRequest request,string token, AppUser user)
    {
        CodeSubmission submission = new CodeSubmission()
        {
            InterviewId = request.interviewId,
            SourceCode = request.sourceCode,
            LanguageName = request.languageName,
            Token = token
        };
        return await _codeSubmissionRepository.Save(submission,user);
    }
    public async Task<CodeSubmissionResult> RunCode(RunCodeRequest request,AppUser user)
    {
        
        Task updateInterviewTask = SaveCode(request,user);
        
        string url = _rapidUrl+"submissions/?base64_encoded=false&wait=false";

        Judge0Request judge0Request = new Judge0Request()
        {
            LanguageId = ConvertLanguageNameToId(request.languageName.ToLower()),
            SourceCode = request.sourceCode,
            Stdin = request.stdin,
        };
        
        var jsonFormat = JsonSerializer.Serialize(judge0Request);
        var content = new StringContent(jsonFormat, Encoding.UTF8, "application/json");

        var postTask = _client.PostAsync(url, content);
        
        // make request and update db at the same time (don't fire and forget since that doesnt notify of errors)
        await Task.WhenAll(updateInterviewTask, postTask);
        var response = await postTask;
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TokenResult>(json);
        var submission = await CreateCodeSubmission(request,result.token,user);
        
        return new CodeSubmissionResult()
        {
            codeSubmissionId = submission.Id,
        };
        
    }

    private int ConvertLanguageNameToId(string languageName)
    {
        switch (languageName)
        {
            case "python3":
                return 71;
            case "csharp":
                return 51;
            case "python":
                return 70;
            case "javascript":
                return 63;
            case "java":
                return 62;
            default:
                return -1;
                
        }
    }
    

    public async Task<RunCodeResult?> GetCodeResult(int codeSubmissionId, AppUser user)
    {
        var submission = await _codeSubmissionRepository.GetSubmission(codeSubmissionId,user);

        if (submission == null)
        {
            throw new UnauthorizedAccessException();
        }
        
        string url = $"{_rapidUrl}submissions/{submission.Token}?base64_encoded=false";
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RunCodeResult>(json);
        if (result.status.description.ToLower() == "in queue" || result.status.description.ToLower() == "processing")
        {
            return null;
        }
        return result;
    }
}