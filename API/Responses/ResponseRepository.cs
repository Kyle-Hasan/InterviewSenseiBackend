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

    public async Task<Response> saveResponse(Response response, AppUser user)
    {
        return await base.Save(response, user);
    }

    public async Task deleteResponse(Response response, AppUser user)
    {
        await base.Delete(response, user);
    }

    public async Task<Response> updateAnswer(string answer, string feedback, string videoName, string serverUrl, int questionId, AppUser user)
    {
        Response reponse = new Response();
        reponse.Answer = answer;
        reponse.Feedback = feedback;
        reponse.VideoLink = serverUrl + "/" + videoName;
        reponse.QuestionId = questionId;
       return await this.saveResponse(reponse, user);
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
            feedback = response.Feedback,
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
            Feedback = response.feedback,
            VideoLink = response.videoLink,
            QuestionId = response.questionId,
        };
    }

    public async Task<List<Response>> getResponsesQuestion(int questionId, AppUser user)
    {
        return base._entities.Where(r => r.QuestionId == questionId && r.CreatedById == user.Id ).ToList();
    }
}