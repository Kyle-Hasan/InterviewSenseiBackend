public static class AppConfig
{
    public static bool UseCloudStorage { get; private set; }

    public static void LoadConfiguration(IConfiguration configuration)
    {
        UseCloudStorage = (bool)configuration.GetValue<object>("UseCloudStorage");
    }
}