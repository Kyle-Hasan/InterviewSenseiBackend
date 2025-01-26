using API.Interviews;
using API.Responses;
using API.Users;

namespace API.Questions;

public class QuestionService(IQuestionRepository questionRepository, IinterviewRepository interviewRepository, IResponseRepository responseRepository): IQuestionService
{
    public async Task<QuestionPageDto> GetQuestionAsync(int questionId,AppUser user)
    {
        Question q = await questionRepository.getQuestionByIdWithInterview(questionId,user);
        
        Interview interview = q.Interview;
        // order by id so that the same order is always sent back
        List<Question> sortedList = interview.Questions.OrderBy(x => x.Id).ToList();
        var dto = convertToDto(q, sortedList,interview);
        return dto;

     

    }

    private QuestionPageDto convertToDto(Question q, List<Question> sortedList, Interview interview)
    {
        int index = -1;
        for (int i = 0; i < sortedList.Count; i++)
        {
            if (sortedList[i].Id == q.Id)
            {
                index = i;
                break;
            }
        }
        int length = interview.Questions.Count;
        QuestionPageDto dto = new QuestionPageDto();
        dto.body = q.Body;
        dto.responses = q.Responses.Select(x => responseRepository.convertToDto(x)).ToList();
        dto.id = q.Id;
        dto.type = q.Type;
        if (index > 0)
        {
            dto.previousQuestionId =  sortedList[index - 1].Id;
        }
        else
        {
            dto.previousQuestionId = -1;
        }

        if (index < length - 1)
        {
            dto.nextQuestionId = sortedList[index + 1].Id;
        }
        else
        {
            dto.nextQuestionId = -1;
        }
        dto.interviewId = interview.Id;
        dto.secondsPerAnswer = interview.secondsPerAnswer;
        return dto;
    } 

    public List<QuestionPageDto> ConvertToDtos(List<Question> questions, Interview interview)
    {
        List<QuestionPageDto> dtos = new List<QuestionPageDto>();
        
        List<Question> sortedList = questions.OrderBy(x => x.Id).ToList();

        foreach (Question q in questions)
        {
            var dto  = convertToDto(q, sortedList, interview);
            dtos.Add(dto);
        }
        
        return dtos;
    }

    public async Task<List<QuestionPageDto>> GetQuestionsByInterviewId(int interviewId, AppUser user)
    {
        Interview interview = await interviewRepository.getInterview(user,interviewId);
        return  ConvertToDtos(interview.Questions, interview);
        
        
    }
}