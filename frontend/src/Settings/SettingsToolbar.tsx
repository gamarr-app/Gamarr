import { ReactElement, useCallback, useEffect } from 'react';
import { useBlocker } from 'react-router-dom';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton, {
  PageToolbarButtonProps,
} from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import useKeyboardShortcuts from 'Helpers/Hooks/useKeyboardShortcuts';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AdvancedSettingsButton from './AdvancedSettingsButton';
import PendingChangesModal from './PendingChangesModal';

type ToolbarButtonElement =
  | ReactElement<PageToolbarButtonProps>
  | ReactElement<never>
  | null;

interface SettingsToolbarProps {
  showSave?: boolean;
  isSaving?: boolean;
  hasPendingChanges?: boolean;
  additionalButtons?: ToolbarButtonElement | ToolbarButtonElement[];
  onSavePress?: () => void;
}

function SettingsToolbar({
  showSave = true,
  isSaving,
  hasPendingChanges,
  additionalButtons = null,
  onSavePress,
}: SettingsToolbarProps) {
  const { bindShortcut, unbindShortcut } = useKeyboardShortcuts();

  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      Boolean(hasPendingChanges) &&
      currentLocation.pathname !== nextLocation.pathname
  );

  const handleConfirmNavigation = useCallback(() => {
    if (blocker.state === 'blocked') {
      blocker.proceed();
    }
  }, [blocker]);

  const handleCancelNavigation = useCallback(() => {
    if (blocker.state === 'blocked') {
      blocker.reset();
    }
  }, [blocker]);

  useEffect(() => {
    bindShortcut(
      'saveSettings',
      () => {
        if (hasPendingChanges) {
          onSavePress?.();
        }
      },
      {
        isGlobal: true,
      }
    );

    return () => {
      unbindShortcut('saveSettings');
    };
  }, [hasPendingChanges, bindShortcut, unbindShortcut, onSavePress]);

  return (
    <PageToolbar>
      <PageToolbarSection>
        <AdvancedSettingsButton showLabel={true} />
        {showSave ? (
          <PageToolbarButton
            label={
              hasPendingChanges
                ? translate('SaveChanges')
                : translate('NoChanges')
            }
            iconName={icons.SAVE}
            isSpinning={isSaving}
            isDisabled={!hasPendingChanges}
            onPress={onSavePress}
          />
        ) : null}

        {additionalButtons}
      </PageToolbarSection>

      <PendingChangesModal
        isOpen={blocker.state === 'blocked'}
        onConfirm={handleConfirmNavigation}
        onCancel={handleCancelNavigation}
      />
    </PageToolbar>
  );
}

export default SettingsToolbar;
