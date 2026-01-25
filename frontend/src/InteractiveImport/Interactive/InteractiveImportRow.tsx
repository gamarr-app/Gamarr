import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useDispatch } from 'react-redux';
import Icon from 'Components/Icon';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRowCellButton from 'Components/Table/Cells/TableRowCellButton';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import Column from 'Components/Table/Column';
import TableRow from 'Components/Table/TableRow';
import Popover from 'Components/Tooltip/Popover';
import Game from 'Game/Game';
import GameFormats from 'Game/GameFormats';
import GameLanguages from 'Game/GameLanguages';
import GameQuality from 'Game/GameQuality';
import IndexerFlags from 'Game/IndexerFlags';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import SelectGameModal from 'InteractiveImport/Game/SelectGameModal';
import SelectIndexerFlagsModal from 'InteractiveImport/IndexerFlags/SelectIndexerFlagsModal';
import SelectLanguageModal from 'InteractiveImport/Language/SelectLanguageModal';
import SelectQualityModal from 'InteractiveImport/Quality/SelectQualityModal';
import SelectReleaseGroupModal from 'InteractiveImport/ReleaseGroup/SelectReleaseGroupModal';
import Language from 'Language/Language';
import { QualityModel } from 'Quality/Quality';
import {
  reprocessInteractiveImportItems,
  updateInteractiveImportItem,
} from 'Store/Actions/interactiveImportActions';
import CustomFormat from 'typings/CustomFormat';
import { SelectStateInputProps } from 'typings/props';
import Rejection from 'typings/Rejection';
import formatBytes from 'Utilities/Number/formatBytes';
import formatCustomFormatScore from 'Utilities/Number/formatCustomFormatScore';
import translate from 'Utilities/String/translate';
import InteractiveImportRowCellPlaceholder from './InteractiveImportRowCellPlaceholder';
import styles from './InteractiveImportRow.css';

type SelectType =
  | 'game'
  | 'releaseGroup'
  | 'quality'
  | 'language'
  | 'indexerFlags';

type SelectedChangeProps = SelectStateInputProps & {
  hasGameFileId: boolean;
};

interface InteractiveImportRowProps {
  id: number;
  allowGameChange: boolean;
  relativePath: string;
  game?: Game;
  releaseGroup?: string;
  quality?: QualityModel;
  languages?: Language[];
  size: number;
  customFormats?: CustomFormat[];
  customFormatScore?: number;
  indexerFlags: number;
  rejections: Rejection[];
  columns: Column[];
  gameFileId?: number;
  isReprocessing?: boolean;
  isSelected?: boolean;
  modalTitle: string;
  onSelectedChange(result: SelectedChangeProps): void;
  onValidRowChange(id: number, isValid: boolean): void;
}

function InteractiveImportRow(props: InteractiveImportRowProps) {
  const {
    id,
    allowGameChange,
    relativePath,
    game,
    quality,
    languages,
    releaseGroup,
    size,
    customFormats,
    customFormatScore,
    indexerFlags,
    rejections,
    isSelected,
    modalTitle,
    gameFileId,
    columns,
    onSelectedChange,
    onValidRowChange,
  } = props;

  const dispatch = useDispatch();

  const isGameColumnVisible = useMemo(
    () => columns.find((c) => c.name === 'game')?.isVisible ?? false,
    [columns]
  );
  const isIndexerFlagsColumnVisible = useMemo(
    () => columns.find((c) => c.name === 'indexerFlags')?.isVisible ?? false,
    [columns]
  );

  const [selectModalOpen, setSelectModalOpen] = useState<SelectType | null>(
    null
  );

  const hasAutoSelectedRef = useRef(false);

  useEffect(() => {
    if (
      !hasAutoSelectedRef.current &&
      allowGameChange &&
      game &&
      quality &&
      languages &&
      size > 0
    ) {
      hasAutoSelectedRef.current = true;
      onSelectedChange({
        id,
        hasGameFileId: !!gameFileId,
        value: true,
        shiftKey: false,
      });
    }
  }, [
    allowGameChange,
    game,
    quality,
    languages,
    size,
    id,
    gameFileId,
    onSelectedChange,
  ]);

  useEffect(() => {
    const isValid = !!(game && quality && languages);

    if (isSelected && !isValid) {
      onValidRowChange(id, false);
    } else {
      onValidRowChange(id, true);
    }
  }, [id, game, quality, languages, isSelected, onValidRowChange]);

  const handleSelectedChange = useCallback(
    (result: SelectStateInputProps) => {
      onSelectedChange({
        ...result,
        hasGameFileId: !!gameFileId,
      });
    },
    [gameFileId, onSelectedChange]
  );

  const selectRowAfterChange = useCallback(() => {
    if (!isSelected) {
      onSelectedChange({
        id,
        hasGameFileId: !!gameFileId,
        value: true,
        shiftKey: false,
      });
    }
  }, [id, gameFileId, isSelected, onSelectedChange]);

  const onSelectModalClose = useCallback(() => {
    setSelectModalOpen(null);
  }, [setSelectModalOpen]);

  const onSelectGamePress = useCallback(() => {
    setSelectModalOpen('game');
  }, [setSelectModalOpen]);

  const onGameSelect = useCallback(
    (game: Game) => {
      dispatch(
        updateInteractiveImportItem({
          id,
          game,
        })
      );

      dispatch(reprocessInteractiveImportItems({ ids: [id] }));

      setSelectModalOpen(null);
      selectRowAfterChange();
    },
    [id, dispatch, setSelectModalOpen, selectRowAfterChange]
  );

  const onSelectReleaseGroupPress = useCallback(() => {
    setSelectModalOpen('releaseGroup');
  }, [setSelectModalOpen]);

  const onReleaseGroupSelect = useCallback(
    (releaseGroup: string) => {
      dispatch(
        updateInteractiveImportItem({
          id,
          releaseGroup,
        })
      );

      dispatch(reprocessInteractiveImportItems({ ids: [id] }));

      setSelectModalOpen(null);
      selectRowAfterChange();
    },
    [id, dispatch, setSelectModalOpen, selectRowAfterChange]
  );

  const onSelectQualityPress = useCallback(() => {
    setSelectModalOpen('quality');
  }, [setSelectModalOpen]);

  const onQualitySelect = useCallback(
    (quality: QualityModel) => {
      dispatch(
        updateInteractiveImportItem({
          id,
          quality,
        })
      );

      dispatch(reprocessInteractiveImportItems({ ids: [id] }));

      setSelectModalOpen(null);
      selectRowAfterChange();
    },
    [id, dispatch, setSelectModalOpen, selectRowAfterChange]
  );

  const onSelectLanguagePress = useCallback(() => {
    setSelectModalOpen('language');
  }, [setSelectModalOpen]);

  const onLanguagesSelect = useCallback(
    (languages: Language[]) => {
      dispatch(
        updateInteractiveImportItem({
          id,
          languages,
        })
      );

      dispatch(reprocessInteractiveImportItems({ ids: [id] }));

      setSelectModalOpen(null);
      selectRowAfterChange();
    },
    [id, dispatch, setSelectModalOpen, selectRowAfterChange]
  );

  const onSelectIndexerFlagsPress = useCallback(() => {
    setSelectModalOpen('indexerFlags');
  }, [setSelectModalOpen]);

  const onIndexerFlagsSelect = useCallback(
    (indexerFlags: number) => {
      dispatch(
        updateInteractiveImportItem({
          id,
          indexerFlags,
        })
      );

      dispatch(reprocessInteractiveImportItems({ ids: [id] }));

      setSelectModalOpen(null);
      selectRowAfterChange();
    },
    [id, dispatch, setSelectModalOpen, selectRowAfterChange]
  );

  const gameTitle = game ? game.title : '';

  const showGamePlaceholder = isSelected && !game;
  const showReleaseGroupPlaceholder = isSelected && !releaseGroup;
  const showQualityPlaceholder = isSelected && !quality;
  const showLanguagePlaceholder = isSelected && !languages;
  const showIndexerFlagsPlaceholder = isSelected && !indexerFlags;

  return (
    <TableRow>
      <TableSelectCell
        id={id}
        isSelected={isSelected}
        onSelectedChange={handleSelectedChange}
      />

      <TableRowCell className={styles.relativePath} title={relativePath}>
        {relativePath}
      </TableRowCell>

      {isGameColumnVisible ? (
        <TableRowCellButton
          isDisabled={!allowGameChange}
          title={allowGameChange ? translate('ClickToChangeGame') : undefined}
          onPress={onSelectGamePress}
        >
          {showGamePlaceholder ? (
            <InteractiveImportRowCellPlaceholder />
          ) : (
            gameTitle
          )}
        </TableRowCellButton>
      ) : null}

      <TableRowCellButton
        title={translate('ClickToChangeReleaseGroup')}
        onPress={onSelectReleaseGroupPress}
      >
        {showReleaseGroupPlaceholder ? (
          <InteractiveImportRowCellPlaceholder isOptional={true} />
        ) : (
          releaseGroup
        )}
      </TableRowCellButton>

      <TableRowCellButton
        className={styles.quality}
        title={translate('ClickToChangeQuality')}
        onPress={onSelectQualityPress}
      >
        {showQualityPlaceholder && <InteractiveImportRowCellPlaceholder />}

        {!showQualityPlaceholder && !!quality && (
          <GameQuality className={styles.label} quality={quality} />
        )}
      </TableRowCellButton>

      <TableRowCellButton
        className={styles.languages}
        title={translate('ClickToChangeLanguage')}
        onPress={onSelectLanguagePress}
      >
        {showLanguagePlaceholder && <InteractiveImportRowCellPlaceholder />}

        {!showLanguagePlaceholder && !!languages && (
          <GameLanguages className={styles.label} languages={languages} />
        )}
      </TableRowCellButton>

      <TableRowCell>{formatBytes(size)}</TableRowCell>

      <TableRowCell>
        {customFormats?.length ? (
          <Popover
            anchor={formatCustomFormatScore(
              customFormatScore,
              customFormats.length
            )}
            title={translate('CustomFormats')}
            body={
              <div className={styles.customFormatTooltip}>
                <GameFormats formats={customFormats} />
              </div>
            }
            position={tooltipPositions.LEFT}
          />
        ) : null}
      </TableRowCell>

      {isIndexerFlagsColumnVisible ? (
        <TableRowCellButton
          title={translate('ClickToChangeIndexerFlags')}
          onPress={onSelectIndexerFlagsPress}
        >
          {showIndexerFlagsPlaceholder ? (
            <InteractiveImportRowCellPlaceholder isOptional={true} />
          ) : (
            <>
              {indexerFlags ? (
                <Popover
                  anchor={<Icon name={icons.FLAG} />}
                  title={translate('IndexerFlags')}
                  body={<IndexerFlags indexerFlags={indexerFlags} />}
                  position={tooltipPositions.LEFT}
                />
              ) : null}
            </>
          )}
        </TableRowCellButton>
      ) : null}

      <TableRowCell>
        {rejections.length ? (
          <Popover
            anchor={<Icon name={icons.DANGER} kind={kinds.DANGER} />}
            title={translate('ReleaseRejected')}
            body={
              <ul>
                {rejections.map((rejection, index) => {
                  return <li key={index}>{rejection.reason}</li>;
                })}
              </ul>
            }
            position={tooltipPositions.LEFT}
            canFlip={false}
          />
        ) : null}
      </TableRowCell>

      <SelectGameModal
        isOpen={selectModalOpen === 'game'}
        modalTitle={modalTitle}
        onGameSelect={onGameSelect}
        onModalClose={onSelectModalClose}
      />

      <SelectReleaseGroupModal
        isOpen={selectModalOpen === 'releaseGroup'}
        releaseGroup={releaseGroup ?? ''}
        modalTitle={modalTitle}
        onReleaseGroupSelect={onReleaseGroupSelect}
        onModalClose={onSelectModalClose}
      />

      <SelectQualityModal
        isOpen={selectModalOpen === 'quality'}
        qualityId={quality ? quality.quality.id : 0}
        proper={quality ? quality.revision.version > 1 : false}
        real={quality ? quality.revision.real > 0 : false}
        modalTitle={modalTitle}
        onQualitySelect={onQualitySelect}
        onModalClose={onSelectModalClose}
      />

      <SelectLanguageModal
        isOpen={selectModalOpen === 'language'}
        languageIds={languages ? languages.map((l) => l.id) : []}
        modalTitle={modalTitle}
        onLanguagesSelect={onLanguagesSelect}
        onModalClose={onSelectModalClose}
      />

      <SelectIndexerFlagsModal
        isOpen={selectModalOpen === 'indexerFlags'}
        indexerFlags={indexerFlags ?? 0}
        modalTitle={modalTitle}
        onIndexerFlagsSelect={onIndexerFlagsSelect}
        onModalClose={onSelectModalClose}
      />
    </TableRow>
  );
}

export default InteractiveImportRow;
