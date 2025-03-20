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
    private readonly IFileService _fileService;
    private readonly IdToMessage idToMessage;

    public MessageService(
        IOpenAIService openAIService, 
        IMessageRepository messageRepository, 
        IinterviewRepository interviewRepository, 
        IFileService fileService, 
        IdToMessage idToMessage)
    {
        this.openAIService = openAIService;
        this.messageRepository = messageRepository;
        this.interviewRepository = interviewRepository;
        this._fileService = fileService;
        this.idToMessage = idToMessage;
    }

    private async Task<Message> GetAIResponse(Interview interview, CachedMessageAndResume context, string userTranscript, AppUser user)
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

        return newAIMessage;
    }

    private async Task<MessageResponse> SaveMessages(AppUser user, Interview interview, Message? userMessage, Message? aiMessage, CachedMessageAndResume context)
    {
        MessageResponse response = new MessageResponse();
        response.interviewId = interview.Id;
        if (userMessage != null)
        {
            
            interview.Messages.Add(userMessage);
            context.Messages.Add(userMessage);
            response.userMessage = userMessage.Content;
        }

        if (aiMessage != null)
        {
            interview.Messages.Add(aiMessage);
            context.Messages.Add(aiMessage);
            response.aiResponse = aiMessage.Content;
        }
        await interviewRepository.Save(interview,user);
        return response;
    }

    public async Task<MessageResponse> ProcessUserMessage(AppUser user, string? audioFilePath, int interviewId, string? textMessage)
    {
        // Validate that the interview belongs to this user.
        Interview interview = await interviewRepository.GetInterview(user, interviewId);
        if (interview == null)
        {
            throw new UnauthorizedAccessException();
        }
        List<Task> tasks = new List<Task>();
        // Transcribe the uploaded audio file.
        string userTranscript = "";
        if (!string.IsNullOrEmpty(audioFilePath))
        {
            userTranscript = await openAIService.TranscribeAudioAPI(audioFilePath);
            // Delete the file after transcription.
            Task deleteAudio = IFileService.DeleteFileAsync(audioFilePath);
            tasks.Add(deleteAudio);
        }
        else if (!string.IsNullOrEmpty(textMessage))
        {
            userTranscript = textMessage;
        }
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
        var aiResponse = await GetAIResponse(interview, context, userTranscript, user);
        var responseTask = SaveMessages(user,interview,userMessage,aiResponse,context);
        tasks.Add(responseTask);
        Task.WaitAll(tasks.ToArray());
        var response = await responseTask;
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
        
        var aiResponse = await GetAIResponse(interview, context, "", user);
        var response = await SaveMessages(user,interview,null, aiResponse,context);
        return IMessageService.ConvertToMessageDto(aiResponse);
    }

  

    public async Task<List<Message>> GetMessagesInterview(int interviewId, AppUser user)
    {
       return await messageRepository.GetMessagesInterview(interviewId, user);
    }

    private async Task<CachedMessageAndResume> InitalizeCachedMessageAndResume(Interview interview)
    {
        // Download and transcribe the candidate's resume.
        string resumeUrl = interview.ResumeLink;
        var fileTuple = await _fileService.DownloadPdf(resumeUrl);
        string resumeText = await IFileService.GetPdfTranscriptAsync(fileTuple.FilePath);
        // if you are restarting an existing interview, put its messages back
        List<Message> existingMessages = interview.Messages;
        return new CachedMessageAndResume(existingMessages, resumeText);
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
                As an interviewer, you are strict and never give the candidate the benefit of the doubt.
                You try to ask questions to assess their fit with the role and try your hardest to expose any issues with the candidate's answers by
                asking difficult/thorough follow up questions.
                The idea of this role play is getting more experience with follow up questions, so focus on that as well while also incorporating other questions.
                Make your replies to user messages flow naturally.
                Max 3 follow up questions before trying to move onto a new topic entirely.
            
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
