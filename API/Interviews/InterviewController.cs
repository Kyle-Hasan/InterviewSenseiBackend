using API.Base;
using Microsoft.AspNetCore.Mvc;

namespace API.Interviews;

public class InterviewController(IinterviewService interviewService):BaseController
{
    [HttpPost("rateAnswer")]
    public async Task<ActionResult<RatingResponse>> getRating([FromForm]RatingRequestDTO ratingRequest)
    {
        if (ratingRequest.video == null || ratingRequest.video.Length == 0)
        {
            return BadRequest("no video provided");
        }
        
        var filePath=  Path.Combine("Uploads", "video.webm");

        using (var stream = new FileStream(filePath, FileMode.Create,FileAccess.ReadWrite))
        {
            await ratingRequest.video.CopyToAsync(stream);
        }

       var retVal = await interviewService.rateAnswer(ratingRequest.question, filePath);
       return retVal;


    }
}