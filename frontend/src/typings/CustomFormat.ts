import ModelBase from 'App/ModelBase';

export interface QualityProfileFormatItem {
  format: number;
  name: string;
  score: number;
}

export interface CustomFormatSpecification {
  id: number;
  name: string;
  implementation: string;
  implementationName: string;
  negate: boolean;
  required: boolean;
  fields: Array<{ value: unknown }>;
}

interface CustomFormat extends ModelBase {
  name: string;
  includeCustomFormatWhenRenaming: boolean;
  specifications: CustomFormatSpecification[];
}

export default CustomFormat;
