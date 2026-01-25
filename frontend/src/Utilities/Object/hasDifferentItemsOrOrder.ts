function hasDifferentItemsOrOrder<
  T extends Record<K, unknown>,
  K extends keyof T = 'id' & keyof T
>(prevItems: T[], currentItems: T[], idProp: K = 'id' as K): boolean {
  if (prevItems === currentItems) {
    return false;
  }

  const len = prevItems.length;

  if (len !== currentItems.length) {
    return true;
  }

  for (let i = 0; i < len; i++) {
    if (prevItems[i][idProp] !== currentItems[i][idProp]) {
      return true;
    }
  }

  return false;
}

export default hasDifferentItemsOrOrder;
