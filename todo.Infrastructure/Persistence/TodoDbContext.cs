using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Infrastructure.Persistence;

public sealed class TodoDbContext : DbContext
{
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter =
        new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options)
    {
    }

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
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw DatabaseErrorTranslator.Translate(exception);
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
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<User> userEntity = modelBuilder.Entity<User>();

        userEntity.ToTable("Users");
        userEntity.HasKey(user => user.Id);

        PropertyBuilder<int> userIdProperty = userEntity.Property(user => user.Id);

        userIdProperty.ValueGeneratedOnAdd();

        PropertyBuilder<string> userEmailProperty = userEntity.Property(user => user.Email);

        userEmailProperty.HasMaxLength(256);
        userEmailProperty.IsRequired();

        userEntity.HasIndex(user => user.Email).IsUnique();

        PropertyBuilder<string> userDisplayNameProperty =
            userEntity.Property(user => user.DisplayName);

        userDisplayNameProperty.HasMaxLength(100);
        userDisplayNameProperty.IsRequired();

        PropertyBuilder<string?> userPasswordHashProperty =
            userEntity.Property(user => user.PasswordHash);

        userPasswordHashProperty.HasMaxLength(500);
        userPasswordHashProperty.IsRequired(false);

        PropertyBuilder<string?> userGoogleSubjectIdProperty =
            userEntity.Property(user => user.GoogleSubjectId);

        userGoogleSubjectIdProperty.HasMaxLength(255);
        userGoogleSubjectIdProperty.IsRequired(false);

        userEntity.HasIndex(user => user.GoogleSubjectId).IsUnique();

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

        todoListNameProperty.HasMaxLength(80);
        todoListNameProperty.IsRequired();

        PropertyBuilder<DateTime> todoListCreatedAtUtcProperty =
            todoListEntity.Property(todoList => todoList.CreatedAtUtc);

        todoListCreatedAtUtcProperty.HasConversion(UtcDateTimeConverter);
        todoListCreatedAtUtcProperty.IsRequired();

        PropertyBuilder<int> todoListOwnerUserIdProperty =
            todoListEntity.Property(todoList => todoList.OwnerUserId);

        todoListOwnerUserIdProperty.IsRequired();

        todoListEntity
            .HasIndex(todoList => new { todoList.OwnerUserId, todoList.Name })
            .IsUnique();
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

        todoItemTitleProperty.HasMaxLength(120);
        todoItemTitleProperty.IsRequired();

        PropertyBuilder<string?> todoItemDescriptionProperty =
            todoItemEntity.Property(todoItem => todoItem.Description);

        todoItemDescriptionProperty.HasMaxLength(1000);

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
            .HasIndex(todoItem => new { todoItem.TodoListId, todoItem.Title })
            .IsUnique();
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
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);
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
