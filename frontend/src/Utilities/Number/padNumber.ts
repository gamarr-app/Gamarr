function padNumber(
  input: number | string | null | undefined,
  width: number,
  paddingCharacter: string | number = 0
): string {
  if (input == null) {
    return '';
  }

  const inputStr = `${input}`;
  return inputStr.length >= width
    ? inputStr
    : new Array(width - inputStr.length + 1).join(String(paddingCharacter)) +
        inputStr;
}

export default padNumber;
