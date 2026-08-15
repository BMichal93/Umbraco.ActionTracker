using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SearchPulse.DemoSite.Migrations;

internal sealed class AssignDemoContentTemplatesMigration(
    IMigrationContext context,
    IContentService contentService,
    IContentTypeService contentTypeService,
    IContentPublishingService contentPublishingService) : AsyncMigrationBase(context)
{
    private static readonly CulturePublishScheduleModel[] PublishEnglish = [new() { Culture = "en-US" }];

    protected override async Task MigrateAsync()
    {
        IContentType homeType = contentTypeService.Get(DemoContentTree.HomeAlias)
            ?? throw new InvalidOperationException("The SearchPulse demo home type must exist before templates are assigned.");
        IContentType pageType = contentTypeService.Get(DemoContentTree.PageAlias)
            ?? throw new InvalidOperationException("The SearchPulse demo page type must exist before templates are assigned.");
        int templateId = pageType.DefaultTemplateId;

        foreach (IContent root in contentService.GetRootContent().Where(content => content.ContentType.Key == homeType.Key))
        {
            await AssignAndPublishAsync(root, templateId);

            long totalRecords;
            IEnumerable<IContent> children = contentService.GetPagedChildren(root.Id, 0, int.MaxValue, out totalRecords, null, null, null, true);
            foreach (IContent child in children.Where(content => content.ContentType.Key == pageType.Key))
            {
                await AssignAndPublishAsync(child, templateId);
            }
        }
    }

    private async Task AssignAndPublishAsync(IContent content, int templateId)
    {
        content.TemplateId = templateId;
        contentService.Save(content);
        await contentPublishingService.PublishAsync(content.Key, PublishEnglish, Constants.Security.SuperUserKey);
    }
}



