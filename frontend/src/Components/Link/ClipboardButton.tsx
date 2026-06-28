import copy from 'copy-to-clipboard';
import { useCallback, useEffect, useState } from 'react';
import FormInputButton from 'Components/Form/FormInputButton';
import Icon from 'Components/Icon';
import { icons, kinds } from 'Helpers/Props';
import { ButtonProps } from './Button';
import styles from './ClipboardButton.css';

export interface ClipboardButtonProps extends Omit<ButtonProps, 'children'> {
  value: string;
}

export type ClipboardState = 'success' | 'error' | null;

export default function ClipboardButton({
  id,
  value,
  className = styles.button,
  ...otherProps
}: ClipboardButtonProps) {
  const [state, setState] = useState<ClipboardState>(null);

  useEffect(() => {
    if (!state) {
      return;
    }

    const timeoutId = setTimeout(() => {
      setState(null);
    }, 3000);

    return () => {
      if (timeoutId) {
        clearTimeout(timeoutId);
      }
    };
  }, [state]);

  const copyToClipboard = useCallback(async () => {
    try {
      if ('clipboard' in navigator) {
        await navigator.clipboard.writeText(value);
      } else {
        copy(value);
      }

      setState('success');
    } catch (e) {
      setState('error');
      console.error(`Failed to copy to clipboard`, e);
    }
  }, [value]);

  // copyToClipboard handles its own errors; the sync wrapper keeps onClick
  // void-returning (see @typescript-eslint/no-misused-promises).
  const handleClick = useCallback(() => {
    copyToClipboard();
  }, [copyToClipboard]);

  return (
    <FormInputButton
      className={className}
      onClick={handleClick}
      {...otherProps}
    >
      <span className={state ? styles.showStateIcon : undefined}>
        {state ? (
          <span className={styles.stateIconContainer}>
            <Icon
              name={state === 'error' ? icons.DANGER : icons.CHECK}
              kind={state === 'error' ? kinds.DANGER : kinds.SUCCESS}
            />
          </span>
        ) : null}

        <span className={styles.clipboardIconContainer}>
          <Icon name={icons.CLIPBOARD} />
        </span>
      </span>
    </FormInputButton>
  );
}
