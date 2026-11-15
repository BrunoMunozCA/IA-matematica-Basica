// ============================================================================
// IA Matemática — MotorMatematico.cs
// ============================================================================
// Clase central que comprende el lenguaje del usuario, identifica comandos
// especiales de aprendizaje y resuelve operaciones matemáticas.
//
// Capacidades:
//   - Saludos y despedida.
//   - Aprender nombre ("me llamo Juan").
//   - Aprender respuestas ("aprende pregunta = respuesta").
//   - Guardar notas ("nota: ...", "recuerda que ...").
//   - Cambiar preferencia de decimales ("decimales 3").
//   - Consultar memoria ("que sabes de mi") y estadísticas ("estadisticas").
//   - Operaciones con N operandos: suma, resta, multiplicación, división.
//   - Operaciones avanzadas: potencia, raíz cuadrada, módulo, porcentaje,
//     promedio y factorial.
//   - Formas verbales y simbólicas: "suma 2 8", "2 + 8", "6 x 7", "20 por 5".
//   - Números decimales con coma o punto.
//   - Palabras de relleno ignoradas ("cuanto es", "calcula", ...).
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace IAMatematica;

/// <summary>
/// Motor matemático: interpreta texto del usuario y produce respuestas,
/// ya sean de saludo, resultados de operaciones o consultas sobre la memoria.
/// </summary>
public sealed class MotorMatematico
{
    // Referencia a la memoria para persistir cambios (nombre, aprendizaje, etc.).
    private readonly MemoriaUsuario _memoria;

    // Expresión regular para extraer números enteros o decimales (coma o punto).
    // Ejemplos: "1,5" -> 1.5 ; "3.14" -> 3.14 ; "7" -> 7.
    private static readonly Regex NumeroRegex =
        new(@"\d+(?:[.,]\d+)?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Expresión regular para detectar un guión que NO es parte de un número
    // negativo (evita confundir "resta" con un signo).
    private static readonly Regex GuionOperadorRegex =
        new(@"(?<=\d)-(?=\d)", RegexOptions.Compiled);

    // Expresión regular para detectar una "x" como multiplicación (no como letra).
    private static readonly Regex XMultiplicacionRegex =
        new(@"(?<=\d)\s*x\s*(?=\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Construye el motor vinculado a la memoria del usuario.</summary>
    /// <param name="memoria">Memoria persistente del usuario.</param>
    public MotorMatematico(MemoriaUsuario memoria) => _memoria = memoria;

    // Símbolos que se usan en la salida para cada operación (visivamente más claros).
    private const string SimboloSuma = " + ";
    private const string SimboloResta = " − ";
    private const string SimboloMult = " × ";
    private const string SimboloDiv = " ÷ ";
    private const string SimboloPot = " ^ ";

    /// <summary>Punto de entrada del motor: interpreta la entrada y devuelve la respuesta de la IA.</summary>
    /// <param name="entrada">Texto tal como lo escribió el usuario (ya en minúsculas y sin espacios extremos).</param>
    /// <response>Texto de respuesta listo para mostrar en consola.</response>
    public string Resolver(string entrada)
    {
        string texto = entrada.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(texto))
            return "¿Sí? Dime algo para ayudarte. Escribe 'ayuda' si no sabes qué.";

        // 1. Comandos especiales que gestionan la memoria (deben ir antes que todo).
        string comando = IntentarComandosEspeciales(texto);
        if (comando != null) return comando;

        // 2. Saludos y despedida (la salida ya no se hace, pero el saludo sí).
        if (IntentarSaludo(texto, out string saludo)) return saludo;

        // 3. Consulta sobre la memoria del usuario.
        if (texto.Contains("que sabes") || texto.Contains("que sabes de mi") || texto == "que sabes")
            return ConsultarMemoria();

        // 4. Estadísticas de uso.
        if (texto.Contains("estadistic") || texto.Contains("estadística"))
            return ConsultarEstadisticas();

        // 5. Consultar una respuesta aprendida ANTES de rendirse.
        string aprendida = IntentarRespuestaAprendida(texto);
        if (aprendida != null) return aprendida;

        // 6. Operaciones matemáticas (el corazón del proyecto).
        string resultado = IntentarResolverMatematica(texto);
        if (resultado != null) return resultado;

        // 7. Si nada funcionó, sugiere la ayuda.
        return "No te entendí. Prueba con: hola, como estas, suma 5 3, resta 10 4, " +
               "multiplica 6 7, divide 20 4, o escribe 'ayuda' para ver más opciones.";
    }

    // -----------------------------------------------------------------------
    // Comandos especiales de memoria y aprendizaje
    // -----------------------------------------------------------------------

    /// <summary>Interpreta los comandos de gestión de memoria (nombre, aprendizaje, notas, etc.).
    /// Devuelve la respuesta si la entrada es un comando conocido; otherwise, null.</summary>
    private string? IntentarComandosEspeciales(string texto)
    {
        // — Aprender nombre: "me llamo Juan" o "mi nombre es Juan".
        if (texto.StartsWith("me llamo ") || texto.Contains("me llamo "))
        {
            string nombre = ExtraerDespues(texto, "me llamo ").Trim();
            _memoria.Nombre = Capitalizar(nombre);
            _memoria.Guardar();
            return $"¡Mucho gusto, {_memoria.Nombre}! Estaré guardando todo en tu memoria.";
        }
        if (texto.StartsWith("mi nombre es ") || texto.Contains("mi nombre es "))
        {
            string nombre = ExtraerDespues(texto, "mi nombre es ").Trim();
            _memoria.Nombre = Capitalizar(nombre);
            _memoria.Guardar();
            return $"¡Encantado de conocerte, {_memoria.Nombre}!";
        }

        // — Aprender una respuesta: "aprende pregunta = respuesta".
        if (texto.StartsWith("aprende "))
        {
            return ProcesarAprendizaje(texto);
        }

        // — Notas: "nota: ..." o "recuerda que ...".
        if (texto.StartsWith("nota ") || texto.StartsWith("nota:"))
        {
            string nota = ExtraerDespues(texto, "nota:").Trim();
            if (nota.StartsWith("nota ")) nota = ExtraerDespues(texto, "nota ").Trim();
            if (string.IsNullOrWhiteSpace(nota))
                return "No anoté nada. Escribe: nota: tu mensaje aquí.";
            _memoria.Notas.Add(nota);
            _memoria.Guardar();
            return $"Nota guardada. Tienes {_memoria.Notas.Count} nota(s) en tu memoria.";
        }
        if (texto.StartsWith("recuerda que ") || texto.StartsWith("recuerda "))
        {
            string nota = ExtraerDespues(texto, "recuerda ").Trim();
            if (nota.StartsWith("que ")) nota = ExtraerDespues(texto, "recuerda que ").Trim();
            if (string.IsNullOrWhiteSpace(nota))
                return "No recordé nada. Escribe: recuerda que tu mensaje aquí.";
            _memoria.Notas.Add(nota);
            _memoria.Guardar();
            return $"Recuerdo: '{nota}'. (Notas totales: {_memoria.Notas.Count})";
        }

        // — Preferencia de decimales: "decimales 3".
        if (texto.Contains("decimales "))
        {
            string parte = ExtraerDespues(texto, "decimales ").Trim();
            if (int.TryParse(parte, out int n) && n >= 0 && n <= 15)
            {
                _memoria.PreferenciaDecimales = n;
                _memoria.Guardar();
                return $"Ahora mostraré hasta {n} decimales.";
            }
            return "No entendí la cantidad de decimales. Ejemplo: decimales 3";
        }

        // — Olvidar todo: "olvida todo".
        if (texto.Contains("olvida todo") || texto.Contains("borrar memoria"))
        {
            _memoria.OlvidarTodo();
            return "Memoria borrada. Empezamos de nuevo, sin recuerdos.";
        }

        // — Ayuda: "ayuda".
        if (texto.Contains("ayuda"))
            return DevolverAyuda();

        return null; // No fue un comando especial.
    }

    /// <summary>Procesa el comando "aprende pregunta = respuesta".</summary>
    /// <remarks>
    /// Material didáctico: se busca el separador " = " o "="; si hay varios, se toma
    /// el primer separador y se divide en dos partes.
    /// </remarks>
    private string ProcesarAprendizaje(string texto)
    {
        int sep = texto.IndexOf(" = ", StringComparison.Ordinal);
        if (sep < 0) sep = texto.IndexOf('=');
        if (sep <= 0)
            return "No entendí qué quiero aprender. Usa: aprende pregunta = respuesta";

        string pregunta = texto["aprende ".Length..sep].Trim();
        string respuesta;
        if (sep + 3 < texto.Length && texto[sep + 1] == ' ' && texto[sep + 2] == ' ')
            respuesta = texto[(sep + 3)..].Trim();
        else if (sep + 1 < texto.Length)
            respuesta = texto[(sep + 1)..].TrimStart(' ', '=');
        else
            respuesta = "";

        if (string.IsNullOrWhiteSpace(pregunta) || string.IsNullOrWhiteSpace(respuesta))
            return "No entendí qué quiero aprender. Usa: aprende pregunta = respuesta";

        _memoria.RespuestasAprendidas[pregunta] = respuesta;
        _memoria.Guardar();
        return $"Aprendí algo nuevo: cuando me preguntes '{pregunta}' te responderé '{respuesta}'.";
    }

    /// <summary>Devuelve un mensaje de ayuda con todos los comandos disponibles.</summary>
    private static string DevolverAyuda()
    {
        return @"Ayuda — IA Matemática:

Operaciones (aceptan N operandos):
  suma 1 2 3 | suma 1,5 2 | 2 + 8 | 1 + 2 + 3
  resta 10 4 | 9 - 3 | 20 - 5 - 2
  multiplica 6 7 | 6 x 7 | 20 por 5 | 2 * 3 * 4
  divide 30 5 | 30 / 6 | 100 / 5 / 2

Operaciones avanzadas:
  potencia 2 8 | 2 ^ 8 | 2 elevado a 8
  raiz cuadrada 9 | raiz 16 | raíz 25
  modulo 10 3 | 10 mod 3
  promedio 10 20 30 | media 4 8
  porcentaje 20 150 | porcentaje 10 200
  factorial 5 | factorial 0

Comandos de memoria:
  me llamo Juan / mi nombre es Juan   → recuerda tu nombre
  aprende pregunta = respuesta       → enseña algo
  nota: mensaje / recuerda que ...   → guarda una nota
  decimales 3                        → preferencia de decimales
  que sabes de mi / estadisticas     → consulta tu memoria
  olvida todo                        → borra la memoria
  ayuda                              → este mensaje

Palabras de relleno que se ignoran: cuanto es, calcula, resultado, por favor, ¿?, =, el, la.
Escribe 'salir' para terminar.";
    }

    // -----------------------------------------------------------------------
    // Saludos
    // -----------------------------------------------------------------------

    /// <summary>Detecta saludos y devuelve una respuesta, personalizada si hay nombre.</summary>
    private bool IntentarSaludo(string texto, out string saludo)
    {
        saludo = "";
        if (texto.Contains("hola") || texto == "hi" || texto == "hey" || texto.Contains("buenos"))
        {
            saludo = string.IsNullOrEmpty(_memoria.Nombre)
                ? "¡Hola! ¿En qué te puedo ayudar?"
                : $"¡Hola, {_memoria.Nombre}! ¿En qué te puedo ayudar hoy?";
            return true;
        }
        if (texto.Contains("como estas") || texto.Contains("cómo estás") ||
            texto.Contains("que tal") || texto.Contains("como vas"))
        {
            saludo = string.IsNullOrEmpty(_memoria.Nombre)
                ? "Estoy muy bien, gracias. ¿Y tú?"
                : $"Muy bien, gracias {_memoria.Nombre}. ¿Y tú?";
            return true;
        }
        if (texto.Contains("adiós") || texto.Contains("hasta luego") || texto.Contains("chao"))
        {
            saludo = string.IsNullOrEmpty(_memoria.Nombre)
                ? "¡Hasta luego! Vuelve cuando quieras."
                : $"¡Hasta luego, {_memoria.Nombre}! Vuelve cuando quieras.";
            return true;
        }
        return false;
    }

    // -----------------------------------------------------------------------
    // Consultas sobre la memoria
    // -----------------------------------------------------------------------

    /// <summary>Responde 'que sabes de mi' mostrando lo que la IA recuerda.</summary>
    private string ConsultarMemoria()
    {
        var partes = new List<string>();
        if (!string.IsNullOrEmpty(_memoria.Nombre))
            partes.Add($"te llamas {_memoria.Nombre}");
        partes.Add($"{_memoria.RespuestasAprendidas.Count} respuesta(s) aprendida(s)");
        partes.Add($"{_memoria.Notas.Count} nota(s) guardada(s)");
        partes.Add($"preferencia de {_memoria.PreferenciaDecimales} decimales");
        partes.Add($"{_memoria.ContadorOperaciones} operación(es) resuelta(s) en total");

        return _memoria.ContadorOperaciones == 0
            ? "Todavía no tengo mucha información de ti. ¡Enséñame algo!"
            : $"Sobre ti recuerdo: {string.Join("; ", partes)}.";
    }

    /// <summary>Responde 'estadisticas' mostrando el uso por tipo de operación.</summary>
    private string ConsultarEstadisticas()
    {
        if (_memoria.ContadorOperaciones == 0)
            return "No has resuelto ninguna operación todavía.";

        var linea = new List<string>();
        foreach (var kv in _memoria.OperacionesPorTipo.OrderBy(k => k.Key))
            linea.Add($"{kv.Key}: {kv.Value}");

        return $"Operaciones resueltas: {_memoria.ContadorOperaciones}. " +
               $"Desglose: {string.Join(", ", linea)}.";
    }

    // -----------------------------------------------------------------------
    // Respuestas aprendidas
    // -----------------------------------------------------------------------

    /// <summary>Busca en la memoria una respuesta aprendida que se parezca a la pregunta.
    /// Devuelve la respuesta si la encuentra; otherwise, null.
    /// Se invoca ANTES del mensaje de "no te entendí" para que la IA brille.</summary>
    private string? IntentarRespuestaAprendida(string texto)
    {
        if (_memoria.RespuestasAprendidas.Count == 0) return null;

        // Coincidencia directa.
        if (_memoria.RespuestasAprendidas.TryGetValue(texto, out string exacta))
            return exacta;

        // Coincidencia parcial: la pregunta del usuario contiene (o es contenida por)
        // una clave aprendida. Material didáctico: simple pero útil para un proyecto
        // de aprendizaje; en producción se usaría similitud semántica.
        foreach (var kv in _memoria.RespuestasAprendidas)
        {
            if (texto.Contains(kv.Key) || kv.Key.Contains(texto))
                return kv.Value;
        }
        return null;
    }

    // -----------------------------------------------------------------------
    // Operaciones matemáticas
    // -----------------------------------------------------------------------

    /// <summary>Intenta interpretar la entrada como una operación matemática.
    /// Devuelve el resultado formateado o null si no fue una operación.</summary>
    private string? IntentarResolverMatematica(string texto)
    {
        // Extraer números con coma o punto decimal.
        var (originales, valores) = ExtraerNumeros(texto);

        // Detectar tipo de operación y cuántos operandos requiere.
        (string operacion, int operandosNecesarios) =
            DetectarOperacion(texto, valores.Count);

        if (operacion == "nada" || valores.Count < operandosNecesarios)
            return null;

        // Resolver la operación.
        try
        {
            (string expresion, double resultado) =
                ResolverOperacion(operacion, originales, valores);
            if (resultado is double.NaN or double.PositiveInfinity or double.NegativeInfinity)
                return "El resultado no es un número válido.";

            // Contar en memoria (material didáctico: así la IA aprende qué se usa más).
            _memoria.ContadorOperaciones++;
            if (!_memoria.OperacionesPorTipo.ContainsKey(operacion))
                _memoria.OperacionesPorTipo[operacion] = 0;
            _memoria.OperacionesPorTipo[operacion]++;
            _memoria.Guardar();

            return $"{expresion} = {Formatear(resultado, _memoria.PreferenciaDecimales)}";
        }
        catch (ArgumentException ex)
        {
            return ex.Message; // mensajes de error amigables del validador
        }
    }

    /// <summary>Detecta la operación buscada y cuántos operandos necesita.</summary>
    private static (string operacion, int operandos) DetectarOperacion(
        string texto, int cantidadNumeros)
    {
        // Forma simbólica: buscar símbolos.
        if (texto.Contains("+")) return ("suma", 2);
        if (GuionOperadorRegex.IsMatch(texto) && cantidadNumeros >= 2) return ("resta", 2);
        if (texto.Contains("×") || XMultiplicacionRegex.IsMatch(texto) ||
            texto.Contains(" * ") || texto.Contains(" x ")) return ("multiplica", 2);
        if (texto.Contains("/") || texto.Contains("÷")) return ("divide", 2);
        if (texto.Contains("^")) return ("potencia", 2);

        // Palabras clave de operaciones con operandos variables.
        if (texto.Contains("potencia") || texto.Contains("elevado") || texto.Contains("pow"))
            return ("potencia", 2);
        if (texto.Contains("raíz") || texto.Contains("raiz") || texto.Contains("cuadrada"))
            return ("raiz", 1);
        if (texto.Contains("porcentaje")) return ("porcentaje", 2);
        if (texto.Contains("promedio") || texto.Contains("media"))
            return ("promedio", Math.Max(2, cantidadNumeros));
        if (texto.Contains("modulo") || texto.Contains("módulo") ||
            texto.Contains("residuo") || texto.Contains(" mod "))
            return ("modulo", 2);
        if (texto.Contains("factorial")) return ("factorial", 1);

        // Forma verbal: "20 por 5" significa multiplicar.
        if (texto.Contains(" por ") && cantidadNumeros >= 2) return ("multiplica", 2);

        // Verbos simples de operación (sin símbolo).
        var verbos = new Dictionary<string, (string op, int n)>
        {
            ["suma"] = ("suma", 2),
            ["sumar"] = ("suma", 2),
            ["resta"] = ("resta", 2),
            ["restar"] = ("resta", 2),
            ["multiplica"] = ("multiplica", 2),
            ["multiplicar"] = ("multiplica", 2),
            ["divide"] = ("divide", 2),
            ["dividir"] = ("divide", 2),
        };
        foreach (var kv in verbos)
            if (texto.Contains(kv.Key)) return kv.Value;

        return ("nada", 0);
    }

    /// <summary>
    /// Resuelve una operación identificada y devuelve (expresión legible, resultado).
    /// </summary>
    /// <exception cref="ArgumentException">Cuando los argumentos son inválidos.</exception>
    private static (string expresion, double resultado) ResolverOperacion(
        string operacion, List<string> originales, List<double> valores)
    {
        // Se usa una sentencia switch clásica (no una "switch expression" con =>).
        // Motivo: dentro de una switch expression cada rama debe ser UNA expresión;
        // no se pueden abrir llaves { } con varias instrucciones dentro (for, if, return).
        // Esa era exactamente la causa del error "'{' no es válido".
        switch (operacion)
        {
            case "suma":
                return (
                    string.Join(SimboloSuma, originales),
                    valores.Aggregate((a, b) => a + b));

            case "resta":
                return (
                    string.Join(SimboloResta, originales),
                    valores.Aggregate((a, b) => a - b));

            case "multiplica":
                return (
                    string.Join(SimboloMult, originales),
                    valores.Aggregate((a, b) => a * b));

            case "divide":
                // Validación: no dividir entre cero (material didáctico).
                for (int i = 1; i < valores.Count; i++)
                    if (valores[i] == 0)
                        throw new ArgumentException("No se puede dividir entre cero.");
                return (
                    string.Join(SimboloDiv, originales),
                    valores.Aggregate((a, b) => a / b));

            case "potencia":
                return (
                    $"{originales[0]}{SimboloPot}{originales[1]}",
                    Math.Pow(valores[0], valores[1]));

            case "raiz":
                if (valores[0] < 0)
                    throw new ArgumentException("No se puede calcular raíz de un número negativo.");
                return ($"√{originales[0]}", Math.Sqrt(valores[0]));

            case "porcentaje":
                return (
                    $"{originales[0]}% de {originales[1]}",
                    valores[0] / 100.0 * valores[1]);

            case "promedio":
                return (
                    string.Join(" + ", originales) + " (prom)",
                    valores.Average());

            case "modulo":
                if (valores[1] == 0)
                    throw new ArgumentException("No se puede calcular módulo entre cero.");
                return (
                    $"{originales[0]} mod {originales[1]}",
                    valores[0] % valores[1]);

            case "factorial":
                int n = (int)Math.Round(valores[0]);
                if (n < 0 || n > 20)
                    throw new ArgumentException(
                        "Factorial solo válido entre 0 y 20 (para evitar desbordamiento).");
                long f = 1;
                for (int i = 2; i <= n; i++) f *= i;
                return ($"{n}!", f);

            default:
                throw new ArgumentException("Operación no reconocida internamente.");
        }
    }

    /// <summary>Extrae todos los números del texto con sus originales (coma o punto).</summary>
    private static (List<string> originales, List<double> valores) ExtraerNumeros(string texto)
    {
        var originales = new List<string>();
        var valores = new List<double>();
        foreach (Match m in NumeroRegex.Matches(texto))
        {
            originales.Add(m.Value);
            if (double.TryParse(m.Value.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out double n))
                valores.Add(n);
        }
        return (originales, valores);
    }

    /// <summary>Formatea un número con la precisión del usuario, coma decimal y sin ceros sobrantes.</summary>
    /// <remarks>
    /// Material didáctico: se formatea primero con puntos (InvariantCulture) para las
    /// operaciones matemáticas, y al final se reemplaza el separador por coma para la
    /// salida en español. Luego se recortan los ceros finales.
    /// </remarks>
    private static string Formatear(double valor, int precision)
    {
        string s = valor.ToString($"F{precision}", CultureInfo.InvariantCulture);
        s = s.TrimEnd('0').TrimEnd('.');
        if (string.IsNullOrEmpty(s) || s == "-") s = "0";
        s = s.Replace('.', ',');
        return s;
    }

    /// <summary>Capitaliza la primera letra del texto (para el nombre del usuario).</summary>
    private static string Capitalizar(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        return char.ToUpper(texto[0]) + texto.Substring(1).Trim();
    }

    /// <summary>Devuelve el fragmento que aparece después de una subcadena clave.</summary>
    private static string ExtraerDespues(string texto, string clave)
    {
        int idx = texto.IndexOf(clave, StringComparison.Ordinal);
        if (idx < 0) return "";
        return texto[(idx + clave.Length)..].Trim();
    }
}

