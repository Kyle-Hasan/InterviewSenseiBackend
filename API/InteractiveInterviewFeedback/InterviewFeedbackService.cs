using System.Text.Json;
using API.AI;
using API.AWS;
using API.Interviews;
using API.Messages;
using API.PDF;
using API.Users;

namespace API.InteractiveInterviewFeedback;

public class InterviewFeedbackService(IdToMessage idToMessage, IinterviewRepository interviewRepository, IinterviewFeedbackRepository feedbackRepository,IOpenAIService openAiService, 
    IBlobStorageService blobStorageService, IFileService fileService): IinterviewFeedbackService
{
    public async Task<InterviewFeedbackDTO> EndInterview(AppUser user, int interviewId, IFormFile videoFile, string serverUrl)
    {
        Interview interview = await interviewRepository.GetInterview(user,interviewId);
        if (interview == null)
        {
            throw new UnauthorizedAccessException();
        }
        // save video and create feedback in parallel
        Task<InterviewFeedback> feedbackJob = CreateFeedback(user,interview);
        Task<string> saveVideoFile = SaveVideoFile(videoFile);
        

        
        await Task.WhenAll(feedbackJob, saveVideoFile);

        var feedback = await feedbackJob;
        var videoName = await saveVideoFile;
        
        interview.Feedback = feedback;
        interview.VideoLink = serverUrl + "/" + videoName;

        await interviewRepository.Save(interview,user);
        
       idToMessage.map.TryRemove(interview.Id, out _);
        
        return new InterviewFeedbackDTO()
        {
            positiveFeedback = feedback.PostiveFeedback,
            negativeFeedback = feedback.NegativeFeedback,
            id = feedback.Id
        };



    }

    private async Task<string> SaveVideoFile(IFormFile file)
    {
        string cloudKey = "";
        var fileInfo = await fileService.CreateNewFile(file);
        if (AppConfig.UseCloudStorage)
        {
            cloudKey = await blobStorageService.UploadFileAsync(fileInfo.FilePath, fileInfo.FileName, "videos");
            File.Delete(fileInfo.FilePath);
        }

        return fileInfo.FileName;

    }

    private async Task<InterviewFeedback> CreateFeedback(AppUser user, Interview interview)
    {
        idToMessage.map.TryGetValue(interview.Id, out CachedMessageAndResume context);
        if (context == null)
        {
            throw new BadHttpRequestException("interview not found");
        }
        
        string messages = idToMessage.ConvertMessagesToString(context.Messages);
        
        string prompt = GetInterviewFeedbackPrompt(messages, interview.JobDescription);
        
        string json = await openAiService.MakeRequest(prompt);
        string cleanJson = json.Replace("```json", "").Replace("```", "").Trim();
        InterviewFeedbackJSON feedbackJson = ParseFeedback(cleanJson);

        InterviewFeedback feedback = new InterviewFeedback
        {
            InterviewId = interview.Id,
            NegativeFeedback = string.Join("\n", feedbackJson.negativeFeedback),
            PostiveFeedback = string.Join("\n", feedbackJson.positiveFeedback)
        };
        return feedback;
    }

    public async Task<InterviewFeedbackDTO> GetInterviewFeedback(AppUser user, int interviewId)
    {
        var feedback = await feedbackRepository.GetInterviewFeedbackByInterviewId(interviewId, user);
        return new InterviewFeedbackDTO()
        {
            positiveFeedback = feedback.PostiveFeedback,
            negativeFeedback = feedback.NegativeFeedback,
            id = feedback.Id
        };
    }
    


    private string GetInterviewFeedbackPrompt(string messages, string? jobDescription)
    {
        string jobDescriptionSection = !string.IsNullOrWhiteSpace(jobDescription)
            ? $"**Job Description:**\n{jobDescription}"
            : "The job description is not available. Please generate general interview feedback suitable for a software engineering role.";

        

        string messagesSection = messages != null && messages.Length > 0
            ? $"Interview transcript:\n{messages}"
            : "No messages available. Provide general feedback for an assumed software engineering candidate.";

        return $"""
                    You are evaluating a live mock interview.
                
                    {jobDescriptionSection}
                
                    
                
                    {messagesSection}
                
                    Based on the interview, generate structured feedback in JSON format with two fields:
                    - `positiveFeedback`: A list of positive aspects of the candidate's performance.
                    - `negativeFeedback`: A list of areas where the candidate can improve.
                
                    The feedback should be clear, professional, and useful for the candidate’s improvement. Focus on how well they fit the role,
                    be harsh and unforgiving. You want to give the candidate no benefit of the doubt to simulate feedback for the harshest possible interviewer.
                
                    Return the result in JSON format only, with no explanations or extra text. DO NOT INCLUDE ANY FORMATTING, ONLY JSON OBJECT, no ```json formatting, assume i only need the object.
                """;
    }
    
    
    public static InterviewFeedbackJSON ParseFeedback(string jsonResponse)
    {
        return JsonSerializer.Deserialize<InterviewFeedbackJSON>(jsonResponse) ?? new InterviewFeedbackJSON();
    }
}