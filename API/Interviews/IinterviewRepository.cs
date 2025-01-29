using API.Users;

namespace API.Interviews;

public interface IinterviewRepository
{
    Task delete(Interview interview,AppUser user);
    Task<Interview> save(Interview interview, AppUser user);
    Task<PagedInterviewResponse> getInterviews(AppUser user, InterviewSearchParams interviewSearchParams);
    
   Task<Interview> getInterview(AppUser user, int id);
   
   
   
   Interview getChangedInterview(Interview newInterview, Interview oldInterview);
   
   Task<bool> verifyPdfView(AppUser user, string filePath);
    // get the link to the latest resume this used, if they have no resumes return null
   Task<string> getLatestResume(AppUser user);
   // only fills out url and date created, let the function calling handle logic for figuring out the display name
   Task<ResumeUrlAndName[]> getAllResumes(AppUser user);



}