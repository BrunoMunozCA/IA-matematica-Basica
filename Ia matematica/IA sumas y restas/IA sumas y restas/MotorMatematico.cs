using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace IAMatematica;

public sealed class MotorMatematico
{
    private readonly MemoriaUsuario _memoria;

    private static readonly Regex NumeroRegex =
        new(@"\d+(?:[.,]\d+)?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex GuionOperadorRegex =
        new(@"(?<=\d)-(?=\d)", RegexOptions.Compiled);

    private static readonly Regex XMultiplicacionRegex =
        new(@"(?<=\d)\s*x\s*(?=\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const string SimboloSuma = " + ";
    private const string SimboloResta = " − ";
    private const string SimboloMult = " × ";
    private const string SimboloDiv = " ÷ ";
    private const string SimboloPot = " ^ ";

    public MotorMatematico(MemoriaUsuario memoria) => _memoria = memoria;

    public string Resolver(string entrada)
    {
        string texto = entrada.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(texto))
            return "¿Sí? Dime algo para ayudarte. Escribe 'ayuda' si no sabes qué.";

        string? comando = IntentarComandosEspeciales(texto);
        if (comando != null) return comando;

        if (IntentarSaludo(texto, out string saludo)) return saludo;

        if (texto.Contains("que sabes"))
            return ConsultarMemoria();

        if (texto.Contains("estadistic") || texto.Contains("estadística"))
            return ConsultarEstadisticas();

        string? aprendida = IntentarRespuestaAprendida(texto);
        if (aprendida != null) return aprendida;

        string? resultado = IntentarResolverMatematica(texto);
        if (resultado != null) return resultado;

        return "No te entendí. Prueba con: hola, como estas, suma 5 3, resta 10 4, " +
               "multiplica 6 7, divide 20 4, o escribe 'ayuda' para ver más opciones.";
    }

    private string? IntentarComandosEspeciales(string texto)
    {
        if (texto.Contains("me llamo "))
        {
            string nombre = ExtraerDespues(texto, "me llamo ").Trim();
            _memoria.Nombre = Capitalizar(nombre);
            _memoria.Guardar();
            return $"¡Mucho gusto, {_memoria.Nombre}! Estaré guardando todo en tu memoria.";
        }
        if (texto.Contains("mi nombre es "))
        {
            string nombre = ExtraerDespues(texto, "mi nombre es ").Trim();
            _memoria.Nombre = Capitalizar(nombre);
            _memoria.Guardar();
            return $"¡Encantado de conocerte, {_memoria.Nombre}!";
        }

        if (texto.StartsWith("aprende "))
            return ProcesarAprendizaje(texto);

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
        if (texto.StartsWith("recuerda "))
        {
            string nota = ExtraerDespues(texto, "recuerda ").Trim();
            if (nota.StartsWith("que ")) nota = ExtraerDespues(texto, "recuerda que ").Trim();
            if (string.IsNullOrWhiteSpace(nota))
                return "No recordé nada. Escribe: recuerda que tu mensaje aquí.";
            _memoria.Notas.Add(nota);
            _memoria.Guardar();
            return $"Recuerdo: '{nota}'. (Notas totales: {_memoria.Notas.Count})";
        }

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

        if (texto.Contains("olvida todo") || texto.Contains("borrar memoria"))
        {
            _memoria.OlvidarTodo();
            return "Memoria borrada. Empezamos de nuevo, sin recuerdos.";
        }

        if (texto.Contains("ayuda"))
            return DevolverAyuda();

        return null;
    }

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

    private static string DevolverAyuda()
    {
        return @"Ayuda - IA Matemática:

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
  me llamo Juan / mi nombre es Juan   - recuerda tu nombre
  aprende pregunta = respuesta        - enseña algo
  nota: mensaje / recuerda que ...    - guarda una nota
  decimales 3                         - preferencia de decimales
  que sabes de mi / estadisticas      - consulta tu memoria
  olvida todo                         - borra la memoria
  ayuda                               - este mensaje

Escribe 'salir' para terminar.";
    }

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

    private string ConsultarEstadisticas()
    {
        if (_memoria.ContadorOperaciones == 0)
            return "No has resuelto ninguna operación todavía.";

        var lineas = _memoria.OperacionesPorTipo
            .OrderBy(k => k.Key)
            .Select(kv => $"{kv.Key}: {kv.Value}");

        return $"Operaciones resueltas: {_memoria.ContadorOperaciones}. " +
               $"Desglose: {string.Join(", ", lineas)}.";
    }

    private string? IntentarRespuestaAprendida(string texto)
    {
        if (_memoria.RespuestasAprendidas.Count == 0) return null;

        if (_memoria.RespuestasAprendidas.TryGetValue(texto, out string? exacta))
            return exacta;

        foreach (var kv in _memoria.RespuestasAprendidas)
        {
            if (texto.Contains(kv.Key) || kv.Key.Contains(texto))
                return kv.Value;
        }
        return null;
    }

    private string? IntentarResolverMatematica(string texto)
    {
        var (originales, valores) = ExtraerNumeros(texto);
        var (operacion, operandosNecesarios) = DetectarOperacion(texto, valores.Count);

        if (operacion == "nada" || valores.Count < operandosNecesarios)
            return null;

        try
        {
            var (expresion, resultado) = ResolverOperacion(operacion, originales, valores);
            if (double.IsNaN(resultado) || double.IsInfinity(resultado))
                return "El resultado no es un número válido.";

            _memoria.ContadorOperaciones++;
            _memoria.OperacionesPorTipo[operacion] =
                _memoria.OperacionesPorTipo.GetValueOrDefault(operacion) + 1;
            _memoria.Guardar();

            return $"{expresion} = {Formatear(resultado, _memoria.PreferenciaDecimales)}";
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
    }

    private static (string operacion, int operandos) DetectarOperacion(string texto, int cantidadNumeros)
    {
        if (texto.Contains("+")) return ("suma", 2);
        if (GuionOperadorRegex.IsMatch(texto) && cantidadNumeros >= 2) return ("resta", 2);
        if (texto.Contains("×") || XMultiplicacionRegex.IsMatch(texto) ||
            texto.Contains(" * ") || texto.Contains(" x ")) return ("multiplica", 2);
        if (texto.Contains("/") || texto.Contains("÷")) return ("divide", 2);
        if (texto.Contains("^")) return ("potencia", 2);

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

        if (texto.Contains(" por ") && cantidadNumeros >= 2) return ("multiplica", 2);

        var verbos = new Dictionary<string, string>
        {
            ["suma"] = "suma",
            ["sumar"] = "suma",
            ["resta"] = "resta",
            ["restar"] = "resta",
            ["multiplica"] = "multiplica",
            ["multiplicar"] = "multiplica",
            ["divide"] = "divide",
            ["dividir"] = "divide",
        };
        foreach (var kv in verbos)
            if (texto.Contains(kv.Key)) return (kv.Value, 2);

        return ("nada", 0);
    }

    private static (string expresion, double resultado) ResolverOperacion(
        string operacion, List<string> originales, List<double> valores)
    {
        switch (operacion)
        {
            case "suma":
                return (string.Join(SimboloSuma, originales), valores.Aggregate((a, b) => a + b));

            case "resta":
                return (string.Join(SimboloResta, originales), valores.Aggregate((a, b) => a - b));

            case "multiplica":
                return (string.Join(SimboloMult, originales), valores.Aggregate((a, b) => a * b));

            case "divide":
                for (int i = 1; i < valores.Count; i++)
                    if (valores[i] == 0)
                        throw new ArgumentException("No se puede dividir entre cero.");
                return (string.Join(SimboloDiv, originales), valores.Aggregate((a, b) => a / b));

            case "potencia":
                return ($"{originales[0]}{SimboloPot}{originales[1]}", Math.Pow(valores[0], valores[1]));

            case "raiz":
                if (valores[0] < 0)
                    throw new ArgumentException("No se puede calcular raíz de un número negativo.");
                return ($"√{originales[0]}", Math.Sqrt(valores[0]));

            case "porcentaje":
                return ($"{originales[0]}% de {originales[1]}", valores[0] / 100.0 * valores[1]);

            case "promedio":
                return (string.Join(" + ", originales) + " (prom)", valores.Average());

            case "modulo":
                if (valores[1] == 0)
                    throw new ArgumentException("No se puede calcular módulo entre cero.");
                return ($"{originales[0]} mod {originales[1]}", valores[0] % valores[1]);

            case "factorial":
                int n = (int)Math.Round(valores[0]);
                if (n < 0 || n > 20)
                    throw new ArgumentException("Factorial solo válido entre 0 y 20.");
                long f = 1;
                for (int i = 2; i <= n; i++) f *= i;
                return ($"{n}!", f);

            default:
                throw new ArgumentException("Operación no reconocida.");
        }
    }

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

    private static string Formatear(double valor, int precision)
    {
        string s = valor.ToString($"F{precision}", CultureInfo.InvariantCulture);
        if (s.Contains('.'))
            s = s.TrimEnd('0').TrimEnd('.');
        if (string.IsNullOrEmpty(s) || s == "-") s = "0";
        return s.Replace('.', ',');
    }

    private static string Capitalizar(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        return char.ToUpper(texto[0]) + texto.Substring(1).Trim();
    }

    private static string ExtraerDespues(string texto, string clave)
    {
        int idx = texto.IndexOf(clave, StringComparison.Ordinal);
        if (idx < 0) return "";
        return texto[(idx + clave.Length)..].Trim();
    }
}
