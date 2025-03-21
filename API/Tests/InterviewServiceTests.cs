using API.Messages;
using API.PDF;

namespace API.Tests;
using API.AI;
using API.AWS;
using API.Interviews;
using API.Questions;
using API.Users;



using Xunit;
using Moq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using API.Interviews;    
using API.AI;           
using API.Users;         



    // Test class for InterviewService
    public class InterviewServiceTests
    {
        private readonly Mock<IOpenAIService> _openAiServiceMock;
        private readonly Mock<IinterviewRepository> _interviewRepositoryMock;
        private readonly Mock<IQuestionRepository> _questionRepositoryMock;
        private readonly Mock<IQuestionService> _questionServiceMock;
        private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
        private readonly Mock<IFileService> _pdfServiceMock;
        private readonly Mock<IMessageService> _messageServiceMock;
        private readonly InterviewService _service;
        private readonly AppUser _testUser;

        public InterviewServiceTests()
        {
            // Create mocks for all dependencies.
            _openAiServiceMock = new Mock<IOpenAIService>();
            _interviewRepositoryMock = new Mock<IinterviewRepository>();
            _questionRepositoryMock = new Mock<IQuestionRepository>();
            _questionServiceMock = new Mock<IQuestionService>();
            _blobStorageServiceMock = new Mock<IBlobStorageService>();
            _messageServiceMock = new Mock<IMessageService>();
            _pdfServiceMock = new Mock<IFileService>();

            // Create an instance of InterviewService using the mocked dependencies.
            _service = new InterviewService(
                _openAiServiceMock.Object,
                _interviewRepositoryMock.Object,
                _questionRepositoryMock.Object,
                _questionServiceMock.Object,
                _blobStorageServiceMock.Object,
                _pdfServiceMock.Object,
                _messageServiceMock.Object);

            // Create a test user instance.
            _testUser = new AppUser { Id = 1, UserName = "testuser" };
        }

        // Test for generateQuestions when a resume PDF is provided.
        [Fact]
        public async Task GenerateQuestions_WithResume_Returns_Correct_Questions()
        {
            // Explanation:
            // This test creates a temporary PDF file to simulate a candidate resume.
            // The InterviewService method will extract text from page 2 onwards.
            string tempPdf = Path.GetTempFileName();
            // Create a minimal valid PDF using iText (assumes iText is available).
            using (var writer = new iText.Kernel.Pdf.PdfWriter(tempPdf))
            {
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                {
                    var document = new iText.Layout.Document(pdf);
                    // Page 1 (dummy, will be skipped by the extraction logic).
                    document.Add(new iText.Layout.Element.Paragraph("Dummy Page 1"));
                    pdf.AddNewPage();
                    // Page 2 contains the resume content.
                    document.Add(new iText.Layout.Element.Paragraph("Resume content for testing."));
                    document.Close();
                }
            }

            // Setup the AI service mock to return a fixed response.
            // Note the fixed format response must match your splitting/parsing logic.
            string aiResponse = @"Behavioral Questions:
1. How do you handle deadlines?
2. Describe a time you resolved a conflict.

Technical Questions:
1. What is dependency injection?
2. Explain the SOLID principles.";
            _openAiServiceMock
                .Setup(s => s.MakeRequest(It.IsAny<string>()))
                .ReturnsAsync(aiResponse);

            // Act: Call generateQuestions.
            var result = await _service.GenerateQuestions("Job Description", 2, 2, tempPdf, "Additional details", "resume.pdf");

            // Assert:
            // We expect exactly two behavioral and two technical questions.
            Assert.Equal(2, result.behavioralQuestions.Length);
            Assert.Equal("How do you handle deadlines?", result.behavioralQuestions[0]);
            Assert.Equal(2, result.technicalQuestions.Length);
            Assert.Equal("What is dependency injection?", result.technicalQuestions[0]);

            // Cleanup the temporary PDF.
            File.Delete(tempPdf);
        }

        // Test that GenerateInterview throws an exception when the interview name is empty.
        [Fact]
        public async Task GenerateInterview_WithEmptyName_ThrowsException()
        {
            // Create request object with an empty name
            var request = new GenerateInterviewRequest
            {
                name = "", // Empty name should trigger exception
                jobDescription = "Job Description",
                numberOfBehavioral = 1,
                numberOfTechnical = 1,
                secondsPerAnswer = 30,
                additionalDescription = ""
            };

            // Expect BadHttpRequestException to be thrown
            await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
                await _service.GenerateInterview(_testUser, request,  "http://server"));
        }

        // Test for GenerateInterview method to ensure it returns an Interview with the expected properties.
        [Fact]
        public async Task GenerateInterview_ReturnsInterviewWithQuestions()
        {
            // Explanation:
            // This test sets up the AI service to return one behavioral and one technical question.
            // We also mock the repository save method to simulate assigning an Id to the Interview.

            string aiResponse = @"Behavioral Questions:
1. Describe a challenging project.

Technical Questions:
1. What is polymorphism?";

            _openAiServiceMock
                .Setup(s => s.MakeRequest(It.IsAny<string>()))
                .ReturnsAsync(aiResponse);

            _interviewRepositoryMock
                .Setup(r => r.Save(It.IsAny<Interview>(), _testUser))
                .ReturnsAsync((Interview i, AppUser user) => { i.Id = 101; return i; });

            // Arrange: Create request object
            var request = new GenerateInterviewRequest
            {
                name = "Interview Test",
                jobDescription = "Job Description",
                numberOfBehavioral = 1,
                numberOfTechnical = 1,
                secondsPerAnswer = 45,
                additionalDescription = "Additional info",
                
            };

            // Act: Generate the interview.
            var interview = await _service.GenerateInterview(_testUser, request, "http://server");

            // Assert:
            // Check that all properties are correctly set.
            Assert.Equal("Interview Test", interview.Name);
            Assert.Equal("Job Description", interview.JobDescription);
            Assert.Equal(2, interview.Questions.Count); // one behavioral + one technical
            Assert.Equal("http://server/resume.pdf", interview.ResumeLink);
            Assert.Equal(45, interview.SecondsPerAnswer);
            Assert.Equal("Additional info", interview.AdditionalDescription);
            Assert.Equal(101, interview.Id);
        }

        // Test for getLatestResume method to ensure the URL is correctly parsed.
        [Fact]
        public async Task GetLatestResume_ReturnsFormattedResume()
        {
            // Explanation:
            // The repository returns a URL that includes a GUID. The service should remove
            // the GUID portion so that only the original file name remains.
            string resumeUrl = "http://example.com/Interview/getPdf/guid_resume.pdf";
            _interviewRepositoryMock
                .Setup(r => r.GetLatestResume(_testUser))
                .ReturnsAsync(resumeUrl);

            // Act: Retrieve the latest resume.
            var resume = await _service.GetLatestResume(_testUser);

            // Assert:
            // Verify that the file name has been extracted correctly.
            Assert.Equal("resume.pdf", resume.fileName);
        }

        // Test for serveFile method to ensure it returns a valid FileStream.
        [Fact]
        public async Task ServeFile_ReturnsFileStream()
        {
            // Explanation:
            // This test creates a temporary file and then verifies that the serveFile method
            // returns a valid stream for that file.
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "Test file content");
            var context = new DefaultHttpContext();

            // Act: Call serveFile.
            var stream = await _service.ServeFile("file.txt", tempFile, "folder", context);

            // Assert: Check that the stream is not null.
            Assert.NotNull(stream);

            // Cleanup: Close the stream and delete the temporary file.
            stream.Close();
            File.Delete(tempFile);
        }
    }

