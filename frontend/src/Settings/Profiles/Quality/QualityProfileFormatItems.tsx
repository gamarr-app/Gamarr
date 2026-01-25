import _ from 'lodash';
import React, { useCallback, useMemo, useRef, useState } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputHelpText from 'Components/Form/FormInputHelpText';
import FormLabel from 'Components/Form/FormLabel';
import InlineMarkdown from 'Components/Markdown/InlineMarkdown';
import { sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import { FormatItem } from './EditQualityProfileModalContent';
import QualityProfileFormatItem from './QualityProfileFormatItem';
import styles from './QualityProfileFormatItems.css';

interface FormError {
  message?: string;
  errorMessage?: string;
}

interface QualityProfileFormatItemsProps {
  profileFormatItems: FormatItem[];
  errors?: FormError[];
  warnings?: FormError[];
  onQualityProfileFormatItemScoreChange?: (
    formatId: number,
    value: number
  ) => void;
}

function calcOrder(profileFormatItems: FormatItem[]) {
  const items = profileFormatItems.reduce(
    (acc: Record<number, number>, cur, index) => {
      acc[cur.format] = index;
      return acc;
    },
    {}
  );

  return [...profileFormatItems]
    .sort((a, b) => {
      if (b.score !== a.score) {
        return b.score - a.score;
      }

      return a.name.localeCompare(b.name, undefined, { numeric: true });
    })
    .map((x) => items[x.format]);
}

function QualityProfileFormatItems({
  profileFormatItems,
  errors = [],
  warnings = [],
  onQualityProfileFormatItemScoreChange,
}: QualityProfileFormatItemsProps) {
  const [order, setOrder] = useState(() => calcOrder(profileFormatItems));
  const profileFormatItemsRef = useRef(profileFormatItems);
  profileFormatItemsRef.current = profileFormatItems;

  const reorderItems = useMemo(
    () =>
      _.debounce(() => {
        setOrder(calcOrder(profileFormatItemsRef.current));
      }, 1000),
    []
  );

  const handleScoreChange = useCallback(
    (formatId: number, value: number) => {
      if (onQualityProfileFormatItemScoreChange) {
        onQualityProfileFormatItemScoreChange(formatId, value);
      }
      reorderItems();
    },
    [onQualityProfileFormatItemScoreChange, reorderItems]
  );

  if (profileFormatItems.length < 1) {
    return (
      <InlineMarkdown
        className={styles.addCustomFormatMessage}
        data={translate('WantMoreControlAddACustomFormat')}
      />
    );
  }

  return (
    <FormGroup size={sizes.EXTRA_SMALL}>
      <FormLabel size={sizes.SMALL}>{translate('CustomFormats')}</FormLabel>

      <div>
        <FormInputHelpText text={translate('CustomFormatHelpText')} />

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

        <div className={styles.formats}>
          <div className={styles.headerContainer}>
            <div className={styles.headerTitle}>
              {translate('CustomFormat')}
            </div>
            <div className={styles.headerScore}>{translate('Score')}</div>
          </div>
          {order.map((index) => {
            const { format, name, score } = profileFormatItems[index];
            return (
              <QualityProfileFormatItem
                key={format}
                formatId={format}
                name={name}
                score={score}
                onScoreChange={handleScoreChange}
              />
            );
          })}
        </div>
      </div>
    </FormGroup>
  );
}

export default QualityProfileFormatItems;
