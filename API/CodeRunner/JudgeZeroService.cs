using System.Text;
using System.Text.Json;

namespace API.CodeRunner;

public class JudgeZeroService: ICodeRunnerService
{
    private readonly string _rapidUrl;
    
    private readonly HttpClient _client;
    public JudgeZeroService(HttpClient httpClient): base()
    {
        _client = httpClient;
        string apiKey= Environment.GetEnvironmentVariable("RAPID_API_KEY");
        
        _rapidUrl = Environment.GetEnvironmentVariable("RAPID_URL");
        
        _client.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey.Trim());
        string _rapidHost = Environment.GetEnvironmentVariable("RAPID_HOST");
        _client.DefaultRequestHeaders.Add("x-rapidapi-host", _rapidHost.Trim());
        
        
        
        


    }
    public async Task<string> RunCode(RunCodeRequest request)
    {
        
        string url = _rapidUrl+"submissions/?base64_encoded=false&wait=false";
        
        var jsonFormat = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonFormat, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TokenResult>(json);
        return result.token;
        
    }

    public async Task<RunCodeResult> GetCodeResult(string token)
    {
        string url = $"{_rapidUrl}submissions/{token}?base64_encoded=false";
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RunCodeResult>(json);
        return result;
    }
}