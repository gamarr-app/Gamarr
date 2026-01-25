import React, { useCallback, useState } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputHelpText from 'Components/Form/FormInputHelpText';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import Measure from 'Components/Measure';
import { icons, kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import QualityProfileItemDragPreview from './QualityProfileItemDragPreview';
import QualityProfileItemDragSource from './QualityProfileItemDragSource';
import styles from './QualityProfileItems.css';

interface QualityProfileItemsProps {
  editGroups: boolean;
  dragQualityIndex?: string;
  dropQualityIndex?: string;
  dropPosition?: string;
  qualityProfileItems: Array<{
    id: number;
    name: string;
    allowed: boolean;
    quality?: { id: number; name: string };
    items?: Array<{ quality: { id: number; name: string } }>;
  }>;
  errors?: Array<{ message?: string; errorMessage?: string }>;
  warnings?: Array<{ message?: string; errorMessage?: string }>;
  onToggleEditGroupsMode: () => void;
  onQualityProfileItemAllowedChange: (
    qualityId: number,
    value: boolean
  ) => void;
  onQualityProfileItemDragMove: (payload: {
    dragQualityIndex: string;
    dropQualityIndex: string;
    dropPosition: string;
  }) => void;
  onQualityProfileItemDragEnd: (didDrop: boolean) => void;
  [key: string]: unknown;
}

function QualityProfileItems({
  editGroups,
  dropQualityIndex,
  dropPosition,
  qualityProfileItems,
  errors = [],
  warnings = [],
  onToggleEditGroupsMode,
  ...otherProps
}: QualityProfileItemsProps) {
  const [qualitiesHeight, setQualitiesHeight] = useState(0);
  const [qualitiesHeightEditGroups, setQualitiesHeightEditGroups] = useState(0);

  const handleMeasure = useCallback(
    ({ height }: { height: number }) => {
      if (editGroups) {
        setQualitiesHeightEditGroups((prev) => (height > prev ? height : prev));
      } else {
        setQualitiesHeight((prev) => (height > prev ? height : prev));
      }
    },
    [editGroups]
  );

  const isDragging =
    dropQualityIndex !== null && dropQualityIndex !== undefined;
  const isDraggingUp = isDragging && dropPosition === 'above';
  const isDraggingDown = isDragging && dropPosition === 'below';
  const minHeight = editGroups ? qualitiesHeightEditGroups : qualitiesHeight;

  return (
    <FormGroup size={sizes.EXTRA_SMALL}>
      <FormLabel size={sizes.SMALL}>{translate('Qualities')}</FormLabel>

      <div>
        <FormInputHelpText text={translate('QualitiesHelpText')} />

        {errors.map((error, index) => {
          return (
            <FormInputHelpText
              key={index}
              text={error.errorMessage ?? error.message ?? ''}
              isError={true}
              isCheckInput={false}
            />
          );
        })}

        {warnings.map((warning, index) => {
          return (
            <FormInputHelpText
              key={index}
              text={warning.errorMessage ?? warning.message ?? ''}
              isWarning={true}
              isCheckInput={false}
            />
          );
        })}

        <Button
          className={styles.editGroupsButton}
          kind={kinds.PRIMARY}
          onPress={onToggleEditGroupsMode}
        >
          <div>
            <Icon
              className={styles.editGroupsButtonIcon}
              name={editGroups ? icons.REORDER : icons.GROUP}
            />

            {editGroups
              ? translate('DoneEditingGroups')
              : translate('EditGroups')}
          </div>
        </Button>

        <Measure
          whitelist={['height']}
          includeMargin={false}
          onMeasure={handleMeasure}
        >
          <div
            className={styles.qualities}
            style={{ minHeight: `${minHeight}px` }}
          >
            {qualityProfileItems
              .map(({ id, name, allowed, quality, items }, index) => {
                const identifier = quality ? quality.id : id;

                return (
                  <QualityProfileItemDragSource
                    key={identifier}
                    editGroups={editGroups}
                    groupId={id}
                    qualityId={quality && quality.id}
                    name={quality ? quality.name : name}
                    allowed={allowed}
                    items={items}
                    qualityIndex={`${index + 1}`}
                    isInGroup={false}
                    isDragging={isDragging}
                    isDraggingUp={isDraggingUp}
                    isDraggingDown={isDraggingDown}
                    {...otherProps}
                  />
                );
              })
              .reverse()}

            <QualityProfileItemDragPreview />
          </div>
        </Measure>
      </div>
    </FormGroup>
  );
}

export default QualityProfileItems;
