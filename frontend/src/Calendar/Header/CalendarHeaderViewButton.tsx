import classNames from 'classnames';
import { useCallback } from 'react';
import { CalendarView } from 'Calendar/calendarViews';
import Button, { ButtonProps } from 'Components/Link/Button';
import titleCase from 'Utilities/String/titleCase';
import styles from './CalendarHeaderViewButton.css';

interface CalendarHeaderViewButtonProps extends Omit<
  ButtonProps,
  'children' | 'onPress'
> {
  view: CalendarView;
  selectedView: CalendarView;
  onPress: (view: CalendarView) => void;
}

function CalendarHeaderViewButton({
  view,
  selectedView,
  onPress,
  className,
  ...otherProps
}: CalendarHeaderViewButtonProps) {
  const isSelected = selectedView === view;

  const handlePress = useCallback(() => {
    onPress(view);
  }, [view, onPress]);

  return (
    <Button
      {...otherProps}
      className={classNames(className, isSelected && styles.selected)}
      aria-pressed={isSelected}
      onPress={handlePress}
    >
      {titleCase(view)}
    </Button>
  );
}

export default CalendarHeaderViewButton;
