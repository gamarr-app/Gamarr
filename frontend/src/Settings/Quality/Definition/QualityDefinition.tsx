import React, { useCallback, useEffect } from 'react';
import { useDispatch } from 'react-redux';
import TextInput from 'Components/Form/TextInput';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import { setQualityDefinitionValue } from 'Store/Actions/settingsActions';
import { InputChanged } from 'typings/inputs';
import styles from './QualityDefinition.css';

interface QualityDefinitionProps {
  id: number;
  quality: { name: string };
  title: string;
}

function QualityDefinition({ id, quality, title }: QualityDefinitionProps) {
  const dispatch = useDispatch();

  const handleTitleChange = useCallback(
    ({ value }: InputChanged) => {
      dispatch(setQualityDefinitionValue({ id, name: 'title', value }));
    },
    [dispatch, id]
  );

  useEffect(() => {
    return () => {
      dispatch(clearPendingChanges({ section: 'settings.qualityDefinitions' }));
    };
  }, [dispatch]);

  return (
    <div className={styles.qualityDefinition}>
      <div className={styles.quality}>{quality.name}</div>

      <div className={styles.title}>
        <TextInput
          name={`${id}.${title}`}
          value={title}
          onChange={handleTitleChange}
        />
      </div>
    </div>
  );
}

export default QualityDefinition;
