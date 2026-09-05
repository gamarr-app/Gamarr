import { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditPlatformRootFolderModalContent from './EditPlatformRootFolderModalContent';

interface EditPlatformRootFolderModalProps {
  id?: number;
  isOpen: boolean;
  onModalClose: () => void;
  onDeletePlatformRootFolderPress?: () => void;
}

function EditPlatformRootFolderModal({
  id,
  isOpen,
  onModalClose,
  onDeletePlatformRootFolderPress,
}: EditPlatformRootFolderModalProps) {
  const dispatch = useDispatch();

  const handleModalClose = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'settings.platformRootFolders' }));
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal size={sizes.MEDIUM} isOpen={isOpen} onModalClose={handleModalClose}>
      <EditPlatformRootFolderModalContent
        id={id}
        onModalClose={handleModalClose}
        onDeletePlatformRootFolderPress={onDeletePlatformRootFolderPress}
      />
    </Modal>
  );
}

export default EditPlatformRootFolderModal;
