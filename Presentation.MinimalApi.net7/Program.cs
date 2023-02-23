using Application.Todos;

using Microsoft.AspNetCore.Http.HttpResults;

using Presentation.MinimalApi.Common;

var app = WebAppBuilder.BuildApp(args);
var group = app.MapGroup("/todos");

group.MapGet("", async (ITodoRepository todoRepository, CancellationToken cancellationToken) =>
{
    var todos = await todoRepository.GetTodosAsync(cancellationToken);

    return todos.Select(todo =>
        new TodoListItemDto(todo.Id,
                            todo.Title,
                            todo.Description,
                            todo.IsCompleted,
                            todo.UpdatedAt));
});

group.MapGet("{id}", async (ITodoRepository todoRepository, Guid id, CancellationToken cancellationToken) =>
    await todoRepository.GetTodoByIdAsync(id, cancellationToken))
   .WithName("GetTodo");

group.MapPost("", async Task<Results<CreatedAtRoute<Todo>, BadRequest<string>>> (ITodoRepository todoRepository, CreateTodoDto todoDto, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrEmpty(todoDto.Title))
    {
        return TypedResults.BadRequest("The title is required.");
    }

    if (string.IsNullOrEmpty(todoDto.Title)
        || todoDto.Title.Length is < 5 or > 20)
    {
        return TypedResults.BadRequest("The title must be between 5 and 20 characters.");
    }

    if (string.IsNullOrEmpty(todoDto.Description))
    {
        return TypedResults.BadRequest("The description is required.");
    }

    if (todoDto.Description.Length > 100)
    {
        return TypedResults.BadRequest("The description must be less than 100 characters.");
    }

    var todo = new Todo(todoDto.Title, todoDto.Description);

    await todoRepository.AddTodoAsync(todo, cancellationToken);

    return TypedResults.CreatedAtRoute(todo, "GetTodo", new { id = todo.Id });
}).WithName("CreateTodo");

group.MapPost("{id}/complete", async Task<Results<Ok, NotFound, Conflict<string>>> (ITodoRepository todoRepository, Guid id, CancellationToken cancellationToken) =>
{
    var todo = await todoRepository.GetTodoByIdAsync(id, cancellationToken);

    if (todo is null)
    {
        return TypedResults.NotFound();
    }

    if (todo.IsCompleted)
    {
        return TypedResults.Conflict($"The todo {todo.Id} is already completed.");
    }

    todo.Complete();

    await todoRepository.UpdateTodoAsync(todo, cancellationToken);

    return TypedResults.Ok();
}).WithName("CompleteTodo");

group.MapDelete("{id}", async (ITodoRepository todoRepository, Guid id, CancellationToken cancellationToken) =>
    await todoRepository.DeleteTodoAsync(id, cancellationToken))
   .WithName("DeleteTodo");

app.Run();

public sealed record CreateTodoDto(string Title, string Description);
public sealed record TodoListItemDto(Guid Id, string Title, string Description, bool IsCompleted, DateTime UpdatedAt);