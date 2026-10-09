using Microsoft.AspNetCore.Mvc;
using TodoApi.API.Controllers;

namespace todo.API.Tests;

public class ControllerRouteTests
{
    [Fact]
    public void TodoListsController_UsesTheTodoListsRoute()
    {
        RouteAttribute? route = typeof(TodoListsController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        Assert.NotNull(route);
        Assert.Equal("api/todo-lists", route.Template);
    }

    [Fact]
    public void TodosController_UsesTheNestedTodoRoute()
    {
        RouteAttribute? route = typeof(TodosController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        Assert.NotNull(route);
        Assert.Equal("api/todo-lists/{todoListId:int}/todos", route.Template);
    }
}
