using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SearchPulse.DemoSite.Migrations;

internal sealed class CreateDemoContentMigration(
    IMigrationContext context,
    IContentService contentService,
    IContentTypeService contentTypeService,
    IContentPublishingService contentPublishingService) : AsyncMigrationBase(context)
{
    private const string DemoCulture = "en-US";
    private static readonly CulturePublishScheduleModel[] PublishEnglish = [new() { Culture = DemoCulture }];

    protected override async Task MigrateAsync()
    {
        var contentTypes = new Dictionary<string, IContentType>
        {
            [DemoContentTree.HomeAlias] = contentTypeService.Get(DemoContentTree.HomeAlias)
                ?? throw new InvalidOperationException("The SearchPulse demo home type must be created before content."),
            [DemoContentTree.PageAlias] = contentTypeService.Get(DemoContentTree.PageAlias)
                ?? throw new InvalidOperationException("The SearchPulse demo page type must be created before content."),
        };
        var idsByName = new Dictionary<string, int>();

        foreach (DemoContentNode node in DemoContentTree.Nodes)
        {
            int parentId = node.ParentName is null ? Constants.System.Root : idsByName[node.ParentName];
            IContentType contentType = contentTypes[node.ContentTypeAlias];
            IContent content = contentService.Create(node.Name, parentId, contentType);
            content.TemplateId = contentType.DefaultTemplateId;
            content.SetCultureName(node.Name, DemoCulture);
            content.SetValue("heading", node.Heading, DemoCulture);
            content.SetValue("introduction", node.Introduction, DemoCulture);
            content.SetValue("actionName", node.ActionName, DemoCulture);
            content.SetValue("actionLabel", node.ActionLabel, DemoCulture);
            content.SetValue("detail", node.Detail, DemoCulture);
            contentService.Save(content);

            await contentPublishingService.PublishAsync(content.Key, PublishEnglish, Constants.Security.SuperUserKey);
            idsByName[node.Name] = content.Id;
        }
    }
}
