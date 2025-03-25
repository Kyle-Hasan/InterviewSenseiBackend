namespace API.Interviews;

public record RatingRequestDTO
{
    public IFormFile video  { get; set; }
    
    public string questionId {get;set;}
}