using API.Questions;
using API.Users;
using Microsoft.AspNetCore.Mvc;

namespace API.Interviews;

public interface IinterviewService
{
  

    Task<Interview> GenerateInterview(AppUser user, string interviewName, string jobDescription, int numberOfBehavioral,
        int numberOfTechnical, int secondsPerAnswer, string resumePdfPath, string additionalDescription, string resumeName,string serverUrl, bool isLive);

    Task<GenerateQuestionsResponse> GenerateQuestions(string jobDescription,int numberOfBehavioral, int numberOfTechnical, string resumePdfPath, string additionalDescription, string resumeName );
    
    Task DeleteInterview(Interview interview, AppUser user);
    
    Task<Interview> UpdateInterview(Interview interview, AppUser user);
    
    
    Task<PagedInterviewResponse> GetInterviews(AppUser user,InterviewSearchParams interviewSearchParamsParams);

    Task<InterviewDTO> GetInterviewDto(int id,AppUser user);
    
    Task<Interview> GetInterview(int id,AppUser user);
    
    Task<bool> VerifyVideoView(string fileName, AppUser user);
    
    Task<bool> VerifyPdfView(string fileName, AppUser user);

    InterviewDTO InterviewToDTO(Interview interview);

    Interview DtoToInterview(InterviewDTO interviewDTO);

    Task<FileStream> ServeFile(string fileName, string filePath, string folderName, HttpContext httpContext);
    
    // return the url and file name to the latest resume this user used, returning blank strings if they have no resumes uploaded.
    // If we are using signed urls, return the s3 bucket url here
    Task<ResumeUrlAndName> GetLatestResume(AppUser user);
// fills out the display name witout the guid we added
    Task<ResumeUrlAndName[]> GetAllResumes(AppUser user);
    
    
    




}