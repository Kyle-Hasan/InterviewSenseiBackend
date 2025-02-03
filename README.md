# Interview Sensei Backend  

This repository contains the backend for **Interview Sensei**, an AI-powered interview preparation application. The backend is built with **.NET**, uses **PostgreSQL** for data storage, and is deployed on **AWS** using **EC2, RDS, and S3**. It provides API endpoints for managing users, processing AI-generated questions, storing interview recordings, and delivering feedback.

## Features  
- **AI-powered question generation** based on user-uploaded resumes and job descriptions  
- **User authentication & session management**  
- **Interview recording storage** using AWS S3  
- **AI-generated feedback** on recorded answers  
- **Scalable backend architecture** with Dockerized deployment  
- **PostgreSQL database** hosted on AWS RDS  

## Tech Stack  
- **Backend Framework:** .NET  
- **Database:** PostgreSQL (AWS RDS)  
- **Storage:** AWS S3 (for interview recordings)  
- **Deployment:** AWS EC2, Docker  
- **Authentication:** JWT  

## Setup  

### Prerequisites  
- .NET SDK installed  
- PostgreSQL installed (or an RDS instance)  
- AWS account for cloud storage  
- Docker (if running in a containerized environment)  

### Installation  
1. Clone the repository:  
   ```sh
   git clone https://github.com/YOUR_GITHUB_USERNAME/interview-sensei-backend.git
   cd interview-sensei-backend
   ```

2. Set up environment variables:  
   Create a `.env` file with the following variables:  
   ```env
   DATABASE_URL=your_postgres_connection_string
   AWS_ACCESS_KEY_ID=your_aws_access_key
   AWS_SECRET_ACCESS_KEY=your_aws_secret_key
   AWS_S3_BUCKET_NAME=your_s3_bucket_name
   JWT_SECRET=your_jwt_secret
   ```

3. Run the application:  
   ```sh
   dotnet run
   ```

4. If using Docker:  
   ```sh
   docker build -t interview-sensei-backend .
   docker run -p 5000:5000 interview-sensei-backend
   ```

## API Endpoints  

### Authentication  
- `POST /api/Auth/register` – Register a new user  
- `POST /api/auth/login` – Log in and receive a JWT
-  `GET /api/auth/refreshToken` – Refresh JWT
-  `GET /api/auth/logout` – Logout 

### Interviews  
- `POST /api/Interview/generateInterview` – Create a new interview session 
- `GET /api/Interview/interviewList` – Get paged interviews for user
-  `GET /api/Interview/{id}` – Get specific interview
-  `PUT /api/Interview` – Edit interview
-  `DELETE /api/Interview` – Delete interview
-  `GET /api/Interview/getPdf/{filename}` – Get specific resume
-  `GET /api/Interview/getVideo/{filename}` – Get specific video
-  `GET /api/Interview/getAllResumes` – Get all resumes for user
-  `GET /api/Interview/latestResume` – Get latest resume for user

### Questions
-  `GET /api/Question/{questionId}` – Get specific question
-  `GET /api/Question/byInterviews?interviewId ` – Get all questions for an interview

### Responses
-  `POST /api/Response/rateAnswer` – Rate a response to a question and send back feedback
-  `GET /api/Response/byQuestion?interviewId ` – All responses for a question



## Deployment  

### AWS Services Used  
- **EC2** – Hosting the backend application  
- **RDS** – PostgreSQL database storage  
- **S3** – Storing interview recordings  

### Deployment Steps  
1. **Build the Docker image**  
   ```sh
   docker build -t interview-sensei-backend .
   ```
2. **Push the image to AWS (ECR or EC2)**  
   ```sh
   docker tag interview-sensei-backend your-aws-repo-url
   docker push your-aws-repo-url
   ```
3. **Run on EC2**  
   ```sh
   docker run -d -p 5000:5000 interview-sensei-backend
   ```

## Notes  
- Since the backend is hosted on AWS **free tier**, the application may occasionally be down due to resource limits.  
- If the link is unavailable, please refer to the provided video for an overview of the application's functionality.  

## License  
This project is licensed under the MIT License.  

---
