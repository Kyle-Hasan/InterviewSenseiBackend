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
    public async Task<ActionResult<GenerateQuestionsResponse>> getQuestions(
        [FromForm] GenerateInterviewRequest generateQuestionsRequest)
    {
        var fileName = Guid.NewGuid().ToString() + "-" + generateQuestionsRequest.resume.FileName;
        var filePath = Path.Combine("Uploads", fileName);
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
        {
            await generateQuestionsRequest.resume.CopyToAsync(stream);
        }

        return await interviewService.generateQuestions(generateQuestionsRequest.jobDescription,
            generateQuestionsRequest.numberOfBehavioral, generateQuestionsRequest.numberOfTechnical, filePath,
            generateQuestionsRequest.additionalDescription, fileName);
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
       
        return interviewService.interviewToDTO(i);
    }


    [HttpDelete("{id}")]
    public async Task delete(int id)
    {
        var user = await base.GetCurrentUser();
        var interview = await interviewService.getInterview(id, user);

        await interviewService.deleteInterview(interview, user);
    }

    [HttpPut("")]
    public async Task<InterviewDTO> update([FromBody] InterviewDTO interviewDTO)
    {
        var interview = interviewService.DtoToInterview(interviewDTO);
        var user = await base.GetCurrentUser();
        var updated = await interviewService.updateInterview(interview, user);
        return interviewService.interviewToDTO(updated);
    }

    [HttpGet("{id}")]
    public async Task<InterviewDTO> getInterview(int id)
    {
        var user = await base.GetCurrentUser();
        return await interviewService.getInterviewDto(id, user);
    }

    [HttpGet("interviewList")]
    public async Task<List<InterviewDTO>> getInterviewList(
        [FromQuery] InterviewSearchParams interviewSearchParamsParams)
    {
        var user = await base.GetCurrentUser();
        PagedInterviewResponse pagedInterviewResponse =
            await interviewService.getInterviews(user, interviewSearchParamsParams);
        Response.AddPaginationHeader(pagedInterviewResponse.total, interviewSearchParamsParams.startIndex,
            interviewSearchParamsParams.pageSize);
        return pagedInterviewResponse.interviews.Select(x => interviewService.interviewToDTO(x)).ToList();
    }

    // get video file or signed url to video file in blob storage
    [HttpGet("getVideo/{fileName}")]
    public async Task<IActionResult> GetVideo(string fileName)
    {
        var user = await base.GetCurrentUser();
        var videoPath = Path.Combine("Uploads", fileName);
        bool canView = await interviewService.verifyVideoView(fileName, user);
        if (!canView)
        {
            return Unauthorized();
        }

        if (!AppConfig.UseSignedUrl)
        {
            var stream = await interviewService.serveFile(fileName, videoPath, "videos", HttpContext);
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
    public async Task<IActionResult> getResume(string fileName)
    {
        var user = await base.GetCurrentUser();
        // security check
        bool canView = await interviewService.verifyPdfView(fileName, user);
        if (!canView)
        {
            return Unauthorized();
        }


        if (!AppConfig.UseSignedUrl)
        {
            var pdfPath = Path.Combine("Uploads", fileName);

            var stream = await interviewService.serveFile(fileName, pdfPath, "resumes", HttpContext);

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
    public async Task<ResumeUrlAndName> getLatestResume()
    {
        var user = await base.GetCurrentUser();
        return await interviewService.getLatestResume(user);
    }

    [HttpGet("getAllResumes")]
    public async Task<ResumeUrlAndName[]> getAllResumes()
    {
        var user = await base.GetCurrentUser();
        var resumes = await interviewService.getAllResumes(user);
        return resumes;
    }
}