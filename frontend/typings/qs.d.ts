declare module 'qs' {
  export interface ParsedQs {
    [key: string]: string | string[] | ParsedQs | ParsedQs[] | undefined;
  }

  export interface IParseOptions {
    comma?: boolean;
    delimiter?: string | RegExp;
    depth?: number | false;
    decoder?: (str: string, defaultDecoder: (str: string) => string) => unknown;
    arrayLimit?: number;
    parseArrays?: boolean;
    allowDots?: boolean;
    plainObjects?: boolean;
    allowPrototypes?: boolean;
    parameterLimit?: number;
    strictNullHandling?: boolean;
    ignoreQueryPrefix?: boolean;
    charset?: 'utf-8' | 'iso-8859-1';
    charsetSentinel?: boolean;
    interpretNumericEntities?: boolean;
  }

  export function parse(str: string, options?: IParseOptions): ParsedQs;
  export function stringify(obj: unknown, options?: unknown): string;

  // eslint-disable-next-line init-declarations
  const qs: {
    parse: typeof parse;
    stringify: typeof stringify;
    ParsedQs: ParsedQs;
  };

  export default qs;
}
