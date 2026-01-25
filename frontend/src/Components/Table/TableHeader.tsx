import { ReactNode } from 'react';

interface TableHeaderProps {
  children?: ReactNode;
}

function TableHeader({ children }: TableHeaderProps) {
  return (
    <thead>
      <tr>{children}</tr>
    </thead>
  );
}

export default TableHeader;
