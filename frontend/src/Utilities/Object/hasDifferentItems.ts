function hasDifferentItems<
  T extends Record<K, unknown>,
  K extends keyof T = 'id' & keyof T
>(prevItems: T[], currentItems: T[], idProp: K = 'id' as K): boolean {
  if (prevItems === currentItems) {
    return false;
  }

  if (prevItems.length !== currentItems.length) {
    return true;
  }

  const currentItemIds = new Set<unknown>();

  currentItems.forEach((currentItem) => {
    currentItemIds.add(currentItem[idProp]);
  });

  return prevItems.some((prevItem) => !currentItemIds.has(prevItem[idProp]));
}

export default hasDifferentItems;
