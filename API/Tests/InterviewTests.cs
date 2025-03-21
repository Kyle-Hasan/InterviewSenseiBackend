using API.Data;
using API.Interviews;
using API.Questions;
using API.Responses;
using API.Users;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace API.Tests;

public class InterviewTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task TestGetInterview()
    {

        var mockInterviewHub = new Mock<IHubContext<InterviewHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockGroupClient = new Mock<IClientProxy>();
      
        mockInterviewHub.Setup(hub => hub.Clients).Returns(mockClients.Object);

         var context = CreateInMemoryDbContext();



        IinterviewRepository interviewRepository = new InterviewRepository(context, null);
        IResponseRepository responseRepository = new ResponseRepository(context);
        IQuestionRepository questionRepository = new QuestionRepository(context, responseRepository);

        AppUser appUser = new AppUser
        {
            Id = 1,
            Email = "test@test.com",
            UserName = "test",

        };
        
        Interview interview = new Interview
        {
            Name = "Test Interview",
            CreatedById = 1,
            JobDescription = "Test description",
            ResumeLink = "test.pdf",
            AdditionalDescription = "Test additional description",

        };

        await interviewRepository.Save(interview, appUser);
        
        Response testResponse1 = new Response
        {
            
            Answer = "This is a test",
            NegativeFeedback = "NFB", 
            PositiveFeedback = "PFB",
            ExampleResponse = "Response Example",
            VideoLink = "This is a video",
        };

        Response testResponse2 = new Response
        {
            
            Answer = "This is a test2",
            NegativeFeedback = "NFB", 
            PositiveFeedback = "PFB",
            ExampleResponse = "Response Example",
            VideoLink = "This is a video2",
        };

        Response testResponse3 = new Response
        {
            
            Answer = "This is a test3",
            NegativeFeedback = "NFB", 
            PositiveFeedback = "PFB",
            ExampleResponse = "Response Example",
            VideoLink = "This is a video3",
        };

        await responseRepository.SaveResponse(testResponse1, appUser);
        await responseRepository.SaveResponse(testResponse2, appUser);
        await responseRepository.SaveResponse(testResponse3, appUser);
        
        Question question1 = new Question
        {
            
            Body = "Test question1",
            InterviewId = 1,
            Responses = new List<Response> {testResponse1,testResponse2},
            Type = QuestionType.Behavioral
        };
        
        Question question2 = new Question
        {
           
            Body = "Test question2",
            InterviewId = 1,
            Responses = new List<Response> {testResponse3},
            Type = QuestionType.Technical
        };
        
        await questionRepository.saveQuestion(question1, appUser);
        await questionRepository.saveQuestion(question2, appUser);

        var result =  await interviewRepository.GetInterview(appUser, 1);
        
        Assert.NotNull(result);
        Assert.Equal(interview.Id, result.Id);
        Assert.Equal(interview.CreatedById, result.CreatedById);
        Assert.Equal(interview.JobDescription, result.JobDescription);
        // could change depending on if we use cloud or not, for testing turn it off in the .env
        Assert.Equal(interview.ResumeLink, result.ResumeLink);
        Assert.Equal(interview.AdditionalDescription, result.AdditionalDescription);
        
    }
}