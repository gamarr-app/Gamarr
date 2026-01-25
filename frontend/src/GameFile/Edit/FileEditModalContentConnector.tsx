import React, { useCallback, useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Language from 'Language/Language';
import Quality from 'Quality/Quality';
import { updateGameFiles } from 'Store/Actions/gameFileActions';
import { fetchQualityProfileSchema } from 'Store/Actions/settingsActions';
import getQualities from 'Utilities/Quality/getQualities';
import FileEditModalContent, {
  FileEditSavePayload,
} from './FileEditModalContent';

interface FileEditModalContentConnectorProps {
  gameFileId: number;
  onModalClose: (saved?: boolean) => void;
}

function createMapStateToProps(gameFileId: number) {
  return createSelector(
    (state: AppState) => state.gameFiles,
    (state: AppState) => state.settings.qualityProfiles,
    (state: AppState) => state.settings.languages,
    (gameFiles, qualityProfiles, languages) => {
      const gameFile = gameFiles.items.find((f) => f.id === gameFileId);

      const filterItems = ['Any', 'Original'];
      const filteredLanguages = languages.items.filter(
        (lang) => !filterItems.includes(lang.name)
      );

      const quality = gameFile?.quality;

      return {
        isFetching: qualityProfiles.isSchemaFetching || languages.isFetching,
        isPopulated: qualityProfiles.isSchemaPopulated && languages.isPopulated,
        error: qualityProfiles.schemaError || languages.error,
        qualityId: quality ? quality.quality.id : 0,
        qualities: getQualities(qualityProfiles.schema?.items ?? []),
        languageIds: gameFile?.languages
          ? gameFile.languages.map((l) => l.id)
          : [],
        languages: filteredLanguages,
        indexerFlags: gameFile?.indexerFlags ?? 0,
        edition: gameFile?.edition ?? '',
        releaseGroup: gameFile?.releaseGroup ?? '',
        relativePath: gameFile?.relativePath ?? '',
      };
    }
  );
}

function FileEditModalContentConnector(
  props: FileEditModalContentConnectorProps
) {
  const { gameFileId, onModalClose } = props;
  const dispatch = useDispatch();

  const selector = useMemo(
    () => createMapStateToProps(gameFileId),
    [gameFileId]
  );
  const {
    isFetching,
    isPopulated,
    error,
    qualityId,
    qualities,
    languageIds,
    languages,
    indexerFlags,
    edition,
    releaseGroup,
    relativePath,
  } = useSelector(selector);

  useEffect(() => {
    if (!isPopulated) {
      dispatch(fetchQualityProfileSchema());
    }
  }, [dispatch, isPopulated]);

  const onSaveInputs = useCallback(
    (payload: FileEditSavePayload) => {
      const {
        qualityId: payloadQualityId,
        languageIds: payloadLanguageIds,
        edition: payloadEdition,
        releaseGroup: payloadReleaseGroup,
        indexerFlags: payloadIndexerFlags,
      } = payload;

      const quality = qualities.find(
        (item: Quality) => item.id === payloadQualityId
      );

      const selectedLanguages: Language[] = [];

      payloadLanguageIds.forEach((languageId: number) => {
        const language = languages.find(
          (item) => item.id === parseInt(String(languageId))
        );

        if (language !== undefined) {
          selectedLanguages.push(language);
        }
      });

      const revision = {
        version: 1,
        real: 0,
      };

      dispatch(
        updateGameFiles({
          files: [
            {
              id: gameFileId,
              languages: selectedLanguages,
              indexerFlags: payloadIndexerFlags,
              edition: payloadEdition,
              releaseGroup: payloadReleaseGroup,
              quality: {
                quality,
                revision,
              },
            },
          ],
        })
      );

      onModalClose(true);
    },
    [dispatch, gameFileId, qualities, languages, onModalClose]
  );

  return (
    <FileEditModalContent
      isFetching={isFetching}
      isPopulated={isPopulated}
      error={error}
      qualityId={qualityId}
      qualities={qualities}
      languageIds={languageIds}
      languages={languages}
      indexerFlags={indexerFlags}
      edition={edition}
      releaseGroup={releaseGroup}
      relativePath={relativePath}
      onSaveInputs={onSaveInputs}
      onModalClose={onModalClose}
    />
  );
}

export default FileEditModalContentConnector;
