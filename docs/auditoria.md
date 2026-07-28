# Historial de auditoría

Documento de referencia de la pestaña **Auditoría** de `/administration`. Describe qué acciones
quedan registradas, qué guarda cada registro, quién puede consultarlo y cómo se lee.

---

## 1. Qué se registra

Se registra **toda acción que cambia datos**, además de la entrada al portal y las descargas de
información. Las consultas de pantalla (abrir una ficha, listar usuarios) **no** se registran: son
lecturas y llenarían el historial de ruido sin aportar trazabilidad.

| Módulo | Acciones registradas |
|---|---|
| **Autenticación** | Ingreso al portal · Intento de ingreso fallido · Cambio de contraseña propia |
| **Seguridad** | Crear usuario · Actualizar usuario (correo, nombre, estado, roles) · Restablecer contraseña |
| **Catálogos** | Crear valor · Actualizar valor · Desactivar valor (en los 16 catálogos) |
| **Gobernanza** | Crear ficha · Actualizar identificación · Actualizar diagnóstico del ecosistema · Actualizar oportunidades de cambio · Actualizar ejes PNMC · Actualizar actores · Eliminar ficha |
| **Importaciones** | Cargar archivo (con su resultado) · Eliminar carga del historial |
| **Aprobaciones** | Aprobar ficha · Devolver ficha |
| **Reportes** | Descargar ficha en Excel |

> El **cierre de sesión** no aparece: se resuelve en el navegador y no llega al servidor, así que no
> hay nada que registrar.

Un **intento de ingreso fallido** queda a nombre del correo que se escribió, aunque no corresponda a
ningún usuario: justamente eso es lo que interesa revisar. El motivo se guarda en la descripción,
pero al usuario que intenta entrar nunca se le dice si falló el correo o la contraseña.

---

## 2. Qué guarda cada registro

| Dato | Para qué sirve |
|---|---|
| **Fecha y hora** | Cuándo ocurrió (UTC en la base, hora local en pantalla). |
| **Usuario** | Nombre y correo de quien la ejecutó. |
| **Roles** | Con qué roles actuaba en ese momento. |
| **Dirección IP** | Desde dónde se conectó. |
| **Módulo** | En qué parte del portal ocurrió. |
| **Acción** | Qué hizo, en palabras ("Devolver ficha"). |
| **Objeto afectado** | Sobre qué recayó, con nombre propio: "ficha de Antioquia · 15/03/2026", "Municipios · Medellín". |
| **Descripción** | La acción redactada completa, para leerla sin abrir el detalle. |
| **Resultado** | *Exitoso* o *Fallido*. |
| **Cambios** | Campo a campo: etiqueta, valor anterior y valor nuevo. |
| **Petición** | Verbo y ruta HTTP, como respaldo técnico. |
| **Registro completo** | El objeto antes y después en JSON, para soporte. |

### Cómo se muestran los valores

Los valores se guardan tal como los ve el usuario, no como los guarda la base de datos:

- Los identificadores de catálogo se resuelven a su **nombre** (departamento, municipio, región,
  estados, fuentes de información).
- Los booleanos se muestran como **Sí / No**; las fechas como **dd/mm/aaaa**; las listas separadas
  por coma.
- Un campo vacío se muestra como **(vacío)**.
- Las contraseñas se registran como **(oculto)**: queda constancia de que cambiaron, nunca su valor.
  La contraseña temporal que genera un restablecimiento **no** se guarda; se entrega en pantalla una
  sola vez.
- Los textos muy largos se recortan a 600 caracteres en la vista de cambios; el contenido íntegro
  queda en el registro completo en JSON.

Solo se listan los campos que **realmente** cambiaron. Si se guarda una sección sin tocar nada, el
registro lo dice: *"Guardó … sin cambios"*.

Las secciones de lista (oportunidades, ejes, actores) registran cuántos registros quedaron
(*"pasó de 2 a 3 registros"*) y guardan el contenido completo de antes y después en el detalle: un
diff campo a campo de una lista entera no se podría leer.

---

## 3. Quién lo puede consultar

Solo el **Administrador** y el **Líder de Gobernanza**. El historial expone correos, direcciones IP
y los valores anteriores y nuevos de cada cambio, así que el resto de roles no tiene acceso al
endpoint.

---

## 4. Cómo se consulta

La pestaña permite filtrar por **texto libre** (usuario, acción, objeto afectado o descripción),
**módulo**, **usuario**, **acción** y **rango de fechas**. Los filtros se combinan y se conservan al
cambiar de página. El "hasta" incluye el día completo seleccionado.

Las listas de módulos, acciones y usuarios se arman con lo que ya existe en el historial, no con
un catálogo fijo: solo se ofrece filtrar por lo que de verdad hay.

Cada fila se despliega para ver la descripción, la tabla de cambios (antes tachado en rojo, después
en verde), los datos técnicos y el registro completo.

---

## 5. Registros anteriores a este cambio

Las acciones que se guardaron antes del historial detallado no tienen módulo, descripción ni
detalle de cambios. La migración les asigna el módulo deducido del tipo de objeto
(`FichaDepartamental` → Gobernanza, `UserAccount` → Seguridad) y el resto queda como
*Sin clasificar*. Se siguen viendo, con la información que tenían: usuario, fecha, entidad y
operación.

---

## 6. Dónde queda almacenado

Todo en la tabla `audit_logs`. La migración `database/schema/013_auditoria_detallada.sql` agrega las
columnas nuevas (`module`, `entity_key`, `entity_label`, `description`, `result`, `changes_json`,
`request_method`, `request_path`, `user_roles`), permite que `entity_id` sea nulo —hay acciones sobre
objetos que no se identifican con un GUID, como los catálogos, y otras que no recaen sobre ningún
registro, como un intento de ingreso fallido— y crea los índices por fecha, módulo y usuario con los
que se consulta la pantalla.
