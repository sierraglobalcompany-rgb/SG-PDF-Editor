# F12.3 — Cierre exact-head de Formularios AcroForm existentes

Fecha: 2026-10-13  
Rama: `feat/f12-3-task5-minimal-wpf-flow`  
Base F12.2: `5fc7064a950b3242ae93eced676f745c9d32ad8c`  
HEAD previo al cierre documental: `38a4aab4b2593c16b6d95233c84147f2e3210ab9`

## Resultado

F12.3 queda **CLOSED / GREEN** dentro del alcance probado de campos de texto AcroForm clásicos existentes (`/Tx`).

Declaración segura del slice:

> SG PDF puede rellenar y guardar transaccionalmente campos de texto AcroForm clásicos existentes dentro del flujo soportado, preservando el original y validando automáticamente una muestra representativa de valor, apariencia y estructuras.

La preservación se declara exclusivamente como **representative preservation gate**; no constituye una promesa universal de preservación o compatibilidad para cualquier PDF/formulario.

## AUTO PASS — alcance demostrado

- campo `/Tx` clásico plano;
- campo `/Tx` jerárquico mediante `/Kids`;
- `/FT /Tx` heredado;
- nombres compuestos como `Parent.Child`;
- lectura de valores existentes;
- persistencia de `/V` tras reapertura;
- regeneración y validación de `/AP/N` para el campo objetivo;
- validación visual representativa mediante PDFium FormFill + `FPDF_FFLDraw`;
- publicación transaccional `preflight → supported-field gate → fingerprint → same-dir temp → validation → fingerprint recheck → atomic publish`;
- original intacto;
- destino previo preservado ante fallo pre-publicación, cancelación o drift de fuente;
- fuente firmada o abierta con contraseña bloqueada fail-closed;
- UI WPF mínima reutilizando el panel de propiedades y `Guardar como...`;
- envío al writer únicamente de campos realmente modificados;
- no-op save omitido;
- reset del estado de formulario al cambiar documento/workspace;
- exclusividad con FIRMAR, EDITAR y ORGANIZAR reutilizando guards existentes.

## Representative preservation gate

La muestra automatizada conserva/verifica:

- page count;
- texto no relacionado;
- imagen XObject nativa;
- bookmark + destino;
- enlace URI;
- segundo `/Tx` no objetivo con valor + `/AP/N` intactos;
- metadatos Info seleccionados;
- bytes exactos del PDF fuente.

Esto es evidencia representativa, no una garantía universal sobre cualquier estructura PDF.

## NOT RUN / fuera de alcance

- checkbox y radio (`/Btn`);
- combo box y list box (`/Ch`);
- XFA;
- JavaScript, cálculos y acciones;
- campos de firma como campos de formulario;
- flattening;
- creación o diseñador de formularios;
- styling avanzado;
- compatibilidad universal Adobe/Foxit/Edge u otros lectores;
- preservación universal de toda estructura PDF imaginable.

## KISS / Ley del Ruido

Auditoría desde F12.2 hasta el cierre:

- sin cambio de `.csproj`, lockfile o paquete;
- sin segundo motor PDF;
- sin backend, red o telemetría;
- producción concentrada en `Features/Forms` + un partial WPF focal;
- integraciones de lifecycle en Reader/Labels/Sign/Edit/Organize de una línea cada una donde corresponde;
- sin coordinador de modos, framework MVVM/controller paralelo ni segunda shell/sidebar;
- el validador redundante anterior se eliminó y el save path usa una sola validación pre-publicación.

Resultado: **KISS PASS / Ley del Ruido PASS**.

## Evidencia de verificación previa al cierre documental

Rerun fresco sobre `38a4aab4b2593c16b6d95233c84147f2e3210ab9`:

- workflow: `38451787497`, attempt rerun;
- job: `115504632038`;
- Windows build: 0 warnings / 0 errors;
- tests: 1173/1173 PASS;
- failed: 0;
- skipped: 0;
- repository hygiene: PASS.

La única advertencia observada pertenece al runner GitHub Actions por deprecación de Node/punycode y no al código SG PDF.

## Regla de cierre exact-head

El commit que contiene este documento y actualiza el plan debe pasar nuevamente el workflow Windows completo. Si ese CI exact-head falla, F12.3 vuelve a estado bloqueado y no se inicia otra slice. Si pasa, F12.3 queda cerrado sin merge y sin iniciar F12.4.
