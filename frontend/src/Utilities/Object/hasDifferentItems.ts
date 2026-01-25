interface ItemWithId {
  [key: string]: unknown;
}

function hasDifferentItems<T extends ItemWithId>(
  prevItems: T[],
  currentItems: T[],
  idProp: keyof T = 'id' as keyof T
): boolean {
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
