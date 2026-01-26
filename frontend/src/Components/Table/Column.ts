import React from 'react';
import { SortDirection } from 'Helpers/Props/sortDirections';

type PropertyFunction<T> = () => T;

interface Column<T extends string = string> {
  name: T;
  label: string | PropertyFunction<string> | React.ReactNode;
  className?: string;
  columnLabel?: string;
  isSortable?: boolean;
  fixedSortDirection?: SortDirection;
  isVisible: boolean;
  isModifiable?: boolean;
}

export default Column;
