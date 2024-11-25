using API.AI;

namespace API.Interviews;

public class InterviewService(IOpenAIService openAiService): IinterviewService
{
    private readonly string _splitToken = "@u5W$";
    public async Task<RatingResponse> rateAnswer(string question, string videoPath)
    {
        string transcript =  await openAiService.GetTranscript(videoPath);

        string formatInstruction =
            $"Response should always be in format '{_splitToken}Good: insert your answer here {_splitToken}Needs Improvement: insert your answer here' Absolutely DO NOT forget the ${_splitToken} or this format or else the program breaks.   ";
        
        string prompt =
            $"Imagine you are an interviewer for a company and you are giving a candidate honest feedback about what they could have improved, write like you are talking to them face-to-face. Keep STAR method in mind as well and tone. Be very harsh, don't coddle , given this question " +
            $"${question} say what is good and what needs improvement about this answer ${transcript} give concise and specific feedback that's easy to understand, quick to read and work on immediately, provide examples of what they could've done instead if applicable. "
            + $"${formatInstruction} ";
        
        string response = await openAiService.MakeRequest(prompt);
        string[] split = response.Split(_splitToken);
        var retval =  new RatingResponse
        {
            good = split[1].Split("Good:")[1],
            bad = split[2].Split("Needs Improvement:")[1],
        };
        return retval;
    }

    public async Task<string> generateQuestion(string prompt)
    {
        return null;
    }
}