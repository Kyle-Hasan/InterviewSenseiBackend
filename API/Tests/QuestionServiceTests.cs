using Xunit;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Interviews;    // For Interview
using API.Questions;     // For IQuestionRepository, Question, IQuestionService, QuestionPageDto, QuestionService
using API.Responses;     // For IResponseRepository, Response, ResponseDto
using API.Users;         // For AppUser

namespace API.Tests;

    public class QuestionServiceTests
    {
        [Fact]
        public async Task GetQuestionAsync_ReturnsCorrectQuestionPageDto()
        {
            // Arrange
            var questionRepoMock = new Mock<IQuestionRepository>();
            var interviewRepoMock = new Mock<IinterviewRepository>();
            var responseRepoMock = new Mock<IResponseRepository>();

            // Instantiate the service with mocks.
            var service = new QuestionService(questionRepoMock.Object, interviewRepoMock.Object, responseRepoMock.Object);

            // Create a dummy interview with three questions.
            var interview = new Interview
            {
                Id = 1,
                secondsPerAnswer = 30,
                Questions = new List<Question>()
            };

            var q1 = new Question { Id = 1, Body = "Question 1", Type = "Behavioral", Responses = new List<Response>(), Interview = interview };
            var q2 = new Question { Id = 2, Body = "Question 2", Type = "Technical", Responses = new List<Response>(), Interview = interview };
            var q3 = new Question { Id = 3, Body = "Question 3", Type = "Behavioral", Responses = new List<Response>(), Interview = interview };

            interview.Questions.AddRange(new[] { q1, q2, q3 });

            // Add one dummy response to q2.
            var resp = new Response { Id = 101 };
            q2.Responses.Add(resp);

            // Set up the question repository so that getQuestionByIdWithInterview returns q2.
            var testUser = new AppUser { Id = 10, UserName = "dummy" };
            questionRepoMock.Setup(r => r.GetQuestionByIdWithInterview(2, testUser))
                            .ReturnsAsync(q2);

            // Setup the response repository so that convertToDto returns a predictable ResponseDto.
            responseRepoMock.Setup(r => r.ConvertToDto(It.IsAny<Response>()))
                            .Returns((Response r) => new ResponseDto
                            {
                                id = r.Id,
                                answer = $"Response {r.Id}",
                                negativeFeedback= "Positive Feedback",
                                positiveFeedback = "Negative Feedback",
                                exampleResponse = "Example Response",
                                videoLink = "Link",
                                questionId = 2
                            });

            // Act
            var dto = await service.GetQuestionAsync(2, testUser);

            // Assert
            // Check basic properties.
            Assert.Equal("Question 2", dto.body);
            Assert.Equal(2, dto.id);
            Assert.Equal("Technical", dto.type);
            // With questions sorted by Id, the previous question for q2 is q1 (Id=1) and the next is q3 (Id=3).
            Assert.Equal(1, dto.previousQuestionId);
            Assert.Equal(3, dto.nextQuestionId);
            Assert.Equal(1, dto.interviewId);
            Assert.Equal(30, dto.secondsPerAnswer);
            // Verify that the responses list contains one converted response with Id=101.
            Assert.Single(dto.responses);
            Assert.Equal(101, dto.responses.First().id);
        }

        [Fact]
        public void ConvertToDtos_ReturnsListWithCorrectPreviousAndNextQuestionIds()
        {
            // Arrange
            var questionRepoMock = new Mock<IQuestionRepository>();
            var interviewRepoMock = new Mock<IinterviewRepository>();
            var responseRepoMock = new Mock<IResponseRepository>();

            var service = new QuestionService(questionRepoMock.Object, interviewRepoMock.Object, responseRepoMock.Object);

            // Create an interview with two questions.
            var interview = new Interview
            {
                Id = 5,
                secondsPerAnswer = 45,
                Questions = new List<Question>()
            };
            var q1 = new Question { Id = 10, Body = "Q1", Type = "Type1", Responses = new List<Response>(), Interview = interview };
            var q2 = new Question { Id = 20, Body = "Q2", Type = "Type2", Responses = new List<Response>(), Interview = interview };
            interview.Questions.AddRange(new[] { q1, q2 });

            // For this test, we assume no responses (or they are not needed).
            responseRepoMock.Setup(r => r.ConvertToDto(It.IsAny<Response>()))
                            .Returns((Response r) => new ResponseDto { id = r.Id, answer = "Resp", negativeFeedback = "NFB", positiveFeedback = "PFB",exampleResponse = "Response Example", videoLink = "L", questionId = 0 });

            // Act
            var dtos = service.ConvertToDtos(interview.Questions, interview);

            // Assert: Two DTOs should be returned.
            Assert.Equal(2, dtos.Count);
            // For the first DTO, no previous question exists.
            Assert.Equal(-1, dtos[0].previousQuestionId);
            // Its next question ID should be the second question's ID.
            Assert.Equal(20, dtos[0].nextQuestionId);
            // For the second DTO, the previous question ID is 10 and no next question exists.
            Assert.Equal(10, dtos[1].previousQuestionId);
            Assert.Equal(-1, dtos[1].nextQuestionId);
        }

        [Fact]
        public async Task GetQuestionsByInterviewId_ReturnsDtosForAllQuestionsInInterview()
        {
            // Arrange
            var questionRepoMock = new Mock<IQuestionRepository>();
            var interviewRepoMock = new Mock<IinterviewRepository>();
            var responseRepoMock = new Mock<IResponseRepository>();

            var service = new QuestionService(questionRepoMock.Object, interviewRepoMock.Object, responseRepoMock.Object);

            // Create an interview with three questions.
            var interview = new Interview
            {
                Id = 100,
                secondsPerAnswer = 60,
                Questions = new List<Question>()
            };
            var q1 = new Question { Id = 1, Body = "First", Type = "A", Responses = new List<Response>(), Interview = interview };
            var q2 = new Question { Id = 2, Body = "Second", Type = "B", Responses = new List<Response>(), Interview = interview };
            var q3 = new Question { Id = 3, Body = "Third", Type = "C", Responses = new List<Response>(), Interview = interview };
            interview.Questions.AddRange(new[] { q1, q2, q3 });

            var testUser = new AppUser { Id = 50, UserName = "test" };

            // Set up the interview repository so that getInterview returns our dummy interview.
            interviewRepoMock.Setup(r => r.GetInterview(testUser, 100))
                             .ReturnsAsync(interview);

            // Setup the response repository conversion (if any responses exist; here questions have empty responses).
            responseRepoMock.Setup(r => r.ConvertToDto(It.IsAny<Response>()))
                            .Returns((Response r) => new ResponseDto { id = r.Id, answer = "Resp", negativeFeedback = "NFB", positiveFeedback = "PFB",exampleResponse = "Response Example", videoLink = "L", questionId = 0 });

            // Act
            var dtos = await service.GetQuestionsByInterviewId(100, testUser);

            // Assert: We expect three DTOs corresponding to the three questions.
            Assert.Equal(3, dtos.Count);
            // The questions should be sorted by their Id (ascending).
            Assert.Equal(1, dtos[0].id);
            Assert.Equal(2, dtos[1].id);
            Assert.Equal(3, dtos[2].id);
        }
    }

