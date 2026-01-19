import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import Popover from 'Components/Tooltip/Popover';
import Tooltip from 'Components/Tooltip/Tooltip';
import GameFormats from 'Game/GameFormats';
import GameLanguages from 'Game/GameLanguages';
import GameQuality from 'Game/GameQuality';
import IndexerFlags from 'Game/IndexerFlags';
import FileEditModal from 'GameFile/Edit/FileEditModal';
import MediaInfo from 'GameFile/MediaInfo';
import * as mediaInfoTypes from 'GameFile/mediaInfoTypes';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import formatBytes from 'Utilities/Number/formatBytes';
import formatCustomFormatScore from 'Utilities/Number/formatCustomFormatScore';
import translate from 'Utilities/String/translate';
import FileDetailsModal from '../FileDetailsModal';
import GameFileRowCellPlaceholder from './GameFileRowCellPlaceholder';
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
      mediaInfo,
      relativePath,
      size,
      releaseGroup,
      quality,
      qualityCutoffNotMet,
      customFormats,
      customFormatScore,
      indexerFlags,
      languages,
      dateAdded,
      columns
    } = this.props;

    const {
      isFileDetailsModalOpen,
      isFileEditModalOpen,
      isConfirmDeleteModalOpen
    } = this.state;

    const showQualityPlaceholder = !quality;
    const showLanguagePlaceholder = !languages;

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
              return (
                <TableRowCell
                  key={name}
                  className={styles.relativePath}
                  title={relativePath}
                >
                  {relativePath}
                </TableRowCell>
              );
            }

            if (name === 'customFormats') {
              return (
                <TableRowCell key={name}>
                  <GameFormats
                    formats={customFormats}
                  />
                </TableRowCell>
              );
            }

            if (name === 'customFormatScore') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.customFormatScore}
                >
                  <Tooltip
                    anchor={formatCustomFormatScore(
                      customFormatScore,
                      customFormats.length
                    )}
                    tooltip={<GameFormats formats={customFormats} />}
                    position={tooltipPositions.LEFT}
                  />
                </TableRowCell>
              );
            }

            if (name === 'indexerFlags') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.indexerFlags}
                >
                  {indexerFlags ? (
                    <Popover
                      anchor={<Icon name={icons.FLAG} />}
                      title={translate('IndexerFlags')}
                      body={<IndexerFlags indexerFlags={indexerFlags} />}
                      position={tooltipPositions.LEFT}
                    />
                  ) : null}
                </TableRowCell>
              );
            }

            if (name === 'languages') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.languages}
                >
                  {
                    showLanguagePlaceholder ?
                      <GameFileRowCellPlaceholder /> :
                      null
                  }

                  {
                    !showLanguagePlaceholder && !!languages &&
                      <GameLanguages
                        className={styles.label}
                        languages={languages}
                      />
                  }
                </TableRowCell>
              );
            }

            if (name === 'quality') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.quality}
                >
                  {
                    showQualityPlaceholder ?
                      <GameFileRowCellPlaceholder /> :
                      null
                  }

                  {
                    !showQualityPlaceholder && !!quality &&
                      <GameQuality
                        className={styles.label}
                        quality={quality}
                        isCutoffNotMet={qualityCutoffNotMet}
                      />
                  }
                </TableRowCell>
              );
            }

            if (name === 'audioInfo') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.audio}
                >
                  <MediaInfo
                    type={mediaInfoTypes.AUDIO}
                    gameFileId={id}
                  />
                </TableRowCell>
              );
            }

            if (name === 'audioLanguages') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.audioLanguages}
                >
                  <MediaInfo
                    type={mediaInfoTypes.AUDIO_LANGUAGES}
                    gameFileId={id}
                  />
                </TableRowCell>
              );
            }

            if (name === 'subtitleLanguages') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.subtitles}
                >
                  <MediaInfo
                    type={mediaInfoTypes.SUBTITLES}
                    gameFileId={id}
                  />
                </TableRowCell>
              );
            }

            if (name === 'videoCodec') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.video}
                >
                  <MediaInfo
                    type={mediaInfoTypes.VIDEO}
                    gameFileId={id}
                  />
                </TableRowCell>
              );
            }

            if (name === 'videoDynamicRangeType') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.videoDynamicRangeType}
                >
                  <MediaInfo
                    type={mediaInfoTypes.VIDEO_DYNAMIC_RANGE_TYPE}
                    gameFileId={id}
                  />
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
          mediaInfo={mediaInfo}
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
  size: PropTypes.number.isRequired,
  relativePath: PropTypes.string.isRequired,
  quality: PropTypes.object.isRequired,
  releaseGroup: PropTypes.string,
  customFormats: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFormatScore: PropTypes.number.isRequired,
  indexerFlags: PropTypes.number.isRequired,
  qualityCutoffNotMet: PropTypes.bool.isRequired,
  languages: PropTypes.arrayOf(PropTypes.object).isRequired,
  mediaInfo: PropTypes.object,
  dateAdded: PropTypes.string,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  onDeletePress: PropTypes.func.isRequired
};

GameFileEditorRow.defaultProps = {
  customFormats: [],
  indexerFlags: 0
};

export default GameFileEditorRow;
