using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.AI;
using API.AWS;
using API.InteractiveInterviewFeedback;
using API.Interviews;
using API.Messages;
using API.PDF;
using API.Questions;
using API.Responses;
using API.Users;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;





public class InterviewFeedbackServiceTests
{
    private readonly Mock<IinterviewRepository> _mockInterviewRepository;
    private readonly Mock<IinterviewFeedbackRepository> _mockFeedbackRepository;
    private readonly Mock<IOpenAIService> _mockOpenAiService;
    private readonly Mock<IBlobStorageService> _mockBlobStorageService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly IdToMessage _dummyIdToMessage;
    private readonly InterviewFeedbackService _feedbackService;
    private readonly AppUser _dummyUser = new AppUser();

    public InterviewFeedbackServiceTests()
    {
        _mockInterviewRepository = new Mock<IinterviewRepository>();
        _mockFeedbackRepository = new Mock<IinterviewFeedbackRepository>();
        _mockOpenAiService = new Mock<IOpenAIService>();
        _mockBlobStorageService = new Mock<IBlobStorageService>();
        _mockFileService = new Mock<IFileService>();
        _mockMessageService = new Mock<IMessageService>();
        _dummyIdToMessage = new IdToMessage();

        _feedbackService = new InterviewFeedbackService(
            _dummyIdToMessage,
            _mockInterviewRepository.Object,
            _mockFeedbackRepository.Object,
            _mockOpenAiService.Object,
            _mockBlobStorageService.Object,
            _mockFileService.Object,
            _mockMessageService.Object
        );
    }

    #region EndInterview Tests

    [Fact]
    public async Task EndInterview_ThrowsUnauthorizedAccessException_WhenInterviewNotFound()
    {
        // Arrange
        int interviewId = 1;
        _mockInterviewRepository.Setup(x => x.GetInterview(_dummyUser, interviewId))
            .ReturnsAsync((Interview)null);

        var dummyFile = CreateDummyFormFile("dummy.mp4", "dummy content");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _feedbackService.EndInterview(_dummyUser, interviewId, dummyFile, "http://server"));
    }

    [Fact]
    public async Task EndInterview_Succeeds_WithEmptyVideoLink_AndNoCloudStorage()
    {
        // Arrange
        AppConfig.UseCloudStorage = false;
        int interviewId = 2;
        var interview = new Interview
        {
            Id = interviewId,
            VideoLink = "", // Empty video link
            JobDescription = "Job Desc",
            SecondsPerAnswer = 30,
            Questions = new List<Question>
            {
                new Question { Id = 10, Body = "What is your strength?" }
            }
        };
        // Setup interview repository to return the interview.
        _mockInterviewRepository.Setup(x => x.GetInterview(_dummyUser, interviewId))
            .ReturnsAsync(interview);

        // Setup messageService so that if idToMessage.map is empty, it returns some messages.
        _mockMessageService.Setup(x => x.GetMessagesInterview(interviewId, _dummyUser))
            .ReturnsAsync(new List<Message> { new Message { Id = 101, Content = "Message content", InterviewId = interviewId } });

        // Setup openAiService.MakeRequest for feedback prompt.
        // We return a JSON string without formatting markers.
        string feedbackJson = JsonSerializer.Serialize(new InterviewFeedbackJSON
        {
            positiveFeedback = new List<string> { "Good" },
            negativeFeedback = new List<string> { "Bad" }
        });
        _mockOpenAiService.Setup(x => x.MakeRequest(It.IsAny<string>()))
            .ReturnsAsync(feedbackJson);

        // Setup feedback repository.Delete to do nothing.
        _mockFeedbackRepository.Setup(x => x.Delete(It.IsAny<InterviewFeedback>(), _dummyUser))
            .Returns(Task.CompletedTask);

        // Setup interviewRepository.Save to return the interview.
        _mockInterviewRepository.Setup(x => x.Save(interview, _dummyUser))
            .ReturnsAsync(interview);

        // Add a dummy entry to idToMessage map to verify TryRemove.
        _dummyIdToMessage.map.TryAdd(interviewId, new CachedMessageAndResume());

        // Dummy video file.
        var dummyFile = CreateDummyFormFile("dummy.mp4", "video content");

        // Act
        InterviewFeedbackDTO feedbackDto = await _feedbackService.EndInterview(_dummyUser, interviewId, dummyFile, "http://server");

        // Assert
        Assert.NotNull(feedbackDto);
        Assert.Equal("Good", feedbackDto.positiveFeedback);
        Assert.Equal("Bad", feedbackDto.negativeFeedback);
        // Verify that the idToMessage map no longer contains the interviewId.
        Assert.False(_dummyIdToMessage.map.ContainsKey(interviewId));
    }

    [Fact]
    public async Task EndInterview_Succeeds_WithNonEmptyVideoLink_AndCloudStorage()
    {
        // Arrange
        AppConfig.UseCloudStorage = true;
        int interviewId = 3;
        // Simulate an interview with an existing VideoLink.
        var interview = new Interview
        {
            Id = interviewId,
            VideoLink = "http://server/Interview/getVideo/existingVideo.mp4",
            JobDescription = "Job Desc",
            SecondsPerAnswer = 30,
            Questions = new List<Question>
            {
                new Question { Id = 20, Body = "Tell me about your experience." }
            }
        };
        _mockInterviewRepository.Setup(x => x.GetInterview(_dummyUser, interviewId))
            .ReturnsAsync(interview);

        // Setup messageService to return messages.
        _mockMessageService.Setup(x => x.GetMessagesInterview(interviewId, _dummyUser))
            .ReturnsAsync(new List<Message> { new Message { Id = 201, Content = "Some message", InterviewId = interviewId } });

        // Setup openAiService.MakeRequest to return valid JSON feedback.
        string feedbackJson = JsonSerializer.Serialize(new InterviewFeedbackJSON
        {
            positiveFeedback = new List<string> { "Excellent" },
            negativeFeedback = new List<string> { "Needs improvement" }
        });
        _mockOpenAiService.Setup(x => x.MakeRequest(It.IsAny<string>()))
            .ReturnsAsync(feedbackJson);

        // Setup feedbackRepository.Delete if needed.
        _mockFeedbackRepository.Setup(x => x.Delete(It.IsAny<InterviewFeedback>(), _dummyUser))
            .Returns(Task.CompletedTask);

        // Setup interviewRepository.Save.
        _mockInterviewRepository.Setup(x => x.Save(interview, _dummyUser))
            .ReturnsAsync(interview);

        // Setup blob storage upload.
        _mockBlobStorageService.Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), "videos"))
            .ReturnsAsync("cloudKey");

        // Add a dummy entry to idToMessage map.
        _dummyIdToMessage.map.TryAdd(interviewId, new CachedMessageAndResume());

        // Dummy video file.
        var dummyFile = CreateDummyFormFile("dummy.mp4", "video content");

        // Act
        InterviewFeedbackDTO feedbackDto = await _feedbackService.EndInterview(_dummyUser, interviewId, dummyFile, "http://server");

        // Assert
        Assert.NotNull(feedbackDto);
        Assert.Equal("Excellent", feedbackDto.positiveFeedback);
        Assert.Equal("Needs improvement", feedbackDto.negativeFeedback);
        Assert.False(_dummyIdToMessage.map.ContainsKey(interviewId));
    }

    #endregion

    #region GetInterviewFeedback Tests

    [Fact]
    public async Task GetInterviewFeedback_ReturnsFeedbackDto()
    {
        // Arrange
        int interviewId = 4;
        var feedback = new InterviewFeedback { Id = 500, PostiveFeedback = "Strong points", NegativeFeedback = "Weak points" };
        _mockFeedbackRepository.Setup(x => x.GetInterviewFeedbackByInterviewId(interviewId, _dummyUser))
            .ReturnsAsync(feedback);

        // Act
        InterviewFeedbackDTO dto = await _feedbackService.GetInterviewFeedback(_dummyUser, interviewId);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("Strong points", dto.positiveFeedback);
        Assert.Equal("Weak points", dto.negativeFeedback);
        Assert.Equal(500, dto.id);
    }

    #endregion

    // Helper: Creates a dummy IFormFile.
    private IFormFile CreateDummyFormFile(string fileName, string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "dummy", fileName);
    }
}
