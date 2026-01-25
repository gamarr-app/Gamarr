interface SortableItem {
  sortTitle: string;
}

export default function getIndexOfFirstCharacter(
  items: SortableItem[],
  character: string
): number {
  return items.findIndex((item) => {
    const firstCharacter = item.sortTitle.charAt(0);

    if (character === '#') {
      return !isNaN(Number(firstCharacter));
    }

    return firstCharacter === character;
  });
}
