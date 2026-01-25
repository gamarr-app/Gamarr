import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import { createThunk } from 'Store/thunks';

//
// Variables

const section = 'settings.languages';

//
// Actions Types

export const FETCH_LANGUAGES = 'settings/languages/fetchLanguages';

//
// Action Creators

export const fetchLanguages = createThunk(FETCH_LANGUAGES);

//
// Details

interface Language {
  id: number;
  name: string;
}

export default {
  //
  // State

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null as unknown,
    items: [] as Language[],
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_LANGUAGES]: createFetchHandler(section, '/language'),
  },

  //
  // Reducers

  reducers: {},
};
