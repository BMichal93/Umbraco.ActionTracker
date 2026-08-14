using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace SearchPulse.DemoSite;

/// <summary>
/// Gives the local demo administrator access to the SearchPulse section.
/// </summary>
public sealed class DemoAdministratorSectionAccess(
    IUserGroupService userGroupService,
    IRuntimeState runtimeState) : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    public async Task HandleAsync(
        UmbracoApplicationStartingNotification notification,
        CancellationToken cancellationToken)
    {
        if (runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        var groups = await userGroupService.GetAllAsync(0, 50);
        var administratorGroup = groups.Items.FirstOrDefault(group => group.Alias == "admin");
        if (administratorGroup is null || administratorGroup.AllowedSections.Contains("SearchPulse.Section"))
        {
            return;
        }

        administratorGroup.AddAllowedSection("SearchPulse.Section");
        await userGroupService.UpdateAsync(administratorGroup, Constants.Security.SuperUserKey);
    }
}