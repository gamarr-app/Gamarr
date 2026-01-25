import { useCallback } from 'react';
import MenuItem from 'Components/Menu/MenuItem';

interface AddDownloadClientPresetMenuItemProps {
  name: string;
  implementation: string;
  onPress: (payload: { name: string; implementation: string }) => void;
}

function AddDownloadClientPresetMenuItem({
  name,
  implementation,
  onPress,
}: AddDownloadClientPresetMenuItemProps) {
  const handlePress = useCallback(() => {
    onPress({
      name,
      implementation,
    });
  }, [name, implementation, onPress]);

  return <MenuItem onPress={handlePress}>{name}</MenuItem>;
}

export default AddDownloadClientPresetMenuItem;
