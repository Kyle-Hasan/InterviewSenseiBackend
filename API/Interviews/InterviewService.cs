using API.AI;
using API.Questions;
using API.Users;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace API.Interviews;

public class InterviewService(IOpenAIService openAiService,IinterviewRepository interviewRepository,IQuestionRepository questionRepository): IinterviewService
{
    private readonly string _splitToken = "@u5W$";
    public async Task<QuestionDTO> rateAnswer(string question, int questionId, string videoPath,AppUser user)
    {
        string transcript =  await openAiService.GetTranscript(videoPath);

        string formatInstruction =
            $"Response should always be in format '{_splitToken} Good: insert your answer here {_splitToken} Needs Improvement: insert your answer here' Absolutely DO NOT forget the ${_splitToken} or this format or else the program breaks.   ";
        
        string prompt =
            $"Imagine you are an interviewer who is a complete perfectionist for a company and you are giving a candidate brutally honest feedback about what they could have improved, write like you are talking to them face-to-face. Keep STAR method in mind as well and tone. I want highly critical and specific feedback. Avoid general or vague statements. If transcript is nonsensical or empty just mention 'no strengths shown...' in good and 'Incomplete answer...' in Needs Improvement " +
            $"Use a blunt tone and be as strict as possible.Avoid sugarcoating or fake compliments or feeling like you have to give praise, if its bad don't praise at all. Be very very cynical and give no benefit of the doubt. Make it very hard for the candidate to get good feedback from you, maintain very high standards. Anything that sounds vague or unclear assume the worst. Don't force yourself to find strong points, only consider them strong points if they make you think this person is qualified for the job, its ok to only have 'You showed no strengths in your response' Separate all sentences using 3 periods always " +
            $" , given this question " +
            $"${question} say what is good and what needs improvement about this answer ${transcript} give concise and specific feedback that's easy to understand, quick to read and work on immediately, provide examples of what they could've done instead if applicable. "
            + $"${formatInstruction} ";
        
        string response = await openAiService.MakeRequest(prompt);
        string[] split = response.Split(_splitToken);
        var retval = await questionRepository.updateAnswer(questionId, transcript, response, videoPath,user);
        /*var retval =  new RatingResponse
        {
            good = split[1].Split("Good:")[1],
            bad = split[2].Split("Needs Improvement:")[1],
        };*/
        return QuestionRepository.convertQuestionToDTO(retval);
    }

    private async Task<string> GetPdfTranscriptAsync(string pdfPath)
    {
        return await Task.Run(() =>
        {
            string result = "";
            using(PdfReader reader = new PdfReader(pdfPath))
            using (PdfDocument pdfDoc = new PdfDocument(reader))
            {
                for (int i = 1; i < pdfDoc.GetNumberOfPages(); i++)
                {
                    result += PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i));
                }
            }
            return result;  
        });
    }

    public async Task<GenerateQuestionsResponse> generateQuestions(string jobDescription,int numberOfBehavioral, int numberOfTechnical, string resumePdfPath )
    {
        string resume = "";

        if (resumePdfPath != null)
        {
            resume = await GetPdfTranscriptAsync(resumePdfPath);
        }
        
        string prompt = $@"
        You are an AI specialized in creating highly relevant interview questions.

        Job Description:
        {jobDescription}

        Candidate Resume:
        {resume}

        Generate {numberOfBehavioral} behavioral interview questions that evaluate soft skills and cultural fit.
        Generate {numberOfTechnical} technical interview questions that assess job-specific technical expertise.

        Follow these guidelines:

1. **Behavioral Questions**:
   - Use the candidate's past work experience and projects as the foundation for the questions.
   - Focus on soft skills like teamwork, adaptability, problem-solving, and leadership.
   - Questions must strictly align with the candidate's experience or the job description.

2. **Technical Questions**:
   - Pull all technical questions strictly from the job description. 
   - Prioritize skills mentioned in both the job description and the resume.
   - If the job description does not include specific technical skills, pull from the resume alone.
   - If no job description or resume is availa*ble, generate generic technical questions relevant to the specified role (e.g., Full Stack Developer).
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
        
        string response = await openAiService.MakeRequest(prompt);
        string[] sections = response.Split(new string[] {"Behavioral Questions:", "Technical Questions:" },StringSplitOptions.RemoveEmptyEntries);
        string[] behavioralQuestions = sections[0].Split("\n").Skip(1).Where(q=> q.Length >= 4).Select(q => q.Trim().Substring(3)).ToArray();
        string [] technicalQuestions = sections[1].Split("\n").Skip(1).Where(q=> q.Length >= 4).Select(q => q.Trim().Substring(3)).ToArray();
        return new GenerateQuestionsResponse
        {
            behavioralQuestions = behavioralQuestions,
            technicalQuestions = technicalQuestions,
        };
    }

    public async Task<Interview> GenerateInterview(AppUser user, string interviewName,string jobDescription,int numberOfBehavioral, int numberOfTechnical, string resumePdfPath)
    {
        Interview interview = new Interview();
        var questions = await generateQuestions(jobDescription, numberOfBehavioral, numberOfTechnical, resumePdfPath);
        var technicalQuestions =
            questions.technicalQuestions.Select(x => QuestionRepository.createQuestionFromString(x,"technical")).ToList();
        var behavioralQuestions = questions.behavioralQuestions
            .Select(x => QuestionRepository.createQuestionFromString(x,"behavioral")).ToList();
        var questionList = new List<Question>();
        questionList.AddRange(technicalQuestions);
        questionList.AddRange(behavioralQuestions);
        
        interview.Name = interviewName;
        interview.Questions = questionList;
        interview.JobDescription = jobDescription;
        interview.ResumeLink = resumePdfPath;
        
        var i = await createInterview(interview, user);
        return i;


    }

    public async Task  deleteInterview(Interview interview, AppUser user)
    {
       await interviewRepository.Delete(interview, user);
    }

    public async Task<Interview> updateInterview(Interview interview, AppUser user)
    {
        Interview oldInterview = await getInterview(interview.Id, user);
        Interview toUpdated = interviewRepository.GetChangedInterview(interview, oldInterview);
        return await interviewRepository.Save(toUpdated, user);
    }

    public async Task<Interview> createInterview(Interview interview, AppUser user)
    {
       return await interviewRepository.Save(interview, user);
    }

    public async Task<List<InterviewDTO>> getInterviews(AppUser user)
    {
        List<Interview> interviews = await interviewRepository.GetInterviews(user);
        return interviews.Select(x=> interviewToDTO(x)).ToList();
        
    }

    public async Task<Interview> getInterview(int id,AppUser user)
    {
        Interview i = await interviewRepository.GetInterview(user,id);
        return i;
    }

    public async Task<bool> verifyVideoView(string fileName, AppUser user)
    {
        return await questionRepository.verifyVideoView(fileName, user);
    }

    public async Task<InterviewDTO> getInterviewDto(int id,AppUser user)
    {
        Interview i = await getInterview(id,user);
        return interviewToDTO(i);
    }

    public static InterviewDTO interviewToDTO(Interview interview)
    {
        InterviewDTO interviewDTO = new InterviewDTO();
        if (interview.Questions != null)
        {
            List<QuestionDTO> questionDTOs =
                interview.Questions.Select(x => QuestionRepository.convertQuestionToDTO(x)).ToList();
            interviewDTO.questions = questionDTOs;
        }
        else
        {
            interviewDTO.questions = new List<QuestionDTO>();
        }

        interviewDTO.id = interview.Id;
        interviewDTO.name = interview.Name;
        interviewDTO.resumeLink = interview.ResumeLink;
        interviewDTO.jobDescription = interview.JobDescription;
        return interviewDTO;
        
    }

    public static Interview DtoToInterview(InterviewDTO interviewDTO)
    {
        Interview interview = new Interview();
        interview.Id = interviewDTO.id;
        interview.Name = interviewDTO.name;
        interview.JobDescription = interviewDTO.jobDescription;
        interview.ResumeLink = interviewDTO.resumeLink;

        if (interviewDTO.questions != null)
        {

            interview.Questions = interviewDTO.questions
                .Select(x => QuestionRepository.convertQuestionToEntity(x)).ToList();
        }
        
        

        return interview;
    }
    
    
}