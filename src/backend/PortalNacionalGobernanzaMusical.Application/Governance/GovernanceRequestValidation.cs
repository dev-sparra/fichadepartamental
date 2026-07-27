using PortalNacionalGobernanzaMusical.Application.Common;

namespace PortalNacionalGobernanzaMusical.Application.Governance;

/// <summary>
/// Validación de los datos de la ficha por tipo de captura, con las mismas reglas que aplica el
/// formulario Angular (fecha, obligatorios, correo, celular de 10 dígitos, valores en pesos y
/// longitudes). Garantiza la integridad aunque la petición no venga del formulario del portal.
/// <para>Las etiquetas de los campos son las de la Ficha Departamental para que el mensaje que ve
/// el usuario coincida exactamente con lo que aparece en pantalla.</para>
/// </summary>
public static class GovernanceRequestValidation
{
    private const string FichaSummary = "Revisa los datos de identificación de la ficha antes de guardar.";
    private const string ActorsSummary = "Revisa la información de los actores antes de guardar.";
    private const string AxesSummary = "Revisa la información de los ejes PNMC antes de guardar.";

    public static void EnsureValid(UpdateGovernanceFichaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<FieldValidationError>();

        if (!PortalFieldRules.IsWithinCaptureDateRange(request.FechaLevantamiento))
        {
            errors.Add(new FieldValidationError(
                nameof(request.FechaLevantamiento),
                "Fecha de levantamiento",
                $"Ingresa una fecha entre el {PortalFieldRules.MinCaptureDate:dd/MM/yyyy} y el {PortalFieldRules.MaxCaptureDate:dd/MM/yyyy}."));
        }

        if (request.DepartmentId <= 0)
        {
            errors.Add(new FieldValidationError(
                nameof(request.DepartmentId),
                "Departamento",
                "Selecciona un departamento de la lista."));
        }

        if (!PortalFieldRules.IsRequiredTextPresent(request.ResponsableRegistro))
        {
            errors.Add(new FieldValidationError(
                nameof(request.ResponsableRegistro),
                "Responsable del registro (Gestor)",
                "Escribe el nombre de la persona que diligencia la ficha."));
        }
        else if (!PortalFieldRules.IsWithinLength(request.ResponsableRegistro, PortalFieldRules.ShortTextMaxLength))
        {
            errors.Add(new FieldValidationError(
                nameof(request.ResponsableRegistro),
                "Responsable del registro (Gestor)",
                $"El nombre no puede superar {PortalFieldRules.ShortTextMaxLength} caracteres."));
        }

        AddLongTextError(errors, nameof(request.Observaciones), "Observaciones", request.Observaciones);

        DomainValidationException.ThrowIfAny(FichaSummary, errors);
    }

    public static void EnsureValid(IEnumerable<GovernanceActorDto> actors)
    {
        ArgumentNullException.ThrowIfNull(actors);

        var errors = new List<FieldValidationError>();
        var position = 0;

        foreach (var actor in actors)
        {
            position++;

            if (!PortalFieldRules.IsRequiredTextPresent(actor.NombreAgente))
            {
                errors.Add(new FieldValidationError(
                    $"actors[{position - 1}].nombreAgente",
                    $"Actor {position} · Nombre del agente (creyente)",
                    "Escribe el nombre del agente."));
            }

            // El Rol en el ecosistema depende del Tipo de agente: o se diligencian los dos, o
            // ninguno. Guardar solo uno deja el actor sin clasificar en el ecosistema.
            var hasAgentType = actor.AgentTypeId is > 0;
            var hasRoles = actor.EcosystemRoleIds.Count > 0;

            if (hasAgentType && !hasRoles)
            {
                errors.Add(new FieldValidationError(
                    $"actors[{position - 1}].ecosystemRoleIds",
                    $"Actor {position} · Rol en el ecosistema",
                    "Selecciona al menos un rol de la lista, que se filtra según el tipo de agente elegido."));
            }
            else if (hasRoles && !hasAgentType)
            {
                errors.Add(new FieldValidationError(
                    $"actors[{position - 1}].agentTypeId",
                    $"Actor {position} · Tipo de agente (categoría)",
                    "Selecciona el tipo de agente: de él depende la lista de roles del ecosistema."));
            }

            if (!PortalFieldRules.IsMobilePhone(actor.NumeroContacto))
            {
                errors.Add(new FieldValidationError(
                    $"actors[{position - 1}].numeroContacto",
                    $"Actor {position} · Número de contacto",
                    $"El número de contacto debe tener exactamente {PortalFieldRules.MobilePhoneDigits} dígitos, sin espacios ni guiones."));
            }

            if (!PortalFieldRules.IsEmail(actor.CorreoElectronico))
            {
                errors.Add(new FieldValidationError(
                    $"actors[{position - 1}].correoElectronico",
                    $"Actor {position} · Correo electrónico",
                    "Ingresa un correo con el formato usuario@dominio.com."));
            }

            AddLongTextError(errors, $"actors[{position - 1}].observaciones", $"Actor {position} · Observaciones", actor.Observaciones);
        }

        DomainValidationException.ThrowIfAny(ActorsSummary, errors);
    }

    public static void EnsureValid(IEnumerable<GovernancePnmcAxisDto> axes)
    {
        ArgumentNullException.ThrowIfNull(axes);

        var errors = new List<FieldValidationError>();
        var position = 0;

        foreach (var axis in axes)
        {
            position++;

            if (!PortalFieldRules.IsCopAmount(axis.ValorPropuestaCop))
            {
                errors.Add(new FieldValidationError(
                    $"pnmcAxes[{position - 1}].valorPropuestaCop",
                    $"Eje PNMC {position} · Valor de la propuesta (COP)",
                    "Ingresa un valor en pesos mayor o igual a cero, sin puntos ni símbolos."));
            }

            // El Componente PNMC depende del Eje: o se diligencian los dos, o ninguno. Un eje sin
            // componente deja el hallazgo sin ubicar dentro del Plan Nacional de Música.
            var hasAxis = axis.PnmcAxisId is > 0;
            var hasComponent = axis.PnmcComponentId is > 0;

            if (hasAxis && !hasComponent)
            {
                errors.Add(new FieldValidationError(
                    $"pnmcAxes[{position - 1}].pnmcComponentId",
                    $"Eje PNMC {position} · Componente PNMC",
                    "Selecciona el componente de la lista, que se filtra según el eje PNMC elegido."));
            }
            else if (hasComponent && !hasAxis)
            {
                errors.Add(new FieldValidationError(
                    $"pnmcAxes[{position - 1}].pnmcAxisId",
                    $"Eje PNMC {position} · Eje PNMC",
                    "Selecciona el eje PNMC: de él depende la lista de componentes."));
            }

            AddLongTextError(errors, $"pnmcAxes[{position - 1}].observaciones", $"Eje PNMC {position} · Observaciones", axis.Observaciones);
        }

        DomainValidationException.ThrowIfAny(AxesSummary, errors);
    }

    private static void AddLongTextError(ICollection<FieldValidationError> errors, string field, string label, string? value)
    {
        if (!PortalFieldRules.IsWithinLength(value, PortalFieldRules.LongTextMaxLength))
        {
            errors.Add(new FieldValidationError(
                field,
                label,
                $"El texto no puede superar {PortalFieldRules.LongTextMaxLength} caracteres."));
        }
    }
}
