import { HTMLAttributes, ReactNode } from 'react';
import styles from './ModalFooter.css';

interface ModalFooterProps extends HTMLAttributes<HTMLDivElement> {
  children?: ReactNode;
}

function ModalFooter({ children, ...otherProps }: ModalFooterProps) {
  return (
    <div className={styles.modalFooter} {...otherProps}>
      {children}
    </div>
  );
}

export default ModalFooter;
