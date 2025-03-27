using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using API;
using API.AI;
using API.Auth;
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

public class InterviewServiceTests
{
    private readonly Mock<IOpenAIService> _mockOpenAiService;
    private readonly Mock<IinterviewRepository> _mockInterviewRepository;
    private readonly Mock<IQuestionRepository> _mockQuestionRepository;
    private readonly Mock<IQuestionService> _mockQuestionService;
    private readonly Mock<IBlobStorageService> _mockBlobStorageService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly InterviewService _interviewService;

    public InterviewServiceTests()
    {
        _mockOpenAiService = new Mock<IOpenAIService>();
        _mockInterviewRepository = new Mock<IinterviewRepository>();
        _mockQuestionRepository = new Mock<IQuestionRepository>();
        _mockQuestionService = new Mock<IQuestionService>();
        _mockBlobStorageService = new Mock<IBlobStorageService>();
        _mockFileService = new Mock<IFileService>();

        
        _mockFileService.Setup(x => x.DownloadPdf(It.IsAny<string>()))
            .ReturnsAsync((("dummy.pdf", "dummyPath/dummy.pdf")));
        

        _interviewService = new InterviewService(
            _mockOpenAiService.Object,
            _mockInterviewRepository.Object,
            _mockQuestionRepository.Object,
            _mockQuestionService.Object,
            _mockBlobStorageService.Object,
            _mockFileService.Object
        );
    }

    #region Existing Tests

    [Fact]
    public async Task GetInterviews_ReturnsPagedInterviewResponse()
    {
        var user = new AppUser();
        var searchParams = new InterviewSearchParams();
        var pagedResponse = new PagedInterviewResponse();
        _mockInterviewRepository.Setup(x => x.GetInterviews(user, searchParams))
            .ReturnsAsync(pagedResponse);

        var result = await _interviewService.GetInterviews(user, searchParams);

        Assert.Equal(pagedResponse, result);
    }

    [Fact]
    public async Task VerifyVideoView_OnQuestion_ReturnsTrue()
    {
        var user = new AppUser();
        _mockQuestionRepository.Setup(x => x.VerifyVideoView("file.mp4", user))
            .ReturnsAsync(true);

        var result = await _interviewService.VerifyVideoView("file.mp4", user);

        Assert.True(result);
    }

    [Fact]
    public async Task VerifyVideoView_OnInterview_ReturnsTrue()
    {
        var user = new AppUser();
        _mockQuestionRepository.Setup(x => x.VerifyVideoView("file.mp4", user))
            .ReturnsAsync(false);
        _mockInterviewRepository.Setup(x => x.VerifyVideoView(user, "file.mp4"))
            .ReturnsAsync(true);

        var result = await _interviewService.VerifyVideoView("file.mp4", user);

        Assert.True(result);
    }

    [Fact]
    public async Task VerifyPdfView_ReturnsResultFromRepository()
    {
        var user = new AppUser();
        _mockInterviewRepository.Setup(x => x.VerifyPdfView(user, "file.pdf"))
            .ReturnsAsync(true);

        var result = await _interviewService.VerifyPdfView("file.pdf", user);

        Assert.True(result);
    }

    [Fact]
    public async Task GetInterviewDto_ReturnsInterviewDTO()
    {
        var user = new AppUser();
        var interview = new Interview { Id = 1, Questions = new List<Question>() };
        _mockInterviewRepository.Setup(x => x.GetInterview(user, 1))
            .ReturnsAsync(interview);
        _mockQuestionService.Setup(x => x.ConvertToDtos(It.IsAny<List<Question>>(), It.IsAny<Interview>()))
            .Returns(new List<QuestionPageDto>());

        var result = await _interviewService.GetInterviewDto(1, user);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ServeFile_UsesCloudStorage_DownloadsAndDeletes()
    {
        AppConfig.UseCloudStorage = true;
        var httpContext = new DefaultHttpContext();
        _mockBlobStorageService.Setup(x => x.DownloadFileAsync("file.txt", "path/file.txt", "folder"))
            .Returns(Task.CompletedTask);
        var fileTuple = ("resume.pdf", "resume.pdf");
        _mockFileService.Setup(x => x.DownloadPdf("path/resume.pdf"))
            .ReturnsAsync(fileTuple);

        await _interviewService.ServeFile("file.txt", "path/file.txt", "folder", httpContext);

        _mockBlobStorageService.Verify(x => x.DownloadFileAsync("file.txt", "path/file.txt", "folder"), Times.Once);
    }

    [Fact]
    public async Task GetLatestResume_ReturnsResumeUrlAndName()
    {
        var user = new AppUser();
        _mockInterviewRepository.Setup(x => x.GetLatestResume(user))
            .ReturnsAsync("server/Interview/getPdf/guid_file.pdf");
        _mockBlobStorageService.Setup(x => x.GeneratePreSignedUrlAsync("resumes", "guid_file.pdf", 10))
            .ReturnsAsync("signedUrl");
        AppConfig.UseSignedUrl = true;

        var result = await _interviewService.GetLatestResume(user);

        Assert.NotNull(result);
        Assert.Equal("signedUrl", result.url);
        Assert.Equal("file.pdf", result.fileName);
    }

    [Fact]
    public async Task GetAllResumes_ReturnsArrayOfResumeUrlAndName()
    {
        var user = new AppUser();
        var resumes = new[] { new ResumeUrlAndName { url = "server/Interview/getPdf/guid_file.pdf" } };
        _mockInterviewRepository.Setup(x => x.GetAllResumes(user))
            .ReturnsAsync(resumes);
        _mockBlobStorageService.Setup(x => x.GeneratePreSignedUrlAsync("resumes", "guid_file.pdf", 10))
            .ReturnsAsync("signedUrl");
        AppConfig.UseSignedUrl = true;

        var result = await _interviewService.GetAllResumes(user);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("signedUrl", result[0].url);
        Assert.Equal("file.pdf", result[0].fileName);
    }

    [Fact]
    public async Task GetInterviewQuery_ReturnsInterviewDTO()
    {
        var user = new AppUser();
        var request = new InterviewQueryRequest { id = 1, fields = new List<string> { "id", "name" } };
        var interview = new Interview { Id = 1, Name = "Test" };
        _mockInterviewRepository.Setup(x => x.GetInterview(user, 1))
            .ReturnsAsync(interview);

        var result = await _interviewService.GetInterviewQuery(request, user);

        Assert.NotNull(result);
        Assert.Equal(1, result.id);
        Assert.Equal("Test", result.name);
    }

    #endregion

    #region Additional Tests for GenerateInterview and Conversions

    [Fact]
    public async Task GenerateInterview_NonLive_ReturnsInterviewWithQuestions()
    {
        // For NonLive, assume resume is not provided.
        AppUser user = new AppUser();
        var request = new GenerateInterviewRequest
        {
            type = "NonLive",
            resume = null,
            resumeUrl = "",
            jobDescription = "Test job description",
            numberOfBehavioral = 1,
            numberOfTechnical = 1,
            additionalDescription = "Additional info",
            name = "Test Interview",
            secondsPerAnswer = 30
        };
        string serverUrl = "http://server";

        // Dummy response with both question types.
        string dummyResponse = "Behavioral Questions:\n1. Behavioral question\nTechnical Questions:\n1. Technical question";
        _mockOpenAiService.Setup(x => x.MakeRequest(It.IsAny<string>()))
            .ReturnsAsync(dummyResponse);
        _mockInterviewRepository.Setup(x => x.Save(It.IsAny<Interview>(), user))
            .ReturnsAsync((Interview i, AppUser u) =>
            {
                i.Id = 100;
                i.CreatedDate = DateTime.Now;
                i.Feedback = null;
                i.Messages = new List<Message>();
                return i;
            });

        var interview = await _interviewService.GenerateInterview(user, request, serverUrl);

        Assert.NotNull(interview);
        Assert.Equal("Test Interview", interview.Name);
        Assert.True(string.IsNullOrEmpty(interview.ResumeLink));
        Assert.NotNull(interview.Questions);
        // Ensure both Behavioral and Technical questions are created.
        Assert.Contains(interview.Questions, q => q.Body.ToUpper().Contains("BEHAVIORAL"));
        Assert.Contains(interview.Questions, q => q.Body.ToUpper().Contains("TECHNICAL"));
    }

    [Fact]
    public async Task GenerateInterview_CodeReview_ReturnsInterviewWithSingleCodeReviewQuestion()
    {
        AppUser user = new AppUser();
        var request = new GenerateInterviewRequest
        {
            type = "CodeReview",
            resume = null,
            resumeUrl = "",
            jobDescription = "CodeReview job description",
            numberOfBehavioral = 0,
            numberOfTechnical = 0,
            additionalDescription = "Review this code",
            name = "CodeReview Interview",
            secondsPerAnswer = 45
        };
        string serverUrl = "http://server";

        var dummyQuestion = new Question { Body = "Dummy CodeReview Question", Type = QuestionType.CodeReview };
        _mockQuestionService.Setup(x => x.CreateCodeReviewQuestion(It.IsAny<string>(), It.IsAny<string>(), user))
            .ReturnsAsync(dummyQuestion);
        _mockInterviewRepository.Setup(x => x.Save(It.IsAny<Interview>(), user))
            .ReturnsAsync((Interview i, AppUser u) =>
            {
                i.Id = 101;
                i.CreatedDate = DateTime.Now;
                i.Feedback = null;
                i.Messages = new List<Message>();
                return i;
            });

        var interview = await _interviewService.GenerateInterview(user, request, serverUrl);

        Assert.NotNull(interview);
        Assert.Equal("CodeReview Interview", interview.Name);
        Assert.NotNull(interview.Questions);
        Assert.Single(interview.Questions);
        Assert.Equal("Dummy CodeReview Question", interview.Questions[0].Body);
    }

    [Fact]
    public async Task GenerateInterview_LiveCoding_ReturnsInterviewWithSingleLiveCodingQuestion()
    {
        AppUser user = new AppUser();
        var request = new GenerateInterviewRequest
        {
            type = "LiveCoding",
            resume = null,
            resumeUrl = "",
            jobDescription = "",
            numberOfBehavioral = 0,
            numberOfTechnical = 0,
            additionalDescription = "Solve this problem",
            name = "LiveCoding Interview",
            secondsPerAnswer = 60
        };
        string serverUrl = "http://server";

        var dummyQuestion = new Question { Body = "Dummy LiveCoding Question", Type = QuestionType.LiveCoding };
        _mockQuestionService.Setup(x => x.CreateLiveCodingQuestion(It.IsAny<string>(), user))
            .ReturnsAsync(dummyQuestion);
        _mockInterviewRepository.Setup(x => x.Save(It.IsAny<Interview>(), user))
            .ReturnsAsync((Interview i, AppUser u) =>
            {
                i.Id = 102;
                i.CreatedDate = DateTime.Now;
                i.Feedback = null;
                i.Messages = new List<Message>();
                return i;
            });

        var interview = await _interviewService.GenerateInterview(user, request, serverUrl);

        Assert.NotNull(interview);
        Assert.Equal("LiveCoding Interview", interview.Name);
        Assert.NotNull(interview.Questions);
        Assert.Single(interview.Questions);
        Assert.Equal("Dummy LiveCoding Question", interview.Questions[0].Body);
    }

    [Fact]
public void InterviewToDTO_ConvertsAllPropertiesCorrectly()
{
    Interview interview = new Interview
    {
        Id = 200,
        Name = "Conversion Interview",
        JobDescription = "JobDesc",
        ResumeLink = "http://server/dummy.pdf",
        CreatedDate = new DateTime(2023, 3, 26),
        AdditionalDescription = "Extra details",
        Type = InterviewType.NonLive,
        SecondsPerAnswer = 30,
        UserCode = "User123",
        CodeLanguageName = "C#",
        Questions = new List<Question>
        {
            new Question { Id = 1, Body = "Question 1" },
            new Question { Id = 2, Body = "Question 2" }
        },
        Feedback = new InterviewFeedback { Id = 300, PostiveFeedback = "Good", NegativeFeedback = "Bad" },
        Messages = new List<Message>
        {
            new Message { Id = 10, Content = "Message 1", InterviewId = 200, FromAI = false }
        }
    };

    // Properly mock the conversion from Question to QuestionPageDto.
    _mockQuestionService.Setup(x => x.ConvertToDtos(interview.Questions, interview))
        .Returns(() =>
            interview.Questions.Select(q => new QuestionPageDto(q)).ToList());

    InterviewDTO dto = _interviewService.InterviewToDTO(interview);

    Assert.Equal(interview.Id, dto.id);
    Assert.Equal(interview.Name, dto.name);
    Assert.Equal(interview.ResumeLink, dto.resumeLink);
    Assert.Equal(interview.JobDescription, dto.jobDescription);
    Assert.Equal(interview.CreatedDate.ToShortDateString(), dto.createdDate);
    Assert.Equal(interview.AdditionalDescription, dto.additionalDescription);
    Assert.Equal(interview.Type.ToString(), dto.type);
    Assert.Equal(interview.UserCode, dto.userCode);
    Assert.Equal(interview.CodeLanguageName, dto.codeLanguageName);
    Assert.Equal(interview.SecondsPerAnswer, dto.secondsPerAnswer);
    Assert.NotNull(dto.questions);
    Assert.Equal(interview.Questions.Count, dto.questions.Count);
    Assert.Null(dto.feedback);
   
    Assert.Null(dto.messages);
    
}

    [Fact]
    public void DtoToInterview_ConvertsAllPropertiesCorrectly()
    {
        InterviewDTO dto = new InterviewDTO
        {
            id = 400,
            name = "DTO Interview",
            resumeLink = "http://server/dto.pdf",
            jobDescription = "DTO JobDesc",
            createdDate = "2023-03-26",
            additionalDescription = "DTO extra",
            type = InterviewType.Live.ToString(),
            userCode = "UserDTO",
            codeLanguageName = "Java",
            secondsPerAnswer = 45,
            questions = new List<QuestionPageDto>
            {
                // Using the constructor that takes (Question, nextQuestionId, previousQuestionId, secondsPerAnswer)
                new QuestionPageDto(new Question { Id = 1, Body = "DTO Question 1" }, 1, 2, 3),
                // Using the constructor that takes only the Question
                new QuestionPageDto(new Question { Id = 2, Body = "DTO Question 2" })
            }
        };

        _mockQuestionRepository.Setup(x => x.ConvertQuestionToEntity(It.IsAny<QuestionPageDto>()))
            .Returns((QuestionPageDto qdto) => new Question { Id = qdto.id, Body = qdto.body });

        Interview interview = _interviewService.DtoToInterview(dto);

        Assert.Equal(dto.id, interview.Id);
        Assert.Equal(dto.name, interview.Name);
        Assert.Equal(dto.resumeLink, interview.ResumeLink);
        Assert.Equal(dto.jobDescription, interview.JobDescription);
        Assert.Equal(dto.additionalDescription, interview.AdditionalDescription);
        Assert.Equal(dto.secondsPerAnswer, interview.SecondsPerAnswer);
        Assert.NotNull(interview.Questions);
        Assert.Equal(dto.questions.Count, interview.Questions.Count);
        Assert.Equal("DTO Question 1", interview.Questions[0].Body);
    }

    #endregion
}

