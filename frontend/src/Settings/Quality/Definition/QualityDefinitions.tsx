import _ from 'lodash';
import React, { useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import FieldSet from 'Components/FieldSet';
import PageSectionContent from 'Components/Page/PageSectionContent';
import {
  fetchQualityDefinitions,
  saveQualityDefinitions,
} from 'Store/Actions/settingsActions';
import {
  OnChildStateChange,
  SetChildSave,
} from 'typings/Settings/SettingsState';
import translate from 'Utilities/String/translate';
import QualityDefinition from './QualityDefinition';
import styles from './QualityDefinitions.css';

function createQualityDefinitionsSelector() {
  return createSelector(
    (state: AppState) => state.settings.qualityDefinitions,
    (qualityDefinitions) => {
      const items = qualityDefinitions.items.map((item) => {
        const pendingChanges = qualityDefinitions.pendingChanges[item.id] || {};
        return { ...item, ...pendingChanges };
      });

      return {
        isFetching: qualityDefinitions.isFetching,
        isPopulated: qualityDefinitions.isPopulated,
        error: qualityDefinitions.error,
        isSaving: qualityDefinitions.isSaving,
        hasPendingChanges: !_.isEmpty(qualityDefinitions.pendingChanges),
        items,
      };
    }
  );
}

interface QualityDefinitionsProps {
  setChildSave: SetChildSave;
  onChildStateChange: OnChildStateChange;
}

function QualityDefinitions({
  setChildSave,
  onChildStateChange,
}: QualityDefinitionsProps) {
  const dispatch = useDispatch();

  const selector = useMemo(createQualityDefinitionsSelector, []);
  const { isFetching, isPopulated, error, isSaving, hasPendingChanges, items } =
    useSelector(selector);

  useEffect(() => {
    dispatch(fetchQualityDefinitions());
    setChildSave(() => dispatch(saveQualityDefinitions()));
  }, [dispatch, setChildSave]);

  useEffect(() => {
    onChildStateChange({
      isSaving,
      hasPendingChanges,
    });
  }, [isSaving, hasPendingChanges, onChildStateChange]);

  return (
    <FieldSet legend={translate('QualityDefinitions')}>
      <PageSectionContent
        errorMessage={translate('QualityDefinitionsLoadError')}
        isFetching={isFetching}
        isPopulated={isPopulated}
        error={error}
      >
        <div className={styles.header}>
          <div className={styles.quality}>{translate('Quality')}</div>
          <div className={styles.title}>{translate('Title')}</div>
        </div>

        <div className={styles.definitions}>
          {items.map((item) => {
            return (
              <QualityDefinition
                key={item.id}
                id={item.id}
                quality={item.quality}
                title={item.title}
              />
            );
          })}
        </div>
      </PageSectionContent>
    </FieldSet>
  );
}

export default QualityDefinitions;
