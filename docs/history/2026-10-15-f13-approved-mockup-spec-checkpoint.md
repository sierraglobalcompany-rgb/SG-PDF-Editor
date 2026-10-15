# SG PDF Editor — Checkpoint F13 Mockup aprobado

**Fecha:** 2026-10-15  
**Rama:** `plan/post-ui4-f13-f18-expansion-20261014`

## Estado

- UI-4 continúa `FAIL-P1`; no se salta ese gate.
- El usuario aprobó visualmente el nuevo mockup SG PDF Dashboard + Documento abierto.
- El mockup pasa a ser contrato visual de F13, no mera inspiración.
- No se implementó código productivo en este bloque.
- `main` no fue tocado.

## Spec autoritativo

`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

Commit de creación del spec:
`6eaa6bc211dc7c07eb99fcdaeb74541a4f2da4d5`

Commit de STATE actualizado:
`2f5a7647a4c952c29b85bdb9830ec522df6e233a`

## Identidad del mockup aprobado

Nombre de referencia:
`sg_pdf_f13_approved_mockup.jpg`

Preview comprimido generado para continuidad:
- tamaño: 1400×788;
- SHA-256: `0180644f4167f9dd25ea6992c9debe84968c5cfd478f3f167875bf0590560f51`.

Original generado en esta conversación:
- tamaño: 1672×941;
- SHA-256: `1d7aa71ac3313f074e0448f29f158d95b6c468a4bf36255729b1c5684c74bd47`.

Si la imagen binaria no está disponible en un chat futuro, el spec textual conserva el contrato visual suficiente para continuar. Si se vuelve a adjuntar una imagen, comprobar el hash cuando sea posible.

## Decisiones visuales congeladas

- Dashboard local/offline.
- Rail Home/documento de ~96 px.
- Tool cards con icono + título + descripción.
- Documento abierto: rail izquierdo + canvas central + panel contextual derecho de ~320 px.
- Toolbar contextual por owner.
- Segoe UI / WPF nativo / ThemeMode System.
- Light + Dark obligatorios.
- Sin login, publicidad, cloud obligatorio ni IA.
- No tabs multi-documento en F13.
- No framework UI nuevo.
- El placeholder `SG` del mockup NO es el icono oficial.
- El icono oficial requiere la fuente aprobada real del usuario.

## Ownership

El shell solo enruta a owners existentes. No reimplementa motores ni writers.

Owner map detallado:
`docs/product/2026-10-14-existing-tool-entrypoint-inventory.md`

## Próximo gate metodológico

Antes de escribir el plan técnico exacto por archivos/componentes (B), el usuario debe revisar/aceptar el spec escrito F13.

Después de esa aprobación:
1. usar `superpowers:writing-plans`;
2. reemplazar/actualizar el plan F13 preliminar contra el mockup aprobado;
3. dividir en tareas TDD pequeñas con checkpoint por tarea;
4. implementación nativa por ChatGPT;
5. usar Codex únicamente para QA real Windows/GUI que ChatGPT no puede ejecutar.

## Orden de producto preservado

`resolver UI-4 P1 → rerun UI-4 → F13 shell aprobado → F14 productización → F15 Edit V3 → F16/F17/F18`
