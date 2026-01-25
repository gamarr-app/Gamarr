import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import ExcludeGameModalContentConnector from './ExcludeGameModalContentConnector';

interface ExcludeGameModalProps {
  isOpen: boolean;
  onModalClose: (didExclude?: boolean) => void;
  igdbId: number;
  title: string;
  year?: number;
}

function ExcludeGameModal({
  isOpen,
  onModalClose,
  ...otherProps
}: ExcludeGameModalProps) {
  return (
    <Modal isOpen={isOpen} size={sizes.MEDIUM} onModalClose={onModalClose}>
      <ExcludeGameModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default ExcludeGameModal;
