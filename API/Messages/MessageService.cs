using System.Text;
using System.Text.Json;
using API.AI;
using API.InteractiveInterviewFeedback;
using API.Interviews;
using API.PDF;
using API.Responses;
using API.Users;
using System.Collections.Concurrent;
using API.Questions;

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

    private async Task<Message> GetAIResponse(Interview interview, CachedMessageAndResume context,  AppUser user, MessageType messageType)
    {
        // Convert cached messages to a string
        string messagesString = idToMessage.ConvertMessagesToString(context.Messages);
        string jobDescription = interview.JobDescription;
        string prompt = "";
        if (interview.Type == InterviewType.Live ||  interview.Type == InterviewType.NonLive)
        {
            prompt = GetInterviewPrompt(messagesString, jobDescription, context.ResumeText,
                interview.AdditionalDescription);
        }
        else if (interview.Type == InterviewType.CodeReview)
        {
            prompt = GetCodeReviewContinuationConversationPrompt(messagesString,jobDescription,interview.AdditionalDescription,context.ResumeText,context.QuestionBody);
        }
        
        
        else if (interview.Type == InterviewType.LiveCoding)
        {
            prompt = GetLiveCodingContinuationConversationPrompt(messagesString, context.QuestionBody,
                interview.AdditionalDescription);
        }

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
        // updates the interview if its cached or anything
        interview = await interviewRepository.Save(interview,user);
        response.aiMessageId = aiMessage.Id;
        response.userMessageId = userMessage?.Id ?? -1;
        return response;
    }

    private MessageType ConvertStringToMessageType(string messageType)
    {
        switch (messageType)
        {
            case "Text":
                return MessageType.Text;
            case "Coding":
                return MessageType.Coding;
            default:
                return MessageType.Text;
        }
        
    }

    private async Task<string?> DownloadAudioFile(IFormFile audio)
    {
        string? filePath = null;
        if (audio != null && audio.Length > 0)
        {
            var fileResult = await IFileService.CreateNewFile(audio);
            filePath = fileResult.FilePath;
        }
        return filePath;
    }

    public async Task<MessageResponse> ProcessUserMessage(AppUser user, CreateUserMessageDto createMessage)
    {
        string? audioFilePath = await DownloadAudioFile(createMessage.audio);
        // Validate that the interview belongs to this user.
        Interview interview = await interviewRepository.GetInterview(user, createMessage.interviewId);
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
        else if (!string.IsNullOrEmpty(createMessage.textMessage))
        {
            userTranscript = createMessage.textMessage;
        }
        MessageType messageType = ConvertStringToMessageType(createMessage.messageType);
        Message userMessage = new Message()
        {
            Content = userTranscript,
            Interview = interview,
            InterviewId = createMessage.interviewId,
            FromAI = false,
            Type = messageType
        };

        // Atomically get or add the cached context for this interview.
        CachedMessageAndResume context = idToMessage.map.GetOrAdd(interview.Id, 
            _ => InitalizeCachedMessageAndResume(interview).Result);

        if (!string.IsNullOrEmpty(createMessage.code))
        {
            context.Code = createMessage.code;
            // interview should get saved eventually with the messages, so hold off on the save for now
            interview.UserCode = createMessage.code;
        }

        context.Messages.Add(userMessage);
        var aiResponse = await GetAIResponse(interview, context,  user, messageType);
        var responseTask = SaveMessages(user,interview,userMessage,aiResponse,context);
        tasks.Add(responseTask);
        Task.WaitAll(tasks.ToArray());
        var response = await responseTask;
        return response;
    }

    public async Task<MessageDTO> GetInitialInterviewMessage(AppUser user, int interviewId)
    {
        Interview interview = await interviewRepository.GetInterview(user, interviewId);
        if (interview == null)
        {
            throw new UnauthorizedAccessException();
        }
        // Initialize context and atomically add/update the cache.
        CachedMessageAndResume context = await InitalizeCachedMessageAndResume(interview);
        idToMessage.map.AddOrUpdate(interview.Id, context, (key, old) => context);
        
        var aiResponse = await GetAIResponse(interview, context, user,MessageType.Text);
        var response = await SaveMessages(user,interview,null, aiResponse,context);
        return new MessageDTO(aiResponse);
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
        // if its a coding question cache the question body
        CachedMessageAndResume cached  = new CachedMessageAndResume(existingMessages, resumeText);

        if (interview.Type is InterviewType.CodeReview or InterviewType.LiveCoding)
        {
            Question question = interview.Questions.Where(x=> x.Type == QuestionType.LiveCoding || x.Type == QuestionType.CodeReview).FirstOrDefault();
            if (question != null)
            {
                cached.QuestionBody = question.Body;
            }
        }
        
        return cached;
    }
    
    private string GetLiveCodingContinuationConversationPrompt(string messages, string questionBody, string additionalDescription)
{
    string messagesSection = messages.Length > 0
        ? $"Interview so far:\n{messages}"
        : "The candidate has been given a coding task. Begin the conversation by observing their progress and asking relevant questions.";

    return $"""
            You are conducting a live coding interview. Imagine you are role playing as the interviewer and make the conversation flow naturally.
            You are not just providing a coding task, you are participating in a conversation, so please take the conversation context very seriously.
            As an interviewer, you are strict and never give the candidate the benefit of the doubt.
            You try to assess their coding skills and problem-solving abilities.
            The candidate has already been given a coding task: {questionBody}. Your role is to monitor their progress, ask relevant questions, and provide guidance in a natural conversation.
            Make your replies to user messages flow naturally.
            If the candidate is struggling, gently nudge them in the right direction with hints and suggestions.
            Ask the candidate to explain their thought process as they code.
            Emphasize the importance of code clarity, efficiency, and correctness.
            Consider edge cases and potential errors.
            Max 3 follow up questions before trying to move onto a new topic entirely.

            Coding Task:
            {questionBody}

            Conversation Context (please consider these current messages in detail, generate next response based on what has already been said for natural flow, prioritize last AI and user interaction the most, if you don't understand what the user is saying, point that out):
            {messagesSection}

            Based on the candidate's progress and the conversation so far, continue the live coding interview by responding as the interviewer would, naturally.
            - Progress the conversation naturally from the previous steps.
            - Focus on the candidate's coding ability and problem solving skills.
            - Gently nudge the candidate in the right direction if they are struggling.
            - Ask the candidate to explain their thought process.
            - Assume a coding task has already been given.
            - Respond as an interviewer in a conversation.

            Also consider this additional info (if available and relevant to interviews):
            {additionalDescription}

            Respond as the interviewer in a natural conversational style.
            """;
}

private string GetCodeReviewContinuationConversationPrompt(string messages, string? jobDescription, string additionalDescription, string codeSnippet, string questionBody)
{
    string jobDescriptionSection = !string.IsNullOrWhiteSpace(jobDescription)
        ? $"**Job Description:**\n{jobDescription}"
        : "The job description is not available. Continue evaluating the candidate's code review skills.";

   ;

    string messagesSection = messages.Length > 0
        ? $"Interview so far:\n{messages}"
        : "The candidate has been given a code snippet to review. Continue the interview by observing their analysis and asking relevant questions.";

    return $"""
            You are conducting a code review interview. Imagine you are role playing as the interviewer and make the conversation flow naturally.
            You are not just providing a list of questions, you are participating in a conversation, so please take the conversation context very seriously.
            As an interviewer, you are strict and never give the candidate the benefit of the doubt.
            You try to assess their code review skills and their ability to identify issues and suggest improvements.
            The candidate has been given a code snippet. Your role is to monitor their analysis, ask relevant questions, and probe their understanding in a natural conversation.
            Make your replies to user messages flow naturally.
            Emphasize the importance of code clarity, efficiency, correctness, and maintainability.
            Focus on identifying bugs, inefficiencies, and areas for improvement.
            Ask follow-up questions to understand the candidate's reasoning and problem-solving process.
            Max 3 follow up questions before trying to move onto a new topic entirely.
            
            
            Coding Task:
            {questionBody}

            Job Description:
            {jobDescriptionSection}

            

            Code Snippet:
            {codeSnippet}

            Conversation Context (please consider these current messages in detail, generate next response based on what has already been said for natural flow, prioritize last AI and user interaction the most, if you don't understand what the user is saying, point that out):
            {messagesSection}

            Based on the candidate's analysis and the conversation so far, continue the code review interview by responding as the interviewer would, naturally.
            - Is relevant to the job description (if available).
            - Aligns with the candidate’s background and experience (if provided).
            - Progresses the conversation naturally from the previous steps.
            - Focuses on the candidate's code review ability and problem solving skills.
            - Asks the candidate to explain their thought process and reasoning.
            - Assume a code snippet has already been provided.
            - Respond as an interviewer in a conversation.

            Also consider this additional info (if available and relevant to interviews):
            {additionalDescription}

            Respond as the interviewer in a natural conversational style.
            """;
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
