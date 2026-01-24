import React, { useCallback } from 'react';
import MenuItem from 'Components/Menu/MenuItem';

interface AddSpecificationPresetMenuItemProps {
  name: string;
  implementation: string;
  onPress: (payload: { name: string; implementation: string }) => void;
}

function AddSpecificationPresetMenuItem({
  name,
  implementation,
  onPress,
}: AddSpecificationPresetMenuItemProps) {
  const handlePress = useCallback(() => {
    onPress({
      name,
      implementation,
    });
  }, [name, implementation, onPress]);

  return <MenuItem onPress={handlePress}>{name}</MenuItem>;
}

export default AddSpecificationPresetMenuItem;
