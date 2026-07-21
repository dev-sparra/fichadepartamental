param(
    [string]$VariablesFile = "C:\Mincultura\ficha-gobernanza\docs\variables.md",
    [string]$OutputFile = "C:\Mincultura\ficha-gobernanza\database\seed\001_master_catalogs.sql"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Escape-Sql([string]$value) {
    return $value.Replace("'", "''")
}

function Write-LookupInsert {
    param(
        [System.Text.StringBuilder]$Builder,
        [string]$TableName,
        [object[]]$Values
    )

    [void]$Builder.AppendLine("INSERT INTO $TableName (id, name, display_order, is_active) VALUES")

    for ($i = 0; $i -lt $Values.Count; $i++) {
        $suffix = if ($i -lt $Values.Count - 1) { ',' } else { '' }
        $name = Escape-Sql([string]$Values[$i])
        [void]$Builder.AppendLine(("    ({0}, '{1}', {0}, 1){2}" -f ($i + 1), $name, $suffix))
    }

    [void]$Builder.AppendLine('ON DUPLICATE KEY UPDATE name = VALUES(name), display_order = VALUES(display_order), is_active = VALUES(is_active);')
    [void]$Builder.AppendLine()
}

function Write-DepartmentsAndMunicipalities {
    param(
        [System.Text.StringBuilder]$Builder,
        [pscustomobject]$Data
    )

    $departmentId = 1
    $municipalityId = 1
    $departmentMap = @{}

    [void]$Builder.AppendLine('INSERT INTO catalog_departments (id, name, display_order, is_active) VALUES')
    $departments = @($Data.Departamentos)

    for ($i = 0; $i -lt $departments.Count; $i++) {
        $name = [string]$departments[$i]
        $departmentMap[$name] = $departmentId
        $suffix = if ($i -lt $departments.Count - 1) { ',' } else { '' }
        [void]$Builder.AppendLine(("    ({0}, '{1}', {0}, 1){2}" -f $departmentId, (Escape-Sql $name), $suffix))
        $departmentId++
    }

    [void]$Builder.AppendLine('ON DUPLICATE KEY UPDATE name = VALUES(name), display_order = VALUES(display_order), is_active = VALUES(is_active);')
    [void]$Builder.AppendLine()

    [void]$Builder.AppendLine('INSERT INTO catalog_municipalities (id, department_id, name, display_order, is_active) VALUES')
    $municipalityLines = New-Object System.Collections.Generic.List[string]

    foreach ($departmentName in $Data.Ciudades_por_departamento.PSObject.Properties.Name) {
        $departmentValue = $Data.Ciudades_por_departamento.$departmentName

        if ($departmentValue -isnot [System.Array]) {
            continue
        }

        $currentDepartmentId = $departmentMap[$departmentName]
        for ($i = 0; $i -lt $departmentValue.Count; $i++) {
            $municipalityName = Escape-Sql([string]$departmentValue[$i])
            $municipalityLines.Add(("    ({0}, {1}, '{2}', {3}, 1)" -f $municipalityId, $currentDepartmentId, $municipalityName, ($i + 1)))
            $municipalityId++
        }
    }

    for ($i = 0; $i -lt $municipalityLines.Count; $i++) {
        $suffix = if ($i -lt $municipalityLines.Count - 1) { ',' } else { '' }
        [void]$Builder.AppendLine($municipalityLines[$i] + $suffix)
    }

    [void]$Builder.AppendLine('ON DUPLICATE KEY UPDATE department_id = VALUES(department_id), name = VALUES(name), display_order = VALUES(display_order), is_active = VALUES(is_active);')
    [void]$Builder.AppendLine()
}

function Write-ComponentMappings {
    param(
        [System.Text.StringBuilder]$Builder,
        [pscustomobject]$Data
    )

    $axes = @($Data.EjePNMC)
    $componentsByAxis = $Data.mapa_Componente_por_Eje
    $componentId = 1

    [void]$Builder.AppendLine('INSERT INTO catalog_pnmc_components (id, pnmc_axis_id, name, display_order, is_active) VALUES')
    $lines = New-Object System.Collections.Generic.List[string]

    for ($axisIndex = 0; $axisIndex -lt $axes.Count; $axisIndex++) {
        $axisName = [string]$axes[$axisIndex]
        $components = @($componentsByAxis.$axisName)
        for ($componentIndex = 0; $componentIndex -lt $components.Count; $componentIndex++) {
            $componentName = Escape-Sql([string]$components[$componentIndex])
            $lines.Add(("    ({0}, {1}, '{2}', {3}, 1)" -f $componentId, ($axisIndex + 1), $componentName, ($componentIndex + 1)))
            $componentId++
        }
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $suffix = if ($i -lt $lines.Count - 1) { ',' } else { '' }
        [void]$Builder.AppendLine($lines[$i] + $suffix)
    }

    [void]$Builder.AppendLine('ON DUPLICATE KEY UPDATE pnmc_axis_id = VALUES(pnmc_axis_id), name = VALUES(name), display_order = VALUES(display_order), is_active = VALUES(is_active);')
    [void]$Builder.AppendLine()
}

function Write-EcosystemRoles {
    param(
        [System.Text.StringBuilder]$Builder,
        [pscustomobject]$Data
    )

    $agentTypes = @($Data.TiposAgente)
    $rolesMap = $Data.mapa_Rol_por_TipoAgente
    $roleId = 1
    [void]$Builder.AppendLine('INSERT INTO catalog_ecosystem_roles (id, agent_type_id, name, display_order, is_active) VALUES')
    $lines = New-Object System.Collections.Generic.List[string]

    for ($agentTypeIndex = 0; $agentTypeIndex -lt $agentTypes.Count; $agentTypeIndex++) {
        $agentType = [string]$agentTypes[$agentTypeIndex]
        $roles = @($rolesMap.$agentType)
        for ($roleIndex = 0; $roleIndex -lt $roles.Count; $roleIndex++) {
            $roleName = Escape-Sql([string]$roles[$roleIndex])
            $lines.Add(("    ({0}, {1}, '{2}', {3}, 1)" -f $roleId, ($agentTypeIndex + 1), $roleName, ($roleIndex + 1)))
            $roleId++
        }
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $suffix = if ($i -lt $lines.Count - 1) { ',' } else { '' }
        [void]$Builder.AppendLine($lines[$i] + $suffix)
    }

    [void]$Builder.AppendLine('ON DUPLICATE KEY UPDATE agent_type_id = VALUES(agent_type_id), name = VALUES(name), display_order = VALUES(display_order), is_active = VALUES(is_active);')
    [void]$Builder.AppendLine()
}

function Write-Years {
    param(
        [System.Text.StringBuilder]$Builder,
        [object[]]$Values
    )

    [void]$Builder.AppendLine('INSERT INTO catalog_years (id, value, is_active) VALUES')

    for ($i = 0; $i -lt $Values.Count; $i++) {
        $yearValue = [int]$Values[$i]
        $suffix = if ($i -lt $Values.Count - 1) { ',' } else { '' }
        [void]$Builder.AppendLine(("    ({0}, {1}, 1){2}" -f ($i + 1), $yearValue, $suffix))
    }

    [void]$Builder.AppendLine('ON DUPLICATE KEY UPDATE value = VALUES(value), is_active = VALUES(is_active);')
    [void]$Builder.AppendLine()
}

function Write-IndicatorDefinitions {
    param(
        [System.Text.StringBuilder]$Builder
    )

    $definitions = @(
        @{ Id = 1; Order = 1; Target = '25'; Action = 'Departamentos con asesorías y articulaciones realizadas a través de los comités departamentales de música: Gobernanza'; Name = 'Porcentaje de avance en la consolidación de los Comités Departamentales de Música' },
        @{ Id = 2; Order = 2; Target = '5'; Action = 'Asesorías y asistencias técnicas realizadas para la formulación e implementación de planes departamentales y desarrollo políticas del sector musical: Gobernanza'; Name = 'Número de Planes de Desarrollo Musical' },
        @{ Id = 3; Order = 3; Target = '25'; Action = 'Fomentar la creación y consolidación de asociaciones y redes de artistas que impulsen la organización y representación del sector en cada territorio.'; Name = 'Número de Asociaciones de artistas consolidadas' },
        @{ Id = 4; Order = 4; Target = '1'; Action = 'Establecer un mecanismo de participación del sector musical en todas las fases del ciclo de las políticas públicas (formulación, implementación y evaluación) en articulación con entidades regionales, departamentales, municipales.'; Name = 'Porcentaje de avance en el mecanismo de participación del sector musical' },
        @{ Id = 5; Order = 5; Target = '1'; Action = 'Implementar una estrategia articulada entre la Dirección de Poblaciones, la Dirección de Fomento Regional y la Dirección de Artes, en coordinación con el Ministerio de Salud, para promover la inclusión de artistas-músicos en los programas de seguridad social, como BEPS y otras estrategias implementadas por el Ministerio de las Culturas.'; Name = 'Porcentaje de avance en la implementación de la estrategia para promover la inclusión de músicos en programas de seguridad social' },
        @{ Id = 6; Order = 6; Target = '1'; Action = 'En articulación con los diferentes agentes del sector musical, construir criterios de regulación de tarifas de referencia para la valoración económica de los diferentes oficios del sector musical.'; Name = 'Porcentaje de avance en la tabla de tarifas de referencia para los oficios del sector musical.' },
        @{ Id = 7; Order = 7; Target = '6'; Action = 'Impulsar la creación y el fortalecimiento de mercados regionales de música, tanto para la circulación en vivo como en soportes digitales, promoviendo el acceso a circuitos comerciales y contribuyendo a la sostenibilidad económica del sector musical en los territorios.'; Name = 'Número de mercados musicales creados y fortalecidos' }
    )

    [void]$Builder.AppendLine('INSERT INTO catalog_indicator_definitions (id, action_name, indicator_name, target_value, display_order, is_active) VALUES')
    for ($i = 0; $i -lt $definitions.Count; $i++) {
        $item = $definitions[$i]
        $suffix = if ($i -lt $definitions.Count - 1) { ',' } else { '' }
        [void]$Builder.AppendLine(("    ({0}, '{1}', '{2}', {3}, {4}, 1){5}" -f $item.Id, (Escape-Sql $item.Action), (Escape-Sql $item.Name), $item.Target, $item.Order, $suffix))
    }

    [void]$Builder.AppendLine('ON DUPLICATE KEY UPDATE action_name = VALUES(action_name), indicator_name = VALUES(indicator_name), target_value = VALUES(target_value), display_order = VALUES(display_order), is_active = VALUES(is_active);')
    [void]$Builder.AppendLine()
}

function Write-IndicatorDetailTemplates {
    param(
        [System.Text.StringBuilder]$Builder
    )

    $templates = @(
        @{ Id = 1; IndicatorId = 4; Order = 1; Formula = 'Diseño del mecanismo 25%'; Detail = 'Apropiación de la reglamentación en torno al Sistema Nacional de Cultura SNCu, establecido por la Ley 397 de 1997 (Ley General de Cultura), se define como el "conjunto de instancias y procesos de desarrollo institucional, planificación e información articulados entre sí, que posibilitan el desarrollo cultural y el acceso de la comunidad a los bienes y servicios culturales según los principios de descentralización, participación y autonomía". Acta de sesión teórica de acompañamiento "7%"' },
        @{ Id = 2; IndicatorId = 4; Order = 2; Formula = 'Diseño del mecanismo 25%'; Detail = 'Diagnóstico y caracterización de Consejos Municipales, Distritales y Departamentales de Cultura, de igual manera, identificar el/la Consejero Nacional de Música existentes en el territorio, alojar datos de sus representantes y estado del consejo en la presente Ficha, pestaña "Actores" 8%' },
        @{ Id = 3; IndicatorId = 4; Order = 3; Formula = 'Diseño del mecanismo 25%'; Detail = 'Creación de un canal de comunicación (grupo de WhatsApp) estimulando la comunicación permanente entre los diferentes eslabones del SNCu identificados (grupo de WhatsApp creado) "10%"' },
        @{ Id = 4; IndicatorId = 4; Order = 4; Formula = 'Establecimiento de Hoja de Ruta y Cronograma 50%'; Detail = 'Encuentro entre los diferentes eslabones del SNCu identificados con el fin de establecer una hoja de ruta y cronograma de actividades (teniendo en cuenta un margen diverso de temas, que permitan la identificación de documentos normativos establecidos (Planes Municipales y Departamentales de Cultura, PNMC, PNC, PND, Ley General de Cultura), revisión de avances de los documentos que atañen lo territorial y análisis de lo Nacional e Internacional, propuestas para participar en acciones de actualización o creación de Planes de Cultura, entre otros. Acta de encuentros y cronograma de actividades "50%"' },
        @{ Id = 5; IndicatorId = 4; Order = 5; Formula = 'Ejecución y Evaluación 25%'; Detail = 'Implementación de la hoja de ruta trazada de acuerdo al cronograma establecido. Documentos que evidencien la acción realizada (Actas, Fotos, videos, listados de asistencia, entre otros) 25%' },
        @{ Id = 6; IndicatorId = 5; Order = 1; Formula = 'Diagnóstico del estado de acceso a la seguridad social 25%'; Detail = 'Articulación con Colpensiones a nivel municipal o departamental con el fin de evaluar el alcance de apoyos a la población Adulto Mayor (cantidad de beneficiarios por municipio o departamento). Documento que evidencie la información oficial de Colpensiones en el territorio 25%. Nota: BEPS no es el único beneficio para la población Adulto Mayor, existen también los programas solidarios Colombia Mayor y Dignidad Mayor, sin embargo, estos son administrados por Prosperidad Social a nivel municipal o departamental.' },
        @{ Id = 7; IndicatorId = 5; Order = 2; Formula = 'Diseño de la estrategia 25%'; Detail = 'Articulación con entes territoriales culturales o instituciones educativas con el fin de diseñar la estrategia de inclusión de nuevos beneficiarios a los programas solidarios para la población Adulto Mayor, por ejemplo: una actividad de recolección de documentos de manera presencial, realizado en Casa de la Cultura o Colegio, articulando administración municipal, institución educativa, Colpensiones, Prosperidad Social. Documento que evidencie la mencionada articulación 25%' },
        @{ Id = 8; IndicatorId = 5; Order = 3; Formula = 'Implementación 25%'; Detail = 'Jornada(s) de inclusión de nuevos beneficiarios. Evidencia de actividad realizada 25%' },
        @{ Id = 9; IndicatorId = 5; Order = 4; Formula = 'Evaluación 25%'; Detail = 'Verificación en sistemas de Colpensiones o Prosperidad Social la inclusión de los nuevos beneficiarios. Pantallazo de sistemas de información o documento que acredite el beneficio de un número indeterminado de Adultos Mayores 25%. Si por cuestiones temporales el beneficiario aún no aparece en plataforma, es suficiente para la evaluación el formulario de registro.' },
        @{ Id = 10; IndicatorId = 6; Order = 1; Formula = 'Diagnóstico de problemática 25%'; Detail = 'Identificación de la problemática relacionada con tarifas en un Subsector particular del Ecosistema Musical y propiciar diálogos con representantes. Evidencia de encuentros 25%' },
        @{ Id = 11; IndicatorId = 6; Order = 2; Formula = 'Diseño de los criterios de regulación 25%'; Detail = 'Concertación en torno a los criterios de regulación (trayectoria, calidad, formato, producción en escena, producción musical). Documento que evidencie los criterios de regulación, tomar en cuenta procesos similares 25%' },
        @{ Id = 12; IndicatorId = 6; Order = 3; Formula = 'Realización de la tabla 25%'; Detail = 'Tabla de valores que relacione los criterios definidos y el cruce con horas laborales y recursos logísticos 25%' },
        @{ Id = 13; IndicatorId = 6; Order = 4; Formula = 'Evaluación 25%'; Detail = 'Validación temporal (1 mes) revisando el impacto de la concertación. Encuestas o entrevistas que den cuenta del alcance del diálogo Subsectorial 25%' }
    )

    [void]$Builder.AppendLine('INSERT INTO catalog_indicator_detail_templates (id, indicator_definition_id, sort_order, formula_label, detail_description) VALUES')
    for ($i = 0; $i -lt $templates.Count; $i++) {
        $item = $templates[$i]
        $suffix = if ($i -lt $templates.Count - 1) { ',' } else { '' }
        [void]$Builder.AppendLine(("    ({0}, {1}, {2}, '{3}', '{4}'){5}" -f $item.Id, $item.IndicatorId, $item.Order, (Escape-Sql $item.Formula), (Escape-Sql $item.Detail), $suffix))
    }

    [void]$Builder.AppendLine('ON DUPLICATE KEY UPDATE sort_order = VALUES(sort_order), formula_label = VALUES(formula_label), detail_description = VALUES(detail_description);')
    [void]$Builder.AppendLine()
}

$jsonText = Get-Content -LiteralPath $VariablesFile -Raw | ConvertFrom-Json
$builder = New-Object System.Text.StringBuilder

[void]$builder.AppendLine('-- Archivo generado automaticamente desde docs/variables.md')
[void]$builder.AppendLine('-- No editar manualmente; regenerar con database/scripts/Generate-MasterCatalogSeed.ps1')
[void]$builder.AppendLine()

Write-LookupInsert -Builder $builder -TableName 'catalog_region_ocad' -Values @($jsonText.RegionOCAD)
Write-LookupInsert -Builder $builder -TableName 'catalog_committee_statuses' -Values @($jsonText.EstadoComite)
Write-LookupInsert -Builder $builder -TableName 'catalog_plan_statuses' -Values @($jsonText.EstadoPlan)
Write-LookupInsert -Builder $builder -TableName 'catalog_priority_levels' -Values @($jsonText.NivelAMB)
Write-LookupInsert -Builder $builder -TableName 'catalog_pnmc_axes' -Values @($jsonText.EjePNMC)
Write-ComponentMappings -Builder $builder -Data $jsonText
Write-LookupInsert -Builder $builder -TableName 'catalog_approach_options' -Values @($jsonText.Enfoques)
Write-LookupInsert -Builder $builder -TableName 'catalog_schedule_options' -Values @($jsonText.Cronograma)
Write-LookupInsert -Builder $builder -TableName 'catalog_proposal_statuses' -Values @($jsonText.EstadoPropuesta)
Write-LookupInsert -Builder $builder -TableName 'catalog_agent_types' -Values @($jsonText.TiposAgente)
Write-EcosystemRoles -Builder $builder -Data $jsonText
Write-LookupInsert -Builder $builder -TableName 'catalog_territorial_levels' -Values @($jsonText.NivelTerritorial)
Write-LookupInsert -Builder $builder -TableName 'catalog_information_sources' -Values @($jsonText.FuenteInfo)
Write-LookupInsert -Builder $builder -TableName 'catalog_months' -Values @($jsonText.Meses)
Write-Years -Builder $builder -Values @($jsonText.Años)
Write-DepartmentsAndMunicipalities -Builder $builder -Data $jsonText
Write-IndicatorDefinitions -Builder $builder
Write-IndicatorDetailTemplates -Builder $builder

$parentDirectory = Split-Path -Parent $OutputFile
if (-not (Test-Path -LiteralPath $parentDirectory)) {
    New-Item -ItemType Directory -Path $parentDirectory -Force | Out-Null
}

Set-Content -LiteralPath $OutputFile -Value $builder.ToString() -Encoding UTF8
