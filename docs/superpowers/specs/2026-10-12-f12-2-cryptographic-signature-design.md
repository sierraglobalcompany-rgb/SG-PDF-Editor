# F12.2 — Firma criptográfica PDF local

Fecha: 2026-10-12  
Base: F12.1 Safe Redaction automatizada cerrada en `feat/f12-professional`  
Rama: `feat/f12-professional`

## 1. Objetivo

Añadir una primera firma criptográfica PDF real, local y verificable, sin convertir SG PDF Editor en una plataforma de certificados ni depender de servicios web.

Flujo objetivo del MVP:

`PDF local sin firma previa → certificado X.509 con clave privada desde .pfx/.p12 → firma CMS/PKCS#7 SHA-256 → Guardar como... → reabrir → validar ByteRange + CMS → original intacto`

La firma visual de F3 y la firma criptográfica son conceptos distintos. F12.2 puede reutilizar una apariencia visual existente más adelante, pero el primer cierre técnico depende de la validez criptográfica, no de una imagen.

## 2. Principios obligatorios

1. KISS/YAGNI: implementar primero el camino PFX/P12 mínimo y probado.
2. Offline: la firma básica no requiere red, cuenta, SaaS, API key, TSA, OCSP ni CRL online.
3. `Guardar como...`: el archivo fuente nunca se sobrescribe durante F12.2.
4. Fail-closed: si no puede demostrarse la integridad criptográfica de la salida, no se publica.
5. Reusar antes de añadir: PDFsharp 6.2.4 ya presente estructura la firma PDF; .NET aporta CMS/PKCS#7 mediante `System.Security.Cryptography.Pkcs` si supera el gate de dependencia.
6. PDFium sigue siendo el motor principal de lectura/render del producto y se amplía solo con primitivas de inspección necesarias para validar la firma.
7. No segundo writer general, no arquitectura de proveedores, no plugin framework.
8. No merge a `main` sin aprobación explícita.

## 3. Alcance MVP

El primer slice automatizado admite únicamente:

- PDF local existente;
- fuente no modificada durante la operación;
- archivo `.pfx` o `.p12` local;
- contraseña del contenedor recibida en memoria y no persistida;
- certificado X.509 que incluya clave privada utilizable;
- firma CMS/PKCS#7 detached compatible con firma PDF;
- SHA-256 como digest inicial;
- un único proceso de firma por operación;
- salida nueva mediante `Guardar como...`;
- reapertura y validación criptográfica automatizada;
- limpieza de temporales en éxito, error y cancelación.

## 4. Fuera de alcance del primer MVP

- timestamp TSA;
- validación online de revocación OCSP/CRL;
- HSM, token USB, smartcard o middleware PKCS#11;
- firma remota o en nube;
- cuentas, backend o almacenamiento de certificados;
- PAdES-LT/LTA o promesas de conformidad regulatoria;
- firma masiva/batch;
- múltiples firmantes o countersignatures;
- edición posterior de un PDF ya firmado;
- promesas de validez jurídica, identidad certificada o no repudio;
- selector del almacén de certificados de Windows en el primer GREEN;
- segunda biblioteca PDF o biblioteca criptográfica de terceros si .NET cubre CMS de forma suficiente.

## 5. Decisión técnica

### 5.1 Estructura de firma PDF

Usar `PdfSharp.Pdf.Signatures.DigitalSignatureHandler` del PDFsharp ya presente para gestionar la estructura PDF de firma, reserva de `Contents` y `ByteRange`.

No implementar manualmente el diccionario de firma, offsets ni parcheo hexadecimal salvo evidencia concreta de una carencia en PDFsharp 6.2.4.

### 5.2 Firmante CMS

Implementar un adaptador mínimo de `PdfSharp.Pdf.Signatures.IDigitalSigner` respaldado por APIs BCL de .NET:

- `X509Certificate2` para el certificado y clave privada;
- `SignedCms` / `CmsSigner` para CMS/PKCS#7;
- SHA-256;
- firma detached de los bytes entregados por PDFsharp.

`System.Security.Cryptography.Pkcs` solo puede entrar al runtime después de un gate explícito que confirme versión compatible con `net10.0-windows`, licencia aceptable y lock reproducible.

No añadir BouncyCastle preventivamente.

### 5.3 Inspección y validación independiente

La aceptación no puede depender solo de que PDFsharp no lance excepción al guardar.

La salida se reabre y se valida de forma independiente:

1. PDFium confirma que existe exactamente la firma esperada;
2. se leen `ByteRange` y `Contents` mediante las APIs de firma del PDFium pinneado, si están exportadas;
3. se reconstruyen exactamente los bytes cubiertos por `ByteRange`;
4. `SignedCms` decodifica `Contents` y comprueba la firma contra esos bytes;
5. la salida abre/renderiza correctamente;
6. el archivo fuente conserva fingerprint/bytes intactos.

Si las APIs de inspección de firma necesarias no están disponibles en el PDFium pinneado, el capability gate debe fallar y se evalúa el fallback KISS más pequeño antes de escribir un parser PDF general.

## 6. Política de seguridad

Se rechaza antes de publicar cuando ocurra cualquiera de estos casos:

- PFX/P12 inexistente o ilegible;
- contraseña incorrecta;
- contenedor sin certificado apropiado;
- certificado sin clave privada;
- clave privada inaccesible o algoritmo no soportado por el MVP;
- certificado fuera de su periodo de validez, salvo decisión futura explícita distinta;
- PDF fuente abierto con contraseña;
- PDF fuente con firma criptográfica previa en este MVP;
- source fingerprint distinto antes de publicación;
- CMS inválido;
- `ByteRange` inválido, solapado, fuera de archivo o que no represente la firma esperada;
- firma que no cubra los bytes esperados;
- salida que no reabra/renderice;
- cancelación o error en cualquier etapa.

No se intenta “reparar” silenciosamente un certificado o PDF incompatible.

## 7. Transacción

Orden mínimo:

1. validar entradas y snapshot/fingerprint del origen;
2. cargar PFX/P12 local y validar certificado/clave;
3. preparar documento de salida en temporal;
4. PDFsharp aplica `DigitalSignatureHandler` con el firmante CMS;
5. cerrar completamente handles/streams de escritura;
6. reabrir temporal y validar firma + render + estructura básica;
7. volver a comprobar fingerprint del origen;
8. publicar destino atómicamente cuando sea viable;
9. limpiar temporales siempre.

El destino previo válido no debe destruirse si la nueva firma falla antes de la publicación.

## 8. UX mínima posterior al gate técnico

La UI solo se abre después de demostrar el pipeline criptográfico.

Primer flujo previsto:

- comando profesional `Firmar digitalmente`;
- selector local `.pfx/.p12`;
- contraseña en control seguro/no persistente;
- mostrar sujeto, emisor y vigencia del certificado antes de confirmar;
- `Guardar como...` como único destino;
- resultado conservador: `Firma criptográfica creada y validada localmente` solo cuando el validador pasa.

Una apariencia visual reutilizando F3 es opcional para un bloque posterior y nunca sustituye la validación criptográfica.

## 9. Windows Certificate Store

El almacén de certificados de Windows es la segunda entrada prevista dentro de F12.2, no parte del primer GREEN.

Solo se incorpora después de cerrar PFX/P12 y debe reutilizar el mismo pipeline de firma/validación. No se crea otro writer ni otro validador.

La selección se limitará a certificados con clave privada apropiada y se dejará que Windows gestione proveedores de clave soportados por `X509Certificate2`/CNG/CSP. Smartcards/HSM que exijan UX o middleware específico quedan fuera hasta tener hardware y evidencia.

## 10. Primer micro-gate obligatorio

Antes de UI, Windows Store o apariencia visual:

1. fixture genera o carga un certificado de prueba local con clave privada;
2. se firma un PDF sintético existente con SHA-256;
3. se demuestra que el archivo fuente no cambió;
4. PDFium detecta una firma;
5. `ByteRange` es válido y acotado al archivo;
6. `Contents` contiene CMS decodificable;
7. `SignedCms` valida la firma sobre exactamente los bytes definidos por `ByteRange`;
8. el PDF firmado reabre y renderiza;
9. alterar un byte cubierto por la firma hace fallar la validación.

Este gate decide GO/NO-GO del camino PDFsharp + .NET PKCS para F12.2.

## 11. Dependencias y licencias

Estado actual:

- `PDFsharp` 6.2.4 ya es dependencia runtime aprobada;
- `bblanchon.PDFium.Win32` sigue como PDF engine principal;
- el lock actual no incluye `System.Security.Cryptography.Pkcs`.

Antes de añadir `System.Security.Cryptography.Pkcs`:

- fijar versión exacta compatible con .NET 10;
- verificar licencia y transitive dependencies;
- actualizar lock/manifest de terceros según las reglas existentes;
- demostrar restore locked y funcionamiento offline después de disponer del paquete localmente.

No se añade BouncyCastle, iText, Syncfusion, Aspose ni otro motor/SDK comercial o copyleft fuerte para este slice.

## 12. Criterios de aceptación automatizada

F12.2 MVP PFX puede considerarse automatizadamente cerrado solo si:

1. capability gate de PDFsharp/PDFium/PKCS pasa en el runtime pinneado;
2. existe RED→GREEN del firmante CMS y del PDF firmado real;
3. certificado sin clave privada y contraseña incorrecta fallan de forma controlada;
4. fuente firmada previamente sigue bloqueada en este MVP;
5. original permanece intacto;
6. salida firmada reabre/renderiza;
7. `ByteRange` y CMS se validan independientemente;
8. corrupción de un byte firmado se detecta;
9. cancelación/fallo no publican salida parcial ni destruyen un destino previo válido;
10. Release build + suite completa pasan en Windows CI sobre HEAD exacto;
11. no entra red, cuenta, backend, segundo PDF engine ni dependencia criptográfica innecesaria;
12. pruebas automatizadas no se presentan como validación jurídica, confianza CA, revocación online ni interoperabilidad física/manual.

## 13. Gates manuales que seguirán abiertos

Aunque el slice automatizado cierre, seguirán NOT RUN hasta evidencia real:

- PFX/P12 reales de usuario;
- certificados emitidos por distintas autoridades;
- validación en Adobe Acrobat Reader, Foxit y Edge;
- Windows Certificate Store real;
- smartcard/token/HSM;
- documentos privados representativos;
- comprobación offline física en equipo limpio;
- valoración jurídica/regulatoria de firmas según el contexto de uso.

## 14. Ley del Ruido

Orden obligatorio ante cualquier carencia:

`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`

Para F12.2 esto significa:

- reutilizar PDFsharp antes de construir sintaxis PDF;
- reutilizar BCL .NET antes de incorporar biblioteca criptográfica externa;
- reutilizar el patrón de fingerprint/temporales/Save As antes de crear otra infraestructura transaccional;
- reutilizar F3 para apariencia visual si se necesita;
- no añadir abstracciones para TSA, HSM, Store, múltiples algoritmos o proveedores antes de que exista un caso real.
