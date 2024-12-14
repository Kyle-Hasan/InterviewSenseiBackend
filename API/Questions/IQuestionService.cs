using API.Users;

namespace API.Questions;

public interface IQuestionService
{
    Task<QuestionPageDto> GetQuestionAsync(int questionId);
}