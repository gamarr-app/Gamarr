const regex = /\b\w+/g;

function titleCase(input: string | null | undefined): string {
  if (!input) {
    return '';
  }

  return input.replace(regex, (match) => {
    return match.charAt(0).toUpperCase() + match.substr(1).toLowerCase();
  });
}

export default titleCase;
