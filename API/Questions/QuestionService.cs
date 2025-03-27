using API.AI;
using API.Interviews;
using API.Responses;
using API.Users;

namespace API.Questions;

public class QuestionService(IQuestionRepository questionRepository, IinterviewRepository interviewRepository, IResponseRepository responseRepository, IOpenAIService aiService): IQuestionService
{
    public async Task<QuestionPageDto> GetQuestionAsync(int questionId,AppUser user)
    {
        Question q = await questionRepository.GetQuestionByIdWithInterview(questionId,user);
        
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
        QuestionPageDto dto = new QuestionPageDto(q);
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
        dto.secondsPerAnswer = interview.SecondsPerAnswer;
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
    
    private string GetLiveCodingInterviewPrompt(string jobDescription, string additionalDescription)
    {
        return $@"
Generate a random LeetCode-like problem, make it hard. 
Keep additional description in mind but dont stray too far from original idea.
Also only return the text needed for the problem and some example text cases, nothing else.
No formatting, imagine this is put into a python comment, only \n and no other formatting. No **, `` or \
ONLY GET PROBLEM statement, no solutions or adding your own thoughts.
=== ADDITIONAL DESCRIPTION ===
{additionalDescription}";
    }

    private string GetCodeReviewInterviewPrompt(string jobDescription, string additionalDescription)
    {
        return $@"
    You are an experienced software engineer conducting a code review interview. Return your entire answer as a single block of text with no special formatting.

    If a job description and additional context are provided, review them and craft interview questions involving code reviews that test the candidate’s relevant skills.

    If no job description or additional context is provided, conduct a random code review interview by providing a short generic code snippet in any common language and asking the candidate to identify issues, suggest improvements, and explain best practices.
    Only problem statement, don't need your thoughts.
    Only the code is needed at this point.
    === JOB DESCRIPTION ===
    {jobDescription}

    === ADDITIONAL DESCRIPTION ===
    {additionalDescription}";
    }

    public async Task<Question> CreateLiveCodingQuestion(string additionalDescription, AppUser user)
    {
        return await CreateCodeQuestion(additionalDescription,"", user,QuestionType.LiveCoding);
    }

    public async Task<Question> CreateCodeReviewQuestion(string additionalDescription, string jobDescription, AppUser user)
    {
        return await CreateCodeQuestion(additionalDescription, jobDescription, user, QuestionType.CodeReview);
    }

    private async Task<Question> CreateCodeQuestion(string additionalDescription, string jobDescription, AppUser user, QuestionType questionType)
    {
        string prompt = "";
        if (questionType == QuestionType.LiveCoding)
        {
            prompt = GetLiveCodingInterviewPrompt(additionalDescription, additionalDescription);
        }
        else if (questionType == QuestionType.CodeReview)
        {
            prompt = GetCodeReviewInterviewPrompt(jobDescription, additionalDescription);
        }
        
        string aiResponse = await aiService.MakeRequest(prompt);
        Question question = new Question();
        question.Body = aiResponse;
        question.Responses = new List<Response>();
        question.Type = questionType;
        question.isPremade = false;
        return question;
        
    }

    public async Task<List<QuestionPageDto>> GetQuestionsByInterviewId(int interviewId, AppUser user)
    {
        Interview interview = await interviewRepository.GetInterview(user,interviewId);
        if (interview == null)
        {
            throw new UnauthorizedAccessException();
        }
        List<Question> questions = interview.Questions;
        return  ConvertToDtos(questions, interview);
        
        
    }
}