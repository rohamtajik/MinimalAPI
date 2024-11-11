using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

app.Use(async (context, next) =>
{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");  
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished.");

});


var todos = new List<Todo>();

app.MapPost("/todos", (Todo task) =>
{
    if (todos.Any(t => t.Id == task.Id))
    {
        return Results.BadRequest("This Id exists");
    }
    else
    {
        todos.Add(task);
        return TypedResults.Created("/todos/{id}", task);
    }
       
});

app.MapGet("/todos", () => todos);
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound>(int id) =>
{
    var targetTodo = todos.SingleOrDefault(t => id == t.Id);
    return targetTodo is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(targetTodo);
});

app.MapDelete("/todos", () =>
{
    todos.Clear();
    return TypedResults.NoContent();
});

app.MapDelete("/todos/{id}", (int id) =>
{
    todos.RemoveAll(t => id == t.Id);
    return TypedResults.NoContent();
});
app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);