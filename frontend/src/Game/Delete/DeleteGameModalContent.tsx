import { useCallback, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import InlineMarkdown from 'Components/Markdown/InlineMarkdown';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { Statistics } from 'Game/Game';
import useGame from 'Game/useGame';
import { icons, inputTypes, kinds } from 'Helpers/Props';
import { deleteGame, setDeleteOption } from 'Store/Actions/gameActions';
import { CheckInputChanged } from 'typings/inputs';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './DeleteGameModalContent.css';

export interface DeleteGameModalContentProps {
  gameId: number;
  onModalClose: () => void;
}

function DeleteGameModalContent({
  gameId,
  onModalClose,
}: DeleteGameModalContentProps) {
  const dispatch = useDispatch();
  const {
    title,
    path,
    collection,
    statistics = {} as Statistics,
  } = useGame(gameId)!;
  const { addImportExclusion } = useSelector(
    (state: AppState) => state.games.deleteOptions
  );

  const { gameFileCount = 0, sizeOnDisk = 0 } = statistics;

  const [deleteFiles, setDeleteFiles] = useState(false);

  const handleDeleteFilesChange = useCallback(
    ({ value }: CheckInputChanged) => {
      setDeleteFiles(value);
    },
    []
  );

  const handleDeleteGameConfirmed = useCallback(() => {
    dispatch(
      deleteGame({
        id: gameId,
        collectionIgdbId: collection?.igdbId,
        deleteFiles,
        addImportExclusion,
      })
    );

    onModalClose();
  }, [
    gameId,
    collection,
    addImportExclusion,
    deleteFiles,
    dispatch,
    onModalClose,
  ]);

  const handleDeleteOptionChange = useCallback(
    ({ name, value }: CheckInputChanged) => {
      dispatch(setDeleteOption({ [name]: value }));
    },
    [dispatch]
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('DeleteHeader', { title })}</ModalHeader>

      <ModalBody>
        <div className={styles.pathContainer}>
          <Icon className={styles.pathIcon} name={icons.FOLDER} />

          {path}
        </div>

        <FormGroup>
          <FormLabel>{translate('AddListExclusion')}</FormLabel>

          <FormInputGroup
            type={inputTypes.CHECK}
            name="addImportExclusion"
            value={addImportExclusion}
            helpText={translate('AddListExclusionGameHelpText')}
            kind={kinds.DANGER}
            onChange={handleDeleteOptionChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>
            {gameFileCount === 0
              ? translate('DeleteGameFolder')
              : translate('DeleteGameFiles', { gameFileCount })}
          </FormLabel>

          <FormInputGroup
            type={inputTypes.CHECK}
            name="deleteFiles"
            value={deleteFiles}
            helpText={
              gameFileCount === 0
                ? translate('DeleteGameFolderHelpText')
                : translate('DeleteGameFilesHelpText')
            }
            kind={kinds.DANGER}
            onChange={handleDeleteFilesChange}
          />
        </FormGroup>

        {deleteFiles ? (
          <div className={styles.deleteFilesMessage}>
            <div>
              <InlineMarkdown
                data={translate('DeleteGameFolderConfirmation', { path })}
                blockClassName={styles.folderPath}
              />
            </div>

            {gameFileCount ? (
              <div className={styles.deleteCount}>
                {translate('DeleteGameFolderGameCount', {
                  gameFileCount,
                  size: formatBytes(sizeOnDisk),
                })}
              </div>
            ) : null}
          </div>
        ) : null}
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>

        <Button kind={kinds.DANGER} onPress={handleDeleteGameConfirmed}>
          {translate('Delete')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default DeleteGameModalContent;
