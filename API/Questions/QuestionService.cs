using API.Interviews;

namespace API.Questions;

public class QuestionService(IQuestionRepository questionRepository, IinterviewRepository interviewRepository): IQuestionService
{
    public async Task<QuestionPageDto> GetQuestionAsync(int questionId)
    {
        Question q = await questionRepository.getQuestionById(questionId);

        return null;

    }
}