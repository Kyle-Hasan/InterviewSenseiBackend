using System.Text;
using System.Text.Json;
using API.AI;
using API.InteractiveInterviewFeedback;
using API.Interviews;
using API.PDF;
using API.Responses;
using API.Users;



namespace API.Messages;

public class MessageService(IOpenAIService openAIService, IMessageRepository messageRepository, IinterviewRepository interviewRepository, IPDFService pdfService, IdToMessage idToMessage): IMessageService
{
    


    private async Task<MessageResponse> GetAIResponse(Interview interview, CachedMessageAndResume context, string userTranscript, AppUser user)
    {
        
        string messagesString = idToMessage.ConvertMessagesToString(context.Messages);
        
        string jobDescription = interview.JobDescription;
        
        string prompt = GetInterviewPrompt(messagesString, jobDescription,context.ResumeText,interview.AdditionalDescription);
        
        string aiResponse = await openAIService.MakeRequest(prompt);

        Message newAIMessage = new Message
        {
            Content = aiResponse,
            Interview = interview,
            InterviewId = interview.Id,
            FromAI = true
        };
        // save and add to cache without waiting for db
        messageRepository.CreateMessage(newAIMessage,user);
        context.Messages.Add(newAIMessage);

        
        MessageResponse response = new MessageResponse
        {
            aiResponse = aiResponse,
            interviewId = interview.Id,
            userMessage = userTranscript
        };



        return response;
    }
    
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

        Message userMessage = new Message()
        {
            Content = userTranscript,
            Interview = interview,
            InterviewId = interviewId,
            FromAI = false
        };
        // get the cached context for this conversation if it exists, if not make on
        bool messagesExist = idToMessage.map.TryGetValue(interview.Id, out CachedMessageAndResume context);
        if (!messagesExist)
        {
            context = await InitalizeCachedMessageAndResume(interview);
            idToMessage.map.Add(interview.Id, context);
        }
        
        context.Messages.Add(userMessage);
        
        var response = await GetAIResponse(interview, context,userTranscript, user);
        
        return response;

    }

    public async Task<string> GetInitialInterviewMessage(AppUser user, int interviewId)
    {
        Interview interview = await interviewRepository.GetInterview(user,interviewId);
        if (interview == null)
        {
            throw new UnauthorizedAccessException();
        }
        var context = await InitalizeCachedMessageAndResume(interview);
        idToMessage.map.Add(interview.Id, context);
        var response =  await GetAIResponse(interview, context, "", user);
        return response.aiResponse;
    }

    private async Task<CachedMessageAndResume> InitalizeCachedMessageAndResume(Interview interview)
    {
        string resumeUrl = interview.ResumeLink;
        var fileTuple = await pdfService.DownloadPdf(resumeUrl);
        string resumeText=  await pdfService.GetPdfTranscriptAsync(fileTuple.FilePath);
        var context = new CachedMessageAndResume(new List<Message>(),resumeText);
        return context;
    }

    

    

    private string GetInterviewPrompt(string messages, string? jobDescription, string? userResume, string additionalDescription)
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
                    
                    - Also consider the this additional info(if avaliable) ONLY if its relevant to interviews
                    {additionalDescription}
                
                    Return only the next interview question without any explanations or formatting.
                """;
    }
    
    

   
}