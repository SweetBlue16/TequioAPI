# EstandaresCodificacion.md

## Regla Universal de Idioma (Bilingüismo Estratégico)
*   **Código Fuente (Inglés):** Toda la estructura lógica (nombres de clases, métodos, variables, interfaces, endpoints y pruebas unitarias) debe redactarse estrictamente en **inglés** para cumplir con las buenas prácticas de la industria.
*   **Capa de Presentación y Respuestas API (Español):** Todo el texto visible para el usuario (botones, etiquetas, modales, alertas de UI) y los mensajes de respuesta o error devueltos por la API REST deben redactarse en **español** claro, cotidiano y libre de tecnicismos, adaptado a los productores locales.

## 1. C# (Middleware - API REST)
*   **Nomenclatura:** Clases y métodos en `PascalCase`. Interfaces obligatoriamente con prefijo "I" mayúscula. Variables y parámetros en `camelCase`. Campos privados con prefijo guion bajo (`_camelCase`). Constantes en `PascalCase`.
*   **Estructura:** Límite de 120 caracteres por línea. Indentación de 4 espacios (cero tabuladores). Formato de llaves Allman estricto (cada llave en su propia línea). Solo se omiten llaves en sentencias `if` de una sola instrucción.
*   **Documentación:** Uso exclusivo de comentarios XML de triple barra (`///`) para métodos públicos y clases.
*   **Manejo de Excepciones:** Prohibido únicamente el uso de bloques genéricos `catch (Exception ex)`. Se deben capturar excepciones específicas registrando en logs.
*   **Pruebas:** Uso de xUnit. Clases con sufijo `Test`. Métodos nombrados como `Test[Acción]With[Estado]Should[Resultado expected]`.

## 2. TypeScript (Frontend - React)
*   **Nomenclatura:** Componentes, tipos e interfaces en `PascalCase`. **Prohibido** usar prefijo "I" para interfaces. Funciones y hooks en `camelCase`. Variables del DOM deben ser explícitas (ej. `submitButton`, prohibido usar `btn`). Constantes globales en `UPPER_SNAKE_CASE`.
*   **Estructura:** Formato de llaves 1TBS (llave de apertura en la misma línea). Indentación de 2 espacios. Preferencia por funciones flecha.
*   **Documentación:** Estándar JSDoc (`/**...*/`) para componentes y props.
*   **Manejo de Excepciones:** Llamadas a la API obligatoriamente en `try-catch` con `async/await`. Prohibido exponer errores nativos en UI; usar componentes tipo Toast.
*   **Pruebas:** Uso de Jest + React Testing Library. Bloques `describe` e `it` redactados como oraciones en inglés.

## 3. SQL Server (Base de Datos)
*   **Nomenclatura DDL:** Tablas y columnas en `PascalCase` y estrictamente en **singular**.
*   **Transact-SQL:** Parámetros y variables locales en `@camelCase`.
*   **Vistas y Procedimientos:** Vistas con prefijo `VW_PascalCase`. Procedimientos almacenados con prefijo `USP_VerboPascalCase`.
*   **Estilo:** Palabras reservadas (SELECT, FROM, etc.) estrictamente en MAYÚSCULAS. Indentación de 4 espacios. Cláusulas principales alineadas en nuevas líneas.

## 4. UI/UX y Accesibilidad Frontend
*   **Colores y Alto Contraste:** Fondo de aplicación `#FFFDF9`, Contenedores `#FFFFFF`, Texto Principal `#18110A`, Botones primarios `#D9481E`, Confirmaciones `#1E6F43`, Alertas `#FFB703`.
*   **Tipografía:** *Plus Jakarta Sans*. Títulos en Bold (700/800), cuerpo en Regular (400/500) con tamaño mínimo de 16px.
*   **Interacción (Accesibilidad):** Botones e interruptores de gran tamaño. Obligatorio solicitar confirmación explícita (Modal) antes de acciones definitivas. Lenguaje libre de tecnicismos.