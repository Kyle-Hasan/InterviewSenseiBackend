using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using API.Interviews;         
using API.Questions;        
using API.Responses;       
using API.Users;           
using API.Data;
namespace API.Tests;


  

    public class QuestionRepositoryTests
    {
        private readonly AppDbContext _context;
        private readonly QuestionRepository _repository;
        private readonly Mock<IResponseRepository> _responseRepositoryMock;
        private readonly AppUser _testUser;

        public QuestionRepositoryTests()
        {
            // Create an in‑memory database.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                            .Options;
            _context = new AppDbContext(options);
            _responseRepositoryMock = new Mock<IResponseRepository>();
            // Instantiate our repository with the in‑memory context and the mocked response repository.
            _repository = new QuestionRepository(_context, _responseRepositoryMock.Object);
            // Create a test user.
            _testUser = new AppUser { Id = 1, UserName = "testuser" };
        }

        public void Dispose() 
        {
            _context.Dispose();
        }

        [Fact]
        public async Task SaveQuestion_SavesQuestionInDatabase()
        {
            // Arrange
            var question = new Question 
            { 
                Body = "Test body", 
                Type = "TestType", 
                Responses = new List<Response>(), 
                CreatedById = _testUser.Id 
            };
            // Act
            var saved = await _repository.saveQuestion(question, _testUser);
            // Assert: Retrieve the question from the database.
            var fetched = await _context.Set<Question>().FindAsync(saved.Id);
            Assert.NotNull(fetched);
            Assert.Equal("Test body", fetched.Body);
        }

        [Fact]
        public async Task GetQuestionById_ReturnsQuestion_WhenUserIsOwner()
        {
            // Arrange: Add a question with CreatedById equal to test user's id.
            var question = new Question 
            { 
                Id = 10, 
                Body = "Q", 
                Type = "T", 
                Responses = new List<Response>(), 
                CreatedById = _testUser.Id 
            };
            _context.Set<Question>().Add(question);
            await _context.SaveChangesAsync();
            // Act
            var result = await _repository.getQuestionById(10, _testUser);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Id);
        }

        [Fact]
        public async Task GetQuestionById_ThrowsUnauthorizedAccessException_WhenUserIsNotOwner()
        {
            // Arrange: Add a question with a different CreatedById.
            var question = new Question 
            { 
                Id = 11, 
                Body = "Q", 
                Type = "T", 
                Responses = new List<Response>(), 
                CreatedById = 999 
            };
            _context.Set<Question>().Add(question);
            await _context.SaveChangesAsync();
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _repository.getQuestionById(11, _testUser));
        }

        [Fact]
        public async Task GetQuestionByIdWithInterview_ReturnsQuestion_WithInterviewAndResponses()
        {
            // Arrange: Create an interview with two questions.
            var interview = new Interview 
            { 
                Id = 100, 
                secondsPerAnswer = 30, 
                Questions = new List<Question>() 
            };
            var q1 = new Question 
            { 
                Id = 20, 
                Body = "Body 20", 
                Type = "TypeA", 
                Responses = new List<Response>(), 
                CreatedById = _testUser.Id, 
                Interview = interview 
            };
            var q2 = new Question 
            { 
                Id = 21, 
                Body = "Body 21", 
                Type = "TypeB", 
                Responses = new List<Response>(), 
                CreatedById = _testUser.Id, 
                Interview = interview 
            };
            interview.Questions.AddRange(new[] { q1, q2 });
            _context.Set<Interview>().Add(interview);
            _context.Set<Question>().AddRange(q1, q2);
            await _context.SaveChangesAsync();
            // Act: Retrieve q2 with its interview.
            var result = await _repository.getQuestionByIdWithInterview(21, _testUser);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(21, result.Id);
            Assert.NotNull(result.Interview);
            Assert.Equal(2, result.Interview.Questions.Count);
        }

        [Fact]
        public async Task UpdateAnswer_AddsResponseAndSavesQuestion()
        {
            // Arrange: Create and save a question.
            var question = new Question 
            { 
                Id = 30, 
                Body = "Old Body", 
                Type = "TypeX", 
                Responses = new List<Response>(), 
                CreatedById = _testUser.Id 
            };
            _context.Set<Question>().Add(question);
            await _context.SaveChangesAsync();
            // Setup the responseRepository mock to simulate updateAnswer.
            var dummyResponse = new Response { Id = 200 };
            _responseRepositoryMock.Setup(r => r.updateAnswer("New Answer", "New Feedback", "video.mp4", "http://server", 30, _testUser))
                                  .ReturnsAsync(dummyResponse);
            // Act
            var updated = await _repository.updateAnswer(question, "New Answer", "New Feedback", "video.mp4", "http://server", _testUser);
            // Assert: Verify that the dummy response was added.
            Assert.Contains(dummyResponse, updated.Responses);
            // Also, ensure the question was saved (i.e. exists in the database).
            var fetched = await _context.Set<Question>().FindAsync(updated.Id);
            Assert.NotNull(fetched);
            Assert.Contains(dummyResponse, fetched.Responses);
        }

        [Fact]
        public async Task DeleteQuestion_CallsSaveMethod()
        {
            // Arrange: Create and save a question.
            var question = new Question 
            { 
                Id = 40, 
                Body = "Delete me", 
                Type = "T", 
                Responses = new List<Response>(), 
                CreatedById = _testUser.Id 
            };
            _context.Set<Question>().Add(question);
            await _context.SaveChangesAsync();
            // Act: Call deleteQuestion (per implementation, it calls Save).
            await _repository.deleteQuestion(question, _testUser);
            // Assert: The question should still exist in the database.
            var exists = await _context.Set<Question>().AnyAsync(q => q.Id == question.Id);
            Assert.True(exists);
        }

        [Fact]
        public void ConvertQuestionToDTO_MapsPropertiesCorrectly()
        {
            // Arrange: Create a dummy question.
            var question = new Question 
            { 
                Id = 50, 
                Body = "Test Body", 
                Type = "TestType", 
                Responses = new List<Response>() 
            };
            // Act
            var dto = _repository.convertQuestionToDTO(question);
            // Assert
            Assert.Equal(50, dto.id);
            Assert.Equal("Test Body", dto.body);
            Assert.Equal("TestType", dto.type);
            Assert.Empty(dto.responses);
        }

        [Fact]
        public async Task VerifyVideoView_ReturnsValueFromResponseRepository()
        {
            // Arrange: Setup the mock to return true.
            _responseRepositoryMock.Setup(r => r.verifyVideoView("video.mp4", _testUser))
                                  .ReturnsAsync(true);
            // Act
            var result = await _repository.verifyVideoView("video.mp4", _testUser);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ConvertQuestionToEntity_CreatesQuestionFromDTO()
        {
            // Arrange: Create a dummy QuestionDTO with one ResponseDto.
            var responseDto = new ResponseDto 
            { 
                id = 300, 
                answer = "Ans", 
                feedback = "FB", 
                videoLink = "L", 
                questionId = 60 
            };
            var questionDTO = new QuestionDTO 
            { 
                id = 70, 
                body = "DTO Body", 
                type = "DTOType", 
                responses = new List<ResponseDto> { responseDto } 
            };
            // Setup the mock to convert the responseDto to a Response.
            var dummyResponse = new Response { Id = 300 };
            _responseRepositoryMock.Setup(r => r.dtoToResponse(responseDto))
                                  .Returns(dummyResponse);
            // Act
            var question = _repository.convertQuestionToEntity(questionDTO);
            // Assert
            Assert.Equal(70, question.Id);
            Assert.Equal("DTO Body", question.Body);
            Assert.Single(question.Responses);
            Assert.Equal(300, question.Responses.First().Id);
        }

        [Fact]
        public void CreateQuestionFromString_ReturnsNewQuestionWithBodyAndType()
        {
            // Act
            var question = QuestionRepository.createQuestionFromString("Sample Body", "SampleType");
            // Assert
            Assert.Equal("Sample Body", question.Body);
            Assert.Equal("SampleType", question.Type);
            Assert.NotNull(question.Responses);
            Assert.Empty(question.Responses);
        }
    }

