using System;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.AI;
using API.Interviews;
using API.Messages;
using API.PDF;
using API.Questions;
using API.Responses;
using API.Users;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;



public class MessageServiceTests : IDisposable
{
    private readonly Mock<IOpenAIService> _mockOpenAiService;
    private readonly Mock<IMessageRepository> _mockMessageRepository;
    private readonly Mock<IinterviewRepository> _mockInterviewRepository;
    private readonly Mock<IFileService> _mockFileService;
    private readonly IdToMessage _idToMessage;
    private readonly MessageService _messageService;
    private readonly string _testPdfPath;
    private readonly AppUser _dummyUser = new AppUser();

    public MessageServiceTests()
    {
        _mockOpenAiService = new Mock<IOpenAIService>();
        _mockMessageRepository = new Mock<IMessageRepository>();
        _mockInterviewRepository = new Mock<IinterviewRepository>();
        _mockFileService = new Mock<IFileService>();
        _idToMessage = new IdToMessage();

        // Assume test.pdf exists in the current directory.
        _testPdfPath = Path.Combine(Directory.GetCurrentDirectory(), "test.pdf");
        if (!File.Exists(_testPdfPath))
        {
            throw new FileNotFoundException("test.pdf file not found in the current directory.");
        }

        // Setup DownloadPdf to return test.pdf.
        _mockFileService.Setup(x => x.DownloadPdf(It.IsAny<string>()))
            .ReturnsAsync(("test.pdf", _testPdfPath));

        _messageService = new MessageService(
            _mockOpenAiService.Object,
            _mockMessageRepository.Object,
            _mockInterviewRepository.Object,
            _mockFileService.Object,
            _idToMessage
        );
    }

    #region ProcessUserMessage Tests

    [Fact]
    public async Task ProcessUserMessage_TextOnly_InitialCacheEmpty_ReturnsMessageResponse()
    {
        // Arrange
        int interviewId = 100;
        var interview = new Interview
        {
            Id = interviewId,
            ResumeLink = "", // avoid triggering file download
            JobDescription = "Job Description Text",
            AdditionalDescription = "Additional interview context",
            Questions = new List<Question> { new Question { Id = 1, Body = "Sample question?" } },
            Messages = new List<Message>()
        };
        _mockInterviewRepository.Setup(x => x.GetInterview(_dummyUser, interviewId))
            .ReturnsAsync(interview);

        var createMessage = new CreateUserMessageDto
        {
            interviewId = interviewId,
            textMessage = "User text message",
            messageType = "Text",
            code = null,
            audio = null
        };

        string aiDummyResponse = "AI reply based on conversation";
        _mockOpenAiService.Setup(x => x.MakeRequest(It.IsAny<string>()))
            .ReturnsAsync(aiDummyResponse);

        _mockInterviewRepository.Setup(x => x.Save(interview, _dummyUser))
            .ReturnsAsync(interview);

        // Act
        MessageResponse response = await _messageService.ProcessUserMessage(_dummyUser, createMessage);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("User text message", response.userMessage);
        Assert.Equal(aiDummyResponse, response.aiResponse);
        Assert.True(_idToMessage.map.ContainsKey(interviewId));
    }

    [Fact]
    public async Task ProcessUserMessage_AudioProvided_WithPrepopulatedCache_ReturnsMessageResponse()
    {
        // Arrange
        int interviewId = 200;
        var interview = new Interview
        {
            Id = interviewId,
            ResumeLink = "", // For simplicity
            JobDescription = "Job Desc for audio",
            AdditionalDescription = "Some additional info",
            Questions = new List<Question> { new Question { Id = 10, Body = "What is your background?" } },
            Messages = new List<Message>()
        };
        _mockInterviewRepository.Setup(x => x.GetInterview(_dummyUser, interviewId))
            .ReturnsAsync(interview);

        var prepopulatedContext = new CachedMessageAndResume(new List<Message>(), "Prepopulated resume text");
        _idToMessage.map.TryAdd(interviewId, prepopulatedContext);

        var dummyAudioFile = CreateDummyFormFile("audio.mp3", "audio content");
        var createMessage = new CreateUserMessageDto
        {
            interviewId = interviewId,
            textMessage = null,
            messageType = "Text", // Audio provided will be transcribed.
            code = null,
            audio = dummyAudioFile
        };

        string audioTranscript = "Audio transcript text";
        _mockOpenAiService.Setup(x => x.TranscribeAudioAPI(It.IsAny<string>(),false))
            .ReturnsAsync(audioTranscript);

        string aiResponse = "AI response for audio message";
        _mockOpenAiService.Setup(x => x.MakeRequest(It.IsAny<string>()))
            .ReturnsAsync(aiResponse);

        _mockInterviewRepository.Setup(x => x.Save(interview, _dummyUser))
            .ReturnsAsync(interview);

        // Act
        MessageResponse response = await _messageService.ProcessUserMessage(_dummyUser, createMessage);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(audioTranscript, response.userMessage);
        Assert.Equal(aiResponse, response.aiResponse);
        Assert.True(_idToMessage.map.ContainsKey(interviewId));
        Assert.Contains(_idToMessage.map[interviewId].Messages, m => m.Content == audioTranscript);
    }

    #endregion

    #region GetInitialInterviewMessage Tests

    [Fact]
    public async Task GetInitialInterviewMessage_InitialCacheEmpty_ReturnsMessageDTOAndUpdatesCache()
    {
        // Arrange
        int interviewId = 300;
        var interview = new Interview
        {
            Id = interviewId,
            ResumeLink = "test.pdf", // triggers DownloadPdf using our test.pdf
            JobDescription = "Interview Job Description",
            AdditionalDescription = "Additional info",
            Questions = new List<Question> { new Question { Id = 50, Body = "Initial question?" } },
            Messages = new List<Message>()
        };
        _mockInterviewRepository.Setup(x => x.GetInterview(_dummyUser, interviewId))
            .ReturnsAsync(interview);

        string aiResponse = "Initial AI interview response";
        _mockOpenAiService.Setup(x => x.MakeRequest(It.IsAny<string>()))
            .ReturnsAsync(aiResponse);

        _mockInterviewRepository.Setup(x => x.Save(interview, _dummyUser))
            .ReturnsAsync(interview);

        // Act
        MessageDTO dto = await _messageService.GetInitialInterviewMessage(_dummyUser, interviewId);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(aiResponse, dto.content);
        Assert.True(_idToMessage.map.ContainsKey(interviewId));
    }

    #endregion

    #region GetMessagesInterview Test

    [Fact]
    public async Task GetMessagesInterview_ReturnsExpectedMessages()
    {
        int interviewId = 400;
        var messagesList = new List<Message>
        {
            new Message { Id = 1, Content = "Message one", InterviewId = interviewId, FromAI = false },
            new Message { Id = 2, Content = "Message two", InterviewId = interviewId, FromAI = true }
        };
        _mockMessageRepository.Setup(x => x.GetMessagesInterview(interviewId, _dummyUser))
            .ReturnsAsync(messagesList);

        List<Message> result = await _messageService.GetMessagesInterview(interviewId, _dummyUser);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.Content == "Message one");
        Assert.Contains(result, m => m.Content == "Message two");
    }

    #endregion

    // Helper: Create a dummy IFormFile from a string.
    private IFormFile CreateDummyFormFile(string fileName, string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "dummy", fileName);
    }

    public void Dispose()
    {
        // No disposal needed for the external test.pdf.
        // Optionally, you could add cleanup code here if you created temporary files.
    }
}
