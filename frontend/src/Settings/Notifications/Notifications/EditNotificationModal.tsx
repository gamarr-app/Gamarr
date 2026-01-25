import { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import {
  cancelSaveNotification,
  cancelTestNotification,
} from 'Store/Actions/settingsActions';
import EditNotificationModalContent from './EditNotificationModalContent';

const section = 'settings.notifications';

interface EditNotificationModalProps {
  id?: number;
  isOpen: boolean;
  onModalClose: () => void;
  onDeleteNotificationPress?: () => void;
}

function EditNotificationModal({
  id,
  isOpen,
  onModalClose,
  onDeleteNotificationPress,
}: EditNotificationModalProps) {
  const dispatch = useDispatch();

  const handleModalClose = useCallback(() => {
    dispatch(clearPendingChanges({ section }));
    dispatch(cancelTestNotification({ section }));
    dispatch(cancelSaveNotification({ section }));
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal size={sizes.MEDIUM} isOpen={isOpen} onModalClose={handleModalClose}>
      <EditNotificationModalContent
        id={id}
        onModalClose={handleModalClose}
        onDeleteNotificationPress={onDeleteNotificationPress}
      />
    </Modal>
  );
}

export default EditNotificationModal;
