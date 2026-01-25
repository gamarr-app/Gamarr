import _ from 'lodash';
import getSectionState from 'Utilities/State/getSectionState';

interface Field {
  name: string;
  value: unknown;
  [key: string]: unknown;
}

interface KeyValueField {
  name: string;
  value: unknown;
}

interface ProviderItem {
  id?: number;
  fields?: Field[];
  presets?: unknown[];
  [key: string]: unknown;
}

interface SectionState {
  items: ProviderItem[];
  pendingChanges: {
    fields?: Record<string, unknown>;
    [key: string]: unknown;
  };
  selectedSchema?: ProviderItem;
  schema?: ProviderItem;
  [key: string]: unknown;
}

interface Payload {
  id?: number;
  [key: string]: unknown;
}

type GetState = () => Record<string, unknown>;

function getProviderState(
  payload: Payload,
  getState: GetState,
  section: string,
  keyValueOnly: boolean = true
): Record<string, unknown> {
  const { id, ...otherPayload } = payload;

  const state = getSectionState(getState(), section, true) as SectionState;
  const pendingChanges: Record<string, unknown> = Object.assign(
    {},
    state.pendingChanges,
    otherPayload
  );
  const pendingFields = (state.pendingChanges.fields || {}) as Record<
    string,
    unknown
  >;
  delete pendingChanges.fields;

  const item: ProviderItem = id
    ? _.find(state.items, { id }) || {}
    : state.selectedSchema || state.schema || {};

  if (item.fields) {
    pendingChanges.fields = _.reduce(
      item.fields,
      (result: (Field | KeyValueField)[], field: Field) => {
        const name = field.name;

        const value = pendingFields.hasOwnProperty(name)
          ? pendingFields[name]
          : field.value;

        if (keyValueOnly) {
          result.push({
            name,
            value,
          });
        } else {
          result.push({
            ...field,
            value,
          });
        }

        return result;
      },
      []
    );
  }

  const result: Record<string, unknown> = Object.assign(
    {},
    item,
    pendingChanges
  );

  delete result.presets;

  return result;
}

export default getProviderState;
