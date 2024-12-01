using API.Base;
using API.Questions;

namespace API.Interviews;

public class Interview:BaseEntity
{
    public string Name { get; set; }
    public List<Question> Questions { get; set; }
    
    public string ResumeLink { get; set; }
    
    public string JobDescription { get; set; }
}