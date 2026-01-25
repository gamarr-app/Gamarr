import React, { useCallback, useState } from 'react';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import Column from 'Components/Table/Column';
import TableRow from 'Components/Table/TableRow';
import GameLanguages from 'Game/GameLanguages';
import FileEditModal from 'GameFile/Edit/FileEditModal';
import { icons, kinds } from 'Helpers/Props';
import Language from 'Language/Language';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import FileDetailsModal from '../FileDetailsModal';
import styles from './GameFileEditorRow.css';

interface GameFileEditorRowProps {
  id: number;
  path?: string;
  size: number;
  relativePath: string;
  sceneName?: string;
  releaseGroup?: string;
  version?: string;
  languages?: Language[];
  dateAdded?: string;
  columns: Column[];
  onDeletePress: (id: number) => void;
}

function GameFileEditorRow(props: GameFileEditorRowProps) {
  const {
    id,
    path,
    relativePath,
    size,
    sceneName,
    releaseGroup,
    version,
    languages = [],
    dateAdded,
    columns,
    onDeletePress,
  } = props;

  const [isConfirmDeleteModalOpen, setIsConfirmDeleteModalOpen] =
    useState(false);
  const [isFileDetailsModalOpen, setIsFileDetailsModalOpen] = useState(false);
  const [isFileEditModalOpen, setIsFileEditModalOpen] = useState(false);

  const onDeletePressHandler = useCallback(() => {
    setIsConfirmDeleteModalOpen(true);
  }, []);

  const onConfirmDelete = useCallback(() => {
    setIsConfirmDeleteModalOpen(false);
    onDeletePress(id);
  }, [id, onDeletePress]);

  const onConfirmDeleteModalClose = useCallback(() => {
    setIsConfirmDeleteModalOpen(false);
  }, []);

  const onFileDetailsPress = useCallback(() => {
    setIsFileDetailsModalOpen(true);
  }, []);

  const onFileDetailsModalClose = useCallback(() => {
    setIsFileDetailsModalOpen(false);
  }, []);

  const onFileEditPress = useCallback(() => {
    setIsFileEditModalOpen(true);
  }, []);

  const onFileEditModalClose = useCallback(() => {
    setIsFileEditModalOpen(false);
  }, []);

  return (
    <TableRow>
      {columns.map((column) => {
        const { name, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        if (name === 'relativePath') {
          // Empty relativePath means this is a folder-based GameFile
          const displayPath = relativePath || translate('GameFolder');
          const isFolder = !relativePath;

          return (
            <TableRowCell
              key={name}
              className={styles.relativePath}
              title={isFolder ? translate('GameFolderTooltip') : relativePath}
            >
              {displayPath}
            </TableRowCell>
          );
        }

        if (name === 'size') {
          return (
            <TableRowCell
              key={name}
              className={styles.size}
              title={String(size)}
            >
              {formatBytes(size)}
            </TableRowCell>
          );
        }

        if (name === 'languages') {
          return (
            <TableRowCell key={name} className={styles.languages}>
              {languages && languages.length > 0 ? (
                <GameLanguages languages={languages} />
              ) : null}
            </TableRowCell>
          );
        }

        if (name === 'releaseGroup') {
          return (
            <TableRowCell key={name} className={styles.releaseGroup}>
              {releaseGroup}
            </TableRowCell>
          );
        }

        if (name === 'version') {
          return (
            <TableRowCell key={name} className={styles.version}>
              {version}
            </TableRowCell>
          );
        }

        if (name === 'dateAdded') {
          return (
            <RelativeDateCell
              key={name}
              className={styles.dateAdded}
              date={dateAdded}
            />
          );
        }

        if (name === 'actions') {
          return (
            <TableRowCell key={name} className={styles.actions}>
              <IconButton
                title={translate('EditGameFile')}
                name={icons.EDIT}
                onPress={onFileEditPress}
              />

              <IconButton
                title={translate('Details')}
                name={icons.MEDIA_INFO}
                onPress={onFileDetailsPress}
              />

              <IconButton
                title={translate('DeleteFile')}
                name={icons.REMOVE}
                onPress={onDeletePressHandler}
              />
            </TableRowCell>
          );
        }

        return null;
      })}

      <FileDetailsModal
        isOpen={isFileDetailsModalOpen}
        path={path}
        size={size}
        dateAdded={dateAdded}
        sceneName={sceneName}
        releaseGroup={releaseGroup}
        onModalClose={onFileDetailsModalClose}
      />

      <FileEditModal
        gameFileId={id}
        isOpen={isFileEditModalOpen}
        onModalClose={onFileEditModalClose}
      />

      <ConfirmModal
        isOpen={isConfirmDeleteModalOpen}
        kind={kinds.DANGER}
        title={translate('DeleteSelectedGameFiles')}
        message={translate('DeleteSelectedGameFilesHelpText')}
        confirmLabel={translate('Delete')}
        onConfirm={onConfirmDelete}
        onCancel={onConfirmDeleteModalClose}
      />
    </TableRow>
  );
}

export default GameFileEditorRow;
