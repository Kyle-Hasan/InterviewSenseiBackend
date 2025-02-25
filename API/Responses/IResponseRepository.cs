using API.Questions;
using API.Users;

namespace API.Responses;

public interface IResponseRepository
{
    Task<Response> SaveResponse(Response response,AppUser user);
    Task DeleteResponse(Response response, AppUser user);

    Task<Response> UpdateAnswer(string answer, string positiveFeedback,string negativeFeedback, string sampleResponse, string videoName, string serverUrl, int questionId, AppUser user);

    Task<Response> getResponseById(int id);
    
    Task<bool> verifyVideoView(string filename,AppUser user);
    
    ResponseDto convertToDto(Response response);
    Response dtoToResponse(ResponseDto response);
    
    Task<List<Response>> getResponsesQuestion(int questionId,AppUser user);

}