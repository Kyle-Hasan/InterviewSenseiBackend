using System.Text.Json;
using API.AI;
using API.AWS;
using API.Interviews;
using API.Questions;
using API.Users;

namespace API.Responses;


public class Feedback
{
    public string positiveFeedback { get; set; }
    public string negativeFeedback { get; set; }
    public string exampleResponse { get; set; }
}

public class ResponseService(IOpenAIService openAiService,IResponseRepository responseRepository, IQuestionRepository questionRepository, IBlobStorageService blobStorageService): IResponseService
{
   
    public async Task<ResponseDto> RateAnswer(int questionId, string videoPath,string videoName, string serverUrl,AppUser user)
    {
        /* save video cloud in the background and delete one saved on server (cloud key is the same as video name for now so dont need to do anything with it)
         Don't delete video on server just yet to avoid the unlikely race condition it deletes the video before transcribing
         */
        Task<string> cloudKey = null;
        if (AppConfig.UseCloudStorage)
        {
            cloudKey = blobStorageService.UploadFileAsync(videoPath, videoName, "videos");
        }

        // transcribe api, find the question being answered and then ask chat gpt to rate the answer
        string transcript =  await openAiService.TranscribeAudioAPI(videoPath);
        Question question = await questionRepository.GetQuestionById(questionId,user);
   

        // response format outlined, split token used to easily split positive and negative feedback(used on frontend).
        // The token itself is a string that the answer nor the rating would ever normally contain, guaranteeing no weird splits due to that
        string jsonFormatInstruction = "Return your entire response as plain JSON with exactly these keys: { \\\"positiveFeedback\\\": string, \\\"negativeFeedback\\\": string, \\\"exampleResponse\\\": string }. Do not include any markdown formatting, code fences, or any extra text.";

        string prompt = "Imagine you are an interviewer for a top company giving brutally honest feedback using the STAR method. Make sure no field is empty " +
                        "If the transcript is nonsensical or empty, use 'no strengths shown' for positive feedback and 'Incomplete answer' for negative feedback. " +
                        "Focus solely on points mentioned in the candidate’s answer, providing clear, actionable feedback. Put separate points on newlines starting with \n. Do not include - at the beginning or anything else, no extra formatting on your end, I will handle display as bullet points. " +
                        "Include three follow-up questions and a refined sample answer that clearly outlines strengths, weaknesses, and improvements. Include follow up questions in negative feedback, each question on a separate line. " +
                        "Do not use any split tokens or extra separators; your entire response must be in JSON format as specified. SEND ONLY JSON as raw text no formatting no ` characters at all. DO NOT EVER INCLUDE NULL, if you have nothing to say, leave as empty string or put 'No feedback', we can't have the json parsing null ever " +
                        "\nQuestion: " + question.Body +
                        "\nCandidate's Answer: " + transcript +
                        "\n" + jsonFormatInstruction;



        
        string feedbackJson = await openAiService.MakeRequest(prompt);
        feedbackJson = feedbackJson.Replace("```json", "");
        feedbackJson = feedbackJson.Replace("```", "");
        
        Feedback feedback = JsonSerializer.Deserialize<Feedback>(feedbackJson);
        
        var response = await responseRepository.UpdateAnswer( transcript, feedback.positiveFeedback,feedback.negativeFeedback,feedback.exampleResponse, videoName,serverUrl,question.Id,user);
        // make sure video is uploaded before we leave this function so if an error happens we can deal with it
        if (AppConfig.UseCloudStorage)
        {
            await cloudKey;
            File.Delete(videoPath);
        }

      
        return new ResponseDto(response);

    }
}