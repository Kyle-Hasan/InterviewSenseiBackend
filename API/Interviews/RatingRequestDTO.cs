namespace API.Interviews;

public class RatingRequestDTO
{
    public IFormFile video  { get; set; }
    public string question { get; set; }
    public string id {get;set;}
}