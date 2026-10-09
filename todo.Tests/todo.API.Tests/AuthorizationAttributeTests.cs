using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.API.Authentication;
using TodoApi.API.Controllers;

namespace todo.API.Tests;

public class AuthorizationAttributeTests
{
    [Theory]
    [InlineData(typeof(TodoListsController))]
    [InlineData(typeof(TodosController))]
    public void TodoControllers_RequireASignedInUser(Type controllerType)
    {
        AuthorizeAttribute? authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Null(controllerType.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void AdminUsersController_RequiresTheAdminPolicy()
    {
        AuthorizeAttribute? authorize =
            typeof(AdminUsersController).GetCustomAttribute<AuthorizeAttribute>();
        RouteAttribute? route = typeof(AdminUsersController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.AdminOnly, authorize.Policy);
        Assert.NotNull(route);
        Assert.Equal("api/admin/users", route.Template);
    }

    [Theory]
    [InlineData(nameof(AuthController.GetConfig))]
    [InlineData(nameof(AuthController.Register))]
    [InlineData(nameof(AuthController.Login))]
    [InlineData(nameof(AuthController.LoginWithGoogle))]
    [InlineData(nameof(AuthController.Refresh))]
    [InlineData(nameof(AuthController.Logout))]
    public void AuthController_SignInEndpointsAreOpenToAnonymousUsers(string actionName)
    {
        MethodInfo action = typeof(AuthController).GetMethod(actionName)!;

        Assert.NotNull(action.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void AuthController_CurrentUserEndpointRequiresASignedInUser()
    {
        MethodInfo action = typeof(AuthController).GetMethod(nameof(AuthController.GetCurrentUser))!;

        Assert.NotNull(action.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void AuthController_RequiresTheClientHeaderAgainstCsrf()
    {
        Assert.NotNull(typeof(AuthController).GetCustomAttribute<RequireClientHeaderAttribute>());
    }
}
