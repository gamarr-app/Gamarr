import { ReactNode } from 'react';

interface TableBodyProps {
  children?: ReactNode;
}

function TableBody({ children }: TableBodyProps) {
  return <tbody>{children}</tbody>;
}

export default TableBody;
