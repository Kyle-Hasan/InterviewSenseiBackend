using API.Users;

namespace API.Responses;

public interface IResponseService
{
    Task<ResponseDto> rateAnswer(int questionId, string videoPath, string videoName, string serverUrl, AppUser user);
}