import { SortDirection } from 'Helpers/Props/sortDirections';
import { SelectStateInputProps } from './props';

export type SortCallback = (
  sortKey: string,
  sortDirection?: SortDirection
) => void;

export type SelectStateChangeCallback = (result: SelectStateInputProps) => void;
