using API.Users;

namespace API.Interviews;

public interface IinterviewRepository
{
    Task Delete(Interview interview,AppUser user);
    Task<Interview> Save(Interview interview, AppUser user);
    Task<PagedInterviewResponse> GetInterviews(AppUser user, InterviewSearchParams interviewSearchParams);
    
   Task<Interview> GetInterview(AppUser user, int id);
   
   
   
   Interview GetChangedInterview(Interview newInterview, Interview oldInterview);
   
   Task<bool> VerifyPdfView(AppUser user, string filePath);
    // get the link to the latest resume this used, if they have no resumes return null
   Task<string> GetLatestResume(AppUser user);
   // only fills out url and date created, let the function calling handle logic for figuring out the display name
   Task<ResumeUrlAndName[]> GetAllResumes(AppUser user);



}