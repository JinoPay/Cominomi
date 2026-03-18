using System.Text.Json;
using System.Text.Json.Nodes;
using Cominomi.Shared.Services.Migration;

namespace Cominomi.Shared.Tests;

public class JsonMigratorTests
{
    public JsonMigratorTests()
    {
        // 각 테스트 전 레지스트리 초기화
        MigrationRegistry.Initialize();
    }

    [Fact]
    public void MigrateJson_NoSchemaVersion_StampsVersion()
    {
        var json = """{"id": "abc", "name": "test"}""";

        var (result, changed) = JsonMigrator.MigrateJson("Workspace", json);

        Assert.True(changed);
        var obj = JsonNode.Parse(result)!.AsObject();
        Assert.Equal(1, obj["schemaVersion"]!.GetValue<int>());
    }

    [Fact]
    public void MigrateJson_AlreadyCurrentVersion_NoChange()
    {
        var json = """{"schemaVersion": 1, "id": "abc"}""";

        var (result, changed) = JsonMigrator.MigrateJson("Workspace", json);

        Assert.False(changed);
        Assert.Equal(json, result);
    }

    [Fact]
    public void MigrateJson_UnknownModelKey_NoChange()
    {
        var json = """{"id": "abc"}""";

        var (result, changed) = JsonMigrator.MigrateJson("UnknownModel", json);

        Assert.False(changed);
        Assert.Equal(json, result);
    }

    [Fact]
    public void MigrateJson_InvalidJson_ReturnsUnchanged()
    {
        var json = "not valid json {{{";

        var (result, changed) = JsonMigrator.MigrateJson("Workspace", json);

        Assert.False(changed);
        Assert.Equal(json, result);
    }

    [Fact]
    public void MigrateJson_SequentialMigrations_Applied()
    {
        // 커스텀 모델로 v0→v1→v2 순차 마이그레이션 테스트
        JsonMigrator.Register("TestModel", currentVersion: 3,
            (0, node => { node["addedInV1"] = "hello"; }),
            (1, node => { node["addedInV2"] = 42; }),
            (2, node => { node["addedInV3"] = true; })
        );

        var json = """{"name": "test"}""";

        var (result, changed) = JsonMigrator.MigrateJson("TestModel", json);

        Assert.True(changed);
        var obj = JsonNode.Parse(result)!.AsObject();
        Assert.Equal(3, obj["schemaVersion"]!.GetValue<int>());
        Assert.Equal("hello", obj["addedInV1"]!.GetValue<string>());
        Assert.Equal(42, obj["addedInV2"]!.GetValue<int>());
        Assert.True(obj["addedInV3"]!.GetValue<bool>());
    }

    [Fact]
    public void MigrateJson_PartialMigration_OnlyAppliesNeeded()
    {
        JsonMigrator.Register("PartialModel", currentVersion: 3,
            (0, node => { node["v1Field"] = "a"; }),
            (1, node => { node["v2Field"] = "b"; }),
            (2, node => { node["v3Field"] = "c"; })
        );

        // 이미 v2인 파일 → v2→v3 마이그레이션만 적용
        var json = """{"schemaVersion": 2, "name": "test", "v1Field": "a", "v2Field": "b"}""";

        var (result, changed) = JsonMigrator.MigrateJson("PartialModel", json);

        Assert.True(changed);
        var obj = JsonNode.Parse(result)!.AsObject();
        Assert.Equal(3, obj["schemaVersion"]!.GetValue<int>());
        Assert.Equal("c", obj["v3Field"]!.GetValue<string>());
    }

    [Fact]
    public void MigrateJson_PreservesExistingProperties()
    {
        var json = """{"id": "abc-123", "name": "my workspace", "status": "Ready"}""";

        var (result, changed) = JsonMigrator.MigrateJson("Workspace", json);

        Assert.True(changed);
        var obj = JsonNode.Parse(result)!.AsObject();
        Assert.Equal("abc-123", obj["id"]!.GetValue<string>());
        Assert.Equal("my workspace", obj["name"]!.GetValue<string>());
        Assert.Equal("Ready", obj["status"]!.GetValue<string>());
    }

    [Fact]
    public void GetCurrentVersion_ReturnsRegisteredVersion()
    {
        Assert.Equal(1, JsonMigrator.GetCurrentVersion("Session"));
        Assert.Equal(1, JsonMigrator.GetCurrentVersion("Workspace"));
        Assert.Equal(0, JsonMigrator.GetCurrentVersion("NonExistent"));
    }
}
