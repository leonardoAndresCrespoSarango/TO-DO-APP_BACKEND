namespace Backend_to_do_app.DB_Data
{
    using Backend_to_do_app.Models;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Representa el contexto de base de datos para la app.
    /// Proporciona acceso a las entidades y permite configurar las propiedades y relaciones de la base de datos.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ApplicationDbContext"/> con las opciones especificadas.
        /// </summary>
        /// <param name="options">Las opciones para configurar el contexto de base de datos.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Obtiene o establece la tabla de tareas en la base de datos.
        /// </summary>
        public DbSet<Task> Tasks { get; set; }

        /// <summary>
        /// Configura el modelo de datos para la base de datos mediante la definición de restricciones y propiedades de la entidad.
        /// </summary>
        /// <param name="modelBuilder">El generador de modelos que se utiliza para configurar las entidades de la base de datos.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la entidad Task
            modelBuilder.Entity<Task>(entity =>
            {
                /// <summary>
                /// Configura la clave primaria para la entidad <see cref="Task"/>.
                /// </summary>
                entity.HasKey(t => t.Id);

                /// <summary>
                /// Configura la propiedad Title como obligatoria.
                /// </summary>
                entity.Property(t => t.Title).IsRequired();

                /// <summary>
                /// Configura el valor predeterminado de la propiedad CreatedAt para que sea la fecha y hora actual.
                /// </summary>
                entity.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
