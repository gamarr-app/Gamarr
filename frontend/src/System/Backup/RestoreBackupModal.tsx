import Modal from 'Components/Modal/Modal';
import RestoreBackupModalContentConnector from './RestoreBackupModalContentConnector';

interface RestoreBackupModalProps {
  isOpen: boolean;
  id?: number;
  name?: string;
  onModalClose: () => void;
}

function RestoreBackupModal(props: RestoreBackupModalProps) {
  const { isOpen, onModalClose, ...otherProps } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <RestoreBackupModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default RestoreBackupModal;
