export default function isString(
  possibleString: unknown
): possibleString is string {
  return typeof possibleString === 'string' || possibleString instanceof String;
}
