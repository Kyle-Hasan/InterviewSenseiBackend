﻿using API.Base;
using API.Interviews;

namespace API.Messages;

public class Message: BaseEntity
{
    
    public virtual Interview Interview { get; set; }
    public int InterviewId { get; set; }
    
    public string Content { get; set; }
    
    public bool FromAI {get; set;}
    
    public MessageType Type { get; set; }
    
    public string? Code { get; set; }
    
    
    
}