namespace API.AWS;

public interface IBlobStorageService
{
    /*
     *  file name: file name
     *  downloadPath : where you want the file to be stored
     * folderName: name of folder where file is stored
     */
    Task DownloadFileAsync(string fileName, string downloadPath, string folderName);
    /* returns: string with the identifier the cloud service needs to get this file again ( for now lets just assume this is always the video name),
     * so its redundant to return it but just in case something changes
     * filePath: where the file currently is on the system
     *  fileName: name you want the file to stored with on the cloud
     * folderName: name of folder where file is stored
     */
    Task<string> UploadFileAsync(string filePath, string fileName, string folderName);
    /* returns: string with the identifier the cloud service needs to get this file again ( for now lets just assume this is always the video name),
     * so its redundant to return it but just in case something changes
     * Also deletes the file path when its done downloading
     * filePath: where the file currently is on the system
     *  fileName: name you want the file to stored with on the cloud
     * folderName: name of folder of where to store file
     */
    Task<string> UploadFileDeleteAsync(string filePath, string fileName , string folderName);
}