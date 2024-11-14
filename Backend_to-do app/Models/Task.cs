namespace Backend_to_do_app.Models
{
    /// <summary>
    /// Representa una tarea dentro de la aplicación de gestión de tareas.
    /// </summary>
    public class Task
    {
        /// <summary>
        /// Establece el identificador único de la tarea.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Establece el título o nombre de la tarea.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Indica si la tarea está completada.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Indica si la tarea está eliminada de forma lógica.
        /// </summary>
        /// <remarks>
        /// Esta propiedad se utiliza para manejar una eliminación lógica de la tarea,
        /// lo que permite que la tarea sea marcada como eliminada sin eliminarla de la base de datos.
        /// </remarks>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Establece la fecha y hora de creación de la tarea.
        /// </summary>
        /// <remarks>
        /// Esta propiedad se inicializa automáticamente con la fecha y hora actual al crear una nueva instancia de la tarea.
        /// </remarks>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}