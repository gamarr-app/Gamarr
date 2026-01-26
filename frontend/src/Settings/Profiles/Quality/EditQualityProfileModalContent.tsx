import { useCallback, useEffect, useRef, useState } from 'react';
import { Error } from 'App/State/AppSectionState';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Measure from 'Components/Measure';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import { InputChanged } from 'typings/inputs';
import { Failure } from 'typings/pending';
import translate from 'Utilities/String/translate';
import QualityProfileFormatItems from './QualityProfileFormatItems';
import QualityProfileItems from './QualityProfileItems';
import styles from './EditQualityProfileModalContent.css';

const MODAL_BODY_PADDING = parseInt(dimensions.modalBodyPadding);

const platformOptions = [
  { key: 1, value: 'PC', order: 1 },
  { key: 8, value: 'Linux', order: 2 },
  { key: 9, value: 'Mac', order: 3 },
  { key: 2, value: 'PlayStation', order: 4 },
  { key: 3, value: 'Xbox', order: 5 },
  { key: 4, value: 'Nintendo', order: 6 },
];

interface PendingValue<T> {
  value: T;
  errors?: Failure[];
  warnings?: Failure[];
  pending?: boolean;
  previousValue?: T;
}

export interface QualityProfileItemValue {
  id?: number;
  name?: string;
  allowed?: boolean;
  quality?: { id: number; name: string };
  items?: QualityProfileItemValue[];
}

export interface FormatItem {
  id?: number;
  name: string;
  format: number;
  score: number;
}

export interface QualityProfilePendingItem {
  id: PendingValue<number>;
  name: PendingValue<string>;
  upgradeAllowed: PendingValue<boolean>;
  cutoff: PendingValue<number>;
  minFormatScore: PendingValue<number>;
  minUpgradeFormatScore: PendingValue<number>;
  cutoffFormatScore: PendingValue<number>;
  language: PendingValue<{ id: number; Name?: string }>;
  preferredPlatforms: PendingValue<number[]>;
  items: PendingValue<QualityProfileItemValue[]>;
  formatItems: PendingValue<FormatItem[]>;
  [key: string]: PendingValue<unknown>;
}

function getCustomFormatRender(
  formatItems: PendingValue<FormatItem[]>,
  otherProps: Record<string, unknown>
) {
  return (
    <QualityProfileFormatItems
      profileFormatItems={formatItems.value}
      errors={formatItems.errors}
      warnings={formatItems.warnings}
      {...otherProps}
    />
  );
}

interface EditQualityProfileModalContentProps {
  editGroups: boolean;
  isFetching: boolean;
  error?: Error | null;
  isSaving: boolean;
  saveError?: Error;
  qualities: Array<{ key: number; value: string }>;
  customFormats: Array<{ key: number; value: string; score: number }>;
  languages: Array<{ key: number; value: string }>;
  item: QualityProfilePendingItem;
  isInUse: boolean;
  dragQualityIndex?: string | null;
  dropQualityIndex?: string | null;
  dropPosition?: string | null;
  onInputChange: (change: InputChanged) => void;
  onCutoffChange: (change: InputChanged) => void;
  onLanguageChange: (change: InputChanged) => void;
  onToggleEditGroupsMode: () => void;
  onSavePress: () => void;
  onContentHeightChange: (height: number) => void;
  onModalClose: () => void;
  onDeleteQualityProfilePress?: () => void;
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

function EditQualityProfileModalContent({
  editGroups,
  isFetching,
  error,
  isSaving,
  saveError,
  qualities,
  languages,
  item,
  isInUse,
  dragQualityIndex,
  dropQualityIndex,
  dropPosition,
  onInputChange,
  onCutoffChange,
  onLanguageChange,
  onSavePress,
  onToggleEditGroupsMode,
  onContentHeightChange,
  onModalClose,
  onDeleteQualityProfilePress,
  onQualityProfileItemAllowedChange,
  onQualityProfileItemDragMove,
  onQualityProfileItemDragEnd,
  ...otherProps
}: EditQualityProfileModalContentProps) {
  const [headerHeight, setHeaderHeight] = useState(0);
  const [bodyHeight, setBodyHeight] = useState(0);
  const [footerHeight, setFooterHeight] = useState(0);

  const prevDimensions = useRef({
    headerHeight: 0,
    bodyHeight: 0,
    footerHeight: 0,
  });

  useEffect(() => {
    if (
      headerHeight > 0 &&
      bodyHeight > 0 &&
      footerHeight > 0 &&
      (headerHeight !== prevDimensions.current.headerHeight ||
        bodyHeight !== prevDimensions.current.bodyHeight ||
        footerHeight !== prevDimensions.current.footerHeight)
    ) {
      const padding = MODAL_BODY_PADDING * 2;
      onContentHeightChange(headerHeight + bodyHeight + footerHeight + padding);
    }
    prevDimensions.current = { headerHeight, bodyHeight, footerHeight };
  }, [headerHeight, bodyHeight, footerHeight, onContentHeightChange]);

  const handleHeaderMeasure = useCallback(
    ({ height = 0 }: { height?: number }) => {
      setHeaderHeight((prev) => (height > prev ? height : prev));
    },
    []
  );

  const handleBodyMeasure = useCallback(
    ({ height = 0 }: { height?: number }) => {
      setBodyHeight((prev) => (height > prev ? height : prev));
    },
    []
  );

  const handleFooterMeasure = useCallback(
    ({ height = 0 }: { height?: number }) => {
      setFooterHeight((prev) => (height > prev ? height : prev));
    },
    []
  );

  const {
    id,
    name,
    upgradeAllowed,
    cutoff,
    minFormatScore,
    minUpgradeFormatScore,
    cutoffFormatScore,
    language,
    preferredPlatforms,
    items,
    formatItems,
  } = item;

  const languageId = language ? language.value.id : 0;

  return (
    <ModalContent onModalClose={onModalClose}>
      <Measure
        whitelist={['height']}
        includeMargin={false}
        onMeasure={handleHeaderMeasure}
      >
        <ModalHeader>
          {id
            ? translate('EditQualityProfile')
            : translate('AddQualityProfile')}
        </ModalHeader>
      </Measure>

      <ModalBody>
        <Measure whitelist={['height']} onMeasure={handleBodyMeasure}>
          <div>
            {isFetching && <LoadingIndicator />}

            {!isFetching && !!error && (
              <Alert kind={kinds.DANGER}>
                {translate('AddQualityProfileError')}
              </Alert>
            )}

            {!isFetching && !error && (
              <Form {...otherProps}>
                <div className={styles.formGroupsContainer}>
                  <div className={styles.formGroupWrapper}>
                    <FormGroup size={sizes.EXTRA_SMALL}>
                      <FormLabel size={sizes.SMALL}>
                        {translate('Name')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.TEXT}
                        name="name"
                        {...name}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    <FormGroup size={sizes.EXTRA_SMALL}>
                      <FormLabel size={sizes.SMALL}>
                        {translate('UpgradesAllowed')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.CHECK}
                        name="upgradeAllowed"
                        {...upgradeAllowed}
                        helpText={translate('UpgradesAllowedHelpText')}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    {upgradeAllowed.value && (
                      <FormGroup size={sizes.EXTRA_SMALL}>
                        <FormLabel size={sizes.SMALL}>
                          {translate('UpgradeUntil')}
                        </FormLabel>

                        <FormInputGroup
                          type={inputTypes.SELECT}
                          name="cutoff"
                          {...cutoff}
                          values={qualities}
                          helpText={translate('UpgradeUntilGameHelpText')}
                          onChange={onCutoffChange}
                        />
                      </FormGroup>
                    )}

                    {formatItems.value.length > 0 && (
                      <FormGroup size={sizes.EXTRA_SMALL}>
                        <FormLabel size={sizes.SMALL}>
                          {translate('MinimumCustomFormatScore')}
                        </FormLabel>

                        <FormInputGroup
                          type={inputTypes.NUMBER}
                          name="minFormatScore"
                          {...minFormatScore}
                          helpText={translate(
                            'MinimumCustomFormatScoreHelpText'
                          )}
                          onChange={onInputChange}
                        />
                      </FormGroup>
                    )}

                    {upgradeAllowed.value && formatItems.value.length > 0 && (
                      <FormGroup size={sizes.EXTRA_SMALL}>
                        <FormLabel size={sizes.SMALL}>
                          {translate('UpgradeUntilCustomFormatScore')}
                        </FormLabel>

                        <FormInputGroup
                          type={inputTypes.NUMBER}
                          name="cutoffFormatScore"
                          {...cutoffFormatScore}
                          helpText={translate(
                            'UpgradeUntilCustomFormatScoreGameHelpText'
                          )}
                          onChange={onInputChange}
                        />
                      </FormGroup>
                    )}

                    {upgradeAllowed.value && formatItems.value.length > 0 ? (
                      <FormGroup size={sizes.EXTRA_SMALL}>
                        <FormLabel size={sizes.SMALL}>
                          {translate('MinimumCustomFormatScoreIncrement')}
                        </FormLabel>

                        <FormInputGroup
                          type={inputTypes.NUMBER}
                          name="minUpgradeFormatScore"
                          min={1}
                          {...minUpgradeFormatScore}
                          helpText={translate(
                            'MinimumCustomFormatScoreIncrementHelpText'
                          )}
                          onChange={onInputChange}
                        />
                      </FormGroup>
                    ) : null}

                    <FormGroup size={sizes.EXTRA_SMALL}>
                      <FormLabel size={sizes.SMALL}>
                        {translate('Language')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.LANGUAGE_SELECT}
                        name="language"
                        values={languages}
                        value={languageId}
                        helpText={translate('LanguageHelpText')}
                        onChange={onLanguageChange}
                      />
                    </FormGroup>

                    <FormGroup size={sizes.EXTRA_SMALL}>
                      <FormLabel size={sizes.SMALL}>
                        {translate('PreferredPlatforms')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.TAG_SELECT}
                        name="preferredPlatforms"
                        value={
                          preferredPlatforms ? preferredPlatforms.value : []
                        }
                        values={platformOptions}
                        helpText={translate('PreferredPlatformsHelpText')}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    <div className={styles.formatItemLarge}>
                      {getCustomFormatRender(formatItems, otherProps)}
                    </div>
                  </div>

                  <div className={styles.formGroupWrapper}>
                    <QualityProfileItems
                      editGroups={editGroups}
                      dragQualityIndex={dragQualityIndex ?? undefined}
                      dropQualityIndex={dropQualityIndex ?? undefined}
                      dropPosition={dropPosition ?? undefined}
                      qualityProfileItems={items.value}
                      errors={items.errors}
                      warnings={items.warnings}
                      onToggleEditGroupsMode={onToggleEditGroupsMode}
                      onQualityProfileItemAllowedChange={
                        onQualityProfileItemAllowedChange
                      }
                      onQualityProfileItemDragMove={
                        onQualityProfileItemDragMove
                      }
                      onQualityProfileItemDragEnd={onQualityProfileItemDragEnd}
                      {...otherProps}
                    />
                  </div>

                  <div className={styles.formatItemSmall}>
                    {getCustomFormatRender(formatItems, otherProps)}
                  </div>
                </div>
              </Form>
            )}
          </div>
        </Measure>
      </ModalBody>

      <Measure
        whitelist={['height']}
        includeMargin={false}
        onMeasure={handleFooterMeasure}
      >
        <ModalFooter>
          {id ? (
            <div
              className={styles.deleteButtonContainer}
              title={
                isInUse
                  ? translate('QualityProfileInUseGameListCollection')
                  : undefined
              }
            >
              <Button
                kind={kinds.DANGER}
                isDisabled={isInUse}
                onPress={onDeleteQualityProfilePress}
              >
                {translate('Delete')}
              </Button>
            </div>
          ) : null}

          <Button onPress={onModalClose}>{translate('Cancel')}</Button>

          <SpinnerErrorButton
            isSpinning={isSaving}
            error={saveError}
            onPress={onSavePress}
          >
            {translate('Save')}
          </SpinnerErrorButton>
        </ModalFooter>
      </Measure>
    </ModalContent>
  );
}

export default EditQualityProfileModalContent;
