﻿using API.Questions;

namespace API.Interviews;

public class InterviewDTO
{
    public List<QuestionDTO>? questions { get; set; }
    
    public int id {get; set;}
    public string name {get; set;}
    
    public string jobDescription {get; set;}
    
    public string resumeLink {get; set;}
    
    public string createdDate {get; set;}
    
    
    
}