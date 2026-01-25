import _ from 'lodash';
import React from 'react';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import IconButton from 'Components/Link/IconButton';
import gameEntities from 'Game/gameEntities';
import { icons, sortDirections } from 'Helpers/Props';
import createSetClientSideCollectionSortReducer from 'Store/Actions/Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetTableOptionReducer from 'Store/Actions/Creators/Reducers/createSetTableOptionReducer';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import { removeItem, set, updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';

//
// Variables

export const section = 'gameFiles';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isDeleting: false,
  deleteError: null,
  isSaving: false,
  saveError: null,
  sortKey: 'relativePath',
  sortDirection: sortDirections.ASCENDING,
  items: [],

  columns: [
    {
      name: 'relativePath',
      label: () => translate('RelativePath'),
      isVisible: true,
      isSortable: true
    },
    {
      name: 'size',
      label: () => translate('Size'),
      isVisible: true,
      isSortable: true
    },
    {
      name: 'languages',
      label: () => translate('Languages'),
      isVisible: true
    },
    {
      name: 'releaseGroup',
      label: () => translate('ReleaseGroup'),
      isVisible: true
    },
    {
      name: 'version',
      label: () => translate('Version'),
      isVisible: true
    },
    {
      name: 'dateAdded',
      label: () => translate('Added'),
      isVisible: false,
      isSortable: true
    },
    {
      name: 'actions',
      columnLabel: () => translate('Actions'),
      label: React.createElement(IconButton, { name: icons.ADVANCED_SETTINGS }),
      isVisible: true,
      isModifiable: false
    }
  ]
};

export const persistState = [
  'gameFiles.columns',
  'gameFiles.sortDirection',
  'gameFiles.sortKey'
];

//
// Actions Types

export const FETCH_GAME_FILES = 'gameFiles/fetchGameFiles';
export const DELETE_GAME_FILE = 'gameFiles/deleteGameFile';
export const DELETE_GAME_FILES = 'gameFiles/deleteGameFiles';
export const UPDATE_GAME_FILES = 'gameFiles/updateGameFiles';
export const CLEAR_GAME_FILES = 'gameFiles/clearGameFiles';
export const SET_GAME_FILES_SORT = 'gameFiles/setGameFilesSort';
export const SET_GAME_FILES_TABLE_OPTION = 'gameFiles/setGameFilesTableOption';

//
// Action Creators

export const fetchGameFiles = createThunk(FETCH_GAME_FILES);
export const deleteGameFile = createThunk(DELETE_GAME_FILE);
export const deleteGameFiles = createThunk(DELETE_GAME_FILES);
export const updateGameFiles = createThunk(UPDATE_GAME_FILES);
export const clearGameFiles = createAction(CLEAR_GAME_FILES);
export const setGameFilesSort = createAction(SET_GAME_FILES_SORT);
export const setGameFilesTableOption = createAction(SET_GAME_FILES_TABLE_OPTION);

//
// Helpers

const deleteGameFileHelper = createRemoveItemHandler(section, '/gameFile');

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_GAME_FILES]: createFetchHandler(section, '/gameFile'),

  [DELETE_GAME_FILE]: function(getState, payload, dispatch) {
    const {
      id: gameFileId,
      gameEntity = gameEntities.GAMES
    } = payload;

    const gameSection = _.last(gameEntity.split('.'));
    const deletePromise = deleteGameFileHelper(getState, payload, dispatch);

    deletePromise.done(() => {
      const games = getState().games.items;
      const gamesWithRemovedFiles = _.filter(games, { gameFileId });

      dispatch(batchActions([
        ...gamesWithRemovedFiles.map((game) => {
          return updateItem({
            section: gameSection,
            ...game,
            gameFileId: 0,
            hasFile: false
          });
        })
      ]));
    });
  },

  [DELETE_GAME_FILES]: function(getState, payload, dispatch) {
    const {
      gameFileIds
    } = payload;

    dispatch(set({ section, isDeleting: true }));

    const promise = createAjaxRequest({
      url: '/gameFile/bulk',
      method: 'DELETE',
      dataType: 'json',
      data: JSON.stringify({ gameFileIds })
    }).request;

    promise.done(() => {
      const games = getState().games.items;
      const gamesWithRemovedFiles = gameFileIds.reduce((acc, gameFileId) => {
        acc.push(..._.filter(games, { gameFileId }));

        return acc;
      }, []);

      dispatch(batchActions([
        ...gameFileIds.map((id) => {
          return removeItem({ section, id });
        }),

        ...gamesWithRemovedFiles.map((game) => {
          return updateItem({
            section: 'games',
            ...game,
            gameFileId: 0,
            hasFile: false
          });
        }),

        set({
          section,
          isDeleting: false,
          deleteError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isDeleting: false,
        deleteError: xhr
      }));
    });
  },

  [UPDATE_GAME_FILES]: function(getState, payload, dispatch) {
    const { files } = payload;

    dispatch(set({ section, isSaving: true }));

    const requestData = files;

    const promise = createAjaxRequest({
      url: '/gameFile/bulk',
      method: 'PUT',
      dataType: 'json',
      data: JSON.stringify(requestData)
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        ...files.map((file) => {
          const id = file.id;
          const props = {};
          const gameFile = data.find((f) => f.id === id);

          props.qualityCutoffNotMet = gameFile.qualityCutoffNotMet;
          props.customFormats = gameFile.customFormats;
          props.customFormatScore = gameFile.customFormatScore;
          props.languages = gameFile.languages;
          props.quality = gameFile.quality;
          props.edition = gameFile.edition;
          props.releaseGroup = gameFile.releaseGroup;
          props.indexerFlags = gameFile.indexerFlags;

          return updateItem({
            section,
            id,
            ...props
          });
        }),

        set({
          section,
          isSaving: false,
          saveError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_GAME_FILES_TABLE_OPTION]: createSetTableOptionReducer(section),

  [CLEAR_GAME_FILES]: (state) => {
    return Object.assign({}, state, {
      isFetching: false,
      isPopulated: false,
      error: null,
      isDeleting: false,
      deleteError: null,
      isSaving: false,
      saveError: null,
      items: []
    });
  },

  [SET_GAME_FILES_SORT]: createSetClientSideCollectionSortReducer(section)

}, defaultState, section);
