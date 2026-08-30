using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("__IA Matematica Basica__");
        Console.WriteLine("Solo sirve para hacer operaciones matematicas básicas");
        Console.WriteLine("Ejemplos de operaciones: suma 2 8 | resta 9 4 | multiplica 6 7 | divide 30 5");
        Console.WriteLine("Escribe 'salir' para terminar.\n");

        while (true)
        {
            Console.Write("Tú: ");
            string entrada = Console.ReadLine()?.Trim().ToLower() ?? "";

            if (entrada == "salir")
            {
                Console.WriteLine("IA: ¡Hasta luego!");
                break;
            }

            string respuesta = Responder(entrada);
            Console.WriteLine("IA: " + respuesta);
            Console.WriteLine();
        }
    }

    static string Responder(string texto)
    {
        // --- Saludos ---
        if (texto.Contains("hola") || texto == "hi" || texto == "hey")
            return "¡Hola! ¿En qué te puedo ayudar?";

        if (texto.Contains("como estas") || texto.Contains("cómo estás") || texto.Contains("que tal"))
            return "Estoy muy bien, gracias. ¿Y tú?";

        // --- Operaciones matemáticas ---
        // Formato esperado: "suma 5 3", "resta 10 4", etc.
        string[] partes = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (partes.Length >= 3)
        {
            string operacion = partes[0];

            if (double.TryParse(partes[1], out double a) && double.TryParse(partes[2], out double b))
            {
                switch (operacion)
                {
                    case "suma":
                    case "sumar":
                    case "+":
                        return $"{a} + {b} = {a + b}";

                    case "resta":
                    case "restar":
                    case "-":
                        return $"{a} - {b} = {a - b}";

                    case "multiplica":
                    case "multiplicar":
                    case "*":
                    case "x":
                        return $"{a} × {b} = {a * b}";

                    case "divide":
                    case "dividir":
                    case "/":
                        if (b == 0)
                            return "No se puede dividir entre cero.";
                        return $"{a} ÷ {b} = {a / b}";
                }
            }
        }

        // Si no entiende nada
        return "No te entendí. Prueba con: hola, como estas, suma 5 3, resta 10 4, multiplica 6 7, divide 20 4";
    }
}