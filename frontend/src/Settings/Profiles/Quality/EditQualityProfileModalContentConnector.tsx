import _ from 'lodash';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import {
  fetchQualityProfileSchema,
  saveQualityProfile,
  setQualityProfileValue,
} from 'Store/Actions/settingsActions';
import createProfileInUseSelector from 'Store/Selectors/createProfileInUseSelector';
import { createProviderSettingsSelectorHook } from 'Store/Selectors/createProviderSettingsSelector';
import { InputChanged } from 'typings/inputs';
import EditQualityProfileModalContent, {
  FormatItem,
} from './EditQualityProfileModalContent';

interface QualityItem {
  id?: number;
  name?: string;
  allowed?: boolean;
  quality?: { id: number; name: string };
  items?: QualityItem[];
}

function getQualityItemGroupId(qualityProfile: {
  items: { value: Array<{ id?: number }> };
}) {
  const ids = _.filter(
    _.map(qualityProfile.items.value, 'id'),
    (i) => i != null
  );

  return Math.max(1000, ...ids) + 1;
}

function parseIndex(index: string): [number | null, number] {
  const split = index.split('.');

  if (split.length === 1) {
    return [null, parseInt(split[0]) - 1];
  }

  return [parseInt(split[0]) - 1, parseInt(split[1]) - 1];
}

function createQualitiesSelector() {
  return createSelector(
    createProviderSettingsSelectorHook('qualityProfiles', undefined),
    (qualityProfile) => {
      const items = qualityProfile.item.items;
      if (!items || !items.value) {
        return [];
      }

      return _.reduceRight(
        items.value,
        (
          result: Array<{ key: number; value: string }>,
          { allowed, id, name, quality }: QualityItem
        ) => {
          if (allowed) {
            if (id) {
              result.push({ key: id, value: name ?? '' });
            } else if (quality) {
              result.push({ key: quality.id, value: quality.name });
            }
          }
          return result;
        },
        []
      );
    }
  );
}

function createFormatsSelector() {
  return createSelector(
    createProviderSettingsSelectorHook('qualityProfiles', undefined),
    (customFormat) => {
      const items = customFormat.item.formatItems;
      if (!items || !items.value) {
        return [];
      }

      return _.reduceRight(
        items.value,
        (
          result: Array<{ key: number; value: string; score: number }>,
          { id, name, format, score }: FormatItem
        ) => {
          if (id) {
            result.push({ key: id, value: name ?? '', score: score ?? 0 });
          } else if (format != null) {
            result.push({ key: format, value: name ?? '', score: score ?? 0 });
          }
          return result;
        },
        []
      );
    }
  );
}

function createLanguagesSelectorForProfiles() {
  return createSelector(
    (state: {
      settings: { languages: { items: Array<{ id: number; name: string }> } };
    }) => state.settings.languages,
    (languages) => {
      const items = languages.items;
      const filterItems = ['Unknown'];

      if (!items) {
        return [];
      }

      return items
        .filter((lang) => !filterItems.includes(lang.name))
        .map((item) => ({ key: item.id, value: item.name }));
    }
  );
}

function createMapStateSelector(id: number | undefined) {
  const profileInUseSelector = createProfileInUseSelector('qualityProfileId');

  return createSelector(
    createProviderSettingsSelectorHook('qualityProfiles', id),
    createQualitiesSelector(),
    createFormatsSelector(),
    createLanguagesSelectorForProfiles(),
    (state: AppState) => profileInUseSelector(state, { id: id ?? 0 }),
    (qualityProfile, qualities, customFormats, languages, isInUse) => {
      return {
        qualities,
        customFormats,
        languages,
        ...qualityProfile,
        isInUse,
      };
    }
  );
}

interface QualityProfilePending {
  cutoff: { value: number };
  items: { value: QualityItem[] };
  formatItems: { value: FormatItem[] };
  [key: string]: { value: unknown };
}

interface EditQualityProfileModalContentConnectorProps {
  id?: number;
  onContentHeightChange: (height: number) => void;
  onModalClose: () => void;
}

function EditQualityProfileModalContentConnector({
  id,
  onContentHeightChange,
  onModalClose,
}: EditQualityProfileModalContentConnectorProps) {
  const dispatch = useDispatch();

  const {
    isFetching,
    isPopulated,
    isSaving,
    saveError,
    item,
    qualities,
    customFormats,
    languages,
    isInUse,
    ...otherSettings
  } = useSelector(createMapStateSelector(id));

  const typedItem = item as unknown as QualityProfilePending;

  const [dragQualityIndex, setDragQualityIndex] = useState<string | null>(null);
  const [dropQualityIndex, setDropQualityIndex] = useState<string | null>(null);
  const [dropPosition, setDropPosition] = useState<string | null>(null);
  const [editGroups, setEditGroups] = useState(false);

  const prevIsSaving = useRef(isSaving);

  useEffect(() => {
    if (!id && !isPopulated) {
      dispatch(fetchQualityProfileSchema());
    }
  }, [dispatch, id, isPopulated]);

  useEffect(() => {
    if (prevIsSaving.current && !isSaving && !saveError) {
      onModalClose();
    }
    prevIsSaving.current = isSaving;
  }, [isSaving, saveError, onModalClose]);

  const ensureCutoff = useCallback(
    (qualityProfile: {
      cutoff: { value: number };
      items: { value: QualityItem[] };
    }) => {
      const cutoff = qualityProfile.cutoff.value;

      const cutoffItem = _.find(
        qualityProfile.items.value,
        (i: QualityItem) => {
          if (!cutoff) {
            return false;
          }
          return i.id === cutoff || (i.quality && i.quality.id === cutoff);
        }
      ) as QualityItem | undefined;

      if (!cutoff || !cutoffItem || !cutoffItem.allowed) {
        const firstAllowed = _.find(qualityProfile.items.value, {
          allowed: true,
        });
        let cutoffId = null;

        if (firstAllowed) {
          cutoffId = firstAllowed.quality
            ? firstAllowed.quality.id
            : firstAllowed.id;
        }

        dispatch(setQualityProfileValue({ name: 'cutoff', value: cutoffId }));
      }
    },
    [dispatch]
  );

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(setQualityProfileValue({ name, value }));
    },
    [dispatch]
  );

  const handleCutoffChange = useCallback(
    ({ name, value }: InputChanged) => {
      const numId = parseInt(value as string);
      const foundItem = _.find(typedItem.items.value, (i: QualityItem) => {
        if (i.quality) {
          return i.quality.id === numId;
        }
        return i.id === numId;
      }) as QualityItem | undefined;

      if (!foundItem) {
        return;
      }

      const cutoffId = foundItem.quality ? foundItem.quality.id : foundItem.id;
      dispatch(setQualityProfileValue({ name, value: cutoffId }));
    },
    [dispatch, typedItem]
  );

  const handleLanguageChange = useCallback(
    ({ name, value }: InputChanged) => {
      const numId = parseInt(value as string);
      const language = _.find(
        languages,
        (lang: { key: number }) => lang.key === numId
      );

      if (language) {
        dispatch(
          setQualityProfileValue({
            name,
            value: { id: language.key, Name: language.value },
          })
        );
      }
    },
    [dispatch, languages]
  );

  const handleSavePress = useCallback(() => {
    dispatch(saveQualityProfile({ id }));
  }, [dispatch, id]);

  const handleQualityProfileItemAllowedChange = useCallback(
    (qualityId: number, allowed: boolean) => {
      const qualityProfile = _.cloneDeep(typedItem);
      const items = qualityProfile.items.value;
      const foundItem = _.find(
        items,
        (i: QualityItem) => i.quality && i.quality.id === qualityId
      ) as QualityItem | undefined;

      if (!foundItem) {
        return;
      }

      foundItem.allowed = allowed;

      dispatch(setQualityProfileValue({ name: 'items', value: items }));
      ensureCutoff(qualityProfile);
    },
    [dispatch, typedItem, ensureCutoff]
  );

  const handleQualityProfileFormatItemScoreChange = useCallback(
    (formatId: number, score: number) => {
      const qualityProfile = _.cloneDeep(typedItem);
      const formatItems = qualityProfile.formatItems.value;
      const foundItem = _.find(
        formatItems,
        (i: FormatItem) => i.format === formatId
      ) as FormatItem | undefined;

      if (!foundItem) {
        return;
      }

      foundItem.score = score;

      dispatch(
        setQualityProfileValue({ name: 'formatItems', value: formatItems })
      );
    },
    [dispatch, typedItem]
  );

  const handleItemGroupAllowedChange = useCallback(
    (groupId: number, allowed: boolean) => {
      const qualityProfile = _.cloneDeep(typedItem);
      const items = qualityProfile.items.value;
      const group = _.find(items, (i: QualityItem) => i.id === groupId) as
        | QualityItem
        | undefined;

      if (!group) {
        return;
      }

      group.allowed = allowed;
      (group.items ?? []).forEach((i: QualityItem) => {
        i.allowed = allowed;
      });

      dispatch(setQualityProfileValue({ name: 'items', value: items }));
      ensureCutoff(qualityProfile);
    },
    [dispatch, typedItem, ensureCutoff]
  );

  const handleItemGroupNameChange = useCallback(
    (groupId: number, name: string) => {
      const qualityProfile = _.cloneDeep(typedItem);
      const items = qualityProfile.items.value;
      const group = _.find(items, (i: QualityItem) => i.id === groupId) as
        | QualityItem
        | undefined;

      if (!group) {
        return;
      }

      group.name = name;

      dispatch(setQualityProfileValue({ name: 'items', value: items }));
    },
    [dispatch, typedItem]
  );

  const handleCreateGroupPress = useCallback(
    (qualityId: number) => {
      const qualityProfile = _.cloneDeep(typedItem);
      const items = qualityProfile.items.value;
      const foundItem = _.find(
        items,
        (i: QualityItem) => i.quality && i.quality.id === qualityId
      ) as QualityItem | undefined;

      if (!foundItem || !foundItem.quality) {
        return;
      }

      const index = items.indexOf(foundItem);
      const groupId = getQualityItemGroupId(qualityProfile);

      const group: QualityItem = {
        id: groupId,
        name: foundItem.quality.name,
        allowed: foundItem.allowed,
        items: [foundItem],
      };

      items.splice(index, 1, group);

      dispatch(setQualityProfileValue({ name: 'items', value: items }));
      ensureCutoff(qualityProfile);
    },
    [dispatch, typedItem, ensureCutoff]
  );

  const handleDeleteGroupPress = useCallback(
    (groupId: number) => {
      const qualityProfile = _.cloneDeep(typedItem);
      const items = qualityProfile.items.value;
      const group = _.find(items, (i: QualityItem) => i.id === groupId) as
        | QualityItem
        | undefined;

      if (!group || !group.items) {
        return;
      }

      const index = items.indexOf(group);

      items.splice(index, 1, ...group.items);

      dispatch(setQualityProfileValue({ name: 'items', value: items }));
      ensureCutoff(qualityProfile);
    },
    [dispatch, typedItem, ensureCutoff]
  );

  const handleQualityProfileItemDragMove = useCallback(
    (options: {
      dragQualityIndex: string;
      dropQualityIndex: string;
      dropPosition: string;
    }) => {
      const {
        dragQualityIndex: newDragIndex,
        dropQualityIndex: newDropIndex,
        dropPosition: newDropPosition,
      } = options;

      const [dragGroupIndex, dragItemIndex] = parseIndex(newDragIndex);
      const [dropGroupIndex, dropItemIndex] = parseIndex(newDropIndex);

      if (
        (newDropPosition === 'below' && dropItemIndex - 1 === dragItemIndex) ||
        (newDropPosition === 'above' && dropItemIndex + 1 === dragItemIndex)
      ) {
        setDragQualityIndex(null);
        setDropQualityIndex(null);
        setDropPosition(null);
        return;
      }

      let adjustedDropIndex = newDropIndex;

      if (
        newDropPosition === 'above' &&
        dragGroupIndex !== dropGroupIndex &&
        dropGroupIndex != null
      ) {
        adjustedDropIndex = `${dropGroupIndex + 1}.${dropItemIndex + 2}`;
      }

      if (
        newDropPosition === 'above' &&
        dragGroupIndex !== dropGroupIndex &&
        dropGroupIndex == null
      ) {
        adjustedDropIndex = `${dropItemIndex + 2}`;
      }

      if (
        newDropPosition === 'below' &&
        dragGroupIndex === dropGroupIndex &&
        dropGroupIndex != null &&
        dragItemIndex < dropItemIndex
      ) {
        adjustedDropIndex = `${dropGroupIndex + 1}.${dropItemIndex}`;
      }

      if (
        newDropPosition === 'below' &&
        dragGroupIndex === dropGroupIndex &&
        dropGroupIndex == null &&
        dragItemIndex < dropItemIndex
      ) {
        adjustedDropIndex = `${dropItemIndex}`;
      }

      setDragQualityIndex(newDragIndex);
      setDropQualityIndex(adjustedDropIndex);
      setDropPosition(newDropPosition);
    },
    []
  );

  const handleQualityProfileItemDragEnd = useCallback(
    (didDrop: boolean) => {
      if (didDrop && dropQualityIndex != null && dragQualityIndex != null) {
        const qualityProfile = _.cloneDeep(typedItem);
        const items = qualityProfile.items.value;
        const [dragGroupIdx, dragItemIdx] = parseIndex(dragQualityIndex);
        const [dropGroupIdx, dropItemIdx] = parseIndex(dropQualityIndex);

        let draggedItem: QualityItem | null = null;
        let dropGroup: QualityItem | null = null;

        if (dropGroupIdx != null) {
          dropGroup = items[dropGroupIdx];
        }

        if (dragGroupIdx == null) {
          draggedItem = items.splice(dragItemIdx, 1)[0];
        } else {
          const group = items[dragGroupIdx];
          if (group.items) {
            draggedItem = group.items.splice(dragItemIdx, 1)[0];

            if (!group.items.length) {
              items.splice(dragGroupIdx, 1);
            }
          }
        }

        if (draggedItem) {
          if (dropGroupIdx == null) {
            items.splice(dropItemIdx, 0, draggedItem);
          } else if (dropGroup?.items) {
            dropGroup.items.splice(dropItemIdx, 0, draggedItem);
          }
        }

        dispatch(setQualityProfileValue({ name: 'items', value: items }));
        ensureCutoff(qualityProfile);
      }

      setDragQualityIndex(null);
      setDropQualityIndex(null);
      setDropPosition(null);
    },
    [dispatch, typedItem, ensureCutoff, dragQualityIndex, dropQualityIndex]
  );

  const handleToggleEditGroupsMode = useCallback(() => {
    setEditGroups((prev) => !prev);
  }, []);

  if (_.isEmpty(item.items) && !isFetching) {
    return null;
  }

  return (
    <EditQualityProfileModalContent
      dragQualityIndex={dragQualityIndex}
      dropQualityIndex={dropQualityIndex}
      dropPosition={dropPosition}
      editGroups={editGroups}
      id={id}
      isFetching={isFetching}
      isSaving={isSaving}
      saveError={saveError}
      item={
        item as unknown as React.ComponentProps<
          typeof EditQualityProfileModalContent
        >['item']
      }
      qualities={qualities}
      customFormats={customFormats}
      languages={languages}
      isInUse={isInUse}
      onContentHeightChange={onContentHeightChange}
      onModalClose={onModalClose}
      onSavePress={handleSavePress}
      onInputChange={handleInputChange}
      onCutoffChange={handleCutoffChange}
      onLanguageChange={handleLanguageChange}
      onCreateGroupPress={handleCreateGroupPress}
      onDeleteGroupPress={handleDeleteGroupPress}
      onQualityProfileItemAllowedChange={handleQualityProfileItemAllowedChange}
      onItemGroupAllowedChange={handleItemGroupAllowedChange}
      onItemGroupNameChange={handleItemGroupNameChange}
      onQualityProfileItemDragMove={handleQualityProfileItemDragMove}
      onQualityProfileItemDragEnd={handleQualityProfileItemDragEnd}
      onQualityProfileFormatItemScoreChange={
        handleQualityProfileFormatItemScoreChange
      }
      onToggleEditGroupsMode={handleToggleEditGroupsMode}
      {...otherSettings}
    />
  );
}

export default EditQualityProfileModalContentConnector;
