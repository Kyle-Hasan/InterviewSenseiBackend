using API.Users;

namespace API.Questions;

public interface IQuestionRepository
{
    Task<Question> saveQuestion(Question question,AppUser user);
    Task deleteQuestion(Question question, AppUser user);

    Task<Question> updateAnswer(int id, string answer, string feedback, string videoPath, AppUser user);

    Task<Question> getQuestionById(int id);
    
    Task<bool> verifyVideoView(string filename,AppUser user);
}