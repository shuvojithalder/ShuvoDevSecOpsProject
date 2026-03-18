using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API",
        Version = "v1"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Minimal API v1");
    c.RoutePrefix = string.Empty; // Swagger UI at application root
});


app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

var todos = new List<TodoItem>
{
    new(1, "Learn minimal APIs", false),
    new(2, "Add Swagger", true)
};

app.MapGet("/todos", () => todos)
   .WithName("GetTodos");

app.MapGet("/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
}).WithName("GetTodoById");

app.MapPost("/todos", (TodoItem newTodo) =>
{
    var nextId = todos.Count == 0 ? 1 : todos.Max(t => t.Id) + 1;
    var todo = new TodoItem(nextId, newTodo.Title, newTodo.IsDone);
    todos.Add(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
}).WithName("CreateTodo");

app.MapPut("/todos/{id:int}", (int id, TodoItem updated) =>
{
    var index = todos.FindIndex(t => t.Id == id);
    if (index == -1) return Results.NotFound();

    var todo = updated with { Id = id };
    todos[index] = todo;
    return Results.Ok(todo);
}).WithName("UpdateTodo");

app.MapDelete("/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    if (todo is null) return Results.NotFound();

    todos.Remove(todo);
    return Results.NoContent();
}).WithName("DeleteTodo");

app.Run();

record TodoItem(int Id, string Title, bool IsDone);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
