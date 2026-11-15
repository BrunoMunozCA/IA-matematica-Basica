// ============================================================================
// IA Matemática — MemoriaUsuario.cs
// ============================================================================
// Clase que modela la memoria persistente del usuario. Almacena el nombre,
// las respuestas personalizadas que la IA aprende, las notas, la preferencia
// de decimales y las estadísticas de uso de operaciones.
// La memoria se serializa a JSON y se persiste en dos ubicaciones:
//   1. Carpeta de datos de la aplicación (%AppData%/IA-Matematica/memoria.json)
//   2. Directorio de trabajo actual (respaldo local, "memoria.json")
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace IAMatematica;

/// <summary>
/// Representa la memoria del usuario, persistida en un archivo JSON.
/// Se carga al iniciar la aplicación y se guarda tras cada modificación,
/// de modo que la IA "recuerde" a la persona entre ejecuciones.
/// </summary>
public sealed class MemoriaUsuario
{
    // Nombre que la persona le dio a la IA para recordarla ("me llamo Juan").
    /// <summary>Nombre de la persona usuario.</summary>
    public string Nombre { get; set; } = "";

    /// <summary>
    /// Respuestas que la persona le enseñó a la IA mediante el comando
    /// "aprende pregunta = respuesta". La clave es la pregunta.
    /// </summary>
    public Dictionary<string, string> RespuestasAprendidas { get; set; } = new();

    /// <summary>Notas personales que la persona quiera guardar ("nota: ...").</summary>
    public List<string> Notas { get; set; } = new();

    /// <summary>Cantidad de decimales que el usuario prefiere mostrar (por defecto 2).</summary>
    public int PreferenciaDecimales { get; set; } = 2;

    /// <summary>Contador total de operaciones matemáticas resueltas.</summary>
    public int ContadorOperaciones { get; set; }

    /// <summary>
    /// Cuántas veces se usó cada tipo de operación. La clave es el nombre
    /// de la operación ("suma", "resta", "multiplica", ...).
    /// </summary>
    public Dictionary<string, int> OperacionesPorTipo { get; set; } = new();

    // Vías de persistencia.
    /// <summary>Ruta principal de la memoria: %AppData%/IA-Matematica/memoria.json</summary>
    private static string RutaPrincipal
    {
        get
        {
            string carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "IA-Matematica");
            Directory.CreateDirectory(carpeta); // garantiza que existe
            return Path.Combine(carpeta, "memoria.json");
        }
    }

    /// <summary>Ruta de respaldo: memoria.json en el directorio de trabajo actual.</summary>
    private static string RutaRespaldo =>
        Path.Combine(Directory.GetCurrentDirectory(), "memoria.json");

    /// <summary>
    /// Carga la memoria desde la ruta principal; si no existe o es inválida,
    /// intenta cargarla desde el respaldo. Si todo falla, devuelve una memoria
    /// nueva y vacía (sin perder la ejecución).
    /// </summary>
    /// <remarks>Material didáctico: este patrón (try-primario, try-respaldo, fallback)
    /// es una buena práctica para archivos de configuración.</remarks>
    public static MemoriaUsuario Cargar()
    {
        try
        {
            if (File.Exists(RutaPrincipal))
                return Deserializar(File.ReadAllText(RutaPrincipal));
        }
        catch (Exception ex)
        {
            // Material didáctico: se intenta el respaldo antes de rendirse.
            Console.Error.WriteLine(
                $"IA: No se pudo cargar la memoria principal ({ex.Message}); probando respaldo...");
        }

        try
        {
            if (File.Exists(RutaRespaldo))
                return Deserializar(File.ReadAllText(RutaRespaldo));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"IA: No se pudo cargar el respaldo de memoria ({ex.Message}).");
        }

        // Si no hay memoria, se inicia una nueva (la IA aprende desde cero).
        Console.WriteLine("IA: Estrenando memoria nueva. ¡Vamos a aprender!");
        return new MemoriaUsuario();
    }

    /// <summary>Salva la memoria en la ruta principal y en el respaldo.</summary>
    public void Guardar()
    {
        try
        {
            string json = Serializar(this);
            File.WriteAllText(RutaPrincipal, json);
            // Respaldo en directorio actual para mayor seguridad.
            try { File.WriteAllText(RutaRespaldo, json); } catch { /* no crítico */ }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"IA: Error al guardar memoria: {ex.Message}");
        }
    }

    /// <summary>Borra toda la memoria (comando "olvida todo").</summary>
    public void OlvidarTodo()
    {
        Nombre = "";
        RespuestasAprendidas.Clear();
        Notas.Clear();
        ContadorOperaciones = 0;
        OperacionesPorTipo.Clear();
        // Preferencia de decimales no se resetea para no molestar al usuario.

        try
        {
            // Se borra el archivo principal y el respaldo para evitar datos huérfanos.
            if (File.Exists(RutaPrincipal)) File.Delete(RutaPrincipal);
            if (File.Exists(RutaRespaldo)) File.Delete(RutaRespaldo);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"IA: No se pudo borrar memoria: {ex.Message}");
        }
    }

    /// <summary>Serializa la memoria a JSON con sangrado legible.</summary>
    private static string Serializar(MemoriaUsuario m) =>
        JsonSerializer.Serialize(m, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null // mantiene los nombres de propiedades tal cual
        });

    /// <summary>Deserializa JSON a objeto MemoriaUsuario.</summary>
    private static MemoriaUsuario Deserializar(string json) =>
        JsonSerializer.Deserialize<MemoriaUsuario>(json) ?? new MemoriaUsuario();
}

