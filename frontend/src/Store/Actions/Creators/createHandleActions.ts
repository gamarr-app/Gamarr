import _ from 'lodash';
import { Action } from 'redux';
import { handleActions } from 'redux-actions';
import {
  CLEAR_PENDING_CHANGES,
  REMOVE_ITEM,
  SET,
  UPDATE,
  UPDATE_ITEM,
  UPDATE_SERVER_SIDE_COLLECTION,
} from 'Store/Actions/baseActions';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type State = Record<string, any>;

interface ItemWithId {
  id: number;
  [key: string]: unknown;
}

interface SectionState {
  items: ItemWithId[];
  itemMap?: Record<number, number>;
  item?: unknown;
  pendingChanges?: Record<string, unknown>;
  saveError?: unknown;
  [key: string]: unknown;
}

interface BasePayload {
  section: string;
  [key: string]: unknown;
}

interface UpdatePayload extends BasePayload {
  data: unknown;
}

interface UpdateItemPayload extends BasePayload {
  id: number;
  updateOnly?: boolean;
}

interface RemoveItemPayload extends BasePayload {
  id: number;
}

interface ServerSideCollectionPayload extends BasePayload {
  data: {
    records: ItemWithId[];
    totalRecords: number;
    pageSize: number;
    [key: string]: unknown;
  };
}

interface PayloadAction<T> extends Action {
  payload: T;
}

const omittedProperties = ['section', 'id'];

function createItemMap(data: ItemWithId[]): Record<number, number> {
  return data.reduce(
    (acc: Record<number, number>, d: ItemWithId, index: number) => {
      acc[d.id] = index;
      return acc;
    },
    {}
  );
}

export default function createHandleActions(
  handlers: Record<string, unknown>,
  defaultState: State,
  section: string
) {
  return handleActions(
    {
      [SET]: function (state: State, { payload }: PayloadAction<BasePayload>) {
        const payloadSection = payload.section;
        const [baseSection] = payloadSection.split('.');

        if (section === baseSection) {
          const newState = Object.assign(
            getSectionState(state, payloadSection),
            _.omit(payload, omittedProperties)
          );

          return updateSectionState(state, payloadSection, newState);
        }

        return state;
      },

      [UPDATE]: function (
        state: State,
        { payload }: PayloadAction<UpdatePayload>
      ) {
        const payloadSection = payload.section;
        const [baseSection] = payloadSection.split('.');

        if (section === baseSection) {
          const newState = getSectionState(
            state,
            payloadSection
          ) as SectionState;

          if (_.isArray(payload.data)) {
            newState.items = payload.data;
            newState.itemMap = createItemMap(payload.data);
          } else {
            newState.item = payload.data;
          }

          return updateSectionState(state, payloadSection, newState as State);
        }

        return state;
      },

      [UPDATE_ITEM]: function (
        state: State,
        { payload }: PayloadAction<UpdateItemPayload>
      ) {
        const {
          section: payloadSection,
          updateOnly = false,
          ...otherProps
        } = payload;

        const [baseSection] = payloadSection.split('.');

        if (section === baseSection) {
          const newState = getSectionState(
            state,
            payloadSection
          ) as SectionState;
          const items = newState.items;

          const itemMap = newState.itemMap ?? createItemMap(items);
          const index = payload.id in itemMap ? itemMap[payload.id] : -1;

          newState.items = [...items];

          if (index >= 0) {
            const item = items[index];
            const newItem = { ...item, ...otherProps };

            if (_.isEqual(item, newItem)) {
              return state;
            }

            newState.items.splice(index, 1, newItem);
          } else if (!updateOnly) {
            const newIndex =
              newState.items.push({ ...otherProps } as ItemWithId) - 1;

            newState.itemMap = { ...itemMap };
            newState.itemMap[payload.id] = newIndex;
          }

          return updateSectionState(state, payloadSection, newState as State);
        }

        return state;
      },

      [CLEAR_PENDING_CHANGES]: function (
        state: State,
        { payload }: PayloadAction<BasePayload>
      ) {
        const payloadSection = payload.section;
        const [baseSection] = payloadSection.split('.');

        if (section === baseSection) {
          const newState = getSectionState(
            state,
            payloadSection
          ) as SectionState;
          newState.pendingChanges = {};

          if (Object.prototype.hasOwnProperty.call(newState, 'saveError')) {
            newState.saveError = null;
          }

          return updateSectionState(state, payloadSection, newState as State);
        }

        return state;
      },

      [REMOVE_ITEM]: function (
        state: State,
        { payload }: PayloadAction<RemoveItemPayload>
      ) {
        const payloadSection = payload.section;
        const [baseSection] = payloadSection.split('.');

        if (section === baseSection) {
          const newState = getSectionState(
            state,
            payloadSection
          ) as SectionState;

          newState.items = [...newState.items];
          _.remove(newState.items, { id: payload.id });

          newState.itemMap = createItemMap(newState.items);

          return updateSectionState(state, payloadSection, newState as State);
        }

        return state;
      },

      [UPDATE_SERVER_SIDE_COLLECTION]: function (
        state: State,
        { payload }: PayloadAction<ServerSideCollectionPayload>
      ) {
        const payloadSection = payload.section;
        const [baseSection] = payloadSection.split('.');

        if (section === baseSection) {
          const data = payload.data;
          const newState = getSectionState(
            state,
            payloadSection
          ) as SectionState;

          const serverState = _.omit(data, ['records']);
          const calculatedState = {
            totalPages: Math.max(
              Math.ceil(data.totalRecords / data.pageSize),
              1
            ),
            items: data.records,
            itemMap: createItemMap(data.records),
          };

          return updateSectionState(
            state,
            payloadSection,
            Object.assign(newState, serverState, calculatedState) as State
          );
        }

        return state;
      },

      ...(handlers as Record<
        string,
        (state: State, action: PayloadAction<unknown>) => State
      >),
    } as Record<
      string,
      (state: State, action: PayloadAction<unknown>) => State
    >,
    defaultState
  );
}
