using API.AWS;
using API.Base;
using API.Extensions;
using API.Questions;
using API.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Interviews;
[Authorize]
public class InterviewController(IinterviewService interviewService,UserManager<AppUser> userManager, IBlobStorageService blobStorageService):BaseController(userManager)
{
    

    [HttpPost("generateQuestions")]
    public async Task<ActionResult<GenerateQuestionsResponse>> getQuestions(
        [FromForm] GenerateInterviewRequest generateQuestionsRequest)
    {
        var fileName = Guid.NewGuid().ToString() + "-" + generateQuestionsRequest.resume.FileName;
        var filePath= Path.Combine("Uploads", fileName);
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
        {
            await generateQuestionsRequest.resume.CopyToAsync(stream);
        }

        return await interviewService.generateQuestions(generateQuestionsRequest.jobDescription,
            generateQuestionsRequest.numberOfBehavioral, generateQuestionsRequest.numberOfTechnical, filePath,generateQuestionsRequest.additionalDescription,fileName);
    }
    
    [HttpPost("generateInterview")]
    public async Task<ActionResult<InterviewDTO>> generateInterview(
        [FromForm] GenerateInterviewRequest generateQuestionsRequest)
    {
        if (generateQuestionsRequest.numberOfBehavioral + generateQuestionsRequest.numberOfTechnical == 0)
        {
            return BadRequest();
        }
        else if (generateQuestionsRequest.secondsPerAnswer < 10 || generateQuestionsRequest.numberOfBehavioral > 300)
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
        return interviewService.interviewToDTO(i);
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
        var interview = interviewService.DtoToInterview(interviewDTO);
        var user = await base.GetCurrentUser();
        var updated = await interviewService.updateInterview(interview,user);
        return interviewService.interviewToDTO(updated);
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
        return pagedInterviewResponse.interviews.Select(x=> interviewService.interviewToDTO(x)).ToList();
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

        var stream = await interviewService.ServeFile(fileName, videoPath, "videos", HttpContext);
        var mimeType = "video/webm";
        var response = File(stream, mimeType, enableRangeProcessing: true);
        

        return response;
    }


    [HttpGet("getPdf/{fileName}")]
    public async Task<IActionResult> getInterview(string fileName)
    {
        var user = await base.GetCurrentUser();
        // security check
        bool canView =  await interviewService.verifyPdfView(fileName,user);
        if (!canView)
        {
            return Unauthorized();
        }
        
        var pdfPath = Path.Combine("Uploads", fileName);
        
        var stream = await interviewService.ServeFile(fileName, pdfPath, "resumes", HttpContext);

        var mimeType = "application/pdf";
        var response = File(stream, mimeType, enableRangeProcessing: true);
        

        return response;
        
    }
    
    
    
    
}