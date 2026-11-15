# IA Matemática

Asistente de consola escrito en **C#** que entiende peticiones matemáticas en lenguaje natural (español), resuelve operaciones básicas y avanzadas, y **recuerda cosas de ti entre sesiones** gracias a una memoria persistente en JSON.

```
__IA Matemática__
Soy una IA personal dedicada a la matemática que aprende de ti.

Tú: me llamo ana
IA: ¡Mucho gusto, Ana! Estaré guardando todo en tu memoria.

Tú: suma 2 8
IA: 2 + 8 = 10

Tú: 20 por 5
IA: 20 × 5 = 100

Tú: aprende capital de chile = santiago
IA: Aprendí algo nuevo: cuando me preguntes 'capital de chile' te responderé 'santiago'.
```

> Nota: la "IA" de este proyecto es un motor basado en reglas (palabras clave y expresiones regulares), pensado como proyecto didáctico. No utiliza modelos de aprendizaje automático.

---

## Características

- **Operaciones con N operandos:** suma, resta, multiplicación y división.
- **Operaciones avanzadas:** potencia, raíz cuadrada, módulo, porcentaje, promedio y factorial.
- **Lenguaje flexible:** acepta formas verbales (`suma 2 8`), simbólicas (`2 + 8`, `6 x 7`, `2 ^ 8`) y coloquiales (`20 por 5`, `2 elevado a 8`).
- **Decimales con coma o punto:** `1,5` y `1.5` son equivalentes. La salida usa coma decimal.
- **Memoria persistente:** nombre, notas, respuestas aprendidas, preferencia de decimales y estadísticas de uso.
- **Aprendizaje de respuestas:** enséñale pares pregunta/respuesta con `aprende pregunta = respuesta`.
- **Manejo de errores amigable:** división entre cero, raíz de negativos, factorial fuera de rango, etc.

## Requisitos

- [.NET SDK 6.0 o superior](https://dotnet.microsoft.com/download) (el código usa *namespaces* de ámbito de archivo y `new()` con tipo de destino, disponibles desde C# 10 / .NET 6).

## Instalación y ejecución

Si el repositorio ya incluye un archivo `.csproj`, basta con:

```bash
git clone https://github.com/BrunoMunozCA/IA-matematica-Basica.git
cd "IA-matematica-Basica/Ia matematica/IA sumas y restas/IA sumas y restas"
dotnet run
```

Si solo tienes los archivos `.cs`, crea un proyecto de consola y copia los archivos dentro:

```bash
dotnet new console -n IAMatematica
cd IAMatematica
# Reemplaza Program.cs y añade MotorMatematico.cs y MemoriaUsuario.cs
dotnet run
```

## Uso

Escribe tus peticiones después de `Tú:`. La IA responde con `IA:`. Para terminar, escribe `salir`.

### Operaciones

| Tipo | Ejemplos |
|---|---|
| Suma | `suma 1 2 3`, `suma 1,5 2`, `2 + 8`, `1 + 2 + 3` |
| Resta | `resta 10 4`, `9-3`, `20-5-2` |
| Multiplicación | `multiplica 6 7`, `6 x 7`, `20 por 5`, `2 * 3 * 4` |
| División | `divide 30 5`, `30 / 6`, `100 / 5 / 2` |
| Potencia | `potencia 2 8`, `2 ^ 8`, `2 elevado a 8` |
| Raíz cuadrada | `raiz cuadrada 9`, `raiz 16`, `raíz 25` |
| Módulo | `modulo 10 3`, `10 mod 3` |
| Promedio | `promedio 10 20 30`, `media 4 8` |
| Porcentaje | `porcentaje 20 150` (20 % de 150) |
| Factorial | `factorial 5`, `factorial 0` (válido de 0 a 20) |

Las palabras de relleno como `cuanto es`, `calcula` o `por favor` se ignoran, porque el motor solo extrae los números y detecta la operación.

### Comandos de memoria

| Comando | Qué hace |
|---|---|
| `me llamo Juan` / `mi nombre es Juan` | Guarda tu nombre y la IA te saluda por él. |
| `aprende pregunta = respuesta` | Enseña una respuesta personalizada. |
| `nota: mensaje` / `recuerda que ...` | Guarda una nota personal. |
| `decimales 3` | Cambia la cantidad máxima de decimales mostrados (0 a 15). |
| `que sabes de mi` | Muestra un resumen de lo que la IA recuerda. |
| `estadisticas` | Muestra cuántas operaciones has resuelto, por tipo. |
| `olvida todo` / `borrar memoria` | Borra la memoria (conserva la preferencia de decimales en la sesión actual). |
| `ayuda` | Muestra la ayuda dentro de la aplicación. |
| `salir` | Termina el programa. |

## Dónde se guarda la memoria

La memoria se guarda en formato JSON en dos ubicaciones:

1. **Principal:** `%AppData%/IA-Matematica/memoria.json` (en Windows; en Linux/macOS corresponde a la carpeta de datos de aplicación del usuario).
2. **Respaldo:** `memoria.json` en el directorio desde el que ejecutas el programa.

Ejemplo de contenido:

```json
{
  "Nombre": "Ana",
  "RespuestasAprendidas": { "capital de chile": "santiago" },
  "Notas": ["comprar cuaderno"],
  "PreferenciaDecimales": 2,
  "ContadorOperaciones": 3,
  "OperacionesPorTipo": { "suma": 2, "multiplica": 1 }
}
```

## Estructura del proyecto

```
IA sumas y restas/
├── Program.cs           # Punto de entrada y bucle de conversación
├── MotorMatematico.cs   # Interpretación del texto y resolución de operaciones
└── MemoriaUsuario.cs    # Modelo de memoria y persistencia en JSON
```

| Archivo | Responsabilidad |
|---|---|
| `Program.cs` | Muestra la bienvenida, carga la memoria, lee la entrada del usuario y muestra la respuesta del motor. |
| `MotorMatematico.cs` | Clase central: detecta comandos, saludos, consultas, respuestas aprendidas y operaciones matemáticas. |
| `MemoriaUsuario.cs` | Guarda y carga el estado del usuario (nombre, notas, respuestas, estadísticas). |

Para una explicación detallada de cada clase y del flujo interno, consulta [DOCUMENTACION.md](DOCUMENTACION.md).

## Limitaciones conocidas

- No se soportan números negativos como operandos (el motor solo reconoce dígitos).
- La resta con símbolo requiere que el guión esté pegado a los números (`9-3`); con espacios (`9 - 3`) no se detecta.
- La entrada se convierte a minúsculas, por lo que nombres y respuestas aprendidas se guardan en minúsculas (el nombre se capitaliza solo en la primera letra).
- Con `decimales 0`, los resultados enteros terminados en cero pueden mostrarse recortados (por ejemplo, `100` como `1`).
- La comprensión del lenguaje se basa en palabras clave, no en comprensión semántica.

## Autor

Proyecto de [BrunoMunozCA](https://github.com/BrunoMunozCA).
