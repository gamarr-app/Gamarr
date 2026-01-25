function split(input: string | null | undefined, separator = ','): string[] {
  if (!input) {
    return [];
  }

  return input.split(separator).reduce<string[]>((result, s) => {
    if (s) {
      result.push(s);
    }

    return result;
  }, []);
}

export default split;
