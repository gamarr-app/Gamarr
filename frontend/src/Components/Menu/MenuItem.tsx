import classNames from 'classnames';
import Link, { LinkProps } from 'Components/Link/Link';
import styles from './MenuItem.css';

export interface MenuItemProps extends LinkProps {
  className?: string;
  children: React.ReactNode;
  isDisabled?: boolean;
}

function MenuItem({
  className = styles.menuItem,
  children,
  isDisabled = false,
  ...otherProps
}: MenuItemProps) {
  return (
    <Link
      className={classNames(className, isDisabled && styles.isDisabled)}
      isDisabled={isDisabled}
      {...otherProps}
    >
      {children}
    </Link>
  );
}

export default MenuItem;
