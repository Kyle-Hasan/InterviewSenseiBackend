using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;

namespace API.AI;


public class GeminiService: OpenAIService
{
    private readonly string _geminiUrl;
    private readonly string _geminiAPIKey;
    private readonly HttpClient _client;
    public GeminiService(): base()
    {
      
        _geminiAPIKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        _geminiUrl = Environment.GetEnvironmentVariable("GEMINI_ENDPOINT") + _geminiAPIKey;
        _client = new HttpClient();
       
    }
    public override async Task<string>  MakeRequest(String question) {
        
        var requestBody = new JObject
        {
            { "contents", new JArray { new JObject { { "parts", new JObject { { "text", question } } } } } },
    
            
            
        }.ToString();
        
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        
        try
        {
            var response = await _client.PostAsync(_geminiUrl, content);
            response.EnsureSuccessStatusCode(); 

            string responseJson = await response.Content.ReadAsStringAsync();
           
            JObject json = JObject.Parse(responseJson);

        
            var predictions = json["candidates"] as JArray;

            if (predictions != null && predictions.Count > 0)
            {
                return predictions[0]["content"]["parts"][0]["text"].ToString(); 
            }
            else
            {
                return "No text generated.";
            }

        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return $"Error: {ex.Message}";
        }
        catch (Exception ex)  
        {
            Console.WriteLine($"Error: {ex.Message}");
            return $"Error: {ex.Message}";
        }
        
    }
}