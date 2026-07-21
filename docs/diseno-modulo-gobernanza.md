# Documento de diseño — Módulo `/governance` (Ficha Departamental de Gobernanza)

> **Fuente única de verdad (SSOT):** `docs/ficha_departamental_gobernanza.xlsm`
> **Alcance:** creación, edición, importación y exportación de la Ficha Departamental de Gobernanza.
> **Regla de oro:** la plataforma se adapta al Excel; el Excel **no** se adapta a la plataforma.
> **Método:** análisis técnico realizado sobre el OOXML crudo del archivo (no sobre documentación derivada). Fecha: 2026-07-06.

---

## 0. Resumen ejecutivo y hallazgos clave

El `.xlsm` es un paquete OOXML (ZIP) con protección **a nivel de hoja/libro** (contraseña `gobernza2026`), **no cifrado a nivel de archivo**. Esto es una ventaja crítica: podemos leerlo y reescribir sus celdas de datos con librerías OOXML manejadas (sin Excel instalado, compatible con IIS/Plesk).

**Diagnóstico del código existente** (el proyecto ya está andamiado — .NET 9 Clean Architecture + Angular 20 + MySQL):

| # | Hallazgo | Severidad | Acción |
|---|----------|-----------|--------|
| 1 | `ExcelExportService` **reconstruye un libro nuevo desde cero** con ClosedXML, con encabezados/columnas inventados y solo unos pocos campos. Genera `.xlsx`, **pierde macros, validaciones, estilos y el modelo**. | 🔴 Crítica | Reemplazar por escritura sobre la **plantilla oficial** con `DocumentFormat.OpenXml`. |
| 2 | `ImportTemplateService` genera una plantilla `.xlsx` **nueva y divergente**: nombres de rango distintos (`NivelPrioridad`≠`NivelAMB`, `Anios`≠`Años`, `CompEje_1`≠`Comp_Eje_1`, `Rol_1`≠`Acto_*`), sin macros. Rompe el round-trip offline↔web. | 🟠 Alta | Servir la **plantilla oficial `.xlsm`** como descarga en blanco (opcionalmente sincronizando `Variables` desde catálogos). |
| 3 | Las 3 listas dependientes (Ciudad←Depto, Componente←Eje, Rol←TipoAgente) usan validación **x14 `INDIRECT(VLOOKUP(...))`**, no validación clásica. La documentación previa las describía como validación normal. | 🔵 Referencia | Modelar como listas dependientes en el schema; conservar tal cual en export. |
| 4 | El multi-select es **una sola macro** `ThisWorkbook.Workbook_SheetChange` con separador **`", "`** y semántica *toggle*. El import ya usa `", "` (correcto). | 🟢 OK | Documentado; replicar en UI con control multi-select. |
| 5 | `Rol en el ecosistema` (Actores!D) es **dependiente Y multi-select** a la vez. | 🟠 Importante | El schema debe soportar el combinado dependiente+múltiple. |
| 6 | Reglas de validación de import ligeramente desalineadas: teléfono Excel = `longitud 7–20`; el código solo advierte si `>7`. Email Excel exige además `LEN>=5`. | 🟡 Baja | Alinear `WorkbookImportService`. |
| 7 | Artefactos heredados a **preservar pero ignorar**: modelo Power Query `IFCDFramework2018.xlsx` (Activities/Objectives/Outputs), `webextensions` (taskpanes), rangos legacy `TipoAgente`/`ComponentePNMC`. | 🔵 Referencia | No mapear; se conservan intactos al preservar la plantilla. |
| 8 | Rango `Departamentos` = `A2:A35` (34 celdas) para **33 departamentos** (A35 vacía). | 🟡 Baja | Verificar el seed de `catalog_departments` = 33. |

**Recomendación central de arquitectura:** introducir un **Blueprint de la Ficha** (metadatos derivados del Excel) como SSOT compartida entre backend y frontend. Un único registro de mapeo *(hoja, columna, fila, tipo, validación, catálogo, editable-por, multi-select, dependiente-de)* alimenta simultáneamente: (a) el formulario dinámico Angular, (b) la validación FluentValidation, (c) la lectura de import y (d) la escritura de export. Así se elimina la definición manual campo por campo y se garantiza que las cuatro rutas nunca se desincronicen.

---

## 1. Inventario de hojas, campos y estructuras

### 1.1 Hojas del libro (8)

| # | Hoja | `sheetId` | Visible | ListObject (tabla) | Rango | Rol que diligencia |
|---|------|-----------|---------|--------------------|-------|--------------------|
| 1 | **Identificación** | 13 | Sí | `Identificacion` | `A1:G51` | Gestor |
| 2 | **Diagnóstico ecosistema** | 14 | Sí | `Diagnostico` | `A1:N51` | Gestor |
| 3 | **Oportunidades de cambio** | 15 | Sí | `Oportunidades` | `A1:G51` | Gestor |
| 4 | **Ejes PNMC** | 16 | Sí | `EjesPNMC` | `A1:R51` | Gestor |
| 5 | **Actores** | 17 | Sí | `Actores` | `A1:H51` | Gestor |
| 6 | **Indicadores** | 18 | Sí | `Indicadores` | `A2:AG9` | **Líder** |
| 7 | **Detalle Indicadores** | 19 | Sí | *(sin tabla)* | `A1:K14` | **Líder** |
| 8 | **Variables** | 20 | `veryHidden` | *(sin tabla)* | catálogos | *(sistema)* |

> **Permisos (según `docs/instrucciones.md`, autoritativo):** el **Gestor Departamental** edita hojas 1–5 (Identificación→Actores). El **Líder de Gobernanza** diligencia hojas 6–7 (Indicadores, Detalle Indicadores) y aprueba/devuelve. El **Administrador** todo. *(La frase del brief “hojas entre Indicadores…Actores” se interpreta como Identificación→Actores; queda confirmada por instrucciones.md y por la propia aclaración del brief de que Indicadores/Detalle son exclusivas del Líder.)*

### 1.2 Campos por hoja

Leyenda de tipo: `texto`, `fecha`, `entero`, `decimal`, `lista`, `lista-dep` (dependiente), `multi` (multi-select por macro), `calc` (fórmula, celda bloqueada), `fijo` (catálogo predefinido, bloqueada).

#### Hoja 1 — Identificación (`A1:G51`) — 1 fila por ficha
| Col | Campo | Tipo | Validación / catálogo | Edita |
|-----|-------|------|-----------------------|-------|
| A | Fecha de levantamiento | fecha | `DATE(2000,1,1)…DATE(2100,12,31)`, formato dd/mm/aaaa | Gestor |
| B | Departamento | lista | `Departamentos` (33) | Gestor |
| C | Ciudad | lista-dep | `INDIRECT(VLOOKUP($B,Variables!$A$2:$O$34,15,FALSE))` → `Ciu_<Depto>` | Gestor |
| D | Responsable del registro (Gestor) | texto | — | Gestor |
| E | Región OCAD | lista | `RegionOCAD` (6) | Gestor |
| F | Fuente de información | **multi** | `FuenteInfo` (8) · `Multi_Fuente` | Gestor |
| G | Observaciones | texto | — | Gestor |

#### Hoja 2 — Diagnóstico ecosistema (`A1:N51`)
| Col | Campo | Tipo | Validación / catálogo |
|-----|-------|------|-----------------------|
| A | Departamento | calc | `=IF(Identificación!B=""," ",Identificación!B)` |
| B | Caracterización general del ecosistema musical | texto | — |
| C | Fortalezas identificadas | texto | — |
| D | Políticas priorizadas | texto | — |
| E | Debilidades identificadas | texto | — |
| F | Tensiones o conflictos | texto | — |
| G | CODEMUS – Comité Dptal de Música | lista | `EstadoComite` = Creado/Por crear/Inactivo |
| H | Plan Departamental de Cultura | lista | `EstadoPlan` = En ejecución/Por renovar/No existe |
| I | Consejo Departamental de Cultura | lista | inline: Existe/No existe/Por renovar |
| J | Plan Departamental de Música | lista | `EstadoPlan` |
| K | Ordenanzas Culturales | lista | inline: Existe/No existe/Por activar (prompt: nº en Observaciones) |
| L | Consejo Departamental de Música | texto | — |
| M | Mesa sectorial o territorial identificada | texto | — |
| N | Observaciones | texto | — |

#### Hoja 3 — Oportunidades de cambio (`A1:G51`) — N filas
| Col | Campo | Tipo | Validación |
|-----|-------|------|-----------|
| A | Departamento | calc | link a Identificación!B |
| B | Situación identificada | texto | — |
| C | Componente PNMC - Otras dependencias / Entidades | texto | — |
| D | Aliados y creyentes | texto | — |
| E | Territorio de influencia | texto | — |
| F | Nivel de impacto | lista | `NivelAMB` = Alto/Medio/Bajo |
| G | Descripción adicional | texto | — |

#### Hoja 4 — Ejes PNMC (`A1:R51`) — N filas
| Col | Campo | Tipo | Validación |
|-----|-------|------|-----------|
| A | Departamento | calc | link a Identificación!B |
| B | Descripción hallazgo | texto | — |
| C | Eje PNMC | lista | `EjePNMC` (3 ejes) |
| D | Componente PNMC | lista-dep | `INDIRECT(VLOOKUP($C,Variables!$BG$2:$BH$4,2,FALSE))` → `Comp_Eje_1/2/3` |
| E | Acción Estratégica | texto | — |
| F | Política priorizada | texto | — |
| G | Armonización PNC | texto | — |
| H | Armonización PND | texto | — |
| I | Armonización Internacional | texto | — |
| J | Nivel prioridad | lista | `NivelAMB` |
| K | Aliados / Responsables | texto | — |
| L | Fuentes de financiación | texto | — |
| M | Valor de la propuesta (COP) | entero | `≥ 0` |
| N | Enfoques | **multi** | `Enfoques` (5) · `Multi_Enfoques` |
| O | Descripción | texto | — |
| P | Cronograma | lista | `Cronograma` (4) |
| Q | Estado | lista | `EstadoPropuesta` (4) |
| R | Observaciones | texto | — |

#### Hoja 5 — Actores (`A1:H51`) — N filas
| Col | Campo | Tipo | Validación |
|-----|-------|------|-----------|
| A | Departamento | calc | link a Identificación!B |
| B | Nombre del agente (creyente) | texto | — |
| C | Tipo de agente (categoría) | lista | `TiposAgente` (4) |
| D | Rol en el ecosistema | **lista-dep + multi** | `INDIRECT(VLOOKUP($C,Variables!$BB$2:$BC$5,2,FALSE))` → `Acto_Inst_Int/Ext/Sect/Comu` · `Multi_ActorEspec` |
| E | Nivel territorial | **multi** | `NivelTerritorial` (5) · `Multi_NivelTerritorial` |
| F | Número de contacto | texto | `longitud 7–20` |
| G | Correo electrónico | texto | custom: `AND(ISNUMBER(SEARCH("@",G)),ISNUMBER(SEARCH(".",G)),LEN(G)>=5)` |
| H | Observaciones | texto | — |

#### Hoja 6 — Indicadores (`A2:AG9`) — **Líder** · encabezado fila 2 · 7 indicadores fijos filas 3–9
| Col | Campo | Tipo | Validación |
|-----|-------|------|-----------|
| A | Departamento | **multi** | `Departamentos` · `Multi_DeptInd` (A3:A9) |
| B | Acción | fijo | catálogo (7 acciones) |
| C | Nombre Indicador | fijo | catálogo |
| D | Meta | fijo | `decimal ≥ 0`; valores fijos: 25, 5, 25, 1, 1, 1, 6 |
| E…AB | 12 pares (Avance cuantitativo, Detalle) por mes Ene…Dic | decimal / texto | avance `decimal ≥ 0` |
| AC | Valor actual (calc.) | calc | `IF(Meta<=1, MAX(avances), SUM(avances))` |
| AD | % Cumplimiento | calc | `IFERROR(AC/Meta,0)` |
| AE | Fuente | texto | — |
| AF | Año | entero | `2000–2100` |
| AG | Observaciones generales | texto | — |

#### Hoja 7 — Detalle Indicadores (`A1:K14`) — **Líder** · 3 bloques (filas 2, 7, 11)
| Col | Campo | Tipo | Validación |
|-----|-------|------|-----------|
| A | Departamento | **multi** | `Departamentos` · `Multi_DeptDet_1/2/3` (A2, A7, A11) |
| B | Acción | fijo | catálogo |
| C | Nombre | fijo | catálogo |
| D | Meta | fijo | formato % |
| E | Fórmula de cálculo | fijo | desglose de subcriterios y su peso % |
| F | Descripción / detalle | fijo | — |
| G | MESES | **multi** | `Meses` (12) · `Multi_MesesDet` (G2:G14) |
| H | Valor actual (calc.) | calc | `IFERROR(INDEX(Indicadores!$AC$3:$AC$9, MATCH($C, Indicadores!$C$3:$C$9, 0)), 0)` |
| I | Fuente | texto | — |
| J | Año | lista | `Años` (2024–2035) |
| K | Observaciones | texto | — |

### 1.3 Hoja Variables (catálogos) y rangos con nombre

`Variables` (veryHidden) aloja **todos** los catálogos como rangos con nombre. Además contiene **columnas auxiliares de resolución** que hacen funcionar las listas dependientes:
- Col **O**: por cada departamento, el nombre del rango `Ciu_<Depto>` (usado por el VLOOKUP de Ciudad).
- Cols **BB:BC**: mapa `TipoAgente → Acto_*`.
- Cols **BG:BH**: mapa `Eje → Comp_Eje_*`.

El inventario completo de rangos con nombre está en el **Anexo A**.

---

## 2. Reglas de negocio identificadas

1. **Protección/edición.** Todas las hojas están protegidas (SHA-512, `selectLockedCells=1`, orden y autofiltro deshabilitados). El libro tiene `lockStructure=1` (no se pueden agregar/quitar/reordenar hojas). Solo son editables las celdas `locked=false` (los campos de diligenciamiento); encabezados, columnas `calc`/`fijo` y la hoja Variables permanecen bloqueados. → En web: RBAC por hoja + campos `calc`/`fijo` de solo lectura.
2. **Departamento en cascada.** El Departamento se captura una sola vez en Identificación!B; en Diagnóstico/Oportunidades/Ejes/Actores la columna A lo **hereda por fórmula** (no se re-captura). → En web: se toma del encabezado de la ficha; no se pide de nuevo.
3. **Listas dependientes (3).** Ciudad depende de Departamento; Componente PNMC depende de Eje PNMC; Rol depende de Tipo de agente. Si el padre está vacío, la lista hija está vacía. Cambiar el padre debe invalidar/limpiar el hijo. → En web: cascada reactiva.
4. **Multi-select toggle (macro).** En los campos `multi`, cada selección **agrega** el valor (`old & ", " & new`); volver a elegir un valor ya presente lo **quita** (comparación *case-insensitive*); vaciar limpia la celda. Separador exacto: **`", "`**. → En web: control multi-select; persistencia como filas hijas; serialización a celda con `", "`.
5. **Rol = dependiente + múltiple.** Actores!D combina ambas reglas: opciones filtradas por Tipo de agente y selección múltiple.
6. **Indicadores como catálogo fijo.** Las 7 filas de Indicadores (Acción/Nombre/Meta) son un catálogo predefinido no editable; el Líder solo captura avances mensuales, fuente, año, departamentos y observaciones. Lo mismo aplica a los 3 bloques de Detalle (Acción/Nombre/Meta/Fórmula/Descripción fijos).
7. **Cálculos de indicadores.** `Valor actual = MAX(avances)` si `Meta ≤ 1`, si no `SUM(avances)`; `% Cumplimiento = Valor actual / Meta` (0 si error). Detalle!H toma el Valor actual del indicador correspondiente por `INDEX/MATCH`.
8. **Obligatoriedad mínima.** Para materializar una ficha se requiere al menos **Fecha de levantamiento + Departamento** (regla ya presente en el import). El resto de hojas son opcionales/incrementales.
9. **Unicidad.** Ficha identificada por *(Departamento, Fecha de levantamiento)*; indicadores por *(Departamento, Indicador, Año)*; detalle por *(Departamento, Indicador, plantilla, Año)*.
10. **Formato numérico.** Valor de la propuesta = entero COP `≥0`; avances = decimal `≥0`; Meta con formato % en Detalle.

---

## 3. Validaciones y listas desplegables

### 3.1 Validaciones clásicas (por hoja)
- **Identificación:** A=fecha(rango), B=`Departamentos`, E=`RegionOCAD`, F=`FuenteInfo`.
- **Diagnóstico:** G=`EstadoComite`, H&J=`EstadoPlan`, I=inline(Existe/No existe/Por renovar), K=inline(Existe/No existe/Por activar).
- **Oportunidades:** F=`NivelAMB`.
- **Ejes PNMC:** C=`EjePNMC`, J=`NivelAMB`, M=`whole ≥0`, N=`Enfoques`, P=`Cronograma`, Q=`EstadoPropuesta`.
- **Actores:** C=`TiposAgente`, E=`NivelTerritorial`, F=`textLength 7–20`, G=`custom email`.
- **Indicadores:** A=`Departamentos`, D=`decimal ≥0`, E..AB(avances)=`decimal ≥0`, AF=`whole 2000–2100`.
- **Detalle:** A=`Departamentos`, G=`Meses`, J=`Años`.

### 3.2 Validaciones dependientes (x14 `INDIRECT`)
| Hoja | Celdas | Fórmula |
|------|--------|---------|
| Identificación | `C2:C51` | `INDIRECT(VLOOKUP($B2,Variables!$A$2:$O$34,15,FALSE))` |
| Ejes PNMC | `D2:D51` | `INDIRECT(VLOOKUP($C2,Variables!$BG$2:$BH$4,2,FALSE))` |
| Actores | `D2:D51` | `INDIRECT(VLOOKUP($C2,Variables!$BB$2:$BC$5,2,FALSE))` |

### 3.3 Catálogos (valores)
Resumen (detalle completo en Anexo A y en `docs/variables.md`):

| Rango | Valores |
|-------|---------|
| `Departamentos` | 33 departamentos (incluye Bogotá D.C.) |
| `RegionOCAD` | Caribe, Centro Oriente, Centro Sur, Eje Cafetero, Llanos, Pacífico |
| `EstadoComite` | Creado, Por crear, Inactivo |
| `EstadoPlan` | En ejecución, Por renovar, No existe |
| `NivelAMB` | Alto, Medio, Bajo |
| `EjePNMC` | 3 ejes (textos largos) |
| `Enfoques` | Diferencial, Biocultural, Derechos territoriales, Poblacional, Interseccional |
| `Cronograma` | 1 a 3 / 4 a 6 / 7 a 9 / 10 a 12 meses |
| `EstadoPropuesta` | Propuesta, En gestión, Implementación, Consolidada |
| `TiposAgente` | Institucional-Interno, Institucional-Externo, Sectorial, Comunitario y sociedad civil |
| `NivelTerritorial` | Local, Municipal, Departamental, Nacional, Internacional |
| `FuenteInfo` | Ente territorial, Aliado estratégico, Sector privado, Sociedad civil, Gobernación, CODEMUS, Mintrabajo, Otro |
| `Meses` | Enero…Diciembre |
| `Años` | 2024…2035 |
| `Ciu_<Depto>` × 33 | municipios por departamento |
| `Acto_Inst_Int/Ext/Sect/Comu` | roles por tipo de agente |
| `Comp_Eje_1/2/3` | componentes por eje |

> **Nota de integridad:** los catálogos deben sembrarse en `catalog_*` **exactamente** con estos textos (incluidos acentos y puntuación), porque el import valida por igualdad de cadena y el export debe reproducirlos idénticos para no romper las validaciones de la plantilla.

---

## 4. Fórmulas y cálculos a migrar

| Fórmula (Excel) | Ubicación | Estrategia web |
|-----------------|-----------|----------------|
| `=IF(Identificación!B=""," ",Identificación!B)` | col A de hojas 2–5 | **No se migra como cálculo**: el Departamento vive en el encabezado de la ficha. En export **no se escribe** (la fórmula de la plantilla lo rellena sola). |
| `IF(Meta<=1, MAX(avances), SUM(avances))` | Indicadores!AC | **Backend** (ya implementado en `ReplaceIndicatorsAsync`). Persistir `CurrentValueCalculated`. En export no se escribe AC (fórmula lo recalcula). |
| `IFERROR(AC/Meta,0)` | Indicadores!AD | **Backend**: `CompliancePercentageCalculated`. |
| `IFERROR(INDEX(Indicadores!AC, MATCH(C, Indicadores!C)),0)` | Detalle!H | **Backend** al consultar el detalle (mismo valor que el indicador). |
| `INDIRECT(VLOOKUP(...))` (listas dependientes) | validaciones x14 | **Backend/Frontend**: se resuelve con los mapas `Municipios×Depto`, `Componentes×Eje`, `Roles×TipoAgente` (ya existen como FKs en DB). |
| Macro multi-select `", "` | `Workbook_SheetChange` | **Frontend** (control multi) + **Backend** (filas hijas). En export se serializa uniendo con `", "`. |

> Regla de exportación derivada: **solo se escriben celdas de captura**. Las celdas `calc` se dejan con su fórmula original; al abrir el `.xlsm` Excel recalcula y muestra los mismos valores. Esto evita duplicar lógica y mantiene el archivo idéntico al oficial.

---

## 5. Mapeo Excel → Modelo de datos → UI

El modelo relacional y las entidades **ya existen** y encajan uno-a-uno con las hojas. Multi-selects y colecciones se modelan como tablas hijas.

| Hoja Excel | Entidad de dominio | Tabla(s) MySQL | Cardinalidad | Paso UI |
|------------|--------------------|----------------|--------------|---------|
| Identificación | `FichaDepartamental` (+ `FichaFuenteInformacion`) | `fichas_departamentales`, `ficha_fuentes_informacion` | 1 ficha (+N fuentes) | Paso 1 |
| Diagnóstico ecosistema | `DiagnosticoEcosistema` | `diagnosticos_ecosistema` | 1:1 | Paso 2 |
| Oportunidades de cambio | `OportunidadCambio` | `oportunidades_cambio` | 1:N | Paso 3 |
| Ejes PNMC | `EjePnmcRegistro` (+ `EjePnmcRegistroEnfoque`) | `ejes_pnmc_registros`, `ejes_pnmc_registro_enfoques` | 1:N (+N enfoques) | Paso 4 |
| Actores | `Actor` (+ `ActorRolEcosistema`, `ActorNivelTerritorial`) | `actores`, `actor_roles_ecosistema`, `actor_niveles_territoriales` | 1:N (+N roles/niveles) | Paso 5 |
| Indicadores | `IndicatorRecord` (+ `IndicatorMonthlyProgress`) | `indicator_records`, `indicator_monthly_progresses` | 1:N (12 meses) | Módulo Líder |
| Detalle Indicadores | `IndicatorDetailRecord` (+ `IndicatorDetailRecordMonth`) | `indicator_detail_records`, `indicator_detail_record_months` | 1:N | Módulo Líder |
| Variables | catálogos | `catalog_*` (22 tablas) | seed | Administración |

**Registro de mapeo de celdas (el corazón de la SSOT).** Se propone un artefacto declarativo — *ejemplo conceptual* (no es código final):

```jsonc
// FichaBlueprint (versionado; derivado del .xlsm)
{
  "sheet": "Identificación", "table": "Identificacion", "range": "A1:G51",
  "role": "GestorDepartamental", "kind": "single-row",
  "fields": [
    { "col": "A", "key": "fechaLevantamiento", "type": "date",
      "min": "2000-01-01", "max": "2100-12-31", "editable": true },
    { "col": "B", "key": "departamento", "type": "list", "catalog": "Departamentos" },
    { "col": "C", "key": "ciudad", "type": "list", "dependsOn": "B",
      "catalog": "Ciu_{B}" },
    { "col": "F", "key": "fuentes", "type": "multi", "catalog": "FuenteInfo",
      "separator": ", " }
    // ...
  ]
}
```

De este registro se derivan: columnas de import (col→propiedad), celdas de export (propiedad→col), controles del formulario, y reglas FluentValidation. **Un solo lugar que cambiar cuando cambie el Excel.**

---

## 6. Estrategia de importación

Base ya implementada en `WorkbookImportService` (staging + validación + upsert). Se conserva el enfoque y se refina:

1. **Lectura tolerante** de las 7 hojas por posición de columna (según Blueprint), filas 2–51 (3–9 en Indicadores, 2–14 en Detalle). Lectura con ClosedXML (solo lectura; no necesita preservar macros).
2. **Staging** en `import_*` con `import_batches`; nada toca las tablas finales hasta validar.
3. **Validación por celda** contra catálogos (snapshot en memoria) y contra las reglas del Excel:
   - Listas simples y dependientes (Ciudad×Depto, Componente×Eje, Rol×TipoAgente).
   - Multi-select: separar por `", "` y validar cada token (warning si token desconocido).
   - Fecha, `Año 2000–2100`, `Valor ≥ 0`, **teléfono 7–20** (ajustar), **email con `LEN>=5`** (ajustar).
   - Errores vs. advertencias: los **Error** bloquean la persistencia de la ficha; los **Warning** se registran y continúan.
4. **Reporte** de inconsistencias (`import_validation_issues`) con hoja, fila, celda, código y valor crudo → se muestra al usuario antes de confirmar.
5. **Upsert idempotente** por claves de unicidad (evita duplicados; permite re-importar y actualizar). Colecciones (oportunidades, ejes, actores, meses) se reemplazan por ficha.
6. **Auditoría** del lote y de los cambios resultantes.

Ajustes concretos: (a) alinear teléfono/email a las reglas exactas del Excel; (b) leer multi-selects usando **exactamente** `", "`; (c) tomar las columnas desde el Blueprint en lugar de índices mágicos.

---

## 7. Estrategia de exportación preservando el `.xlsm`

### 7.1 Limitación técnica y decisión
- **ClosedXML no sirve para exportar** el `.xlsm`: al guardar no round-trip-ea `vbaProject.bin` (pierde macros) y escribe formato `.xlsx`. El `ExcelExportService` actual, además, reconstruye el libro desde cero.
- **Decisión:** usar **`DocumentFormat.OpenXml`** (ya referenciado en el proyecto de tests) para **abrir la plantilla oficial y sobrescribir solo celdas de datos**, dejando intactos VBA, validaciones (clásicas y x14), estilos, formatos, celdas protegidas, hoja Variables, modelo y webextensions. Es 100% manejado (sin Excel/Interop) → compatible con IIS/Plesk.

### 7.2 Procedimiento (por ficha)
1. **Cargar** una copia en memoria de la plantilla oficial `ficha_departamental_gobernanza.xlsm` (bundled como *embedded resource* o archivo de contenido desplegado; hoy **no** está en `src/`, hay que incluirla).
2. Abrir con `SpreadsheetDocument.Open(stream, isEditable:true)`.
3. Por cada hoja/entidad, según el **Blueprint**, escribir **solo las celdas de captura**:
   - Fechas → valor numérico serial (conserva el estilo dd/mm/aaaa de la plantilla).
   - Números → celda numérica.
   - Texto/listas → `inlineStr` (evita tocar `sharedStrings.xml`).
   - Multi-selects → una sola cadena unida con `", "`.
   - Filas de colección desde la fila 2 (3 en Indicadores) hacia abajo, respetando el máximo de filas de la plantilla.
   - **No** escribir columnas `calc` (Departamento heredado, AC, AD, H): sus fórmulas se preservan y recalculan solas.
4. Marcar el libro para **recálculo completo** al abrir (`CalculationProperties.FullCalcOnLoad = true`) para que AC/AD/H y el Departamento heredado se actualicen.
5. Guardar y **descargar como `.xlsm`** (content-type `application/vnd.ms-excel.sheet.macroEnabled.12`).

### 7.3 Plantilla en blanco para diligenciamiento offline
Reemplazar la salida de `ImportTemplateService` por **la misma plantilla oficial `.xlsm`** servida en blanco. Opcionalmente, si el Administrador edita catálogos, sincronizar **solo el rango de datos de la hoja Variables** con el mismo escritor OpenXML (preservando todo lo demás). Así, offline y web usan literalmente el mismo formato y macros.

### 7.4 Garantías
Round-trip verificable: exportar → abrir en Excel → las macros multi-select funcionan, las listas dependientes filtran, los cálculos cuadran, y volver a importar reproduce los mismos datos (mismo separador `", "`, mismos catálogos).

---

## 8. Propuesta de arquitectura y plan por fases

### 8.1 Arquitectura objetivo
- **SSOT — `FichaBlueprint`** (Shared): metadatos derivados del Excel (hojas, campos, columnas, tipos, validaciones, catálogos, editable-por, multi/dependiente). Versionado (`blueprintVersion`) para acompañar cambios del Excel.
- **Backend (.NET 9, Clean Architecture):**
  - `Application`: contratos + interfaces (ya existen `IGovernanceFichaService`, `IWorkbookImportService`, `IExcelExportService`).
  - `Infrastructure`:
    - `Excel/` — **nuevo** `OpenXmlTemplateWriter` (export) que consume el Blueprint + la plantilla oficial.
    - `Imports/` — refinar `WorkbookImportService` para leer por Blueprint.
    - Endpoint `GET /api/governance/blueprint` para servir la SSOT al frontend.
  - Validación con **FluentValidation** generada desde el Blueprint.
- **Frontend (Angular 20 standalone + signals + Reactive Forms):**
  - Feature `governance/` con un **renderizador de formulario dinámico** que construye `FormGroup`/`FormArray` a partir del Blueprint (stepper por hoja). Controles: fecha, texto, select, select-dependiente, multi-select, tabla editable (colecciones).
  - RBAC por `role.guard`: Gestor ve hojas 1–5 editables e Indicadores/Detalle solo lectura; Líder al revés.
- **Persistencia (MySQL):** modelo relacional ya creado (`fichas_departamentales`, hijas, `catalog_*`, `import_*`, `security_*`, `audit_logs`, `approval_records`). Scripts SQL versionados; **el usuario aplica los scripts** (no ejecutar migraciones automáticamente).

### 8.2 Plan por fases (cada fase requiere aprobación antes de continuar)

| Fase | Entregable | Estado previo |
|------|-----------|---------------|
| **0. Diseño** (este documento) | Análisis + arquitectura | ✅ Hecho |
| **1. Blueprint SSOT** | `FichaBlueprint` + endpoint `GET /api/governance/blueprint` + `FichaBlueprintParityTests` (7 pruebas contra el `.xlsm`) | ✅ Hecho |
| **2. Catálogos** | Paridad seed `catalog_*` == Variables verificada por `CatalogSeedParityTests` (17 casos); lookup `years` cableado (`GET /catalogs/lookups/years`) | ✅ Hecho (núcleo) |
| **3. Export fiel `.xlsm`** | `OpenXmlFichaWriter` (preserva VBA/validaciones/estilos/fórmulas) + plantilla embebida + `ExcelExportService` reescrito + `OpenXmlFichaWriterTests` (7 pruebas round-trip) | ✅ Hecho (5 hojas Gestor; Indicadores/Detalle con el módulo Líder) |
| **4. Plantilla en blanco** | `ImportTemplateService` ahora sirve el `.xlsm` oficial en blanco (vía `FichaTemplateProvider`); `ImportTemplateServiceTests` confirma paridad byte a byte | ✅ Hecho |
| **5. Import por Blueprint** | Validaciones alineadas al Excel con límites del Blueprint: teléfono 7–20 y email `LEN≥5` (`ActorContactValidation` + 15 pruebas); separador `", "` confirmado. Lectura de columnas por Blueprint: refactor opcional diferido (la estructura ya la protege el test de paridad) | ✅ Hecho (núcleo) |
| **6. Formulario web + RBAC** | El stepper (5 hojas, cascadas, multi-select, workflow) **ya existía**. Fase 6 añadió: RBAC (Gestor/Admin editan; Líder/Admin revisan), modo solo lectura para no-editores, validadores del Excel (correo `@`/`.`/≥5, teléfono 7–20, valor ≥0) con errores en línea, y corrigió el nombre de descarga a `.xlsm`. Build Angular OK | ✅ Hecho |
| **7. Workflow + auditoría** | Workflow (aprobar/rechazar + `approval_records`) ya existía. Fase 7 completó la auditoría de cambios: **IP del cliente** (`ip_address`, script `008`), captura de **valor anterior** en diagnóstico/oportunidades/ejes/actores (antes `old=null`), y `GovernanceAuditTests` (EF InMemory) | ✅ Hecho |
| **8. Pruebas E2E + endurecimiento** | `ExcelRoundTripTests`: ClosedXML abre el `.xlsm` oficial y el export re-lee con los mismos valores/celdas (multi ", ", fechas, acentos). Checklist de endurecimiento/despliegue: [checklist-endurecimiento-despliegue.md](checklist-endurecimiento-despliegue.md) | ✅ Hecho (E2E + checklist; rate-limiting/headers documentados como pendientes) |

### 8.3 Riesgos y mitigaciones
- **Deriva Excel↔plataforma:** mitiga el Blueprint + tests de paridad que fallan si el `.xlsm` cambia.
- **Pérdida de macros en export:** mitiga OpenXML sobre plantilla + prueba automatizada que verifica presencia de `vbaProject.bin` y validaciones tras exportar.
- **Textos de catálogo con acentos/puntuación:** seed idéntico byte-a-byte al Excel; validación por igualdad exacta.
- **Rangos con celdas vacías** (p. ej. `Departamentos` 34 celdas/33 valores): tolerar vacíos al leer.

---

## Anexo A — Rangos con nombre (inventario)

**Catálogos (hoja Variables):** `Departamentos` (A2:A35), `RegionOCAD` (B2:B7), `EstadoComite` (C2:C4), `EstadoPlan` (D2:D4), `NivelAMB` (E2:E4), `EjePNMC` (F2:F5), `ComponentePNMC` (G2:G7, *legacy*), `Enfoques` (H2:H6), `Cronograma` (I2:I5), `EstadoPropuesta` (J2:J5), `TipoAgente` (K2:K5, *legacy*), `NivelTerritorial` (L2:L6), `FuenteInfo` (M2:M9), `Meses` (N2:N13), `TiposAgente` (AW2:AW5), `Acto_Inst_Int` (AX2:AX8), `Acto_Inst_Ext` (AY2:AY10), `Acto_Sect` (AZ2:AZ14), `Acto_Comu` (BA2:BA9), `Comp_Eje_1` (BD2:BD3), `Comp_Eje_2` (BE2:BE7), `Comp_Eje_3` (BF2:BF3), `Años` (BI2:BI13), `Ciu_<Depto>` × 33 (P:AV).

**Auxiliares de dependencia:** col `O` (Depto→Ciu_), `BB:BC` (TipoAgente→Acto_), `BG:BH` (Eje→Comp_).

**Rangos de multi-select (macro):** `Multi_Fuente` (Identificación!F2:F51), `Multi_Enfoques` (Ejes!N2:N51), `Multi_NivelTerritorial` (Actores!E2:E51), `Multi_ActorEspec` (Actores!D2:D51), `Multi_DeptInd` (Indicadores!A3:A9), `Multi_MesesDet` (Detalle!G2:G14), `Multi_DeptDet_1/2/3` (Detalle!A2/A7/A11).

**A ignorar (modelo Power Query heredado, preservar en export):** `_xlcn.WorksheetConnection_IFCDFramework2018.xlsx*` (Activities/Objectives/Outputs).

## Anexo B — Macro VBA (única)

`ThisWorkbook.Workbook_SheetChange`: para celdas dentro de los rangos `Multi_*`, implementa selección múltiple *toggle* con separador `", "` (agrega; si el valor ya está, lo quita; *case-insensitive*; una sola celda a la vez). Los módulos `Hoja1…Hoja8` están **vacíos**. **No hay cálculos en VBA** — todos son fórmulas de hoja. La macro solo es necesaria para la experiencia dentro de Excel; en web se replica con controles multi-select y persistencia en tablas hijas.

## Anexo C — Reconciliación con documentos previos

`estructura-archivo-ficha-departamental.md` y `variables.md` son útiles pero **parcialmente inferidos**. Correcciones verificadas contra el archivo: (1) las listas dependientes son validaciones **x14 INDIRECT**, no clásicas; (2) `Rol` (Actores!D) es dependiente **y** multi-select; (3) el separador multi-select es **`", "`** (confirmado en la macro); (4) `Departamentos` tiene 33 valores (rango de 34 celdas); (5) Indicadores tiene **7** filas de datos (3–9). Ante cualquier conflicto, **manda el `.xlsm`**.
