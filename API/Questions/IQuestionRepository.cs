using API.Users;

namespace API.Questions;

public interface IQuestionRepository
{
    Task<Question> saveQuestion(Question question,AppUser user);
    Task deleteQuestion(Question question, AppUser user);


    Task<Question> GetQuestionById(int id,AppUser user);
    
    Task<Question> GetQuestionByIdWithInterview(int id,AppUser user);
    
    

    
    Task<bool> VerifyVideoView(string fileName, AppUser user);

    Question ConvertQuestionToEntity(QuestionDTO questionDTO);
    
    


}