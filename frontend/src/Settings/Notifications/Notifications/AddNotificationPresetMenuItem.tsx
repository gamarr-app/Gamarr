import { useCallback } from 'react';
import MenuItem from 'Components/Menu/MenuItem';

interface AddNotificationPresetMenuItemProps {
  name: string;
  implementation: string;
  onPress: (payload: { name: string; implementation: string }) => void;
}

function AddNotificationPresetMenuItem({
  name,
  implementation,
  onPress,
}: AddNotificationPresetMenuItemProps) {
  const handlePress = useCallback(() => {
    onPress({
      name,
      implementation,
    });
  }, [name, implementation, onPress]);

  return <MenuItem onPress={handlePress}>{name}</MenuItem>;
}

export default AddNotificationPresetMenuItem;
