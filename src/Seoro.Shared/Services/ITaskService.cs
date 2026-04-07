using Seoro.Shared.Models;
using TaskStatus = Seoro.Shared.Models.TaskStatus;

namespace Seoro.Shared.Services;

public interface ITaskService
{
    Task DeleteAsync(string taskId);
    Task UpdateStatusAsync(string taskId, TaskStatus status);
    Task<List<TaskItem>> GetAllAsync();
    Task<List<TaskItem>> GetBySessionAsync(string sessionId);
    Task<TaskItem?> GetAsync(string taskId);
    Task<TaskItem> CreateAsync(string sessionId, string subject, string description = "");
}