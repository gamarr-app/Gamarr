import { HTMLAttributes, ReactNode } from 'react';
import styles from './ModalHeader.css';

interface ModalHeaderProps extends HTMLAttributes<HTMLDivElement> {
  children?: ReactNode;
}

function ModalHeader({ children, ...otherProps }: ModalHeaderProps) {
  return (
    <div className={styles.modalHeader} {...otherProps}>
      {children}
    </div>
  );
}

export default ModalHeader;
