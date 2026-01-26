import { ReactElement, useCallback, useEffect, useState } from 'react';
import { useBeforeUnload, useNavigate } from 'react-router-dom';
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

interface SettingsToolbarProps {
  showSave?: boolean;
  isSaving?: boolean;
  hasPendingChanges?: boolean;
  additionalButtons?: ReactElement<PageToolbarButtonProps> | null;
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
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [pendingPath, setPendingPath] = useState<string | null>(null);
  const navigate = useNavigate();

  // Warn on browser/tab close when there are pending changes
  useBeforeUnload(
    useCallback(
      (event) => {
        if (hasPendingChanges) {
          event.preventDefault();
        }
      },
      [hasPendingChanges]
    )
  );

  const handleConfirmNavigation = useCallback(() => {
    setIsModalOpen(false);
    if (pendingPath) {
      navigate(pendingPath);
      setPendingPath(null);
    }
  }, [navigate, pendingPath]);

  const handleCancelNavigation = useCallback(() => {
    setIsModalOpen(false);
    setPendingPath(null);
  }, []);

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
        isOpen={isModalOpen}
        onConfirm={handleConfirmNavigation}
        onCancel={handleCancelNavigation}
      />
    </PageToolbar>
  );
}

export default SettingsToolbar;
