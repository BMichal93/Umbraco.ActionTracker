using Umbraco.Cms.Core.Packaging;
using SearchPulse.DemoSite.Migrations;

namespace SearchPulse.DemoSite;

/// <summary>
/// Seeds the disposable demo only once through Umbraco's package migration state.
/// </summary>
public sealed class DemoContentSeedPlan : PackageMigrationPlan
{
    public DemoContentSeedPlan()
        : base("SearchPulse.DemoSite.ContentSeed")
    {
    }

    protected override void DefinePlan()
    {
        From(InitialState)
            .To<CreateDemoDocumentTypesMigration>("searchpulse-demo-document-types-created")
            .To<CreateDemoContentMigration>("searchpulse-demo-content-created")
            .To<AssignDemoContentTemplatesMigration>("searchpulse-demo-content-templates-assigned")
            .To<ConfigureDemoTemplateRoutingMigration>("searchpulse-demo-template-routing-configured")
            .To<ConfigureDemoRootDomainMigration>("searchpulse-demo-root-domain-configured");
    }
}
