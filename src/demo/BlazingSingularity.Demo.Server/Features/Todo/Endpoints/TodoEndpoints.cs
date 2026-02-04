using BlazingSingularity.Demo.Features.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlazingSingularity.Demo.Features.Todo.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/todos").WithTags("Todo");
        group
            .MapGet(
                "/",
                async ([FromServices] TodoRepository todoRepository) =>
                {
                    await Task.Delay(5_000);
                    return await todoRepository.GetTodosAsync();
                }
            )
            .WithName("GetTodos")
            .WithSummary("Retrieve a list of todo items.");

        group
            .MapGet(
                "/{id}",
                async (Guid id, [FromServices] TodoRepository todoRepository) =>
                {
                    var fetchedTodo = await todoRepository.GetTodoByIdAsync(id);
                    if (fetchedTodo is null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok(fetchedTodo);
                }
            )
            .WithName("GetTodoById")
            .WithSummary("Retrieve a specific todo item by its ID.");

        return endpoints;
    }
}
