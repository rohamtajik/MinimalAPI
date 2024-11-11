using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITaskService>(new inmemorytaskservice());

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

app.Use(async (context, next) =>
{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished.");
    
});

var todos = new List<Todo>();

app.MapPost("/todos", (Todo task, ITaskService service) =>
    {
        if (todos.Any(t => t.Id == task.Id))
        {
            return Results.BadRequest("This Id exists");
        }
        else
        {
            service.AddTodo(task);
            return TypedResults.Created("/todos/{id}", task);
        }

    })
    .AddEndpointFilter(async (context, next) =>
    {
        var taskArgument = context.GetArgument<Todo>(0);
        var errors = new Dictionary<string, string[]>();

        if (taskArgument.DueDate < DateTime.UtcNow)
        {
            errors.Add(nameof(Todo.DueDate), new string[] { "Cannot have due date in the past" });
        }

        if (taskArgument.IsCompleted)
        {
            errors.Add(nameof(Todo.IsCompleted), new string[] { "Cannot add Complete todo." });
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        return await next(context);
    });


app.MapGet("/todos", (ITaskService service) => service.GetTodos());
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id,ITaskService service) =>
{
    var targetTodo = service.GetTodoById(id);
    return targetTodo is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(targetTodo);
});

app.MapDelete("/todos", () =>
{
    todos.Clear();
    return TypedResults.NoContent();
});

app.MapDelete("/todos/{id}", (int id, ITaskService service) =>
{
    service.DeleteTodoById(id);
    return TypedResults.NoContent();
});
app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);

public interface ITaskService
{
    Todo? GetTodoById(int id);

    List<Todo> GetTodos();

    void DeleteTodoById(int id);

    Todo AddTodo(Todo task);
}

class inmemorytaskservice : ITaskService
{
    private readonly List<Todo> _todos = new List<Todo>();

    public Todo AddTodo(Todo task)
    {
        _todos.Add(task);
        return task;
    }

    public void DeleteTodoById(int id)
    {
        _todos.RemoveAll(task => id == task.Id);
    }

    public Todo? GetTodoById(int id)
    {
        return _todos.SingleOrDefault(t => id == t.Id);
    }

    public List<Todo> GetTodos()
    {
        return _todos;
    }
}
