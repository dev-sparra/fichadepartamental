{
  "archivo": {
    "nombre": "ficha_departamental_gobernanza.xlsm",
    "proteccion": "Todas las hojas están protegidas con contraseña (clave: gobernanza2026). Las celdas marcadas locked=true no son editables aunque se desproteja la hoja para lectura; las locked=false son los campos de diligenciamiento."
  },
  "hojas": {
    "Identificación": {
      "tabla": "Identificacion",
      "rango": "A1:G51",
      "filaEncabezado": 1,
      "campos": [
        {"col":"A","nombre":"Fecha de levantamiento","editable":true,"tipoDato":"fecha","validacion":null},
        {"col":"B","nombre":"Departamento","editable":true,"tipoDato":"lista","validacion":{"tipo":"lista_fija","fuente":"Departamentos"}},
        {"col":"C","nombre":"Ciudad","editable":true,"tipoDato":"lista_dependiente","validacion":{"tipo":"lista_dependiente","dependeDe":"B (Departamento)","formula":"=INDIRECT(VLOOKUP($B2,Variables!$A$2:$O$34,15,FALSE))","nota":"Cada departamento resuelve a una lista de municipios nombrada Ciu_<Departamento> en Variables"}},
        {"col":"D","nombre":"Responsable del registro (Gestor)","editable":true,"tipoDato":"texto","validacion":null},
        {"col":"E","nombre":"Región OCAD","editable":true,"tipoDato":"lista","validacion":{"tipo":"lista_fija","fuente":"RegionOCAD"}},
        {"col":"F","nombre":"Fuente de información","editable":true,"tipoDato":"lista_multiple","validacion":{"tipo":"lista_fija","fuente":"FuenteInfo","multiSeleccion":true}},
        {"col":"G","nombre":"Observaciones","editable":true,"tipoDato":"texto","validacion":null}
      ]
    },
    "Diagnóstico ecosistema": {
      "tabla": "Diagnostico",
      "rango": "A1:N51",
      "campos": [
        {"col":"A","nombre":"Departamento","editable":false,"tipoDato":"calculado","formula":"=IF(Identificación!$B2=\"\",\"\",Identificación!$B2)","nota":"Se autocompleta desde la hoja Identificación, no editable"},
        {"col":"B","nombre":"Caracterización general del ecosistema musical","editable":true,"tipoDato":"texto"},
        {"col":"C","nombre":"Fortalezas identificadas","editable":true,"tipoDato":"texto"},
        {"col":"D","nombre":"Políticas priorizadas","editable":true,"tipoDato":"texto"},
        {"col":"E","nombre":"Debilidades identificadas","editable":true,"tipoDato":"texto"},
        {"col":"F","nombre":"Tensiones o conflictos","editable":true,"tipoDato":"texto"},
        {"col":"G","nombre":"CODEMUS – Comité Dptal de Música","editable":true,"tipoDato":"lista","validacion":{"fuente":"EstadoComite"}},
        {"col":"H","nombre":"Plan Departamental de Cultura","editable":true,"tipoDato":"lista","validacion":{"fuente":"EstadoPlan"}},
        {"col":"I","nombre":"Consejo Departamental de Cultura","editable":true,"tipoDato":"lista","validacion":{"tipo":"lista_inline","valores":["Existe","No existe","Por renovar"]}},
        {"col":"J","nombre":"Plan Departamental de Música","editable":true,"tipoDato":"lista","validacion":{"fuente":"EstadoPlan"}},
        {"col":"K","nombre":"Ordenanzas Culturales","editable":true,"tipoDato":"lista","validacion":{"tipo":"lista_inline","valores":["Existe","No existe","Por activar"]}},
        {"col":"L","nombre":"Consejo Departamental de Música","editable":true,"tipoDato":"texto"},
        {"col":"M","nombre":"Mesa sectorial o territorial identificada","editable":true,"tipoDato":"texto"},
        {"col":"N","nombre":"Observaciones","editable":true,"tipoDato":"texto"}
      ]
    },
    "Oportunidades de cambio": {
      "tabla": "Oportunidades",
      "rango": "A1:G51",
      "campos": [
        {"col":"A","nombre":"Departamento","editable":false,"tipoDato":"calculado","nota":"Vinculado desde Identificación!B"},
        {"col":"B","nombre":"Situación identificada","editable":true,"tipoDato":"texto"},
        {"col":"C","nombre":"Componente PNMC - Otras dependencias / Entidades","editable":true,"tipoDato":"texto"},
        {"col":"D","nombre":"Aliados y creyentes","editable":true,"tipoDato":"texto"},
        {"col":"E","nombre":"Territorio de influencia","editable":true,"tipoDato":"texto"},
        {"col":"F","nombre":"Nivel de impacto","editable":true,"tipoDato":"lista","validacion":{"fuente":"NivelAMB"}},
        {"col":"G","nombre":"Descripción adicional","editable":true,"tipoDato":"texto"}
      ]
    },
    "Ejes PNMC": {
      "tabla": "EjesPNMC",
      "rango": "A1:R51",
      "campos": [
        {"col":"A","nombre":"Departamento","editable":false,"tipoDato":"calculado","nota":"Vinculado desde Identificación!B"},
        {"col":"B","nombre":"Descripción hallazgo","editable":true,"tipoDato":"texto"},
        {"col":"C","nombre":"Eje PNMC","editable":true,"tipoDato":"lista","validacion":{"fuente":"EjePNMC"}},
        {"col":"D","nombre":"Componente PNMC","editable":true,"tipoDato":"lista_dependiente","validacion":{"dependeDe":"C (Eje PNMC)","formula":"=INDIRECT(VLOOKUP($C2,Variables!$BG$2:$BH$4,2,FALSE))","nota":"Resuelve a Comp_Eje_1 / Comp_Eje_2 / Comp_Eje_3 según el eje"}},
        {"col":"E","nombre":"Acción Estratégica","editable":true,"tipoDato":"texto"},
        {"col":"F","nombre":"Política priorizada","editable":true,"tipoDato":"texto"},
        {"col":"G","nombre":"Armonización PNC","editable":true,"tipoDato":"texto"},
        {"col":"H","nombre":"Armonización PND","editable":true,"tipoDato":"texto"},
        {"col":"I","nombre":"Armonización Internacional","editable":true,"tipoDato":"texto"},
        {"col":"J","nombre":"Nivel prioridad","editable":true,"tipoDato":"lista","validacion":{"fuente":"NivelAMB"}},
        {"col":"K","nombre":"Aliados / Responsables","editable":true,"tipoDato":"texto"},
        {"col":"L","nombre":"Fuentes de financiación","editable":true,"tipoDato":"texto"},
        {"col":"M","nombre":"Valor de la propuesta (COP)","editable":true,"tipoDato":"numero_entero"},
        {"col":"N","nombre":"Enfoques","editable":true,"tipoDato":"lista_multiple","validacion":{"fuente":"Enfoques","multiSeleccion":true}},
        {"col":"O","nombre":"Descripción","editable":true,"tipoDato":"texto"},
        {"col":"P","nombre":"Cronograma","editable":true,"tipoDato":"lista","validacion":{"fuente":"Cronograma"}},
        {"col":"Q","nombre":"Estado","editable":true,"tipoDato":"lista","validacion":{"fuente":"EstadoPropuesta"}},
        {"col":"R","nombre":"Observaciones","editable":true,"tipoDato":"texto"}
      ]
    },
    "Actores": {
      "tabla": "Actores",
      "rango": "A1:H51",
      "campos": [
        {"col":"A","nombre":"Departamento","editable":false,"tipoDato":"calculado","nota":"Vinculado desde Identificación!B"},
        {"col":"B","nombre":"Nombre del agente (creyente)","editable":true,"tipoDato":"texto"},
        {"col":"C","nombre":"Tipo de agente (categoría)","editable":true,"tipoDato":"lista","validacion":{"fuente":"TiposAgente"}},
        {"col":"D","nombre":"Rol en el ecosistema","editable":true,"tipoDato":"lista_dependiente","validacion":{"dependeDe":"C (Tipo de agente)","formula":"=INDIRECT(VLOOKUP($C2,Variables!$BB$2:$BC$5,2,FALSE))","nota":"Resuelve a Acto_Inst_Int / Acto_Inst_Ext / Acto_Sect / Acto_Comu"}},
        {"col":"E","nombre":"Nivel territorial","editable":true,"tipoDato":"lista_multiple","validacion":{"fuente":"NivelTerritorial","multiSeleccion":true}},
        {"col":"F","nombre":"Número de contacto","editable":true,"tipoDato":"texto_validado","validacion":{"tipo":"longitud_texto"}},
        {"col":"G","nombre":"Correo electrónico","editable":true,"tipoDato":"texto_validado","validacion":{"tipo":"personalizada_formato_email"}},
        {"col":"H","nombre":"Observaciones","editable":true,"tipoDato":"texto"}
      ]
    },
    "Indicadores": {
      "tabla": "Indicadores",
      "rango": "A2:AG9",
      "filaEncabezado": "2 (con agrupador de meses en fila 1)",
      "nota": "8 filas fijas, una por indicador (catálogo predefinido no editable en B,C,D)",
      "campos": [
        {"col":"A","nombre":"Departamento","editable":true,"tipoDato":"lista","validacion":{"fuente":"Departamentos"}},
        {"col":"B","nombre":"Acción","editable":false,"tipoDato":"texto_fijo","nota":"Catálogo predefinido de 7 acciones, no editable"},
        {"col":"C","nombre":"Nombre Indicador","editable":false,"tipoDato":"texto_fijo"},
        {"col":"D","nombre":"Meta","editable":false,"tipoDato":"numero_fijo"},
        {"col":"E-AB","nombre":"Avance cuantitativo / Detalle (x12 meses: Enero..Diciembre)","editable":true,"tipoDato":"par numero+texto por mes","validacion":{"numero":"decimal"}},
        {"col":"AC","nombre":"Valor actual (calc.)","editable":false,"tipoDato":"calculado","formula":"MAX o SUM de los 12 avances según si Meta<=1"},
        {"col":"AD","nombre":"% Cumplimiento","editable":false,"tipoDato":"calculado","formula":"=IFERROR([@[Valor actual (calc.)]]/[@Meta],0)"},
        {"col":"AE","nombre":"Fuente","editable":true,"tipoDato":"texto"},
        {"col":"AF","nombre":"Año","editable":true,"tipoDato":"numero_entero_validado"},
        {"col":"AG","nombre":"Observaciones generales","editable":true,"tipoDato":"texto"}
      ]
    },
    "Detalle Indicadores": {
      "tabla": null,
      "rango": "A1:K14",
      "nota": "Ficha detallada por indicador (3 filas fijas: 2, 7, 11), sin tabla nativa de Excel",
      "campos": [
        {"col":"A","nombre":"Departamento","editable":true,"tipoDato":"lista","validacion":{"fuente":"Departamentos"}},
        {"col":"B","nombre":"Acción","editable":false,"tipoDato":"texto_fijo"},
        {"col":"C","nombre":"Nombre","editable":false,"tipoDato":"texto_fijo"},
        {"col":"D","nombre":"Meta","editable":false,"tipoDato":"numero_fijo","formato":"%"},
        {"col":"E","nombre":"Fórmula de cálculo","editable":false,"tipoDato":"texto_fijo","nota":"Desglose de subcriterios y su peso %"},
        {"col":"F","nombre":"Descripción / detalle","editable":false,"tipoDato":"texto_fijo"},
        {"col":"G","nombre":"MESES","editable":true,"tipoDato":"lista","validacion":{"fuente":"Meses"}},
        {"col":"H","nombre":"Valor actual (calc.)","editable":false,"tipoDato":"calculado","formula":"=IFERROR(INDEX(Indicadores!$AC$3:$AC$9,MATCH($C2,Indicadores!$C$3:$C$9,0)),0)"},
        {"col":"I","nombre":"Fuente","editable":true,"tipoDato":"texto"},
        {"col":"J","nombre":"Año","editable":true,"tipoDato":"lista","validacion":{"fuente":"Años"}},
        {"col":"K","nombre":"Observaciones","editable":true,"tipoDato":"texto"}
      ]
    }
  },
  "catalogos_Variables": {
    "nota": "Hoja 'Variables' (protegida) contiene los rangos con nombre usados por las validaciones de lista de todas las hojas.",
    "Departamentos": ["Amazonas","Antioquia","Arauca","Atlántico","Bogotá D.C.","Bolívar","Boyacá","Caldas","Caquetá","Casanare","Cauca","Cesar","Chocó","Córdoba","Cundinamarca","Guainía","Guaviare","Huila","La Guajira","Magdalena","Meta","Nariño","Norte de Santander","Putumayo","Quindío","Risaralda","San Andrés y Providencia","Santander","Sucre","Tolima","Valle del Cauca","Vaupés","Vichada"],
    "RegionOCAD": ["Caribe","Centro Oriente","Centro Sur","Eje Cafetero","Llanos","Pacífico"],
    "EstadoComite": ["Creado","Por crear","Inactivo"],
    "EstadoPlan": ["En ejecución","Por renovar","No existe"],
    "NivelAMB": ["Alto","Medio","Bajo"],
    "EjePNMC": ["1. Música para la vida, el diálogo intercultural y la diversidad biocultural.","2. Fortalecimiento de las prácticas, expresiones y oficios de la música.","3. Gobernanza musical e integración cultural e intersectorial."],
    "Enfoques": ["Diferencial","Biocultural","Derechos territoriales","Poblacional","Interseccional"],
    "Cronograma": ["1 a 3 meses","4 a 6 meses","7 a 9 meses","10 a 12 meses"],
    "EstadoPropuesta": ["Propuesta","En gestión","Implementación","Consolidada"],
    "TiposAgente": ["Institucional - Agente Interno","Institucional - Agente Externo","Sectorial","Comunitario y sociedad civil"],
    "NivelTerritorial": ["Local","Municipal","Departamental","Nacional","Internacional"],
    "FuenteInfo": ["Ente territorial","Aliado estratégico","Sector privado","Sociedad civil","Gobernación","CODEMUS","Mintrabajo","Otro"],
    "Meses": ["Enero","Febrero","Marzo","Abril","Mayo","Junio","Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre"],
    "Años": [2024,2025,2026,2027,2028,2029,2030,2031,2032,2033,2034,2035],
    "mapa_Rol_por_TipoAgente": {
      "Institucional - Agente Interno": ["Grupo de Música","Alcaldías","Creadores(as) y compositores(as)","Asociaciones y sindicatos de músicos", "... (lista completa en columna Acto_Inst_Int)"],
      "Institucional - Agente Externo": ["Grupo de Gobernanza y Políticas Públicas","Gobernaciones","Intérpretes y agrupaciones","Organizaciones sin ánimo de lucro","... (columna Acto_Inst_Ext)"],
      "Sectorial": ["Fomento Regional","Secretarías Locales de Cultura","Docentes y formadores(as)","Asociaciones de padres de familia","... (columna Acto_Sect)"],
      "Comunitario y sociedad civil": ["D.E.D.E.","Ministerio del Trabajo","Investigadores(as) y musicólogos(as)","Organizadores de festivales y eventos comunitarios","... (columna Acto_Comu)"]
    },
    "mapa_Componente_por_Eje": {
      "1. Música para la vida, el diálogo intercultural y la diversidad biocultural.": ["Apropiación de la música y de los derechos culturales.","Enfoque poblacional y cultura de paz."],
      "2. Fortalecimiento de las prácticas, expresiones y oficios de la música.": ["Formación.","Creación y producción.","Circulación.","Dotación e infraestructura."],
      "3. Gobernanza musical e integración cultural e intersectorial.": ["Participación ciudadana, intersectorialidad y articulación territorial.","Sostenibilidad, condiciones laborales y economías de la música."]
    },
    "Ciudades_por_departamento": {
      "nota": "Cada uno de los 33 departamentos tiene su propia lista de municipios en Variables, columnas P:AW (nombradas Ciu_<Departamento>), con entre 8 y 125 municipios cada una. Por el volumen (>1000 valores en total) no se incluyen aquí en detalle — puedo exportarlas completas en un archivo aparte si las necesitas."
    }
  }
}