using System;

namespace IAMatematica;

class Program
{
    static void Main()
    {
        Console.WriteLine("__IA Matemática__");
        Console.WriteLine("Soy una IA personal dedicada a la matemática que aprende de ti.");
        Console.WriteLine("Ejemplos: suma 2 8 | resta 9 4 | multiplica 6 7 | divide 30 5");
        Console.WriteLine("Escribe 'ayuda' para ver todo lo que puedo hacer.");
        Console.WriteLine("Escribe 'salir' para terminar.\n");

        MemoriaUsuario memoria = MemoriaUsuario.Cargar();
        MotorMatematico motor = new(memoria);

        while (true)
        {
            Console.Write("Tú: ");
            string entrada = Console.ReadLine()?.Trim() ?? "";

            if (entrada.Equals("salir", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("IA: " + motor.Resolver("adiós"));
                break;
            }

            Console.WriteLine("IA: " + motor.Resolver(entrada));
            Console.WriteLine();
        }
    }
}