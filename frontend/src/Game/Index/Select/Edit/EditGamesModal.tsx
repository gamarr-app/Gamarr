import Modal from 'Components/Modal/Modal';
import EditGamesModalContent from './EditGamesModalContent';

interface EditGamesModalProps {
  isOpen: boolean;
  gameIds: number[];
  onSavePress(payload: object): void;
  onModalClose(): void;
}

function EditGamesModal(props: EditGamesModalProps) {
  const { isOpen, gameIds, onSavePress, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <EditGamesModalContent
        gameIds={gameIds}
        onSavePress={onSavePress}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default EditGamesModal;
