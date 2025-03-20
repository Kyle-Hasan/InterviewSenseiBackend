using API.AWS;
using API.Base;
using API.Extensions;
using API.Questions;
using API.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using API.InteractiveInterviewFeedback;
using API.PDF;

namespace API.Interviews;

[Authorize]
public class InterviewController(
    IinterviewService interviewService,
    UserManager<AppUser> userManager,
    IBlobStorageService blobStorageService,
    IinterviewFeedbackService interviewFeedbackService,
    IFileService fileService) : BaseController(userManager)
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
        [FromForm] GenerateInterviewRequest generateInterviewRequest)
    {
        if (!generateInterviewRequest.isLive &&
            (generateInterviewRequest.numberOfBehavioral + generateInterviewRequest.numberOfTechnical == 0))
        {
            return BadRequest();
        }
        else if (!generateInterviewRequest.isLive && (generateInterviewRequest.secondsPerAnswer < 10 ||
                                                      generateInterviewRequest.numberOfBehavioral > 300))
        {
            return BadRequest();
        }

        string filePath = "";
        string fileName = "";
        string serverUrl = $"{Request.Scheme}s://{Request.Host}{Request.PathBase}/api/Interview/getPdf";
        // existing resume url, should be on our server so to get its name just strip the server part
        if (generateInterviewRequest.resumeUrl != null)
        {

            var fileTuple = await fileService.DownloadPdf(generateInterviewRequest.resumeUrl);
            filePath = fileTuple.FilePath;
            fileName = fileTuple.FileName;
        }
        // new resume uploaded directly(only when no signed urls)
        else if (generateInterviewRequest.resume != null &&
                 generateInterviewRequest.resume.ContentType == "application/pdf")
        {
            var fileTuple = await IFileService.CreateNewFile(generateInterviewRequest.resume);
            fileName = fileTuple.FileName;
            filePath = fileTuple.FilePath;

        }


      

        var i = await interviewService.GenerateInterview(CurrentUser, generateInterviewRequest.name,
            generateInterviewRequest.jobDescription,
            generateInterviewRequest.numberOfBehavioral, generateInterviewRequest.numberOfTechnical,
            generateInterviewRequest.secondsPerAnswer, filePath, generateInterviewRequest.additionalDescription,
            fileName, serverUrl, generateInterviewRequest.isLive);

        return interviewService.InterviewToDTO(i);
    }


    [HttpDelete("{id}")]
    public async Task Delete(int id)
    {
        
        var interview = await interviewService.GetInterview(id, CurrentUser);

        await interviewService.DeleteInterview(interview, CurrentUser);
    }

    [HttpPut("")]
    public async Task<InterviewDTO> Update([FromBody] InterviewDTO interviewDTO)
    {
        var interview = interviewService.DtoToInterview(interviewDTO);
       
        var updated = await interviewService.UpdateInterview(interview, CurrentUser);
        return interviewService.InterviewToDTO(updated);
    }

    [HttpGet("{id}")]
    public async Task<InterviewDTO> GetInterview(int id)
    {
        
        return await interviewService.GetInterviewDto(id, CurrentUser);
    }

    [HttpGet("interviewList")]
    public async Task<List<InterviewDTO>> GetInterviewList(
        [FromQuery] InterviewSearchParams interviewSearchParamsParams)
    {
        PagedInterviewResponse pagedInterviewResponse =
            await interviewService.GetInterviews(CurrentUser, interviewSearchParamsParams);
        Response.AddPaginationHeader(pagedInterviewResponse.total, interviewSearchParamsParams.startIndex,
            interviewSearchParamsParams.pageSize);
        return pagedInterviewResponse.interviews.Select(x => interviewService.InterviewToDTO(x)).ToList();
    }

    // get video file or signed url to video file in blob storage
    [HttpGet("getVideo/{fileName}")]
    public async Task<IActionResult> GetVideo(string fileName)
    {
        
        var videoPath = Path.Combine("Uploads", fileName);
        bool canView = await interviewService.VerifyVideoView(fileName, CurrentUser);
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
      
        // security check
        bool canView = await interviewService.VerifyPdfView(fileName, CurrentUser);
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
        
        return await interviewService.GetLatestResume(CurrentUser);
    }

    [HttpGet("getAllResumes")]
    public async Task<ResumeUrlAndName[]> GetAllResumes()
    {
       
        var resumes = await interviewService.GetAllResumes(CurrentUser);
        return resumes;
    }

    [HttpPost]
    [Route("endInterview")]

    public async Task<InterviewFeedbackDTO> EndInterview([FromForm] InterviewEndRequest request)
    {
        string serverUrl = $"{Request.Scheme}s://{Request.Host}{Request.PathBase}/api/Interview/getVideo";

        if (request.video == null)
        {
            throw new BadHttpRequestException("No video provided");
        }
        return await interviewFeedbackService.EndInterview(CurrentUser, request.interviewId,request.video,serverUrl);
    }

    [HttpGet("getFeedbackAndMessages")]
    public async Task<FeedbackAndTranscript> GetFeedbackAndMessages([FromQuery] int interviewId)
    {
        return await interviewService.GetFeedbackAndMessages(CurrentUser,interviewId);
    }
}