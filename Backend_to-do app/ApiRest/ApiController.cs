using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_to_do_app.DB_Data;  
using TaskModel = Backend_to_do_app.Models.Task; 

/// <summary>
/// Controlador API para gestionar las tareas en la aplicación de gestión de tareas.
/// Proporciona endpoints para obtener, agregar, completar y eliminar tareas, así como para obtener estadísticas de las tareas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="TasksController"/> con el contexto de base de datos proporcionado.
    /// </summary>
    /// <param name="context">El contexto de base de datos utilizado para interactuar con la tabla de tareas.</param>
    public TasksController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene todas las tareas no eliminadas de la base de datos.
    /// </summary>
    /// <returns>Una lista de tareas no eliminadas.</returns>
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var tasks = await _context.Tasks.Where(t => !t.IsDeleted).ToListAsync();  
        return Ok(tasks);
    }

    /// <summary>
    /// Agrega una nueva tarea a la base de datos.
    /// </summary>
    /// <param name="newTask">Los datos de la nueva tarea a agregar.</param>
    /// <returns>La tarea recién creada.</returns>
    [HttpPost]
    public async Task<IActionResult> AddTask([FromBody] TaskModel newTask)
    {
        if (newTask == null)
        {
            return BadRequest("Invalid task data.");
        }

        _context.Tasks.Add(newTask);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTasks), new { id = newTask.Id }, newTask);
    }

    /// <summary>
    /// Marca una tarea como completada.
    /// </summary>
    /// <param name="id">El identificador de la tarea a completar.</param>
    /// <returns>Una respuesta vacía indicando éxito o un error si la tarea no fue encontrada.</returns>
    [HttpPut("{id}/complete")]
    public async Task<IActionResult> CompleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        task.IsCompleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Elimina lógicamente una tarea, marcándola como eliminada en lugar de eliminarla físicamente de la base de datos.
    /// </summary>
    /// <param name="id">El identificador de la tarea a eliminar.</param>
    /// <returns>Una respuesta vacía indicando éxito o un error si la tarea no fue encontrada.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        task.IsDeleted = true;  // Marca la tarea como eliminada
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Obtiene estadísticas sobre las tareas, incluyendo la cantidad de tareas completadas, no completadas y eliminadas.
    /// </summary>
    /// <returns>Un objeto con el número de tareas completadas, no completadas y eliminadas.</returns>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetTaskStatistics()
    {
        var totalTasks = await _context.Tasks.CountAsync();
        var completedTasks = await _context.Tasks.CountAsync(t => t.IsCompleted);
        var deletedTasks = await _context.Tasks.CountAsync(t => t.IsDeleted);
        var notCompletedTasks = totalTasks - completedTasks - deletedTasks;

        var stats = new
        {
            completed = completedTasks,
            notCompleted = notCompletedTasks,
            deleted = deletedTasks
        };

        return Ok(stats);
    }
}
