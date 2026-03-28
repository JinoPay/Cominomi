using Cominomi.Shared.Models;

namespace Cominomi.Shared.Services;

public interface ISessionReplayService
{
    Task<List<SessionReplaySummary>> ListSessionsAsync();
    Task<(List<SessionReplaySummary> Sessions, int Total, bool HasMore)> ListSessionsPagedAsync(int limit, int offset);
    Task<List<SessionReplayEvent>> LoadEventsAsync(string filePath, int skip = 0, int take = 100);
    Task<List<SessionSearchResult>> SearchSessionsAsync(string query, int maxResults = 20);
    Task<SessionTagStore> GetTagsAsync();
    Task SetTagAsync(string sessionId, List<string> tags);
    Task<string> ExportMarkdownAsync(string filePath);
    Task<List<LiveSessionInfo>> DetectLiveSessionsAsync();
}
