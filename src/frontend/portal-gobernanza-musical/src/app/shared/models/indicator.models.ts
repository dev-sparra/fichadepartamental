export interface IndicatorMonthlyProgress {
  id: string;
  monthOptionId: number;
  monthName: string;
  quantitativeAdvance: number | null;
  detail: string | null;
}

export interface IndicatorRecord {
  id: string;
  departmentId: number;
  departmentName: string;
  indicatorDefinitionId: number;
  indicatorName: string;
  actionName: string;
  targetValue: number;
  year: number;
  source: string | null;
  generalObservations: string | null;
  currentValueCalculated: number;
  compliancePercentageCalculated: number;
  monthlyProgresses: IndicatorMonthlyProgress[];
}

export interface UpdateIndicatorMonthlyProgressRequest {
  quantitativeAdvance: number | null;
  detail: string | null;
}

export interface UpdateIndicatorRecordRequest {
  source: string | null;
  generalObservations: string | null;
  monthlyProgresses: UpdateIndicatorMonthlyProgressRequest[];
}

export interface IndicatorDetailRecord {
  id: string;
  departmentId: number;
  departmentName: string;
  indicatorDefinitionId: number;
  indicatorName: string;
  indicatorDetailTemplateId: number;
  formulaLabel: string;
  detailDescription: string;
  year: number;
  source: string | null;
  observations: string | null;
  currentValueCalculated: number | null;
  selectedMonthIds: number[];
}

export interface UpdateIndicatorDetailRecordRequest {
  source: string | null;
  observations: string | null;
  selectedMonthIds: number[];
}

export interface IndicatorWorksheet {
  records: IndicatorRecord[];
  details: IndicatorDetailRecord[];
}
