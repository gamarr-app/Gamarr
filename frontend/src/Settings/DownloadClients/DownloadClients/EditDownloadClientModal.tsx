import React, { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import {
  cancelSaveDownloadClient,
  cancelTestDownloadClient,
} from 'Store/Actions/settingsActions';
import EditDownloadClientModalContent from './EditDownloadClientModalContent';

interface EditDownloadClientModalProps {
  id?: number;
  isOpen: boolean;
  onModalClose: () => void;
  onDeleteDownloadClientPress?: () => void;
}

function EditDownloadClientModal({
  id,
  isOpen,
  onModalClose,
}: EditDownloadClientModalProps) {
  const dispatch = useDispatch();

  const handleModalClose = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'settings.downloadClients' }));
    dispatch(cancelTestDownloadClient({ section: 'settings.downloadClients' }));
    dispatch(cancelSaveDownloadClient({ section: 'settings.downloadClients' }));
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal size={sizes.MEDIUM} isOpen={isOpen} onModalClose={handleModalClose}>
      <EditDownloadClientModalContent id={id} onModalClose={handleModalClose} />
    </Modal>
  );
}

export default EditDownloadClientModal;
