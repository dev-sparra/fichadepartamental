using PortalNacionalGobernanzaMusical.Shared.Constants;

namespace PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;

/// <summary>
/// Instancia canónica del Blueprint. Refleja exactamente la estructura verificada del
/// archivo oficial <c>ficha_departamental_gobernanza.xlsm</c> (hojas, columnas, tipos,
/// validaciones, catálogos, listas dependientes y selección múltiple). Es la fuente única
/// de verdad para formulario, validación, importación y exportación.
///
/// <para>La correspondencia con el archivo se verifica automáticamente en
/// <c>FichaBlueprintParityTests</c> (falla si el Excel y este Blueprint divergen).</para>
/// </summary>
public sealed class FichaBlueprintProvider : IFichaBlueprintProvider
{
    public const string BlueprintVersion = "1.0.0";
    public const string MultiSeparator = ", ";

    private static readonly string[] Months =
    [
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    ];

    private static readonly FichaBlueprint Instance = Build();

    public FichaBlueprint GetBlueprint() => Instance;

    private static FichaBlueprint Build() => new()
    {
        Version = BlueprintVersion,
        SourceWorkbook = "ficha_departamental_gobernanza.xlsm",
        MultiSelectSeparator = MultiSeparator,
        Sheets =
        [
            Identificacion(),
            Diagnostico(),
            Oportunidades(),
            EjesPnmc(),
            Actores(),
            Indicadores(),
            DetalleIndicadores()
        ]
    };

    // ----------------------------------------------------------------- Hojas del Gestor

    private static BlueprintSheet Identificacion() => new()
    {
        Name = "Identificación",
        Key = "identificacion",
        Table = "Identificacion",
        Range = "A1:G51",
        HeaderRow = 1,
        DataStartRow = 2,
        DataEndRow = 51,
        Kind = BlueprintSheetKinds.SingleRow,
        EditableByRole = Roles.GestorDepartamental,
        Fields =
        [
            DateField("A", "fechaLevantamiento", "Fecha de levantamiento", "2000-01-01", "2100-12-31"),
            Lst("B", "departmentId", "Departamento", "departments", "Departamentos",
                v: new BlueprintValidation { Required = true, Message = "Selecciona un departamento de la lista." }),
            Dependent("C", "municipalityId", "Ciudad", "B", BlueprintCascades.MunicipalitiesByDepartment,
                prompt: "Lista filtrada según el Departamento."),
            Text("D", "responsableRegistro", "Responsable del registro (Gestor)"),
            Lst("E", "regionOcadOptionId", "Región OCAD", "region-ocad", "RegionOCAD"),
            Multi("F", "informationSourceIds", "Fuente de información", "information-sources", "FuenteInfo", "Multi_Fuente"),
            Text("G", "observaciones", "Observaciones")
        ]
    };

    private static BlueprintSheet Diagnostico() => new()
    {
        Name = "Diagnóstico ecosistema",
        Key = "diagnostico",
        Table = "Diagnostico",
        Range = "A1:N51",
        HeaderRow = 1,
        DataStartRow = 2,
        DataEndRow = 51,
        Kind = BlueprintSheetKinds.SingleRow,
        EditableByRole = Roles.GestorDepartamental,
        Fields =
        [
            DepartamentoLink("A"),
            Text("B", "caracterizacionGeneral", "Caracterización general del ecosistema musical"),
            Text("C", "fortalezasIdentificadas", "Fortalezas identificadas"),
            Text("D", "politicasPriorizadas", "Políticas priorizadas"),
            Text("E", "debilidadesIdentificadas", "Debilidades identificadas"),
            Text("F", "tensionesOConflictos", "Tensiones o conflictos"),
            Lst("G", "committeeStatusOptionId", "CODEMUS – Comité Dptal de Música", "committee-statuses", "EstadoComite"),
            Lst("H", "planDepartamentalCulturaStatusId", "Plan Departamental de Cultura", "plan-statuses", "EstadoPlan"),
            Inline("I", "consejoDepartamentalCultura", "Consejo Departamental de Cultura",
                ["Existe", "No existe", "Por renovar"]),
            Lst("J", "planDepartamentalMusicaStatusId", "Plan Departamental de Música", "plan-statuses", "EstadoPlan"),
            Inline("K", "ordenanzasCulturales", "Ordenanzas Culturales",
                ["Existe", "No existe", "Por activar"],
                prompt: "Colocar en Observaciones el número de la ordenanza."),
            Text("L", "consejoDepartamentalMusica", "Consejo Departamental de Música"),
            Text("M", "mesaSectorialTerritorial", "Mesa sectorial o territorial identificada"),
            Text("N", "observaciones", "Observaciones")
        ]
    };

    private static BlueprintSheet Oportunidades() => new()
    {
        Name = "Oportunidades de cambio",
        Key = "oportunidades",
        Table = "Oportunidades",
        Range = "A1:G51",
        HeaderRow = 1,
        DataStartRow = 2,
        DataEndRow = 51,
        Kind = BlueprintSheetKinds.Collection,
        EditableByRole = Roles.GestorDepartamental,
        Fields =
        [
            DepartamentoLink("A"),
            Text("B", "situacionIdentificada", "Situación identificada"),
            Text("C", "componenteOtrasDependenciasEntidades", "Componente PNMC - Otras dependencias / Entidades"),
            Text("D", "aliadosYCreyentes", "Aliados y creyentes"),
            Text("E", "territorioInfluencia", "Territorio de influencia"),
            Lst("F", "priorityLevelOptionId", "Nivel de impacto", "priority-levels", "NivelAMB"),
            Text("G", "descripcionAdicional", "Descripción adicional")
        ]
    };

    private static BlueprintSheet EjesPnmc() => new()
    {
        Name = "Ejes PNMC",
        Key = "ejes-pnmc",
        Table = "EjesPNMC",
        Range = "A1:R51",
        HeaderRow = 1,
        DataStartRow = 2,
        DataEndRow = 51,
        Kind = BlueprintSheetKinds.Collection,
        EditableByRole = Roles.GestorDepartamental,
        Fields =
        [
            DepartamentoLink("A"),
            Text("B", "descripcionHallazgo", "Descripción hallazgo"),
            Lst("C", "pnmcAxisId", "Eje PNMC", "pnmc-axes", "EjePNMC"),
            Dependent("D", "pnmcComponentId", "Componente PNMC", "C", BlueprintCascades.ComponentsByAxis,
                prompt: "Lista filtrada según el Eje PNMC seleccionado."),
            Text("E", "accionEstrategica", "Acción Estratégica"),
            Text("F", "politicaPriorizada", "Política priorizada"),
            Text("G", "armonizacionPnc", "Armonización PNC"),
            Text("H", "armonizacionPnd", "Armonización PND"),
            Text("I", "armonizacionInternacional", "Armonización Internacional"),
            Lst("J", "priorityLevelOptionId", "Nivel prioridad", "priority-levels", "NivelAMB"),
            Text("K", "aliadosResponsables", "Aliados / Responsables"),
            Text("L", "fuentesFinanciacion", "Fuentes de financiación"),
            IntField("M", "valorPropuestaCop", "Valor de la propuesta (COP)", min: 0),
            Multi("N", "approachOptionIds", "Enfoques", "approaches", "Enfoques", "Multi_Enfoques"),
            Text("O", "descripcion", "Descripción"),
            Lst("P", "scheduleOptionId", "Cronograma", "schedule-options", "Cronograma"),
            Lst("Q", "proposalStatusOptionId", "Estado", "proposal-statuses", "EstadoPropuesta"),
            Text("R", "observaciones", "Observaciones")
        ]
    };

    private static BlueprintSheet Actores() => new()
    {
        Name = "Actores",
        Key = "actores",
        Table = "Actores",
        Range = "A1:H51",
        HeaderRow = 1,
        DataStartRow = 2,
        DataEndRow = 51,
        Kind = BlueprintSheetKinds.Collection,
        EditableByRole = Roles.GestorDepartamental,
        Fields =
        [
            DepartamentoLink("A"),
            Text("B", "nombreAgente", "Nombre del agente (creyente)",
                v: new BlueprintValidation { Required = true }),
            Lst("C", "agentTypeId", "Tipo de agente (categoría)", "agent-types", "TiposAgente",
                prompt: "Selección única. La lista de Actor específico se filtra por esta categoría."),
            Dependent("D", "ecosystemRoleIds", "Rol en el ecosistema", "C", BlueprintCascades.RolesByAgentType,
                prompt: "Selección múltiple. Cada clic agrega; volver a hacerlo quita. Filtrado por Tipo de agente.",
                multi: true, multiRange: "Multi_ActorEspec"),
            Multi("E", "territorialLevelOptionIds", "Nivel territorial", "territorial-levels", "NivelTerritorial", "Multi_NivelTerritorial"),
            Text("F", "numeroContacto", "Número de contacto",
                v: new BlueprintValidation { MinLength = 7, MaxLength = 20, Message = "El número de contacto debe tener entre 7 y 20 caracteres." }),
            Text("G", "correoElectronico", "Correo electrónico",
                v: new BlueprintValidation
                {
                    MinLength = 5,
                    ExcelFormula = "AND(ISNUMBER(SEARCH(\"@\",G2)),ISNUMBER(SEARCH(\".\",G2)),LEN(G2)>=5)",
                    Message = "Correo electrónico inválido (debe contener @ y . y al menos 5 caracteres)."
                }),
            Text("H", "observaciones", "Observaciones")
        ]
    };

    // ----------------------------------------------------------------- Hojas del Líder

    private static BlueprintSheet Indicadores()
    {
        var fields = new List<BlueprintField>
        {
            Multi("A", "departments", "Departamento", "departments", "Departamentos", "Multi_DeptInd",
                prompt: "Selección múltiple: cada clic en una opción la agrega o la quita."),
            Fixed("B", "action", "Acción"),
            Fixed("C", "indicatorName", "Nombre Indicador"),
            Fixed("D", "target", "Meta")
        };

        for (var m = 0; m < 12; m++)
        {
            var avanceCol = ColLetter(5 + (m * 2));   // E, G, I, ...
            var detalleCol = ColLetter(6 + (m * 2));  // F, H, J, ...
            fields.Add(DecimalField(avanceCol, $"avance{Months[m]}", $"{Months[m]} · Avance cuantitativo",
                min: 0, msg: "Solo se aceptan valores numéricos ≥ 0."));
            fields.Add(Text(detalleCol, $"detalle{Months[m]}", $"{Months[m]} · Detalle"));
        }

        fields.Add(Calc("AC", "currentValue", "Valor actual (calc.)",
            "IF(Meta<=1, MAX(avances mensuales), SUM(avances mensuales))"));
        fields.Add(Calc("AD", "compliance", "% Cumplimiento", "IFERROR(Valor actual / Meta, 0)"));
        fields.Add(Text("AE", "source", "Fuente"));
        fields.Add(IntField("AF", "year", "Año", min: 2000, max: 2100, msg: "Año entre 2000 y 2100."));
        fields.Add(Text("AG", "generalObservations", "Observaciones generales"));

        return new BlueprintSheet
        {
            Name = "Indicadores",
            Key = "indicadores",
            Table = "Indicadores",
            Range = "A2:AG9",
            HeaderRow = 2,
            DataStartRow = 3,
            DataEndRow = 9,
            Kind = BlueprintSheetKinds.FixedCatalog,
            EditableByRole = Roles.LiderGobernanza,
            Fields = fields
        };
    }

    private static BlueprintSheet DetalleIndicadores() => new()
    {
        Name = "Detalle Indicadores",
        Key = "detalle-indicadores",
        Table = null,
        Range = "A1:K14",
        HeaderRow = 1,
        DataStartRow = 2,
        DataEndRow = 14,
        Kind = BlueprintSheetKinds.FixedCatalog,
        EditableByRole = Roles.LiderGobernanza,
        Fields =
        [
            Multi("A", "departments", "Departamento", "departments", "Departamentos", "Multi_DeptDet_1",
                prompt: "Selección múltiple: cada clic agrega; volver a hacer clic quita."),
            Fixed("B", "action", "Acción"),
            Fixed("C", "name", "Nombre"),
            Fixed("D", "target", "Meta"),
            Fixed("E", "formula", "Fórmula de cálculo"),
            Fixed("F", "description", "Descripción / detalle"),
            Multi("G", "months", "MESES", "months", "Meses", "Multi_MesesDet",
                prompt: "Selección múltiple via macro. Cada clic agrega o quita un mes."),
            Calc("H", "currentValue", "Valor actual (calc.)",
                "IFERROR(INDEX(Indicadores!AC, MATCH(Nombre, Indicadores!C)), 0)"),
            Text("I", "source", "Fuente"),
            Lst("J", "year", "Año", "years", "Años"),
            Text("K", "observations", "Observaciones")
        ]
    };

    // ----------------------------------------------------------------- Fábricas de campos

    private static BlueprintField DepartamentoLink(string col) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = "departamento",
        Label = "Departamento",
        Type = BlueprintFieldTypes.Calculated,
        Editable = false,
        Formula = "=IF(Identificación!B=\"\",\"\",Identificación!B)",
        Prompt = "Se hereda automáticamente del Departamento capturado en Identificación."
    };

    private static BlueprintField Calc(string col, string key, string label, string formula) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = key,
        Label = label,
        Type = BlueprintFieldTypes.Calculated,
        Editable = false,
        Formula = formula
    };

    private static BlueprintField Text(string col, string key, string label, BlueprintValidation? v = null) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = key,
        Label = label,
        Type = BlueprintFieldTypes.Text,
        Validation = v
    };

    private static BlueprintField Fixed(string col, string key, string label) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = key,
        Label = label,
        Type = BlueprintFieldTypes.Fixed,
        Editable = false
    };

    private static BlueprintField DateField(string col, string key, string label, string min, string max) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = key,
        Label = label,
        Type = BlueprintFieldTypes.Date,
        Prompt = "Formato dd/mm/aaaa",
        Validation = new BlueprintValidation
        {
            Required = true,
            DateMin = min,
            DateMax = max,
            Message = "Ingresa una fecha entre 01/01/2000 y 31/12/2100."
        }
    };

    private static BlueprintField IntField(string col, string key, string label, decimal? min = null, decimal? max = null, string? msg = null) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = key,
        Label = label,
        Type = BlueprintFieldTypes.Integer,
        Validation = (min is null && max is null && msg is null)
            ? null
            : new BlueprintValidation { Min = min, Max = max, Message = msg }
    };

    private static BlueprintField DecimalField(string col, string key, string label, decimal? min = null, string? msg = null) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = key,
        Label = label,
        Type = BlueprintFieldTypes.Decimal,
        Validation = new BlueprintValidation { Min = min, Message = msg }
    };

    private static BlueprintField Lst(string col, string key, string label, string catalog, string excelRange, string? prompt = null, BlueprintValidation? v = null) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = key,
        Label = label,
        Type = BlueprintFieldTypes.List,
        Catalog = catalog,
        ExcelRange = excelRange,
        Prompt = prompt,
        Validation = v
    };

    private static BlueprintField Inline(string col, string key, string label, IReadOnlyList<string> options, string? prompt = null) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = key,
        Label = label,
        Type = BlueprintFieldTypes.List,
        InlineOptions = options,
        Prompt = prompt
    };

    private static BlueprintField Dependent(string col, string key, string label, string dependsOnColumn, string cascade, string? prompt = null, bool multi = false, string? multiRange = null) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = key,
        Label = label,
        Type = BlueprintFieldTypes.DependentList,
        DependsOnColumn = dependsOnColumn,
        Cascade = cascade,
        Prompt = prompt,
        MultiSelect = multi,
        MultiSeparator = multi ? MultiSeparator : null,
        MultiRange = multiRange
    };

    private static BlueprintField Multi(string col, string key, string label, string catalog, string excelRange, string multiRange, string? prompt = null) => new()
    {
        Column = col,
        ColumnIndex = ColIndex(col),
        Key = key,
        Label = label,
        Type = BlueprintFieldTypes.List,
        Catalog = catalog,
        ExcelRange = excelRange,
        MultiSelect = true,
        MultiSeparator = MultiSeparator,
        MultiRange = multiRange,
        Prompt = prompt
    };

    private static int ColIndex(string col)
    {
        var n = 0;
        foreach (var c in col)
        {
            n = (n * 26) + (c - 'A' + 1);
        }

        return n;
    }

    private static string ColLetter(int index)
    {
        var letters = string.Empty;
        while (index > 0)
        {
            var remainder = (index - 1) % 26;
            letters = (char)('A' + remainder) + letters;
            index = (index - 1) / 26;
        }

        return letters;
    }
}
