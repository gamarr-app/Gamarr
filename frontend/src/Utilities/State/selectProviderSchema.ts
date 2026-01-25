import _ from 'lodash';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

interface Schema {
  implementation: string;
  name?: string;
  presets?: Schema[];
  [key: string]: unknown;
}

interface SectionState {
  schema: Schema[];
  selectedSchema?: Schema;
  [key: string]: unknown;
}

interface Payload {
  implementation: string;
  presetName?: string;
}

type SchemaDefaults = Record<string, unknown> | ((schema: Schema) => Schema);

function applySchemaDefaults(
  selectedSchema: Schema,
  schemaDefaults?: SchemaDefaults
): Schema {
  if (!schemaDefaults) {
    return selectedSchema;
  } else if (_.isFunction(schemaDefaults)) {
    return schemaDefaults(selectedSchema);
  }

  return Object.assign(selectedSchema, schemaDefaults);
}

function selectProviderSchema<T extends object>(
  state: T,
  section: string,
  payload: Payload,
  schemaDefaults?: SchemaDefaults
): T {
  const newState = getSectionState(state, section) as SectionState;

  const { implementation, presetName } = payload;

  const selectedImplementation = _.find(newState.schema, { implementation });

  const selectedSchema = presetName
    ? _.find(selectedImplementation?.presets, { name: presetName })
    : selectedImplementation;

  newState.selectedSchema = applySchemaDefaults(
    _.cloneDeep(selectedSchema) as Schema,
    schemaDefaults
  );

  return updateSectionState(state, section, newState);
}

export default selectProviderSchema;
