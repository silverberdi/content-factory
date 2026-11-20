# Design

## Context

El estado observado en `src/web` ya declara Angular 22, PrimeNG 22, Tailwind CSS 4, `@primeuix/themes` 3 y `@primeng/themes` 21 residual. `app.config.ts` consume Aura desde `@primeuix/themes/aura`. El contexto canónico y `22_TECH_STACK_LOCK.md` todavía contienen referencias a Angular/PrimeNG 21, por lo que la primera tarea debe separar estado efectivo, documentación desactualizada y consumidores reales antes de cualquier edición.

El cambio es de frontend y toolchain, no de dominio, API, persistencia, autenticación, n8n ni despliegue. La navegación y las pantallas actuales son el contrato de regresión. Las comprobaciones de navegador para navegación, async UI, viewport y temas deben ser ejecutadas por el executor configurado, con evidencia observable; no se delegan al operador ni a `human_validation`.

## Goals / Non-Goals

**Goals:**

- Dejar una única combinación efectiva y reproducible de dependencias para Angular 22, PrimeNG 22, Tailwind CSS 4 y `@primeuix/themes` 3.
- Tomar la decisión sobre `@primeng/themes` 21 a partir de consumidores/importaciones reales, actualizando también el lockfile.
- Preservar el comportamiento de las rutas y superficies existentes en desktop, tablet y mobile, en ambos temas.
- Alinear únicamente la documentación canónica afectada con evidencia del estado final.
- Producir evidencia de instalación limpia, tests, build y checks focalizados de navegador.

**Non-Goals:**

- No cambiar requisitos funcionales, APIs, backend, datos, seguridad, CI/CD, despliegue o producción.
- No introducir otra librería de temas, un framework de testing adicional ni una abstracción de layout.
- No corregir deuda visual o funcional no causada por la consolidación.

## Decisions

1. **Reconfirmación antes de edición.** Censar `package.json`, lockfile, configuración Angular, importaciones de temas y referencias a `@primeng/themes`; registrar el resultado en la evidencia del change. Se elige este orden sobre editar por intuición porque el contexto canónico contradice el estado observado y el usuario exige basarse en el repositorio real.

2. **Dependencia de temas anterior condicionada por consumidores.** Si el censo no encuentra consumidores de `@primeng/themes`, retirar solo esa dependencia y regenerar el lockfile con la versión de npm declarada. Si existe un consumidor, conservarlo o migrarlo con el menor cambio compatible, documentando el consumidor y la razón. No se sustituye `@primeuix/themes` ni se rediseña Aura.

3. **Cambios mínimos de compilación.** Alinear solo paquetes cuya incompatibilidad sea demostrada por instalación, compilación o tests. Conservar las versiones objetivo y la versión de Node compatible declarada por el toolchain; no hacer una actualización general de Angular, PrimeNG, TypeScript o Tailwind.

4. **Verificación proporcional y ejecutable.** Reutilizar scripts y pruebas existentes de `src/web`, y añadir o ajustar checks solo para la superficie modificada. El executor configurado debe ejecutar `npm ci`, tests locales, build y los checks Playwright existentes o focalizados para navegación, async UI, viewport y temas. La evidencia debe incluir comandos, resultado y artefactos observables.

5. **Documentación canónica condicionada por evidencia.** Actualizar `22_TECH_STACK_LOCK.md` y cualquier documento canónico directamente afectado solo después de confirmar el estado final. La discrepancia Angular 21/22 se trata como sincronización documental dentro de este change, no como autorización para redefinir el stack por anticipado.

6. **Sin cambios de arquitectura o runtime.** No hay API, migración, seguridad, observabilidad de backend ni invocación n8n. Por tanto no se añaden adaptadores, servicios, eventos, bases de datos ni infraestructura; el rollback consiste en revertir el diff de dependencias, lockfile, documentación y cualquier ajuste de compilación junto con sus checks.

## Risks / Trade-offs

- **[Riesgo]** `@primeng/themes` 21 puede estar referenciado fuera de los imports obvios. → Buscar en todo `src/web` y configuración relacionada antes de retirar; validar instalación y build tras el cambio.
- **[Riesgo]** La discrepancia entre contexto canónico y estado observado puede ocultar una decisión aún no sincronizada. → No alterar el código basándose en el contexto 21; conservar la evidencia del estado real y limitar la documentación a las referencias afectadas.
- **[Riesgo]** Una normalización de toolchain puede cambiar CSS, temas o tiempos de carga sin cambiar TypeScript. → Ejecutar checks de navegador en ambos temas y viewports definidos, comparando navegación y controles afectados con evidencia reproducible.
- **[Riesgo]** La instalación limpia puede depender de red o del runtime disponible. → Declarar Node/npm compatibles, usar el lockfile como fuente reproducible y reportar explícitamente cualquier bloqueo externo; no compensarlo con cambios de versiones especulativos.
