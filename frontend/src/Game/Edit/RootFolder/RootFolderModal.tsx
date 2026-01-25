import Modal from 'Components/Modal/Modal';
import RootFolderModalContent, {
  RootFolderModalContentProps,
} from './RootFolderModalContent';

interface RootFolderModalProps extends RootFolderModalContentProps {
  isOpen: boolean;
}

function RootFolderModal({
  isOpen,
  rootFolderPath,
  gameId,
  onSavePress,
  onModalClose,
}: RootFolderModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <RootFolderModalContent
        gameId={gameId}
        rootFolderPath={rootFolderPath}
        onSavePress={onSavePress}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default RootFolderModal;
