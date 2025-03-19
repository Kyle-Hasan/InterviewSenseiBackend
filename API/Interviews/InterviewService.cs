using API.AI;
using API.AWS;
using API.Questions;
using API.Users;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using API.Extensions;
using System.Collections.Generic;
using System.Text;
using API.InteractiveInterviewFeedback;
using API.Messages;
using API.PDF;

namespace API.Interviews;

public class InterviewService(
    IOpenAIService openAiService,
    IinterviewRepository interviewRepository,
    IQuestionRepository questionRepository,
    IQuestionService questionService,
    IBlobStorageService blobStorageService,
    IFileService fileService,
    IMessageService messageService) : IinterviewService
{

    private string GetQuestionPrompt(string jobDescription, string resume, int numberOfBehavioral,
        int numberOfTechnical, string additionalDescription)
    {
        return $@"
        You are an AI specialized in creating highly relevant interview questions.

        Job Description:
        {jobDescription}

        Candidate Resume:
        {resume}

        Generate exactly {numberOfBehavioral} behavioral interview questions that evaluate soft skills and cultural fit, double check this.
        Generate exactly {numberOfTechnical} technical interview questions that assess job-specific technical expertise, double check this.

        Follow these guidelines:

1. **Behavioral Questions**:
   - Use the candidate's past work experience and projects as the foundation for the questions.
   - Focus on soft skills like teamwork, adaptability, problem-solving, and leadership.
   - Questions must strictly align with the candidate's experience or the job description.

2. **Technical Questions**:
   - Pull all technical questions strictly from the job description. 
   - Prioritize skills mentioned in both the job description and the resume.
   - If the job description does not include specific technical skills, pull from the resume alone.
   - If no job description or resume is available, generate generic technical questions relevant to the specified role (e.g., Full Stack Developer).
   - **Framework-/Language-Specific Questions*:
     - Prioritize practical, hands-on questions about frameworks, languages, and tools explicitly mentioned in the job description or resume (e.g., C#, Angular, SQL Server).
     - Examples:
       - ""What is dependency injection in Angular, and how would you use it in a real project?""
       - ""Explain the difference between a `Task` and a `ValueTask` in C# and when to use each.""
   - **Broad Design Questions**:
     - Include some broader design questions to evaluate the candidate’s understanding of concepts like performance optimization, scalability, or UI/UX design.
     - Examples:
       - ""How would you approach designing a responsive user interface in Angular? What are some key considerations you would keep in mind?""
       - ""Describe how you would structure a microservices-based application and manage its communication.""
   - Ensure an absolute pure focus on Framework-/Language-Specific Questions.
3. **Formatting**:
   - Use the exact format below without any deviations or special characters like `*`:
     Behavioral Questions:
     1. [Behavioral question 1]
     2. [Behavioral question 2]
     ...

     Technical Questions:
     1. [Technical question 1]
     2. [Technical question 2]
     ...

4. **Relevance**:
   - Strictly avoid asking about technologies or skills not mentioned in the job description or resume.
   - Ensure all questions are practical and cover a broad range of skills mentioned in the job description.

5. **Fallback Rules**:
   - If no job description is provided, only generate questions from the resume.
   - If no resume is provided, use generic technical questions based on the job title or role.

6. **Question Depth**:
   - For technical questions, ask about practical applications, comparisons (e.g., ""Explain the difference between X and Y""), or specific implementation steps.
   - Ensure all questions are concise, clear, and relevant.

7. **Avoid Special Characters**:
   - Do not use special characters like `*` in the response under any circumstances.
   - Responses should use plain text formatting for clarity.
        ";

    }

    public async Task<GenerateQuestionsResponse> GenerateQuestions(string jobDescription, int numberOfBehavioral,
        int numberOfTechnical, string resumePdfPath, string additionalDescription, string resumeName)
    {
        string resume = "";


        if (!string.IsNullOrEmpty(resumePdfPath))
        {
            resume = await fileService.GetPdfTranscriptAsync(resumePdfPath);
        }

        /* upload resume in the background to blob storage,delete old resume because we are done with it
         after reading it to avoid an error where 2 processes are using the same file

        */
        Task<string> cloudKey = null;
        if (AppConfig.UseCloudStorage && !String.IsNullOrEmpty(resumeName) && !String.IsNullOrEmpty(resumePdfPath))
        {
            cloudKey = blobStorageService.UploadFileDeleteAsync(resumePdfPath, resumeName, "resumes");
        }

        // put everything in prompt for chat gpt request(info about format in prompt)
        string prompt = GetQuestionPrompt(jobDescription,resume,numberOfBehavioral,numberOfTechnical,additionalDescription);

        if (!string.IsNullOrEmpty(additionalDescription))
        {
            prompt +=
                " Please consider this additional description when you make the questions, assume this information has a very importance, be certain it is taken into account.  If asked to include specific questions, make your own call of whether to count as behavioral or technical but don't go over the user specified caps for either category ever. If you decide that the information is unrelated to other information, then put it under behavioral. \n" +
                additionalDescription;
        }

        string response = await openAiService.MakeRequest(prompt);
        string[] behavioralQuestions = new string[] { };
        string[] technicalQuestions = new string[] { };
        // split questions into categories and send back
        string[] sections = response.Split(new string[] { "Behavioral Questions:", "Technical Questions:" },
            StringSplitOptions.RemoveEmptyEntries);
        if (numberOfBehavioral > 0)
        {
            behavioralQuestions = sections[0].Split("\n").Skip(1).Where(q => q.Length >= 4)
                .Select(q => q.Trim().Substring(3)).ToArray();
        }

        if (numberOfTechnical > 0)
        {
            technicalQuestions = sections[1].Split("\n").Skip(1).Where(q => q.Length >= 4)
                .Select(q => q.Trim().Substring(3)).ToArray();
        }

        // wait for resume upload in order to deal with errors there
        if (AppConfig.UseCloudStorage && !String.IsNullOrEmpty(resumeName) && !String.IsNullOrEmpty(resumePdfPath))
        {
            await cloudKey;
        }

        return new GenerateQuestionsResponse
        {
            behavioralQuestions = behavioralQuestions,
            technicalQuestions = technicalQuestions,
        };
    }


    private async Task<List<Question>>  CreateNonInteractiveInterviewQuestions(string jobDescription, int numberOfBehavioral, int numberOfTechnical, string resumePdfPath,string additionalDescription,string resumeName)
    {
        var questions = await GenerateQuestions(jobDescription, numberOfBehavioral, numberOfTechnical, resumePdfPath,
            additionalDescription, resumeName);
        var technicalQuestions =
            questions.technicalQuestions.Select(x => QuestionRepository.createQuestionFromString(x, "technical"))
                .ToList();
        var behavioralQuestions = questions.behavioralQuestions
            .Select(x => QuestionRepository.createQuestionFromString(x, "behavioral")).ToList();
        var questionList = new List<Question>();
        if (technicalQuestions.Any())
        {
            questionList.AddRange(technicalQuestions);
        }

        if (behavioralQuestions.Any())
        {
            questionList.AddRange(behavioralQuestions);
        }
        return questionList;
    }
    public async Task<Interview> GenerateInterview(AppUser user, string interviewName, string jobDescription,
        int numberOfBehavioral, int numberOfTechnical, int secondsPerAnswer, string resumePdfPath,
        string additionalDescription, string resumeName, string serverUrl, bool isLive)
    {
        Interview interview = new Interview();

        if (!isLive)
        {
            var questionList = await CreateNonInteractiveInterviewQuestions(jobDescription, numberOfBehavioral, numberOfTechnical, resumePdfPath,additionalDescription, resumeName);
            interview.Questions = questionList;
        }

       

        if (string.IsNullOrEmpty(jobDescription))
        {
            jobDescription = "";
        }

        if (string.IsNullOrEmpty(resumePdfPath))
        {
            resumePdfPath = "";
        }

        if (string.IsNullOrEmpty(interviewName))
        {
            throw new BadHttpRequestException("Interview name is required.");
        }

        interview.Name = interviewName;
       
        interview.JobDescription = jobDescription;
        if (!string.IsNullOrEmpty(resumeName))
        {
            interview.ResumeLink = serverUrl + "/" + resumeName;
        }

        interview.SecondsPerAnswer = secondsPerAnswer;
        interview.AdditionalDescription = additionalDescription;
        interview.IsLive = isLive;

        var i = await createInterview(interview, user);

        return i;
    }

    public async Task DeleteInterview(Interview interview, AppUser user)
    {
        await interviewRepository.Delete(interview, user);
    }

    // only update properties that changed
    public async Task<Interview> UpdateInterview(Interview interview, AppUser user)
    {
        Interview oldInterview = await GetInterview(interview.Id, user);
        Interview toUpdated = interviewRepository.GetChangedInterview(interview, oldInterview);
        return await interviewRepository.Save(toUpdated, user);
    }

    public async Task<Interview> createInterview(Interview interview, AppUser user)
    {
        return await interviewRepository.Save(interview, user);
    }

    public async Task<PagedInterviewResponse> GetInterviews(AppUser user, InterviewSearchParams interviewSearchParams)
    {
        return await interviewRepository.GetInterviews(user, interviewSearchParams);
    }

    public async Task<Interview> GetInterview(int id, AppUser user)
    {
        Interview i = await interviewRepository.GetInterview(user, id);
        return i;
    }

    //verify methods check whether that user can view the file

    public async Task<bool> VerifyVideoView(string fileName, AppUser user)
    {
        // cant do multithreading nicely due to dbcontext not being thread-safe, do it sequentially for simplicity
        var onQuestion =  await questionRepository.VerifyVideoView(fileName, user);
        if (onQuestion)
        {
            return true;
        }
        var onVideoTask  = await interviewRepository.VerifyVideoView(user,fileName);
        return onVideoTask;
        
        
    }

    public async Task<bool> VerifyPdfView(string fileName, AppUser user)
    {
        return await interviewRepository.VerifyPdfView(user, fileName);
    }

    public async Task<InterviewDTO> GetInterviewDto(int id, AppUser user)
    {
        Interview i = await GetInterview(id, user);
        return InterviewToDTO(i);
    }

    public InterviewDTO InterviewToDTO(Interview interview)
    {
        InterviewDTO interviewDTO = new InterviewDTO();
        if (interview.Questions != null)
        {
            var questions = questionService.ConvertToDtos(interview.Questions, interview);
            interviewDTO.questions = questions;
        }
        else
        {
            interviewDTO.questions = new List<QuestionPageDto>();
        }

        interviewDTO.id = interview.Id;
        interviewDTO.name = interview.Name;
        interviewDTO.resumeLink = interview.ResumeLink;
        interviewDTO.jobDescription = interview.JobDescription;
        interviewDTO.createdDate = interview.CreatedDate.ToShortDateString();
        interviewDTO.additionalDescription =
            interview.AdditionalDescription == null ? "" : interview.AdditionalDescription;
        interviewDTO.isLive = interview.IsLive;

        interviewDTO.secondsPerAnswer = interview.SecondsPerAnswer;
        return interviewDTO;
    }

    public Interview DtoToInterview(InterviewDTO interviewDTO)
    {
        Interview interview = new Interview();
        interview.Id = interviewDTO.id;
        interview.Name = interviewDTO.name;
        interview.JobDescription = interviewDTO.jobDescription;
        interview.ResumeLink = interviewDTO.resumeLink;
        interview.SecondsPerAnswer = interviewDTO.secondsPerAnswer;
        interview.AdditionalDescription = interviewDTO.additionalDescription;

        if (interviewDTO.questions != null)
        {
            interview.Questions = interviewDTO.questions
                .Select(x => questionRepository.ConvertQuestionToEntity(x)).ToList();
        }

        return interview;
    }

    public async Task<FileStream> ServeFile(string fileName, string filePath, string folderName,
        HttpContext httpContext)
    {
        if (AppConfig.UseCloudStorage)
        {
            await blobStorageService.DownloadFileAsync(fileName, filePath, folderName);
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);


        if (AppConfig.UseCloudStorage)
        {
            httpContext.Response.OnCompleted(async () =>
            {
                stream.Close();
                System.IO.File.Delete(filePath);
            });
        }

        return stream;
    }


    public async Task<ResumeUrlAndName> GetLatestResume(AppUser user)
    {
        string url = await interviewRepository.GetLatestResume(user);
        if (url == null)
        {
            return new ResumeUrlAndName
            {
                url = "",
                fileName = ""
            };
        }

        // get every after /Interview/getPdf/ in the url to get actual file name and not anything in the server endpoint to get it
        string searchPattern = "/Interview/getPdf/";
        // gives start index of pattern

        string filename = url.GetStringAfterPattern(searchPattern);
        if (AppConfig.UseSignedUrl)
        {
            url = await blobStorageService.GeneratePreSignedUrlAsync("resumes", filename);
        }

        // cut off guid part we added to file name now to
        // get the original name the user uploaded(we needed the guid part to fish it from storage)

        filename = filename.GetStringAfterPattern("_");

        return new ResumeUrlAndName
        {
            url = url,
            fileName = filename
        };
    }

    public async Task FormatResume(ResumeUrlAndName resume)
    {
        string searchPattern = "/Interview/getPdf/";
        // gives start index of pattern
        string filename = resume.url.GetStringAfterPattern(searchPattern);
        
        // convert url to signed url for viewing
        if (AppConfig.UseSignedUrl)
        {
            resume.url = await blobStorageService.GeneratePreSignedUrlAsync("resumes", filename);
        }

        // cut off guid part we added to file name now to
        // get the original name the user uploaded(we needed the guid part to fish it from storage)
        filename = filename.GetStringAfterPattern("_");
        resume.fileName = filename;
       
    }

    // return resumes url and names for a user
    public async Task<ResumeUrlAndName[]> GetAllResumes(AppUser user)
    {
        ResumeUrlAndName[] resumes = await interviewRepository.GetAllResumes(user);
        
        foreach (ResumeUrlAndName resume in resumes)
        {
            await FormatResume(resume);
        }
        return resumes;
    }

    public async Task<FeedbackAndTranscript> GetFeedbackAndMessages(AppUser user, int interviewId)
    {
         var feedbackAndMessages = await interviewRepository.GetFeedbackAndMessages(user, interviewId);
        FeedbackAndTranscript feedbackAndTranscript = new FeedbackAndTranscript();
        InterviewFeedbackDTO interviewFeedbackDTO = null;
        if (feedbackAndMessages.feedback != null)
        {
            interviewFeedbackDTO = new InterviewFeedbackDTO()
            {
                negativeFeedback = feedbackAndMessages.feedback.NegativeFeedback,
                positiveFeedback = feedbackAndMessages.feedback.PostiveFeedback,
                id = feedbackAndMessages.feedback.Id
            };
        }

        List<MessageDto> messageDtos = feedbackAndMessages.messages.Select(x => IMessageService.ConvertToMessageDto(x)).ToList();

        feedbackAndTranscript.feedback = interviewFeedbackDTO;
        feedbackAndTranscript.messages = messageDtos;
        feedbackAndTranscript.videoLink = feedbackAndMessages.videoLink;
        
        return feedbackAndTranscript;
    }
}