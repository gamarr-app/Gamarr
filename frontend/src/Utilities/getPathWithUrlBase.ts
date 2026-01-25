export default function getPathWithUrlBase(path: string): string {
  return `${window.Gamarr.urlBase}${path}`;
}
