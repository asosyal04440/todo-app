using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Common;
using TodoApi.Infrastructure.Persistence.Auditing;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Infrastructure.Persistence;

public sealed partial class TodoDbContext : DbContext
{
    private const string NameKeyCollation = "Latin1_General_100_BIN2";

    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter =
        new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private readonly ICurrentUser? _currentUser;
    private readonly TimeProvider _timeProvider;

    public TodoDbContext(DbContextOptions<TodoDbContext> options,
        ICurrentUser? currentUser = null, TimeProvider? timeProvider = null)
        : base(options)
    {
        _currentUser = currentUser;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    public DbSet<TodoList> TodoLists
    {
        get
        {
            return Set<TodoList>();
        }
    }

    public DbSet<TodoItem> TodoItems
    {
        get
        {
            return Set<TodoItem>();
        }
    }

    public DbSet<User> Users
    {
        get
        {
            return Set<User>();
        }
    }

    public DbSet<RefreshToken> RefreshTokens
    {
        get
        {
            return Set<RefreshToken>();
        }
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ExpandSoftDeletionAsync(true, cancellationToken);
        List<AuditRecord> auditRecords = PrepareAudit();
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            DetachFailedAudit(auditRecords);
            throw DatabaseErrorTranslator.Translate(exception);
        }
        catch
        {
            DetachFailedAudit(auditRecords);
            throw;
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ExpandSoftDeletionAsync(false, CancellationToken.None).GetAwaiter().GetResult();
        List<AuditRecord> auditRecords = PrepareAudit();
        try { return base.SaveChanges(acceptAllChangesOnSuccess); }
        catch (DbUpdateException exception)
        {
            DetachFailedAudit(auditRecords);
            throw DatabaseErrorTranslator.Translate(exception);
        }
        catch
        {
            DetachFailedAudit(auditRecords);
            throw;
        }
    }

    private List<AuditRecord> PrepareAudit()
    {
        ChangeTracker.DetectChanges();
        ChangeTracker.CascadeChanges();
        if (ChangeTracker.Entries<AuditRecord>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Persisted audit records cannot be changed through this context.");

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        int? actorId = _currentUser?.UserIdOrNull;
        List<AuditRecord> records = [];
        foreach (EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>().ToArray())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedAtUtc).CurrentValue = now;
                entry.Property(entity => entity.CreatedBy).CurrentValue = actorId;
                entry.Property(entity => entity.UpdatedAtUtc).CurrentValue = null;
                entry.Property(entity => entity.UpdatedBy).CurrentValue = null;
                entry.Property(entity => entity.IsDeleted).CurrentValue = false;
                entry.Property(entity => entity.DeletedAtUtc).CurrentValue = null;
                entry.Property(entity => entity.DeletedBy).CurrentValue = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Property(entity => entity.IsDeleted).OriginalValue)
                    throw new InvalidOperationException("Deleted records cannot be modified through this context.");
                RestoreAuditMetadata(entry);
                if (!entry.Properties.Any(property => property.IsModified))
                    continue;
                entry.Property(entity => entity.UpdatedAtUtc).CurrentValue = now;
                entry.Property(entity => entity.UpdatedBy).CurrentValue = actorId;
                records.Add(CreateAuditRecord(entry, AuditAction.Updated, actorId, now));
            }
            else if (entry.State == EntityState.Deleted)
            {
                if (entry.Property(entity => entity.IsDeleted).OriginalValue)
                {
                    entry.State = EntityState.Unchanged;
                    continue;
                }
                records.Add(CreateAuditRecord(entry, AuditAction.Deleted, actorId, now));
                entry.State = EntityState.Unchanged;
                entry.Property(entity => entity.IsDeleted).CurrentValue = true;
                entry.Property(entity => entity.DeletedAtUtc).CurrentValue = now;
                entry.Property(entity => entity.DeletedBy).CurrentValue = actorId;
            }
        }
        AuditRecords.AddRange(records);
        return records;
    }

    private static AuditRecord CreateAuditRecord(EntityEntry<AuditableEntity> entry,
        AuditAction action, int? actorId, DateTime now) =>
        new(entry.Metadata.ClrType.Name, (int)entry.Property("Id").CurrentValue!, action, actorId, now);

    private static void RestoreAuditMetadata(EntityEntry<AuditableEntity> entry)
    {
        foreach (string name in new[] { nameof(AuditableEntity.CreatedAtUtc), nameof(AuditableEntity.CreatedBy),
            nameof(AuditableEntity.UpdatedAtUtc), nameof(AuditableEntity.UpdatedBy),
            nameof(AuditableEntity.IsDeleted), nameof(AuditableEntity.DeletedAtUtc), nameof(AuditableEntity.DeletedBy) })
        {
            PropertyEntry property = entry.Property(name);
            property.CurrentValue = property.OriginalValue;
            property.IsModified = false;
        }
    }

    private void DetachFailedAudit(IEnumerable<AuditRecord> records)
    {
        foreach (AuditRecord record in records) Entry(record).State = EntityState.Detached;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>().ToArray())
        {
            if (entry.State == EntityState.Modified && entry.Entity.IsDeleted &&
                !entry.Property(entity => entity.IsDeleted).OriginalValue)
            {
                RestoreAuditMetadata(entry);
                entry.State = EntityState.Deleted;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureTodoList(modelBuilder);
        ConfigureTodoItem(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
        ConfigureUserToTodoListRelationship(modelBuilder);
        ConfigureTodoListToTodoItemRelationship(modelBuilder);

        ConfigureAudit<User>(modelBuilder);
        ConfigureAudit<TodoList>(modelBuilder);
        ConfigureAudit<TodoItem>(modelBuilder);
        modelBuilder.Entity<User>().HasQueryFilter(user => !user.IsDeleted);
        modelBuilder.Entity<TodoList>().HasQueryFilter(list => !list.IsDeleted &&
            Users.Any(user => user.Id == list.OwnerUserId));
        modelBuilder.Entity<TodoItem>().HasQueryFilter(todo => !todo.IsDeleted &&
            TodoLists.Any(list => list.Id == todo.TodoListId));
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(token =>
            Users.Any(user => user.Id == token.UserId));
        EntityTypeBuilder<AuditRecord> audit = modelBuilder.Entity<AuditRecord>();
        audit.ToTable("AuditRecords");
        audit.HasKey(record => record.Id);
        audit.Property(record => record.EntityType).HasMaxLength(80).IsRequired();
        audit.Property(record => record.Action).HasConversion<int>();
        audit.Property(record => record.OccurredAtUtc).HasConversion(UtcDateTimeConverter);
        audit.HasIndex(record => new { record.EntityType, record.EntityId, record.OccurredAtUtc });

        if (Database.IsSqlServer())
        {
            modelBuilder.Entity<TodoList>().Property(list => list.NormalizedName)
                .UseCollation(NameKeyCollation);
            modelBuilder.Entity<TodoItem>().Property(todo => todo.NormalizedTitle)
                .UseCollation(NameKeyCollation);
        }
    }

    private static void ConfigureAudit<TEntity>(ModelBuilder modelBuilder) where TEntity : AuditableEntity
    {
        EntityTypeBuilder<TEntity> entity = modelBuilder.Entity<TEntity>();
        entity.HasBaseType((Type?)null);
        entity.Property(value => value.CreatedBy).IsRequired(false);
        entity.Property(value => value.UpdatedBy).IsRequired(false);
        entity.Property(value => value.UpdatedAtUtc).HasConversion(UtcDateTimeConverter).IsRequired(false);
        entity.Property(value => value.IsDeleted).HasDefaultValue(false).IsConcurrencyToken();
        entity.Property(value => value.DeletedAtUtc).HasConversion(UtcDateTimeConverter).IsRequired(false);
        entity.Property(value => value.DeletedBy).IsRequired(false);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<User> userEntity = modelBuilder.Entity<User>();

        userEntity.ToTable("Users");
        userEntity.HasKey(user => user.Id);

        PropertyBuilder<int> userIdProperty = userEntity.Property(user => user.Id);

        userIdProperty.ValueGeneratedOnAdd();

        PropertyBuilder<string> userEmailProperty = userEntity.Property(user => user.Email);

        userEmailProperty.HasMaxLength(User.MaximumEmailLength);
        userEmailProperty.IsRequired();

        userEntity.HasIndex(user => user.Email).IsUnique().HasFilter("[IsDeleted] = 0");

        PropertyBuilder<string> userDisplayNameProperty =
            userEntity.Property(user => user.DisplayName);

        userDisplayNameProperty.HasMaxLength(User.MaximumDisplayNameLength);
        userDisplayNameProperty.IsRequired();

        PropertyBuilder<string?> userPasswordHashProperty =
            userEntity.Property(user => user.PasswordHash);

        userPasswordHashProperty.HasMaxLength(500);
        userPasswordHashProperty.IsRequired(false);

        PropertyBuilder<string?> userGoogleSubjectIdProperty =
            userEntity.Property(user => user.GoogleSubjectId);

        userGoogleSubjectIdProperty.HasMaxLength(User.MaximumGoogleSubjectIdLength);
        userGoogleSubjectIdProperty.IsRequired(false);

        userEntity.HasIndex(user => user.GoogleSubjectId).IsUnique()
            .HasFilter("[GoogleSubjectId] IS NOT NULL AND [IsDeleted] = 0");

        PropertyBuilder<UserRole> userRoleProperty = userEntity.Property(user => user.Role);

        userRoleProperty.HasConversion<int>();
        userRoleProperty.IsRequired();

        PropertyBuilder<DateTime> userCreatedAtUtcProperty =
            userEntity.Property(user => user.CreatedAtUtc);

        userCreatedAtUtcProperty.HasConversion(UtcDateTimeConverter);
        userCreatedAtUtcProperty.IsRequired();

        userEntity.Ignore(user => user.HasPassword);
        userEntity.Ignore(user => user.HasGoogleLogin);
        userEntity.Ignore(user => user.IsAdmin);
    }

    private static void ConfigureTodoList(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<TodoList> todoListEntity = modelBuilder.Entity<TodoList>();

        todoListEntity.ToTable("TodoLists");
        todoListEntity.HasKey(todoList => todoList.Id);

        PropertyBuilder<int> todoListIdProperty =
            todoListEntity.Property(todoList => todoList.Id);

        todoListIdProperty.ValueGeneratedOnAdd();

        PropertyBuilder<string> todoListNameProperty =
            todoListEntity.Property(todoList => todoList.Name);

        todoListNameProperty.HasMaxLength(TodoList.MaximumNameLength);
        todoListNameProperty.IsRequired();

        todoListEntity.Property(list => list.NormalizedName)
            .HasMaxLength(TodoList.MaximumNormalizedNameLength)
            .IsRequired();

        PropertyBuilder<DateTime> todoListCreatedAtUtcProperty =
            todoListEntity.Property(todoList => todoList.CreatedAtUtc);

        todoListCreatedAtUtcProperty.HasConversion(UtcDateTimeConverter);
        todoListCreatedAtUtcProperty.IsRequired();

        PropertyBuilder<int> todoListOwnerUserIdProperty =
            todoListEntity.Property(todoList => todoList.OwnerUserId);

        todoListOwnerUserIdProperty.IsRequired();

        todoListEntity
            .HasIndex(todoList => new { todoList.OwnerUserId, todoList.NormalizedName })
            .IsUnique().HasFilter("[IsDeleted] = 0");
    }

    private static void ConfigureTodoItem(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<TodoItem> todoItemEntity = modelBuilder.Entity<TodoItem>();

        todoItemEntity.ToTable("TodoItems");
        todoItemEntity.HasKey(todoItem => todoItem.Id);

        PropertyBuilder<int> todoItemIdProperty =
            todoItemEntity.Property(todoItem => todoItem.Id);

        todoItemIdProperty.ValueGeneratedOnAdd();

        PropertyBuilder<int> todoItemTodoListIdProperty =
            todoItemEntity.Property(todoItem => todoItem.TodoListId);

        todoItemTodoListIdProperty.IsRequired();

        PropertyBuilder<string> todoItemTitleProperty =
            todoItemEntity.Property(todoItem => todoItem.Title);

        todoItemTitleProperty.HasMaxLength(TodoItem.MaximumTitleLength);
        todoItemTitleProperty.IsRequired();

        todoItemEntity.Property(todo => todo.NormalizedTitle)
            .HasMaxLength(TodoItem.MaximumNormalizedTitleLength)
            .IsRequired();

        PropertyBuilder<string?> todoItemDescriptionProperty =
            todoItemEntity.Property(todoItem => todoItem.Description);

        todoItemDescriptionProperty.HasMaxLength(TodoItem.MaximumDescriptionLength);

        PropertyBuilder<bool> todoItemIsCompletedProperty =
            todoItemEntity.Property(todoItem => todoItem.IsCompleted);

        todoItemIsCompletedProperty.IsRequired();

        PropertyBuilder<DateTime> todoItemCreatedAtUtcProperty =
            todoItemEntity.Property(todoItem => todoItem.CreatedAtUtc);

        todoItemCreatedAtUtcProperty.HasConversion(UtcDateTimeConverter);
        todoItemCreatedAtUtcProperty.IsRequired();

        PropertyBuilder<DateTime?> todoItemDueAtUtcProperty =
            todoItemEntity.Property(todoItem => todoItem.DueAtUtc);

        todoItemDueAtUtcProperty.HasConversion(UtcDateTimeConverter);
        todoItemDueAtUtcProperty.IsRequired(false);

        PropertyBuilder<TodoPriority> todoItemPriorityProperty =
            todoItemEntity.Property(todoItem => todoItem.Priority);

        todoItemPriorityProperty.HasConversion<int>();
        todoItemPriorityProperty.IsRequired();

        PropertyBuilder<DateTime?> todoItemReminderSentAtUtcProperty =
            todoItemEntity.Property(todoItem => todoItem.ReminderSentAtUtc);

        todoItemReminderSentAtUtcProperty.HasConversion(UtcDateTimeConverter);
        todoItemReminderSentAtUtcProperty.IsRequired(false);

        PropertyBuilder<int> todoItemReminderAttemptCountProperty =
            todoItemEntity.Property(todoItem => todoItem.ReminderAttemptCount);

        todoItemReminderAttemptCountProperty.IsRequired();
        todoItemReminderAttemptCountProperty.HasDefaultValue(0);

        PropertyBuilder<DateTime?> todoItemReminderLastAttemptAtUtcProperty =
            todoItemEntity.Property(todoItem => todoItem.ReminderLastAttemptAtUtc);

        todoItemReminderLastAttemptAtUtcProperty.HasConversion(UtcDateTimeConverter);
        todoItemReminderLastAttemptAtUtcProperty.IsRequired(false);

        todoItemEntity.HasIndex(todoItem => todoItem.DueAtUtc);

        todoItemEntity
            .HasIndex(todoItem => new { todoItem.TodoListId, todoItem.NormalizedTitle })
            .IsUnique().HasFilter("[IsDeleted] = 0");
    }

    private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<RefreshToken> refreshTokenEntity = modelBuilder.Entity<RefreshToken>();

        refreshTokenEntity.ToTable("RefreshTokens");
        refreshTokenEntity.HasKey(refreshToken => refreshToken.Id);

        PropertyBuilder<int> refreshTokenIdProperty =
            refreshTokenEntity.Property(refreshToken => refreshToken.Id);

        refreshTokenIdProperty.ValueGeneratedOnAdd();

        PropertyBuilder<string> tokenHashProperty =
            refreshTokenEntity.Property(refreshToken => refreshToken.TokenHash);

        tokenHashProperty.HasMaxLength(64);
        tokenHashProperty.IsFixedLength();
        tokenHashProperty.IsUnicode(false);
        tokenHashProperty.IsRequired();

        refreshTokenEntity.HasIndex(refreshToken => refreshToken.TokenHash).IsUnique();

        PropertyBuilder<string?> replacedByTokenHashProperty =
            refreshTokenEntity.Property(refreshToken => refreshToken.ReplacedByTokenHash);

        replacedByTokenHashProperty.HasMaxLength(64);
        replacedByTokenHashProperty.IsFixedLength();
        replacedByTokenHashProperty.IsUnicode(false);
        replacedByTokenHashProperty.IsRequired(false);

        PropertyBuilder<DateTime> createdAtUtcProperty =
            refreshTokenEntity.Property(refreshToken => refreshToken.CreatedAtUtc);

        createdAtUtcProperty.HasConversion(UtcDateTimeConverter);
        createdAtUtcProperty.IsRequired();

        PropertyBuilder<DateTime> expiresAtUtcProperty =
            refreshTokenEntity.Property(refreshToken => refreshToken.ExpiresAtUtc);

        expiresAtUtcProperty.HasConversion(UtcDateTimeConverter);
        expiresAtUtcProperty.IsRequired();

        PropertyBuilder<DateTime?> revokedAtUtcProperty =
            refreshTokenEntity.Property(refreshToken => refreshToken.RevokedAtUtc);

        revokedAtUtcProperty.HasConversion(UtcDateTimeConverter);
        revokedAtUtcProperty.IsRequired(false);

        refreshTokenEntity.Ignore(refreshToken => refreshToken.IsRevoked);

        refreshTokenEntity
            .HasOne(refreshToken => refreshToken.User)
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.ClientNoAction);
    }

    private static void ConfigureUserToTodoListRelationship(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<TodoList> todoListEntity = modelBuilder.Entity<TodoList>();

        ReferenceNavigationBuilder<TodoList, User> ownerNavigation =
            todoListEntity.HasOne<User>();

        ReferenceCollectionBuilder<User, TodoList> ownerRelationship =
            ownerNavigation.WithMany();

        ownerRelationship.HasForeignKey(todoList => todoList.OwnerUserId);
        ownerRelationship.OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureTodoListToTodoItemRelationship(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<TodoList> todoListEntity = modelBuilder.Entity<TodoList>();

        CollectionNavigationBuilder<TodoList, TodoItem> todoItemsNavigation =
            todoListEntity.HasMany(todoList => todoList.Todos);

        ReferenceCollectionBuilder<TodoList, TodoItem> todoItemsRelationship =
            todoItemsNavigation.WithOne();

        todoItemsRelationship.HasForeignKey(todoItem => todoItem.TodoListId);
        todoItemsRelationship.OnDelete(DeleteBehavior.Cascade);

        NavigationBuilder<TodoList, IReadOnlyCollection<TodoItem>> todoListTodosNavigation =
            todoListEntity.Navigation(todoList => todoList.Todos);

        todoListTodosNavigation.HasField("_todos");
        todoListTodosNavigation.UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
