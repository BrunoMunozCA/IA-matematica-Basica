using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace IAMatematica;

public sealed class MemoriaUsuario
{
    public string Nombre { get; set; } = "";
    public Dictionary<string, string> RespuestasAprendidas { get; set; } = new();
    public List<string> Notas { get; set; } = new();
    public int PreferenciaDecimales { get; set; } = 2;
    public int ContadorOperaciones { get; set; }
    public Dictionary<string, int> OperacionesPorTipo { get; set; } = new();

    private static string RutaPrincipal
    {
        get
        {
            string carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "IA-Matematica");
            Directory.CreateDirectory(carpeta);
            return Path.Combine(carpeta, "memoria.json");
        }
    }

    private static string RutaRespaldo =>
        Path.Combine(Directory.GetCurrentDirectory(), "memoria.json");

    public static MemoriaUsuario Cargar()
    {
        try
        {
            if (File.Exists(RutaPrincipal))
                return Deserializar(File.ReadAllText(RutaPrincipal));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"IA: No se pudo cargar la memoria principal ({ex.Message}); probando respaldo...");
        }

        try
        {
            if (File.Exists(RutaRespaldo))
                return Deserializar(File.ReadAllText(RutaRespaldo));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"IA: No se pudo cargar el respaldo de memoria ({ex.Message}).");
        }

        Console.WriteLine("IA: Estrenando memoria nueva. ¡Vamos a aprender!");
        return new MemoriaUsuario();
    }

    public void Guardar()
    {
        try
        {
            string json = Serializar(this);
            File.WriteAllText(RutaPrincipal, json);
            try { File.WriteAllText(RutaRespaldo, json); } catch { }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"IA: Error al guardar memoria: {ex.Message}");
        }
    }

    public void OlvidarTodo()
    {
        Nombre = "";
        RespuestasAprendidas.Clear();
        Notas.Clear();
        ContadorOperaciones = 0;
        OperacionesPorTipo.Clear();

        try
        {
            if (File.Exists(RutaPrincipal)) File.Delete(RutaPrincipal);
            if (File.Exists(RutaRespaldo)) File.Delete(RutaRespaldo);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"IA: No se pudo borrar memoria: {ex.Message}");
        }
    }

    private static string Serializar(MemoriaUsuario m) =>
        JsonSerializer.Serialize(m, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        });

    private static MemoriaUsuario Deserializar(string json) =>
        JsonSerializer.Deserialize<MemoriaUsuario>(json) ?? new MemoriaUsuario();
}
