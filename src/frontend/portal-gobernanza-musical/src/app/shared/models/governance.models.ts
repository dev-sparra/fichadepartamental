export interface GovernanceFichaSummary {
  id: string;
  departmentName: string;
  fechaLevantamiento: string;
  responsableRegistro: string;
  regionOcadName: string | null;
  municipalityName: string | null;
  opportunitiesCount: number;
  axesCount: number;
  actorsCount: number;
  /** Estado de revisión que envía el backend: Borrador, Aprobado o Devuelto. */
  approvalStatus: string;
}

export interface GovernanceFichaDetail {
  id: string;
  fechaLevantamiento: string;
  departmentId: number;
  municipalityId: number | null;
  responsableRegistro: string;
  regionOcadOptionId: number | null;
  observaciones: string | null;
  informationSourceIds: number[];
}

export interface UpdateGovernanceFichaRequest {
  fechaLevantamiento: string;
  departmentId: number;
  municipalityId: number | null;
  responsableRegistro: string;
  regionOcadOptionId: number | null;
  observaciones: string | null;
  informationSourceIds: number[];
}

export interface GovernanceDiagnostic {
  id: string | null;
  caracterizacionGeneral: string | null;
  fortalezasIdentificadas: string | null;
  politicasPriorizadas: string | null;
  debilidadesIdentificadas: string | null;
  tensionesOConflictos: string | null;
  committeeStatusOptionId: number | null;
  planDepartamentalCulturaStatusId: number | null;
  consejoDepartamentalCultura: string | null;
  planDepartamentalMusicaStatusId: number | null;
  ordenanzasCulturales: string | null;
  consejoDepartamentalMusica: string | null;
  mesaSectorialTerritorial: string | null;
  observaciones: string | null;
}

export interface GovernanceOpportunity {
  id: string | null;
  situacionIdentificada: string | null;
  componenteOtrasDependenciasEntidades: string | null;
  aliadosYCreyentes: string | null;
  territorioInfluencia: string | null;
  priorityLevelOptionId: number | null;
  descripcionAdicional: string | null;
}

export interface GovernancePnmcAxis {
  id: string | null;
  descripcionHallazgo: string | null;
  pnmcAxisId: number | null;
  pnmcComponentId: number | null;
  accionEstrategica: string | null;
  politicaPriorizada: string | null;
  armonizacionPnc: string | null;
  armonizacionPnd: string | null;
  armonizacionInternacional: string | null;
  priorityLevelOptionId: number | null;
  aliadosResponsables: string | null;
  fuentesFinanciacion: string | null;
  valorPropuestaCop: number | null;
  approachOptionIds: number[];
  descripcion: string | null;
  scheduleOptionId: number | null;
  proposalStatusOptionId: number | null;
  observaciones: string | null;
}

export interface GovernanceActor {
  id: string | null;
  nombreAgente: string;
  agentTypeId: number | null;
  ecosystemRoleIds: number[];
  territorialLevelOptionIds: number[];
  numeroContacto: string | null;
  correoElectronico: string | null;
  observaciones: string | null;
}
