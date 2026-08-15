using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SearchPulse.DemoSite.Migrations;

internal sealed class CreateDemoDocumentTypesMigration(
    IMigrationContext context,
    IContentTypeService contentTypeService,
    IDataTypeService dataTypeService,
    ITemplateService templateService,
    IShortStringHelper shortStringHelper) : AsyncMigrationBase(context)
{
    protected override async Task MigrateAsync()
    {
        IDataType textbox = (await dataTypeService.GetByEditorAliasAsync("Umbraco.TextBox")).First();
        IDataType textarea = (await dataTypeService.GetByEditorAliasAsync("Umbraco.TextArea")).First();

        IContentType home = await CreateContentTypeAsync(DemoContentTree.HomeAlias, "SearchPulse Demo Home", true, textbox, textarea);
        IContentType page = await CreateContentTypeAsync(DemoContentTree.PageAlias, "SearchPulse Demo Page", false, textbox, textarea);

        var templateAttempt = await templateService.CreateForContentTypeAsync(
            "SearchPulse Demo Page",
            DemoContentTree.TemplateAlias,
            DemoContentTree.PageAlias,
            Constants.Security.SuperUserKey);
        if (!templateAttempt.Success)
        {
            throw new InvalidOperationException("Could not create the SearchPulse demo page template.");
        }

        ITemplate template = templateAttempt.Result ?? throw new InvalidOperationException("The SearchPulse demo page template was not returned.");
        home.AllowedContentTypes = [new ContentTypeSort(page.Key, 0, page.Alias)];
        home.AllowedTemplates = [template];
        page.AllowedTemplates = [template];
        home.SetDefaultTemplate(template);
        page.SetDefaultTemplate(template);

        await contentTypeService.UpdateAsync(home, Constants.Security.SuperUserKey);
        await contentTypeService.UpdateAsync(page, Constants.Security.SuperUserKey);
    }

    private async Task<IContentType> CreateContentTypeAsync(
        string alias,
        string name,
        bool allowedAsRoot,
        IDataType textbox,
        IDataType textarea)
    {
        var contentType = new ContentType(shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            Icon = "icon-document",
            AllowedAsRoot = allowedAsRoot,
            Variations = ContentVariation.Culture,
        };

        var properties = new PropertyTypeCollection(false)
        {
            CreateProperty(shortStringHelper, textbox, "heading", "Heading"),
            CreateProperty(shortStringHelper, textarea, "introduction", "Introduction"),
            CreateProperty(shortStringHelper, textbox, "actionName", "Tracked action name"),
            CreateProperty(shortStringHelper, textbox, "actionLabel", "Tracked action label"),
            CreateProperty(shortStringHelper, textarea, "detail", "Signal description"),
        };

        contentType.PropertyGroups.Add(new PropertyGroup(properties)
        {
            Name = "Content",
            Alias = "content",
            Type = PropertyGroupType.Group,
        });

        await contentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);
        return contentType;
    }

    private static PropertyType CreateProperty(
        IShortStringHelper shortStringHelper,
        IDataType dataType,
        string alias,
        string name) => new(shortStringHelper, dataType)
    {
        Alias = alias,
        Name = name,
        Mandatory = true,
        Variations = ContentVariation.Culture,
    };
}
