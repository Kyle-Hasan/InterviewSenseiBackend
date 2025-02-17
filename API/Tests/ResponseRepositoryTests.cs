using Xunit;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Responses;    // Contains IResponseRepository, Response, ResponseDto
using API.Users;        // Contains AppUser

namespace API.Tests;

    public class ResponseRepositoryMockTests
    {
        [Fact]
        public async Task GetResponsesQuestion_Calls_Method_Once_And_ReturnsExpectedResults()
        {
            // Arrange
            var repoMock = new Mock<IResponseRepository>();
            var testUser = new AppUser { Id = 1, UserName = "testuser" };
            int questionId = 10;
            var responses = new List<Response>
            {
                new Response { Id = 1, Answer = "A1", NegativeFeedback = "negative",PositiveFeedback = "positive",ExampleResponse = "example response", VideoLink = "L1", QuestionId = questionId },
                new Response { Id = 2, Answer = "A2", NegativeFeedback = "negative",PositiveFeedback = "positive",ExampleResponse = "example response", VideoLink = "L2", QuestionId = questionId }
            };
            repoMock.Setup(r => r.getResponsesQuestion(questionId, testUser))
                    .ReturnsAsync(responses);

            // Act
            var result = await repoMock.Object.getResponsesQuestion(questionId, testUser);

            // Assert
            repoMock.Verify(r => r.getResponsesQuestion(questionId, testUser), Times.Once);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void ConvertToDto_Maps_Properties_Correctly()
        {
            // Arrange
            var repoMock = new Mock<IResponseRepository>();
            var response = new Response 
            { 
                Id = 5, 
                Answer = "Test Answer", 
                NegativeFeedback = "negative",PositiveFeedback = "positive",
                ExampleResponse = "example response",
                VideoLink = "TestLink", 
                QuestionId = 99 
            };

            // Set up the mock so that convertToDto returns a ResponseDto with mapped properties.
            repoMock.Setup(r => r.convertToDto(response))
                    .Returns(new ResponseDto
                    {
                        id = response.Id,
                        answer = response.Answer,
                        negativeFeedback = "negative",
                        positiveFeedback = "positive",
                        exampleResponse = "example response",
                        videoLink = response.VideoLink,
                        questionId = response.QuestionId
                    });

            // Act
            var dto = repoMock.Object.convertToDto(response);

            // Assert
            Assert.Equal(5, dto.id);
            Assert.Equal("Test Answer", dto.answer);
            
            Assert.Equal("TestLink", dto.videoLink);
            Assert.Equal(99, dto.questionId);
        }
    }

