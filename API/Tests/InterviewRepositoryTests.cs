using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;
using API.Interviews;         // Contains interviewRepository, Interview, InterviewSearchParams, PagedInterviewResponse, ResumeUrlAndName
using API.Data;               // Contains AppDbContext
using API.Users;              // Contains AppUser
using API.AWS;                // For IBlobStorageService if needed

public class InterviewHub : Hub { }

namespace API.Interviews.Tests
{
    // Our test class uses an in‑memory database for the AppDbContext.
    public class InterviewRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly InterviewRepository _repository;
        private readonly Mock<IHubContext<InterviewHub>> _hubContextMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly AppUser _testUser;

        public InterviewRepositoryTests()
        {
            // Set up an in‑memory database.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // Create a test user.
            _testUser = new AppUser { Id = 1, UserName = "testuser" };

            // Set up the hub context mock.
            _hubContextMock = new Mock<IHubContext<InterviewHub>>();
            _clientProxyMock = new Mock<IClientProxy>();
            var hubClientsMock = new Mock<IHubClients>();
            // When Groups("interviews") is requested, return our client proxy mock.
            hubClientsMock.Setup(clients => clients.Groups("interviews"))
                          .Returns(_clientProxyMock.Object);
            _hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

            // Create the repository.
            _repository = new InterviewRepository(_context, _hubContextMock.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task Delete_RemovesInterview_AndSendsCacheInvalidation()
        {
            // Arrange: add an interview for our test user.
            var interview = new Interview 
            { 
                Id = 1, 
                CreatedById = _testUser.Id, 
                CreatedDate = DateTime.UtcNow, 
                ResumeLink = "dummy" 
            };
            _context.Set<Interview>().Add(interview);
            await _context.SaveChangesAsync();

            // Act: call delete.
            await _repository.Delete(interview, _testUser);

            // Assert: verify the interview is removed.
            var exists = await _context.Set<Interview>().AnyAsync(i => i.Id == interview.Id);
            Assert.False(exists);

            // Verify that SendAsync was called on the hub group.
            _clientProxyMock.Verify(
                cp => cp.SendAsync("entitiesUpdated", "interviews", "true", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Save_AddsInterview_AndSendsCacheInvalidation()
        {
            // Arrange: create a new interview.
            var interview = new Interview 
            { 
                CreatedById = _testUser.Id, 
                CreatedDate = DateTime.UtcNow, 
                ResumeLink = "dummy" 
            };

            // Act: call save.
            var savedInterview = await _repository.Save(interview, _testUser);

            // Assert: the interview should now exist.
            var exists = await _context.Set<Interview>().AnyAsync(i => i.Id == savedInterview.Id);
            Assert.True(exists);

            // Verify hub message was sent.
            _clientProxyMock.Verify(
                cp => cp.SendAsync("entitiesUpdated", "interviews", "true", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetInterviews_ReturnsFilteredAndPaginatedResults()
        {
            // Arrange: seed several interviews for _testUser.
            var now = DateTime.UtcNow;
            var interviews = new List<Interview>
            {
                new Interview { Id = 1, CreatedById = _testUser.Id, Name = "Alpha", CreatedDate = now.AddMinutes(-10) },
                new Interview { Id = 2, CreatedById = _testUser.Id, Name = "Beta", CreatedDate = now.AddMinutes(-5) },
                new Interview { Id = 3, CreatedById = _testUser.Id, Name = "Gamma", CreatedDate = now }
            };
            _context.Set<Interview>().AddRange(interviews);
            await _context.SaveChangesAsync();

            // Set search parameters – for example, filter by name containing "a" (assuming case sensitivity).
            var searchParams = new InterviewSearchParams 
            { 
                name = "a", 
                dateSort = "DESC", 
                startIndex = 0, 
                pageSize = 2 
            };

            // Act.
            var pagedResponse = await _repository.GetInterviews(_testUser, searchParams);

            // Assert.
            // "Alpha" and "Gamma" contain "a" (if case sensitive, adjust accordingly).
            Assert.Equal(2, pagedResponse.total);
            Assert.Equal(2, pagedResponse.interviews.Count);
            // With DESC sorting by CreatedDate, the most recent ("Gamma") should come first.
            Assert.Equal("Gamma", pagedResponse.interviews[0].Name);
            Assert.Equal("Alpha", pagedResponse.interviews[1].Name);
        }

        [Fact]
        public async Task GetInterview_ReturnsInterview_WhenUserIsOwner()
        {
            // Arrange: add an interview with CreatedById equal to test user's id.
            var interview = new Interview 
            { 
                Id = 10, 
                CreatedById = _testUser.Id, 
                CreatedDate = DateTime.UtcNow 
            };
            _context.Set<Interview>().Add(interview);
            await _context.SaveChangesAsync();

            // Act.
            var result = await _repository.GetInterview(_testUser, 10);

            // Assert.
            Assert.NotNull(result);
            Assert.Equal(10, result.Id);
        }

        [Fact]
        public async Task GetInterview_ThrowsUnauthorizedAccessException_WhenUserIsNotOwner()
        {
            // Arrange: add an interview with a different CreatedById.
            var interview = new Interview 
            { 
                Id = 11, 
                CreatedById = 999, 
                CreatedDate = DateTime.UtcNow 
            };
            _context.Set<Interview>().Add(interview);
            await _context.SaveChangesAsync();

            // Act & Assert.
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _repository.GetInterview(_testUser, 11));
        }

        [Fact]
        public void GetChangedInterview_ReturnsUpdatedInterview()
        {
            // Arrange: create two interview objects with different values.
            var oldInterview = new Interview { Id = 20, Name = "Old Name", ResumeLink = "old.pdf" };
            var newInterview = new Interview { Id = 20, Name = "New Name", ResumeLink = "new.pdf" };

            // Act.
            var result = _repository.GetChangedInterview(newInterview, oldInterview);

            // Assert: assuming that updateObjectFields copies new properties.
            Assert.Equal("New Name", result.Name);
            Assert.Equal("new.pdf", result.ResumeLink);
        }

        [Fact]
        public async Task VerifyPdfView_ReturnsTrue_WhenInterviewWithFileExists()
        {
            // Arrange: add an interview whose ResumeLink contains "resume.pdf".
            var interview = new Interview 
            { 
                Id = 30, 
                CreatedById = _testUser.Id, 
                ResumeLink = "http://example.com/Interview/getPdf/resume.pdf" 
            };
            _context.Set<Interview>().Add(interview);
            await _context.SaveChangesAsync();

            // Act.
            var result = await _repository.VerifyPdfView(_testUser, "resume.pdf");

            // Assert.
            Assert.True(result);
        }

        [Fact]
        public async Task GetLatestResume_ReturnsLatestResumeLink()
        {
            // Arrange: add two interviews with different CreatedDates and ResumeLink values.
            var interview1 = new Interview 
            { 
                Id = 40, 
                CreatedById = _testUser.Id, 
                ResumeLink = "link1", 
                CreatedDate = DateTime.UtcNow.AddHours(-1) 
            };
            var interview2 = new Interview 
            { 
                Id = 41, 
                CreatedById = _testUser.Id, 
                ResumeLink = "link2", 
                CreatedDate = DateTime.UtcNow 
            };
            _context.Set<Interview>().AddRange(interview1, interview2);
            await _context.SaveChangesAsync();

            // Act.
            var latest = await _repository.GetLatestResume(_testUser);

            // Assert.
            Assert.Equal("link2", latest);
        }

        [Fact]
        public async Task GetAllResumes_ReturnsUniqueResumes_SortedDescendingByDate()
        {
            // Arrange: add interviews with duplicate ResumeLink values.
            var interview1 = new Interview 
            { 
                Id = 50, 
                CreatedById = _testUser.Id, 
                ResumeLink = "link1", 
                CreatedDate = DateTime.UtcNow.AddHours(-3) 
            };
            var interview2 = new Interview 
            { 
                Id = 51, 
                CreatedById = _testUser.Id, 
                ResumeLink = "link2", 
                CreatedDate = DateTime.UtcNow.AddHours(-2) 
            };
            var interview3 = new Interview 
            { 
                Id = 52, 
                CreatedById = _testUser.Id, 
                ResumeLink = "link1", 
                CreatedDate = DateTime.UtcNow.AddHours(-1) 
            };
            _context.Set<Interview>().AddRange(interview1, interview2, interview3);
            await _context.SaveChangesAsync();

            // Act.
            var resumes = await _repository.GetAllResumes(_testUser);

            // Assert:
            // There should be two unique resumes.
            Assert.Equal(2, resumes.Length);
            // They should be sorted descending by date (the one with the most recent CreatedDate first).
            Assert.Equal("link1", resumes[0].url); // interview3's resume
            Assert.Equal("link2", resumes[1].url);
        }
    }
}
