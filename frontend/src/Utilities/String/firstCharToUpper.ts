function firstCharToUpper(input: string): string {
  if (!input) {
    return '';
  }

  return [].map
    .call(input, (char: string, i: number) => (i ? char : char.toUpperCase()))
    .join('');
}

export default firstCharToUpper;
