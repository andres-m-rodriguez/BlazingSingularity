using BlazingSingularity.Demo.Client.Todo.Dtos;

namespace BlazingSingularity.Demo.Features.Services;

public class TodoRepository
{
    private readonly List<TodoForListDto> _todos =
    [
        new(Guid.CreateVersion7(), "Buy groceries", false, DateTime.Now.AddDays(-2)),
        new(Guid.CreateVersion7(), "Walk the dog", true, DateTime.Now.AddDays(-1)),
        new(Guid.CreateVersion7(), "Finish Blazing Singularity project", false, DateTime.Now),
    ];

    public Task<List<TodoForListDto>> GetTodosAsync()
    {
        return Task.FromResult(_todos);
    }

    public Task<TodoForListDto?> GetTodoByIdAsync(Guid id)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        return Task.FromResult(todo);
    }

    public Task CompleteTask(Guid id)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo is not null)
        {
            var index = _todos.IndexOf(todo);
            _todos[index] = todo with { IsCompleted = true };
        }

        return Task.CompletedTask;
    }
}
