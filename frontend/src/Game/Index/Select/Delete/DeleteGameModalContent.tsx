import { orderBy } from 'lodash';
import React, { useCallback, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import Game from 'Game/Game';
import { bulkDeleteGame, setDeleteOption } from 'Store/Actions/gameActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import { InputChanged } from 'typings/inputs';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './DeleteGameModalContent.css';

interface DeleteGameModalContentProps {
  gameIds: number[];
  onModalClose(): void;
}

const selectDeleteOptions = createSelector(
  (state: AppState) => state.games.deleteOptions,
  (deleteOptions) => deleteOptions
);

function DeleteGameModalContent(props: DeleteGameModalContentProps) {
  const { gameIds, onModalClose } = props;

  const { addImportExclusion } = useSelector(selectDeleteOptions);
  const allGames: Game[] = useSelector(createAllGamesSelector());
  const dispatch = useDispatch();

  const [deleteFiles, setDeleteFiles] = useState(false);

  const games = useMemo((): Game[] => {
    const games = gameIds.map((id) => {
      return allGames.find((s) => s.id === id);
    }) as Game[];

    return orderBy(games, ['sortTitle']);
  }, [gameIds, allGames]);

  const onDeleteFilesChange = useCallback(
    ({ value }: InputChanged<boolean>) => {
      setDeleteFiles(value);
    },
    [setDeleteFiles]
  );

  const onDeleteOptionChange = useCallback(
    ({ name, value }: { name: string; value: boolean }) => {
      dispatch(
        setDeleteOption({
          [name]: value,
        })
      );
    },
    [dispatch]
  );

  const onDeleteGamesConfirmed = useCallback(() => {
    setDeleteFiles(false);

    dispatch(
      bulkDeleteGame({
        gameIds,
        deleteFiles,
        addImportExclusion,
      })
    );

    onModalClose();
  }, [
    gameIds,
    deleteFiles,
    addImportExclusion,
    setDeleteFiles,
    dispatch,
    onModalClose,
  ]);

  const { totalGameFileCount, totalSizeOnDisk } = useMemo(() => {
    return games.reduce(
      (acc, { statistics = {} }) => {
        const { gameFileCount = 0, sizeOnDisk = 0 } = statistics;

        acc.totalGameFileCount += gameFileCount;
        acc.totalSizeOnDisk += sizeOnDisk;

        return acc;
      },
      {
        totalGameFileCount: 0,
        totalSizeOnDisk: 0,
      }
    );
  }, [games]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {games.length > 1
          ? translate('DeleteSelectedGames')
          : translate('DeleteSelectedGame')}
      </ModalHeader>

      <ModalBody>
        <div>
          <FormGroup>
            <FormLabel>{translate('AddListExclusion')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="addImportExclusion"
              value={addImportExclusion}
              helpText={translate('AddListExclusionGameHelpText')}
              onChange={onDeleteOptionChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>
              {games.length > 1
                ? translate('DeleteGameFolders')
                : translate('DeleteGameFolder')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="deleteFiles"
              value={deleteFiles}
              helpText={
                games.length > 1
                  ? translate('DeleteGameFoldersHelpText')
                  : translate('DeleteGameFolderHelpText')
              }
              kind="danger"
              onChange={onDeleteFilesChange}
            />
          </FormGroup>
        </div>

        <div className={styles.message}>
          {deleteFiles
            ? translate('DeleteGameFolderCountWithFilesConfirmation', {
                count: games.length,
              })
            : translate('DeleteGameFolderCountConfirmation', {
                count: games.length,
              })}
        </div>

        <ul>
          {games.map(({ title, path, statistics = {} }) => {
            const { gameFileCount = 0, sizeOnDisk = 0 } = statistics;

            return (
              <li key={title}>
                <span>{title}</span>

                {deleteFiles && (
                  <span>
                    <span className={styles.pathContainer}>
                      -<span className={styles.path}>{path}</span>
                    </span>

                    {!!gameFileCount && (
                      <span className={styles.statistics}>
                        (
                        {translate('DeleteGameFolderGameCount', {
                          gameFileCount,
                          size: formatBytes(sizeOnDisk),
                        })}
                        )
                      </span>
                    )}
                  </span>
                )}
              </li>
            );
          })}
        </ul>

        {deleteFiles && !!totalGameFileCount ? (
          <div className={styles.deleteFilesMessage}>
            {translate('DeleteGameFolderGameCount', {
              gameFileCount: totalGameFileCount,
              size: formatBytes(totalSizeOnDisk),
            })}
          </div>
        ) : null}
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <Button kind={kinds.DANGER} onPress={onDeleteGamesConfirmed}>
          {translate('Delete')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default DeleteGameModalContent;
