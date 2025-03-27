using System;
using System.IO;
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
using Moq;
using Xunit;

public class ResponseServiceTests
{
    private readonly Mock<IOpenAIService> _mockOpenAiService;
    private readonly Mock<IResponseRepository> _mockResponseRepository;
    private readonly Mock<IQuestionRepository> _mockQuestionRepository;
    private readonly Mock<IBlobStorageService> _mockBlobStorageService;
    private readonly ResponseService _responseService;
    private readonly AppUser _dummyUser = new AppUser();

    public ResponseServiceTests()
    {
        _mockOpenAiService = new Mock<IOpenAIService>();
        _mockResponseRepository = new Mock<IResponseRepository>();
        _mockQuestionRepository = new Mock<IQuestionRepository>();
        _mockBlobStorageService = new Mock<IBlobStorageService>();

        // Use the constructor-based implementation.
        _responseService = new ResponseService(
            _mockOpenAiService.Object,
            _mockResponseRepository.Object,
            _mockQuestionRepository.Object,
            _mockBlobStorageService.Object
        );
    }

    [Fact]
public async Task RateAnswer_ReturnsResponseDto_WithCloudStorageTrue()
{
    // Arrange
    AppConfig.UseCloudStorage = true;
    string videoPath = "dummyVideo.mp4";
    string videoName = "dummyVideo.mp4";
    string serverUrl = "http://server";
    int questionId = 123;

    // Assume a dummy user is defined:
    var _dummyUser = new AppUser();

    // Setup blob storage to simulate upload.
    _mockBlobStorageService.Setup(x => x.UploadFileAsync(videoPath, videoName, "videos"))
        .ReturnsAsync("cloudKeyDummy");

    // Setup transcription.
    string transcript = "dummy transcript";
    _mockOpenAiService.Setup(x => x.TranscribeAudioAPI(videoPath,true))
        .ReturnsAsync(transcript);

    // Setup question repository to return a question.
    var dummyQuestion = new Question { Id = questionId, Body = "What is your greatest strength?" };
    _mockQuestionRepository.Setup(x => x.GetQuestionById(questionId, _dummyUser))
        .ReturnsAsync(dummyQuestion);

    // Build a dummy feedback string that will split correctly.
    // Format: somePrefix + splitToken + positive feedback + splitToken + negative feedback + splitToken + example response.
    string splitToken = "@u5W$";
    string dummyFeedback = "prefix" + splitToken + "Positive Feedback" + splitToken + "Negative Feedback" + splitToken + "Example Response";
    _mockOpenAiService.Setup(x => x.MakeRequest(It.Is<string>(s =>
            s.Contains(dummyQuestion.Body) &&
            s.Contains(transcript) &&
            s.Contains(splitToken)
        )))
        .ReturnsAsync(dummyFeedback);

    // Setup response repository update call to return a Response object.
    _mockResponseRepository.Setup(x => x.UpdateAnswer(
            transcript,
            "Positive Feedback",
            "Negative Feedback",
            "Example Response",
            videoName,
            serverUrl,
            dummyQuestion.Id,
            _dummyUser))
        .ReturnsAsync(new Response {
            Answer = "updated response",
            PositiveFeedback = "Positive Feedback",
            NegativeFeedback = "Negative Feedback",
            ExampleResponse = "Example Response",
            VideoLink = serverUrl + "/video",
            QuestionId = dummyQuestion.Id,
            Question = dummyQuestion
        });

    // Act
    ResponseDto responseDto = await _responseService.RateAnswer(questionId, videoPath, videoName, serverUrl, _dummyUser);

    // Assert
    Assert.NotNull(responseDto);
    Assert.Equal("updated response", responseDto.answer);

    // Verify that MakeRequest was called with a prompt containing the expected pieces.
    _mockOpenAiService.Verify(x => x.MakeRequest(It.Is<string>(s =>
        s.Contains(dummyQuestion.Body) &&
        s.Contains(transcript) &&
        s.Contains(splitToken)
    )), Times.Once);

    // Note: File.Delete is a static call; it cannot be verified using Moq.
}


    [Fact]
public async Task RateAnswer_ReturnsResponseDto_WithCloudStorageFalse()
{
    // Arrange
    AppConfig.UseCloudStorage = false;
    string videoPath = "dummyVideo.mp4";
    string videoName = "dummyVideo.mp4";
    string serverUrl = "http://server";
    int questionId = 456;

    // Since UseCloudStorage is false, blob storage upload is not triggered.
    // Setup transcription.
    string transcript = "another dummy transcript";
    _mockOpenAiService.Setup(x => x.TranscribeAudioAPI(videoPath,true))
        .ReturnsAsync(transcript);

    // Setup question repository.
    var dummyQuestion = new Question { Id = questionId, Body = "Describe a challenging project." };
    _mockQuestionRepository.Setup(x => x.GetQuestionById(questionId, _dummyUser))
        .ReturnsAsync(dummyQuestion);

    // Dummy feedback string using the split token.
    string splitToken = "@u5W$";
    string dummyFeedback = "pre" + splitToken + "PosFB" + splitToken + "NegFB" + splitToken + "ExampleResp";
    _mockOpenAiService.Setup(x => x.MakeRequest(It.Is<string>(s =>
        s.Contains(dummyQuestion.Body) &&
        s.Contains(transcript) &&
        s.Contains(splitToken)
    )))
        .ReturnsAsync(dummyFeedback);

    // Setup response repository to return a Response object.
    _mockResponseRepository.Setup(x => x.UpdateAnswer(
            transcript,
            "PosFB",
            "NegFB",
            "ExampleResp",
            videoName,
            serverUrl,
            dummyQuestion.Id,
            _dummyUser))
        .ReturnsAsync(new Response {
            Answer = "response without cloud",
            PositiveFeedback = "PosFB",
            NegativeFeedback = "NegFB",
            ExampleResponse = "ExampleResp",
            VideoLink = serverUrl + "/video", // example video link
            QuestionId = dummyQuestion.Id,
            Question = dummyQuestion
        });

    // Act
    ResponseDto responseDto = await _responseService.RateAnswer(questionId, videoPath, videoName, serverUrl, _dummyUser);

    // Assert
    Assert.NotNull(responseDto);
    Assert.Equal("response without cloud", responseDto.answer);

    // Verify that MakeRequest was called with a prompt containing the expected pieces.
    _mockOpenAiService.Verify(x => x.MakeRequest(It.Is<string>(s =>
        s.Contains(dummyQuestion.Body) &&
        s.Contains(transcript) &&
        s.Contains(splitToken)
    )), Times.Once);
}
}
