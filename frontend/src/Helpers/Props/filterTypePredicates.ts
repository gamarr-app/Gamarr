import * as filterTypes from './filterTypes';

type FilterPredicateFunction = (
  itemValue: unknown,
  filterValue: unknown
) => boolean;

const filterTypePredicates: Record<string, FilterPredicateFunction> = {
  [filterTypes.CONTAINS]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    if (Array.isArray(itemValue)) {
      return itemValue.some((v) => v === filterValue);
    }

    return (itemValue as string)
      .toLowerCase()
      .includes((filterValue as string).toLowerCase());
  },

  [filterTypes.EQUAL]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    return itemValue === filterValue;
  },

  [filterTypes.GREATER_THAN]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    return (itemValue as number) > (filterValue as number);
  },

  [filterTypes.GREATER_THAN_OR_EQUAL]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    return (itemValue as number) >= (filterValue as number);
  },

  [filterTypes.LESS_THAN]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    return (itemValue as number) < (filterValue as number);
  },

  [filterTypes.LESS_THAN_OR_EQUAL]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    return (itemValue as number) <= (filterValue as number);
  },

  [filterTypes.NOT_CONTAINS]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    if (Array.isArray(itemValue)) {
      return !itemValue.some((v) => v === filterValue);
    }

    return !(itemValue as string)
      .toLowerCase()
      .includes((filterValue as string).toLowerCase());
  },

  [filterTypes.NOT_EQUAL]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    return itemValue !== filterValue;
  },

  [filterTypes.STARTS_WITH]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    return (itemValue as string)
      .toLowerCase()
      .startsWith((filterValue as string).toLowerCase());
  },

  [filterTypes.NOT_STARTS_WITH]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    return !(itemValue as string)
      .toLowerCase()
      .startsWith((filterValue as string).toLowerCase());
  },

  [filterTypes.ENDS_WITH]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    return (itemValue as string)
      .toLowerCase()
      .endsWith((filterValue as string).toLowerCase());
  },

  [filterTypes.NOT_ENDS_WITH]: function (
    itemValue: unknown,
    filterValue: unknown
  ): boolean {
    return !(itemValue as string)
      .toLowerCase()
      .endsWith((filterValue as string).toLowerCase());
  },
};

export default filterTypePredicates;
