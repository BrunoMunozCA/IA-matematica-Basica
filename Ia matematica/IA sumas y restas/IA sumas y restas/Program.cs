// ============================================================================
// IA Matemática — Program.cs
// ============================================================================
// Punto de entrada de la aplicación de consola. Mantiene el bucle principal
// donde el usuario escribe y la IA responde, con el formato original:
//   Tú: <entrada>
//   IA:  <respuesta>
// ============================================================================

using System;

namespace IAMatematica;

class Program
{
    /// <summary>Punto de entrada. Ejecuta el bucle de conversación de la IA.</summary>
    static void Main()
    {
        Console.WriteLine("__IA Matemática__");
        Console.WriteLine("Soy una IA personal dedicada a la matemática que aprende de ti.");
        Console.WriteLine("Ejemplos: suma 2 8 | resta 9 4 | multiplica 6 7 | divide 30 5");
        Console.WriteLine("Escribe 'ayuda' para ver todo lo que puedo hacer.");
        Console.WriteLine("Escribe 'salir' para terminar.\n");

        // Cargar la memoria persistente (la "memoria" de la IA entre sesiones).
        MemoriaUsuario memoria = MemoriaUsuario.Cargar();
        MotorMatematico motor = new(memoria);

        // Bucle principal de la consola.
        while (true)
        {
            Console.Write("Tú: ");
            string entrada = Console.ReadLine()?.Trim() ?? "";

            // Salir del programa.
            if (entrada.Equals("salir", StringComparison.OrdinalIgnoreCase))
            {
                string despedida = motor.Resolver("adiós");
                Console.WriteLine("IA: " + despedida);
                break;
            }

            // Pedir al motor que interprete y responder.
            string respuesta = motor.Resolver(entrada);
            Console.WriteLine("IA: " + respuesta);
            Console.WriteLine();
        }
    }
}
