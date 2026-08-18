# Manual de Estándares C# + SQL Server

> Versión 8. Corrige defectos funcionales detectados en la revisión de la v7: `@@ROWCOUNT` obligatorio en todo UPDATE, baja lógica y reactivación, y `CAST(SCOPE_IDENTITY() AS BIGINT)` en las inserciones. Agrega el patrón recomendado de autorización por recurso (ownership), define el estilo institucional de API como REST pragmático con una convención de acciones para operaciones que no son CRUD, y elimina los textos de relleno en `Motivo`. Además declara explícitamente decisiones heredadas: mezcla de idiomas en los sufijos de los campos de control, alcance real de la trazabilidad del patrón base y uso de hora local del servidor.

---

# PARTE 1 — Estándar general

## 1. Introducción

Este manual define estándares técnicos para proyectos desarrollados en C# con SQL Server.

Está orientado a establecer reglas comunes para:

- APIs .NET Core.
- Windows Forms.
- Aplicaciones de consola.
- Servicios Windows.
- Worker Services.
- Módulos internos que consuman SQL Server.

El objetivo es que todos los proyectos nuevos tengan una forma común de estructurarse, nombrarse y conectarse a base de datos.

También busca mejorar gradualmente los proyectos antiguos cuando se realicen mantenimientos o nuevos módulos.

---

## 2. Objetivo del manual

El objetivo principal es establecer una guía institucional para el desarrollo con C# y SQL Server.

Este manual busca:

- Estandarizar nombres de clases, métodos, tablas y procedures.
- Definir buenas prácticas de acceso a datos.
- Evitar código desordenado o difícil de mantener.
- Evitar procedimientos almacenados muy complejos o con JSON como único parámetro.
- Mejorar la seguridad de credenciales y de contraseñas de usuario.
- Definir criterios para auditoría, baja lógica y reactivación.
- Establecer reglas de manejo de errores y de concurrencia.
- Definir una estructura uniforme de respuestas para las APIs, incluida paginación.
- Definir criterios básicos para consumo de servicios externos REST y SOAP, incluyendo resiliencia de red.
- Definir criterios básicos para carga de archivos.
- Definir un mínimo de pruebas, monitoreo (`/health`) y logging estructurado.
- Mejorar la trazabilidad y mantenibilidad de los sistemas.

El manual debe ayudar a reducir la incertidumbre técnica. La idea es que el desarrollador pueda consultar qué hacer en casos frecuentes y aplicar una forma común de trabajo.

---

## 3. Alcance del manual

El manual será obligatorio para proyectos nuevos desarrollados en C# con SQL Server.

Para proyectos antiguos, será una guía de mejora progresiva.

Esto significa:

```text
Proyectos nuevos:
Deben aplicar el estándar.

Proyectos antiguos:
Pueden mantenerse, pero se recomienda aplicar el estándar cuando se modifiquen módulos.
```

El manual podrá aplicarse a:

- APIs .NET Core.
- Windows Forms.
- Aplicaciones de consola.
- Servicios Windows.
- Worker Services.
- Librerías C#.
- Módulos internos conectados a SQL Server.

---

## 4. Versiones y tecnología

Para proyectos nuevos se recomienda usar .NET moderno.

Los proyectos antiguos desarrollados en .NET Framework podrán mantenerse en su versión actual, salvo que exista una decisión formal de migración.

No se recomienda iniciar nuevos desarrollos en tecnologías obsoletas si no existe una razón técnica o institucional.

---

## 5. Principios generales de desarrollo

Todo proyecto deberá buscar:

- claridad;
- separación de responsabilidades;
- nombres consistentes;
- configuración segura;
- acceso a datos ordenado;
- manejo adecuado de errores;
- mantenimiento simple;
- código entendible para otros desarrolladores;
- compatibilidad con SQL Server;
- facilidad para ser revisado por IA o por otro desarrollador.

Regla general:

```text
El código debe ser entendible, mantenible y trazable.

Entendible:
Que otro programador pueda leerlo y entenderlo.

Mantenible:
Que se pueda modificar sin dañar todo el sistema.

Trazable:
Que se pueda saber quién hizo qué, cuándo y por qué.
```

### 5.1 Entendible

Significa que otro desarrollador debe poder leer el código y comprender qué hace sin sufrir.

Ejemplo no tan claro:

```text
public async Task<List<CiudadResponse>> Get()
{
    return await _repo.X();
}
```

Ejemplo más entendible:

```text
public async Task<List<CiudadResponse>> ListarAsync()
{
    return await _ciudadRepository.ListarAsync();
}
```

También aplica a SQL.

No recomendado:

```text
SELECT * FROM TbCiudad
```

Recomendado:

```text
SELECT
    IdCiudad,
    Descripcion,
    IdDepartamento,
    Departamento
FROM dbo.TbCiudad
WHERE Activo = 1;
```

### 5.2 Mantenible

Significa que el código se puede modificar después sin romper todo.

Ejemplo malo:

```text
Controller → SQL directo
```

Si mañana cambia la lógica, tienes que tocar el controller, la consulta, la validación y todo junto.

Ejemplo mantenible:

```text
Controller → Service → Repository → SQL Server
```

Porque cada parte tiene su responsabilidad:

```text
Controller:
recibe la petición

Service:
valida y aplica reglas

Repository:
llama al procedure

SQL Server:
guarda o consulta datos
```

Entonces si mañana cambia una validación, probablemente solo tocas el Service.

Si cambia el procedure, tocas el Repository y SQL.

### 5.3 Trazable

Significa que se puede saber qué pasó, cuándo pasó y quién lo hizo.

Por eso usamos campos como:

```text
IdUsuarioCreate
FechaCreate
IdUsuarioUpdate
FechaUpdate
IdUsuarioDelete
FechaDelete
Motivo
```

Con trazabilidad puedes saber:

```text
Quién modificó: IdUsuarioUpdate
Cuándo modificó: FechaUpdate
Por qué lo hizo: Motivo = 'Corrección de descripción'
```

*(actualizado en v7: se elimina `Accion` porque es derivable de qué campo `Fecha*`/`IdUsuario*` está poblado; `Detail` se renombra a `Motivo`. Ver [§18](#18-llaves-primarias) y [§22](#22-activo-y-estado) para el detalle completo.)*

**Alcance de la trazabilidad** *(nuevo en v8)*: los campos de control guardan únicamente el estado del **último** cambio. Cada UPDATE sobreescribe el `Motivo`, el `IdUsuarioUpdate` y la `FechaUpdate` anteriores: de un registro modificado cinco veces solo se conoce la quinta modificación. Si el negocio exige el historial completo de cambios de una tabla, esa tabla debe definirse como system-versioned temporal table (ver §34) — ese es precisamente el criterio para considerarla "tabla crítica".

---

# PARTE 2 — APIs .NET modernas

## 6. Acceso a datos

El estándar de acceso a datos será:

```text
APIs nuevas:
Dapper + procedimientos almacenados.

WinForms antiguos o proyectos heredados:
ADO.NET permitido.

Entity Framework:
No será parte del estándar institucional principal.
```

### 6.1 Dapper

Dapper será la opción recomendada para APIs nuevas.

Ventajas:

- simple;
- rápido;
- mapea resultados a clases;
- funciona bien con stored procedures;
- evita sobrecarga innecesaria;
- se adapta a bases de datos existentes.

### 6.2 ADO.NET

ADO.NET seguirá permitido en proyectos antiguos o Windows Forms.

Ejemplos:

```csharp
SqlConnection
SqlCommand
SqlDataAdapter
DataTable
DataSet
```

Sin embargo, se recomienda evitar que la interfaz llame directamente a SQL Server.

### 6.3 Entity Framework

Entity Framework no será el estándar institucional principal.

La razón es que la institución trabaja principalmente con SQL Server y procedimientos almacenados.

---

## 7. Organización de proyectos C#

### 7.1 APIs .NET Core

Estructura recomendada:

```text
ApiWeb
├── Controllers

Application
├── Services

Infrastructure
├── Connection
├── Data
├── ExternalServices

Dtos
├── Entidad
    ├── EntidadContracts.cs
```

Flujo recomendado para operaciones con base de datos:

```text
Controller → Service → Repository → SQL Server
```

Flujo recomendado para consumir servicios externos REST o SOAP:

```text
Controller → Service → ExternalService Client → Servicio externo
```

### 7.2 Windows Forms

Ver Parte 5 — Sistemas heredados y Windows Forms.

### 7.3 Worker Services y servicios Windows

Ver sección 15 (Worker Services y procesos por lotes), dentro de esta misma parte.

---

## 8. Estándares de nombres en C#

| Elemento | Estilo | Ejemplo |
|---|---|---|
| Clases | PascalCase | `CiudadService` |
| Controllers | Singular + `Controller` | `CiudadController` |
| Services | Singular + `Service` | `CiudadService` |
| Repositories | Singular + `Repository` | `CiudadRepository` |
| Métodos | PascalCase y en español | `ObtenerPorIdAsync` |
| Propiedades | PascalCase | `IdCiudad` |
| Variables locales | camelCase | `ciudadActual` |
| Campos privados | `_camelCase` | `_ciudadRepository` |
| Archivos | PascalCase | `CiudadController.cs` |

### Reglas

- Los controllers se nombrarán en singular.
- Los métodos de controllers, services y repositories se nombrarán en español.
- No usar nombres ambiguos.
- No usar abreviaciones difíciles de entender.
- Usar nombres que expresen intención.
- Evitar nombres genéricos como `Clase1`, `Datos`, `Proceso`, `Manager` sin contexto.

Ejemplos recomendados:

```text
CiudadController
CiudadService
CiudadRepository
ListarAsync
ObtenerPorIdAsync
InsertarAsync
ActualizarAsync
DarBajaAsync
```

Ejemplos no recomendados:

```text
CiudadesController
CityController
GetAllAsync
DoProcess
Manager
```

---

## 9. Manejo de errores en C#

No se permitirán `catch` vacíos.

Ejemplo no permitido:

```csharp
try
{
    // código
}
catch
{
}
```

Todo error debe manejarse, convertirse en una respuesta clara o relanzarse cuando corresponda.

Ejemplo recomendado:

```csharp
try
{
    var resultado = await _repository.InsertarAsync(request);
    return ApiResponse<long>.Ok(resultado, "Registro guardado correctamente.");
}
catch (SqlException ex)
{
    _logger.LogError(ex, "Error de base de datos al insertar persona.");
    return ApiResponse<long>.Error("No se pudo guardar el registro.");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error inesperado al insertar persona.");
    return ApiResponse<long>.Error("Ocurrió un error inesperado.");
}
```

**Registrar antes de responder** *(nuevo en v8)*: el mensaje amigable es para el usuario; el error técnico completo se registra **siempre** en el log antes de devolver la respuesta (con su `TraceId`, ver §45). Un `catch` que devuelve el mensaje amigable sin loguear descarta la única evidencia del fallo: cuando llegue el reporte "me salió un error", no habrá forma de saber qué pasó.

El usuario debe recibir un mensaje amigable, no un error técnico.

Ejemplo recomendado para el usuario:

```text
No se pudo guardar el registro.
```

Ejemplo no recomendado para el usuario:

```text
Violation of UNIQUE KEY constraint...
Invalid column name...
Timeout expired...
```

---

## 10. Testing mínimo *(nuevo en v6)*

El manual no exige cobertura total, pero sí un mínimo institucional para reducir el riesgo de regresiones en cada cambio.

Regla mínima:

```text
Services   → unit tests (lógica de negocio, validaciones, casos de error).
Repositories → pruebas de integración contra una base de datos de pruebas.
Controllers → pruebas opcionales; si existen, de integración (levantando la API).
```

Motivo:

```text
Los Services concentran las reglas de negocio: son los más rentables de cubrir con unit tests.
Los Repositories dependen de SQL Server real: probarlos con mocks no detecta errores de procedure/parámetros, por eso se recomienda integración.
```

No es obligatorio alcanzar un porcentaje de cobertura específico, pero todo Service nuevo debe incluir al menos las pruebas de sus casos de validación y de error más importantes (duplicado crítico, registro no encontrado, campos obligatorios).

---

## 11. Inyección de dependencias

En APIs nuevas se usará inyección de dependencias por constructor.

Ejemplo:

```csharp
private readonly CiudadService _ciudadService;

public CiudadController(CiudadService ciudadService)
{
    _ciudadService = ciudadService;
}
```

Para mantener simplicidad:

```text
Usar inyección por constructor.
No usar new directo para services/repositories en APIs nuevas.
```

Ejemplo no recomendado:

```csharp
var repo = new CiudadRepository();
```

Ejemplo recomendado:

```csharp
public CiudadService(CiudadRepository ciudadRepository)
{
    _ciudadRepository = ciudadRepository;
}
```

---

## 12. APIs .NET Core

Las APIs nuevas deberán tener:

- Swagger;
- JWT para endpoints protegidos;
- `[Authorize]` donde corresponda;
- CORS configurado por ambiente;
- `ApiResponse<T>`;
- contrato estándar de paginación y filtros;
- Dapper;
- procedures;
- estructura Controller → Service → Repository.

### Swagger

Swagger será obligatorio en APIs nuevas.

### Documentación de endpoints en Swagger

Cada endpoint deberá tener documentación básica para que Swagger muestre claramente:

- qué hace el endpoint;
- cómo se consume;
- qué datos recibe;
- qué estructura devuelve;
- qué códigos HTTP puede responder.

Se recomienda usar comentarios XML en los controllers.

`summary` es el resumen corto que aparece en Swagger para explicar rápidamente qué hace el endpoint.

`remarks` permite agregar una explicación más amplia, ejemplos de consumo, body de ejemplo y respuesta esperada.

Ejemplo:

```csharp
/// <summary>
/// Lista las personas activas registradas en el sistema.
/// </summary>
/// <remarks>
/// Ejemplo de consumo:
///
/// GET /api/persona
///
/// Respuesta exitosa:
/// {
///   "success": true,
///   "message": "Registros obtenidos correctamente.",
///   "data": [
///     {
///       "idPersona": 1,
///       "nombre": "Juan",
///       "apellido": "Pérez",
///       "documento": "1234567"
///     }
///   ]
/// }
/// </remarks>
/// <returns>Lista de personas activas.</returns>
[HttpGet]
[ProducesResponseType(typeof(ApiResponse<List<PersonaResponse>>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> Listar()
{
    var respuesta = await _personaService.ListarAsync();
    return Ok(respuesta);
}
```

Para que Swagger lea los comentarios XML, se debe habilitar la generación de documentación XML en el `.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

Y registrar el XML en `Program.cs`:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});
```

Regla recomendada:

```text
Todo endpoint público de la API debe tener summary, descripción breve, códigos de respuesta y ejemplo básico de consumo cuando sea necesario.
```

### JWT

Las APIs protegidas usarán JWT.

El login será público, pero los endpoints internos deberán protegerse con `[Authorize]`.

Ejemplo:

```csharp
[Authorize]
[HttpGet("listar")]
public async Task<IActionResult> Listar()
{
    // Endpoint protegido
}
```

#### Issuer

El `Issuer` indica quién emite el token.

Ejemplo:

```text
Issuer = ApiInstitucional
```

Representa a la API o sistema que genera el JWT.

#### Audience

El `Audience` indica para quién fue creado el token.

Ejemplo:

```text
Audience = SistemasInstitucionales
```

Permite controlar que un token emitido para un sistema no sea aceptado indebidamente por otro.

#### Tiempo de expiración

El token debe tener un tiempo de vida definido.

Ejemplos:

```text
30 minutos
1 hora
2 horas
8 horas
```

Mientras más corto sea el tiempo, mayor seguridad.

Mientras más largo sea el tiempo, mayor comodidad para el usuario.

La duración debe definirse según el tipo de sistema.

Ejemplo:

```text
Sistema crítico:
Token corto.

Sistema interno administrativo:
Token moderado.

Aplicación con sesión larga:
Usar access token corto + refresh token (ver Parte 4, sección 39).
```

#### Clave secreta segura

La clave secreta se usa para firmar el token.

No debe ser una palabra simple ni una clave corta.

No recomendado:

```text
123456
clave
secret
mi_clave
```

Recomendado:

```text
Clave larga, compleja y administrada fuera del código fuente (ver Parte 4, sección 38 — Gestión de secretos por ambiente).
```

#### No guardar claves JWT en Git

Las claves reales de JWT no deben subirse al repositorio. Ver Parte 4, sección 38 (Gestión de secretos por ambiente) para el mecanismo concreto según el ambiente.

### CORS

CORS permite controlar qué aplicaciones frontend pueden consumir la API desde un navegador.

**Cambio en v6:** las URLs permitidas ya no se escriben directamente en `Program.cs`. Se leen desde configuración por ambiente, igual que cualquier otro valor que cambia entre desarrollo y producción (§43 — Publicación y ambientes).

Ejemplo de configuración, `appsettings.Development.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200"
    ]
  }
}
```

`appsettings.Production.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://sistemas.institucion.gob.bo"
    ]
  }
}
```

`Program.cs` solo lee la configuración, no declara URLs:

```csharp
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsInstitucional", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("CorsInstitucional");
```

Ventajas de CORS bien configurado:

- evita que cualquier dominio consuma la API desde navegador;
- reduce riesgos de exposición innecesaria;
- separa configuración de desarrollo y producción sin tocar código;
- permite controlar qué frontends institucionales están autorizados.

No recomendado en producción:

```csharp
.AllowAnyOrigin()
.AllowAnyHeader()
.AllowAnyMethod()
```

### Controllers y rutas

Los controllers se nombrarán en singular.

Ejemplo recomendado:

```text
CiudadController
PersonaController
UsuarioController
```

Las rutas base también se manejarán en singular.

Ejemplo recomendado:

```text
GET    /api/persona
GET    /api/persona/{id}
POST   /api/persona
PUT    /api/persona/{id}
DELETE /api/persona/{id}
```

El método `DELETE` representa baja lógica, no eliminación física.

### Estilo institucional: REST pragmático *(nuevo en v8)*

El estándar institucional de rutas es **REST pragmático**: se sigue el formato REST (recursos como sustantivos, verbos HTTP con semántica, códigos de estado), con desviaciones conscientes y declaradas:

```text
- Las inserciones responden 200 OK, no 201 Created (decisión institucional de simplicidad).
- Los controllers y rutas base van en singular (/api/persona, no /api/personas).
- DELETE ejecuta baja lógica, no eliminación física.
```

No es REST académico y no pretende serlo. Lo que se busca es el formato uniforme de rutas, no el cumplimiento estricto de la teoría.

#### Acciones que no son CRUD

Cuando una operación de negocio no mapea a los verbos estándar (derivar un trámite, anular, generar un reporte), se usa el formato de **acción sobre recurso**: la acción va al final de la ruta, en minúsculas, como verbo en infinitivo, siempre con `POST`:

```text
POST /api/tramite/{id}/derivar
POST /api/tramite/{id}/anular
POST /api/persona/{id}/reactivar
POST /api/persona/buscar            (consultas con filtros complejos, ver paginación)
POST /api/reporte/mensual/generar
```

Reglas:

```text
1. Una operación de negocio = un endpoint con nombre propio.
   No crear endpoints genéricos con un parámetro "accion" adentro:
   ocultan las operaciones y complican la autorización por endpoint.
2. La acción vive en el controller de la entidad principal afectada
   (derivar un trámite → /api/tramite/..., aunque involucre funcionarios).
3. Cada acción tiene su procedure propio (PA_Tramite_Derivar, PA_Tramite_Anular)
   cumpliendo las reglas de §25-§26, incluido @@ROWCOUNT (§23.5).
4. Las acciones que cambian estado registran los campos de control
   igual que una actualización normal (IdUsuarioUpdate, FechaUpdate, Motivo).
```

#### Sistemas heredados con estilo RPC

Los sistemas existentes anteriores a esta versión usan rutas estilo RPC — el verbo dentro de la ruta y `POST` para consultas:

```text
POST /api/usuario/BuscarUsuariosDadoDatos
POST /api/funcionario/ObtenerFuncionario
```

Esos sistemas **no se migran ni se "corrigen"**: siguen funcionando con su estilo. Las APIs nuevas siguen el formato REST pragmático de esta sección. Regla dura: **no mezclar ambos estilos dentro de una misma API** — una API es REST pragmático o es RPC heredada, nunca las dos cosas a la vez.

### Métodos en C#

Los métodos del controller, service y repository se nombrarán en español.

Ejemplo:

```csharp
ListarAsync()
ObtenerPorIdAsync()
InsertarAsync()
ActualizarAsync()
DarBajaAsync()
```

### ApiResponse

Todos los endpoints deberán devolver la misma estructura principal:

```json
{
  "success": true,
  "message": "Mensaje",
  "data": {}
}
```

Esto aplica tanto para respuestas correctas como para validaciones, errores, duplicados, registros no encontrados y errores internos.

No se deben devolver formatos diferentes como:

```json
{
  "error": "Error interno"
}
```

### ApiResponse con métodos estáticos

Se recomienda que `ApiResponse<T>` tenga métodos estáticos para construir respuestas de forma uniforme.

Ejemplo:

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message)
    {
        return new ApiResponse<T> { Success = true, Message = message, Data = data };
    }

    public static ApiResponse<T> Error(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message, Data = default };
    }

    public static ApiResponse<T> NotFound(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message, Data = default };
    }

    public static ApiResponse<T> Validation(T data, string message = "Existen campos obligatorios o inválidos.")
    {
        return new ApiResponse<T> { Success = false, Message = message, Data = data };
    }

    public static ApiResponse<T> Conflict(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message, Data = default };
    }
}
```

Ventaja:

```text
Sin métodos estáticos:
new ApiResponse<T> { Success = true, Message = "...", Data = ... }

Con métodos estáticos:
ApiResponse<T>.Ok(data, "...")
ApiResponse<T>.Error("...")
```

### Contrato estándar de paginación y filtros *(nuevo en v6)*

El manual v5 dejaba la paginación y los filtros fuera de alcance. Eso llevaba a que cada API definiera su propia convención (`page/pageSize`, `pagina/cantidad`, `limit/offset`), rompiendo la consistencia que el resto del manual sí exige.

Regla:

```text
Query string estándar para listados paginados:
GET /api/persona?pagina=1&cantidad=10&buscar=juan
```

- `pagina`: base 1 (la primera página es `1`, no `0`).
- `cantidad`: cantidad de registros por página. Definir un máximo razonable por endpoint (ej. 100) para evitar consultas excesivas.
- `buscar`: texto libre opcional para búsquedas simples. Para filtros más complejos, usar un endpoint dedicado (`POST /api/persona/buscar`) con un DTO de filtros tipado.

Respuesta estándar cuando el listado es paginado:

```json
{
  "success": true,
  "message": "Registros obtenidos correctamente.",
  "data": [],
  "paginacion": {
    "pagina": 1,
    "cantidad": 10,
    "total": 137,
    "totalPaginas": 14
  }
}
```

Para catálogos pequeños que se traen completos (sin paginar), no es necesario incluir el bloque `paginacion`.

Los procedures de listado paginado deben devolver el total de registros junto con la página solicitada (por ejemplo, con `COUNT(*) OVER()` o una segunda consulta), para no depender de que el backend adivine el total.

### Ejemplos de respuestas estándar

#### Guardado correcto

HTTP `200 OK`:

```json
{
  "success": true,
  "message": "Registro guardado correctamente.",
  "data": {
    "idPersona": 1
  }
}
```

#### Listado con datos

HTTP `200 OK`:

```json
{
  "success": true,
  "message": "Registros obtenidos correctamente.",
  "data": [
    {
      "idPersona": 1,
      "nombre": "Juan",
      "apellido": "Pérez",
      "documento": "1234567"
    }
  ]
}
```

En respuestas normales de API no se deben devolver campos internos de control como `Activo`, `Motivo`, `FechaCreate`, `FechaUpdate` o `FechaDelete`.

#### Listado sin registros

No debe tratarse como error.

HTTP `200 OK`:

```json
{
  "success": true,
  "message": "No se encontraron registros activos.",
  "data": []
}
```

#### Búsqueda por ID sin resultado

HTTP `404 Not Found`:

```json
{
  "success": false,
  "message": "No se encontró el registro solicitado.",
  "data": null
}
```

#### Error de validación

HTTP `400 Bad Request`:

```json
{
  "success": false,
  "message": "Existen campos obligatorios o inválidos.",
  "data": {
    "errores": [
      {
        "campo": "nombre",
        "mensaje": "El nombre es obligatorio."
      },
      {
        "campo": "documento",
        "mensaje": "El documento es obligatorio."
      }
    ]
  }
}
```

#### Duplicado crítico

HTTP `400 Bad Request`:

```json
{
  "success": false,
  "message": "Ya existe una persona registrada con el mismo documento.",
  "data": {
    "campo": "documento"
  }
}
```

#### Conflicto de concurrencia *(nuevo en v6, ver Parte 3 §32 — Transacciones)*

HTTP `409 Conflict`:

```json
{
  "success": false,
  "message": "El registro fue modificado por otro usuario. Vuelva a cargarlo antes de guardar.",
  "data": null
}
```

#### Error interno

HTTP `500 Internal Server Error`:

```json
{
  "success": false,
  "message": "No se pudo completar la operación. Intente nuevamente.",
  "data": null
}
```

#### Sin permiso

HTTP `403 Forbidden`:

```json
{
  "success": false,
  "message": "No tiene permiso para realizar esta acción.",
  "data": null
}
```

#### Sin token o token inválido

HTTP `401 Unauthorized`:

```json
{
  "success": false,
  "message": "Debe iniciar sesión para acceder a este recurso.",
  "data": null
}
```

### Campos de control en respuestas normales de API

Los campos de control y auditoría deben existir en las tablas de SQL Server, pero no deben devolverse en las respuestas normales de la API.

Estos campos son internos del sistema:

```text
Activo
Motivo
IdUsuarioCreate
FechaCreate
IdUsuarioUpdate
FechaUpdate
IdUsuarioDelete
FechaDelete
```

Estos campos sirven para auditoría, trazabilidad y control interno, pero normalmente no deben mostrarse al frontend ni al usuario final.

#### Ejemplo no recomendado

No se recomienda que una respuesta normal de listado devuelva datos de control:

```json
{
  "success": true,
  "message": "Registros obtenidos correctamente.",
  "data": [
    {
      "idCiudad": 1,
      "descripcion": "Santa Cruz",
      "idDepartamento": 7,
      "departamento": "Santa Cruz",
      "activo": true,
      "motivo": null,
      "fechaCreate": "2026-05-12T09:09:29",
      "fechaUpdate": null,
      "fechaDelete": null
    }
  ]
}
```

Aunque esos datos existan en la tabla, no son necesarios para una pantalla CRUD normal.

#### Ejemplo recomendado

La respuesta normal debe devolver solo los datos necesarios para la pantalla o para el consumo del frontend:

```json
{
  "success": true,
  "message": "Registros obtenidos correctamente.",
  "data": [
    {
      "idCiudad": 1,
      "descripcion": "Santa Cruz",
      "idDepartamento": 7,
      "departamento": "Santa Cruz"
    }
  ]
}
```

*(actualizado en v7: como `IdCiudad` ya es por defecto un `BIGINT IDENTITY` correlativo — ver [§18](#18-llaves-primarias) —, no hace falta devolver un campo `nro` aparte. Ese campo adicional solo aplica en tablas que estén bajo la excepción de PK `UNIQUEIDENTIFIER`, donde puede exponerse un `Uid{Entidad}` en vez del identificador interno.)*

#### Regla para los SELECT

Los procedimientos almacenados de consulta no deben usar `SELECT *`.

Además, los listados normales no deben seleccionar campos internos de control.

No recomendado:

```sql
SELECT *
FROM dbo.TbCiudad
WHERE Activo = 1;
```

Tampoco recomendado para respuestas normales:

```sql
SELECT
    IdCiudad,
    Descripcion,
    IdDepartamento,
    Departamento,
    Activo,
    Motivo,
    FechaCreate,
    FechaUpdate,
    FechaDelete
FROM dbo.TbCiudad
WHERE Activo = 1;
```

Recomendado:

```sql
SELECT
    IdCiudad,
    Descripcion,
    IdDepartamento,
    Departamento
FROM dbo.TbCiudad
WHERE Activo = 1;
```

El filtro `WHERE Activo = 1` sí debe usarse internamente para devolver solo registros vigentes, pero el campo `Activo` no necesita enviarse en la respuesta normal.

#### Regla para contratos Response

Los contratos de respuesta normales no deben incluir campos de control.

No recomendado:

```csharp
public class CiudadResponse
{
    public long IdCiudad { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public long IdDepartamento { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaCreate { get; set; }
    public DateTime? FechaUpdate { get; set; }
    public DateTime? FechaDelete { get; set; }
}
```

Recomendado:

```csharp
public class CiudadResponse
{
    public long IdCiudad { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public long IdDepartamento { get; set; }
    public string Departamento { get; set; } = string.Empty;
}
```

#### Excepción

Los campos de control solo podrán devolverse en endpoints especiales de auditoría, administración o trazabilidad.

Ejemplos:

```text
GET /api/ciudad/{id}/auditoria
GET /api/usuario/{id}/auditoria
```

En esos casos se recomienda crear un contrato separado, por ejemplo:

```csharp
public class CiudadAuditoriaResponse
{
    public long IdCiudad { get; set; }
    public bool Activo { get; set; }
    public string? Motivo { get; set; }
    public long? IdUsuarioCreate { get; set; }
    public DateTime FechaCreate { get; set; }
    public long? IdUsuarioUpdate { get; set; }
    public DateTime? FechaUpdate { get; set; }
    public long? IdUsuarioDelete { get; set; }
    public DateTime? FechaDelete { get; set; }
}
```

Regla:

```text
SQL Server guarda los campos de control.
La API normal devuelve solo los datos que necesita el frontend.
Los campos de control se devuelven únicamente en endpoints especiales de auditoría o administración.
```

### Códigos HTTP estándar

Los códigos HTTP se ven al consumir el servicio en herramientas como:

- Swagger;
- Postman;
- Insomnia;
- pestaña Network de las herramientas de desarrollador del navegador;
- respuesta de `HttpClient` en Angular mediante `response.status`, si se observa la respuesta completa.

Ejemplo: aunque el cuerpo JSON tenga `success`, el protocolo HTTP también informa el estado de la operación.

| Caso | Código HTTP | Observación |
|---|---:|---|
| Listar correctamente | `200 OK` | Con datos o lista vacía. |
| Obtener por ID correctamente | `200 OK` | Registro encontrado. |
| Insertar correctamente | `200 OK` | Decisión institucional de simplicidad: se mantiene `200 OK` en vez de `201 Created`. Es una desviación intencional del REST estricto. |
| Actualizar correctamente | `200 OK` | Registro actualizado. |
| Dar de baja correctamente | `200 OK` | Baja lógica. |
| Reactivar correctamente | `200 OK` | Ver Parte 3, §24. |
| Error de validación | `400 Bad Request` | Campos obligatorios o inválidos. |
| Duplicado crítico | `400 Bad Request` | Dato único ya existe. |
| Sin token | `401 Unauthorized` | Usuario no autenticado. |
| Sin permiso | `403 Forbidden` | Usuario autenticado sin autorización. |
| Registro no encontrado | `404 Not Found` | Búsqueda por ID sin resultado. |
| Conflicto de concurrencia | `409 Conflict` | `RowVersion` no coincide (ver Parte 3, §32). |
| Error interno | `500 Internal Server Error` | Error inesperado. |

### Versionamiento

El versionamiento será recomendado, pero no obligatorio.

Ejemplo:

```text
/api/v1/usuario
/api/v1/persona
/api/v2/persona
```

Se recomienda usar versionamiento cuando:

- la API será consumida por varios sistemas;
- existen aplicaciones móviles o clientes externos;
- se prevén cambios fuertes en contratos de entrada o salida;
- se necesita mantener una versión antigua funcionando mientras se libera una nueva;
- la API es pública o institucionalmente crítica.

Ejemplo de uso:

```text
/api/v1/persona → devuelve nombre, apellido y documento.
/api/v2/persona → además devuelve correo, celular y estado laboral.
```

Para APIs internas simples, puede omitirse inicialmente.

---

## 13. Consumo de servicios externos REST y SOAP

Los sistemas pueden necesitar consumir servicios externos, ya sea APIs REST o servicios SOAP.

Ejemplos:

- servicios de otras unidades internas;
- servicios nacionales;
- servicios de terceros;
- servicios SOAP heredados;
- APIs REST modernas.

### Regla principal

No se deben consumir servicios externos directamente desde el controller.

Flujo recomendado:

```text
Controller
   ↓
Service
   ↓
ExternalService / Client
   ↓
API REST externa o servicio SOAP
```

Estructura recomendada:

```text
Infrastructure
├── ExternalServices
    ├── PadronRestClient.cs
    ├── TramiteSoapClient.cs
    ├── CiudadanoApiClient.cs
```

### Configuración

Las URLs, usuarios, tokens o claves de servicios externos no deben estar quemados en el código.

Deben configurarse mediante `appsettings`, variables de entorno o mecanismos seguros según el ambiente (ver Parte 4, §38).

Ejemplo:

```json
{
  "ServiciosExternos": {
    "PadronApi": {
      "BaseUrl": "https://api.institucion.gob.bo",
      "TimeoutSeconds": 30
    }
  }
}
```

### Resiliencia de red *(nuevo en v6)*

Todo cliente de servicio externo debe definir explícitamente:

```text
Timeout obligatorio (recomendado: 10-30 segundos según el servicio).
Reintentos controlados solo para operaciones idempotentes (ej. consultas GET), con un máximo definido (ej. 2-3 intentos) y backoff entre intentos.
No reintentar automáticamente operaciones que puedan duplicar efectos (ej. un envío de correo o una confirmación de trámite) sin una validación de idempotencia.
```

Motivo: sin timeout ni límite de reintentos, un servicio externo caído puede colgar la API institucional en vez de fallar rápido con un mensaje claro.

Ejemplo conceptual usando `HttpClient` con política de reintentos (por ejemplo, con Polly):

```csharp
services.AddHttpClient<PadronRestClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
})
.AddPolicyHandler(Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrTransientHttpStatusCode()
    .WaitAndRetryAsync(2, intento => TimeSpan.FromSeconds(2 * intento)));
```

Si no se usa una librería de resiliencia, como mínimo debe configurarse el timeout del `HttpClient` y capturar `TaskCanceledException`/`HttpRequestException` para devolver un `ApiResponse<T>.Error` amigable en vez de dejar que la excepción suba sin control.

### Manejo de errores

Aunque el servicio externo responda con otro formato, la API interna debe convertir la respuesta al estándar `ApiResponse<T>`.

Ejemplo:

```json
{
  "success": false,
  "message": "No se pudo consultar el servicio externo.",
  "data": null
}
```

No se debe devolver directamente al frontend el error crudo del servicio externo.

### SOAP

Para servicios SOAP no se usará como estándar la opción de Visual Studio **Connected Services**.

La recomendación será crear una clase dedicada dentro de `Infrastructure/ExternalServices`, por ejemplo:

```text
TramiteSoapClient.cs
```

Esta clase será responsable de:

- armar la solicitud SOAP si corresponde;
- consumir el endpoint SOAP;
- interpretar la respuesta;
- manejar timeouts y errores;
- devolver un resultado entendible al service.

El service será quien decida cómo usar esa respuesta dentro del flujo de negocio.

Regla:

```text
El controller no consume SOAP.
El service coordina.
El cliente SOAP se ubica en Infrastructure/ExternalServices.
La respuesta final hacia el frontend conserva ApiResponse<T>.
```

---

## 14. Carga y manejo de archivos

Cuando una API reciba archivos, se recomienda usar `multipart/form-data`.

No se recomienda enviar archivos como `base64` en JSON, porque aumenta el tamaño del contenido y hace más pesado el request.

Regla recomendada:

```text
Archivos pequeños o grandes enviados desde frontend → multipart/form-data
No recomendado → archivo en base64 dentro de JSON
```

Ejemplo de request en .NET:

```csharp
public class DocumentoUploadRequest
{
    public IFormFile Archivo { get; set; } = default!;
    public string Descripcion { get; set; } = string.Empty;
}
```

Ejemplo de endpoint:

```csharp
[HttpPost("subir")]
public async Task<IActionResult> Subir([FromForm] DocumentoUploadRequest request)
{
    ...
}
```

### Validaciones obligatorias al recibir archivos *(nuevo en v8)*

Todo endpoint que reciba archivos debe validar en el backend, antes de guardar nada:

```text
1. Tamaño máximo institucional: 50 MB por archivo.
   Un endpoint puede definir un límite menor según su negocio;
   nunca mayor sin justificación documentada.

2. Tipos permitidos por LISTA BLANCA: solo documentos e imágenes —
   PDF, JPG, PNG, DOC, DOCX.
   Nunca ejecutables ni scripts (.exe, .bat, .dll, .ps1, .js, etc.).
   Se valida la extensión y el content-type contra la lista blanca;
   no se usa lista negra ("todo menos .exe"), porque siempre queda
   un tipo peligroso fuera de la lista.

3. Nombre físico generado por el sistema: el archivo se guarda como
   GUID + extensión (ej. 3f2a9c1e-....pdf). El nombre original que
   envió el usuario se guarda únicamente como metadata en la tabla,
   nunca como nombre físico — un nombre malicioso podría intentar
   escribir fuera de la carpeta destino.
```

Ejemplo del límite de tamaño en el endpoint:

```csharp
[HttpPost("subir")]
[RequestSizeLimit(52_428_800)] // 50 MB
public async Task<IActionResult> Subir([FromForm] DocumentoUploadRequest request)
{
    ...
}
```

### Flujo de almacenamiento *(nuevo en v8)*

```text
1. La API recibe el archivo y aplica las validaciones (tamaño, tipo).
2. Lo guarda de manera temporal con su nombre GUID + extensión.
3. El archivo se envía a su almacenamiento definitivo: una API
   institucional dedicada a archivos o un almacenamiento de objetos
   (ej. MinIO).
4. SQL Server guarda solo la metadata: nombre original, extensión,
   content-type, tamaño y la referencia al almacenamiento definitivo.
```

El detalle de cómo el servicio definitivo almacena internamente los archivos queda fuera del alcance de este manual.

### Excepción permitida para base64

Se permitirá recibir archivos en base64 únicamente cuando se trabaje con sistemas antiguos, servicios antiguos o procedures antiguos que ya funcionen de esa manera.

Ejemplo:

```text
Sistema antiguo:
Recibe documento en base64 dentro de JSON.

API nueva o módulo nuevo:
Puede aceptar temporalmente base64 solo por compatibilidad.
```

#### Reglas mínimas para base64 por compatibilidad

Cuando se reciba un archivo en base64 por excepción, se deberán considerar estas reglas mínimas:

```text
1. No usar base64 como estándar en proyectos nuevos.
2. Usar base64 solo cuando un sistema antiguo lo requiera.
3. No guardar el base64 completo en logs.
4. No confiar ciegamente en el nombre del archivo enviado por el usuario.
5. Documentar que el uso de base64 es por compatibilidad.
```

Ejemplo de DTO para compatibilidad:

```csharp
public class DocumentoBase64Request
{
    public Guid IdSolicitud { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string Base64 { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
```

Regla:

```text
Proyectos nuevos:
Usar multipart/form-data.

Sistemas antiguos:
Se permite base64 solo por compatibilidad.
```

### Almacenamiento futuro

A futuro se evaluará el uso de MinIO para almacenamiento de archivos.

Recomendación conceptual:

```text
SQL Server:
Guardar metadata del archivo.

MinIO u otro almacenamiento:
Guardar el archivo físico.
```

Ejemplo de metadata en SQL Server:

```text
IdDocumento
NombreArchivo
Extension
ContentType
TamanioBytes
RutaStorage
FechaCreate
IdUsuarioCreate
Activo
```

---

## 15. Worker Services y procesos por lotes

Un Worker Service es un proyecto C# que corre en segundo plano y no tiene pantalla.

Se usa para procesos automáticos que deben ejecutarse cada cierto tiempo o de forma continua.

Casos de uso reales:

- leer marcaciones biométricas desde un reloj o base externa;
- enviar correos pendientes;
- procesar archivos cargados por usuarios;
- sincronizar datos con otro sistema;
- consumir una cola de mensajes;
- actualizar estados de trámites vencidos;
- generar reportes programados;
- importar información desde archivos Excel o CSV;
- consultar un servicio externo cada cierto tiempo.

### Procesos por lotes

Un proceso por lote procesa muchos registros en una sola ejecución.

Ejemplos:

- enviar 500 correos pendientes;
- importar 10.000 filas de un Excel;
- procesar documentos pendientes;
- recalcular saldos;
- migrar información antigua;
- sincronizar todos los funcionarios activos.

### Diferencia práctica

```text
Worker Service:
Proyecto que corre en segundo plano.

Proceso por lote:
Trabajo que procesa muchos registros.
```

Un Worker Service puede ejecutar procesos por lotes.

### Evitar doble ejecución

Se puede controlar con una bandera, lock o estado en base de datos.

Ejemplo:

```sql
UPDATE TbProceso
SET EnEjecucion = 1
WHERE Nombre = 'ImportarDatos'
  AND EnEjecucion = 0;
```

Si no actualiza nada, significa que ya hay otro proceso ejecutándose.

También se puede registrar:

```text
FechaInicio
FechaFin
Estado
CantidadProcesada
Error
```

Este manual solo introduce el concepto. La implementación completa de Workers puede documentarse en una guía separada si el equipo lo necesita.

---

# PARTE 3 — SQL Server

## 16. Estándares de nombres en SQL Server

| Elemento | Estilo | Ejemplo |
|---|---|---|
| Tablas | Prefijo `Tb` + PascalCase | `TbCiudad` |
| Procedures | Prefijo `PA` + Entidad + Acción | `PA_Ciudad_Insertar` |
| Columnas | PascalCase | `IdCiudad`, `FechaCreate` |
| Parámetros | PascalCase con `@` | `@IdCiudad` |
| Vistas | Prefijo recomendado `Vw` | `VwCiudadActiva` |
| Funciones | Prefijo recomendado `FN` | `FN_ObtenerNombreCompleto` |

### Reglas

No usar:

- espacios;
- acentos;
- nombres ambiguos;
- mezcla de idiomas innecesaria;
- nombres excesivamente abreviados.

Ejemplo no recomendado:

```text
Fecha Creación
NOMBRE USUARIO
Id Usuario
```

Ejemplo recomendado:

```text
FechaCreate
NombreUsuario
IdUsuario
```

### Excepción heredada: sufijos de los campos de control *(nuevo en v8)*

Los sufijos `Create`/`Update`/`Delete` de los campos de control (`FechaCreate`, `IdUsuarioUpdate`, `FechaDelete`, etc.) mezclan inglés con español. Es un estándar institucional anterior a la regla de no mezclar idiomas y se mantiene por compatibilidad con todas las tablas, procedures y clases existentes: renombrarlos (`FechaCreacion`, `FechaModificacion`, `FechaBaja`) rompería todo lo construido a cambio de un beneficio solo cosmético.

Los demás campos de control ya están en español (`Activo`, `Motivo`) y se mantienen así.

Esta excepción no habilita a mezclar idiomas en campos nuevos: los campos de negocio se nombran en español, sin mezclas (`NombreUsuario`, no `NameUsuario`).

---

## 17. Estándares para tablas

Las tablas nuevas deben tener:

- llave primaria (`IdEntidad BIGINT IDENTITY`, ver [§18](#18-llaves-primarias));
- campos propios del negocio;
- campo `Activo`;
- campos de auditoría;
- campo `Motivo`.

Ejemplo *(actualizado en v7)*:

```sql
CREATE TABLE dbo.TbPersona
(
    IdPersona BIGINT IDENTITY(1,1) NOT NULL,

    Nombre NVARCHAR(150) NOT NULL,
    Apellido NVARCHAR(150) NOT NULL,
    Documento NVARCHAR(50) NOT NULL,

    Activo BIT NOT NULL DEFAULT 1,

    IdUsuarioCreate BIGINT NULL,
    FechaCreate DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),

    IdUsuarioUpdate BIGINT NULL,
    FechaUpdate DATETIME2(0) NULL,

    IdUsuarioDelete BIGINT NULL,
    FechaDelete DATETIME2(0) NULL,

    Motivo NVARCHAR(300) NULL,

    CONSTRAINT PK_TbPersona PRIMARY KEY CLUSTERED (IdPersona)
);
```

*(se eliminan `Accion`, `Detail` y la columna `Nro`; ver [§18](#18-llaves-primarias) y [§22](#22-activo-y-estado) para el detalle de por qué)*

---

## 18. Llaves primarias

Para proyectos nuevos se recomienda usar una llave primaria con el formato:

```text
Id + NombreEntidad
```

Ejemplos recomendados:

```text
TbPersona  → IdPersona
TbCiudad   → IdCiudad
TbUsuario  → IdUsuario
TbRol      → IdRol
```

No se recomienda que las llaves primarias de proyectos nuevos se llamen solamente `Id`, porque en consultas grandes, procedures, Dapper, Swagger y documentación se vuelve menos claro a qué entidad pertenece cada identificador.

### Ejemplo recomendado

```sql
CREATE TABLE dbo.TbPersona
(
    IdPersona BIGINT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(150) NOT NULL,

    CONSTRAINT PK_TbPersona PRIMARY KEY CLUSTERED (IdPersona)
);
```

Si otra tabla depende de persona:

```sql
CREATE TABLE dbo.TbAuto
(
    IdAuto BIGINT IDENTITY(1,1) NOT NULL,
    IdPersona BIGINT NOT NULL,
    Placa NVARCHAR(20) NOT NULL,

    CONSTRAINT PK_TbAuto PRIMARY KEY CLUSTERED (IdAuto),
    CONSTRAINT FK_TbAuto_TbPersona FOREIGN KEY (IdPersona)
        REFERENCES dbo.TbPersona(IdPersona)
);
```

Con esta regla queda claro:

```text
TbPersona.IdPersona   → llave primaria de Persona
TbAuto.IdPersona      → llave foránea hacia Persona
TbAuto.IdAuto         → llave primaria de Auto
```

### Ejemplo menos claro

```sql
SELECT 
    p.Id,
    a.Id,
    c.Id
FROM dbo.TbPersona p
INNER JOIN dbo.TbAuto a ON a.IdPersona = p.Id
INNER JOIN dbo.TbCiudad c ON c.Id = p.IdCiudad;
```

Aunque funciona, visualmente es menos entendible.

### Ejemplo más claro

```sql
SELECT 
    p.IdPersona,
    a.IdAuto,
    c.IdCiudad
FROM dbo.TbPersona p
INNER JOIN dbo.TbAuto a ON a.IdPersona = p.IdPersona
INNER JOIN dbo.TbCiudad c ON c.IdCiudad = p.IdCiudad;
```

Esta forma es más clara para:

- SQL Server;
- C#;
- Dapper;
- Swagger;
- procedimientos almacenados;
- documentación;
- la IA;
- otros desarrolladores.

### Regla institucional sobre llaves primarias *(actualizado en v7)*

La v6 presentaba `UNIQUEIDENTIFIER` y `BIGINT IDENTITY` como "dos opciones igualmente válidas" para la PK. La v7 simplifica esto: **`BIGINT IDENTITY` es la regla por defecto** para toda tabla nueva, y `UNIQUEIDENTIFIER` deja de ser una alternativa de uso general — pasa a ser la excepción, solo cuando existe una necesidad concreta.

Para proyectos nuevos, la llave primaria por defecto es:

```sql
IdEntidad BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY CLUSTERED
```

Ejemplo:

```sql
IdPersona BIGINT IDENTITY(1,1) NOT NULL,

CONSTRAINT PK_TbPersona PRIMARY KEY CLUSTERED (IdPersona)
```

Este identificador se usa para relaciones internas, llaves foráneas, procedures, joins y operaciones normales de base de datos — incluida la exposición en la API (`/api/persona/145`). Ya no se agrega un campo `Nro` aparte: si `IdEntidad` ya es `IDENTITY`, un `Nro` adicional sería redundante.

**Tablas existentes o proyectos anteriores** que ya usan `UNIQUEIDENTIFIER` como PK no se migran automáticamente. Se mantienen como están, salvo que exista una refactorización planificada y aprobada — migrar el tipo de una PK en producción afecta FKs, índices y procedures existentes, y no es una decisión que se resuelva desde este manual.

#### Cuándo sí agregar un `UNIQUEIDENTIFIER` adicional

Solo cuando exista una necesidad real, por ejemplo:

- integración con sistemas externos;
- sincronización distribuida (varios orígenes generando IDs sin coordinarse);
- generación del identificador fuera de SQL Server;
- exposición pública de un identificador que **no debe ser correlativo** (por ejemplo, un enlace de invitación o un token compartible).

En ese caso se agrega como columna adicional, no como reemplazo de la PK, con el prefijo `Uid` + nombre de entidad:

```sql
CREATE TABLE dbo.TbPersona
(
    IdPersona BIGINT IDENTITY(1,1) NOT NULL,
    UidPersona UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),

    Nombre NVARCHAR(150) NOT NULL,

    CONSTRAINT PK_TbPersona PRIMARY KEY CLUSTERED (IdPersona),
    CONSTRAINT UQ_TbPersona_UidPersona UNIQUE NONCLUSTERED (UidPersona)
);
```

`IdPersona` sigue siendo el clustering key físico (rápido, sin fragmentación de índice); `UidPersona` es el identificador público que se expone en la API, URLs o integraciones externas. Se usa `NEWSEQUENTIALID()` en vez de `NEWID()` para evitar fragmentar el índice nonclustered con inserciones aleatorias (`NEWID()` genera valores aleatorios; `NEWSEQUENTIALID()` genera valores crecientes en la vida de la instancia).

#### La seguridad no depende del tipo de ID

Que un ID sea correlativo (`145`) y no un GUID no es, por sí solo, un problema de seguridad. El riesgo real es de autorización: si un endpoint no valida que el usuario autenticado tenga permiso sobre el recurso solicitado, cualquier tipo de identificador puede terminar filtrando datos (un GUID también se filtra en logs, links compartidos o respuestas de otros endpoints). Por eso la regla institucional es:

```text
Todo endpoint debe validar en el backend que el usuario autenticado
tenga permiso para consultar, modificar o eliminar el recurso solicitado,
independientemente de si el identificador es correlativo o no.
```

Un `UNIQUEIDENTIFIER` es una capa adicional de defensa en profundidad (dificulta la enumeración a ciegas), pero nunca reemplaza la validación de autorización en el endpoint.

*(nuevo en v8: el patrón concreto recomendado para implementar esta validación está en §40 — Autorización por recurso.)*

### Proyectos antiguos

Si un sistema antiguo ya tiene columnas llamadas `Id` y funciona correctamente, no es obligatorio cambiarlo solo por cumplir el estándar.

Regla:

```text
Proyectos nuevos:
Usar IdEntidad BIGINT IDENTITY.

Proyectos antiguos:
Mantener Id (o UNIQUEIDENTIFIER) si ya existe y cambiarlo solo si hay una refactorización planificada.
```

---

## 19. Llaves foráneas

Las llaves foráneas deberán usar el mismo nombre de la llave primaria de la tabla referenciada.

Ejemplos:

```text
TbPersona.IdPersona → llave primaria
TbAuto.IdPersona    → llave foránea hacia Persona

TbCiudad.IdCiudad   → llave primaria
TbPersona.IdCiudad  → llave foránea hacia Ciudad
```

Esto mantiene claridad tanto en SQL Server como en C#.

Ejemplo:

```sql
CREATE TABLE dbo.TbPersona
(
    IdPersona BIGINT IDENTITY(1,1) NOT NULL,
    IdCiudad BIGINT NOT NULL,
    Nombre NVARCHAR(150) NOT NULL,

    CONSTRAINT PK_TbPersona PRIMARY KEY CLUSTERED (IdPersona),
    CONSTRAINT FK_TbPersona_TbCiudad FOREIGN KEY (IdCiudad)
        REFERENCES dbo.TbCiudad(IdCiudad)
);
```

---

## 20. Auditoría

Todas las tablas nuevas deberán tener campos de auditoría.

Campos recomendados:

```sql
IdUsuarioCreate BIGINT NULL,
FechaCreate DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),

IdUsuarioUpdate BIGINT NULL,
FechaUpdate DATETIME2(0) NULL,

IdUsuarioDelete BIGINT NULL,
FechaDelete DATETIME2(0) NULL,

Motivo NVARCHAR(300) NULL
```

El tipo de dato de `IdUsuarioCreate`, `IdUsuarioUpdate` e `IdUsuarioDelete` debe coincidir con el tipo de dato del ID principal de la tabla de usuarios.

Por defecto, `TbUsuario.IdUsuario` es `BIGINT` (ver [§18](#18-llaves-primarias)), por lo que los campos de auditoría serán `BIGINT`.

Si `TbUsuario` es un caso excepcional que usa `UNIQUEIDENTIFIER` como PK, los campos de auditoría deberán ser `UNIQUEIDENTIFIER` para poder referenciarlo.

Estos campos guardan solo el último cambio de cada tipo (ver la nota de alcance de trazabilidad en §5.3): el historial completo de modificaciones, cuando el negocio lo exige, se obtiene con temporal tables (§34). *(nuevo en v8)*

---

## 21. Fechas

Para campos `DATETIME2`, se recomienda usar:

```sql
SYSDATETIME()
```

Ejemplo:

```sql
FechaCreate DATETIME2(0) NOT NULL DEFAULT SYSDATETIME()
```

`SYSDATETIME()` devuelve fecha y hora local del servidor con precisión compatible con `DATETIME2`.

Se decidió usar hora local del servidor por facilidad para reportes institucionales.

**Deuda declarada: hora local vs UTC** *(nuevo en v8)*: la práctica general de la industria es almacenar UTC (`SYSUTCDATETIME()`) y convertir a hora local solo al mostrar. La decisión institucional de usar hora local es consciente y se sostiene porque Bolivia (UTC-4) no tiene horario de verano y todos los sistemas corren en la misma zona. Si en el futuro hubiera integración con sistemas externos o infraestructura en otra zona horaria, las fechas dejarían de ser comparables entre sí y deberá evaluarse la migración a UTC.

---

## 22. Activo y Estado

*(renombrado en v7: se elimina la subsección `Accion` — ver justificación abajo)*

### 22.1 Activo

Indica si el registro está vigente.

```sql
Activo BIT NOT NULL DEFAULT 1
```

Valores:

```text
1 = activo
0 = dado de baja
```

### 22.1.1 Por qué ya no existe `Accion` *(nuevo en v7)*

Las versiones anteriores mantenían una columna `Accion CHAR(1)` (`C`/`U`/`D`) para indicar la última acción realizada sobre el registro. Se elimina en v7 porque es información derivable de las columnas de auditoría que ya existen — no aporta un dato nuevo, solo lo duplica:

```text
Si FechaDelete IS NOT NULL         → el registro fue dado de baja.
Si FechaUpdate IS NOT NULL         → el registro fue actualizado (y no dado de baja).
Si ninguno de los dos está poblado → el registro solo fue creado.
```

Mantener `Accion` como columna aparte obliga a sincronizarla manualmente en cada `INSERT`/`UPDATE`/procedure, con el riesgo de que quede desactualizada si alguien actualiza las fechas pero olvida actualizar `Accion` (o viceversa). El campo `Detail` se renombra a `Motivo` y pasa a ser el único campo de texto libre para justificar una operación (por qué se dio de baja, por qué se reactivó, etc.).

Si en algún momento se necesita un reporte que dependa de distinguir tipos de acción de forma estructurada y consultable (por ejemplo, "cuántas reactivaciones hubo este mes"), la solución no es reintroducir `Accion` en la tabla principal, sino crear una tabla de historial aparte (`TbPersonaHistorial`) — no vale la pena cargar la regla general con esa complejidad para un caso excepcional.

### 22.2 Estado

`Estado` solo debe usarse cuando represente un estado real del negocio.

Ejemplos:

```text
EstadoTicket: PENDIENTE, ASIGNADO, ATENDIDO, CERRADO
EstadoTramite: RECEPCIONADO, DERIVADO, OBSERVADO, FINALIZADO
```

No se debe usar `Estado` para reemplazar a `Activo`.

### 22.3 Precedencia entre Activo y Estado *(nuevo en v6)*

La v5 explicaba bien el concepto de cada campo por separado, pero no definía qué pasa cuando ambos existen en la misma tabla y parecen solaparse.

Caso real: `TbTramite` tiene `EstadoTramite = 'CANCELADO'`. ¿Eso implica `Activo = 0`?

Regla:

```text
Activo controla si el registro existe funcionalmente en el CRUD (aparece en listados normales, se puede editar, etc.).
Estado controla el flujo de negocio de ese registro.

Un estado terminal (CANCELADO, FINALIZADO, RECHAZADO, etc.) NO implica automáticamente Activo = 0.
Activo = 0 se usa únicamente cuando el registro ya no debe aparecer como vigente en el CRUD — es decir, cuando alguien ejecuta una baja lógica explícita sobre el registro, sea cual sea su Estado.
```

Ejemplo: un trámite `CANCELADO` sigue apareciendo en el listado de trámites (con su estado visible) porque `Activo` sigue en `1`; solo pasa a `Activo = 0` si un usuario decide eliminarlo lógicamente del sistema (por ejemplo, un trámite cargado por error).

---

## 23. Lógica de negocio para campos de control

Los campos de control no deberán ser enviados completos desde el frontend.

En las operaciones de inserción, actualización y baja lógica se enviará un solo parámetro de usuario:

```text
@IdUsuario
```

Luego, dentro del procedimiento almacenado se asignará ese usuario al campo que corresponda según la operación:

```text
INSERT  → IdUsuarioCreate
UPDATE  → IdUsuarioUpdate
DELETE  → IdUsuarioDelete
```

Regla general:

```text
El frontend envía datos de negocio.
El backend valida y determina el usuario de la operación.
El procedure recibe @IdUsuario.
SQL Server completa los campos de control según la acción.
```

El frontend no debe enviar campos como `Activo`, `FechaCreate`, `FechaUpdate`, `FechaDelete`, `IdUsuarioCreate`, `IdUsuarioUpdate` ni `IdUsuarioDelete`, salvo el caso de actualización y baja lógica donde se enviará un motivo para registrar en `Motivo`.

### 23.1 Insertar

En una inserción se envía:

```text
@IdUsuario
```

Dentro del procedure, `@IdUsuario` se asigna a:

```text
IdUsuarioCreate
```

No se envían desde el frontend:

```text
FechaCreate
IdUsuarioUpdate
FechaUpdate
IdUsuarioDelete
FechaDelete
Activo
```

Valores que se establecen:

```text
FechaCreate = SYSDATETIME()
Activo      = 1
```

Ejemplo:

```sql
CREATE PROCEDURE dbo.PA_Persona_Insertar
    @Nombre NVARCHAR(150),
    @Apellido NVARCHAR(150),
    @Documento NVARCHAR(50),
    @IdUsuario BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.TbPersona
    (
        Nombre,
        Apellido,
        Documento,
        Activo,
        IdUsuarioCreate,
        FechaCreate
    )
    VALUES
    (
        @Nombre,
        @Apellido,
        @Documento,
        1,
        @IdUsuario,
        SYSDATETIME()
    );

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS IdPersona;
END;
```

**Por qué el `CAST`** *(nuevo en v8)*: `SCOPE_IDENTITY()` devuelve `NUMERIC(38,0)`, no `BIGINT`. Sin el `CAST`, el mapeo de Dapper a `long` depende de una conversión implícita frágil que puede fallar según la versión del driver. La regla es devolver siempre el ID nuevo con `CAST(SCOPE_IDENTITY() AS BIGINT)`.

### 23.2 Actualizar

En una actualización se envía:

```text
@IdUsuario
@Motivo
```

Dentro del procedure, `@IdUsuario` se asigna a:

```text
IdUsuarioUpdate
```

El campo `@Motivo` se registra en:

```text
Motivo
```

No se deben modificar:

```text
IdUsuarioCreate
FechaCreate
IdUsuarioDelete
FechaDelete
Activo
```

Valores que se establecen:

```text
FechaUpdate = SYSDATETIME()
Motivo      = @Motivo
```

Si no se envía un motivo, `Motivo` queda en `NULL` *(actualizado en v8)*: `NULL` ya significa "sin motivo". No se usan textos de relleno como `'Modificado'` — no aportan información (la acción ya es derivable de `FechaUpdate`) e impiden distinguir después "no se pidió motivo" de "el usuario escribió eso". Si un flujo de negocio exige motivo obligatorio, esa obligación se valida en el Service, no se disimula con un valor por defecto en SQL.

Ejemplo:

```sql
CREATE PROCEDURE dbo.PA_Persona_Actualizar
    @IdPersona BIGINT,
    @Nombre NVARCHAR(150),
    @Apellido NVARCHAR(150),
    @Documento NVARCHAR(50),
    @IdUsuario BIGINT,
    @Motivo NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TbPersona
    SET
        Nombre = @Nombre,
        Apellido = @Apellido,
        Documento = @Documento,
        IdUsuarioUpdate = @IdUsuario,
        FechaUpdate = SYSDATETIME(),
        Motivo = NULLIF(@Motivo, '')
    WHERE IdPersona = @IdPersona
      AND Activo = 1;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
```

### 23.3 Dar de baja

En una baja lógica se envía:

```text
@IdUsuario
@Motivo
```

Dentro del procedure, `@IdUsuario` se asigna a:

```text
IdUsuarioDelete
```

El campo `@Motivo` se registra en:

```text
Motivo
```

No se deben modificar:

```text
IdUsuarioCreate
FechaCreate
IdUsuarioUpdate
FechaUpdate
```

Valores que se establecen:

```text
FechaDelete = SYSDATETIME()
Motivo      = @Motivo
Activo      = 0
```

Si no se envía un motivo, `Motivo` queda en `NULL` *(actualizado en v8)*: la baja ya es identificable por `FechaDelete` y `Activo = 0`, un texto de relleno como `'Dado de baja'` no agrega nada.

Ejemplo:

```sql
CREATE PROCEDURE dbo.PA_Persona_DarBaja
    @IdPersona BIGINT,
    @IdUsuario BIGINT,
    @Motivo NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TbPersona
    SET
        Activo = 0,
        IdUsuarioDelete = @IdUsuario,
        FechaDelete = SYSDATETIME(),
        Motivo = NULLIF(@Motivo, '')
    WHERE IdPersona = @IdPersona
      AND Activo = 1;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
```

### 23.4 Regla resumida

| Acción | Parámetros enviados | Usuario que se registra | Fecha que se registra | Motivo | Activo |
|---|---|---|---|---|---:|
| Insertar | `@IdUsuario` | `IdUsuarioCreate` | `FechaCreate` | `NULL` | `1` |
| Actualizar | `@IdUsuario`, `@Motivo` | `IdUsuarioUpdate` | `FechaUpdate` | `@Motivo` o `NULL` | No cambia |
| Dar de baja | `@IdUsuario`, `@Motivo` | `IdUsuarioDelete` | `FechaDelete` | `@Motivo` o `NULL` | `0` |
| Reactivar *(nuevo)* | `@IdUsuario`, `@Motivo` | `IdUsuarioUpdate` | `FechaUpdate` | `'Reactivado'` + motivo si se envía | `1` |

Esta lógica permite mantener trazabilidad básica sin que el frontend tenga que enviar campos internos de auditoría.

### 23.5 Filas afectadas: `@@ROWCOUNT` obligatorio *(nuevo en v8)*

Todo procedure de **actualización, baja lógica y reactivación** debe terminar devolviendo las filas afectadas:

```sql
SELECT @@ROWCOUNT AS FilasAfectadas;
```

**Por qué es obligatorio.** El `WHERE IdPersona = @IdPersona AND Activo = 1` protege contra editar registros inexistentes o dados de baja, pero cuando no encuentra nada el UPDATE afecta 0 filas **sin ningún error**: el procedure termina "bien", la API devuelve éxito y el usuario cree que guardó un cambio que no ocurrió. Es un éxito falso. Devolver `FilasAfectadas` le da a la capa C# la información para distinguir "operación aplicada" de "no encontré nada".

La capa C# debe verificar el resultado y convertir el `0` en un error controlado:

```csharp
var filasAfectadas = await connection.ExecuteScalarAsync<int>(
    "PA_Persona_Actualizar",
    parametros,
    commandType: CommandType.StoredProcedure);

if (filasAfectadas == 0)
{
    // El registro no existe o ya está inactivo.
    // Devolver el error controlado estándar (ApiResponse con success = false),
    // nunca un éxito.
    return ApiResponse<object>.Error("Registro no encontrado o inactivo.");
}
```

Notas:

- El error controlado por `FilasAfectadas = 0` se responde como el "Registro no encontrado" estándar: `404 Not Found` (ver la tabla de códigos HTTP en §12).
- En inserciones no aplica: el procedure de insertar ya devuelve el ID nuevo (`CAST(SCOPE_IDENTITY() AS BIGINT)`), y si el INSERT falla lanza error.
- El procedure de actualización con control de concurrencia (`RowVersion`, ver §32) ya devolvía `FilasAfectadas`; esta regla generaliza ese mismo patrón a todos los UPDATE, bajas y reactivaciones.
- `SET NOCOUNT ON` no afecta a `@@ROWCOUNT`: solo suprime los mensajes "N rows affected", el valor sigue disponible.

---

## 24. Baja lógica

No se deben realizar eliminaciones físicas en CRUDs estándar de negocio.

La baja lógica se realizará actualizando:

```sql
Activo = 0
IdUsuarioDelete = @IdUsuario
FechaDelete = SYSDATETIME()
Motivo = NULLIF(@Motivo, '')
```

Ejemplo:

```sql
UPDATE dbo.TbPersona
SET
    Activo = 0,
    IdUsuarioDelete = @IdUsuario,
    FechaDelete = SYSDATETIME(),
    Motivo = NULLIF(@Motivo, '')
WHERE IdPersona = @IdPersona
  AND Activo = 1;

SELECT @@ROWCOUNT AS FilasAfectadas;
```

*(actualizado en v8: sin motivo enviado, `Motivo` queda `NULL` — ver §23.3; y toda baja devuelve `FilasAfectadas` — ver §23.5)*

Los listados deberán filtrar solamente registros activos:

```sql
WHERE Activo = 1
```

### Reactivación de un registro dado de baja *(nuevo en v6)*

La v5 definía la baja lógica pero no decía qué hacer cuando el negocio necesita revertirla — por ejemplo, una ciudad dada de baja por error, un funcionario marcado inactivo por error, o un parámetro eliminado lógicamente que se quiere recuperar.

Regla: la reactivación **no** es un nuevo `INSERT` ni deja `FechaDelete` como referencia "viva" — es una actualización que limpia el estado de baja.

Procedure estándar:

```sql
CREATE PROCEDURE dbo.PA_Persona_Reactivar
    @IdPersona BIGINT,
    @IdUsuario BIGINT,
    @Motivo NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TbPersona
    SET
        Activo = 1,
        IdUsuarioUpdate = @IdUsuario,
        FechaUpdate = SYSDATETIME(),
        Motivo = 'Reactivado' + ISNULL(': ' + NULLIF(@Motivo, ''), '')
    WHERE IdPersona = @IdPersona
      AND Activo = 0;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
```

Reglas explícitas:

```text
Activo vuelve a 1.
Se usa IdUsuarioUpdate/FechaUpdate — no un campo nuevo de "reactivación" (es una actualización, no una nueva creación).
FechaDelete NO se limpia: queda como registro histórico de que hubo una baja anterior.
Motivo documenta explícitamente que fue una reactivación, con el motivo si se especifica.
```

Si la tabla tiene índice único filtrado (`WHERE Activo = 1`, ver §31 — Duplicados críticos), la reactivación puede fallar por duplicado si mientras tanto se creó un nuevo registro activo con el mismo valor único (ej. mismo `Documento`). En ese caso el procedure debe devolver un error de duplicado igual que en un insert, no un error genérico.

### Excepciones controladas para eliminación física

La eliminación física podrá evaluarse solo en casos que no representen información principal del negocio o que no requieran trazabilidad histórica.

Ejemplos:

#### Tablas temporales

Tablas creadas para cálculos intermedios o procesos que solo viven durante una operación.

Ejemplo:

```sql
CREATE TABLE #TmpImportacion
(
    Documento NVARCHAR(50),
    Nombre NVARCHAR(150)
);
```

Estas tablas pueden eliminarse físicamente porque no representan datos finales del sistema.

#### Tablas staging o de carga

Tablas usadas para recibir información antes de validarla e insertarla en tablas definitivas.

Ejemplo:

```text
TbCargaPersonaTemporal
TbStagingImportacionExcel
TbCargaMasivaVehiculos
```

Una vez procesada la carga, se puede limpiar físicamente la tabla staging si existe una política definida.

#### Registros de prueba controlados

Registros creados en ambientes de desarrollo o pruebas, no en producción, y que no forman parte de la operación real.

Ejemplo:

```text
Personas de prueba creadas para validar una pantalla antes de liberar el módulo.
```

#### Logs antiguos

Si existiera una política separada de retención de logs, podrían eliminarse físicamente registros antiguos.

Ejemplo:

```text
Eliminar logs mayores a 2 años, si la institución define esa política.
```

Este manual no desarrolla el manejo de logs, pero reconoce que podrían existir políticas separadas de limpieza.

#### Archivos huérfanos o datos auxiliares sin valor histórico

Ejemplo:

```text
Un archivo subido al servidor, pero que nunca fue confirmado ni asociado a un registro final.
```

En ese caso se podría eliminar el archivo físico o el registro auxiliar, si no forma parte de un trámite, documento o proceso oficial.

#### Detalles recién creados por error antes de confirmar una operación

Ejemplo:

```text
Se creó temporalmente el detalle de una factura, pero la cabecera nunca fue confirmada.
```

Si el proceso no fue confirmado y no tiene valor histórico, puede evaluarse su eliminación física.

Regla:

```text
CRUD de negocio = baja lógica obligatoria (con reactivación disponible si el negocio lo requiere).
Datos temporales, staging o auxiliares = eliminación física solo si está justificada.
```

---

## 25. Procedimientos almacenados

Los CRUDs deberán manejar procedures por acción.

Formato:

```text
PA_Entidad_Accion
```

Ejemplo:

```text
PA_Persona_Listar
PA_Persona_ObtenerPorId
PA_Persona_Insertar
PA_Persona_Actualizar
PA_Persona_DarBaja
PA_Persona_Reactivar
```

No se recomienda usar un procedure único con un parámetro `@Accion` para hacer todo.

---

## 26. Parámetros en procedures

Los procedures deberán usar parámetros explícitos.

No se usará JSON como único parámetro.

### Usuario auditor

Para operaciones de inserción, actualización, baja lógica y reactivación, el procedure recibirá un parámetro genérico:

```text
@IdUsuario
```

No se enviarán como parámetros separados:

```text
@IdUsuarioCreate
@IdUsuarioUpdate
@IdUsuarioDelete
```

El procedure asignará `@IdUsuario` al campo correspondiente según la acción:

| Acción | Campo de auditoría que recibe `@IdUsuario` |
|---|---|
| Insertar | `IdUsuarioCreate` |
| Actualizar | `IdUsuarioUpdate` |
| Dar de baja | `IdUsuarioDelete` |
| Reactivar | `IdUsuarioUpdate` |

El tipo de dato de `@IdUsuario` debe coincidir con el tipo de dato de la llave primaria de `TbUsuario`.

Ejemplo por defecto, si `TbUsuario.IdUsuario` es `BIGINT`:

```sql
@IdUsuario BIGINT
```

Ejemplo si `TbUsuario` es un caso excepcional que usa `UNIQUEIDENTIFIER` (ver [§18](#18-llaves-primarias)):

```sql
@IdUsuario UNIQUEIDENTIFIER
```

### Ejemplo recomendado de inserción

```sql
CREATE PROCEDURE dbo.PA_Persona_Insertar
    @Nombre NVARCHAR(150),
    @Apellido NVARCHAR(150),
    @Documento NVARCHAR(50),
    @IdUsuario BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.TbPersona
    (
        Nombre,
        Apellido,
        Documento,
        Activo,
        IdUsuarioCreate,
        FechaCreate
    )
    VALUES
    (
        @Nombre,
        @Apellido,
        @Documento,
        1,
        @IdUsuario,
        SYSDATETIME()
    );

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS IdPersona;
END;
```

### Ejemplo recomendado de actualización

```sql
CREATE PROCEDURE dbo.PA_Persona_Actualizar
    @IdPersona BIGINT,
    @Nombre NVARCHAR(150),
    @Apellido NVARCHAR(150),
    @Documento NVARCHAR(50),
    @IdUsuario BIGINT,
    @Motivo NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TbPersona
    SET
        Nombre = @Nombre,
        Apellido = @Apellido,
        Documento = @Documento,
        IdUsuarioUpdate = @IdUsuario,
        FechaUpdate = SYSDATETIME(),
        Motivo = NULLIF(@Motivo, '')
    WHERE IdPersona = @IdPersona
      AND Activo = 1;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
```

### Ejemplo recomendado de baja lógica

```sql
CREATE PROCEDURE dbo.PA_Persona_DarBaja
    @IdPersona BIGINT,
    @IdUsuario BIGINT,
    @Motivo NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TbPersona
    SET
        Activo = 0,
        IdUsuarioDelete = @IdUsuario,
        FechaDelete = SYSDATETIME(),
        Motivo = NULLIF(@Motivo, '')
    WHERE IdPersona = @IdPersona
      AND Activo = 1;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
```

### Contrato de versionado de stored procedures *(nuevo en v6)*

Varios sistemas pueden compartir el mismo procedure (por ejemplo, una API nueva y un WinForms antiguo consumiendo `PA_Persona_Actualizar`). Para no romper a los consumidores existentes cuando el procedure cambia:

```text
1. Nunca eliminar un parámetro existente de un procedure ya publicado/consumido.
2. Nunca reordenar los parámetros existentes.
3. Los parámetros nuevos se agregan siempre al final y con un valor DEFAULT (para no exigirlos a los consumidores actuales).
4. Si un cambio realmente requiere romper el contrato (ej. cambiar el tipo de un parámetro), se crea un procedure nuevo versionado (PA_Persona_Actualizar_V2) en vez de modificar el existente, y se coordina la migración de los consumidores antes de retirar el anterior.
```

Ejemplo de cómo agregar un parámetro sin romper compatibilidad:

```sql
-- Antes
CREATE PROCEDURE dbo.PA_Persona_Insertar
    @Nombre NVARCHAR(150),
    @Apellido NVARCHAR(150),
    @Documento NVARCHAR(50),
    @IdUsuario BIGINT
AS ...

-- Después: se agrega @Celular al final, con DEFAULT
CREATE PROCEDURE dbo.PA_Persona_Insertar
    @Nombre NVARCHAR(150),
    @Apellido NVARCHAR(150),
    @Documento NVARCHAR(50),
    @IdUsuario BIGINT,
    @Celular NVARCHAR(30) = NULL
AS ...
```

Con esto, los consumidores que ya llaman al procedure sin `@Celular` siguen funcionando sin cambios.

---

## 27. Por qué evitar JSON como único parámetro

Antes se usaba:

```sql
PA_Persona_Insertar @jsonDato NVARCHAR(MAX)
```

Esto se evitará porque:

- dificulta seguir la lógica;
- complica depuración;
- traslada validaciones al procedure;
- oculta los campos reales requeridos;
- dificulta documentar servicios;
- hace más difícil trabajar con Swagger;
- hace más difícil que la IA entienda el flujo.

Nuevo estándar:

```sql
PA_Persona_Insertar
    @Nombre,
    @Apellido,
    @Documento,
    @IdUsuario
```

El procedure decide si `@IdUsuario` se registra como `IdUsuarioCreate`, `IdUsuarioUpdate` o `IdUsuarioDelete`, según la acción.

### Excepción controlada para JSON en procedures antiguos

Como regla general, los procedimientos almacenados de proyectos nuevos no deberán recibir JSON como único parámetro.

Para proyectos nuevos se deberán usar parámetros explícitos en los procedures.

Ejemplo recomendado:

```sql
CREATE PROCEDURE dbo.PA_Ciudad_Insertar
    @Descripcion NVARCHAR(150),
    @IdDepartamento BIGINT,
    @IdUsuario BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    -- Lógica de inserción
END;
```

No recomendado para proyectos nuevos:

```sql
CREATE PROCEDURE dbo.PA_Ciudad_Insertar
    @jsonDato NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- Leer todo desde JSON
END;
```

#### Excepción permitida

Se permitirá JSON como único parámetro únicamente cuando se trabaje con procedures antiguos de sistemas antiguos que ya fueron construidos de esa manera.

Esta excepción aplica para casos de mantenimiento, compatibilidad o integración con sistemas heredados.

Ejemplo:

```text
Sistema antiguo:
Ya tiene un procedure WebInsertarDetalleUsuario que recibe @jsonDato.

API nueva o módulo nuevo:
Puede recibir un DTO tipado y luego serializarlo a JSON solo para consumir ese procedure antiguo.
```

#### Regla para C#

Aunque el procedure antiguo reciba JSON, el controller no debe recibir un string JSON libre.

El controller debe recibir un DTO tipado, con propiedades en español como el resto del manual exige (**corregido en v6** — el ejemplo de la v5 mezclaba inglés y español; se corrige a continuación).

Ejemplo:

```csharp
public class InsertarDetalleUsuarioRequest
{
    public Guid IdUsuario { get; set; }
    public List<DetalleUsuarioItemRequest> Lista { get; set; } = new();
}

public class DetalleUsuarioItemRequest
{
    public Guid IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Apodo { get; set; } = string.Empty;
    public string DocumentoIdentidad { get; set; } = string.Empty;
    public string? CodigoMunicipal { get; set; }
    public Guid IdSolicitud { get; set; }
    public int Activo { get; set; } = 1;
}
```

Nota: este DTO refleja el contrato del procedure antiguo tal cual existe — por eso usa `Guid` como ID e incluye `Activo` como `int`. En sistemas nuevos eso no está permitido: los IDs son `BIGINT`/`long` (§18) y `Activo` jamás se envía desde el cliente (§23).

Luego el repository puede convertir el DTO a JSON para llamar al procedure antiguo.

Ejemplo:

```csharp
public async Task InsertarDetalleUsuarioAsync(InsertarDetalleUsuarioRequest request)
{
    var jsonDato = JsonSerializer.Serialize(request);

    await using var connection = new SqlConnection(_connectionString);

    await connection.ExecuteAsync(
        "dbo.WebInsertarDetalleUsuario",
        new { jsonDato },
        commandType: CommandType.StoredProcedure
    );
}
```

Flujo recomendado:

```text
Frontend → DTO tipado
Controller → recibe el request
Service → aplica la lógica necesaria
Repository → serializa a JSON solo por compatibilidad
SQL Server → procedure antiguo recibe @jsonDato
```

Regla:

```text
Proyectos nuevos:
No usar JSON como único parámetro.

Sistemas antiguos:
Se permite JSON únicamente por compatibilidad con procedures existentes.
```

---

## 28. Procedures de inserción

Los procedures de inserción deberán devolver el ID creado.

Ejemplo *(actualizado en v8)*:

```sql
SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS IdPersona;
```

El `CAST` es obligatorio porque `SCOPE_IDENTITY()` devuelve `NUMERIC(38,0)`, no `BIGINT` (ver §23.1).

Esto permite que C# pueda continuar el flujo, devolver el ID al frontend o usarlo para insertar datos relacionados.

---

## 29. Manejo de errores en SQL Server

SQL Server podrá usar `TRY/CATCH` cuando haya una transacción interna.

Ejemplo:

```sql
BEGIN TRY
    BEGIN TRANSACTION;

    -- operaciones

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH
```

Sin embargo, el manejo final del error será en C#.

---

## 30. Validaciones

El frontend puede validar para mejorar la experiencia del usuario, pero el backend debe validar lo importante.

### Frontend

Validaciones sugeridas:

- campos requeridos;
- formato de correo;
- longitud máxima;
- campos numéricos;
- fechas obligatorias.

### Backend

Debe validar:

- campos requeridos;
- permisos;
- reglas de negocio;
- duplicados importantes;
- datos obligatorios antes de llamar al procedure.

No se debe confiar solamente en el frontend.

### SQL Server

SQL Server debe proteger la integridad de los datos. Esto significa que, aunque el frontend y el backend validen, la base de datos debe tener reglas mínimas para evitar datos incoherentes.

#### Llave primaria

Garantiza que cada registro tenga un identificador único.

```sql
CONSTRAINT PK_TbPersona PRIMARY KEY (IdPersona)
```

#### Llave foránea

Garantiza que un registro relacionado exista en la tabla padre.

Ejemplo: una persona pertenece a una ciudad existente.

```sql
CONSTRAINT FK_TbPersona_TbCiudad FOREIGN KEY (IdCiudad)
    REFERENCES dbo.TbCiudad(IdCiudad)
```

Con esto, SQL Server no permitirá registrar una persona con un `IdCiudad` que no exista en `TbCiudad`.

#### Restricciones CHECK

Sirven para limitar valores permitidos.

Ejemplo: validar que `EstadoTicket` solo acepte valores conocidos del negocio (ver [§22.2](#222-estado)).

```sql
CONSTRAINT CK_TbTicket_EstadoTicket
CHECK (EstadoTicket IN ('PENDIENTE', 'ASIGNADO', 'ATENDIDO', 'CERRADO'))
```

Ejemplo: validar que el campo `Activo` solo sea 0 o 1. En `BIT` esto ya está controlado por el tipo de dato, pero en otros campos puede ser necesario.

#### Índices únicos

Sirven para impedir duplicados cuando un dato debe ser único para el negocio.

Ejemplo: evitar dos usuarios activos con la misma cuenta.

```sql
CREATE UNIQUE INDEX UX_TbUsuario_Cuenta
ON dbo.TbUsuario(Cuenta)
WHERE Activo = 1;
```

Ejemplo: evitar dos ciudades activas con el mismo código.

```sql
CREATE UNIQUE INDEX UX_TbCiudad_Codigo
ON dbo.TbCiudad(Codigo)
WHERE Activo = 1;
```

La condición `WHERE Activo = 1` permite que un registro dado de baja no bloquee necesariamente la creación de uno nuevo, si el negocio lo permite. Ver también §24 sobre qué pasa cuando en vez de crear uno nuevo se reactiva el existente.

Regla:

```text
Frontend = experiencia de usuario.
Backend = validación de negocio.
SQL Server = integridad y protección final de datos.
```

---

## 31. Duplicados críticos

Los duplicados críticos se evaluarán según el caso.

Cuando un dato sea realmente único para el negocio, se recomienda validarlo en backend y protegerlo en SQL Server.

Ejemplos:

- cuenta de usuario;
- CI;
- código de ciudad;
- número de trámite;
- correo institucional.

### Protección en SQL Server

Ejemplo:

```sql
CREATE UNIQUE INDEX UX_TbUsuario_Cuenta
ON dbo.TbUsuario(Cuenta)
WHERE Activo = 1;
```

### Respuesta recomendada ante duplicado

Cuando exista un duplicado crítico, el backend debe devolver un mensaje amigable y mantener el formato `ApiResponse<T>`.

Ejemplo:

```json
{
  "success": false,
  "message": "Ya existe una persona registrada con el mismo documento.",
  "data": {
    "campo": "documento"
  }
}
```

No se debe mostrar al usuario el error técnico de SQL Server.

### ¿Cómo se programa esta respuesta?

La validación puede hacerse antes de insertar, desde el service.

Ejemplo en el service:

```csharp
public async Task<ApiResponse<object>> InsertarAsync(PersonaCreateRequest request, long idUsuario)
{
    if (string.IsNullOrWhiteSpace(request.Documento))
    {
        return ApiResponse<object>.Validation(
            new { errores = new[] { new { campo = "documento", mensaje = "El documento es obligatorio." } } },
            "Existen campos obligatorios o inválidos."
        );
    }

    var existeDocumento = await _personaRepository.ExisteDocumentoAsync(request.Documento);

    if (existeDocumento)
    {
        return ApiResponse<object>.ErrorCampo(
            "Ya existe una persona registrada con el mismo documento.",
            "documento"
        );
    }

    var idPersona = await _personaRepository.InsertarAsync(request, idUsuario);

    return ApiResponse<object>.Ok(
        new { idPersona },
        "Registro guardado correctamente."
    );
}
```

Para poder devolver el campo afectado, se puede agregar un método estático al `ApiResponse<T>`:

```csharp
public static ApiResponse<object> ErrorCampo(string message, string campo)
{
    return new ApiResponse<object>
    {
        Success = false,
        Message = message,
        Data = new { campo }
    };
}
```

Y en el controller se puede responder con `400 Bad Request`:

```csharp
[HttpPost]
public async Task<IActionResult> Insertar([FromBody] PersonaCreateRequest request)
{
    var idUsuario = ObtenerIdUsuarioDesdeToken();
    var respuesta = await _personaService.InsertarAsync(request, idUsuario);

    if (!respuesta.Success)
        return BadRequest(respuesta);

    return Ok(respuesta);
}
```

También se debe mantener la protección en SQL Server con un índice único cuando corresponda, porque el backend puede fallar, puede existir concurrencia o puede insertarse información desde otro proceso.

Regla:

```text
Duplicado crítico = validar en backend + proteger en SQL Server + responder con mensaje amigable.
```

---

## 32. Transacciones

Las transacciones se manejarán principalmente desde C# cuando el proceso involucre varias operaciones.

Ejemplo:

```text
Crear persona
Crear autos
Confirmar todo
```

Si una operación falla, se revierte todo.

Flujo:

```text
Abrir conexión
Iniciar transacción
Ejecutar procedures
Confirmar
Revertir si falla
```

Si toda la operación pertenece únicamente a SQL Server, se puede evaluar una transacción en el procedure.

### Caso de uso ficticio

Supongamos que se debe crear una persona y registrar sus autos. Si falla el registro de un auto, también debe revertirse la persona.

Ejemplo conceptual en un service usando Dapper:

```csharp
public async Task<ApiResponse<object>> CrearPersonaConAutosAsync(
    PersonaConAutosCreateRequest request,
    long idUsuario)
{
    await using var connection = DbConnectionFactory.CreateSqlConnection();
    await DbConnectionFactory.OpenAsync(connection);

    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        var idPersona = await connection.QuerySingleAsync<long>(
            "PA_Persona_Insertar",
            new
            {
                request.Nombre,
                request.Apellido,
                request.Documento,
                IdUsuario = idUsuario
            },
            transaction: transaction,
            commandType: CommandType.StoredProcedure
        );

        foreach (var auto in request.Autos)
        {
            await connection.ExecuteAsync(
                "PA_Auto_Insertar",
                new
                {
                    IdPersona = idPersona,
                    auto.Placa,
                    auto.Marca,
                    IdUsuario = idUsuario
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure
            );
        }

        await transaction.CommitAsync();

        return ApiResponse<object>.Ok(
            new { idPersona },
            "Persona y autos registrados correctamente."
        );
    }
    catch (SqlException)
    {
        await transaction.RollbackAsync();
        return ApiResponse<object>.Error("No se pudo guardar la información en base de datos.");
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        return ApiResponse<object>.Error("Ocurrió un error inesperado al registrar la información.");
    }
}
```

En este ejemplo:

```text
Si se inserta la persona pero falla un auto:
se revierte todo.

Si todos los autos se registran correctamente:
se confirma la transacción.
```

La transacción debe usarse cuando varias operaciones forman una sola unidad de trabajo.

### Concurrencia optimista con RowVersion *(nuevo en v6)*

Las reglas de actualización de la v5 usan `WHERE IdPersona = @IdPersona AND Activo = 1`, lo cual evita editar un registro dado de baja, pero no evita que dos usuarios se pisen cambios entre sí.

Ejemplo del problema:

```text
Usuario A abre el registro de Persona.
Usuario B abre el mismo registro de Persona.
Usuario A cambia el celular y guarda.
Usuario B cambia la dirección (sin ver el cambio de A) y guarda.
El cambio de A queda pisado silenciosamente.
```

Regla para tablas donde varios usuarios pueden editar el mismo registro: agregar una columna de control de versión.

```sql
ALTER TABLE dbo.TbPersona
ADD RowVersion ROWVERSION NOT NULL;
```

El procedure de actualización exige que el `RowVersion` recibido coincida con el actual:

```sql
CREATE PROCEDURE dbo.PA_Persona_Actualizar
    @IdPersona BIGINT,
    @Nombre NVARCHAR(150),
    @Apellido NVARCHAR(150),
    @Documento NVARCHAR(50),
    @RowVersion BINARY(8),
    @IdUsuario BIGINT,
    @Motivo NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TbPersona
    SET
        Nombre = @Nombre,
        Apellido = @Apellido,
        Documento = @Documento,
        IdUsuarioUpdate = @IdUsuario,
        FechaUpdate = SYSDATETIME(),
        Motivo = NULLIF(@Motivo, '')
    WHERE IdPersona = @IdPersona
      AND Activo = 1
      AND RowVersion = @RowVersion;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
```

En el repository/service, si `FilasAfectadas = 0` y el registro existe (comprobado por separado o inferido), significa que otro usuario ya lo modificó. La API responde:

```text
409 Conflict
```

con el mensaje estándar definido en la Parte 2 (§12, "Conflicto de concurrencia").

Aplicar esta columna solo en tablas donde la edición concurrente sea un riesgo real (catálogos poco editados no la necesitan obligatoriamente).

---

## 33. Rendimiento

### Paginación

Ver Parte 2, §12 — Contrato estándar de paginación y filtros.

Para catálogos pequeños se puede traer todo, sin paginar.

### Filtros

Ver Parte 2, §12. Los filtros simples pueden enviarse mediante query string (`?buscar=`); para búsquedas complejas se evaluará un endpoint específico con DTO de filtros.

### SELECT *

No se recomienda usar:

```sql
SELECT *
```

Los procedures deben seleccionar solo las columnas necesarias.

### Índices

Se recomienda crear índices en columnas de búsqueda cuando el volumen de datos o la frecuencia de consulta lo justifique.

La definición de índices puede requerir revisión del desarrollador y/o DBA.

---

## 34. System-versioned temporal tables

SQL Server permite manejar tablas versionadas con historial automático mediante `SYSTEM_VERSIONED TEMPORAL TABLES`.

Esta funcionalidad está disponible desde SQL Server 2016.

Una tabla temporal versionada mantiene dos tablas:

```text
Tabla actual      → contiene el estado vigente del registro.
Tabla histórica   → contiene versiones anteriores del registro.
```

Ejemplo conceptual:

```text
TbPersona
TbPersona_History
```

### Para qué sirve

Sirve para:

- auditoría institucional;
- historial de cambios;
- revisión de versiones anteriores;
- trazabilidad de datos críticos;
- saber cómo era un registro antes de una modificación.

No necesariamente debe aplicarse a todas las tablas. Se recomienda para tablas importantes o sensibles.

> **Advertencia reforzada en v6:** no activar system-versioned temporal tables automáticamente en toda tabla nueva "por si acaso". Usar únicamente en tablas críticas (ver lista al final de esta sección) y, antes de activarlo, revisar con el DBA el impacto en almacenamiento, mantenimiento de la tabla histórica y rendimiento de escritura — especialmente en tablas de alto volumen de updates.

### Campos requeridos

Una tabla versionada necesita dos columnas de período:

```sql
ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
ValidTo DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
```

Ejemplo para una tabla nueva:

```sql
CREATE TABLE dbo.TbPersona
(
    IdPersona BIGINT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(150) NOT NULL,
    Documento NVARCHAR(50) NOT NULL,

    Activo BIT NOT NULL DEFAULT 1,
    IdUsuarioCreate BIGINT NULL,
    FechaCreate DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    IdUsuarioUpdate BIGINT NULL,
    FechaUpdate DATETIME2(0) NULL,
    IdUsuarioDelete BIGINT NULL,
    FechaDelete DATETIME2(0) NULL,
    Motivo NVARCHAR(300) NULL,

    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo),

    CONSTRAINT PK_TbPersona PRIMARY KEY CLUSTERED (IdPersona)
)
WITH
(
    SYSTEM_VERSIONING = ON
    (
        HISTORY_TABLE = dbo.TbPersona_History
    )
);
```

### Qué pasa cuando se actualiza un registro

Si se ejecuta un `UPDATE`, SQL Server conserva la versión anterior en la tabla histórica y deja la nueva versión en la tabla actual.

Ejemplo:

```sql
UPDATE dbo.TbPersona
SET Nombre = 'Juan Carlos'
WHERE IdPersona = 1;
```

Resultado conceptual:

```text
TbPersona:
versión actual con Nombre = Juan Carlos

TbPersona_History:
versión anterior con Nombre = Juan
```

### Qué pasa cuando se da de baja lógica

En nuestro estándar no hacemos eliminación física para CRUDs de negocio.

La baja lógica es un `UPDATE`:

```sql
UPDATE dbo.TbPersona
SET Activo = 0,
    FechaDelete = SYSDATETIME()
WHERE IdPersona = 1;
```

Como es un `UPDATE`, SQL Server guarda la versión anterior activa en la tabla histórica y deja en la tabla actual el registro con `Activo = 0`. Lo mismo aplica a la reactivación (§24): al ser también un `UPDATE`, queda registrada en el historial.

### Qué pasa si se elimina físicamente

Aunque no sea la práctica del estándar para CRUDs de negocio, es importante conocerlo.

Si se ejecuta:

```sql
DELETE FROM dbo.TbPersona
WHERE IdPersona = 1;
```

SQL Server elimina el registro de la tabla actual, pero conserva la última versión en la tabla histórica.

Aun así, para nuestros CRUDs institucionales se mantiene la regla:

```text
No usar eliminación física en datos de negocio.
Usar baja lógica con Activo = 0.
```

### Consultar historial

Consultar cómo estaba una tabla en un momento específico:

```sql
SELECT *
FROM dbo.TbPersona
FOR SYSTEM_TIME AS OF '2026-05-01 10:00:00'
WHERE IdPersona = 1;
```

Consultar todas las versiones de un registro:

```sql
SELECT *
FROM dbo.TbPersona
FOR SYSTEM_TIME ALL
WHERE IdPersona = 1
ORDER BY ValidFrom;
```

### Aplicar versionamiento a una tabla existente

Supongamos que ya existe `TbPersona`.

Primero se agregan las columnas de período:

```sql
ALTER TABLE dbo.TbPersona
ADD
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
        NOT NULL DEFAULT SYSUTCDATETIME(),
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
        NOT NULL DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
```

Luego se activa el versionamiento:

```sql
ALTER TABLE dbo.TbPersona
SET
(
    SYSTEM_VERSIONING = ON
    (
        HISTORY_TABLE = dbo.TbPersona_History,
        DATA_CONSISTENCY_CHECK = ON
    )
);
```

Nota: en los ejemplos de campos de auditoría usamos `SYSDATETIME()` por hora local institucional. En las columnas internas de período del versionamiento puede usarse `SYSUTCDATETIME()` porque SQL Server maneja el período temporal del sistema.

### Agregar una columna a una tabla versionada

Para cambios simples, SQL Server permite algunas operaciones con versionamiento activo. Sin embargo, como regla institucional segura, se recomienda hacer cambios estructurales controlados desactivando temporalmente el versionamiento, aplicando el cambio en tabla actual e histórica, y activándolo nuevamente.

Ejemplo: agregar `Celular`.

```sql
ALTER TABLE dbo.TbPersona
SET (SYSTEM_VERSIONING = OFF);

ALTER TABLE dbo.TbPersona
ADD Celular NVARCHAR(30) NULL;

ALTER TABLE dbo.TbPersona_History
ADD Celular NVARCHAR(30) NULL;

ALTER TABLE dbo.TbPersona
SET
(
    SYSTEM_VERSIONING = ON
    (
        HISTORY_TABLE = dbo.TbPersona_History,
        DATA_CONSISTENCY_CHECK = ON
    )
);
```

### Quitar una columna a una tabla versionada

Ejemplo: quitar `Celular`.

```sql
ALTER TABLE dbo.TbPersona
SET (SYSTEM_VERSIONING = OFF);

ALTER TABLE dbo.TbPersona
DROP COLUMN Celular;

ALTER TABLE dbo.TbPersona_History
DROP COLUMN Celular;

ALTER TABLE dbo.TbPersona
SET
(
    SYSTEM_VERSIONING = ON
    (
        HISTORY_TABLE = dbo.TbPersona_History,
        DATA_CONSISTENCY_CHECK = ON
    )
);
```

### Modificar una columna a una tabla versionada

Ejemplo: ampliar el tamaño de `Nombre`.

```sql
ALTER TABLE dbo.TbPersona
SET (SYSTEM_VERSIONING = OFF);

ALTER TABLE dbo.TbPersona
ALTER COLUMN Nombre NVARCHAR(200) NOT NULL;

ALTER TABLE dbo.TbPersona_History
ALTER COLUMN Nombre NVARCHAR(200) NOT NULL;

ALTER TABLE dbo.TbPersona
SET
(
    SYSTEM_VERSIONING = ON
    (
        HISTORY_TABLE = dbo.TbPersona_History,
        DATA_CONSISTENCY_CHECK = ON
    )
);
```

### Desactivar definitivamente el versionamiento

Si se decide dejar de usar historial automático:

```sql
ALTER TABLE dbo.TbPersona
SET (SYSTEM_VERSIONING = OFF);
```

La tabla histórica no se elimina automáticamente. Si se quiere eliminar, debe hacerse de forma controlada y con respaldo previo.

### Desventajas o cuidados

- aumenta el uso de almacenamiento;
- puede impactar en rendimiento si la tabla tiene muchos cambios;
- la tabla histórica debe ser administrada;
- los cambios de estructura requieren más cuidado;
- no reemplaza completamente una auditoría funcional con usuario, acción y motivo;
- puede complicar scripts de mantenimiento;
- no debe aplicarse automáticamente a todas las tablas.

### Recomendación institucional

Usar system-versioned temporal tables solo en tablas importantes donde se requiera trazabilidad fuerte.

Ejemplos:

```text
TbUsuario
TbFuncionario
TbContrato
TbTramite
TbDocumento
TbMovimientoImportante
```

No es necesario aplicarlo a todos los catálogos simples.

---

# PARTE 4 — Seguridad

## 35. Seguridad y credenciales

No se deben guardar credenciales reales en código fuente.

No se deben subir a Git archivos con:

- contraseñas reales;
- claves JWT reales;
- tokens;
- credenciales de dominio;
- credenciales de producción.

Ver §38 (Gestión de secretos por ambiente) para el mecanismo concreto de dónde sí deben vivir estos valores.

Recomendación:

```text
appsettings.json              → puede subir con valores genéricos (placeholders vacíos)
appsettings.Development.json  → no subir si contiene credenciales reales; usar dotnet user-secrets en su lugar
appsettings.Production.json   → no subir si contiene credenciales reales
appsettings.example.json      → sí subir como guía, con valores de ejemplo
```

---

## 36. Conexión a SQL Server

Para producción se recomienda usar usuario de dominio cuando sea posible.

Una buena práctica es configurar el Application Pool de IIS con una cuenta de dominio y usar:

```text
Integrated Security=True
```

Así se evita guardar contraseñas en el proyecto.

También se deben restringir permisos NTFS sobre carpetas publicadas.

---

## 37. Almacenamiento seguro de contraseñas *(nuevo en v6)*

La v5 definía JWT, `Issuer`, `Audience` y expiración, pero no decía cómo debe guardarse la contraseña del usuario. Sin esta regla, un desarrollador podría terminar guardando la contraseña en texto plano o con un hash inseguro (`MD5`, `SHA1`, `SHA256` sin salt).

### Reglas obligatorias

```text
Nunca guardar la contraseña en texto plano.
Nunca usar MD5 para contraseñas.
Nunca usar SHA1 para contraseñas.
Nunca usar SHA256 (u otro hash genérico) sin salt como único mecanismo.
Usar un algoritmo diseñado para contraseñas: BCrypt, Argon2id o PBKDF2.
```

### Por qué importa

Si alguien obtiene una copia de la base de datos (robo, backup filtrado, acceso indebido), no debe poder leer las contraseñas de los usuarios directamente ni revertir el hash con fuerza bruta razonable. Los algoritmos genéricos (MD5, SHA1, SHA256 simple) son rápidos de calcular, lo que los hace vulnerables a ataques de fuerza bruta/diccionario a gran escala. BCrypt/Argon2id/PBKDF2 están diseñados deliberadamente para ser lentos y resistentes a ese tipo de ataque.

### Qué se guarda en la tabla

No se guarda:

```text
Password = "admin123"
```

Se guarda el resultado del hash (incluye el salt embebido, según la librería usada):

```text
PasswordHash = "$2a$12$K9x3F7z...."
```

Columna recomendada:

```sql
PasswordHash NVARCHAR(200) NOT NULL
```

### Ejemplo conceptual en C# (BCrypt)

```csharp
// Al crear el usuario
var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

// Al hacer login
var esValida = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);
```

### Regla de verificación en login

El login nunca debe comparar la contraseña recibida contra la guardada con `==` o con un `SELECT ... WHERE Password = @Password`. La comparación se hace siempre con la función `Verify`/`Check` del algoritmo elegido, nunca con SQL directo sobre la contraseña en texto plano.

### Protección contra fuerza bruta en el login *(nuevo en v8)*

Guardar bien la contraseña protege contra el robo de la base de datos, pero no impide que un atacante pruebe miles de contraseñas directamente contra `POST /api/auth/login`. Todo endpoint de autenticación debe tener al menos una de estas dos protecciones (idealmente ambas):

**1. Bloqueo temporal tras intentos fallidos.** Se registran los intentos fallidos por cuenta y, superado un límite, la cuenta se bloquea temporalmente:

```text
5 intentos fallidos seguidos → bloqueo temporal de 15 minutos.
El contador se reinicia con un login exitoso.
El bloqueo es temporal y automático: no requiere intervención de un administrador.
```

**2. Rate limiting en los endpoints de autenticación.** .NET incluye el mecanismo desde .NET 7, sin librerías externas:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
    });
});
```

```csharp
[EnableRateLimiting("login")]
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request) { ... }
```

Regla adicional: el mensaje de error del login debe ser genérico — `"Usuario o contraseña incorrectos"` — sin revelar si la cuenta existe o no. Un mensaje como `"El usuario no existe"` le confirma al atacante qué cuentas son válidas para concentrar el ataque.

---

## 38. Gestión de secretos por ambiente *(nuevo en v6)*

La v5 decía "no subir secretos a Git", pero no respondía la pregunta obvia: ¿entonces dónde se ponen? Esta sección define el mecanismo concreto según el ambiente.

### Desarrollo local

Usar `dotnet user-secrets` (herramienta incluida en el SDK de .NET), no `appsettings.Development.json` con valores reales.

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Database=...;Integrated Security=True;"
dotnet user-secrets set "Jwt:SecretKey" "clave-real-de-desarrollo"
```

Los secretos quedan almacenados fuera del repositorio (en el perfil del usuario de Windows/Linux), y `IConfiguration` los combina automáticamente con `appsettings.json` en tiempo de ejecución, sin código adicional.

### Producción

Usar variables de entorno del servidor, una cuenta de dominio con `Integrated Security=True` (ver §36), o un vault institucional si existe (Windows Credential Manager, Azure Key Vault u otro mecanismo aprobado). Nunca un archivo `appsettings.Production.json` con la contraseña real dentro del repositorio, aunque el repositorio sea privado.

### Ejemplo

No recomendado (contraseña real versionada):

```json
{
  "ConnectionStrings": {
    "Default": "Server=192.168.1.10;User Id=sa;Password=Produccion2024;"
  }
}
```

Recomendado (placeholder en el repo; el valor real se inyecta fuera del código):

```json
{
  "ConnectionStrings": {
    "Default": ""
  }
}
```

### Regla resumida

```text
Desarrollo local  → dotnet user-secrets
Producción        → variables de entorno / cuenta de dominio con Integrated Security / vault institucional
Nunca             → contraseña real dentro de un appsettings.*.json versionado en Git
```

---

## 39. Refresh token y revocación de sesión *(nuevo en v6)*

La v5 mencionaba el refresh token solo como concepto ("access token dura poco, refresh token dura más"), sin definir dónde vive, cómo se invalida, ni qué pasa al cerrar sesión.

### Tabla de control

```sql
CREATE TABLE dbo.TbRefreshToken
(
    IdRefreshToken BIGINT IDENTITY(1,1) NOT NULL,
    IdUsuario BIGINT NOT NULL,
    TokenHash NVARCHAR(200) NOT NULL,
    FechaCreate DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    FechaExpiracion DATETIME2(0) NOT NULL,
    FechaRevocacion DATETIME2(0) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    IpCreate NVARCHAR(50) NULL,
    UserAgent NVARCHAR(300) NULL,

    CONSTRAINT PK_TbRefreshToken PRIMARY KEY CLUSTERED (IdRefreshToken)
);
```

*(actualizado en v7: `IdRefreshToken` pasa a `BIGINT IDENTITY` — sigue la regla por defecto de [§18](#18-llaves-primarias). No aplica la excepción de `UNIQUEIDENTIFIER` porque este identificador nunca se expone al cliente ni se usa como secreto: el valor sensible es el refresh token en sí, que ni siquiera se guarda aquí — solo su hash en `TokenHash`.)*

El refresh token en sí **no se guarda en texto plano** — se guarda su hash (`TokenHash`, con SHA-256 basta para este caso porque no es una contraseña de usuario final sino un token generado aleatoriamente de alta entropía), igual que se evita guardar la contraseña en texto plano.

### Reglas

```text
Al hacer login, se genera un access token (corto) + un refresh token (largo), y el refresh token se registra en TbRefreshToken.
Al hacer logout, se marca el refresh token activo como Activo = 0 y FechaRevocacion = SYSDATETIME(). El access token en curso seguirá siendo técnicamente válido hasta que expire (es corto por diseño), pero no podrá renovarse.
Cada vez que se usa un refresh token para pedir un access token nuevo, se rota: se invalida el refresh token usado (Activo = 0) y se emite uno nuevo. Esto limita el daño si un refresh token es interceptado.
Si se intenta usar un refresh token ya revocado o expirado, se rechaza con 401 y se fuerza un nuevo login.
```

### Ejemplo de tiempos

```text
Access token: 30 minutos
Refresh token: 8 horas, con rotación en cada uso
```

### Endpoint de logout

```text
POST /api/auth/logout
```

Revoca el/los refresh token(s) activos del usuario autenticado. Debe estar protegido con `[Authorize]`.

---

## 40. Autorización por recurso *(nuevo en v8)*

El §18 establece la regla: todo endpoint debe validar que el usuario autenticado tenga permiso sobre el recurso solicitado, independientemente del tipo de identificador. Esta sección muestra **cómo** implementarla.

**Carácter de esta sección: recomendación, no obligación.** Implementar la validación de ownership en cada servicio tiene un costo real de trabajo, y una regla costosa impuesta como obligatoria tiende a no cumplirse. Por eso este patrón se presenta como implementación de referencia: se recomienda aplicarlo **priorizando los recursos sensibles** (datos personales, trámites, documentos de un usuario), no necesariamente en cada endpoint de cada catálogo. La autorización por **roles y permisos** (por ejemplo, un funcionario autorizado a ver trámites de otros usuarios) queda fuera del alcance de esta versión y se definirá en una versión futura.

Este patrón cubre el caso de **ownership**: recursos que pertenecen a un usuario y solo su dueño debe poder consultarlos o modificarlos. Sin esta validación, cualquier usuario autenticado puede acceder a datos ajenos simplemente cambiando el ID de la petición (`/api/tramite/145` → `/api/tramite/146`) — es la vulnerabilidad conocida como IDOR (*Insecure Direct Object Reference*), una de las más comunes en APIs.

### 40.1 El `IdUsuario` sale del token, nunca del frontend

El `IdUsuario` con el que se valida la autorización se obtiene **siempre de los claims del JWT**, que está firmado por el servidor y no puede ser falsificado por el cliente.

Al generar el token en el login:

```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
    new Claim(ClaimTypes.Name, usuario.NombreUsuario)
};
```

Al leerlo en el controller (o en un servicio auxiliar reutilizable):

```csharp
var idUsuario = long.Parse(
    User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

Regla dura:

```text
El frontend nunca envía "su propio" IdUsuario como dato confiable.
Si un request lo incluye en el body o la URL, el backend lo ignora
y usa el IdUsuario de los claims del token.
```

Un atacante puede escribir lo que quiera en el body de la petición; lo que no puede es falsificar el claim de un JWT firmado.

### 40.2 La validación vive en el Service

- El **Controller** solo transporta: extrae el `idUsuario` de los claims y lo pasa al Service.
- El **Service** valida la propiedad del recurso antes de operar — es donde viven las reglas de negocio, y la autorización por recurso es una de ellas.
- El **Repository** no decide autorización: solo accede a datos.

```csharp
public async Task<TramiteResponse> ObtenerAsync(long idTramite, long idUsuario)
{
    var tramite = await _tramiteRepository.ObtenerAsync(idTramite);

    if (tramite is null)
        return null; // el controller responde el "no encontrado" estándar

    if (tramite.IdUsuarioPropietario != idUsuario)
        throw new AccesoDenegadoException("No tiene permiso sobre este recurso.");

    return tramite;
}
```

La excepción de acceso denegado se traduce en el middleware de errores al `403 Forbidden` estándar (ver §12, tabla de códigos HTTP), con el formato `ApiResponse` de siempre.

```text
Si se adopta el patrón para un recurso, aplicarlo en TODAS las operaciones
sobre ese recurso (obtener, actualizar, dar de baja, reactivar),
no solo en la consulta — proteger solo la lectura deja abierta la escritura.
```

### 40.3 Defensa en profundidad: filtrar por `@IdUsuario` en los listados

Para los listados, además de la validación en el Service, el procedure recibe `@IdUsuario` y filtra desde el origen, de modo que el usuario solo pueda recibir lo suyo aunque una capa superior falle:

```sql
CREATE PROCEDURE dbo.PA_Tramite_ListarPorUsuario
    @IdUsuario BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IdTramite,
        Descripcion,
        EstadoTramite,
        FechaCreate
    FROM dbo.TbTramite
    WHERE IdUsuarioPropietario = @IdUsuario
      AND Activo = 1;
END;
```

### 40.4 Resumen del patrón

```text
1. El IdUsuario confiable sale de los claims del JWT, nunca del body ni de la URL.
2. El Service valida el ownership del recurso antes de cualquier operación.
3. Recurso inexistente → "no encontrado" estándar; recurso ajeno → 403 Forbidden.
4. Los listados filtran por @IdUsuario también en el procedure (defensa en profundidad).
5. La autorización por roles/permisos queda para una versión futura.
```

---

# PARTE 5 — Sistemas heredados y Windows Forms

## 41. Windows Forms

El manual incluye reglas básicas para Windows Forms porque, aunque el foco institucional está en APIs modernas, todavía existen y seguirán existiendo sistemas Windows Forms en producción. Esta parte se mantiene como sección propia del manual, no como apéndice secundario.

### Nombres de formularios

Se recomienda usar prefijo `Frm`.

Ejemplos:

```text
FrmUsuario
FrmCiudad
FrmPersona
```

### Separación de responsabilidades

Estructura recomendada:

```text
Formulario → Capa de negocio → Capa de datos → Procedure
```

Ejemplo:

```text
FrmCiudad
   ↓
NCiudad
   ↓
DCiudad
   ↓
PA_Ciudad_Insertar
```

No se recomienda:

```text
Formulario → SQL directo
```

Las excepciones controladas de JSON como único parámetro (Parte 3, §27) y de archivos en base64 (Parte 2, §14) aplican principalmente a la integración entre sistemas nuevos y este tipo de sistemas heredados.

---

# PARTE 6 — Operación y despliegue

## 42. Git y control de versiones

El manual incluirá reglas básicas de Git:

- no subir credenciales;
- no subir archivos generados innecesarios;
- usar commits claros;
- mantener control de versiones para proyectos nuevos;
- separar configuración sensible de código fuente (ver Parte 4, §38).

---

## 43. Publicación y ambientes

Se recomienda manejar ambientes separados:

```text
Desarrollo
Pruebas
Producción
```

Cada ambiente debe tener su propia configuración.

Ejemplo:

```text
appsettings.json
appsettings.Development.json
appsettings.Production.json
```

Los archivos con credenciales reales no deben subirse a Git (ver Parte 4, §38).

### Publicación en IIS para desarrolladores

Para publicar una API ASP.NET Core, el desarrollador no debe copiar el proyecto completo al servidor.

Debe publicar el proyecto en modo `Release` y copiar únicamente la carpeta generada por la publicación.

Flujo recomendado:

```text
1. Publicar el proyecto en modo Release.
2. Copiar la carpeta publish, no el proyecto completo.
```

Ejemplo por comando:

```bash
dotnet publish -c Release -o ./publish
```

La carpeta `publish` contiene los archivos necesarios para desplegar la API.

No se debe copiar al servidor:

```text
Controllers
Services
Repositories
Program.cs
.csproj
bin
obj
```

Sí se debe copiar el contenido generado en:

```text
publish
```

Aunque Visual Studio permite usar la ruta por defecto:

```text
bin\Release\net9.0\publish\
```

se recomienda usar una carpeta específica de publicación para mantener mayor orden, por ejemplo:

```text
D:\Publicaciones\NombreApi
```

La ruta de publicación no debe contener nombres personales del desarrollador ni depender de una carpeta privada del equipo.

Regla:

```text
El servidor recibe la aplicación publicada.
No recibe el código fuente completo del proyecto.
```

### Nota sobre CI/CD *(nuevo en v6)*

El flujo de publicación descrito arriba es manual (`dotnet publish` + copiar carpeta) y sigue siendo válido como estándar actual. A futuro se evaluará automatizarlo con un pipeline de CI/CD (compilación y pruebas automáticas al subir cambios, publicación controlada, posibilidad de reversión). Esto queda como **fase futura**, no como requisito obligatorio de esta versión del manual.

---

## 44. Health check *(nuevo en v6)*

Toda API nueva debe exponer un endpoint de salud para monitoreo externo (balanceadores, IIS, herramientas de monitoreo como Zabbix o Uptime Kuma).

```text
GET /health
```

Respuesta mínima:

```json
{
  "status": "Healthy"
}
```

Ejemplo usando el paquete estándar de ASP.NET Core (`Microsoft.Extensions.Diagnostics.HealthChecks`):

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Default")!, name: "sql-server");

var app = builder.Build();

app.MapHealthChecks("/health");
```

Este endpoint no requiere `[Authorize]` (debe poder consultarlo un balanceador o monitor externo sin autenticarse), pero tampoco debe exponer detalles internos sensibles en la respuesta (nombres de servidores, cadenas de conexión, versiones exactas de librerías).

---

## 45. Logging estructurado y TraceId *(nuevo en v6)*

Un log como `"Error al guardar persona"` no permite reconstruir qué pasó. El estándar institucional exige logging estructurado con un identificador de correlación (`TraceId`) por petición.

### Qué significa "estructurado"

En vez de una línea de texto libre, el log se registra como un objeto con campos consultables:

```json
{
  "traceId": "0HN28B3F4G5H6:00000001",
  "usuario": "filemon",
  "endpoint": "/api/persona",
  "metodo": "POST",
  "error": "Timeout SQL",
  "fecha": "2026-07-02T10:15:32"
}
```

### Por qué importa

Cuando un usuario reporta "me salió un error", se le pide (o se captura automáticamente) el `TraceId` mostrado en la respuesta de error, y con eso se busca exactamente esa petición en los logs, sin tener que adivinar cuál de miles de líneas corresponde.

### Recomendación de implementación

- Usar una librería de logging estructurado (por ejemplo, Serilog) en vez de `Console.WriteLine` o archivos de texto planos.
- ASP.NET Core ya genera un `TraceIdentifier` por request (`HttpContext.TraceIdentifier`); incluirlo en cada log y, opcionalmente, devolverlo en las respuestas de error (`ApiResponse<T>.Error` puede incluir el `TraceId` en un campo adicional para que el usuario lo reporte a soporte).
- No registrar en logs contraseñas, tokens completos, ni el contenido completo de archivos base64 (ver Parte 2, §14).

---

# PARTE 7 — Checklist y conclusiones

## 46. Checklist técnico

### Checklist para APIs

- Usa estructura Controller → Service → Repository.
- Usa Dapper.
- Usa procedures.
- Usa Swagger.
- Los endpoints tienen comentarios `summary` y ejemplos básicos de consumo cuando corresponde.
- Usa JWT en endpoints protegidos.
- Usa `[Authorize]` donde corresponde.
- Usa ApiResponse<T>.
- Mantiene la misma estructura de respuesta en éxito, validación, no encontrado, conflicto y error.
- Las respuestas normales no devuelven campos internos de control como `Activo`, `Motivo`, `FechaCreate`, `FechaUpdate` o `FechaDelete`.
- Si se requieren datos de auditoría, se usan endpoints y contratos separados.
- Los listados paginados siguen el contrato estándar (`pagina`, `cantidad`, `buscar`, bloque `paginacion`).
- Usa códigos HTTP estándar, incluyendo `409 Conflict` para concurrencia.
- Usa controller en singular.
- Usa rutas en singular.
- Sigue el estilo REST pragmático (§12); las operaciones que no son CRUD usan el formato de acción `POST /api/entidad/{id}/accion`; no mezcla estilos REST y RPC en una misma API. *(nuevo en v8)*
- En actualizaciones, bajas y reactivaciones verifica `FilasAfectadas` y convierte `0` en error controlado, nunca en éxito. *(nuevo en v8)*
- Usa métodos en español.
- Configura CORS leyendo dominios permitidos desde configuración por ambiente, no hardcodeados en `Program.cs`.
- Los servicios externos tienen timeout y política de reintentos definida.
- Expone `/health`.
- Registra logs estructurados con `TraceId`.
- Tiene al menos unit tests de Service e integración de Repository para la lógica crítica.
- No usa credenciales reales en Git; usa `dotnet user-secrets` en desarrollo.

### Checklist para SQL Server

- Las llaves primarias nuevas usan formato `IdEntidad BIGINT IDENTITY` (por defecto).
- Si se agrega `UNIQUEIDENTIFIER`, es solo por necesidad justificada (integración externa, sync distribuido, exposición no correlativa), como columna adicional `Uid{Entidad}` con `NEWSEQUENTIALID()` — no como reemplazo de la PK.
- Tablas existentes con `UNIQUEIDENTIFIER` como PK no se migran automáticamente.
- Tabla con llave primaria.
- Tabla con `Activo`.
- Tabla con auditoría (`IdUsuario*`/`Fecha*` + `Motivo`).
- INSERT, UPDATE, DELETE lógico y REACTIVAR aplican correctamente campos de control.
- `Motivo` permite NULL y queda NULL cuando no se envía motivo — sin textos de relleno como `'Modificado'` o `'Dado de baja'`. *(actualizado en v8)*
- Cada endpoint valida autorización sobre el recurso solicitado, sin depender del tipo de identificador.
- Procedures con parámetros explícitos.
- No se usa JSON como único parámetro (salvo compatibilidad documentada con sistemas antiguos).
- Los procedures compartidos entre sistemas respetan el contrato de versionado (no quitar/reordenar parámetros).
- Insert devuelve el ID creado con `CAST(SCOPE_IDENTITY() AS BIGINT)`. *(actualizado en v8)*
- Todo procedure de UPDATE, baja lógica y reactivación termina con `SELECT @@ROWCOUNT AS FilasAfectadas`. *(nuevo en v8)*
- Listados filtran `Activo = 1`.
- Existe procedure de reactivación donde el negocio lo requiera.
- Tablas con edición concurrente real tienen `RowVersion` y el procedure de actualización lo valida.
- Los SELECT normales no devuelven campos internos de control salvo endpoints de auditoría.
- No se usa `SELECT *`.
- Si se usan tablas versionadas, se valida el manejo de tabla histórica, cambios de estructura y se limita a tablas críticas.

### Checklist para C#

- No hay `catch` vacíos.
- Se manejan errores con mensajes amigables.
- Ningún `catch` devuelve el mensaje amigable sin registrar antes la excepción completa en el log (§9, §45). *(nuevo en v8)*
- Se usa inyección de dependencias en APIs nuevas.
- No se usa `new` directo para services/repositories en APIs nuevas.
- Clases y métodos tienen nombres claros, en español donde el manual lo exige.
- Los services validan antes de llamar al repository.
- Los repositories no contienen reglas de negocio.
- Existen pruebas mínimas (unit para Service, integración para Repository) en los flujos críticos.

### Checklist para seguridad

- No hay contraseñas en código fuente.
- No hay secretos en Git; se usa `dotnet user-secrets` en desarrollo y variables de entorno/vault en producción.
- Las contraseñas de usuario se guardan con BCrypt/Argon2id/PBKDF2, nunca en texto plano ni con hash genérico sin salt.
- El refresh token se guarda hasheado, se revoca en logout y rota en cada uso.
- El login tiene protección contra fuerza bruta (bloqueo temporal tras intentos fallidos y/o rate limiting) y su mensaje de error no revela si la cuenta existe. *(nuevo en v8)*
- Producción usa configuración segura.
- CORS no está abierto para todos y se configura por ambiente.
- JWT protege endpoints internos.
- (Recomendado, no obligatorio) Para recursos sensibles con dueño, se aplica el patrón de autorización por recurso del §40: `IdUsuario` desde los claims del JWT y ownership validado en el Service. *(nuevo en v8)*

### Checklist para Windows Forms

- Formularios con prefijo `Frm`.
- El formulario no llama directamente a SQL Server.
- Existe separación básica entre UI, negocio y datos.
- Los errores se manejan correctamente.

### Checklist para servicios externos REST/SOAP

- El controller no consume directamente el servicio externo.
- Existe una clase cliente en `Infrastructure/ExternalServices`.
- URLs y parámetros están en configuración.
- Credenciales reales no están en Git.
- El cliente define timeout y política de reintentos.
- El error externo se transforma a mensaje amigable.
- La API interna mantiene `ApiResponse<T>`.

### Checklist para archivos

- Se usa `multipart/form-data`.
- No se envían archivos como base64 salvo compatibilidad con sistemas antiguos.
- El backend valida tamaño máximo (50 MB institucional, o menor por endpoint). *(nuevo en v8)*
- Solo se aceptan tipos por lista blanca (PDF, JPG, PNG, DOC, DOCX); nunca ejecutables ni scripts. *(nuevo en v8)*
- El archivo físico se guarda como GUID + extensión; el nombre original solo como metadata. *(nuevo en v8)*
- El almacenamiento local es temporal; el destino definitivo es una API institucional de archivos o un almacenamiento de objetos (ej. MinIO). *(nuevo en v8)*
- Se guarda metadata del archivo.

---

## 47. Conclusiones

Este manual establece un estándar institucional para proyectos C# con SQL Server.

Las decisiones principales son:

```text
1.  Obligatorio para proyectos nuevos.
2.  Recomendado para proyectos antiguos.
3.  APIs nuevas con Dapper + procedures.
4.  WinForms antiguos pueden usar ADO.NET.
5.  Entity Framework no será estándar.
6.  Procedures normales, sin JSON como único parámetro (salvo compatibilidad documentada).
7.  Insert devuelve ID creado.
8.  Transacciones manejadas desde C# cuando involucran varias operaciones.
9.  No se permiten catch vacíos.
10. Credenciales reales prohibidas en código fuente y Git; uso obligatorio de dotnet user-secrets en desarrollo y vault/variables de entorno en producción.
11. Producción preferentemente con Application Pool + usuario de dominio.
12. Fechas locales con SYSDATETIME() para DATETIME2.
13. Duplicados críticos se evalúan según el caso y devuelven mensaje amigable.
14. Inyección de dependencias por constructor.
15. JWT + [Authorize] para APIs protegidas, con refresh token revocable y rotativo.
16. CORS cerrado a URLs permitidas en producción, configuradas por ambiente (no hardcodeadas).
17. ApiResponse con success, message y data.
18. ApiResponse mantiene el mismo formato en éxito, validación, error, no encontrado y conflicto (409).
19. Controllers y rutas en singular.
20. Métodos C# en español.
21. Versionamiento /api/v1 recomendado, no obligatorio.
22. WinForms con separación Formulario → Negocio → Datos → Procedure, documentado en parte propia del manual.
23. Worker Services mencionados como recomendación.
24. Paginación backend con contrato estándar (pagina/cantidad/buscar + bloque paginacion).
25. Filtros simples por query string; filtros complejos en endpoint dedicado.
26. No usar SELECT *.
27. Las respuestas normales de API no deben exponer campos internos de control.
28. Los campos de control solo se devuelven en endpoints especiales de auditoría o administración.
29. CRUD de negocio usa baja lógica, con procedure de reactivación disponible.
30. Eliminación física solo para temporales, staging o casos controlados.
31. Carga de archivos mediante multipart/form-data.
32. No enviar archivos como base64 salvo compatibilidad con sistemas antiguos.
33. A futuro se evaluará MinIO para archivos.
34. Servicios externos REST/SOAP deben consumirse desde clases dedicadas, con timeout y reintentos definidos.
35. La API interna no debe devolver errores crudos de servicios externos.
36. Procedures reciben `@IdUsuario` y asignan internamente a create, update, delete o reactivar.
37. UPDATE y DELETE lógico pueden recibir `@Motivo` para registrar la justificación de la operación en la columna `Motivo`.
38. SQL Server protege integridad con PK, FK, CHECK e índices únicos.
39. SOAP no usará Connected Services como estándar; se usará cliente dedicado.
40. System-versioned temporal tables se usarán solo en tablas importantes, con revisión de impacto en almacenamiento/rendimiento.
41. Llaves primarias nuevas usan `IdEntidad BIGINT IDENTITY` por defecto; `UNIQUEIDENTIFIER` solo se agrega como columna `Uid{Entidad}` adicional cuando hay necesidad justificada, y tablas existentes con GUID como PK no se migran automáticamente.
42. Contraseñas de usuario se almacenan con BCrypt/Argon2id/PBKDF2, nunca en texto plano.
43. Tablas con edición concurrente relevante usan RowVersion y responden 409 Conflict ante conflicto.
44. Activo controla vigencia en el CRUD; un Estado terminal no implica Activo = 0 por sí solo.
45. Procedures compartidos entre sistemas respetan un contrato de versionado (no quitar/reordenar parámetros existentes).
46. Toda API expone /health y registra logs estructurados con TraceId.
47. Se exige un mínimo de testing: unit tests en Services, integración en Repositories.
48. CI/CD queda reconocido como fase futura, no obligatoria en esta versión.
49. Incluir checklist técnico ampliado.
50. Se elimina `Accion` de los campos de control (es derivable de las columnas `Fecha*`/`IdUsuario*`); `Detail` se renombra a `Motivo`.
51. La seguridad de un endpoint depende de validar autorización sobre el recurso solicitado, no del tipo de identificador (correlativo o GUID) usado en la PK.
52. Todo procedure de UPDATE, baja lógica y reactivación devuelve SELECT @@ROWCOUNT AS FilasAfectadas, y la capa C# convierte 0 filas en un error controlado — nunca en un éxito falso.
53. Las inserciones devuelven el ID nuevo con CAST(SCOPE_IDENTITY() AS BIGINT), porque SCOPE_IDENTITY() devuelve NUMERIC(38,0).
54. Motivo queda NULL cuando no se envía motivo; no se usan textos de relleno. Si un flujo exige motivo obligatorio, se valida en el Service.
55. El estilo institucional de rutas es REST pragmático, con desviaciones declaradas (200 OK en inserciones, singular). Las operaciones que no son CRUD usan el formato de acción POST /api/entidad/{id}/accion.
56. Los sistemas heredados con rutas estilo RPC no se migran; las APIs nuevas siguen el formato REST pragmático y no se mezclan estilos dentro de una misma API.
57. La autorización por recurso (§40) es una recomendación, no una obligación: IdUsuario desde los claims del JWT, ownership validado en el Service y @IdUsuario en los listados, priorizando recursos sensibles. Roles y permisos quedan para una versión futura.
58. Los sufijos Create/Update/Delete de los campos de control son una excepción heredada a la regla de no mezclar idiomas; no habilitan mezclas en campos nuevos.
59. Los campos de control guardan solo el último cambio; el historial completo, cuando el negocio lo exige, requiere temporal tables (§34). La hora local con SYSDATETIME() es decisión consciente, con la deuda frente a UTC declarada (§21).
60. El endpoint de login se protege contra fuerza bruta: bloqueo temporal tras intentos fallidos y/o rate limiting, con mensaje de error genérico que no revela si la cuenta existe.
61. Todo catch registra la excepción completa en el log (con TraceId) antes de devolver el mensaje amigable al usuario; un error descartado sin loguear es evidencia perdida.
62. Los archivos subidos se validan en el backend: máximo 50 MB, lista blanca de tipos (PDF, imágenes, Word — nunca ejecutables), nombre físico GUID + extensión con el nombre original solo como metadata, almacenamiento local temporal y destino definitivo en un servicio institucional de archivos (API dedicada o MinIO).
```

El objetivo final es mejorar la calidad, seguridad, trazabilidad y mantenibilidad de los sistemas desarrollados en C# con SQL Server.
