import Modal from 'Components/Modal/Modal';
import AddNotificationModalContent from './AddNotificationModalContent';

interface AddNotificationModalProps {
  isOpen: boolean;
  onModalClose: (options?: { notificationSelected?: boolean }) => void;
}

function AddNotificationModal({
  isOpen,
  onModalClose,
}: AddNotificationModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <AddNotificationModalContent onModalClose={onModalClose} />
    </Modal>
  );
}

export default AddNotificationModal;
