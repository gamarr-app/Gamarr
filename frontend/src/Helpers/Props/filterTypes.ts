export const CONTAINS = 'contains';
export const EQUAL = 'equal';
export const GREATER_THAN = 'greaterThan';
export const GREATER_THAN_OR_EQUAL = 'greaterThanOrEqual';
export const IN_LAST = 'inLast';
export const NOT_IN_LAST = 'notInLast';
export const IN_NEXT = 'inNext';
export const NOT_IN_NEXT = 'notInNext';
export const LESS_THAN = 'lessThan';
export const LESS_THAN_OR_EQUAL = 'lessThanOrEqual';
export const NOT_CONTAINS = 'notContains';
export const NOT_EQUAL = 'notEqual';
export const STARTS_WITH = 'startsWith';
export const NOT_STARTS_WITH = 'notStartsWith';
export const ENDS_WITH = 'endsWith';
export const NOT_ENDS_WITH = 'notEndsWith';

export type FilterType =
  | typeof CONTAINS
  | typeof EQUAL
  | typeof GREATER_THAN
  | typeof GREATER_THAN_OR_EQUAL
  | typeof IN_LAST
  | typeof NOT_IN_LAST
  | typeof IN_NEXT
  | typeof NOT_IN_NEXT
  | typeof LESS_THAN
  | typeof LESS_THAN_OR_EQUAL
  | typeof NOT_CONTAINS
  | typeof NOT_EQUAL
  | typeof STARTS_WITH
  | typeof NOT_STARTS_WITH
  | typeof ENDS_WITH
  | typeof NOT_ENDS_WITH;

export const all: FilterType[] = [
  CONTAINS,
  EQUAL,
  GREATER_THAN,
  GREATER_THAN_OR_EQUAL,
  LESS_THAN,
  LESS_THAN_OR_EQUAL,
  NOT_CONTAINS,
  NOT_EQUAL,
  IN_LAST,
  NOT_IN_LAST,
  IN_NEXT,
  NOT_IN_NEXT,
  STARTS_WITH,
  NOT_STARTS_WITH,
  ENDS_WITH,
  NOT_ENDS_WITH,
];
