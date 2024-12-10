using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;

namespace API.FFMPEG
{
    public class FfmpegService
    {
        public FfmpegService()
        {
            GlobalFFOptions.Configure(options => options.BinaryFolder = @"C:\ProgramData\chocolatey\lib\ffmpeg\tools\ffmpeg\bin");

        }
        public static async Task<string> extractAudioFile(string videoPath)
        {
            string fileName = Guid.NewGuid().ToString() + ".wav";
            string wavPath = Path.Combine("Uploads", fileName);
            

            
         
            await FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(wavPath, true, options => options
                    .WithCustomArgument("-vn") 
                    .WithCustomArgument("-ar 16000") 
                    .WithCustomArgument("-ac 1") 
                    .WithCustomArgument("-f wav"))
                .ProcessAsynchronously();

            return wavPath;
        }
        
    }
    
}
