import Modal from 'Components/Modal/Modal';
import SelectVersionModalContent from './SelectVersionModalContent';

interface SelectVersionModalProps {
  isOpen: boolean;
  version: string;
  modalTitle: string;
  onVersionSelect(version: string): void;
  onModalClose(): void;
}

function SelectVersionModal(props: SelectVersionModalProps) {
  const { isOpen, ...otherProps } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={otherProps.onModalClose}>
      <SelectVersionModalContent {...otherProps} />
    </Modal>
  );
}

export default SelectVersionModal;
