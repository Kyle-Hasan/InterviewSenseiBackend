using System.Runtime.CompilerServices;
using API.Base;
using API.Data;
using API.Responses;
using API.Users;
using Microsoft.EntityFrameworkCore;

namespace API.Questions;

public class QuestionRepository:BaseRepository<Question>,IQuestionRepository
{
    private readonly IResponseRepository responseRepository;
    public QuestionRepository(AppDbContext appDbContext, IResponseRepository responseRepository) : base(appDbContext)
    {
        this.responseRepository = responseRepository;
    }

    public async Task<Question> saveQuestion(Question question,AppUser user)
    {
       return await base.Save(question,user);
    }

    public async Task<Question> getQuestionById(int id,AppUser user)
    {
        var question =  await base.getById(id);
        if (question.CreatedById != user.Id)
        {
            throw new UnauthorizedAccessException();
        }
        return question;
    }

    public async Task<Question> getQuestionByIdWithInterview(int id, AppUser user)
    {
       return  _entities.Include(x=> x.Interview).ThenInclude(x=> x.Questions).Include(x=> x.Responses)
            .FirstOrDefault(x=> x.Id == id && x.CreatedById == user.Id);
            
    }


    public async Task<Question> updateAnswer(Question question, string answer,string positiveFeedback,string negativeFeedback,string exampleResponse, string videoName, string serverUrl, AppUser user)
    {
        
        
        var newResponse = await this.responseRepository.updateAnswer(answer,positiveFeedback,negativeFeedback,exampleResponse, videoName,serverUrl,question.Id, user);
        question.Responses.Add(newResponse);
        return await base.Save(question, user);
    }

    public async Task deleteQuestion(Question question,AppUser user)
    {
        await base.Save(question,user);
    }

    public QuestionDTO convertQuestionToDTO(Question question)
    {
        return new QuestionDTO
        {
            id = question.Id,
          
            body = question.Body,
            type = question.Type,
            responses = new List<ResponseDto>()
        };
    }

    public async Task<bool> verifyVideoView(string fileName, AppUser user)
    {
       return await this.responseRepository.verifyVideoView(fileName, user);
    }

    public Question convertQuestionToEntity(QuestionDTO questionDTO)
    {
        return new Question
        {
            Id = questionDTO.id,
           
            Body = questionDTO.body,
            
            Responses = questionDTO.responses.Select(x=> this.responseRepository.dtoToResponse(x)).ToList()

        };
    }

    public static Question createQuestionFromString(string body, string type)
    {
        return new Question
        {
            Body = body,
           
            Type = type,
            Responses = new List<Response>()

        };
    }
}