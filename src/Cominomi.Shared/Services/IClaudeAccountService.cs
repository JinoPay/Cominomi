using Cominomi.Shared.Models;

namespace Cominomi.Shared.Services;

public interface IClaudeAccountService
{
    event Action? OnAccountsChanged;

    Task<List<ClaudeAccount>> GetAccountsAsync();
    Task<ClaudeAccount?> GetActiveAccountAsync();

    /// <summary>Reads the current Claude CLI auth state and registers it as a new account.</summary>
    Task<ClaudeAccount> RegisterCurrentAsync(string profileName);

    Task RemoveAccountAsync(string accountId);
    Task UpdateProfileNameAsync(string accountId, string newName);

    /// <summary>Switches the active Claude CLI credentials to the specified account. Returns false on failure.</summary>
    Task<bool> SwitchToAsync(string accountId);

    /// <summary>Returns false when a streaming session is active (switch would be unsafe).</summary>
    bool CanSwitch();

    /// <summary>Fetches API usage for an account. Returns null on any error.</summary>
    Task<AccountUsageInfo?> FetchUsageAsync(string accountId);

    /// <summary>
    ///     Compares the live ~/.claude/ state against stored accounts and updates IsActive flags.
    ///     Call on page entry to catch external logins / logouts.
    /// </summary>
    Task SyncActiveAccountAsync();

    /// <summary>계정의 백업 데이터를 읽어 내보내기 봉투를 구성합니다. 백업 없으면 예외.</summary>
    Task<AccountExportEnvelope> PrepareExportAsync(string accountId);

    /// <summary>가져오기 봉투로 계정을 등록합니다. 새 GUID로 저장.</summary>
    Task<ClaudeAccount> ImportAccountAsync(AccountExportEnvelope envelope, string profileName);
}
