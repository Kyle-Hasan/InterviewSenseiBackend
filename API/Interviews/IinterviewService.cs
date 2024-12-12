using API.Questions;
using API.Users;

namespace API.Interviews;

public interface IinterviewService
{
    Task<QuestionDTO> rateAnswer(int questionId, string videoPath,string videoName, string serverUrl,AppUser user);

    Task<Interview> GenerateInterview(AppUser user, string interviewName, string jobDescription, int numberOfBehavioral,
        int numberOfTechnical, int secondsPerAnswer, string resumePdfPath, string additionalDescription, string resumeName,string serverUrl);

    Task<GenerateQuestionsResponse> generateQuestions(string jobDescription,int numberOfBehavioral, int numberOfTechnical, string resumePdfPath, string additionalDescription );
    
    Task deleteInterview(Interview interview, AppUser user);
    
    Task<Interview> updateInterview(Interview interview, AppUser user);
    
    Task<Interview> createInterview(Interview interview, AppUser user);
    
    Task<PagedInterviewResponse> getInterviews(AppUser user,InterviewSearchParams interviewSearchParamsParams);

    Task<InterviewDTO> getInterviewDto(int id,AppUser user);
    
    Task<Interview> getInterview(int id,AppUser user);
    
    Task<bool> verifyVideoView(string fileName, AppUser user);
    
    Task<bool> verifyPdfView(string fileName, AppUser user);
    
    


}