using System.Runtime.Intrinsics.X86;
using API.Base;
using API.Data;
using API.Questions;
using API.Users;
using Microsoft.EntityFrameworkCore;

namespace API.Responses;

public class ResponseRepository: BaseRepository<Response>,IResponseRepository
{
    public ResponseRepository(AppDbContext appDbContext) : base(appDbContext)
    {
    }

    public async Task<Response> SaveResponse(Response response, AppUser user)
    {
        return await base.Save(response, user);
    }

    public async Task DeleteResponse(Response response, AppUser user)
    {
        await base.Delete(response, user);
    }

    public async Task<Response> UpdateAnswer(string answer, string positiveFeedback,string negativeFeedback, string sampleResponse, string videoName, string serverUrl, int questionId, AppUser user)
    {
        Response response = new Response();
        response.Answer = answer;
        response.PositiveFeedback = positiveFeedback;
        response.NegativeFeedback = negativeFeedback;
        response.ExampleResponse = sampleResponse;
        response.VideoLink = serverUrl + "/" + videoName;
        response.QuestionId = questionId;
       return await this.SaveResponse(response, user);
    }

    public async Task<Response> getResponseById(int id)
    {
        return await base.getById(id);
    }

    public async Task<bool> verifyVideoView(string filename, AppUser user)
    {
        return await base._entities.AnyAsync(q => q.VideoLink.Contains(filename) && q.CreatedById == user.Id);
    }

    public ResponseDto convertToDto(Response response)
    {
        return new ResponseDto
        {
            id = response.Id,
            answer = response.Answer,
            negativeFeedback = response.NegativeFeedback, 
            positiveFeedback = response.PositiveFeedback,
            exampleResponse = response.ExampleResponse,
            videoLink = response.VideoLink,
            questionId = response.QuestionId,

        };
    }

    public Response dtoToResponse(ResponseDto response)
    {
        return new Response
        {
            Id = response.id,
            Answer = response.answer,
            NegativeFeedback = response.negativeFeedback,
            PositiveFeedback = response.positiveFeedback,
            ExampleResponse = response.exampleResponse,
            VideoLink = response.videoLink,
            QuestionId = response.questionId,
        };
    }

    public async Task<List<Response>> getResponsesQuestion(int questionId, AppUser user)
    {
        return base._entities.Where(r => r.QuestionId == questionId && r.CreatedById == user.Id ).ToList();
    }
}