using Amazon;
using Amazon.Internal;
using Amazon.S3;
using Amazon.S3;
using Amazon.S3.Model;
namespace API.AWS;

public class S3Service: IBlobStorageService
{
    private readonly IAmazonS3 _amazonS3;
    private readonly string _bucketName;

    public S3Service(IConfiguration config)
    {
        var awsSettings  = config.GetSection("AWS").Get<AwsSettings>();
        var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey);
        _amazonS3 = new AmazonS3Client(awsCredentials,RegionEndpoint.GetBySystemName(awsSettings.Region));
        _bucketName = awsSettings.BucketName;
    }

    public async Task DownloadFileAsync(string fileName,string downloadPath, string folderName)
    {
        try
        {
            string keyName = $"{folderName}/{fileName}";
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = keyName
            };
            // using to clean up streams/network requests once they go out of scope
            using var getResponse = await _amazonS3.GetObjectAsync(getRequest) ;
            using var responseStream = getResponse.ResponseStream;
            using var fileStream = new FileStream(downloadPath, FileMode.Create);
            await responseStream.CopyToAsync(fileStream);
            
        }
        
        catch (AmazonS3Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task<string> UploadFileAsync(string filePath, string fileName, string folderName)
    {
        try
        {
            string keyName = $"{folderName}/{fileName}";  
            var putRequest = new PutObjectRequest { BucketName = _bucketName, Key = keyName, InputStream = new FileStream(filePath, FileMode.Open) };
            var response = await _amazonS3.PutObjectAsync(putRequest);
            return keyName;
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine(e.Message);
        }
        return null;
    }

    public async Task<string> UploadFileDeleteAsync(string filePath, string fileName,string folderName)
    {
       
        string cloudKey = await UploadFileAsync(filePath, fileName,folderName);
        File.Delete(filePath);
        return cloudKey;
    }
}