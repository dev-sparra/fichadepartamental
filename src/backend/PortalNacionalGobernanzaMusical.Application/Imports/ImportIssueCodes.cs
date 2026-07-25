namespace PortalNacionalGobernanzaMusical.Application.Imports;

/// <summary>
/// Códigos técnicos de incidencia. Se guardan en <c>import_validation_issues.error_code</c> para
/// trazabilidad y son la llave con la que <c>ImportIssueNarrator</c> arma el mensaje funcional que
/// ve el usuario. Nunca se muestran como mensaje principal.
/// </summary>
public static class ImportIssueCodes
{
    // ── Archivo (formato y estructura) ──────────────────────────────────────────────
    public const string FileEmpty = "FILE_EMPTY";
    public const string FileExtensionInvalid = "FILE_EXTENSION_INVALID";
    public const string FileNameInvalid = "FILE_NAME_INVALID";
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string FileNotReadable = "FILE_NOT_READABLE";
    public const string SheetMissing = "FILE_SHEET_MISSING";
    public const string HeaderMismatch = "FILE_HEADER_MISMATCH";
    public const string SheetEmpty = "FILE_SHEET_EMPTY";
    public const string WorkbookWithoutData = "FILE_WITHOUT_DATA";

    // ── Identificación ─────────────────────────────────────────────────────────────
    public const string IdentDateRequired = "IDENT_DATE_REQUIRED";
    public const string IdentDepartmentInvalid = "IDENT_DEPARTMENT_INVALID";
    public const string IdentCityInvalid = "IDENT_CITY_INVALID";
    public const string IdentRegionInvalid = "IDENT_REGION_INVALID";
    public const string IdentSourceInvalid = "IDENT_SOURCE_INVALID";

    // ── Diagnóstico ────────────────────────────────────────────────────────────────
    public const string DiagCommitteeInvalid = "DIAG_COMMITTEE_INVALID";
    public const string DiagPlanCultureInvalid = "DIAG_PLAN_CULTURE_INVALID";
    public const string DiagPlanMusicInvalid = "DIAG_PLAN_MUSIC_INVALID";
    public const string DiagCouncilInvalid = "DIAG_COUNCIL_INVALID";
    public const string DiagOrdinanceInvalid = "DIAG_ORDINANCE_INVALID";

    // ── Oportunidades y Ejes PNMC ──────────────────────────────────────────────────
    public const string OppPriorityInvalid = "OPP_PRIORITY_INVALID";
    public const string AxisInvalid = "EJES_AXIS_INVALID";
    public const string AxisComponentInvalid = "EJES_COMPONENT_INVALID";
    public const string AxisPriorityInvalid = "EJES_PRIORITY_INVALID";
    public const string AxisApproachInvalid = "EJES_APPROACH_INVALID";
    public const string AxisScheduleInvalid = "EJES_SCHEDULE_INVALID";
    public const string AxisStatusInvalid = "EJES_STATUS_INVALID";

    // ── Actores ────────────────────────────────────────────────────────────────────
    public const string ActorAgentTypeInvalid = "ACTOR_AGENT_TYPE_INVALID";
    public const string ActorRoleMappingMissing = "ACTOR_ROLE_MAPPING_MISSING";
    public const string ActorRoleInvalid = "ACTOR_ROLE_INVALID";
    public const string ActorTerritorialLevelInvalid = "ACTOR_TERRITORIAL_LEVEL_INVALID";
    public const string ActorPhoneLength = "ACTOR_PHONE_LENGTH";
    public const string ActorPhoneFormat = "ACTOR_PHONE_FORMAT";
    public const string ActorEmailInvalid = "ACTOR_EMAIL_INVALID";

    // ── Indicadores ────────────────────────────────────────────────────────────────
    public const string IndicatorsDepartmentInvalid = "INDICATORS_DEPARTMENT_INVALID";
    public const string IndicatorNameInvalid = "INDICATOR_NAME_INVALID";
    public const string IndicatorNotFound = "INDICATOR_NOT_FOUND";
    public const string IndicatorYearInvalid = "INDICATOR_YEAR_INVALID";
    public const string IndicatorYearFormat = "INDICATOR_YEAR_FORMAT";
    public const string DetailDepartmentInvalid = "DETAIL_DEPARTMENT_INVALID";
    public const string DetailMonthInvalid = "DETAIL_MONTH_INVALID";
    public const string DetailYearInvalid = "DETAIL_YEAR_INVALID";
    public const string DetailYearFormat = "DETAIL_YEAR_FORMAT";
    public const string DetailIndicatorNotFound = "DETAIL_INDICATOR_NOT_FOUND";
    public const string DetailTemplateNotFound = "DETAIL_TEMPLATE_NOT_FOUND";

    // ── Materialización de la ficha ────────────────────────────────────────────────
    public const string PersistIdentificationRequired = "PERSIST_IDENTIFICATION_REQUIRED";
    public const string PersistIdentificationMultiple = "PERSIST_IDENTIFICATION_MULTIPLE";
    public const string PersistDiagnosticMultiple = "PERSIST_DIAGNOSTIC_MULTIPLE";
    public const string PersistSectionError = "PERSIST_SECTION_ERROR";
    public const string ImportException = "IMPORT_EXCEPTION";

    // ── Severidades ────────────────────────────────────────────────────────────────
    public const string SeverityError = "Error";
    public const string SeverityWarning = "Warning";
    public const string SeverityInfo = "Info";
}
