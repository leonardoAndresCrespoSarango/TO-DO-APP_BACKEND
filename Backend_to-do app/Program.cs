using Backend_to_do_app.DB_Data;
using Backend_to_do_app.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskModel = Backend_to_do_app.Models.Task;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Annotations;

/// <summary>
/// Punto de entrada de la aplicación para configurar servicios, middlewares, y endpoints principales.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// Configuración de la cadena de conexión
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Configuración de DbContext y habilitación de reintentos automáticos para la base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(2, TimeSpan.FromSeconds(5), null)
            .CommandTimeout(30)
    ));

// Configura SignalR para comunicación en tiempo real
builder.Services.AddSignalR();

// Configura los servicios de MVC y documentación con Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API de Tareas (TO-DO-LIST)",
        Version = "v1",
        Description = "API para la gestión de tareas en tiempo real.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Leonardo Crespo",
            Email = "lcresposarango@gmail.com"
        }
    });
    c.EnableAnnotations();

    // Carga el archivo XML para documentación 
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configuración de CORS
app.UseCors("AllowAllOrigins");

// Configuración de Swagger 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mi API v1");
        c.RoutePrefix = string.Empty;
    });
}

/// <summary>
/// Endpoint para verificar la conexión con la base de datos.
/// </summary>
app.MapGet("/api/test-db-connection", async (ApplicationDbContext dbContext) =>
{
    try
    {
        await dbContext.Database.CanConnectAsync();
        return Results.Ok(new { message = "Conexión exitosa a la base de datos." });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = "Error al conectar con la base de datos", details = ex.Message }, statusCode: 500);
    }
});

/// <summary>
/// Endpoint para recuperar todas las tareas no eliminadas.
/// </summary>
app.MapGet("/api/tasks", async (ApplicationDbContext dbContext) =>
    {
        var tasks = await dbContext.Tasks.Where(t => !t.IsDeleted).ToListAsync();
        return Results.Ok(tasks);
    })
    .WithMetadata(new SwaggerOperationAttribute("Obtiene todas las tareas", "Este endpoint devuelve una lista de todas las tareas que no están marcadas como eliminadas."))
    .WithMetadata(new SwaggerResponseAttribute(200, "Lista de tareas"))
    .WithMetadata(new SwaggerResponseAttribute(500, "Error al obtener las tareas"));

/// <summary>
/// Endpoint para agregar una nueva tarea en la base de datos.
/// </summary>
app.MapPost("/api/tasks", async (ApplicationDbContext dbContext, TaskModel newTask) =>
    {
        if (newTask == null)
        {
            return Results.BadRequest("Invalid task data.");
        }

        if (!await dbContext.Database.CanConnectAsync())
        {
            return Results.Json(new { error = "Database connection unavailable." }, statusCode: 503);
        }

        newTask.CreatedAt = newTask.CreatedAt.ToUniversalTime();
        dbContext.Tasks.Add(newTask);
        await dbContext.SaveChangesAsync();

        return Results.Created($"/api/tasks/{newTask.Id}", newTask);
    })
    .WithMetadata(new SwaggerOperationAttribute("Agrega una nueva tarea", "Este endpoint crea una nueva tarea con los datos proporcionados."))
    .WithMetadata(new SwaggerResponseAttribute(201, "La tarea fue creada exitosamente"))
    .WithMetadata(new SwaggerResponseAttribute(400, "Datos de la tarea no válidos"))
    .WithMetadata(new SwaggerResponseAttribute(503, "Error de conexión con la base de datos"));

/// <summary>
/// Endpoint para marcar una tarea como completada.
/// </summary>
app.MapPut("/api/tasks/{id}/complete", async (ApplicationDbContext dbContext, int id) =>
    {
        var task = await dbContext.Tasks.FindAsync(id);
        if (task == null)
        {
            return Results.NotFound();
        }

        task.IsCompleted = true;
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    })
    .WithMetadata(new SwaggerOperationAttribute("Marca una tarea como completada", "Este endpoint actualiza el estado de la tarea a 'completada'."))
    .WithMetadata(new SwaggerResponseAttribute(204, "La tarea fue marcada como completada"))
    .WithMetadata(new SwaggerResponseAttribute(404, "La tarea no fue encontrada"));

/// <summary>
/// Endpoint para eliminar una tarea de forma lógica.
/// </summary>
app.MapDelete("/api/tasks/{id}", async (ApplicationDbContext dbContext, int id) =>
    {
        var task = await dbContext.Tasks.FindAsync(id);
        if (task == null)
        {
            return Results.NotFound();
        }

        task.IsDeleted = true;
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    })
    .WithMetadata(new SwaggerOperationAttribute("Elimina una tarea de forma lógica", "Este endpoint marca una tarea como eliminada, pero no la elimina físicamente de la base de datos."))
    .WithMetadata(new SwaggerResponseAttribute(204, "La tarea fue eliminada correctamente"))
    .WithMetadata(new SwaggerResponseAttribute(404, "La tarea no fue encontrada"));

/// <summary>
/// Endpoint para obtener estadísticas de las tareas (total de tareas, completadas, no completadas, eliminadas).
/// Este endpoint utiliza WebSocket para enviar actualizaciones en tiempo real a los clientes conectados.
/// </summary>
app.MapGet("/api/tasks/statistics", async (ApplicationDbContext dbContext, IHubContext<TaskHub> hubContext) =>
    {
        var stats = await dbContext.Tasks
            .GroupBy(t => 1)  // Un solo grupo para obtener las estadísticas
            .Select(g => new
            {
                TotalTasks = g.Count(),
                CompletedTasks = g.Count(t => t.IsCompleted),
                DeletedTasks = g.Count(t => t.IsDeleted),
                NotCompletedTasks = g.Count(t => !t.IsCompleted && !t.IsDeleted)
            })
            .FirstOrDefaultAsync();

        if (stats == null)
        {
            stats = new { TotalTasks = 0, CompletedTasks = 0, DeletedTasks = 0, NotCompletedTasks = 0 };
        }

        // Enviar actualización de estadísticas en tiempo real a todos los clientes conectados
        await hubContext.Clients.All.SendAsync("ReceiveStatisticsUpdate", stats);

        return Results.Ok(stats);
    })
    .WithMetadata(new SwaggerOperationAttribute("Obtiene estadísticas de las tareas", "Este endpoint devuelve las estadísticas generales de las tareas: total de tareas, tareas completadas, tareas eliminadas, etc."))
    .WithMetadata(new SwaggerResponseAttribute(200, "Devuelve las estadísticas de las tareas"))
    .WithMetadata(new SwaggerResponseAttribute(500, "Error al obtener las estadísticas"));

/// <summary>
/// Configura el Hub de SignalR para enviar estadísticas en tiempo real a través de WebSocket.
/// </summary>
app.MapHub<TaskHub>("/taskHub");

app.Run();

/// <summary>
/// Clase Hub de SignalR para enviar actualizaciones de estadísticas de tareas en tiempo real a los clientes conectados.
/// </summary>
public class TaskHub : Hub
{
    /// <summary>
    /// Método para enviar una actualización de estadísticas a todos los clientes.
    /// </summary>
    /// <param name="stats">El objeto con las estadísticas de tareas a enviar.</param>
    public async System.Threading.Tasks.Task SendStatisticsUpdate(object stats)
    {
        await Clients.All.SendAsync("ReceiveStatisticsUpdate", stats);
    }
}
