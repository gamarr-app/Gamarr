declare module 'jquery' {
  interface JQueryXHR extends XMLHttpRequest {
    aborted?: boolean;
    responseJSON?: unknown;
  }

  interface JQueryDeferred<T> {
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
  }

  interface JQueryStatic {
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

  const $: JQueryStatic = {} as JQueryStatic;
  export default $;
}
