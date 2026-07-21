# ADR-001 · Frontend: Blazor Server

## Estado
Aceptado

## Contexto
Sistema on-premise, uso interno, 10-30 usuarios en LAN institucional.
Un solo desarrollador (full stack senior en .NET) potenciado con Claude Code.

## Decisión
Usar **Blazor Server** como frontend, integrado en el mismo proyecto ASP.NET Core.

## Alternativas consideradas

| Opción | Ventajas | Desventajas |
|---|---|---|
| **Blazor Server** (elegida) | Un solo lenguaje/repo, sin duplicar validaciones, sin CORS, secretos nunca llegan al cliente, tiempos de desarrollo menores | Requiere conexión SignalR persistente (no problemático en LAN); no funciona offline |
| SPA (React/Vue) + API separada | Máxima flexibilidad de UI, reusable para mobile/público a futuro | Duplica lógica de validación, más piezas para mantener en solitario, CORS, dos ciclos de release |
| Blazor WebAssembly | Corre en el cliente, menos carga en servidor | Requiere exponer una API igual, mayor complejidad de auth (tokens), no aporta valor en LAN interna |

## Consecuencias
- Si en el futuro se necesita una app pública o mobile nativa, se deberá
  extraer una API REST desde `Application` (ya desacoplada por Clean
  Architecture) y construir un nuevo frontend — el dominio no se ve afectado.
- Mientras el uso sea interno y en LAN, esta decisión minimiza el esfuerzo
  de mantenimiento para un equipo de desarrollo unipersonal.
