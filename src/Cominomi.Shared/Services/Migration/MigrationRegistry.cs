namespace Cominomi.Shared.Services.Migration;

/// <summary>
/// 앱 시작 시 모든 모델의 스키마 버전과 마이그레이션 스텝 등록.
/// </summary>
public static class MigrationRegistry
{
    public static void Initialize()
    {
        // v1: 최초 버전 스탬핑 (v0 = schemaVersion 필드 없는 기존 파일)
        // 구조 변경 없음 — schemaVersion 필드만 추가
        JsonMigrator.Register("Session", currentVersion: 1);
        JsonMigrator.Register("Workspace", currentVersion: 1);
        JsonMigrator.Register("AppSettings", currentVersion: 1);
        JsonMigrator.Register("MemoryEntry", currentVersion: 1);
        JsonMigrator.Register("TaskItem", currentVersion: 1);

        // 향후 마이그레이션 예시:
        // JsonMigrator.Register("Session", currentVersion: 2,
        //     (1, node => { node["newField"] = "defaultValue"; }));
    }
}
