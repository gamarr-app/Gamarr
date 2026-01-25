import Modal from 'Components/Modal/Modal';
import AddDownloadClientModalContent from './AddDownloadClientModalContent';

interface AddDownloadClientModalProps {
  isOpen: boolean;
  onModalClose: (options?: { downloadClientSelected?: boolean }) => void;
}

function AddDownloadClientModal({
  isOpen,
  onModalClose,
}: AddDownloadClientModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <AddDownloadClientModalContent onModalClose={onModalClose} />
    </Modal>
  );
}

export default AddDownloadClientModal;
