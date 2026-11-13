# RubricaEvaluacion.md

Este documento detalla las directrices inamovibles que el código debe cumplir para satisfacer los criterios técnicos de evaluación del proyecto.

## 1. Arquitectura de Software
*   **Separación en Frontend:** El proyecto debe presentar una separación total de capas. El código frontend (React/TS) no debe contener lógica de negocio mezclada, debe usar marcado HTML semántico accesible y garantizar un diseño responsivo puro.
*   **Separación en Middleware:** La API REST en ASP.NET Core debe hacer uso estricto y correcto de los métodos HTTP (GET, POST, PUT, PATCH). Debe implementar seguridad robusta, validación de entradas y protección mediante JWT.
*   **Separación en Backend:** Queda estrictamente prohibido colocar código de acceso a base de datos (consultas SQL en texto crudo) en los controladores. La lógica de control de inventarios debe manejarse mediante Vistas, Funciones y Procedimientos Almacenados en SQL Server.

## 2. Calidad de Código y Despliegue
*   **Buenas Prácticas:** Nombrado impecable de variables, métodos y clases siguiendo estrictamente los `EstandaresCodificacion.md`. No debe existir código lógico o CSS redundante.
*   **Uso de Servidores:** La arquitectura debe diseñarse para correr de forma distribuida en al menos 3 nodos lógicos independientes (Frontend, Backend API, Base de Datos).
*   **Seguridad:** Implementación de cifrado de datos críticos y validación exhaustiva de todas las entradas del usuario.

## 3. Entregables Críticos
*   **Completitud Funcional:** El sistema debe abarcar el 100% de los Casos de Uso y endpoints definidos.
*   **Pruebas Unitarias:** Obligatorio generar un marco de pruebas unitarias robusto (xUnit para C# y Jest para React) que avale el buen funcionamiento de los métodos core (cálculo de totales, generación de tokens, control de concurrencia).
*   **Accesibilidad:** Obligatorio implementar y documentar en el código al menos 2 pautas de accesibilidad frontend comprobables (ej. uso correcto de atributos ARIA, navegación total por teclado/Tab, y validación de contraste de colores de fondo vs. texto).