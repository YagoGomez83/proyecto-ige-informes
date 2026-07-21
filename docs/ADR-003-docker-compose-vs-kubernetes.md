# ADR-003 · Infraestructura: Docker Compose (no Kubernetes)

## Estado
Aceptado

## Contexto
Despliegue on-premise en un único servidor institucional, 10-30 usuarios.

## Decisión
Usar **Docker Compose** para orquestar los contenedores (app, PostgreSQL,
MinIO), no Kubernetes.

## Justificación
Kubernetes agrega complejidad operativa real (control plane, etcd, ingress
controller, gestión de nodos) que solo se justifica cuando hay necesidad
de auto-escalado horizontal, múltiples nodos o alta disponibilidad
multi-región. Ninguno de esos escenarios aplica a un servidor único
on-premise con esta cantidad de usuarios. Introducir K8s aquí sería
**sobre-ingeniería**: más superficie para mantener, sin beneficio
proporcional.

## Consecuencias
- Backups y alta disponibilidad quedan a nivel de infraestructura del
  servidor (snapshots de VM, backups de volúmenes Docker) — se detalla
  en `07-plan-despliegue.md`.
- Si en el futuro la institución migra a un clúster con múltiples
  dependencias/jurisdicciones y necesita alta disponibilidad real,
  esta decisión se revisita (los contenedores ya están preparados para
  eso, migrar de Compose a K8s no requiere rediseñar la app).
