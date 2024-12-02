using API.Questions;
using API.Users;

namespace API.Interviews;

public interface IinterviewService
{
    Task<QuestionDTO> rateAnswer(string question, int questionId, string videoPath,string videoName, string serverUrl,AppUser user);

    Task<Interview> GenerateInterview(AppUser user, string interviewName, string jobDescription, int numberOfBehavioral,
        int numberOfTechnical, string resumePdfPath);

    Task<GenerateQuestionsResponse> generateQuestions(string jobDescription,int numberOfBehavioral, int numberOfTechnical, string resumePdfPath );
    
    Task deleteInterview(Interview interview, AppUser user);
    
    Task<Interview> updateInterview(Interview interview, AppUser user);
    
    Task<Interview> createInterview(Interview interview, AppUser user);
    
    Task<List<InterviewDTO>> getInterviews(AppUser user);

    Task<InterviewDTO> getInterviewDto(int id,AppUser user);
    
    Task<Interview> getInterview(int id,AppUser user);
    
    Task<bool> verifyVideoView(string fileName, AppUser user);
    
    


}