import qs, { ParsedQs } from 'qs';

interface ParsedUrl {
  hash: string;
  host: string;
  hostname: string;
  href: string;
  origin: string;
  pathname: string;
  port: string;
  protocol: string;
  search: string;
  isAbsolute: boolean;
  params: ParsedQs;
}

const anchor = document.createElement('a');

export default function parseUrl(url: string): ParsedUrl {
  anchor.href = url;

  const properties: ParsedUrl = {
    hash: anchor.hash,
    host: anchor.host,
    hostname: anchor.hostname,
    href: anchor.href,
    origin: anchor.origin,
    pathname: anchor.pathname,
    port: anchor.port,
    protocol: anchor.protocol,
    search: anchor.search,
    isAbsolute: /^[\w:]*\/\//.test(url),
    params: {},
  };

  if (properties.search) {
    properties.params = qs.parse(properties.search.substring(1));
  }

  return properties;
}
