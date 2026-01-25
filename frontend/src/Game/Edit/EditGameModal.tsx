import { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditGameModalContent, {
  EditGameModalContentProps,
} from './EditGameModalContent';

interface EditGameModalProps extends EditGameModalContentProps {
  isOpen: boolean;
}

function EditGameModal({
  isOpen,
  onModalClose,
  ...otherProps
}: EditGameModalProps) {
  const dispatch = useDispatch();

  const handleModalClose = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'games' }));
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal isOpen={isOpen} onModalClose={handleModalClose}>
      <EditGameModalContent {...otherProps} onModalClose={handleModalClose} />
    </Modal>
  );
}

export default EditGameModal;
