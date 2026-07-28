# Flujo funcional de una importación

Documento de referencia del módulo **/imports**. Describe qué ocurre en cada etapa cuando un
usuario carga la Ficha Departamental de Gobernanza diligenciada en el archivo oficial, dónde queda
la información, qué estados existen, cuándo se vuelve visible, cuándo se puede editar y qué pasa
si hay errores parciales.

> Archivo oficial: **`ficha_departamental_gobernanza.xlsm`** (misma plantilla que se descarga desde
> el portal). Es la fuente única de verdad de hojas, columnas y listas. Lo que se valida es el
> **formato y la estructura**, no el nombre del archivo.
>
> **Alcance:** se importan las cinco hojas de la ficha departamental —`Identificación`,
> `Diagnóstico ecosistema`, `Oportunidades de cambio`, `Ejes PNMC` y `Actores`— con **todas sus
> filas diligenciadas**. Las hojas `Indicadores` y `Detalle Indicadores` **no se importan**: el
> archivo puede traerlas y se ignoran, porque esos avances se registran desde el módulo de
> Indicadores.

---

## 1. Etapas del flujo

En el portal estas etapas se muestran al pulsar **Cargar y validar**, dentro de una ventana de
progreso que acompaña la carga y termina mostrando el resultado real: el estado alcanzado, la etapa
en la que se detuvo si fue rechazada y el siguiente paso.


| # | Etapa | Qué hace el sistema | Resultado si falla |
|---|-------|---------------------|--------------------|
| 1 | **Archivo seleccionado** | El usuario elige o arrastra el archivo. El navegador valida extensión y tamaño antes de subirlo. | No se envía nada al servidor. Mensaje inmediato con la corrección. |
| 2 | **Validación del formato** | El servidor verifica extensión `.xlsm`, tamaño (máx. 10 MB) y que el libro se pueda abrir. **El nombre del archivo es libre**: se acepta renombrado por departamento, fecha o versión. | Lote **Importación rechazada**. No se guarda ningún dato. |
| 3 | **Validación de la estructura** | Verifica las cinco hojas que se importan (`Identificación`, `Diagnóstico ecosistema`, `Oportunidades de cambio`, `Ejes PNMC` y `Actores`) y que cada columna esté en su posición con el encabezado esperado. También rechaza la plantilla en blanco. | Lote **Importación rechazada**, indicando hoja y columna. |
| 4 | **Validación de los datos** | Copia las filas a las tablas de trabajo del lote y compara cada valor contra los catálogos oficiales (departamentos, municipios por departamento, ejes y componentes PNMC, roles por tipo de agente, niveles, años, correos, celulares…). | Las filas con error quedan fuera; se reportan como incidencias. |
| 5 | **Creación del lote** | Queda el registro de la carga con archivo, fecha, conteos y resultado, consultable en *Historial de lotes*. | — |
| 6 | **Procesamiento** | Materializa la información válida en la ficha departamental y sus secciones, tomando **todas las filas** de cada hoja. Cada sección se guarda de forma aislada. | Si una sección falla, las demás se conservan y se informa qué sección revisar. |
| 7 | **Importación completada** | Se calcula el estado final y se informa el resultado con su siguiente paso. | — |
| 8 | **Datos disponibles en Gobernanza** | La ficha queda visible y editable en `/governance`, lista para revisión del Líder de Gobernanza. | — |

---

## 2. Estados de una importación

Los códigos internos (`Validating`, `Processing`, `Completed`, `CompletedWithWarnings`,
`CompletedWithErrors`, `Rejected`) **nunca** se muestran al usuario: el backend los traduce a una
etiqueta funcional con descripción y siguiente paso (`ImportStatusCatalog`).

| Etiqueta que ve el usuario | Cuándo ocurre | Siguiente paso |
|---|---|---|
| **Archivo en validación** | Se está verificando formato y estructura. | Esperar unos segundos. |
| **Procesando archivo** | El archivo es válido y se están leyendo los datos. | Esperar el resultado. |
| **Importación exitosa** | Todo se importó sin incidencias. | Consultar la ficha en Gobernanza. |
| **Importación completada con observaciones** | Se importó, pero hay valores por revisar o filas con errores que quedaron fuera. | Corregir lo indicado y volver a cargar. |
| **Importación rechazada** | El archivo no corresponde al formato oficial o no pudo procesarse. | Descargar la plantilla oficial y volver a cargar. |

---

## 3. Dónde queda almacenada la información

1. **Registro del lote** — `import_batches`: archivo, tamaño, fecha, conteos, estado y el correo de
   quien hizo la carga (`created_by_email`).
2. **Incidencias** — `import_validation_issues`: severidad, hoja, fila, celda, código, valor
   recibido y contexto (valor esperado, cómo corregirlo y detalle técnico de soporte).
3. **Filas de trabajo del lote** (staging) — `import_*_staging_rows`: copia fiel de lo leído en el
   Excel, para auditoría y trazabilidad de la carga.
4. **Datos definitivos** — `fichas_departamentales` y sus secciones (`diagnosticos_ecosistema`,
   `oportunidades_cambio`, `ejes_pnmc`, `actores` y sus tablas de selección múltiple).

`Diagnóstico ecosistema` es la única hoja de la que se toma una sola fila, porque la ficha tiene un
diagnóstico. Si trae varias, se usa la primera diligenciada y se deja la observación. De
`Oportunidades de cambio`, `Ejes PNMC` y `Actores` se guardan **todas** las filas.

---

## 4. Quién ve cada importación

El historial de `/imports` es **personal**: el Gestor Departamental ve únicamente los archivos que
él cargó, tanto en la lista de lotes como en el detalle de incidencias. El **Líder de Gobernanza**
y el **Administrador** ven las cargas de todo el equipo, porque hacen seguimiento a los
departamentos. Los lotes anteriores a este cambio no tienen autor registrado y solo los ven esos
dos roles.

---

## 5. Estado, visibilidad y edición de los datos importados

- **Visibilidad**: al terminar con estado *Importación exitosa* o *Importación completada con
  observaciones*, la ficha aparece de inmediato en `/governance`. Un lote *rechazado* no crea ni
  modifica ninguna ficha.
- **Estado de revisión**: la ficha nace en **Borrador · sin revisar**. El Líder de Gobernanza la **aprueba** o la
  **devuelve** con observaciones; en ambos casos el Gestor Departamental recibe una notificación en
  el portal con el cambio de estado y el motivo.
- **Edición**: el Gestor Departamental (y el Administrador) pueden editar todas las secciones desde
  el momento en que la ficha es visible. El Líder de Gobernanza la consulta en modo lectura.
- **Recargas**: volver a cargar el archivo del mismo departamento y misma fecha de levantamiento
  **actualiza** la ficha existente (no la duplica). Las secciones de colección
  (oportunidades, ejes, actores) se reemplazan con el contenido del archivo.
- **Identificación obligatoria**: sin fecha de levantamiento y departamento válidos en la hoja
  `Identificación` no puede materializarse la ficha; se informa como incidencia.

---

## 6. Errores parciales

- La importación es **parcial**: las filas correctas se guardan y solo quedan fuera las filas con
  errores de **severidad Error**, que se listan con el detalle exacto por fila y campo. El lote
  queda como *Importación completada con observaciones*.
- La hoja `Identificación` es la excepción: si su fila tiene errores no puede crearse la ficha y no
  se guarda nada (todas las demás secciones cuelgan de ella).
- Si **todas** las filas de una sección tienen errores, esa sección no se reemplaza: se conserva lo
  que ya estaba guardado de una carga anterior en lugar de quedar vacía.
- Las **observaciones** (severidad Warning, por ejemplo un valor de selección múltiple que no está
  en el catálogo o un celular que no tiene 10 dígitos) **no bloquean** la importación: el dato se
  guarda y se deja la advertencia para revisión. Un valor de selección múltiple que no exista en el
  catálogo simplemente se omite de esa celda; el resto de la fila y de la ficha sí se guarda.
- Si una sección falla al guardarse, las secciones anteriores permanecen guardadas y se informa
  cuál revisar: la carga no se pierde por completo.
- Las filas de trabajo del lote se conservan aunque la carga tenga errores, de modo que siempre
  puede auditarse qué venía en el archivo.

---

## 7. Redacción de las incidencias

Cada incidencia se presenta con: **hoja**, **fila**, **columna**, **nombre del campo** (el mismo que
aparece en la Ficha Departamental del portal), **valor recibido**, **valor esperado** y **cómo
corregirlo**. Ejemplo:

```
Fila 18 · Campo "Correo electrónico"          Celda G18
El valor "juan.gmail.com" no corresponde a un correo electrónico válido.

Fila: 18 · Columna: G · Campo: Correo electrónico
Valor recibido: juan.gmail.com
Valor esperado: Un correo con el formato usuario@dominio.com.
Cómo corregirlo: Corrija el correo en la celda G18 de la hoja "Actores" incluyendo el signo @ y el
dominio (por ejemplo nombre@entidad.gov.co).
```

El nombre del campo se resuelve desde el Blueprint (`BlueprintFieldLocator`), por lo que siempre
coincide con la etiqueta del formulario web y con el encabezado del archivo oficial. El detalle
técnico (excepciones) se guarda aparte y solo lo ve el Administrador, en la sección
*Detalle técnico (soporte)*.
