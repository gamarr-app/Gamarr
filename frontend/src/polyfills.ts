/* eslint no-empty-function: 0, no-extend-native: 0, "@typescript-eslint/no-empty-function": 0 */

declare global {
  interface Window {
    console: Console;
  }

  interface String {
    contains(str: string, startIndex?: number): boolean;
  }
}

window.console = window.console || ({} as Console);
window.console.log = window.console.log || function () {};
window.console.group = window.console.group || function () {};
window.console.groupEnd = window.console.groupEnd || function () {};
window.console.debug = window.console.debug || function () {};
window.console.warn = window.console.warn || function () {};
window.console.assert = window.console.assert || function () {};

if (!String.prototype.startsWith) {
  Object.defineProperty(String.prototype, 'startsWith', {
    enumerable: false,
    configurable: false,
    writable: false,
    value(this: string, searchString: string, position?: number) {
      const pos = position || 0;
      return this.indexOf(searchString, pos) === pos;
    },
  });
}

if (!String.prototype.endsWith) {
  Object.defineProperty(String.prototype, 'endsWith', {
    enumerable: false,
    configurable: false,
    writable: false,
    value(this: string, searchString: string, position?: number) {
      let pos = position || this.length;
      pos = pos - searchString.length;
      const lastIndex = this.lastIndexOf(searchString);
      return lastIndex !== -1 && lastIndex === pos;
    },
  });
}

if (!('contains' in String.prototype)) {
  (
    String.prototype as string & {
      contains: (str: string, startIndex?: number) => boolean;
    }
  ).contains = function (this: string, str: string, startIndex?: number) {
    return String.prototype.indexOf.call(this, str, startIndex) !== -1;
  };
}

export {};
