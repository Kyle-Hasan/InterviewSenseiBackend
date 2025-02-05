using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using API.Questions;       
using API.Responses;        
using API.Users;       
using API.Base;
using API.Interviews;

namespace API.Tests;
    

    // Unit tests for ResponseController.
    public class ResponseControllerTests
    {
        // Mocks for dependencies.
        private readonly Mock<IResponseRepository> _responseRepositoryMock;
        private readonly Mock<IResponseService> _responseServiceMock;
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly AppUser _testUser;
        private readonly ResponseController _controller;

        public ResponseControllerTests()
        {
            // Setup a test user.
            _testUser = new AppUser { Id = 1, UserName = "testuser" };

            // Create mocks for the repository and service.
            _responseRepositoryMock = new Mock<IResponseRepository>();
            _responseServiceMock = new Mock<IResponseService>();

            // Creating a UserManager mock requires passing an IUserStore.
            var store = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                store.Object, null, null, null, null, null, null, null, null);
            // When GetUserAsync is called on the UserManager, always return our test user.
            _userManagerMock
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);

            // Instantiate the controller with the mocks.
            _controller = new ResponseController(
                _responseRepositoryMock.Object,
                _responseServiceMock.Object,
                _userManagerMock.Object);

            // Set up a fake HttpContext with Request properties for constructing serverUrl.
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost", 5000);
            httpContext.Request.PathBase = new PathString("/api");
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        // --- Tests for GET /byQuestion ---
        [Fact]
        public async Task GetResponses_ReturnsExpectedResponseDtos()
        {
            // Arrange
            int questionId = 42;
            // Create a list of dummy Response entities using the provided properties.
            var responses = new List<Response>
            {
                new Response { Id = 1, Answer = "Response1", Feedback = "Feedback1", VideoLink = "Link1", QuestionId = questionId },
                new Response { Id = 2, Answer = "Response2", Feedback = "Feedback2", VideoLink = "Link2", QuestionId = questionId }
            };

            // Setup the repository mock to return our dummy list when getResponsesQuestion is called.
            _responseRepositoryMock
                .Setup(r => r.getResponsesQuestion(questionId, _testUser))
                .ReturnsAsync(responses);

            // Setup the conversion function so that each Response is converted to a ResponseDto.
            _responseRepositoryMock
                .Setup(r => r.convertToDto(It.IsAny<Response>()))
                .Returns<Response>(resp => new ResponseDto 
                { 
                    id = resp.Id, 
                    answer = resp.Answer, 
                    feedback = resp.Feedback, 
                    videoLink = resp.VideoLink, 
                    questionId = resp.QuestionId 
                });

            // Act: Call the GET endpoint.
            var result = await _controller.getResponses(questionId);

            // Assert:
            // Verify that we received two ResponseDto objects with the expected properties.
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].id);
            Assert.Equal("Response1", result[0].answer);
            Assert.Equal(2, result[1].id);
            Assert.Equal("Response2", result[1].answer);
        }

        // --- Tests for POST /rateAnswer ---
        [Fact]
        public async Task GetRating_NoVideoProvided_ReturnsBadRequest()
        {
            // Arrange:
            // Create a RatingRequestDTO with a null video to simulate missing file.
            var ratingRequest = new RatingRequestDTO
            {
                questionId = "123",
                video = null
            };

            // Act: Call the POST endpoint.
            var result = await _controller.getRating(ratingRequest);

            // Assert:
            // The controller should return a BadRequest with the message "no video provided".
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("no video provided", badRequestResult.Value);
        }

        [Fact]
        public async Task GetRating_WithVideo_ReturnsExpectedResponseDto()
        {
            // Arrange:
            // Use the video file "testvideo.mp4" in the same directory as the test assembly.
            string videoFilePath = "testvideo.mp4";
            Assert.True(File.Exists(videoFilePath), "The test video file 'testvideo.mp4' must exist in the test directory.");

            // Open the file stream from testvideo.mp4.
            var fileStream = File.OpenRead(videoFilePath);
            IFormFile formFile = new FormFile(fileStream, 0, fileStream.Length, "video", "testvideo.mp4")
            {
                Headers = new HeaderDictionary(),
                ContentType = "video/mp4"
            };

            var ratingRequest = new RatingRequestDTO
            {
                questionId = "123",
                video = formFile
            };

            // Setup the response service mock.
            // When rateAnswer is called, return a dummy ResponseDto with properties matching our Response class.
            ResponseDto dummyResponseDto = new ResponseDto
            {
                id = 10,
                answer = "Rated Answer",
                feedback = "Test Feedback",
                videoLink = "RatedVideoLink",
                questionId = 123
            };
            _responseServiceMock
                .Setup(s => s.rateAnswer(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), _testUser))
                .ReturnsAsync(dummyResponseDto);

            // Ensure that the "Uploads" folder exists because the controller writes the video there.
            string uploadsDir = "Uploads";
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            // Act: Call the POST endpoint.
            var result = await _controller.getRating(ratingRequest);

            // Assert:
            // Verify that the returned ActionResult contains the expected ResponseDto.
            var okResult = Assert.IsType<ResponseDto>(result.Value);
            Assert.Equal(dummyResponseDto.id, okResult.id);
            Assert.Equal(dummyResponseDto.answer, okResult.answer);
            Assert.Equal(dummyResponseDto.feedback, okResult.feedback);
            Assert.Equal(dummyResponseDto.videoLink, okResult.videoLink);
            Assert.Equal(dummyResponseDto.questionId, okResult.questionId);

            // Optionally, check that a file was written to disk in the Uploads folder.
            var files = Directory.GetFiles(uploadsDir);
            Assert.Single(files);

            // Clean up the created file(s) in the Uploads folder.
            foreach (var file in files)
            {
                File.Delete(file);
            }

            // Clean up: Close the file stream.
            fileStream.Close();
        }
    }

