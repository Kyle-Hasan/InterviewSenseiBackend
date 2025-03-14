using API.Questions;
using API.Users;

namespace API.Responses;

public interface IResponseRepository
{
    Task<Response> SaveResponse(Response response,AppUser user);
    Task DeleteResponse(Response response, AppUser user);

    Task<Response> UpdateAnswer(string answer, string positiveFeedback,string negativeFeedback, string sampleResponse, string videoName, string serverUrl, int questionId, AppUser user);

    Task<Response> getResponseById(int id);
    
    Task<bool> VerifyVideoView(string filename,AppUser user);
    
    ResponseDto ConvertToDto(Response response);
    Response DtoToResponse(ResponseDto response);
    
    Task<List<Response>> GetResponsesQuestion(int questionId,AppUser user);

}