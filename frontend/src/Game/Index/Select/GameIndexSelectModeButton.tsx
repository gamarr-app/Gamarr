import { useCallback } from 'react';
import { useSelect } from 'App/SelectContext';
import PageToolbarButton, {
  PageToolbarButtonProps,
} from 'Components/Page/Toolbar/PageToolbarButton';

interface GameIndexSelectModeButtonProps extends PageToolbarButtonProps {
  isSelectMode: boolean;
  onPress: () => void;
}

function GameIndexSelectModeButton(props: GameIndexSelectModeButtonProps) {
  const { label, iconName, isSelectMode, overflowComponent, onPress } = props;
  const [, selectDispatch] = useSelect();

  const onPressWrapper = useCallback(() => {
    if (isSelectMode) {
      selectDispatch({
        type: 'reset',
      });
    }

    onPress();
  }, [isSelectMode, onPress, selectDispatch]);

  return (
    <PageToolbarButton
      label={label}
      iconName={iconName}
      overflowComponent={overflowComponent}
      onPress={onPressWrapper}
    />
  );
}

export default GameIndexSelectModeButton;
