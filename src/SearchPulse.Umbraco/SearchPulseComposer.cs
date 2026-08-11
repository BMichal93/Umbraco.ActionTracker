using SearchPulse.Umbraco.DependencyInjection;
using SearchPulse.Umbraco.Migrations;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace SearchPulse.Umbraco;

public sealed class SearchPulseComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSearchPulse();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, SearchPulseMigrationRunner>();
    }
}
