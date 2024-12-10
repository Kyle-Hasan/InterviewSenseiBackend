namespace API.Interviews;

public class RatingRequestDTO
{
    public IFormFile video  { get; set; }
    
    public string questionId {get;set;}
}