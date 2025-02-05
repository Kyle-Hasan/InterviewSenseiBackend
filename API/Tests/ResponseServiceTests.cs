using Xunit;
using Moq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using API.Responses;    // Contains IResponseService, ResponseDto
using API.Users;        // Contains AppUser

namespace API.Tests;

    public class ResponseServiceMockTests
    {
        [Fact]
        public async Task RateAnswer_ReturnsResponseDto_WithAnswerInExpectedFormat()
        {
            // Arrange
            var serviceMock = new Mock<IResponseService>();
            int questionId = 55;
            string videoName = "testvideo.mp4";
            string serverUrl = "http://localhost/api/Interview/getVideo";
            var testUser = new AppUser { Id = 2, UserName = "user2" };

            // Use the actual video file "testvideo.mp4" in the same directory.
            string filePath = "testvideo.mp4";
            Assert.True(File.Exists(filePath), "The test video file 'testvideo.mp4' must exist in the test directory.");

            // Define the split token.
            string _splitToken = "@u5W$";

            // Build a regex pattern to verify the expected format.
            // The pattern expects:
            // - The string starts with "Response should always be in format '"
            // - Followed by the split token, a space, "Good: " then some text, a space, the split token,
            // - then a space, "Needs Improvement: " then some text, followed by a closing single quote,
            // - then a space and the literal "Absolutely DO NOT forget the $" followed by the split token,
            // - then " or this format or else the program breaks." and optional trailing whitespace.
            string pattern = @"^Response should always be in format '@u5W\$ Good: .+? @u5W\$ Needs Improvement: .+?' Absolutely DO NOT forget the \$@u5W\$ or this format or else the program breaks\.\s*$";

            // Setup the service mock so that when rateAnswer is called, it returns a ResponseDto
            // with an answer string that matches the expected format.
            var expectedDto = new ResponseDto
            {
                id = 100,
                answer = $"Response should always be in format '{_splitToken} Good: insert your answer here {_splitToken} Needs Improvement: insert your answer here' Absolutely DO NOT forget the ${_splitToken} or this format or else the program breaks.",
                feedback = "Auto feedback",
                videoLink = $"{serverUrl}/{videoName}",
                questionId = questionId
            };

            serviceMock.Setup(s => s.rateAnswer(questionId, filePath, videoName, serverUrl, testUser))
                       .ReturnsAsync(expectedDto);

            // Act
            var dto = await serviceMock.Object.rateAnswer(questionId, filePath, videoName, serverUrl, testUser);

            // Assert
            Assert.Equal(100, dto.id);
            Assert.Equal("Rated Answer", dto.answer);
            Assert.Equal($"{serverUrl}/{videoName}", dto.videoLink);
            Assert.Equal(questionId, dto.questionId);

            // Verify that the answer property matches the expected format using a regex.
            var regex = new Regex(pattern);
            Assert.Matches(regex, dto.feedback);
        }
    }

