import classNames from 'classnames';
import { useCallback, useState } from 'react';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import { getPlatformTitle } from 'Game/platformOptions';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditPlatformRootFolderModal from './EditPlatformRootFolderModal';
import styles from './PlatformRootFolder.css';

interface PlatformRootFolderProps {
  id: number;
  platform: string;
  path: string;
  onConfirmDeletePlatformRootFolder: (id: number) => void;
}

function PlatformRootFolder({
  id,
  platform,
  path,
  onConfirmDeletePlatformRootFolder,
}: PlatformRootFolderProps) {
  const [
    isEditPlatformRootFolderModalOpen,
    setIsEditPlatformRootFolderModalOpen,
  ] = useState(false);
  const [
    isDeletePlatformRootFolderModalOpen,
    setIsDeletePlatformRootFolderModalOpen,
  ] = useState(false);

  const handleEditPlatformRootFolderPress = useCallback(() => {
    setIsEditPlatformRootFolderModalOpen(true);
  }, []);

  const handleEditPlatformRootFolderModalClose = useCallback(() => {
    setIsEditPlatformRootFolderModalOpen(false);
  }, []);

  const handleDeletePlatformRootFolderPress = useCallback(() => {
    setIsEditPlatformRootFolderModalOpen(false);
    setIsDeletePlatformRootFolderModalOpen(true);
  }, []);

  const handleDeletePlatformRootFolderModalClose = useCallback(() => {
    setIsDeletePlatformRootFolderModalOpen(false);
  }, []);

  const handleConfirmDeletePlatformRootFolder = useCallback(() => {
    onConfirmDeletePlatformRootFolder(id);
  }, [id, onConfirmDeletePlatformRootFolder]);

  return (
    <div className={classNames(styles.platformRootFolder)}>
      <div className={styles.platform}>{getPlatformTitle(platform)}</div>
      <div className={styles.path}>{path}</div>

      <div className={styles.actions}>
        <Link onPress={handleEditPlatformRootFolderPress}>
          <Icon name={icons.EDIT} />
        </Link>
      </div>

      <EditPlatformRootFolderModal
        id={id}
        isOpen={isEditPlatformRootFolderModalOpen}
        onModalClose={handleEditPlatformRootFolderModalClose}
        onDeletePlatformRootFolderPress={handleDeletePlatformRootFolderPress}
      />

      <ConfirmModal
        isOpen={isDeletePlatformRootFolderModalOpen}
        kind={kinds.DANGER}
        title={translate('DeletePlatformRootFolder')}
        message={translate('DeletePlatformRootFolderMessageText')}
        confirmLabel={translate('Delete')}
        onConfirm={handleConfirmDeletePlatformRootFolder}
        onCancel={handleDeletePlatformRootFolderModalClose}
      />
    </div>
  );
}

export default PlatformRootFolder;
