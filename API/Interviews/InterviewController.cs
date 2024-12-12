using API.Base;
using API.Extensions;
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
    [RequestSizeLimit(100_000_000)]
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

       var retVal = await interviewService.rateAnswer(int.Parse(ratingRequest.questionId), filePath,videoName,serverUrl,user);
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
            generateQuestionsRequest.numberOfBehavioral, generateQuestionsRequest.numberOfTechnical, filePath,generateQuestionsRequest.additionalDescription);
    }
    
    [HttpPost("generateInterview")]
    public async Task<ActionResult<InterviewDTO>> generateInterview(
        [FromForm] GenerateInterviewRequest generateQuestionsRequest)
    {
        if (generateQuestionsRequest.numberOfBehavioral + generateQuestionsRequest.numberOfTechnical == 0)
        {
            return BadRequest();
        }

        string filePath = "";
        string fileName = "";
        if (generateQuestionsRequest.resume != null)
        {
            fileName = Guid.NewGuid().ToString() + "_" + generateQuestionsRequest.resume.FileName;
            filePath = Path.Combine("Uploads",
                fileName);
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                await generateQuestionsRequest.resume.CopyToAsync(stream);
            }
        }

        var user = await base.GetCurrentUser();
        
        string serverUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/Interview/getPdf";


        var i = await interviewService.GenerateInterview(user,generateQuestionsRequest.name, generateQuestionsRequest.jobDescription,
            generateQuestionsRequest.numberOfBehavioral, generateQuestionsRequest.numberOfTechnical,  generateQuestionsRequest.secondsPerAnswer, filePath, generateQuestionsRequest.additionalDescription,fileName,serverUrl);
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
    public async Task<List<InterviewDTO>> getInterviewList([FromQuery] InterviewSearchParams interviewSearchParamsParams)
    {
        var user = await base.GetCurrentUser();
        PagedInterviewResponse pagedInterviewResponse = await interviewService.getInterviews(user,interviewSearchParamsParams);
        Response.AddPaginationHeader(pagedInterviewResponse.total,interviewSearchParamsParams.startIndex,interviewSearchParamsParams.pageSize);
        return pagedInterviewResponse.interviews.Select(x=> InterviewService.interviewToDTO(x)).ToList();
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


    [HttpGet("getPdf/{fileName}")]
    public async Task<IActionResult> getInterview(string fileName)
    {
        var user = await base.GetCurrentUser();
        
        // security check
        
        var videoPath = Path.Combine("Uploads", fileName);
        
        var stream = new FileStream(videoPath,FileMode.Open,FileAccess.Read);
        var mimeType = "application/pdf";
        return File(stream, mimeType, enableRangeProcessing: true); 
        
    }
    
    
    
    
}