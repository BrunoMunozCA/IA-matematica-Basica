# Documentación técnica — IA Matemática

Este documento describe la arquitectura, el flujo de ejecución y cada clase del proyecto **IA Matemática**, una aplicación de consola en C# que interpreta peticiones matemáticas en español y mantiene una memoria persistente del usuario.

## Índice

1. [Visión general](#1-visión-general)
2. [Arquitectura](#2-arquitectura)
3. [Flujo de ejecución](#3-flujo-de-ejecución)
4. [Program.cs](#4-programcs)
5. [MemoriaUsuario.cs](#5-memoriausuariocs)
6. [MotorMatematico.cs](#6-motormatematicocs)
7. [Formato y reglas de salida](#7-formato-y-reglas-de-salida)
8. [Manejo de errores](#8-manejo-de-errores)
9. [Limitaciones y problemas conocidos](#9-limitaciones-y-problemas-conocidos)
10. [Cómo extender el proyecto](#10-cómo-extender-el-proyecto)

---

## 1. Visión general

| Aspecto | Detalle |
|---|---|
| Lenguaje | C# (requiere C# 10 / .NET 6 o superior) |
| Tipo de aplicación | Consola |
| Namespace | `IAMatematica` |
| Dependencias externas | Ninguna (solo la biblioteca estándar de .NET) |
| Persistencia | Archivo JSON (`System.Text.Json`) |
| Enfoque de "IA" | Reglas: palabras clave y expresiones regulares |

La aplicación funciona como un asistente conversacional: el usuario escribe una frase, el motor decide qué significa (comando, saludo, consulta, operación) y responde.

## 2. Arquitectura

El proyecto tiene tres clases, cada una con una responsabilidad clara:

```
┌──────────────┐   Resolver(texto)   ┌──────────────────┐   lee / escribe   ┌─────────────────┐
│  Program     │ ──────────────────▶ │ MotorMatematico  │ ────────────────▶ │ MemoriaUsuario  │
│ (consola)    │ ◀────────────────── │ (interpretación) │                   │ (estado + JSON) │
└──────────────┘      respuesta      └──────────────────┘                   └─────────────────┘
```

- **`Program`** gestiona la entrada/salida por consola.
- **`MotorMatematico`** contiene toda la lógica de interpretación y cálculo.
- **`MemoriaUsuario`** guarda el estado y se encarga de leerlo/escribirlo en disco.

El motor recibe la memoria por constructor (`MotorMatematico(MemoriaUsuario memoria)`), un patrón simple de inyección de dependencias.

## 3. Flujo de ejecución

1. `Main` imprime el mensaje de bienvenida.
2. Se carga la memoria con `MemoriaUsuario.Cargar()`.
3. Se crea el motor: `new MotorMatematico(memoria)`.
4. Bucle principal:
   1. Muestra `Tú: ` y lee una línea.
   2. Si la línea es `salir` (sin distinguir mayúsculas), pide la despedida al motor con `Resolver("adiós")`, la muestra y termina.
   3. En caso contrario, llama a `motor.Resolver(entrada)` y muestra el resultado con el prefijo `IA: `.

### Orden de decisión dentro de `Resolver`

`Resolver` convierte el texto a minúsculas y prueba, **en este orden**, cada tipo de intención. La primera que coincide gana:

| Paso | Intención | Método |
|---|---|---|
| 0 | Entrada vacía | Devuelve un mensaje pidiendo que escriba algo |
| 1 | Comandos de memoria y ayuda | `IntentarComandosEspeciales` |
| 2 | Saludos, estado de ánimo y despedida | `IntentarSaludo` |
| 3 | "¿Qué sabes de mí?" | `ConsultarMemoria` |
| 4 | Estadísticas | `ConsultarEstadisticas` |
| 5 | Respuestas aprendidas | `IntentarRespuestaAprendida` |
| 6 | Operaciones matemáticas | `IntentarResolverMatematica` |
| 7 | Nada coincide | Mensaje de "No te entendí" con sugerencias |

El orden importa: por ejemplo, una respuesta aprendida tiene prioridad sobre una operación matemática, y los comandos especiales tienen prioridad sobre todo.

## 4. Program.cs

Punto de entrada de la aplicación.

| Elemento | Descripción |
|---|---|
| `static void Main()` | Muestra la bienvenida, carga la memoria, crea el motor y ejecuta el bucle de conversación. |

Puntos a destacar:

- La lectura usa `Console.ReadLine()?.Trim() ?? ""`, de modo que una entrada nula (por ejemplo, fin de flujo) se trata como cadena vacía.
- La despedida se genera pasando `"adiós"` al motor, así que se personaliza con el nombre guardado.

## 5. MemoriaUsuario.cs

Clase `sealed` que modela el estado persistente del usuario.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Nombre` | `string` | Nombre del usuario. Vacío por defecto. |
| `RespuestasAprendidas` | `Dictionary<string,string>` | Pares pregunta → respuesta enseñados con `aprende`. |
| `Notas` | `List<string>` | Notas personales. |
| `PreferenciaDecimales` | `int` | Máximo de decimales a mostrar. Por defecto `2`. |
| `ContadorOperaciones` | `int` | Total de operaciones resueltas. |
| `OperacionesPorTipo` | `Dictionary<string,int>` | Conteo por operación (`suma`, `resta`, `multiplica`...). |

### Métodos

| Método | Visibilidad | Descripción |
|---|---|---|
| `Cargar()` | `public static` | Carga la memoria. Intenta primero la ruta principal, luego el respaldo y, si ambos fallan, devuelve una memoria nueva. |
| `Guardar()` | `public` | Serializa a JSON y escribe en la ruta principal y en el respaldo. |
| `OlvidarTodo()` | `public` | Limpia nombre, respuestas, notas y estadísticas, y borra ambos archivos. No restablece `PreferenciaDecimales`. |
| `Serializar(m)` | `private static` | Serializa con sangrado (`WriteIndented = true`) y sin cambiar los nombres de propiedades. |
| `Deserializar(json)` | `private static` | Deserializa; si el resultado es `null`, devuelve una instancia nueva. |

### Rutas de persistencia

| Ruta | Valor |
|---|---|
| `RutaPrincipal` | `<ApplicationData>/IA-Matematica/memoria.json` (crea la carpeta si no existe) |
| `RutaRespaldo` | `<directorio actual>/memoria.json` |

### Estrategia de carga y guardado

```
Cargar():
  ¿existe la ruta principal?  → intentar leer ─ ok ─▶ devolver memoria
        │                                      └ error ─▶ avisar por stderr y seguir
  ¿existe el respaldo?        → intentar leer ─ ok ─▶ devolver memoria
        │                                      └ error ─▶ avisar por stderr y seguir
  Memoria nueva (mensaje "Estrenando memoria nueva")

Guardar():
  escribir en la ruta principal
  intentar escribir en el respaldo (si falla, se ignora)
  cualquier error principal → mensaje por stderr (no interrumpe el programa)
```

Los errores de guardado nunca detienen la aplicación: se informan por la salida de error estándar y la conversación continúa.

## 6. MotorMatematico.cs

Clase `sealed` central. Se construye con una `MemoriaUsuario` y expone un único método público: `Resolver(string entrada)`.

### Expresiones regulares

| Nombre | Patrón | Uso |
|---|---|---|
| `NumeroRegex` | `\d+(?:[.,]\d+)?` | Extrae enteros y decimales con punto o coma. |
| `GuionOperadorRegex` | `(?<=\d)-(?=\d)` | Detecta un guión entre dos dígitos (resta simbólica). |
| `XMultiplicacionRegex` | `(?<=\d)\s*x\s*(?=\d)` | Detecta una `x` entre dígitos como multiplicación. |

### Símbolos de salida

Constantes usadas al mostrar la expresión: `" + "`, `" − "`, `" × "`, `" ÷ "` y `" ^ "`.

### Comandos especiales (`IntentarComandosEspeciales`)

Devuelve la respuesta si la entrada es un comando de memoria, o `null` si no lo es. Se evalúan en este orden:

| Comando | Condición de activación | Efecto |
|---|---|---|
| Nombre | contiene `me llamo ` | Guarda el nombre (capitalizado). |
| Nombre | contiene `mi nombre es ` | Guarda el nombre (capitalizado). |
| Aprender | empieza por `aprende ` | Delegado en `ProcesarAprendizaje`. |
| Nota | empieza por `nota ` o `nota:` | Añade una nota. |
| Recordar | empieza por `recuerda que ` o `recuerda ` | Añade una nota. |
| Decimales | contiene `decimales ` | Cambia `PreferenciaDecimales` (válido de 0 a 15). |
| Olvidar | contiene `olvida todo` o `borrar memoria` | Llama a `OlvidarTodo()`. |
| Ayuda | contiene `ayuda` | Devuelve el texto de ayuda. |

Cada cambio de estado llama a `_memoria.Guardar()` para persistirlo de inmediato.

### Aprendizaje (`ProcesarAprendizaje`)

Procesa `aprende <pregunta> = <respuesta>`:

1. Busca el separador `" = "`; si no lo encuentra, busca `=`.
2. Extrae la pregunta (lo que está entre `aprende ` y el separador) y la respuesta (lo que sigue).
3. Si alguna de las dos queda vacía, devuelve un mensaje de uso.
4. Guarda `RespuestasAprendidas[pregunta] = respuesta` y persiste.

Si la pregunta ya existía, su respuesta se sobrescribe.

### Saludos (`IntentarSaludo`)

| Intención | Palabras detectadas |
|---|---|
| Saludo | `hola`, `hi`, `hey`, `buenos` |
| Estado de ánimo | `como estas`, `cómo estás`, `que tal`, `como vas` |
| Despedida | `adiós`, `hasta luego`, `chao` |

Si hay nombre guardado, la respuesta lo incluye.

### Consultas de memoria

- **`ConsultarMemoria`** (se activa con `que sabes`): resume nombre, respuestas aprendidas, notas, decimales y operaciones. Si `ContadorOperaciones == 0` responde que aún no tiene mucha información.
- **`ConsultarEstadisticas`** (se activa con `estadistic`): muestra el total y el desglose por operación, ordenado alfabéticamente. Si no hay operaciones, lo indica.

### Respuestas aprendidas (`IntentarRespuestaAprendida`)

1. Coincidencia exacta con una clave.
2. Coincidencia parcial: la entrada contiene una clave, o una clave contiene la entrada.
3. Si no hay coincidencia, devuelve `null`.

### Operaciones matemáticas

El proceso se divide en tres pasos dentro de `IntentarResolverMatematica`:

**1) Extraer números — `ExtraerNumeros`**
Devuelve dos listas paralelas: los textos originales (`"1,5"`) y los valores `double` (`1.5`). La coma se reemplaza por punto antes de convertir, usando `CultureInfo.InvariantCulture`.

**2) Detectar operación — `DetectarOperacion`**
Devuelve `(operación, operandos necesarios)` evaluando, en este orden:

1. Símbolos: `+`, guión entre dígitos, `×`/`x`/`*`, `/`/`÷`, `^`.
2. Palabras clave: `potencia`/`elevado`/`pow`, `raíz`/`raiz`/`cuadrada`, `porcentaje`, `promedio`/`media`, `modulo`/`módulo`/`residuo`/` mod `, `factorial`.
3. La forma `" por "` con al menos dos números (multiplicación).
4. Verbos: `suma(r)`, `resta(r)`, `multiplica(r)`, `divide`/`dividir`.
5. Si nada coincide: `("nada", 0)`.

Si la operación es `nada` o hay menos números de los necesarios, el método devuelve `null` y `Resolver` continúa con el mensaje de "No te entendí".

**3) Resolver — `ResolverOperacion`**
Devuelve `(expresión legible, resultado)`.

| Operación | Cálculo | Validaciones |
|---|---|---|
| `suma` | Suma acumulada de todos los operandos | — |
| `resta` | Resta acumulada de izquierda a derecha | — |
| `multiplica` | Producto de todos los operandos | — |
| `divide` | División acumulada de izquierda a derecha | Ningún divisor puede ser 0 |
| `potencia` | `Math.Pow(a, b)` | — |
| `raiz` | `Math.Sqrt(a)` | El número no puede ser negativo |
| `porcentaje` | `a / 100 × b` | — |
| `promedio` | `Average()` de todos los valores | — |
| `modulo` | `a % b` | El divisor no puede ser 0 |
| `factorial` | Producto de 1 a n (con `long`) | `n` entre 0 y 20 (evita desbordamiento) |

Tras resolver con éxito, el motor incrementa `ContadorOperaciones` y `OperacionesPorTipo[operación]`, y guarda la memoria. Si el resultado es `NaN` o infinito, responde `"El resultado no es un número válido."` y no cuenta la operación.

### Funciones auxiliares

| Método | Descripción |
|---|---|
| `Formatear(valor, precision)` | Formatea con `F{precision}`, recorta ceros finales y punto, y cambia el punto por coma. |
| `Capitalizar(texto)` | Pone en mayúscula la primera letra. |
| `ExtraerDespues(texto, clave)` | Devuelve el texto que sigue a la primera aparición de `clave`, o cadena vacía si no está. |
| `DevolverAyuda()` | Devuelve el texto de ayuda completo. |

## 7. Formato y reglas de salida

- Las respuestas de operaciones tienen la forma `expresión = resultado`, por ejemplo `2 + 8 = 10` o `6 × 7 = 42`.
- Se usa **coma decimal** en los resultados (`1,5`), independientemente de si el usuario escribió coma o punto.
- Se muestran como máximo `PreferenciaDecimales` decimales y se eliminan los ceros sobrantes.
- La expresión conserva los números tal como el usuario los escribió.
- El promedio se muestra como `a + b + c (prom) = resultado`.

## 8. Manejo de errores

| Situación | Comportamiento |
|---|---|
| División entre cero | `"No se puede dividir entre cero."` |
| Módulo con divisor cero | `"No se puede calcular módulo entre cero."` |
| Raíz de número negativo | `"No se puede calcular raíz de un número negativo."` |
| Factorial fuera de 0–20 | Mensaje explicando el rango válido |
| Resultado `NaN` o infinito | `"El resultado no es un número válido."` |
| Error leyendo la memoria | Aviso por `stderr`, intento con el respaldo y, en último caso, memoria nueva |
| Error guardando la memoria | Aviso por `stderr`; la aplicación sigue funcionando |

Las validaciones de operaciones se lanzan como `ArgumentException` desde `ResolverOperacion` y se capturan en `IntentarResolverMatematica`, que devuelve el mensaje al usuario.

## 9. Limitaciones y problemas conocidos

Al revisar el código se detectaron los siguientes puntos. No impiden usar la aplicación, pero conviene tenerlos presentes:

| # | Descripción | Ubicación | Posible solución |
|---|---|---|---|
| 1 | **Resta con espacios no detectada.** `GuionOperadorRegex` exige el guión pegado a los dígitos, por lo que `9 - 3` y `20 - 5 - 2` (que la ayuda muestra como válidos) no se reconocen. `9-3` sí funciona. | `GuionOperadorRegex`, `DevolverAyuda` | Cambiar el patrón a `(?<=\d)\s*-\s*(?=\d)`. |
| 2 | **Números negativos no soportados.** `NumeroRegex` solo captura dígitos, sin signo. | `NumeroRegex` | Admitir un signo opcional con un patrón que distinga negativo de resta. |
| 3 | **`decimales 0` recorta ceros enteros.** `TrimEnd('0')` se aplica incluso sin punto decimal, así que `"100"` pasa a `"1"`. | `Formatear` | Recortar ceros solo si la cadena contiene un punto. |
| 4 | **Comando `nota <texto>` sin dos puntos devuelve "No anoté nada".** Para `nota comprar pan`, `ExtraerDespues(texto, "nota:")` no encuentra la clave y devuelve vacío. `nota: comprar pan` funciona. | `IntentarComandosEspeciales` | Calcular el texto tras `nota:` o `nota ` según el caso. |
| 5 | **Detección por subcadena demasiado amplia.** Comprobaciones como `Contains("hola")`, `Contains("ayuda")` o `Contains("media")` también se activan dentro de otras palabras o frases (por ejemplo, "gracias por la ayuda" muestra la ayuda). | `IntentarSaludo`, `IntentarComandosEspeciales`, `DetectarOperacion` | Usar coincidencia por palabra completa. |
| 6 | **Coincidencia parcial de respuestas aprendidas muy permisiva.** `kv.Key.Contains(texto)` hace que entradas cortas (por ejemplo, una letra) coincidan con cualquier clave que las contenga. | `IntentarRespuestaAprendida` | Exigir una longitud mínima o coincidencia por palabras. |
| 7 | **Todo se guarda en minúsculas.** La entrada se convierte con `ToLowerInvariant()`, así que respuestas aprendidas y notas pierden las mayúsculas, y un nombre compuesto queda como `Juan carlos`. | `Resolver` | Conservar el texto original para nombre, notas y respuestas. |
| 8 | **`que sabes de mi` depende de las operaciones.** Si no se ha resuelto ninguna operación responde "Todavía no tengo mucha información", aunque haya nombre, notas o respuestas guardadas. | `ConsultarMemoria` | Evaluar también nombre, notas y respuestas aprendidas. |
| 9 | **Despedida solo desde `Program`.** `salir` termina el programa; escribir "adiós" dentro de la conversación solo muestra el mensaje sin cerrar. | `Program` | Tratar también esas palabras como salida si se desea. |

## 10. Cómo extender el proyecto

### Añadir una nueva operación (por ejemplo, `cuadrado`)

1. En `DetectarOperacion`, agrega la palabra clave:
   ```csharp
   if (texto.Contains("cuadrado")) return ("cuadrado", 1);
   ```
2. En `ResolverOperacion`, agrega el caso:
   ```csharp
   case "cuadrado":
       return ($"{originales[0]}²", valores[0] * valores[0]);
   ```
3. Actualiza `DevolverAyuda()` para documentarla.

Las estadísticas y el conteo en memoria funcionan automáticamente para la nueva operación.

### Añadir un nuevo comando de memoria

1. Añade una condición en `IntentarComandosEspeciales` antes del `return null`.
2. Modifica la propiedad correspondiente de `MemoriaUsuario` (o agrega una nueva con valor por defecto).
3. Llama a `_memoria.Guardar()` para persistir el cambio.
4. Documenta el comando en `DevolverAyuda()`.

Al agregar propiedades nuevas a `MemoriaUsuario`, los archivos `memoria.json` antiguos siguen siendo compatibles: las propiedades ausentes toman su valor por defecto.
