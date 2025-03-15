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


using API.Interviews;            
using API.Questions;           
using API.Responses;            
using API.Users;                 
using API.AWS;                   
using API.Base;                  
using API.Extensions;
using API.PDF;

namespace API.Interviews.Tests
{
  
    

    public class InterviewControllerTests
    {
        // Mocks for dependencies.
        private readonly Mock<IinterviewService> _interviewServiceMock;
        private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<IPDFService> _pdfServiceMock;
        private readonly InterviewController _controller;
        private readonly AppUser _testUser;

        public InterviewControllerTests()
        {
            // Create a test user.
            _testUser = new AppUser { Id = 1, UserName = "testuser" };

            // Create mocks.
            _interviewServiceMock = new Mock<IinterviewService>();
            _blobStorageServiceMock = new Mock<IBlobStorageService>();
            _pdfServiceMock = new Mock<IPDFService>();

            var store = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                            .ReturnsAsync(_testUser);

            // Instantiate the controller with the mocks.
            _controller = new InterviewController(
                _interviewServiceMock.Object,
                _userManagerMock.Object,
                _blobStorageServiceMock.Object,
                _pdfServiceMock.Object);

            // Set a fake HttpContext.
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost", 5000);
            httpContext.Request.PathBase = new PathString("/api");
            // For endpoints that add pagination headers.
            httpContext.Response.Body = new MemoryStream();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        // POST: generateQuestions
        [Fact]
        public async Task GetQuestions_ReturnsGenerateQuestionsResponse()
        {
            // Arrange
            // Create a dummy resume file from a memory stream.
            string resumeContent = "dummy resume content";
            var resumeStream = new MemoryStream(Encoding.UTF8.GetBytes(resumeContent));
            IFormFile resumeFile = new FormFile(resumeStream, 0, resumeStream.Length, "resume", "resume.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var request = new GenerateInterviewRequest
            {
                resume = resumeFile,
                jobDescription = "Job Description",
                numberOfBehavioral = 2,
                numberOfTechnical = 2,
                additionalDescription = "Additional info"
            };

            // Setup the interview service to return a dummy response.
            var dummyGenerateResponse = new GenerateQuestionsResponse
            {
                behavioralQuestions = new string[] { "Behavioral Q1", "Behavioral Q2" },
                technicalQuestions = new string[] { "Technical Q1", "Technical Q2" }
            };

            _interviewServiceMock.Setup(s => s.GenerateQuestions(
                request.jobDescription,
                request.numberOfBehavioral,
                request.numberOfTechnical,
                It.IsAny<string>(), // filePath (computed by the controller)
                request.additionalDescription,
                It.IsAny<string>()  // fileName (computed by the controller)
            )).ReturnsAsync(dummyGenerateResponse);

            // Act
            var actionResult = await _controller.GetQuestions(request);
            var result = actionResult.Value;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.behavioralQuestions.Length);
            Assert.Equal("Behavioral Q1", result.behavioralQuestions[0]);
        }

        // POST: generateInterview
        [Fact]
        public async Task GenerateInterview_WithValidRequest_ReturnsInterviewDTO()
        {
            // Arrange
            string resumeContent = "dummy pdf content";
            var resumeStream = new MemoryStream(Encoding.UTF8.GetBytes(resumeContent));
            IFormFile resumeFile = new FormFile(resumeStream, 0, resumeStream.Length, "resume", "resume.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var request = new GenerateInterviewRequest
            {
                name = "Interview Name",
                jobDescription = "Job Description",
                numberOfBehavioral = 2,
                numberOfTechnical = 2,
                secondsPerAnswer = 30,
                additionalDescription = "Additional info",
                resume = resumeFile,
                resumeUrl = null // we use resume file in this case
            };

            // Setup the interview service to return a dummy Interview.
            var dummyInterview = new Interview
            {
                Id = 123,
                Name = request.name,
                JobDescription = request.jobDescription,
                SecondsPerAnswer = request.secondsPerAnswer,
                AdditionalDescription = request.additionalDescription,
                ResumeLink = "http://localhost/api/Interview/getPdf/dummy.pdf",
                Questions = new List<Question>() // omitted for simplicity.
            };

            _interviewServiceMock.Setup(s => s.GenerateInterview(
                _testUser,
                request.name,
                request.jobDescription,
                request.numberOfBehavioral,
                request.numberOfTechnical,
                request.secondsPerAnswer,
                It.IsAny<string>(), // filePath computed by controller
                request.additionalDescription,
                It.IsAny<string>(), // fileName computed by controller
                It.IsAny<string>(), 
                false// serverUrl computed by controller
            )).ReturnsAsync(dummyInterview);

            var dummyInterviewDTO = new InterviewDTO
            {
                id = dummyInterview.Id,
                name = dummyInterview.Name,
                jobDescription = dummyInterview.JobDescription,
                resumeLink = dummyInterview.ResumeLink,
                secondsPerAnswer = dummyInterview.SecondsPerAnswer,
                additionalDescription = dummyInterview.AdditionalDescription,
                questions = new List<QuestionPageDto>()
            };
            _interviewServiceMock.Setup(s => s.InterviewToDTO(dummyInterview))
                                  .Returns(dummyInterviewDTO);

            // Act
            var actionResult = await _controller.GenerateInterview(request);
            var resultDTO = actionResult.Value;

            // Assert
            Assert.NotNull(resultDTO);
            Assert.Equal(dummyInterviewDTO.id, resultDTO.id);
            Assert.Equal(dummyInterviewDTO.name, resultDTO.name);
        }

        // DELETE: {id}
        [Fact]
        public async Task DeleteInterview_CallsServiceMethods()
        {
            // Arrange
            int interviewId = 123;
            var dummyInterview = new Interview { Id = interviewId };
            _interviewServiceMock.Setup(s => s.GetInterview(interviewId, _testUser))
                                  .ReturnsAsync(dummyInterview);
            _interviewServiceMock.Setup(s => s.DeleteInterview(dummyInterview, _testUser))
                                  .Returns(Task.CompletedTask);

            // Act
            await _controller.Delete(interviewId);

            // Assert
            _interviewServiceMock.Verify(s => s.DeleteInterview(dummyInterview, _testUser), Times.Once);
        }

        // PUT: update
        [Fact]
        public async Task UpdateInterview_ReturnsUpdatedInterviewDTO()
        {
            // Arrange
            var interviewDTO = new InterviewDTO
            {
                id = 123,
                name = "Updated Interview",
                jobDescription = "Job Desc",
                resumeLink = "link",
                secondsPerAnswer = 40,
                additionalDescription = "Additional",
                questions = new List<QuestionPageDto>()
            };

            var interviewEntity = new Interview { Id = interviewDTO.id, Name = interviewDTO.name };
            _interviewServiceMock.Setup(s => s.DtoToInterview(interviewDTO)).Returns(interviewEntity);
            _interviewServiceMock.Setup(s => s.UpdateInterview(interviewEntity, _testUser))
                                  .ReturnsAsync(interviewEntity);
            _interviewServiceMock.Setup(s => s.InterviewToDTO(interviewEntity))
                                  .Returns(interviewDTO);

            // Act
            var resultDTO = await _controller.Update(interviewDTO);

            // Assert
            Assert.NotNull(resultDTO);
            Assert.Equal(interviewDTO.id, resultDTO.id);
            Assert.Equal(interviewDTO.name, resultDTO.name);
        }

        // GET: {id}
        [Fact]
        public async Task GetInterview_ReturnsInterviewDTO()
        {
            // Arrange
            int interviewId = 123;
            var dummyInterviewDTO = new InterviewDTO { id = interviewId, name = "Test Interview" };
            _interviewServiceMock.Setup(s => s.GetInterviewDto(interviewId, _testUser))
                                  .ReturnsAsync(dummyInterviewDTO);

            // Act
            var resultDTO = await _controller.GetInterview(interviewId);

            // Assert
            Assert.NotNull(resultDTO);
            Assert.Equal(interviewId, resultDTO.id);
        }

        // GET: interviewList
        [Fact]
        public async Task GetInterviewList_ReturnsListOfInterviewDTOs_AndAddsPaginationHeader()
        {
            // Arrange
            var dummyInterviews = new List<Interview>
            {
                new Interview { Id = 1, Name = "Interview1" },
                new Interview { Id = 2, Name = "Interview2" }
            };
            var dummyDTOs = new List<InterviewDTO>
            {
                new InterviewDTO { id = 1, name = "Interview1" },
                new InterviewDTO { id = 2, name = "Interview2" }
            };
            var pagedResponse = new PagedInterviewResponse
            {
                total = 2,
                interviews = dummyInterviews
            };
            var searchParams = new InterviewSearchParams { startIndex = 0, pageSize = 10 };

            _interviewServiceMock.Setup(s => s.GetInterviews(_testUser, searchParams))
                                  .ReturnsAsync(pagedResponse);
            _interviewServiceMock.Setup(s => s.InterviewToDTO(dummyInterviews[0])).Returns(dummyDTOs[0]);
            _interviewServiceMock.Setup(s => s.InterviewToDTO(dummyInterviews[1])).Returns(dummyDTOs[1]);

            // Act
            var resultDTOs = await _controller.GetInterviewList(searchParams);

            // Assert
            Assert.NotNull(resultDTOs);
            Assert.Equal(2, resultDTOs.Count);
            // Verify that a "Pagination" header was added to the response.
            Assert.True(_controller.Response.Headers.ContainsKey("Pagination"));
        }

        // GET: getVideo/{fileName}
        [Fact]
        public async Task GetVideo_ReturnsUnauthorized_WhenVerifyVideoViewFails()
        {
            // Arrange
            string fileName = "video1.webm";
            _interviewServiceMock.Setup(s => s.VerifyVideoView(fileName, _testUser))
                                  .ReturnsAsync(false);

            // Act
            var result = await _controller.GetVideo(fileName);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetVideo_ReturnsFileResult_WhenNotUsingSignedUrl()
        {
            // Arrange
            // Arrange
            string fileName = "test1.mp4";  
            
            _interviewServiceMock.Setup(s => s.VerifyPdfView(fileName, _testUser))
                .ReturnsAsync(true);

            string uploadsDir = "Uploads";
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);
            string filePath = Path.Combine(uploadsDir, fileName);

            // Ensure that test1.mp4 exists in the test directory.
            Assert.True(File.Exists("test1.mp4"), "The file '\"test1.mp4\"' must exist in the test directory.");

            // Copy test1.mp4 to the Uploads folder.
            File.Copy("test1.mp4", filePath, overwrite: true);

            // Open a stream for the copied file.
            var fileStream = File.OpenRead(filePath);
            _interviewServiceMock.Setup(s => s.ServeFile(fileName, filePath, "videos", It.IsAny<HttpContext>()))
                .ReturnsAsync(fileStream);


            // Act
            var result = await _controller.GetVideo(fileName);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("video/webm", fileResult.ContentType);

            // Cleanup
        
            File.Delete(filePath);
        }

        [Fact]
        public async Task GetVideo_ReturnsOkWithSignedUrl_WhenUsingSignedUrl()
        {
            // Arrange
            string fileName = "video1.webm";
        
            _interviewServiceMock.Setup(s => s.VerifyVideoView(fileName, _testUser))
                                  .ReturnsAsync(true);
            _blobStorageServiceMock.Setup(s => s.GeneratePreSignedUrlAsync("videos", fileName, 10))
                                   .ReturnsAsync("http://signedurl/video1.webm");

            // Act
            var result = await _controller.GetVideo(fileName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("http://signedurl/video1.webm", okResult.Value);
        }

        // GET: getPdf/{fileName}
        [Fact]
        public async Task GetPdf_ReturnsUnauthorized_WhenVerifyPdfViewFails()
        {
            // Arrange
            string fileName = "resume.pdf";
            _interviewServiceMock.Setup(s => s.VerifyPdfView(fileName, _testUser))
                                  .ReturnsAsync(false);

            // Act
            var result = await _controller.GetResume(fileName);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetPdf_ReturnsFileResult_WhenNotUsingSignedUrl()
        {
            // Arrange
            string fileName = "test.pdf";  // Use test.pdf instead of resume.pdf.
          
            _interviewServiceMock.Setup(s => s.VerifyPdfView(fileName, _testUser))
                .ReturnsAsync(true);

            string uploadsDir = "Uploads";
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);
            string filePath = Path.Combine(uploadsDir, fileName);

            // Ensure that test.pdf exists in the test directory.
            Assert.True(File.Exists("test.pdf"), "The file 'test.pdf' must exist in the test directory.");

            // Copy test.pdf to the Uploads folder.
            File.Copy("test.pdf", filePath, overwrite: true);

            // Open a stream for the copied file.
            var fileStream = File.OpenRead(filePath);
            _interviewServiceMock.Setup(s => s.ServeFile(fileName, filePath, "resumes", It.IsAny<HttpContext>()))
                .ReturnsAsync(fileStream);

            // Act
            var result = await _controller.GetResume(fileName);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);

            // Cleanup
         
            File.Delete(filePath);
        }

        [Fact]
        public async Task GetPdf_ReturnsOkWithSignedUrl_WhenUsingSignedUrl()
        {
            // Arrange
            string fileName = "resume.pdf";
       
            _interviewServiceMock.Setup(s => s.VerifyPdfView(fileName, _testUser))
                                  .ReturnsAsync(true);
            _blobStorageServiceMock.Setup(s => s.GeneratePreSignedUrlAsync("resumes", fileName, 10))
                                   .ReturnsAsync("http://signedurl/resume.pdf");

            // Act
            var result = await _controller.GetResume(fileName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("http://signedurl/resume.pdf", okResult.Value);
        }

        // GET: getLatestResume
        [Fact]
        public async Task GetLatestResume_ReturnsResumeUrlAndName()
        {
            // Arrange
            var expected = new ResumeUrlAndName { url = "http://example.com/resume.pdf", fileName = "resume.pdf" };
            _interviewServiceMock.Setup(s => s.GetLatestResume(_testUser))
                                  .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetLatestResume();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.url, result.url);
            Assert.Equal(expected.fileName, result.fileName);
        }

        // GET: getAllResumes
        [Fact]
        public async Task GetAllResumes_ReturnsArrayOfResumes()
        {
            // Arrange
            var expected = new ResumeUrlAndName[]
            {
                new ResumeUrlAndName { url = "http://example.com/resume1.pdf", fileName = "resume1.pdf" },
                new ResumeUrlAndName { url = "http://example.com/resume2.pdf", fileName = "resume2.pdf" }
            };
            _interviewServiceMock.Setup(s => s.GetAllResumes(_testUser))
                                  .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetAllResumes();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
        }
    }
}
