using API.Base;
using API.Interviews;
using API.Responses;
using API.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Questions;
[Authorize]
public class ResponseController(IResponseRepository responseRepository, IResponseService responseService, UserManager<AppUser> userManager):BaseController(userManager)
{
    
    [HttpGet("byQuestion")]
    public async Task<List<ResponseDto>> getResponses([FromQuery] int questionId)
    {
        var user = await base.GetCurrentUser();
        var responses = await responseRepository.getResponsesQuestion(questionId,user);
        return responses.Select(responseRepository.convertToDto).ToList();
    }
    
    [HttpPost("rateAnswer")]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<ResponseDto>> getRating([FromForm]RatingRequestDTO ratingRequest)
    {
        var user = await base.GetCurrentUser();
        if (ratingRequest.video == null || ratingRequest.video.Length == 0)
        {
            return BadRequest("no video provided");
        }
        // give video new random name to be saved into system
        string videoName = Guid.NewGuid().ToString() + "_" + ratingRequest.video.FileName;
        var filePath=  Path.Combine("Uploads", videoName);

        using (var stream = new FileStream(filePath, FileMode.Create,FileAccess.ReadWrite))
        {
            await ratingRequest.video.CopyToAsync(stream);
        }
        
        string serverUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/Interview/getVideo";

        var retVal = await responseService.rateAnswer(int.Parse(ratingRequest.questionId), filePath,videoName,serverUrl,user);
        return retVal;


    }
}