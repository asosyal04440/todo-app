using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TodoApi.Application.Exceptions;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Persistence;

namespace todo.Infrastructure.Tests;

public class TodoDbContextModelTests
{
    [Fact]
    public void Model_HasNoPendingSchemaChanges()
    {
        using TodoDbContext dbContext = CreateDbContext();

        Assert.False(dbContext.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Model_MapsListsAndTodosToTheirTables()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IEntityType todoListEntity = dbContext.Model.FindEntityType(typeof(TodoList))!;
        IEntityType todoItemEntity = dbContext.Model.FindEntityType(typeof(TodoItem))!;

        Assert.Equal("TodoLists", todoListEntity.GetTableName());
        Assert.Equal("TodoItems", todoItemEntity.GetTableName());
    }

    [Fact]
    public void Model_RequiresNamesAndLimitsTheirLength()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IProperty listName = dbContext.Model
            .FindEntityType(typeof(TodoList))!
            .FindProperty(nameof(TodoList.Name))!;
        IProperty todoTitle = dbContext.Model
            .FindEntityType(typeof(TodoItem))!
            .FindProperty(nameof(TodoItem.Title))!;

        Assert.False(listName.IsNullable);
        Assert.Equal(80, listName.GetMaxLength());
        Assert.False(todoTitle.IsNullable);
        Assert.Equal(120, todoTitle.GetMaxLength());
    }

    [Fact]
    public void Model_ConfiguresTodoItemsToCascadeWhenTheirListIsDeleted()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IForeignKey relationship = dbContext.Model
            .FindEntityType(typeof(TodoItem))!
            .GetForeignKeys()
            .Single();

        Assert.Equal(typeof(TodoList), relationship.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, relationship.DeleteBehavior);
    }

    [Fact]
    public void Model_StoresPriorityAsAnInteger()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IProperty priority = dbContext.Model
            .FindEntityType(typeof(TodoItem))!
            .FindProperty(nameof(TodoItem.Priority))!;

        Assert.Equal(typeof(int), priority.GetProviderClrType());
    }

    [Fact]
    public void DueDateConverter_RestoresTheUtcDateTimeKind()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IProperty dueDate = dbContext.Model
            .FindEntityType(typeof(TodoItem))!
            .FindProperty(nameof(TodoItem.DueAtUtc))!;
        var converter = dueDate.GetValueConverter();
        DateTime providerDate = new DateTime(2026, 9, 26, 15, 0, 0);

        Assert.NotNull(converter);

        DateTime restoredDate = Assert.IsType<DateTime>(
            converter.ConvertFromProvider(providerDate));

        Assert.Equal(DateTimeKind.Utc, restoredDate.Kind);
    }

    [Fact]
    public void Model_RequiresAUniqueEmailForEveryUser()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IEntityType userEntity = dbContext.Model.FindEntityType(typeof(User))!;
        IProperty email = userEntity.FindProperty(nameof(User.Email))!;
        IIndex emailIndex = userEntity.GetIndexes()
            .Single(index => index.Properties.Single().Name == nameof(User.Email));

        Assert.Equal("Users", userEntity.GetTableName());
        Assert.False(email.IsNullable);
        Assert.True(emailIndex.IsUnique);
    }

    [Fact]
    public void Model_DoesNotStoreCalculatedUserProperties()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IEntityType userEntity = dbContext.Model.FindEntityType(typeof(User))!;

        Assert.Null(userEntity.FindProperty(nameof(User.HasPassword)));
        Assert.Null(userEntity.FindProperty(nameof(User.HasGoogleLogin)));
        Assert.Null(userEntity.FindProperty(nameof(User.IsAdmin)));
    }

    [Fact]
    public void Model_LinksEveryListToItsOwner()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IForeignKey ownerRelationship = dbContext.Model
            .FindEntityType(typeof(TodoList))!
            .GetForeignKeys()
            .Single();

        Assert.Equal(typeof(User), ownerRelationship.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(TodoList.OwnerUserId), ownerRelationship.Properties.Single().Name);
        Assert.True(ownerRelationship.IsRequired);
    }

    [Fact]
    public void Model_StoresRefreshTokenHashesWithAUniqueIndex()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IEntityType refreshTokenEntity = dbContext.Model.FindEntityType(typeof(RefreshToken))!;
        IProperty tokenHash = refreshTokenEntity.FindProperty(nameof(RefreshToken.TokenHash))!;
        IIndex tokenHashIndex = refreshTokenEntity.GetIndexes()
            .Single(index => index.Properties.Single().Name == nameof(RefreshToken.TokenHash));
        IForeignKey userRelationship = refreshTokenEntity.GetForeignKeys().Single();

        Assert.Equal("RefreshTokens", refreshTokenEntity.GetTableName());
        Assert.Equal(64, tokenHash.GetMaxLength());
        Assert.True(tokenHashIndex.IsUnique);
        Assert.Equal(typeof(User), userRelationship.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.ClientNoAction, userRelationship.DeleteBehavior);
    }

    [Fact]
    public void Model_RequiresUniqueTodoTitlesWithinAList()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IIndex index = FindIndex(dbContext, typeof(TodoItem), nameof(TodoItem.TodoListId), nameof(TodoItem.NormalizedTitle));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Model_RequiresUniqueListNamesPerOwner()
    {
        using TodoDbContext dbContext = CreateDbContext();

        IIndex index = FindIndex(dbContext, typeof(TodoList), nameof(TodoList.OwnerUserId), nameof(TodoList.NormalizedName));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Configuration_RetriesTransientDatabaseErrors()
    {
        DbContextOptionsBuilder<TodoDbContext> optionsBuilder = new DbContextOptionsBuilder<TodoDbContext>();
        optionsBuilder.UseTodoSqlServer(
            "Server=localhost;Database=TodoModelOnly;Integrated Security=True;TrustServerCertificate=True");
        using TodoDbContext dbContext = new TodoDbContext(optionsBuilder.Options);

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        Assert.True(executionStrategy.RetriesOnFailure);
    }

    [Fact]
    public void Model_UsesBinaryRequiredKeysForNameEquality()
    {
        using TodoDbContext dbContext = CreateDbContext();
        IModel model = dbContext.GetService<IDesignTimeModel>().Model;
        IProperty name = model.FindEntityType(typeof(TodoList))!.FindProperty(nameof(TodoList.NormalizedName))!;
        IProperty title = model.FindEntityType(typeof(TodoItem))!.FindProperty(nameof(TodoItem.NormalizedTitle))!;

        Assert.False(name.IsNullable);
        Assert.False(title.IsNullable);
        Assert.Equal("Latin1_General_100_BIN2", name.GetCollation());
        Assert.Equal("Latin1_General_100_BIN2", title.GetCollation());
    }

    [Theory]
    [InlineData(2601, true)]
    [InlineData(2627, true)]
    [InlineData(1205, false)]
    public void DatabaseErrorTranslator_RecognizesUniqueViolations(int sqlErrorNumber, bool expected)
    {
        Assert.Equal(expected, DatabaseErrorTranslator.IsUniqueViolation(sqlErrorNumber));
    }

    [Fact]
    public void DatabaseErrorTranslator_TurnsOtherWriteErrorsIntoDataAccessErrors()
    {
        DbUpdateException exception = new DbUpdateException("disk full", new InvalidOperationException());

        Exception translated = DatabaseErrorTranslator.Translate(exception);

        Assert.IsType<DataAccessException>(translated);
        Assert.Same(exception, translated.InnerException);
    }

    [Fact]
    public void DatabaseErrorTranslator_TurnsARecordChangedByAnotherRequestIntoAConcurrencyConflict()
    {
        DbUpdateConcurrencyException exception = new DbUpdateConcurrencyException(
            "The database operation was expected to affect 1 row(s), but actually affected 0 row(s).");

        Exception translated = DatabaseErrorTranslator.Translate(exception);

        Assert.IsType<ConcurrencyConflictException>(translated);
        Assert.Same(exception, translated.InnerException);
    }

    private static IIndex FindIndex(TodoDbContext dbContext, Type entityType, params string[] propertyNames)
    {
        return dbContext.Model
            .FindEntityType(entityType)!
            .GetIndexes()
            .Single(index => index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
    }

    private static TodoDbContext CreateDbContext()
    {
        DbContextOptions<TodoDbContext> options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=TodoModelOnly;Integrated Security=True;TrustServerCertificate=True")
            .Options;

        return new TodoDbContext(options);
    }
}
