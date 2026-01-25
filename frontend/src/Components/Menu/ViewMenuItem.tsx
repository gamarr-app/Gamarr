import { ReactNode } from 'react';
import SelectedMenuItem, { SelectedMenuItemProps } from './SelectedMenuItem';

interface ViewMenuItemProps extends Omit<SelectedMenuItemProps, 'isSelected'> {
  name?: string;
  selectedView: string;
  children: ReactNode;
  onPress: (view: string) => void;
}

function ViewMenuItem({
  name,
  selectedView,
  ...otherProps
}: ViewMenuItemProps) {
  const isSelected = name === selectedView;

  return (
    <SelectedMenuItem name={name} isSelected={isSelected} {...otherProps} />
  );
}

export default ViewMenuItem;
