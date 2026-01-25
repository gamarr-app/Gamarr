interface ItemWithId {
  id: number | string;
}

function hasDifferentItemsOrOrder<T extends ItemWithId>(
  prevItems: T[],
  currentItems: T[],
  idProp: keyof T = 'id' as keyof T
): boolean {
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
