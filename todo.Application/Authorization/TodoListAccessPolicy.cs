using System;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Authorization;

public static class TodoListAccessPolicy
{
    public static bool CanAccess(TodoList todoList, ICurrentUser currentUser)
    {
        if (todoList == null)
        {
            throw new ArgumentNullException(nameof(todoList));
        }

        if (currentUser == null)
        {
            throw new ArgumentNullException(nameof(currentUser));
        }

        if (currentUser.IsAdmin)
        {
            return true;
        }

        bool isOwner = todoList.IsOwnedBy(currentUser.UserId);

        return isOwner;
    }
}
