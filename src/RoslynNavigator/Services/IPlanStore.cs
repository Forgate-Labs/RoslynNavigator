using RoslynNavigator.Models;

namespace RoslynNavigator.Services;

public interface IPlanStore
{
    Task<PlanState> LoadAsync();
    Task SaveAsync(PlanState state);
    Task ClearAsync();
}
