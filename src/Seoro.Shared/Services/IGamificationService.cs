using Seoro.Shared.Models;

namespace Seoro.Shared.Services;

public interface IGamificationService
{
    Task<DashboardStats> ForceRefreshDashboardAsync();
    Task<DashboardStats> GetDashboardStatsAsync();
}