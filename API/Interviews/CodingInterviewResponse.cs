using API.InteractiveInterviewFeedback;
using API.Messages;

namespace API.Interviews;

public record CodingInterviewResponse(string? userCode,string? codeLanguageName,InterviewFeedbackDTO? feedback,List<MessageDTO>? messages,string? questionBody);