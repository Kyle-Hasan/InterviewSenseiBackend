using System.Text.Json;
using API.AI;
using API.Interviews;
using API.Messages;
using API.Users;

namespace API.InteractiveInterviewFeedback;

public class InterviewFeedbackService(IdToMessage idToMessage, IinterviewRepository interviewRepository,IOpenAIService openAiService): IinterviewFeedbackService
{
    public async Task<InterviewFeedback> EndInterview(AppUser user, int interviewId)
    {
        Interview interview = await interviewRepository.GetInterview(user,interviewId);
        if (interview == null)
        {
            throw new UnauthorizedAccessException();
        }

        
        idToMessage.map.TryGetValue(interview.Id, out CachedMessageAndResume context);
        if (context == null)
        {
            throw new BadHttpRequestException("interview not found");
        }
        
        string messages = idToMessage.ConvertMessagesToString(context.Messages);
        
        string prompt = GetInterviewFeedbackPrompt(messages, interview.AdditionalDescription, context.ResumeText);
        
        string json = await openAiService.MakeRequest(prompt);
        string cleanJson = json.Replace("```json", "").Replace("```", "").Trim();
        InterviewFeedbackJSON feedbackJson = ParseFeedback(cleanJson);

        InterviewFeedback feedback = new InterviewFeedback
        {
            InterviewId = interview.Id,
            NegativeFeedback = string.Join("\n", feedbackJson.NegativeFeedback),
            PostiveFeedback = string.Join("\n", feedbackJson.PositiveFeedback)
        };
        
        interview.Feedback = feedback;

        await interviewRepository.Save(interview,user);
        
       idToMessage.map.TryRemove(interview.Id, out _);
        
        return feedback;



    }
    
    private string GetInterviewFeedbackPrompt(string messages, string? jobDescription, string? userResume)
    {
        string jobDescriptionSection = !string.IsNullOrWhiteSpace(jobDescription)
            ? $"**Job Description:**\n{jobDescription}"
            : "The job description is not available. Please generate general interview feedback suitable for a software engineering role.";

        string userResumeSection = !string.IsNullOrWhiteSpace(userResume)
            ? $"**Candidate's Resume:**\n{userResume}"
            : "The candidate's resume is not available. Focus on general software engineering skills.";

        string messagesSection = messages != null && messages.Length > 0
            ? $"Interview transcript:\n{messages}"
            : "No messages available. Provide general feedback for an assumed software engineering candidate.";

        return $"""
                    You are evaluating a live mock interview.
                
                    {jobDescriptionSection}
                
                    {userResumeSection}
                
                    {messagesSection}
                
                    Based on the interview, generate structured feedback in JSON format with two fields:
                    - `positiveFeedback`: A list of positive aspects of the candidate's performance.
                    - `negativeFeedback`: A list of areas where the candidate can improve.
                
                    The feedback should be clear, professional, and useful for the candidate’s improvement.
                
                    Return the result in JSON format only, with no explanations or extra text. DO NOT INCLUDE ANY FORMATTING, ONLY JSON OBJECT, no ```json formatting, assume i only need the object.
                """;
    }
    
    
    public static InterviewFeedbackJSON ParseFeedback(string jsonResponse)
    {
        return JsonSerializer.Deserialize<InterviewFeedbackJSON>(jsonResponse) ?? new InterviewFeedbackJSON();
    }
}