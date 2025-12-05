using BlazingSingularity.Demo.Client.Todo.Dtos;
using System.Net.Http.Json;

namespace BlazingSingularity.Demo.Client.Todo.HttpClients;

public class TodoHttpClient(HttpClient httpClient) 
{ 
    public async Task<List<TodoForListDto>> GetTodosAsync(string? search = null)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? "api/todos/"
            : $"api/todos/?search={Uri.EscapeDataString(search)}";
        var todos = await httpClient.GetFromJsonAsync<List<TodoForListDto>>(url);
        return todos ?? [];
    }
    public Task<TodoForListDto?> GetTodoByIdAsync(Guid id)
    {
        return httpClient.GetFromJsonAsync<TodoForListDto?>($"api/todos/{id}");
    }


}
