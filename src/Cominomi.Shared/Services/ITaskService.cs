using Cominomi.Shared.Models;

namespace Cominomi.Shared.Services;

public interface ITaskService
{
    Task<List<TaskItem>> GetBySessionAsync(string sessionId);
    Task<TaskItem> CreateAsync(string sessionId, string subject, string description = "");
    Task<TaskItem?> GetAsync(string taskId);
    Task UpdateStatusAsync(string taskId, Models.TaskStatus status);
    Task DeleteAsync(string taskId);
    Task<List<TaskItem>> GetAllAsync();
}
