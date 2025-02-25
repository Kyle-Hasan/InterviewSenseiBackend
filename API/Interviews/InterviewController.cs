using API.AWS;
using API.Base;
using API.Extensions;
using API.Questions;
using API.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO;
namespace API.Interviews;

[Authorize]
public class InterviewController(
    IinterviewService interviewService,
    UserManager<AppUser> userManager,
    IBlobStorageService blobStorageService) : BaseController(userManager)
{
    [HttpPost("generateQuestions")]
    public async Task<ActionResult<GenerateQuestionsResponse>> GetQuestions(
        [FromForm] GenerateInterviewRequest generateQuestionsRequest)
    {
        var fileName = Guid.NewGuid().ToString() + "-" + generateQuestionsRequest.resume.FileName;
        var filePath = Path.Combine("Uploads", fileName);
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
        {
            await generateQuestionsRequest.resume.CopyToAsync(stream);
        }

        return await interviewService.GenerateQuestions(generateQuestionsRequest.jobDescription,
            generateQuestionsRequest.numberOfBehavioral, generateQuestionsRequest.numberOfTechnical, filePath,
            generateQuestionsRequest.additionalDescription, fileName);
    }

    [HttpPost("generateInterview")]
    public async Task<ActionResult<InterviewDTO>> GenerateInterview(
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
        string serverUrl = $"{Request.Scheme}s://{Request.Host}{Request.PathBase}/api/Interview/getPdf";
        // existing resume url, should be on our server so to get its name just strip the server part
        if (generateQuestionsRequest.resumeUrl != null)
        {
            if (!AppConfig.UseCloudStorage)
            {
                fileName = generateQuestionsRequest.resumeUrl.GetStringAfterPattern("/api/Interview/getPdf/");
                
            }
            else
            {
                fileName = generateQuestionsRequest.resumeUrl.GetStringAfterPattern("/resumes/");
                fileName = fileName.GetStringBeforePattern("?");
            }
            filePath = Path.Combine("Uploads", fileName);
            
            // download onto local file system if cloud storage is being used
            if (AppConfig.UseCloudStorage)
            {
                await blobStorageService.DownloadFileAsync(fileName, filePath, "resumes");
            }
        }
        else if (generateQuestionsRequest.resume != null && generateQuestionsRequest.resume.ContentType == "application/pdf")
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

        var i = await interviewService.GenerateInterview(user, generateQuestionsRequest.name,
            generateQuestionsRequest.jobDescription,
            generateQuestionsRequest.numberOfBehavioral, generateQuestionsRequest.numberOfTechnical,
            generateQuestionsRequest.secondsPerAnswer, filePath, generateQuestionsRequest.additionalDescription,
            fileName, serverUrl);
       
        return interviewService.InterviewToDTO(i);
    }


    [HttpDelete("{id}")]
    public async Task Delete(int id)
    {
        var user = await base.GetCurrentUser();
        var interview = await interviewService.GetInterview(id, user);

        await interviewService.DeleteInterview(interview, user);
    }

    [HttpPut("")]
    public async Task<InterviewDTO> Update([FromBody] InterviewDTO interviewDTO)
    {
        var interview = interviewService.DtoToInterview(interviewDTO);
        var user = await base.GetCurrentUser();
        var updated = await interviewService.UpdateInterview(interview, user);
        return interviewService.InterviewToDTO(updated);
    }

    [HttpGet("{id}")]
    public async Task<InterviewDTO> GetInterview(int id)
    {
        var user = await base.GetCurrentUser();
        return await interviewService.GetInterviewDto(id, user);
    }

    [HttpGet("interviewList")]
    public async Task<List<InterviewDTO>> GetInterviewList(
        [FromQuery] InterviewSearchParams interviewSearchParamsParams)
    {
        var user = await base.GetCurrentUser();
        PagedInterviewResponse pagedInterviewResponse =
            await interviewService.GetInterviews(user, interviewSearchParamsParams);
        Response.AddPaginationHeader(pagedInterviewResponse.total, interviewSearchParamsParams.startIndex,
            interviewSearchParamsParams.pageSize);
        return pagedInterviewResponse.interviews.Select(x => interviewService.InterviewToDTO(x)).ToList();
    }

    // get video file or signed url to video file in blob storage
    [HttpGet("getVideo/{fileName}")]
    public async Task<IActionResult> GetVideo(string fileName)
    {
        var user = await base.GetCurrentUser();
        var videoPath = Path.Combine("Uploads", fileName);
        bool canView = await interviewService.VerifyVideoView(fileName, user);
        if (!canView)
        {
            return Unauthorized();
        }

        if (!AppConfig.UseSignedUrl)
        {
            var stream = await interviewService.ServeFile(fileName, videoPath, "videos", HttpContext);
            var mimeType = "video/webm";
            var response = File(stream, mimeType, enableRangeProcessing: true);
            return response;
        }
        else
        {
            return Ok(blobStorageService.GeneratePreSignedUrlAsync("videos", fileName, 10));
        }
    }

    // get pdf file or signed url to pdf file in blob storage
    [HttpGet("getPdf/{fileName}")]
    public async Task<IActionResult> GetResume(string fileName)
    {
        var user = await base.GetCurrentUser();
        // security check
        bool canView = await interviewService.VerifyPdfView(fileName, user);
        if (!canView)
        {
            return Unauthorized();
        }


        if (!AppConfig.UseSignedUrl)
        {
            var pdfPath = Path.Combine("Uploads", fileName);

            var stream = await interviewService.ServeFile(fileName, pdfPath, "resumes", HttpContext);

            var mimeType = "application/pdf";
            var response = File(stream, mimeType, enableRangeProcessing: true);


            return response;
        }
        else
        {
            return Ok(blobStorageService.GeneratePreSignedUrlAsync("resumes", fileName, 10));
        }
    }

    [HttpGet("getLatestResume")]
    public async Task<ResumeUrlAndName> GetLatestResume()
    {
        var user = await base.GetCurrentUser();
        return await interviewService.GetLatestResume(user);
    }

    [HttpGet("getAllResumes")]
    public async Task<ResumeUrlAndName[]> GetAllResumes()
    {
        var user = await base.GetCurrentUser();
        var resumes = await interviewService.GetAllResumes(user);
        return resumes;
    }
}