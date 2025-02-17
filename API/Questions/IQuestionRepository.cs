using API.Users;

namespace API.Questions;

public interface IQuestionRepository
{
    Task<Question> saveQuestion(Question question,AppUser user);
    Task deleteQuestion(Question question, AppUser user);

    Task<Question> updateAnswer(Question question, string answer, string positiveFeedback,string negativeFeedback,string exampleResponse, string videoName,string serverUrl, AppUser user);

    Task<Question> getQuestionById(int id,AppUser user);
    
    Task<Question> getQuestionByIdWithInterview(int id,AppUser user);
    
    

    QuestionDTO convertQuestionToDTO(Question question);
    
    Task<bool> verifyVideoView(string fileName, AppUser user);

    Question convertQuestionToEntity(QuestionDTO questionDTO);
    
    


}