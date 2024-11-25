using FFMpegCore;
namespace API.FFMPEG
{
    public class FfmpegService
    {
        public FfmpegService()
        {
            GlobalFFOptions.Configure(options => options.BinaryFolder = @"C:\ProgramData\chocolatey\lib\ffmpeg\tools\ffmpeg\bin");

        }
        public static string extractAudioFile(string videoPath)
        {
            string audioPath = "./extractedAudio.mp3";
            audioPath = Path.GetFullPath(audioPath);
            FFMpeg.ExtractAudio(videoPath, audioPath);
            return audioPath;
        }
    }
    
}
