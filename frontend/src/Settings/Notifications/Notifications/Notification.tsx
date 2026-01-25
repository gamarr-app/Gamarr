import React, { useCallback, useState } from 'react';
import { Tag } from 'App/State/TagsAppState';
import Card from 'Components/Card';
import Label from 'Components/Label';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TagList from 'Components/TagList';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditNotificationModal from './EditNotificationModal';
import styles from './Notification.css';

interface NotificationProps {
  id: number;
  name: string;
  onGrab: boolean;
  onDownload: boolean;
  onUpgrade: boolean;
  onRename: boolean;
  onGameAdded: boolean;
  onGameDelete: boolean;
  onGameFileDelete: boolean;
  onGameFileDeleteForUpgrade: boolean;
  onHealthIssue: boolean;
  onHealthRestored: boolean;
  onApplicationUpdate: boolean;
  onManualInteractionRequired: boolean;
  supportsOnGrab: boolean;
  supportsOnDownload: boolean;
  supportsOnUpgrade: boolean;
  supportsOnRename: boolean;
  supportsOnGameAdded: boolean;
  supportsOnGameDelete: boolean;
  supportsOnGameFileDelete: boolean;
  supportsOnGameFileDeleteForUpgrade: boolean;
  supportsOnHealthIssue: boolean;
  supportsOnHealthRestored: boolean;
  supportsOnApplicationUpdate: boolean;
  supportsOnManualInteractionRequired: boolean;
  tags: number[];
  tagList: Tag[];
  onConfirmDeleteNotification: (id: number) => void;
}

function Notification({
  id,
  name,
  onGrab,
  onDownload,
  onUpgrade,
  onRename,
  onGameAdded,
  onGameDelete,
  onGameFileDelete,
  onGameFileDeleteForUpgrade,
  onHealthIssue,
  onHealthRestored,
  onApplicationUpdate,
  onManualInteractionRequired,
  supportsOnGrab,
  supportsOnDownload,
  supportsOnUpgrade,
  supportsOnRename,
  supportsOnGameAdded,
  supportsOnGameDelete,
  supportsOnGameFileDelete,
  supportsOnGameFileDeleteForUpgrade,
  supportsOnHealthIssue,
  supportsOnHealthRestored,
  supportsOnApplicationUpdate,
  supportsOnManualInteractionRequired,
  tags,
  tagList,
  onConfirmDeleteNotification,
}: NotificationProps) {
  const [isEditNotificationModalOpen, setIsEditNotificationModalOpen] =
    useState(false);
  const [isDeleteNotificationModalOpen, setIsDeleteNotificationModalOpen] =
    useState(false);

  const handleEditNotificationPress = useCallback(() => {
    setIsEditNotificationModalOpen(true);
  }, []);

  const handleEditNotificationModalClose = useCallback(() => {
    setIsEditNotificationModalOpen(false);
  }, []);

  const handleDeleteNotificationPress = useCallback(() => {
    setIsEditNotificationModalOpen(false);
    setIsDeleteNotificationModalOpen(true);
  }, []);

  const handleDeleteNotificationModalClose = useCallback(() => {
    setIsDeleteNotificationModalOpen(false);
  }, []);

  const handleConfirmDeleteNotification = useCallback(() => {
    onConfirmDeleteNotification(id);
  }, [id, onConfirmDeleteNotification]);

  return (
    <Card
      className={styles.notification}
      overlayContent={true}
      onPress={handleEditNotificationPress}
    >
      <div className={styles.name}>{name}</div>

      {supportsOnGrab && onGrab ? (
        <Label kind={kinds.SUCCESS}>{translate('OnGrab')}</Label>
      ) : null}

      {supportsOnDownload && onDownload ? (
        <Label kind={kinds.SUCCESS}>{translate('OnFileImport')}</Label>
      ) : null}

      {supportsOnUpgrade && onDownload && onUpgrade ? (
        <Label kind={kinds.SUCCESS}>{translate('OnFileUpgrade')}</Label>
      ) : null}

      {supportsOnRename && onRename ? (
        <Label kind={kinds.SUCCESS}>{translate('OnRename')}</Label>
      ) : null}

      {supportsOnGameAdded && onGameAdded ? (
        <Label kind={kinds.SUCCESS}>{translate('OnGameAdded')}</Label>
      ) : null}

      {supportsOnHealthIssue && onHealthIssue ? (
        <Label kind={kinds.SUCCESS}>{translate('OnHealthIssue')}</Label>
      ) : null}

      {supportsOnHealthRestored && onHealthRestored ? (
        <Label kind={kinds.SUCCESS}>{translate('OnHealthRestored')}</Label>
      ) : null}

      {supportsOnApplicationUpdate && onApplicationUpdate ? (
        <Label kind={kinds.SUCCESS}>{translate('OnApplicationUpdate')}</Label>
      ) : null}

      {supportsOnGameDelete && onGameDelete ? (
        <Label kind={kinds.SUCCESS}>{translate('OnGameDelete')}</Label>
      ) : null}

      {supportsOnGameFileDelete && onGameFileDelete ? (
        <Label kind={kinds.SUCCESS}>{translate('OnGameFileDelete')}</Label>
      ) : null}

      {supportsOnGameFileDeleteForUpgrade &&
      onGameFileDelete &&
      onGameFileDeleteForUpgrade ? (
        <Label kind={kinds.SUCCESS}>
          {translate('OnGameFileDeleteForUpgrade')}
        </Label>
      ) : null}

      {supportsOnManualInteractionRequired && onManualInteractionRequired ? (
        <Label kind={kinds.SUCCESS}>
          {translate('OnManualInteractionRequired')}
        </Label>
      ) : null}

      {!onGrab &&
      !onDownload &&
      !onRename &&
      !onHealthIssue &&
      !onHealthRestored &&
      !onApplicationUpdate &&
      !onGameAdded &&
      !onGameDelete &&
      !onGameFileDelete &&
      !onManualInteractionRequired ? (
        <Label kind={kinds.DISABLED} outline={true}>
          {translate('Disabled')}
        </Label>
      ) : null}

      <TagList tags={tags} tagList={tagList} />

      <EditNotificationModal
        id={id}
        isOpen={isEditNotificationModalOpen}
        onModalClose={handleEditNotificationModalClose}
        onDeleteNotificationPress={handleDeleteNotificationPress}
      />

      <ConfirmModal
        isOpen={isDeleteNotificationModalOpen}
        kind={kinds.DANGER}
        title={translate('DeleteNotification')}
        message={translate('DeleteNotificationMessageText', { name })}
        confirmLabel={translate('Delete')}
        onConfirm={handleConfirmDeleteNotification}
        onCancel={handleDeleteNotificationModalClose}
      />
    </Card>
  );
}

export default Notification;
