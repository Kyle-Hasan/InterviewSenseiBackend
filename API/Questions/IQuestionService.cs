﻿using API.Interviews;
using API.Users;

namespace API.Questions;

public interface IQuestionService
{
    Task<QuestionPageDto> GetQuestionAsync(int questionId,AppUser user);

    Task<List<QuestionPageDto>> GetQuestionsByInterviewId(int interviewId, AppUser user);

    List<QuestionPageDto> ConvertToDtos(List<Question> questions, Interview interview);
    
    
    Task<Question> CreateLiveCodingQuestion(string additionalDescription, AppUser user);
    
    Task<Question> CreateCodeReviewQuestion(string additionalDescription, string jobDescription, AppUser user);
}