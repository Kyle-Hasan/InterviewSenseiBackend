using API.Base;
using API.Questions;
using API.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Interviews;
[Authorize]
public class InterviewController(IinterviewService interviewService,UserManager<AppUser> userManager):BaseController(userManager)
{
    [HttpPost("rateAnswer")]
    public async Task<ActionResult<QuestionDTO>> getRating([FromForm]RatingRequestDTO ratingRequest)
    {
        var user = await base.GetCurrentUser();
        if (ratingRequest.video == null || ratingRequest.video.Length == 0)
        {
            return BadRequest("no video provided");
        }

        string videoName = Guid.NewGuid().ToString() + "_" + ratingRequest.video.FileName;
        var filePath=  Path.Combine("Uploads", videoName);

        using (var stream = new FileStream(filePath, FileMode.Create,FileAccess.ReadWrite))
        {
            await ratingRequest.video.CopyToAsync(stream);
        }
        
        string serverUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/Interview/getVideo";

       var retVal = await interviewService.rateAnswer(ratingRequest.question,int.Parse(ratingRequest.id), filePath,videoName,serverUrl,user);
       return retVal;


    }

    [HttpPost("generateQuestions")]
    public async Task<ActionResult<GenerateQuestionsResponse>> getQuestions(
        [FromForm] GenerateInterviewRequest generateQuestionsRequest)
    {
        var filePath= Path.Combine("Uploads", Guid.NewGuid().ToString() + "-" + generateQuestionsRequest.resume.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
        {
            await generateQuestionsRequest.resume.CopyToAsync(stream);
        }

        return await interviewService.generateQuestions(generateQuestionsRequest.jobDescription,
            generateQuestionsRequest.numberOfBehavioral, generateQuestionsRequest.numberOfTechnical, filePath);
    }
    
    [HttpPost("generateInterview")]
    public async Task<ActionResult<InterviewDTO>> generateInterview(
        [FromForm] GenerateInterviewRequest generateQuestionsRequest)
    {
        var filePath= Path.Combine("Uploads", "resume.pdf");
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
        {
            await generateQuestionsRequest.resume.CopyToAsync(stream);
        }

        var user = await base.GetCurrentUser();

        var i = await interviewService.GenerateInterview(user,generateQuestionsRequest.name, generateQuestionsRequest.jobDescription,
            generateQuestionsRequest.numberOfBehavioral, generateQuestionsRequest.numberOfTechnical, filePath);
        return InterviewService.interviewToDTO(i);
    }


    [HttpDelete("{id}")]
    public async Task delete(int id)
    {
        var user = await base.GetCurrentUser();
        var interview = await interviewService.getInterview(id,user);
        
        await interviewService.deleteInterview(interview, user);
    }

    [HttpPut("")]
    public async Task<InterviewDTO> update([FromBody]InterviewDTO interviewDTO)
    {
        var interview = InterviewService.DtoToInterview(interviewDTO);
        var user = await base.GetCurrentUser();
        var updated = await interviewService.updateInterview(interview,user);
        return InterviewService.interviewToDTO(updated);
    }

    [HttpGet("{id}")]
    public async Task<InterviewDTO> getInterview(int id)
    {
        var user = await base.GetCurrentUser();
        return await interviewService.getInterviewDto(id,user);
    }

    [HttpGet("interviewList")]
    public async Task<List<InterviewDTO>> getInterviewList()
    {
        var user = await base.GetCurrentUser();
        return await interviewService.getInterviews(user);
    }

    [HttpGet("getVideo/{fileName}")]
    public async Task<IActionResult> GetFile(string fileName)
    {
        var user = await base.GetCurrentUser();
        var videoPath = Path.Combine("Uploads", fileName);
        bool canView =  await interviewService.verifyVideoView(fileName,user);
        if (!canView)
        {
            return Unauthorized();
        }
        
        
        var stream = new FileStream(videoPath,FileMode.Open,FileAccess.Read);
        var mimeType = "video/webm";
        return File(stream, mimeType, enableRangeProcessing: true);
    }
    
    
    
    
}