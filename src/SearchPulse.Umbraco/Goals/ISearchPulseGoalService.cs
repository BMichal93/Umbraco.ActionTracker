using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.Goals;

public interface ISearchPulseGoalService
{
    IReadOnlyList<SearchPulseGoalDto> GetGoals(bool includeDisabled = true);

    SearchPulseGoalDto Create(string name, SearchPulseEventType eventType, string target);

    bool Update(long id, string name, SearchPulseEventType eventType, string target, bool isEnabled);

    bool Delete(long id);
}
