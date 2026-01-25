interface ItemWithId {
  [key: string]: unknown;
}

function getRemovedItems<T extends ItemWithId>(
  prevItems: T[],
  currentItems: T[],
  idProp: keyof T = 'id' as keyof T
): T[] {
  if (prevItems === currentItems) {
    return [];
  }

  const currentItemIds = new Set<unknown>();

  currentItems.forEach((currentItem) => {
    currentItemIds.add(currentItem[idProp]);
  });

  return prevItems.filter((prevItem) => !currentItemIds.has(prevItem[idProp]));
}

export default getRemovedItems;
