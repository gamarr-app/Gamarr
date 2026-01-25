import { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { useSelect } from 'App/SelectContext';
import AppState from 'App/State/AppState';
import { RENAME_GAME } from 'Commands/commandNames';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import usePrevious from 'Helpers/Hooks/usePrevious';
import { kinds } from 'Helpers/Props';
import { saveGameEditor } from 'Store/Actions/gameActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import DeleteGameModal from './Delete/DeleteGameModal';
import EditGamesModal from './Edit/EditGamesModal';
import OrganizeGamesModal from './Organize/OrganizeGamesModal';
import TagsModal from './Tags/TagsModal';
import styles from './GameIndexSelectFooter.css';

interface SavePayload {
  monitored?: boolean;
  qualityProfileId?: number;
  rootFolderPath?: string;
  moveFiles?: boolean;
}

const gameEditorSelector = createSelector(
  (state: AppState) => state.games,
  (games) => {
    const { isSaving, isDeleting, deleteError } = games;

    return {
      isSaving,
      isDeleting,
      deleteError,
    };
  }
);

function GameIndexSelectFooter() {
  const { isSaving, isDeleting, deleteError } = useSelector(gameEditorSelector);

  const isOrganizingGames = useSelector(
    createCommandExecutingSelector(RENAME_GAME)
  );

  const dispatch = useDispatch();

  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isOrganizeModalOpen, setIsOrganizeModalOpen] = useState(false);
  const [isTagsModalOpen, setIsTagsModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isSavingGames, setIsSavingGames] = useState(false);
  const [isSavingTags, setIsSavingTags] = useState(false);
  const previousIsDeleting = usePrevious(isDeleting);

  const [selectState, selectDispatch] = useSelect();
  const { selectedState } = selectState;

  const gameIds = useMemo(() => {
    return getSelectedIds(selectedState);
  }, [selectedState]);

  const selectedCount = gameIds.length ? gameIds.length : 0;

  const onEditPress = useCallback(() => {
    setIsEditModalOpen(true);
  }, [setIsEditModalOpen]);

  const onEditModalClose = useCallback(() => {
    setIsEditModalOpen(false);
  }, [setIsEditModalOpen]);

  const onSavePress = useCallback(
    (payload: SavePayload) => {
      setIsSavingGames(true);
      setIsEditModalOpen(false);

      dispatch(
        saveGameEditor({
          ...payload,
          gameIds,
        })
      );
    },
    [gameIds, dispatch]
  );

  const onOrganizePress = useCallback(() => {
    setIsOrganizeModalOpen(true);
  }, [setIsOrganizeModalOpen]);

  const onOrganizeModalClose = useCallback(() => {
    setIsOrganizeModalOpen(false);
  }, [setIsOrganizeModalOpen]);

  const onTagsPress = useCallback(() => {
    setIsTagsModalOpen(true);
  }, [setIsTagsModalOpen]);

  const onTagsModalClose = useCallback(() => {
    setIsTagsModalOpen(false);
  }, [setIsTagsModalOpen]);

  const onApplyTagsPress = useCallback(
    (tags: number[], applyTags: string) => {
      setIsSavingTags(true);
      setIsTagsModalOpen(false);

      dispatch(
        saveGameEditor({
          gameIds,
          tags,
          applyTags,
        })
      );
    },
    [gameIds, dispatch]
  );

  const onDeletePress = useCallback(() => {
    setIsDeleteModalOpen(true);
  }, [setIsDeleteModalOpen]);

  const onDeleteModalClose = useCallback(() => {
    setIsDeleteModalOpen(false);
  }, []);

  useEffect(() => {
    if (!isSaving) {
      setIsSavingGames(false);
      setIsSavingTags(false);
    }
  }, [isSaving]);

  useEffect(() => {
    if (previousIsDeleting && !isDeleting && !deleteError) {
      selectDispatch({ type: 'unselectAll' });
    }
  }, [previousIsDeleting, isDeleting, deleteError, selectDispatch]);

  useEffect(() => {
    dispatch(fetchRootFolders());
  }, [dispatch]);

  const anySelected = selectedCount > 0;

  return (
    <PageContentFooter className={styles.footer}>
      <div className={styles.buttons}>
        <div className={styles.actionButtons}>
          <SpinnerButton
            isSpinning={isSaving && isSavingGames}
            isDisabled={!anySelected || isOrganizingGames}
            onPress={onEditPress}
          >
            {translate('Edit')}
          </SpinnerButton>

          <SpinnerButton
            kind={kinds.WARNING}
            isSpinning={isOrganizingGames}
            isDisabled={!anySelected || isOrganizingGames}
            onPress={onOrganizePress}
          >
            {translate('RenameFiles')}
          </SpinnerButton>

          <SpinnerButton
            isSpinning={isSaving && isSavingTags}
            isDisabled={!anySelected || isOrganizingGames}
            onPress={onTagsPress}
          >
            {translate('SetTags')}
          </SpinnerButton>
        </div>

        <div className={styles.deleteButtons}>
          <SpinnerButton
            kind={kinds.DANGER}
            isSpinning={isDeleting}
            isDisabled={!anySelected || isDeleting}
            onPress={onDeletePress}
          >
            {translate('Delete')}
          </SpinnerButton>
        </div>
      </div>

      <div className={styles.selected}>
        {translate('GamesSelectedInterp', { count: selectedCount })}
      </div>

      <EditGamesModal
        isOpen={isEditModalOpen}
        gameIds={gameIds}
        onSavePress={onSavePress}
        onModalClose={onEditModalClose}
      />

      <TagsModal
        isOpen={isTagsModalOpen}
        gameIds={gameIds}
        onApplyTagsPress={onApplyTagsPress}
        onModalClose={onTagsModalClose}
      />

      <OrganizeGamesModal
        isOpen={isOrganizeModalOpen}
        gameIds={gameIds}
        onModalClose={onOrganizeModalClose}
      />

      <DeleteGameModal
        isOpen={isDeleteModalOpen}
        gameIds={gameIds}
        onModalClose={onDeleteModalClose}
      />
    </PageContentFooter>
  );
}

export default GameIndexSelectFooter;
