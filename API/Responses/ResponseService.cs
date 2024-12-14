using API.AI;
using API.Interviews;
using API.Questions;
using API.Users;

namespace API.Responses;

public class ResponseService(IOpenAIService openAiService,IResponseRepository responseRepository, IQuestionRepository questionRepository): IResponseService
{
    private readonly string _splitToken = "@u5W$";
    public async Task<ResponseDto> rateAnswer(int questionId, string videoPath,string videoName, string serverUrl,AppUser user)
    {
        string transcript =  await openAiService.TranscribeAudioAPI(videoPath);
        Question question = await questionRepository.getQuestionById(questionId);

        string formatInstruction =
            $"Response should always be in format '{_splitToken} Good: insert your answer here {_splitToken} Needs Improvement: insert your answer here' Absolutely DO NOT forget the ${_splitToken} or this format or else the program breaks.   ";
        
        string prompt =
            $"Imagine you are an interviewer who is a complete perfectionist for a company and you are giving a candidate brutally honest feedback about what they could have improved, write like you are talking to them face-to-face. Keep STAR method in mind as well and tone. I want highly critical and specific feedback. Avoid general or vague statements. If transcript is nonsensical or empty just mention 'no strengths shown...' in good and 'Incomplete answer...' in Needs Improvement " +
            $"Use a blunt tone and be as strict as possible.Avoid sugarcoating or fake compliments or feeling like you have to give praise, if its bad don't praise at all. Be very very cynical and give no benefit of the doubt. If they mention something relevent praise that. Anything that sounds vague or unclear assume the worst but if it's relevent note it as strength for at least mentioning, but mention that vagueness could be improved on with examples. Don't force yourself to find strong points, only consider them strong points if they make you think this person is qualified for the job, its ok to only have 'You showed no strengths in your response' Separate all sentences using 3 periods always " +
            $" , given this question " +
            $"${question.Body} say what is good and what needs improvement about this answer ${transcript} give concise and specific feedback that's easy to understand, quick to read and work on immediately, provide examples of what they could've done instead if applicable. "
            + $"${formatInstruction} ";
        
        string feedback = await openAiService.MakeRequest(prompt);
        string[] split = feedback.Split(_splitToken);
        var response = await responseRepository.updateAnswer( transcript, feedback, videoName,serverUrl,question.Id,user);
        /*var retval =  new RatingResponse
        {
            good = split[1].Split("Good:")[1],
            bad = split[2].Split("Needs Improvement:")[1],
        };*/
        return responseRepository.convertToDto(response);

    }
}