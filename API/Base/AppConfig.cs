public static class AppConfig
{
    public static bool UseCloudStorage { get; set; }
    public static bool UseSignedUrl { get;  set; }
    public static string FFMPEGPath { get; set; }
    

    public static void LoadConfiguration(IConfiguration configuration)
    {
        UseCloudStorage = configuration.GetValue<object>("UseCloudStorage").ToString().ToLower().Equals("true");
        UseSignedUrl = configuration.GetValue<object>("UseSignedUrl").ToString().ToLower().Equals("true");
        FFMPEGPath = configuration.GetValue<object>("FFMPEGPath").ToString();
    }
}