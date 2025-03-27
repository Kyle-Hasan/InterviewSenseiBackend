using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using API.AI;
using API.CodeRunner;
using API.Interviews;
using API.Users;
using Moq;
using Xunit;

// Helper: Fake HttpMessageHandler that returns a predetermined response.
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handlerFunc;
    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handlerFunc(request));
    }
}

public class JudgeZeroServiceTests
{
    private readonly HttpClient _httpClient;
    private readonly JudgeZeroService _judgeZeroService;
    private readonly Mock<IinterviewService> _mockInterviewService;
    private readonly Mock<ICodeSubmissionRepository> _mockCodeSubmissionRepository;
    private readonly AppUser _dummyUser = new AppUser();

    // We'll use fixed environment variables for testing.
    private const string RapidUrl = "https://dummyapi.judge0.com/";
    private const string RapidApiKey = "dummyApiKey";
    private const string RapidHost = "dummyhost.judge0.com";

    public JudgeZeroServiceTests()
    {
        // Set up environment variables for the JudgeZeroService constructor.
        Environment.SetEnvironmentVariable("RAPID_API_KEY", RapidApiKey);
        Environment.SetEnvironmentVariable("RAPID_URL", RapidUrl);
        Environment.SetEnvironmentVariable("RAPID_HOST", RapidHost);

        // Create a fake HttpClient using FakeHttpMessageHandler.
        // For RunCode tests, we'll override the handler per test.
        _httpClient = new HttpClient(new FakeHttpMessageHandler(request =>
        {
            // Default handler (should be overridden in tests)
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("")
            };
        }));

        _mockInterviewService = new Mock<IinterviewService>();
        _mockCodeSubmissionRepository = new Mock<ICodeSubmissionRepository>();

        _judgeZeroService = new JudgeZeroService(_httpClient, _mockInterviewService.Object, _mockCodeSubmissionRepository.Object);
    }

    #region RunCode Tests

    [Fact]
    public async Task RunCode_ReturnsCodeSubmissionResult_WithValidSubmission()
    {
        // Arrange
        var request = new RunCodeRequest
        {
            sourceCode = "print('Hello World')",
            stdin = "",
            languageName = "python3",
            interviewId = 10
        };

        // Setup the interview service's SaveCode method indirectly via UpdateInterview.
        var interview = new Interview { Id = 10, UserCode = "", CodeLanguageName = "" };
        _mockInterviewService.Setup(x => x.GetInterview(It.IsAny<int>(), It.IsAny<AppUser>()))
            .ReturnsAsync(interview);
        _mockInterviewService.Setup(x => x.UpdateInterview(It.IsAny<Interview>(), It.IsAny<AppUser>()))
            .ReturnsAsync(interview);

        // For CreateCodeSubmission, simulate saving and return a CodeSubmission with a known Id and token.
        string expectedToken = "abc123";
        var dummySubmission = new CodeSubmission { Id = 42, Token = expectedToken };
        _mockCodeSubmissionRepository.Setup(x => x.Save(It.IsAny<CodeSubmission>(), It.IsAny<AppUser>()))
            .ReturnsAsync(dummySubmission);

        // Create a fake HttpMessageHandler for the POST.
        var postHandler = new FakeHttpMessageHandler(requestMessage =>
        {
            // Verify URL is built correctly.
            Assert.Contains("submissions/?base64_encoded=false&wait=false", requestMessage.RequestUri.ToString());
            // Return a JSON token result.
            var tokenResult = new TokenResult { token = expectedToken };
            string json = JsonSerializer.Serialize(tokenResult);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        // Replace the HttpClient's handler.
        _httpClient.DefaultRequestHeaders.Clear(); // Clear previous headers if needed.
        var clientForPost = new HttpClient(postHandler);
        // Recreate the service with our new HttpClient.
        var serviceForRunCode = new JudgeZeroService(clientForPost, _mockInterviewService.Object, _mockCodeSubmissionRepository.Object);

        // Act
        CodeSubmissionResult result = await serviceForRunCode.RunCode(request, _dummyUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dummySubmission.Id, result.codeSubmissionId);
    }

    #endregion

    #region GetCodeResult Tests

    [Fact]
    public async Task GetCodeResult_ReturnsNull_WhenStatusIsInQueue()
    {
        // Arrange
        int submissionId = 50;
        string token = "token123";
        var submission = new CodeSubmission { Id = submissionId, Token = token };
        _mockCodeSubmissionRepository.Setup(x => x.GetSubmission(submissionId, _dummyUser))
            .ReturnsAsync(submission);

        // Create a fake HttpMessageHandler for the GET.
        var getHandler = new FakeHttpMessageHandler(requestMessage =>
        {
            // Verify URL is built correctly.
            Assert.Contains($"{RapidUrl}submissions/{token}?base64_encoded=false", requestMessage.RequestUri.ToString());
            // Return a response with status "in queue".
            var runCodeResult = new RunCodeResult
            {
                stdout = "",
                stderr = "",
                memory = 0,
                message = "",
                compileOutput = "",
                codeSubmissionId = submissionId,
                status = new Status { id = 1, description = "In Queue" }
            };
            string json = JsonSerializer.Serialize(runCodeResult);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var clientForGet = new HttpClient(getHandler);
        var serviceForGetResult = new JudgeZeroService(clientForGet, _mockInterviewService.Object, _mockCodeSubmissionRepository.Object);

        // Act
        RunCodeResult result = await serviceForGetResult.GetCodeResult(submissionId, _dummyUser);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCodeResult_ReturnsCompletedResult_WhenStatusIsCompleted()
    {
        // Arrange
        int submissionId = 60;
        string token = "token456";
        var submission = new CodeSubmission { Id = submissionId, Token = token };
        _mockCodeSubmissionRepository.Setup(x => x.GetSubmission(submissionId, _dummyUser))
            .ReturnsAsync(submission);

        // Create a fake HTTP handler for GET.
        var getHandler = new FakeHttpMessageHandler(requestMessage =>
        {
            Assert.Contains($"{RapidUrl}submissions/{token}?base64_encoded=false", requestMessage.RequestUri.ToString());
            var runCodeResult = new RunCodeResult
            {
                stdout = "Output text",
                stderr = "",
                memory = 123456,
                message = "",
                compileOutput = "",
                codeSubmissionId = submissionId,
                status = new Status { id = 3, description = "Accepted" }
            };
            string json = JsonSerializer.Serialize(runCodeResult);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var clientForGet = new HttpClient(getHandler);
        var serviceForGetResult = new JudgeZeroService(clientForGet, _mockInterviewService.Object, _mockCodeSubmissionRepository.Object);

        // Act
        RunCodeResult result = await serviceForGetResult.GetCodeResult(submissionId, _dummyUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Output text", result.stdout);
        Assert.Equal("Accepted", result.status.description);
    }

    #endregion
}
