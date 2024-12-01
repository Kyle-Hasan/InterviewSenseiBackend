using API.Base;
using API.Data;
using API.Users;
using Microsoft.EntityFrameworkCore;

namespace API.Questions;

public class QuestionRepository:BaseRepository<Question>,IQuestionRepository
{
    public QuestionRepository(AppDbContext appDbContext) : base(appDbContext)
    {
    }

    public async Task<Question> saveQuestion(Question question,AppUser user)
    {
       return await base.Save(question,user);
    }

    public async Task<Question> getQuestionById(int id)
    {
        return await base.getById(id);
    }

    public async Task<bool> verifyVideoView(string filename, AppUser user)
    {
        return await base._entities.AnyAsync(q => q.VideoLink.Contains(filename) && q.CreatedById == user.Id);
    }

    public async Task<Question> updateAnswer(int id, string answer,string feedback, string videoPath, AppUser user)
    {
        Question question = await base.getById(id);
        question.Response = answer;
        question.Feedback = feedback;
        question.VideoLink = videoPath;
        return await base.Save(question, user);
    }

    public async Task deleteQuestion(Question question,AppUser user)
    {
        await base.Save(question,user);
    }

    public static QuestionDTO convertQuestionToDTO(Question question)
    {
        return new QuestionDTO
        {
            id = question.Id,
            response = question.Response,
            videoLink = question.VideoLink,
            body = question.Body,
            type = question.Type,
            feedback = question.Feedback,
        };
    }

    public static Question convertQuestionToEntity(QuestionDTO questionDTO)
    {
        return new Question
        {
            Id = questionDTO.id,
            Response = questionDTO.response,
            VideoLink = questionDTO.videoLink,
            Body = questionDTO.body,
            Feedback = questionDTO.feedback,

        };
    }

    public static Question createQuestionFromString(string body, string type)
    {
        return new Question
        {
            Body = body,
            Response = "",
            VideoLink = "",
            Type = type,
            Feedback = "",

        };
    }
}