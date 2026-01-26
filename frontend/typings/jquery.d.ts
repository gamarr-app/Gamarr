declare module 'jquery' {
  interface JQueryXHR extends XMLHttpRequest {
    aborted?: boolean;
    responseJSON?: unknown;
  }

  interface JQueryDeferred<T> {
    resolve(value?: T): JQueryDeferred<T>;
    reject(...args: unknown[]): JQueryDeferred<T>;
    promise(): JQueryPromise<T>;
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  interface JQueryPromise<T = any> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    then<U = any>(
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      doneFilter?: ((value: any) => U | JQueryPromise<U>) | null,
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      failFilter?: ((...args: any[]) => unknown) | null
    ): JQueryPromise<U>;
    always(callback: () => void): JQueryPromise<T>;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    done(callback: (data: any) => void): JQueryPromise<T>;
    fail(callback: (xhr: JQueryXHR) => void): JQueryPromise<T>;
  }

  interface JQuery {
    width(): number | undefined;
    height(): number | undefined;
  }

  interface JQueryStatic {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (selector: string | Element | Window | Document): JQuery;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    ajax(settings: JQueryAjaxSettings): JQueryPromise<any>;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    Deferred<T = any>(): JQueryDeferred<T>;
    param(obj: object, traditional?: boolean): string;
  }

  interface JQueryAjaxSettings {
    url?: string;
    method?: string;
    data?: string | object;
    dataType?: string;
    contentType?: string | boolean | null;
    headers?: Record<string, string>;
    traditional?: boolean;
    global?: boolean;
    xhr?: () => XMLHttpRequest;
    [key: string]: unknown;
  }

  // eslint-disable-next-line init-declarations
  const $: JQueryStatic;
  export default $;
  export { JQueryPromise, JQueryDeferred, JQueryXHR, JQueryAjaxSettings };
}

// Global JQuery namespace for JQuery.Promise<T> and JQuery.Deferred<T> syntax
declare namespace JQuery {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  type Promise<T = any> = import('jquery').JQueryPromise<T>;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  type Deferred<T = any> = import('jquery').JQueryDeferred<T>;
}
