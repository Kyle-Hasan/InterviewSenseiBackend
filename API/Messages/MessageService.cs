using System.Text;
using API.AI;
using API.Interviews;
using API.PDF;
using API.Responses;
using API.Users;



namespace API.Messages;

public class MessageService(IOpenAIService openAIService, IMessageRepository messageRepository, IinterviewRepository interviewRepository, IPDFService pdfService): IMessageService
{
    
    private IDictionary<int,CachedMessageAndResume> _idToMessage = new Dictionary<int, CachedMessageAndResume>();
    // add user message to interview, return ai response while making sure this message belongs to this user
    public async Task<MessageResponse> ProcessUserMessage(AppUser user, string audioFilePath, int interviewId)
    {
        // make sure this interview actually belongs to this user.
        // (we get interview to update any in memory caches that may exist, lazy loading prevents lots of joins so this shouldn't be too slow)
        Interview interview = await interviewRepository.GetInterview(user,interviewId);
        if (interview == null)
        {
            throw new UnauthorizedAccessException();
        }

        string userTranscript = await openAIService.TranscribeAudioAPI(audioFilePath);

        bool messagesExist = _idToMessage.TryGetValue(interviewId, out CachedMessageAndResume context);
        if (!messagesExist)
        {
            string resumeUrl = interview.ResumeLink;
            string resumeText=  await pdfService.GetPdfTranscriptAsync(resumeUrl);
            context = new CachedMessageAndResume(new List<Message>(),resumeText);
        }
        
        StringBuilder builder = new StringBuilder();

        foreach (Message message in context.Messages)
        {
            builder.AppendLine(message.Content);
        }
        
        string jobDescription = interview.JobDescription;
        
        string prompt = GetInterviewPrompt(builder.ToString(), jobDescription,context.ResumeText);
        
        string aiResponse = await openAIService.MakeRequest(prompt);

        return new MessageResponse();





    }
    
    private string GetInterviewPrompt(string messages, string? jobDescription, string? userResume)
    {
        string jobDescriptionSection = !string.IsNullOrWhiteSpace(jobDescription)
            ? $"**Job Description:**\n{jobDescription}"
            : "The job description is not available. Please generate a general interview question.";

        string userResumeSection = !string.IsNullOrWhiteSpace(userResume)
            ? $"**Candidate's Resume:**\n{userResume}"
            : "The candidate's resume is not available. Focus on evaluating their general skills.";

        string messagesSection = messages.Length > 0
            ? $"Interview so far:\n{string.Join("\n", messages)}"
            : "This is the first question of the interview. Start with a relevant opening question.";

        return $"""
                    You are conducting a live mock interview.
                
                    {jobDescriptionSection}
                
                    {userResumeSection}
                
                    {messagesSection}
                
                    Based on the conversation so far, generate the next interview question that:
                    - Is relevant to the job description (if available).
                    - Aligns with the candidate’s background and experience (if provided).
                    - Progresses naturally from the previous questions.
                    - Varies between behavioral, technical, and situational questions depending on the flow of the interview.
                    - Is clear, professional, and challenging enough to assess the candidate's suitability for the role.
                
                    Return only the next interview question without any explanations or formatting.
                """;
    }

   
}