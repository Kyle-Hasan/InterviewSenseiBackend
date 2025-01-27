public static class AppConfig
{
    public static bool UseCloudStorage { get; private set; }
    public static bool UseSignedUrl { get; private set; }
    public static string FFMPEGPath { get; private set; }
    

    public static void LoadConfiguration(IConfiguration configuration)
    {
        UseCloudStorage = configuration.GetValue<object>("UseCloudStorage").ToString().ToLower().Equals(bool.TrueString.ToLower());
        UseSignedUrl = (bool)configuration.GetValue<object>("UseSignedUrl").ToString().ToLower().Equals(bool.TrueString.ToLower());
        FFMPEGPath = configuration.GetValue<object>("FFMPEGPath").ToString();
    }
}