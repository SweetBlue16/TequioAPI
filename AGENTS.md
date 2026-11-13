# AGENTS.md - BACKEND (C# & SQL Server)
# Proyecto: Tequio

## 1. ARQUITECTURA Y CAPAS (REGLA CRÍTICA)
- **Separación Total:** Mantén estricta separación de capas.
- **Controladores:** La API REST en ASP.NET Core debe usar estrictamente los métodos HTTP correctos (GET, POST, PUT, PATCH). 
- **PROHIBICIÓN ABSOLUTA:** Queda estrictamente prohibido colocar código de acceso a base de datos (consultas SQL crudas, Entity Framework expuesto, etc.) dentro de los controladores.
- **Lógica de Datos:** Toda lógica de datos, control de inventarios y validaciones de concurrencia debe residir única y exclusivamente en Procedimientos Almacenados, Vistas y Funciones dentro de SQL Server.

## 2. BILINGÜISMO ESTRATÉGICO
- **Código Fuente (Inglés):** Nombres de clases, métodos, variables, interfaces, endpoints, y pruebas unitarias deben redactarse 100% en inglés.
- **Respuestas (Español):** Todos los mensajes devueltos por la API REST (éxito, error, validaciones) deben redactarse en español claro, cotidiano y sin tecnicismos.

## 3. ESTÁNDARES DE C# Y MIDDLEWARE
- **Formato de Llaves:** Uso estricto del formato Allman (cada llave de apertura y cierre va en su propia línea).
- **Manejo de Excepciones:** PROHIBIDO el uso de bloques genéricos `catch (Exception ex)`. Captura siempre excepciones específicas, regístralas en logs y maneja las respuestas de forma centralizada.
- **Nomenclatura:** `PascalCase` para clases/métodos, `camelCase` para variables, `_camelCase` para campos privados, e `I` mayúscula como prefijo obligatorio para interfaces.
- **Inyección de Dependencias:** Utiliza inyección de dependencias nativa de .NET para acoplar servicios y repositorios.
- **Formato Adicional:** Indentación de 4 espacios. Límite de 120 caracteres por línea. Documentación con comentarios XML (`///`).

## 4. ESTÁNDARES DE SQL SERVER (BASE DE DATOS)
- **Nomenclatura DDL:** Tablas y columnas en `PascalCase` y estrictamente en SINGULAR.
- **Procedimientos Almacenados:** Deben usar el prefijo `USP_VerboPascalCase`. Vistas usan `VW_PascalCase`.
- **Sintaxis:** Palabras reservadas (SELECT, FROM, etc.) estrictamente en MAYÚSCULAS. Variables locales en `@camelCase`.

## 5. PRUEBAS UNITARIAS
- **Framework:** Uso obligatorio de xUnit.
- **Nomenclatura:** Las clases de prueba deben tener el sufijo `Test`. Los métodos deben nombrarse con el formato: `Test[Action]With[State]Should[ExpectedResult]`.

## 6. CONTROL DE VERSIONES (GIT)
- Todos los commits generados desde la terminal deben seguir Conventional Commits.
- Prefijos obligatorios: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `style:`, `perf:`, `chore:`, `ci:`.
- La descripción corta (max 50 caracteres) debe estar en inglés y modo imperativo (ej. "feat(maps): add Mapbox integration").