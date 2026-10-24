namespace TodoApi.Infrastructure.Persistence.Auditing;

public enum AuditAction { Updated = 1, Deleted = 2 }

public sealed class AuditRecord
{
    private AuditRecord() { }

    internal AuditRecord(string entityType, int entityId, AuditAction action, int? actorUserId, DateTime occurredAtUtc)
    {
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        ActorUserId = actorUserId;
        OccurredAtUtc = occurredAtUtc;
    }

    public int Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public int EntityId { get; private set; }
    public AuditAction Action { get; private set; }
    public int? ActorUserId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
}
