using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Domain.Entities;

namespace PortalNacionalGobernanzaMusical.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<RegionOcadOption> RegionOcadOptions => Set<RegionOcadOption>();
    public DbSet<CommitteeStatusOption> CommitteeStatusOptions => Set<CommitteeStatusOption>();
    public DbSet<PlanStatusOption> PlanStatusOptions => Set<PlanStatusOption>();
    public DbSet<PriorityLevelOption> PriorityLevelOptions => Set<PriorityLevelOption>();
    public DbSet<PnmcAxis> PnmcAxes => Set<PnmcAxis>();
    public DbSet<PnmcComponent> PnmcComponents => Set<PnmcComponent>();
    public DbSet<ApproachOption> ApproachOptions => Set<ApproachOption>();
    public DbSet<ScheduleOption> ScheduleOptions => Set<ScheduleOption>();
    public DbSet<ProposalStatusOption> ProposalStatusOptions => Set<ProposalStatusOption>();
    public DbSet<AgentType> AgentTypes => Set<AgentType>();
    public DbSet<EcosystemRole> EcosystemRoles => Set<EcosystemRole>();
    public DbSet<TerritorialLevelOption> TerritorialLevelOptions => Set<TerritorialLevelOption>();
    public DbSet<InformationSourceOption> InformationSourceOptions => Set<InformationSourceOption>();
    public DbSet<MonthOption> MonthOptions => Set<MonthOption>();
    public DbSet<YearOption> YearOptions => Set<YearOption>();
    public DbSet<IndicatorDefinition> IndicatorDefinitions => Set<IndicatorDefinition>();
    public DbSet<IndicatorDetailTemplate> IndicatorDetailTemplates => Set<IndicatorDetailTemplate>();
    public DbSet<IndicatorRecord> IndicatorRecords => Set<IndicatorRecord>();
    public DbSet<IndicatorMonthlyProgress> IndicatorMonthlyProgresses => Set<IndicatorMonthlyProgress>();
    public DbSet<IndicatorDetailRecord> IndicatorDetailRecords => Set<IndicatorDetailRecord>();
    public DbSet<IndicatorDetailRecordMonth> IndicatorDetailRecordMonths => Set<IndicatorDetailRecordMonth>();

    public DbSet<FichaDepartamental> FichasDepartamentales => Set<FichaDepartamental>();
    public DbSet<FichaFuenteInformacion> FichaFuentesInformacion => Set<FichaFuenteInformacion>();
    public DbSet<DiagnosticoEcosistema> DiagnosticosEcosistema => Set<DiagnosticoEcosistema>();
    public DbSet<OportunidadCambio> OportunidadesCambio => Set<OportunidadCambio>();
    public DbSet<EjePnmcRegistro> EjesPnmc => Set<EjePnmcRegistro>();
    public DbSet<EjePnmcRegistroEnfoque> EjesPnmcEnfoques => Set<EjePnmcRegistroEnfoque>();
    public DbSet<Actor> Actores => Set<Actor>();
    public DbSet<ActorRolEcosistema> ActorRolesEcosistema => Set<ActorRolEcosistema>();
    public DbSet<ActorNivelTerritorial> ActorNivelesTerritoriales => Set<ActorNivelTerritorial>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportValidationIssue> ImportValidationIssues => Set<ImportValidationIssue>();
    public DbSet<ImportIdentificationStagingRow> ImportIdentificationStagingRows => Set<ImportIdentificationStagingRow>();
    public DbSet<ImportDiagnosticStagingRow> ImportDiagnosticStagingRows => Set<ImportDiagnosticStagingRow>();
    public DbSet<ImportOpportunityStagingRow> ImportOpportunityStagingRows => Set<ImportOpportunityStagingRow>();
    public DbSet<ImportPnmcAxisStagingRow> ImportPnmcAxisStagingRows => Set<ImportPnmcAxisStagingRow>();
    public DbSet<ImportActorStagingRow> ImportActorStagingRows => Set<ImportActorStagingRow>();
    public DbSet<ImportIndicatorStagingRow> ImportIndicatorStagingRows => Set<ImportIndicatorStagingRow>();
    public DbSet<ImportIndicatorDetailStagingRow> ImportIndicatorDetailStagingRows => Set<ImportIndicatorDetailStagingRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSecurity(modelBuilder);
        ConfigureCatalogs(modelBuilder);
        ConfigureIndicatorMasterData(modelBuilder);
        ConfigureIndicatorOperations(modelBuilder);
        ConfigureGovernance(modelBuilder);
        ConfigureImports(modelBuilder);
        ConfigureWorkflowAudit(modelBuilder);

        ApplySnakeCaseNaming(modelBuilder);
    }

    private static void ConfigureWorkflowAudit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalRecord>(entity =>
        {
            entity.ToTable("approval_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ReviewedByEmail).HasMaxLength(200);
            entity.Property(x => x.ReviewedByName).HasMaxLength(200);
            entity.Property(x => x.Comment).HasColumnType("longtext");
            entity.HasIndex(x => x.FichaDepartamentalId);
            entity.HasOne(x => x.FichaDepartamental).WithMany().HasForeignKey(x => x.FichaDepartamentalId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserEmail).HasMaxLength(200).IsRequired();
            entity.Property(x => x.UserDisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.UserRoles).HasMaxLength(300);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.Module).HasMaxLength(60).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityKey).HasMaxLength(100);
            entity.Property(x => x.EntityLabel).HasMaxLength(400);
            entity.Property(x => x.Operation).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Description).HasColumnType("longtext");
            entity.Property(x => x.Result).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ChangesJson).HasColumnType("longtext");
            entity.Property(x => x.RequestMethod).HasMaxLength(10);
            entity.Property(x => x.RequestPath).HasMaxLength(300);
            entity.Property(x => x.OldValuesJson).HasColumnType("longtext");
            entity.Property(x => x.NewValuesJson).HasColumnType("longtext");
            entity.HasIndex(x => new { x.EntityName, x.EntityId });
            // El historial se consulta por fecha y se filtra por módulo y por usuario.
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.Module);
            entity.HasIndex(x => x.UserEmail);
        });
    }

    private static void ApplySnakeCaseNaming(ModelBuilder modelBuilder)
    {
        static string ToSnakeCase(string name)
        {
            return string.Concat(name.Select((c, i) =>
                i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static void ConfigureSecurity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("security_users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NormalizedEmail).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.Property(x => x.PasswordHash).HasMaxLength(500);
            entity.Property(x => x.MustChangePassword).IsRequired().HasDefaultValue(false);
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("security_roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<UserRoleAssignment>(entity =>
        {
            entity.ToTable("security_user_roles");
            entity.HasKey(x => new { x.UserAccountId, x.RoleId });
            entity.HasOne(x => x.UserAccount).WithMany(x => x.RoleAssignments).HasForeignKey(x => x.UserAccountId);
            entity.HasOne(x => x.Role).WithMany(x => x.UserAssignments).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.ToTable("user_notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecipientEmail).HasMaxLength(200).IsRequired();
            entity.Property(x => x.RecipientNormalizedEmail).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(50).IsRequired();
            entity.Property(x => x.EventCode).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasColumnType("longtext").IsRequired();
            entity.Property(x => x.Tone).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ActionRoute).HasMaxLength(300);
            entity.Property(x => x.RelatedEntityName).HasMaxLength(100);
            entity.Property(x => x.TriggeredByEmail).HasMaxLength(200);
            entity.Property(x => x.TriggeredByName).HasMaxLength(200);
            entity.Property(x => x.IsRead).IsRequired().HasDefaultValue(false);
            // La bandeja siempre se consulta por destinatario y en orden cronológico inverso.
            entity.HasIndex(x => new { x.RecipientNormalizedEmail, x.IsRead });
            entity.HasIndex(x => x.CreatedAtUtc);
        });
    }

    private static void ConfigureCatalogs(ModelBuilder modelBuilder)
    {
        ConfigureCatalogEntity<Department>(modelBuilder, "catalog_departments");
        ConfigureCatalogEntity<Municipality>(modelBuilder, "catalog_municipalities");
        ConfigureCatalogEntity<RegionOcadOption>(modelBuilder, "catalog_region_ocad");
        ConfigureCatalogEntity<CommitteeStatusOption>(modelBuilder, "catalog_committee_statuses");
        ConfigureCatalogEntity<PlanStatusOption>(modelBuilder, "catalog_plan_statuses");
        ConfigureCatalogEntity<PriorityLevelOption>(modelBuilder, "catalog_priority_levels");
        ConfigureCatalogEntity<PnmcAxis>(modelBuilder, "catalog_pnmc_axes");
        ConfigureCatalogEntity<PnmcComponent>(modelBuilder, "catalog_pnmc_components");
        ConfigureCatalogEntity<ApproachOption>(modelBuilder, "catalog_approach_options");
        ConfigureCatalogEntity<ScheduleOption>(modelBuilder, "catalog_schedule_options");
        ConfigureCatalogEntity<ProposalStatusOption>(modelBuilder, "catalog_proposal_statuses");
        ConfigureCatalogEntity<AgentType>(modelBuilder, "catalog_agent_types");
        ConfigureCatalogEntity<EcosystemRole>(modelBuilder, "catalog_ecosystem_roles");
        ConfigureCatalogEntity<TerritorialLevelOption>(modelBuilder, "catalog_territorial_levels");
        ConfigureCatalogEntity<InformationSourceOption>(modelBuilder, "catalog_information_sources");
        ConfigureCatalogEntity<MonthOption>(modelBuilder, "catalog_months");

        modelBuilder.Entity<Municipality>(entity =>
        {
            entity.HasIndex(x => new { x.DepartmentId, x.Name }).IsUnique();
            entity.HasOne(x => x.Department).WithMany(x => x.Municipalities).HasForeignKey(x => x.DepartmentId);
        });

        modelBuilder.Entity<PnmcComponent>(entity =>
        {
            entity.HasIndex(x => new { x.PnmcAxisId, x.Name }).IsUnique();
            entity.HasOne(x => x.PnmcAxis).WithMany(x => x.Components).HasForeignKey(x => x.PnmcAxisId);
        });

        modelBuilder.Entity<EcosystemRole>(entity =>
        {
            entity.HasIndex(x => new { x.AgentTypeId, x.Name }).IsUnique();
            entity.HasOne(x => x.AgentType).WithMany(x => x.EcosystemRoles).HasForeignKey(x => x.AgentTypeId);
        });

        modelBuilder.Entity<YearOption>(entity =>
        {
            entity.ToTable("catalog_years");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Value).IsRequired();
            entity.HasIndex(x => x.Value).IsUnique();
        });
    }

    private static void ConfigureIndicatorMasterData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IndicatorDefinition>(entity =>
        {
            entity.ToTable("catalog_indicator_definitions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActionName).HasColumnType("longtext").IsRequired();
            entity.Property(x => x.IndicatorName).HasColumnType("longtext").IsRequired();
            entity.Property(x => x.TargetValue).HasPrecision(18, 4).IsRequired();
            entity.HasIndex(x => x.DisplayOrder).IsUnique();
        });

        modelBuilder.Entity<IndicatorDetailTemplate>(entity =>
        {
            entity.ToTable("catalog_indicator_detail_templates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FormulaLabel).HasColumnType("longtext").IsRequired();
            entity.Property(x => x.DetailDescription).HasColumnType("longtext").IsRequired();
            entity.HasIndex(x => new { x.IndicatorDefinitionId, x.SortOrder }).IsUnique();
            entity.HasOne(x => x.IndicatorDefinition).WithMany(x => x.DetailTemplates).HasForeignKey(x => x.IndicatorDefinitionId);
        });
    }

    private static void ConfigureIndicatorOperations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IndicatorRecord>(entity =>
        {
            entity.ToTable("indicator_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Source).HasColumnType("longtext");
            entity.Property(x => x.GeneralObservations).HasColumnType("longtext");
            entity.Property(x => x.CurrentValueCalculated).HasPrecision(18, 4);
            entity.Property(x => x.CompliancePercentageCalculated).HasPrecision(18, 6);
            entity.HasIndex(x => new { x.DepartmentId, x.IndicatorDefinitionId, x.Year }).IsUnique();
            entity.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
            entity.HasOne(x => x.IndicatorDefinition).WithMany().HasForeignKey(x => x.IndicatorDefinitionId);
        });

        modelBuilder.Entity<IndicatorMonthlyProgress>(entity =>
        {
            entity.ToTable("indicator_monthly_progresses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.QuantitativeAdvance).HasPrecision(18, 4);
            entity.Property(x => x.Detail).HasColumnType("longtext");
            entity.HasIndex(x => new { x.IndicatorRecordId, x.MonthOptionId }).IsUnique();
            entity.HasOne(x => x.IndicatorRecord).WithMany(x => x.MonthlyProgresses).HasForeignKey(x => x.IndicatorRecordId);
            entity.HasOne(x => x.MonthOption).WithMany().HasForeignKey(x => x.MonthOptionId);
        });

        modelBuilder.Entity<IndicatorDetailRecord>(entity =>
        {
            entity.ToTable("indicator_detail_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Source).HasColumnType("longtext");
            entity.Property(x => x.Observations).HasColumnType("longtext");
            entity.Property(x => x.CurrentValueCalculated).HasPrecision(18, 4);
            entity.HasIndex(x => new { x.DepartmentId, x.IndicatorDefinitionId, x.IndicatorDetailTemplateId, x.Year }).IsUnique();
            entity.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
            entity.HasOne(x => x.IndicatorDefinition).WithMany().HasForeignKey(x => x.IndicatorDefinitionId);
            entity.HasOne(x => x.IndicatorDetailTemplate).WithMany().HasForeignKey(x => x.IndicatorDetailTemplateId);
        });

        modelBuilder.Entity<IndicatorDetailRecordMonth>(entity =>
        {
            entity.ToTable("indicator_detail_record_months");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IndicatorDetailRecordId, x.MonthOptionId }).IsUnique();
            entity.HasOne(x => x.IndicatorDetailRecord).WithMany(x => x.SelectedMonths).HasForeignKey(x => x.IndicatorDetailRecordId);
            entity.HasOne(x => x.MonthOption).WithMany().HasForeignKey(x => x.MonthOptionId);
        });
    }

    private static void ConfigureGovernance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FichaDepartamental>(entity =>
        {
            entity.ToTable("fichas_departamentales");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FechaLevantamiento).IsRequired();
            entity.Property(x => x.ResponsableRegistro).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Observaciones).HasColumnType("longtext");
            entity.Property(x => x.CreatedByEmail).HasMaxLength(200);
            entity.HasIndex(x => new { x.DepartmentId, x.FechaLevantamiento }).IsUnique();
            entity.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
            entity.HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId);
            entity.HasOne(x => x.RegionOcadOption).WithMany().HasForeignKey(x => x.RegionOcadOptionId);
        });

        modelBuilder.Entity<FichaFuenteInformacion>(entity =>
        {
            entity.ToTable("ficha_fuentes_informacion");
            entity.HasKey(x => new { x.FichaDepartamentalId, x.InformationSourceOptionId });
            entity.HasOne(x => x.FichaDepartamental).WithMany(x => x.FuentesInformacion).HasForeignKey(x => x.FichaDepartamentalId);
            entity.HasOne(x => x.InformationSourceOption).WithMany().HasForeignKey(x => x.InformationSourceOptionId);
        });

        modelBuilder.Entity<DiagnosticoEcosistema>(entity =>
        {
            entity.ToTable("diagnosticos_ecosistema");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.FichaDepartamentalId).IsUnique();
            entity.Property(x => x.CaracterizacionGeneral).HasColumnType("longtext");
            entity.Property(x => x.FortalezasIdentificadas).HasColumnType("longtext");
            entity.Property(x => x.PoliticasPriorizadas).HasColumnType("longtext");
            entity.Property(x => x.DebilidadesIdentificadas).HasColumnType("longtext");
            entity.Property(x => x.TensionesOConflictos).HasColumnType("longtext");
            entity.Property(x => x.ConsejoDepartamentalCultura).HasMaxLength(100);
            entity.Property(x => x.OrdenanzasCulturales).HasMaxLength(100);
            entity.Property(x => x.ConsejoDepartamentalMusica).HasColumnType("longtext");
            entity.Property(x => x.MesaSectorialTerritorial).HasColumnType("longtext");
            entity.Property(x => x.Observaciones).HasColumnType("longtext");
            entity.HasOne(x => x.FichaDepartamental).WithOne(x => x.DiagnosticoEcosistema).HasForeignKey<DiagnosticoEcosistema>(x => x.FichaDepartamentalId);
        });

        modelBuilder.Entity<OportunidadCambio>(entity =>
        {
            entity.ToTable("oportunidades_cambio");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SituacionIdentificada).HasColumnType("longtext");
            entity.Property(x => x.ComponenteOtrasDependenciasEntidades).HasColumnType("longtext");
            entity.Property(x => x.AliadosYCreyentes).HasColumnType("longtext");
            entity.Property(x => x.TerritorioInfluencia).HasColumnType("longtext");
            entity.Property(x => x.DescripcionAdicional).HasColumnType("longtext");
            entity.HasOne(x => x.FichaDepartamental).WithMany(x => x.OportunidadesCambio).HasForeignKey(x => x.FichaDepartamentalId);
        });

        modelBuilder.Entity<EjePnmcRegistro>(entity =>
        {
            entity.ToTable("ejes_pnmc_registros");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DescripcionHallazgo).HasColumnType("longtext");
            entity.Property(x => x.AccionEstrategica).HasColumnType("longtext");
            entity.Property(x => x.PoliticaPriorizada).HasColumnType("longtext");
            entity.Property(x => x.ArmonizacionPnc).HasColumnType("longtext");
            entity.Property(x => x.ArmonizacionPnd).HasColumnType("longtext");
            entity.Property(x => x.ArmonizacionInternacional).HasColumnType("longtext");
            entity.Property(x => x.AliadosResponsables).HasColumnType("longtext");
            entity.Property(x => x.FuentesFinanciacion).HasColumnType("longtext");
            entity.Property(x => x.ValorPropuestaCop).HasPrecision(18, 2);
            entity.Property(x => x.Descripcion).HasColumnType("longtext");
            entity.Property(x => x.Observaciones).HasColumnType("longtext");
            entity.HasOne(x => x.FichaDepartamental).WithMany(x => x.EjesPnmc).HasForeignKey(x => x.FichaDepartamentalId);
        });

        modelBuilder.Entity<EjePnmcRegistroEnfoque>(entity =>
        {
            entity.ToTable("ejes_pnmc_registro_enfoques");
            entity.HasKey(x => new { x.EjePnmcRegistroId, x.ApproachOptionId });
            entity.HasOne(x => x.EjePnmcRegistro).WithMany(x => x.Enfoques).HasForeignKey(x => x.EjePnmcRegistroId);
            entity.HasOne(x => x.ApproachOption).WithMany().HasForeignKey(x => x.ApproachOptionId);
        });

        modelBuilder.Entity<Actor>(entity =>
        {
            entity.ToTable("actores");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.NombreAgente).HasColumnType("longtext").IsRequired();
            entity.Property(x => x.NumeroContacto).HasMaxLength(50);
            entity.Property(x => x.CorreoElectronico).HasMaxLength(200);
            entity.Property(x => x.Observaciones).HasColumnType("longtext");
            entity.HasOne(x => x.FichaDepartamental).WithMany(x => x.Actores).HasForeignKey(x => x.FichaDepartamentalId);
        });

        modelBuilder.Entity<ActorRolEcosistema>(entity =>
        {
            entity.ToTable("actor_roles_ecosistema");
            entity.HasKey(x => new { x.ActorId, x.EcosystemRoleId });
            entity.HasOne(x => x.Actor).WithMany(x => x.RolesEcosistema).HasForeignKey(x => x.ActorId);
            entity.HasOne(x => x.EcosystemRole).WithMany().HasForeignKey(x => x.EcosystemRoleId);
        });

        modelBuilder.Entity<ActorNivelTerritorial>(entity =>
        {
            entity.ToTable("actor_niveles_territoriales");
            entity.HasKey(x => new { x.ActorId, x.TerritorialLevelOptionId });
            entity.HasOne(x => x.Actor).WithMany(x => x.NivelesTerritoriales).HasForeignKey(x => x.ActorId);
            entity.HasOne(x => x.TerritorialLevelOption).WithMany().HasForeignKey(x => x.TerritorialLevelOptionId);
        });
    }

    private static void ConfigureImports(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.ToTable("import_batches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.SummaryJson).HasColumnType("longtext");
            entity.Property(x => x.CreatedByEmail).HasMaxLength(256);
            entity.HasIndex(x => x.CreatedByEmail);
        });

        modelBuilder.Entity<ImportValidationIssue>(entity =>
        {
            entity.ToTable("import_validation_issues");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Severity).HasMaxLength(20).IsRequired();
            entity.Property(x => x.SheetName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CellReference).HasMaxLength(20);
            entity.Property(x => x.ErrorCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Message).HasColumnType("longtext").IsRequired();
            entity.Property(x => x.RawValue).HasColumnType("longtext");
            entity.Property(x => x.ContextJson).HasColumnType("longtext");
            entity.HasIndex(x => x.ImportBatchId);
            entity.HasOne(x => x.ImportBatch).WithMany(x => x.ValidationIssues).HasForeignKey(x => x.ImportBatchId);
        });

        ConfigureStagingEntity<ImportIdentificationStagingRow>(modelBuilder, "import_identification_staging_rows", entity =>
        {
            entity.Property(x => x.DepartmentName).HasMaxLength(255);
            entity.Property(x => x.MunicipalityName).HasMaxLength(255);
            entity.Property(x => x.ResponsableRegistro).HasMaxLength(200);
            entity.Property(x => x.RegionOcadName).HasMaxLength(255);
            entity.Property(x => x.InformationSourcesRaw).HasColumnType("longtext");
            entity.Property(x => x.Observaciones).HasColumnType("longtext");
        });

        ConfigureStagingEntity<ImportDiagnosticStagingRow>(modelBuilder, "import_diagnostic_staging_rows", entity =>
        {
            entity.Property(x => x.CaracterizacionGeneral).HasColumnType("longtext");
            entity.Property(x => x.FortalezasIdentificadas).HasColumnType("longtext");
            entity.Property(x => x.PoliticasPriorizadas).HasColumnType("longtext");
            entity.Property(x => x.DebilidadesIdentificadas).HasColumnType("longtext");
            entity.Property(x => x.TensionesOConflictos).HasColumnType("longtext");
            entity.Property(x => x.CommitteeStatusName).HasMaxLength(255);
            entity.Property(x => x.PlanDepartamentalCulturaStatusName).HasMaxLength(255);
            entity.Property(x => x.ConsejoDepartamentalCultura).HasMaxLength(100);
            entity.Property(x => x.PlanDepartamentalMusicaStatusName).HasMaxLength(255);
            entity.Property(x => x.OrdenanzasCulturales).HasMaxLength(100);
            entity.Property(x => x.ConsejoDepartamentalMusica).HasColumnType("longtext");
            entity.Property(x => x.MesaSectorialTerritorial).HasColumnType("longtext");
            entity.Property(x => x.Observaciones).HasColumnType("longtext");
        });

        ConfigureStagingEntity<ImportOpportunityStagingRow>(modelBuilder, "import_opportunity_staging_rows", entity =>
        {
            entity.Property(x => x.SituacionIdentificada).HasColumnType("longtext");
            entity.Property(x => x.ComponenteOtrasDependenciasEntidades).HasColumnType("longtext");
            entity.Property(x => x.AliadosYCreyentes).HasColumnType("longtext");
            entity.Property(x => x.TerritorioInfluencia).HasColumnType("longtext");
            entity.Property(x => x.PriorityLevelName).HasMaxLength(255);
            entity.Property(x => x.DescripcionAdicional).HasColumnType("longtext");
        });

        ConfigureStagingEntity<ImportPnmcAxisStagingRow>(modelBuilder, "import_pnmc_axis_staging_rows", entity =>
        {
            entity.Property(x => x.DescripcionHallazgo).HasColumnType("longtext");
            entity.Property(x => x.PnmcAxisName).HasMaxLength(255);
            entity.Property(x => x.PnmcComponentName).HasMaxLength(255);
            entity.Property(x => x.AccionEstrategica).HasColumnType("longtext");
            entity.Property(x => x.PoliticaPriorizada).HasColumnType("longtext");
            entity.Property(x => x.ArmonizacionPnc).HasColumnType("longtext");
            entity.Property(x => x.ArmonizacionPnd).HasColumnType("longtext");
            entity.Property(x => x.ArmonizacionInternacional).HasColumnType("longtext");
            entity.Property(x => x.PriorityLevelName).HasMaxLength(255);
            entity.Property(x => x.AliadosResponsables).HasColumnType("longtext");
            entity.Property(x => x.FuentesFinanciacion).HasColumnType("longtext");
            entity.Property(x => x.ValorPropuestaCop).HasPrecision(18, 2);
            entity.Property(x => x.EnfoquesRaw).HasColumnType("longtext");
            entity.Property(x => x.Descripcion).HasColumnType("longtext");
            entity.Property(x => x.ScheduleName).HasMaxLength(255);
            entity.Property(x => x.ProposalStatusName).HasMaxLength(255);
            entity.Property(x => x.Observaciones).HasColumnType("longtext");
        });

        ConfigureStagingEntity<ImportActorStagingRow>(modelBuilder, "import_actor_staging_rows", entity =>
        {
            entity.Property(x => x.NombreAgente).HasColumnType("longtext");
            entity.Property(x => x.AgentTypeName).HasMaxLength(255);
            entity.Property(x => x.EcosystemRolesRaw).HasColumnType("longtext");
            entity.Property(x => x.TerritorialLevelsRaw).HasColumnType("longtext");
            entity.Property(x => x.NumeroContacto).HasMaxLength(50);
            entity.Property(x => x.CorreoElectronico).HasMaxLength(200);
            entity.Property(x => x.Observaciones).HasColumnType("longtext");
        });

        ConfigureStagingEntity<ImportIndicatorStagingRow>(modelBuilder, "import_indicator_staging_rows", entity =>
        {
            entity.Property(x => x.DepartmentsRaw).HasColumnType("longtext");
            entity.Property(x => x.ActionName).HasColumnType("longtext");
            entity.Property(x => x.IndicatorName).HasColumnType("longtext");
            entity.Property(x => x.TargetRaw).HasMaxLength(50);
            entity.Property(x => x.MonthlyQuantitativeJson).HasColumnType("longtext");
            entity.Property(x => x.MonthlyDetailJson).HasColumnType("longtext");
            entity.Property(x => x.Fuente).HasColumnType("longtext");
            entity.Property(x => x.YearRaw).HasMaxLength(50);
            entity.Property(x => x.ObservacionesGenerales).HasColumnType("longtext");
        });

        ConfigureStagingEntity<ImportIndicatorDetailStagingRow>(modelBuilder, "import_indicator_detail_staging_rows", entity =>
        {
            entity.Property(x => x.DepartmentsRaw).HasColumnType("longtext");
            entity.Property(x => x.ActionName).HasColumnType("longtext");
            entity.Property(x => x.IndicatorName).HasColumnType("longtext");
            entity.Property(x => x.TargetRaw).HasMaxLength(50);
            entity.Property(x => x.FormulaLabel).HasColumnType("longtext");
            entity.Property(x => x.DetailDescription).HasColumnType("longtext");
            entity.Property(x => x.MonthsRaw).HasColumnType("longtext");
            entity.Property(x => x.CurrentValueRaw).HasMaxLength(50);
            entity.Property(x => x.Fuente).HasColumnType("longtext");
            entity.Property(x => x.YearRaw).HasMaxLength(50);
            entity.Property(x => x.Observaciones).HasColumnType("longtext");
        });
    }

    private static void ConfigureCatalogEntity<TEntity>(ModelBuilder modelBuilder, string tableName)
        where TEntity : class
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey("Id");
            entity.Property("Name").HasMaxLength(255).IsRequired();
            entity.Property("DisplayOrder").IsRequired();
            entity.Property("IsActive").IsRequired();
            entity.HasIndex("Name").IsUnique();
        });
    }

    private static void ConfigureStagingEntity<TEntity>(ModelBuilder modelBuilder, string tableName, Action<Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity>> configure)
        where TEntity : class
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey("Id");
            entity.Property("ImportBatchId");
            entity.Property("SourceRowNumber").IsRequired();
            entity.HasIndex("ImportBatchId");
            // La fila de trabajo pertenece al lote. Sin declarar la relación, EF Core no sabe que
            // esta tabla depende de import_batches y ordena los DELETE por nombre de tabla: el
            // lote se borraba antes que sus filas y la llave foránea de la base de datos rechazaba
            // la operación al eliminar una carga del historial.
            entity.HasOne<ImportBatch>()
                .WithMany()
                .HasForeignKey("ImportBatchId")
                .OnDelete(DeleteBehavior.Cascade);
            configure(entity);
        });
    }
}
