import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import GameLanguages from 'Game/GameLanguages';
import FileEditModal from 'GameFile/Edit/FileEditModal';
import { icons, kinds } from 'Helpers/Props';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import FileDetailsModal from '../FileDetailsModal';
import styles from './GameFileEditorRow.css';

class GameFileEditorRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isConfirmDeleteModalOpen: false,
      isFileDetailsModalOpen: false,
      isFileEditModalOpen: false
    };
  }

  //
  // Listeners

  onDeletePress = () => {
    this.setState({ isConfirmDeleteModalOpen: true });
  };

  onConfirmDelete = () => {
    this.setState({ isConfirmDeleteModalOpen: false });

    this.props.onDeletePress(this.props.id);
  };

  onConfirmDeleteModalClose = () => {
    this.setState({ isConfirmDeleteModalOpen: false });
  };

  onFileDetailsPress = () => {
    this.setState({ isFileDetailsModalOpen: true });
  };

  onFileDetailsModalClose = () => {
    this.setState({ isFileDetailsModalOpen: false });
  };

  onFileEditPress = () => {
    this.setState({ isFileEditModalOpen: true });
  };

  onFileEditModalClose = () => {
    this.setState({ isFileEditModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      id,
      path,
      relativePath,
      size,
      sceneName,
      releaseGroup,
      version,
      languages,
      dateAdded,
      columns
    } = this.props;

    const {
      isFileDetailsModalOpen,
      isFileEditModalOpen,
      isConfirmDeleteModalOpen
    } = this.state;

    return (
      <TableRow>
        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

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
                  title={size}
                >
                  {formatBytes(size)}
                </TableRowCell>
              );
            }

            if (name === 'languages') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.languages}
                >
                  {languages && languages.length > 0 ?
                    <GameLanguages languages={languages} /> :
                    null
                  }
                </TableRowCell>
              );
            }

            if (name === 'releaseGroup') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.releaseGroup}
                >
                  {releaseGroup}
                </TableRowCell>
              );
            }

            if (name === 'version') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.version}
                >
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
                    onPress={this.onFileEditPress}
                  />

                  <IconButton
                    title={translate('Details')}
                    name={icons.MEDIA_INFO}
                    onPress={this.onFileDetailsPress}
                  />

                  <IconButton
                    title={translate('DeleteFile')}
                    name={icons.REMOVE}
                    onPress={this.onDeletePress}
                  />
                </TableRowCell>
              );
            }

            return null;
          })
        }

        <FileDetailsModal
          isOpen={isFileDetailsModalOpen}
          onModalClose={this.onFileDetailsModalClose}
          path={path}
          size={size}
          dateAdded={dateAdded}
          sceneName={sceneName}
          releaseGroup={releaseGroup}
        />

        <FileEditModal
          gameFileId={id}
          isOpen={isFileEditModalOpen}
          onModalClose={this.onFileEditModalClose}
        />

        <ConfirmModal
          isOpen={isConfirmDeleteModalOpen}
          ids={[id]}
          kind={kinds.DANGER}
          title={translate('DeleteSelectedGameFiles')}
          message={translate('DeleteSelectedGameFilesHelpText')}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDelete}
          onCancel={this.onConfirmDeleteModalClose}
        />
      </TableRow>
    );
  }

}

GameFileEditorRow.propTypes = {
  id: PropTypes.number.isRequired,
  path: PropTypes.string,
  size: PropTypes.number.isRequired,
  relativePath: PropTypes.string.isRequired,
  sceneName: PropTypes.string,
  releaseGroup: PropTypes.string,
  version: PropTypes.string,
  languages: PropTypes.arrayOf(PropTypes.object),
  dateAdded: PropTypes.string,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  onDeletePress: PropTypes.func.isRequired
};

GameFileEditorRow.defaultProps = {
  languages: []
};

export default GameFileEditorRow;
