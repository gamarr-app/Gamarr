import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './MoveGameModal.css';

interface MoveGameModalProps {
  originalPath?: string;
  destinationPath?: string;
  destinationRootFolder?: string;
  isOpen: boolean;
  onModalClose: () => void;
  onSavePress: () => void;
  onMoveGamePress: () => void;
}

function MoveGameModal({
  originalPath,
  destinationPath,
  destinationRootFolder,
  isOpen,
  onModalClose,
  onSavePress,
  onMoveGamePress,
}: MoveGameModalProps) {
  if (isOpen && !originalPath && !destinationPath && !destinationRootFolder) {
    console.error(
      'originalPath and destinationPath OR destinationRootFolder must be provided'
    );
  }

  return (
    <Modal
      isOpen={isOpen}
      size={sizes.MEDIUM}
      closeOnBackgroundClick={false}
      onModalClose={onModalClose}
    >
      <ModalContent showCloseButton={true} onModalClose={onModalClose}>
        <ModalHeader>{translate('MoveFiles')}</ModalHeader>

        <ModalBody>
          {destinationRootFolder
            ? translate('MoveGameFoldersToRootFolder', {
                destinationRootFolder,
              })
            : null}

          {originalPath && destinationPath
            ? translate('MoveGameFoldersToNewPath', {
                originalPath,
                destinationPath,
              })
            : null}

          {destinationRootFolder ? (
            <div className={styles.folderRenameMessage}>
              {translate('MoveGameFoldersRenameFolderWarning')}
            </div>
          ) : null}
        </ModalBody>

        <ModalFooter>
          <Button className={styles.doNotMoveButton} onPress={onSavePress}>
            {translate('MoveGameFoldersDontMoveFiles')}
          </Button>

          <Button kind={kinds.DANGER} onPress={onMoveGamePress}>
            {translate('MoveGameFoldersMoveFiles')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default MoveGameModal;
