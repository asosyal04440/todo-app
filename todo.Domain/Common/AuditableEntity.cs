namespace TodoApi.Domain.Common;

public abstract class AuditableEntity
{
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public int? CreatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public int? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public int? DeletedBy { get; private set; }
}
