using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SearchPulse.DemoSite.Migrations;

internal sealed class ConfigureDemoRootDomainMigration(
    IMigrationContext context,
    IContentService contentService,
    IContentTypeService contentTypeService,
    IDomainService domainService) : AsyncMigrationBase(context)
{
    protected override async Task MigrateAsync()
    {
        IContentType homeType = contentTypeService.Get(DemoContentTree.HomeAlias)
            ?? throw new InvalidOperationException("The SearchPulse demo home type must exist before its root domain is configured.");
        IContent home = contentService.GetRootContent().Single(content => content.ContentType.Key == homeType.Key);
        var result = await domainService.UpdateDomainsAsync(home.Key, new DomainsUpdateModel
        {
            DefaultIsoCode = "en-US",
            Domains = [new DomainModel { DomainName = "localhost", IsoCode = "en-US" }],
        });

        if (!result.Success)
        {
            throw new InvalidOperationException("Could not assign the localhost domain to the SearchPulse demo root.");
        }
    }
}
