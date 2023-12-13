namespace AppsMarketplaceWebApi
{
    public class ConfigurationValues(string pathToDefaultAppPic, string pathToImagesDir)
    {
        public string PathToDefaultAppPic { get; } = pathToDefaultAppPic;

        public string PathToImagesDir { get; } = pathToImagesDir;
    }
}
