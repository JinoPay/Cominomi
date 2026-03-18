using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cominomi.Shared.Services.Migration;

/// <summary>
/// JSON 스키마 마이그레이션 엔진.
/// 역직렬화 전 raw JSON 레벨에서 버전 감지 및 순차 마이그레이션 적용.
/// </summary>
public static class JsonMigrator
{
    private static readonly Dictionary<string, int> CurrentVersions = new();
    private static readonly Dictionary<string, SortedList<int, Action<JsonObject>>> Migrations = new();

    /// <summary>
    /// 모델 타입의 현재 버전과 마이그레이션 스텝을 등록.
    /// </summary>
    /// <param name="modelKey">모델 식별자 (예: "Session", "Workspace")</param>
    /// <param name="currentVersion">현재 스키마 버전</param>
    /// <param name="migrations">
    /// (fromVersion, migrate) 튜플 목록.
    /// fromVersion N → N+1 변환 로직.
    /// </param>
    public static void Register(string modelKey, int currentVersion,
        params (int fromVersion, Action<JsonObject> migrate)[] migrations)
    {
        CurrentVersions[modelKey] = currentVersion;
        var steps = new SortedList<int, Action<JsonObject>>();
        foreach (var (fromVersion, migrate) in migrations)
            steps[fromVersion] = migrate;
        Migrations[modelKey] = steps;
    }

    /// <summary>
    /// raw JSON 문자열에 마이그레이션 적용.
    /// </summary>
    /// <returns>(마이그레이션된 JSON, 변경 여부)</returns>
    public static (string Json, bool Changed) MigrateJson(string modelKey, string rawJson)
    {
        if (!CurrentVersions.TryGetValue(modelKey, out var targetVersion))
            return (rawJson, false);

        JsonObject? obj;
        try
        {
            obj = JsonNode.Parse(rawJson)?.AsObject();
        }
        catch
        {
            return (rawJson, false);
        }

        if (obj == null)
            return (rawJson, false);

        var fileVersion = 0;
        if (obj.TryGetPropertyValue("schemaVersion", out var versionNode) &&
            versionNode is JsonValue val && val.TryGetValue<int>(out var v))
        {
            fileVersion = v;
        }

        if (fileVersion >= targetVersion)
            return (rawJson, false);

        // 순차 마이그레이션 적용
        if (Migrations.TryGetValue(modelKey, out var steps))
        {
            foreach (var (fromVersion, migrate) in steps)
            {
                if (fromVersion >= fileVersion && fromVersion < targetVersion)
                    migrate(obj);
            }
        }

        obj["schemaVersion"] = targetVersion;

        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = obj.ToJsonString(options);
        return (result, true);
    }

    public static int GetCurrentVersion(string modelKey)
        => CurrentVersions.TryGetValue(modelKey, out var v) ? v : 0;
}
