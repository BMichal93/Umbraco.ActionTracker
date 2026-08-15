using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SearchPulse.Umbraco.Goals;

public sealed class SearchPulseGoalService(IScopeProvider scopeProvider) : ISearchPulseGoalService
{
    public IReadOnlyList<SearchPulseGoalDto> GetGoals(bool includeDisabled = true)
    {
        using var scope = scopeProvider.CreateScope();
        var query = includeDisabled ? $"SELECT * FROM {SearchPulseGoalDto.TableName} ORDER BY name" : $"SELECT * FROM {SearchPulseGoalDto.TableName} WHERE isEnabled = @0 ORDER BY name";
        var goals = includeDisabled
            ? scope.Database.Fetch<SearchPulseGoalDto>(query)
            : scope.Database.Fetch<SearchPulseGoalDto>(query, true);
        scope.Complete();
        return goals;
    }

    public SearchPulseGoalDto Create(string name, SearchPulseEventType eventType, string target)
    {
        var goal = new SearchPulseGoalDto
        {
            Name = name,
            EventType = eventType.ToString(),
            Target = target,
            IsEnabled = true,
            CreatedUtc = DateTime.UtcNow,
        };
        using var scope = scopeProvider.CreateScope();
        scope.Database.Insert(goal);
        scope.Complete();
        return goal;
    }

    public bool Update(long id, string name, SearchPulseEventType eventType, string target, bool isEnabled)
    {
        using var scope = scopeProvider.CreateScope();
        var updated = scope.Database.Execute(
            $"UPDATE {SearchPulseGoalDto.TableName} SET name = @0, eventType = @1, target = @2, isEnabled = @3 WHERE id = @4",
            name,
            eventType.ToString(),
            target,
            isEnabled,
            id);
        scope.Complete();
        return updated == 1;
    }

    public bool Delete(long id)
    {
        using var scope = scopeProvider.CreateScope();
        var deleted = scope.Database.Execute($"DELETE FROM {SearchPulseGoalDto.TableName} WHERE id = @0", id);
        scope.Complete();
        return deleted == 1;
    }
}
