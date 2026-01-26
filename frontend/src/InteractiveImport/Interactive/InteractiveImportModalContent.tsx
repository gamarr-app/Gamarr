import { cloneDeep, without } from 'lodash';
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import * as commandNames from 'Commands/commandNames';
import SelectInput, { SelectInputOption } from 'Components/Form/SelectInput';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Menu from 'Components/Menu/Menu';
import MenuButton from 'Components/Menu/MenuButton';
import MenuContent from 'Components/Menu/MenuContent';
import SelectedMenuItem from 'Components/Menu/SelectedMenuItem';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import Game from 'Game/Game';
import { GameFile } from 'GameFile/GameFile';
import usePrevious from 'Helpers/Hooks/usePrevious';
import useSelectState from 'Helpers/Hooks/useSelectState';
import { align, icons, kinds, scrollDirections } from 'Helpers/Props';
import SelectGameModal from 'InteractiveImport/Game/SelectGameModal';
import ImportMode from 'InteractiveImport/ImportMode';
import SelectIndexerFlagsModal from 'InteractiveImport/IndexerFlags/SelectIndexerFlagsModal';
import InteractiveImport, {
  InteractiveImportCommandOptions,
} from 'InteractiveImport/InteractiveImport';
import SelectLanguageModal from 'InteractiveImport/Language/SelectLanguageModal';
import SelectQualityModal from 'InteractiveImport/Quality/SelectQualityModal';
import SelectReleaseGroupModal from 'InteractiveImport/ReleaseGroup/SelectReleaseGroupModal';
import Language from 'Language/Language';
import { QualityModel } from 'Quality/Quality';
import { executeCommand } from 'Store/Actions/commandActions';
import {
  deleteGameFiles,
  updateGameFiles,
} from 'Store/Actions/gameFileActions';
import {
  clearInteractiveImport,
  fetchInteractiveImportItems,
  reprocessInteractiveImportItems,
  setInteractiveImportMode,
  setInteractiveImportSort,
  updateInteractiveImportItems,
} from 'Store/Actions/interactiveImportActions';
import createClientSideCollectionSelector, {
  CollectionResult,
} from 'Store/Selectors/createClientSideCollectionSelector';
import { SortCallback } from 'typings/callbacks';
import { SelectStateInputProps } from 'typings/props';
import { CheckInputChanged } from 'typings/inputs';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import InteractiveImportRow from './InteractiveImportRow';
import styles from './InteractiveImportModalContent.css';

type SelectType =
  | 'select'
  | 'game'
  | 'releaseGroup'
  | 'quality'
  | 'language'
  | 'indexerFlags';

type InteractiveImportSelectedChangeCallback = (
  props: SelectStateInputProps & { hasGameFileId: boolean }
) => void;

const COLUMNS = [
  {
    name: 'relativePath',
    label: () => translate('RelativePath'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'game',
    label: () => translate('Game'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'releaseGroup',
    label: () => translate('ReleaseGroup'),
    isVisible: true,
  },
  {
    name: 'quality',
    label: () => translate('Quality'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'languages',
    label: () => translate('Languages'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'size',
    label: () => translate('Size'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'customFormats',
    label: React.createElement(Icon, {
      name: icons.INTERACTIVE,
      title: () => translate('CustomFormat'),
    }),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'indexerFlags',
    label: React.createElement(Icon, {
      name: icons.FLAG,
      title: () => translate('IndexerFlags'),
    }),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'rejections',
    label: React.createElement(Icon, {
      name: icons.DANGER,
      kind: kinds.DANGER,
      title: () => translate('Rejections'),
    }),
    isSortable: true,
    isVisible: true,
  },
];

const importModeOptions: SelectInputOption[] = [
  {
    key: 'chooseImportMode',
    value: () => translate('ChooseImportMode'),
    disabled: true,
  },
  {
    key: 'move',
    value: () => translate('MoveFiles'),
  },
  {
    key: 'copy',
    value: () => translate('HardlinkCopyFiles'),
  },
];

function isSameGameFile(
  file: InteractiveImport,
  originalFile?: InteractiveImport
) {
  const { game } = file;

  if (!originalFile) {
    return false;
  }

  if (!originalFile.game || game?.id !== originalFile.game.id) {
    return false;
  }

  return true;
}

const gameFilesInfoSelector = createSelector(
  (state: AppState) => state.gameFiles.isDeleting,
  (state: AppState) => state.gameFiles.deleteError,
  (isDeleting, deleteError) => {
    return {
      isDeleting,
      deleteError,
    };
  }
);

const importModeSelector = createSelector(
  (state: AppState) => state.interactiveImport.importMode,
  (importMode) => {
    return importMode;
  }
);

interface InteractiveImportCollectionResult
  extends CollectionResult<InteractiveImport> {
  originalItems: InteractiveImport[];
}

const interactiveImportSelector = createSelector(
  createClientSideCollectionSelector<InteractiveImport>('interactiveImport'),
  (state: AppState) => state.interactiveImport.originalItems,
  (collection, originalItems): InteractiveImportCollectionResult => ({
    ...collection,
    originalItems,
  })
);

export interface InteractiveImportModalContentProps {
  downloadId?: string;
  gameId?: number;
  showGame?: boolean;
  allowGameChange?: boolean;
  showDelete?: boolean;
  showImportMode?: boolean;
  showFilterExistingFiles?: boolean;
  title?: string;
  folder?: string;
  sortKey?: string;
  sortDirection?: string;
  initialSortKey?: string;
  initialSortDirection?: string;
  modalTitle: string;
  onModalClose(): void;
}

function InteractiveImportModalContent(
  props: InteractiveImportModalContentProps
) {
  const {
    downloadId,
    gameId,
    allowGameChange = true,
    showGame = true,
    showFilterExistingFiles = false,
    showDelete = false,
    showImportMode = true,
    title,
    folder,
    initialSortKey,
    initialSortDirection,
    modalTitle,
    onModalClose,
  } = props;

  const {
    isFetching,
    isPopulated,
    error,
    items,
    originalItems,
    sortKey,
    sortDirection,
  } = useSelector(interactiveImportSelector);

  const { isDeleting, deleteError } = useSelector(gameFilesInfoSelector);
  const importMode = useSelector(importModeSelector);

  const [invalidRowsSelected, setInvalidRowsSelected] = useState<number[]>([]);
  const [withoutGameFileIdRowsSelected, setWithoutGameFileIdRowsSelected] =
    useState<number[]>([]);
  const [selectModalOpen, setSelectModalOpen] = useState<SelectType | null>(
    null
  );
  const [isConfirmDeleteModalOpen, setIsConfirmDeleteModalOpen] =
    useState(false);
  const [filterExistingFiles, setFilterExistingFiles] = useState(false);
  const [interactiveImportErrorMessage, setInteractiveImportErrorMessage] =
    useState<string | null>(null);
  const [selectState, setSelectState] = useSelectState();
  const { allSelected, allUnselected, selectedState } = selectState;
  const previousIsDeleting = usePrevious(isDeleting);
  const dispatch = useDispatch();
  const isInitializedRef = useRef(false);

  const columns: Column[] = useMemo(() => {
    const result: Column[] = cloneDeep(COLUMNS);

    if (!showGame) {
      const gameColumn = result.find((c) => c.name === 'game');

      if (gameColumn) {
        gameColumn.isVisible = false;
      }
    }

    const showIndexerFlags = items.some((item) => item.indexerFlags);

    if (!showIndexerFlags) {
      const indexerFlagsColumn = result.find((c) => c.name === 'indexerFlags');

      if (indexerFlagsColumn) {
        indexerFlagsColumn.isVisible = false;
      }
    }

    return result;
  }, [showGame, items]);

  const selectedIds: number[] = useMemo(() => {
    return getSelectedIds(selectedState);
  }, [selectedState]);

  const bulkSelectOptions = useMemo(() => {
    const options: SelectInputOption[] = [
      {
        key: 'select',
        value: translate('SelectDropdown'),
        disabled: true,
      },
      {
        key: 'quality',
        value: translate('SelectQuality'),
      },
      {
        key: 'releaseGroup',
        value: translate('SelectReleaseGroup'),
      },
      {
        key: 'language',
        value: translate('SelectLanguage'),
      },
      {
        key: 'indexerFlags',
        value: translate('SelectIndexerFlags'),
      },
    ];

    if (allowGameChange) {
      options.splice(1, 0, {
        key: 'game',
        value: translate('SelectGame'),
      });
    }

    return options;
  }, [allowGameChange]);

  useEffect(() => {
    if (isInitializedRef.current) {
      return;
    }

    isInitializedRef.current = true;

    if (initialSortKey) {
      const sortProps: { sortKey: string; sortDirection?: string } = {
        sortKey: initialSortKey,
      };

      if (initialSortDirection) {
        sortProps.sortDirection = initialSortDirection;
      }

      dispatch(setInteractiveImportSort(sortProps));
    }

    dispatch(
      fetchInteractiveImportItems({
        downloadId,
        gameId,
        folder,
        filterExistingFiles,
      })
    );

    // returned function will be called on component unmount
    return () => {
      dispatch(clearInteractiveImport());
    };
  }, [
    dispatch,
    downloadId,
    gameId,
    folder,
    filterExistingFiles,
    initialSortKey,
    initialSortDirection,
  ]);

  useEffect(() => {
    if (!isDeleting && previousIsDeleting && !deleteError) {
      onModalClose();
    }
  }, [previousIsDeleting, isDeleting, deleteError, onModalClose]);

  const onSelectAllChange = useCallback(
    ({ value }: CheckInputChanged) => {
      setSelectState({ type: value ? 'selectAll' : 'unselectAll', items });
    },
    [items, setSelectState]
  );

  const onSelectedChange = useCallback<InteractiveImportSelectedChangeCallback>(
    ({ id, value, hasGameFileId, shiftKey = false }) => {
      setSelectState({
        type: 'toggleSelected',
        items,
        id,
        isSelected: value,
        shiftKey,
      });

      setWithoutGameFileIdRowsSelected(
        hasGameFileId || !value
          ? without(withoutGameFileIdRowsSelected, id as number)
          : [...withoutGameFileIdRowsSelected, id as number]
      );
    },
    [
      items,
      withoutGameFileIdRowsSelected,
      setSelectState,
      setWithoutGameFileIdRowsSelected,
    ]
  );

  const onValidRowChange = useCallback(
    (id: number, isValid: boolean) => {
      if (isValid && invalidRowsSelected.includes(id)) {
        setInvalidRowsSelected(without(invalidRowsSelected, id));
      } else if (!isValid && !invalidRowsSelected.includes(id)) {
        setInvalidRowsSelected([...invalidRowsSelected, id]);
      }
    },
    [invalidRowsSelected, setInvalidRowsSelected]
  );

  const onDeleteSelectedPress = useCallback(() => {
    setIsConfirmDeleteModalOpen(true);
  }, [setIsConfirmDeleteModalOpen]);

  const onConfirmDelete = useCallback(() => {
    setIsConfirmDeleteModalOpen(false);

    const gameFileIds = items.reduce((acc: number[], item) => {
      if (selectedIds.indexOf(item.id) > -1 && item.gameFileId) {
        acc.push(item.gameFileId);
      }

      return acc;
    }, []);

    dispatch(deleteGameFiles({ gameFileIds }));
  }, [items, selectedIds, setIsConfirmDeleteModalOpen, dispatch]);

  const onConfirmDeleteModalClose = useCallback(() => {
    setIsConfirmDeleteModalOpen(false);
  }, [setIsConfirmDeleteModalOpen]);

  const onImportSelectedPress = useCallback(() => {
    const finalImportMode = downloadId || !showImportMode ? 'auto' : importMode;

    const existingFiles: Partial<GameFile>[] = [];
    const files: InteractiveImportCommandOptions[] = [];

    if (finalImportMode === 'chooseImportMode') {
      setInteractiveImportErrorMessage(
        translate('InteractiveImportNoImportMode')
      );

      return;
    }

    items.forEach((item) => {
      const isSelected = selectedIds.indexOf(item.id) > -1;

      if (isSelected) {
        const {
          game,
          releaseGroup,
          quality,
          languages,
          indexerFlags,
          gameFileId,
        } = item;

        if (!game) {
          setInteractiveImportErrorMessage(
            translate('InteractiveImportNoGame')
          );
          return;
        }

        if (!quality) {
          setInteractiveImportErrorMessage(
            translate('InteractiveImportNoQuality')
          );
          return;
        }

        if (!languages) {
          setInteractiveImportErrorMessage(
            translate('InteractiveImportNoLanguage')
          );
          return;
        }

        setInteractiveImportErrorMessage(null);

        if (gameFileId) {
          const originalItem = originalItems.find((i) => i.id === item.id);

          if (isSameGameFile(item, originalItem)) {
            existingFiles.push({
              id: gameFileId,
              releaseGroup,
              quality,
              languages,
              indexerFlags,
            });

            return;
          }
        }

        files.push({
          path: item.path,
          folderName: item.folderName,
          gameId: game.id,
          releaseGroup,
          quality,
          languages,
          indexerFlags,
          downloadId,
          gameFileId,
        });
      }
    });

    let shouldClose = false;

    if (existingFiles.length) {
      dispatch(
        updateGameFiles({
          files: existingFiles,
        })
      );

      shouldClose = true;
    }

    if (files.length) {
      dispatch(
        executeCommand({
          name: commandNames.INTERACTIVE_IMPORT,
          files,
          importMode: finalImportMode,
        })
      );

      shouldClose = true;
    }

    if (shouldClose) {
      onModalClose();
    }
  }, [
    downloadId,
    showImportMode,
    importMode,
    items,
    originalItems,
    selectedIds,
    onModalClose,
    dispatch,
  ]);

  const onSortPress = useCallback<SortCallback>(
    (sortKey, sortDirection) => {
      dispatch(setInteractiveImportSort({ sortKey, sortDirection }));
    },
    [dispatch]
  );

  const onFilterExistingFilesChange = useCallback(
    (value: string) => {
      const filter = value !== 'all';

      setFilterExistingFiles(filter);

      dispatch(
        fetchInteractiveImportItems({
          downloadId,
          gameId,
          folder,
          filterExistingFiles: filter,
        })
      );
    },
    [downloadId, gameId, folder, setFilterExistingFiles, dispatch]
  );

  const onImportModeChange = useCallback<
    ({ value }: { value: ImportMode }) => void
  >(
    ({ value }) => {
      dispatch(setInteractiveImportMode({ importMode: value }));
    },
    [dispatch]
  );

  const onSelectModalSelect = useCallback<
    ({ value }: { value: SelectType }) => void
  >(
    ({ value }) => {
      setSelectModalOpen(value);
    },
    [setSelectModalOpen]
  );

  const onSelectModalClose = useCallback(() => {
    setSelectModalOpen(null);
  }, [setSelectModalOpen]);

  const onGameSelect = useCallback(
    (game: Game) => {
      dispatch(
        updateInteractiveImportItems({
          ids: selectedIds,
          game,
        })
      );

      dispatch(reprocessInteractiveImportItems({ ids: selectedIds }));

      setSelectModalOpen(null);
    },
    [selectedIds, setSelectModalOpen, dispatch]
  );

  const onReleaseGroupSelect = useCallback(
    (releaseGroup: string) => {
      dispatch(
        updateInteractiveImportItems({
          ids: selectedIds,
          releaseGroup,
        })
      );

      dispatch(reprocessInteractiveImportItems({ ids: selectedIds }));

      setSelectModalOpen(null);
    },
    [selectedIds, dispatch]
  );

  const onLanguagesSelect = useCallback(
    (newLanguages: Language[]) => {
      dispatch(
        updateInteractiveImportItems({
          ids: selectedIds,
          languages: newLanguages,
        })
      );

      dispatch(reprocessInteractiveImportItems({ ids: selectedIds }));

      setSelectModalOpen(null);
    },
    [selectedIds, dispatch]
  );

  const onQualitySelect = useCallback(
    (quality: QualityModel) => {
      dispatch(
        updateInteractiveImportItems({
          ids: selectedIds,
          quality,
        })
      );

      dispatch(reprocessInteractiveImportItems({ ids: selectedIds }));

      setSelectModalOpen(null);
    },
    [selectedIds, dispatch]
  );

  const onIndexerFlagsSelect = useCallback(
    (indexerFlags: number) => {
      dispatch(
        updateInteractiveImportItems({
          ids: selectedIds,
          indexerFlags,
        })
      );

      dispatch(reprocessInteractiveImportItems({ ids: selectedIds }));

      setSelectModalOpen(null);
    },
    [selectedIds, dispatch]
  );

  const errorMessage = getErrorMessage(
    error,
    translate('InteractiveImportLoadError')
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {modalTitle} - {title || folder}
      </ModalHeader>

      <ModalBody scrollDirection={scrollDirections.BOTH}>
        {showFilterExistingFiles && (
          <div className={styles.filterContainer}>
            <Menu alignMenu={align.RIGHT}>
              <MenuButton>
                <Icon name={icons.FILTER} size={22} />

                <div className={styles.filterText}>
                  {filterExistingFiles
                    ? translate('UnmappedFilesOnly')
                    : translate('AllFiles')}
                </div>
              </MenuButton>

              <MenuContent>
                <SelectedMenuItem
                  name="all"
                  isSelected={!filterExistingFiles}
                  onPress={onFilterExistingFilesChange}
                >
                  {translate('AllFiles')}
                </SelectedMenuItem>

                <SelectedMenuItem
                  name="new"
                  isSelected={filterExistingFiles}
                  onPress={onFilterExistingFilesChange}
                >
                  {translate('UnmappedFilesOnly')}
                </SelectedMenuItem>
              </MenuContent>
            </Menu>
          </div>
        )}

        {isFetching ? <LoadingIndicator /> : null}

        {error ? <div>{errorMessage}</div> : null}

        {isPopulated && !!items.length && !isFetching && !isFetching ? (
          <Table
            columns={columns}
            horizontalScroll={true}
            selectAll={true}
            allSelected={allSelected}
            allUnselected={allUnselected}
            sortKey={sortKey}
            sortDirection={sortDirection}
            onSortPress={onSortPress}
            onSelectAllChange={onSelectAllChange}
          >
            <TableBody>
              {items.map((item) => {
                return (
                  <InteractiveImportRow
                    key={item.id}
                    isSelected={selectedState[item.id]}
                    {...item}
                    allowGameChange={allowGameChange}
                    columns={columns}
                    modalTitle={modalTitle}
                    onSelectedChange={onSelectedChange}
                    onValidRowChange={onValidRowChange}
                  />
                );
              })}
            </TableBody>
          </Table>
        ) : null}

        {isPopulated && !items.length && !isFetching
          ? translate('InteractiveImportNoFilesFound')
          : null}
      </ModalBody>

      <ModalFooter className={styles.footer}>
        <div className={styles.leftButtons}>
          {showDelete ? (
            <SpinnerButton
              className={styles.deleteButton}
              kind={kinds.DANGER}
              isSpinning={isDeleting}
              isDisabled={
                !selectedIds.length || !!withoutGameFileIdRowsSelected.length
              }
              onPress={onDeleteSelectedPress}
            >
              {translate('Delete')}
            </SpinnerButton>
          ) : null}

          {!downloadId && showImportMode ? (
            <SelectInput
              className={styles.importMode}
              name="importMode"
              value={importMode}
              values={importModeOptions}
              onChange={onImportModeChange}
            />
          ) : null}

          <SelectInput
            className={styles.bulkSelect}
            name="select"
            value="select"
            values={bulkSelectOptions}
            isDisabled={!selectedIds.length}
            onChange={onSelectModalSelect}
          />
        </div>

        <div className={styles.rightButtons}>
          <Button onPress={onModalClose}>{translate('Cancel')}</Button>

          {interactiveImportErrorMessage && (
            <span className={styles.errorMessage}>
              {interactiveImportErrorMessage}
            </span>
          )}

          <Button
            kind={kinds.SUCCESS}
            isDisabled={!selectedIds.length || !!invalidRowsSelected.length}
            onPress={onImportSelectedPress}
          >
            {translate('Import')}
          </Button>
        </div>
      </ModalFooter>

      <SelectGameModal
        isOpen={selectModalOpen === 'game'}
        modalTitle={modalTitle}
        onGameSelect={onGameSelect}
        onModalClose={onSelectModalClose}
      />

      <SelectReleaseGroupModal
        isOpen={selectModalOpen === 'releaseGroup'}
        releaseGroup=""
        modalTitle={modalTitle}
        onReleaseGroupSelect={onReleaseGroupSelect}
        onModalClose={onSelectModalClose}
      />

      <SelectLanguageModal
        isOpen={selectModalOpen === 'language'}
        languageIds={[0]}
        modalTitle={modalTitle}
        onLanguagesSelect={onLanguagesSelect}
        onModalClose={onSelectModalClose}
      />

      <SelectQualityModal
        isOpen={selectModalOpen === 'quality'}
        qualityId={0}
        proper={false}
        real={false}
        modalTitle={modalTitle}
        onQualitySelect={onQualitySelect}
        onModalClose={onSelectModalClose}
      />

      <SelectIndexerFlagsModal
        isOpen={selectModalOpen === 'indexerFlags'}
        indexerFlags={0}
        modalTitle={modalTitle}
        onIndexerFlagsSelect={onIndexerFlagsSelect}
        onModalClose={onSelectModalClose}
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
    </ModalContent>
  );
}

export default InteractiveImportModalContent;
