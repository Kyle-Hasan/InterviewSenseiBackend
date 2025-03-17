using System.Text;
using System.Text.Json;
using API.AI;
using API.InteractiveInterviewFeedback;
using API.Interviews;
using API.PDF;
using API.Responses;
using API.Users;
using System.Collections.Concurrent;

namespace API.Messages;

public class MessageService : IMessageService
{
    private readonly IOpenAIService openAIService;
    private readonly IMessageRepository messageRepository;
    private readonly IinterviewRepository interviewRepository;
    private readonly IPDFService pdfService;
    private readonly IdToMessage idToMessage;

    public MessageService(
        IOpenAIService openAIService, 
        IMessageRepository messageRepository, 
        IinterviewRepository interviewRepository, 
        IPDFService pdfService, 
        IdToMessage idToMessage)
    {
        this.openAIService = openAIService;
        this.messageRepository = messageRepository;
        this.interviewRepository = interviewRepository;
        this.pdfService = pdfService;
        this.idToMessage = idToMessage;
    }

    private async Task<MessageResponse> GetAIResponse(Interview interview, CachedMessageAndResume context, string userTranscript, AppUser user)
    {
        // Convert cached messages to a string
        string messagesString = idToMessage.ConvertMessagesToString(context.Messages);
        string jobDescription = interview.JobDescription;
        string prompt = GetInterviewPrompt(messagesString, jobDescription, context.ResumeText, interview.AdditionalDescription);
        
        // Get AI response from the OpenAI service
        string aiResponse = await openAIService.MakeRequest(prompt);

        Message newAIMessage = new Message
        {
            Content = aiResponse,
            Interview = interview,
            InterviewId = interview.Id,
            FromAI = true
        };

        // Save the new AI message and add it to the cache
        await messageRepository.CreateMessage(newAIMessage, user);
        context.Messages.Add(newAIMessage);

        return new MessageResponse
        {
            aiResponse = aiResponse,
            interviewId = interview.Id,
            userMessage = userTranscript
        };
    }

    public async Task<MessageResponse> ProcessUserMessage(AppUser user, string audioFilePath, int interviewId)
    {
        // Validate that the interview belongs to this user.
        Interview interview = await interviewRepository.GetInterview(user, interviewId);
        if (interview == null)
        {
            throw new UnauthorizedAccessException();
        }

        // Transcribe the uploaded audio file.
        string userTranscript = await openAIService.TranscribeAudioAPI(audioFilePath);
        // Delete the file immediately after transcription.
        Task deletionTask =  Task.Run(()=> File.Delete(audioFilePath));
        // fire and forget
        deletionTask.ContinueWith((task) =>
        {
            foreach (var ex in task.Exception.InnerExceptions)
            {
                Console.WriteLine(ex);
            }
            
        });
        

        Message userMessage = new Message()
        {
            Content = userTranscript,
            Interview = interview,
            InterviewId = interviewId,
            FromAI = false
        };

        // Atomically get or add the cached context for this interview.
        CachedMessageAndResume context = idToMessage.map.GetOrAdd(interview.Id, 
            _ => InitalizeCachedMessageAndResume(interview).Result);

        context.Messages.Add(userMessage);
        var response = await GetAIResponse(interview, context, userTranscript, user);
        return response;
    }

    public async Task<MessageDto> GetInitialInterviewMessage(AppUser user, int interviewId)
    {
        Interview interview = await interviewRepository.GetInterview(user, interviewId);
        if (interview == null)
        {
            throw new UnauthorizedAccessException();
        }
        // Initialize context and atomically add/update the cache.
        CachedMessageAndResume context = await InitalizeCachedMessageAndResume(interview);
        idToMessage.map.AddOrUpdate(interview.Id, context, (key, old) => context);
        
        var response = await GetAIResponse(interview, context, "", user);
        return new MessageDto()
        {
            content = response.aiResponse,
            interviewId = interview.Id,
            fromAI = true
        };
    }

    private async Task<CachedMessageAndResume> InitalizeCachedMessageAndResume(Interview interview)
    {
        // Download and transcribe the candidate's resume.
        string resumeUrl = interview.ResumeLink;
        var fileTuple = await pdfService.DownloadPdf(resumeUrl);
        string resumeText = await pdfService.GetPdfTranscriptAsync(fileTuple.FilePath);
        return new CachedMessageAndResume(new List<Message>(), resumeText);
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
            ? $"Interview so far:\n{messages}"
            : "This is the first question of the interview. Start with a relevant opening question.";

        return $"""
                You are conducting a live mock interview. Imagine you are role playing as the interviewer and make the conversation flow naturally.
                You are not just asking a list of questions, this is a conversation so please take the conversation context very seriously.
                The idea of this role play is getting more experience with follow up questions, so focus on that as well while also incorporating other questions.
                Make your replies to user messages flow naturally.
            
                Job Description:
                {jobDescriptionSection}
            
                Candidate Resume:
                {userResumeSection}
            
                Conversation Context (please consider these current messages in detail, generate next question based on what has already been said for natural flow, prioritize last AI and user interaction the most, if you don't understand what the user is saying, point that out):
                {messagesSection}
            
                Based on the conversation so far, generate the next interview question that:
                - Is relevant to the job description (if available).
                - Aligns with the candidate’s background and experience (if provided).
                - Progresses naturally from the previous questions.
                - Varies between behavioral, technical, and situational questions depending on the flow of the interview.
                - Is clear, professional, and challenging enough to assess the candidate's suitability for the role.
            
                Also consider this additional info (if available and relevant to interviews):
                {additionalDescription}
            
                Return only the next interview question without any explanations or formatting.
                """;
    }
}
