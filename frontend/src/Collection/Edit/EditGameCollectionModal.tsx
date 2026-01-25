import { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditGameCollectionModalContent, {
  EditGameCollectionModalContentProps,
} from './EditGameCollectionModalContent';

interface EditGameCollectionModalProps
  extends EditGameCollectionModalContentProps {
  isOpen: boolean;
}

function EditGameCollectionModal({
  isOpen,
  onModalClose,
  ...otherProps
}: EditGameCollectionModalProps) {
  const dispatch = useDispatch();

  const handleModalClose = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'gameCollections' }));
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal isOpen={isOpen} onModalClose={handleModalClose}>
      <EditGameCollectionModalContent
        {...otherProps}
        onModalClose={handleModalClose}
      />
    </Modal>
  );
}

export default EditGameCollectionModal;
