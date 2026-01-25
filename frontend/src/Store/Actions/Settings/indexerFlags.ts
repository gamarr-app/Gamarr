import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import { createThunk } from 'Store/thunks';

const section = 'settings.indexerFlags';

export const FETCH_INDEXER_FLAGS = 'settings/indexerFlags/fetchIndexerFlags';

export const fetchIndexerFlags = createThunk(FETCH_INDEXER_FLAGS);

export default {
  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null,
    items: [],
  },

  actionHandlers: {
    [FETCH_INDEXER_FLAGS]: createFetchHandler(section, '/indexerFlag'),
  },

  reducers: {},
};
