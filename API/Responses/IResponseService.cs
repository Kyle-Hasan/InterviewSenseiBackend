using API.Users;

namespace API.Responses;

public interface IResponseService
{
    /*
     * returns: a dto with the newly created response
     * questionId: id of question being answered
     * videoPath: path to video file with response
     * videoName: name of video with the response
     * serverUrl: url prefix for this server
     * user: user making request
     */
    Task<ResponseDto> RateAnswer(int questionId, string videoPath, string videoName, string serverUrl, AppUser user);
}