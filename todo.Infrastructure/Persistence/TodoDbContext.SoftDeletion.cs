using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Persistence;

public sealed partial class TodoDbContext
{
    private async Task ExpandSoftDeletionAsync(bool asynchronous, CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        int[] userIds = ChangeTracker.Entries<User>()
            .Where(entry => entry.State == EntityState.Deleted && !entry.Entity.IsDeleted)
            .Select(entry => entry.Entity.Id).ToArray();
        if (userIds.Length > 0)
        {
            var listsQuery = TodoLists.IgnoreQueryFilters()
                .Where(list => !list.IsDeleted && userIds.Contains(list.OwnerUserId));
            var lists = asynchronous ? await listsQuery.ToListAsync(cancellationToken) : listsQuery.ToList();
            foreach (var list in lists) Entry(list).State = EntityState.Deleted;

            var tokensQuery = RefreshTokens.IgnoreQueryFilters()
                .Where(token => userIds.Contains(token.UserId) && token.RevokedAtUtc == null);
            var tokens = asynchronous ? await tokensQuery.ToListAsync(cancellationToken) : tokensQuery.ToList();
            DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
            foreach (var token in tokens) token.Revoke(now);
        }

        int[] listIds = ChangeTracker.Entries<TodoList>()
            .Where(entry => entry.State == EntityState.Deleted && !entry.Entity.IsDeleted)
            .Select(entry => entry.Entity.Id).ToArray();
        if (listIds.Length > 0)
        {
            var todosQuery = TodoItems.IgnoreQueryFilters()
                .Where(todo => !todo.IsDeleted && listIds.Contains(todo.TodoListId));
            var todos = asynchronous ? await todosQuery.ToListAsync(cancellationToken) : todosQuery.ToList();
            foreach (var todo in todos) Entry(todo).State = EntityState.Deleted;
        }
    }
}
