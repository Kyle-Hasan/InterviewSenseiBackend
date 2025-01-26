using API.Questions;
using API.Users;
using Microsoft.AspNetCore.Mvc;

namespace API.Interviews;

public interface IinterviewService
{
  

    Task<Interview> GenerateInterview(AppUser user, string interviewName, string jobDescription, int numberOfBehavioral,
        int numberOfTechnical, int secondsPerAnswer, string resumePdfPath, string additionalDescription, string resumeName,string serverUrl);

    Task<GenerateQuestionsResponse> generateQuestions(string jobDescription,int numberOfBehavioral, int numberOfTechnical, string resumePdfPath, string additionalDescription, string resumeName );
    
    Task deleteInterview(Interview interview, AppUser user);
    
    Task<Interview> updateInterview(Interview interview, AppUser user);
    
    Task<Interview> createInterview(Interview interview, AppUser user);
    
    Task<PagedInterviewResponse> getInterviews(AppUser user,InterviewSearchParams interviewSearchParamsParams);

    Task<InterviewDTO> getInterviewDto(int id,AppUser user);
    
    Task<Interview> getInterview(int id,AppUser user);
    
    Task<bool> verifyVideoView(string fileName, AppUser user);
    
    Task<bool> verifyPdfView(string fileName, AppUser user);

    InterviewDTO interviewToDTO(Interview interview);

    Interview DtoToInterview(InterviewDTO interviewDTO);

    Task<FileStream> serveFile(string fileName, string filePath, string folderName, HttpContext httpContext);
    
    // return the url and file name to the latest resume this user used, returning blank strings if they have no resumes uploaded.
    // If we are using signed urls, return the s3 bucket url here
    Task<ResumeUrlAndName> getLatestResume(AppUser user);




}