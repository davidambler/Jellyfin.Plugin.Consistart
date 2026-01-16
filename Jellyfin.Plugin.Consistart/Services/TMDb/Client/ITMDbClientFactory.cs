namespace Jellyfin.Plugin.Consistart.Services.TMDb.Client;

internal interface ITMDbClientFactory
{
    ITMDbClientAdapter CreateClient();
}
