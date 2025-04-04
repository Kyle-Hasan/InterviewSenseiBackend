﻿using System.Runtime.CompilerServices;
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

    public async Task<Question> GetQuestionById(int id,AppUser user)
    {
        var question =  await base.GetById(id);
        if (question.CreatedById != user.Id)
        {
            throw new UnauthorizedAccessException();
        }
        return question;
    }

    public async Task<Question> GetQuestionByIdWithInterview(int id, AppUser user)
    {
       return  _entities.Include(x=> x.Interview).ThenInclude(x=> x.Questions).Include(x=> x.Responses)
            .FirstOrDefault(x=> x.Id == id && x.CreatedById == user.Id);
            
    }


    public async Task<Question> updateAnswer(Question question, string answer,string positiveFeedback,string negativeFeedback,string exampleResponse, string videoName, string serverUrl, AppUser user)
    {
        
        
        var newResponse = await this.responseRepository.UpdateAnswer(answer,positiveFeedback,negativeFeedback,exampleResponse, videoName,serverUrl,question.Id, user);
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
            type = question.Type.ToString(),
            responses = new List<ResponseDto>()
        };
    }

    private static QuestionType ConvertStringToQuestionType(string questionType)
    {
        switch (questionType)
        {
            case "Behavioral":
                return QuestionType.Behavioral;
            case "Technical":
                return QuestionType.Technical;
            case "LiveCoding":
                return QuestionType.LiveCoding;
            case "CodeReview":
                return QuestionType.CodeReview;
            default:
                return QuestionType.Behavioral;
        }
    }

    public async Task<bool> VerifyVideoView(string fileName, AppUser user)
    {
       return await this.responseRepository.VerifyVideoView(fileName, user);
    }

    public Question ConvertQuestionToEntity(QuestionDTO questionDTO)
    {
        return new Question
        {
            Id = questionDTO.id,
           
            Body = questionDTO.body,
            
            Responses = questionDTO.responses.Select(x=> this.responseRepository.DtoToResponse(x)).ToList()

        };
    }

    public static Question createQuestionFromString(string body, string type)
    {
        return new Question
        {
            Body = body,
           
            Type = ConvertStringToQuestionType(type),
            Responses = new List<Response>()

        };
    }
}