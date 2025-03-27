using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.AI;
using API.Interviews;
using API.Questions;
using API.Responses;
using API.Users;
using Moq;
using Xunit;

public class QuestionServiceTests
{
    private readonly Mock<IQuestionRepository> _mockQuestionRepository;
    private readonly Mock<IinterviewRepository> _mockInterviewRepository;
    private readonly Mock<IResponseRepository> _mockResponseRepository;
    private readonly Mock<IOpenAIService> _mockAiService;
    private readonly IQuestionService _questionService;

    public QuestionServiceTests()
    {
        _mockQuestionRepository = new Mock<IQuestionRepository>();
        _mockInterviewRepository = new Mock<IinterviewRepository>();
        _mockResponseRepository = new Mock<IResponseRepository>();
        _mockAiService = new Mock<IOpenAIService>();

        _questionService = new QuestionService(
            _mockQuestionRepository.Object,
            _mockInterviewRepository.Object,
            _mockResponseRepository.Object,
            _mockAiService.Object
        );
    }

    #region GetQuestionAsync and ConvertToDtos

    [Fact]
    public async Task GetQuestionAsync_ReturnsCorrectDtoWithPrevNextIds()
    {
        // Arrange
        var user = new AppUser();
        // Create three questions so we can verify ordering.
        var q1 = new Question { Id = 1, Body = "Question 1" };
        var q2 = new Question { Id = 2, Body = "Question 2" };
        var q3 = new Question { Id = 3, Body = "Question 3" };

        var interview = new Interview
        {
            Id = 100,
            SecondsPerAnswer = 30,
            Questions = new List<Question> { q1, q2, q3 }
        };

        // Set the Interview property on each question.
        q1.Interview = interview;
        q2.Interview = interview;
        q3.Interview = interview;

        // Setup the repository to return q2 when requested.
        _mockQuestionRepository.Setup(x => x.GetQuestionByIdWithInterview(2, user))
            .ReturnsAsync(q2);

        // Act
        QuestionPageDto dto = await _questionService.GetQuestionAsync(2, user);

        // Assert: For question with Id=2 in a three-question interview,
        // previousQuestionId should be 1, nextQuestionId should be 3.
        Assert.Equal(1, dto.previousQuestionId);
        Assert.Equal(3, dto.nextQuestionId);
        Assert.Equal(interview.Id, dto.interviewId);
        Assert.Equal(interview.SecondsPerAnswer, dto.secondsPerAnswer);
    }

    [Fact]
    public void ConvertToDtos_WithEmptyQuestions_ReturnsEmptyList()
    {
        // Arrange
        var interview = new Interview
        {
            Id = 101,
            SecondsPerAnswer = 30,
            Questions = new List<Question>()
        };

        // Act
        var dtos = _questionService.ConvertToDtos(interview.Questions, interview);

        // Assert
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }

    [Fact]
    public void ConvertToDtos_WithNonEmptyQuestions_ReturnsCorrectNumberOfDtos()
    {
        // Arrange
        var q1 = new Question { Id = 1, Body = "Q1" };
        var q2 = new Question { Id = 2, Body = "Q2" };
        var interview = new Interview
        {
            Id = 102,
            SecondsPerAnswer = 45,
            Questions = new List<Question> { q1, q2 }
        };
        q1.Interview = interview;
        q2.Interview = interview;

        // Act
        var dtos = _questionService.ConvertToDtos(interview.Questions, interview);

        // Assert
        Assert.Equal(2, dtos.Count);
        // For first question, previous should be -1; for second, next should be -1.
        var dto1 = dtos.First(q => q.id == 1);
        var dto2 = dtos.First(q => q.id == 2);
        Assert.Equal(-1, dto1.previousQuestionId);
        Assert.Equal(2, dto1.nextQuestionId);
        Assert.Equal(1, dto2.previousQuestionId);
        Assert.Equal(-1, dto2.nextQuestionId);
    }

    #endregion

    #region CreateLiveCodingQuestion and CreateCodeReviewQuestion

    [Fact]
    public async Task CreateLiveCodingQuestion_ReturnsQuestionWithExpectedProperties()
    {
        // Arrange
        var user = new AppUser();
        string additionalDescription = "Live coding additional info";

        // Setup the AI response.
        string aiResponse = "Live coding problem statement";
        _mockAiService.Setup(x => x.MakeRequest(It.Is<string>(s => s.Contains("LeetCode"))))
            .ReturnsAsync(aiResponse);

        // Act
        Question question = await _questionService.CreateLiveCodingQuestion(additionalDescription, user);

        // Assert
        Assert.Equal(aiResponse, question.Body);
        Assert.Equal(QuestionType.LiveCoding, question.Type);
        Assert.False(question.isPremade);
        // Also check that prompt used by CreateLiveCodingQuestion contains the additional description.
        _mockAiService.Verify(x => x.MakeRequest(It.Is<string>(s => s.Contains(additionalDescription))), Times.Once);
    }

    [Fact]
   
    public async Task CreateCodeReviewQuestion_ReturnsQuestionWithExpectedProperties()
    {
        // Arrange
        var user = new AppUser();
        string additionalDescription = "Code review additional info";
        string jobDescription = "Job details for code review";

        // Setup the AI response.
        string aiResponse = "Code review interview question";
        _mockAiService.Setup(x => x.MakeRequest(It.Is<string>(s => 
                s.Contains(jobDescription) && s.Contains(additionalDescription))))
            .ReturnsAsync(aiResponse);

        // Act
        Question question = await _questionService.CreateCodeReviewQuestion(additionalDescription, jobDescription, user);

        // Assert
        Assert.NotNull(question.Body);
        Assert.Equal(QuestionType.CodeReview, question.Type);
        Assert.False(question.isPremade);
        // Verify that the prompt includes both the job description and additional description.
        _mockAiService.Verify(x => x.MakeRequest(It.Is<string>(s => 
            s.Contains(jobDescription) && s.Contains(additionalDescription))), Times.Once);
    }

    #endregion

    #region GetQuestionsByInterviewId

    [Fact]
    public async Task GetQuestionsByInterviewId_ReturnsDtosWhenInterviewFound()
    {
        // Arrange
        var user = new AppUser();
        // Create an interview with questions.
        var q1 = new Question { Id = 1, Body = "Q1" };
        var q2 = new Question { Id = 2, Body = "Q2" };
        var interview = new Interview
        {
            Id = 300,
            SecondsPerAnswer = 50,
            Questions = new List<Question> { q1, q2 }
        };
        q1.Interview = interview;
        q2.Interview = interview;

        _mockInterviewRepository.Setup(x => x.GetInterview(user, 300))
            .ReturnsAsync(interview);

        // Act
        var dtos = await _questionService.GetQuestionsByInterviewId(300, user);

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(2, dtos.Count);
    }

    [Fact]
    public async Task GetQuestionsByInterviewId_ThrowsExceptionWhenInterviewNotFound()
    {
        // Arrange
        var user = new AppUser();
        _mockInterviewRepository.Setup(x => x.GetInterview(user, 999))
            .ReturnsAsync((Interview)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _questionService.GetQuestionsByInterviewId(999, user));
    }

    #endregion
}
