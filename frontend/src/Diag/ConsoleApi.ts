import createAjaxRequest from 'Utilities/createAjaxRequest';

// This file contains some helpers for power users in a browser console

interface Resource {
  id?: number;
  [key: string]: unknown;
}

interface FetchOptions {
  method?: string;
  data?: Resource;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
interface ExtendedPromise<T> extends Promise<T> {
  filter: (predicate: (value: any) => boolean) => ExtendedPromise<T>;
  map: <U>(callback: (value: any) => U) => ExtendedPromise<U[]>;
  all: () => ExtendedPromise<T>;
  forEach: <U>(action: (value: any) => U) => ExtendedPromise<U[]>;
}

let hasWarned = false;

function checkActivationWarning(): void {
  if (!hasWarned) {
    console.log('Activated GamarrApi console helpers.');
    console.warn('Be warned: There will be no further confirmation checks.');
    hasWarned = true;
  }
}

function attachAsyncActions<T>(promise: Promise<T>): ExtendedPromise<T> {
  const extendedPromise = promise as ExtendedPromise<T>;

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  extendedPromise.filter = function (
    predicate: (value: any) => boolean
  ): ExtendedPromise<T> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const res = this.then((d) => (d as any[]).filter(predicate) as T);
    attachAsyncActions(res);
    return res as ExtendedPromise<T>;
  };

  extendedPromise.map = function <U>(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    callback: (value: any) => U
  ): ExtendedPromise<U[]> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const res = this.then((d) => (d as any[]).map(callback));
    attachAsyncActions(res);
    return res as ExtendedPromise<U[]>;
  };

  extendedPromise.all = function (): ExtendedPromise<T> {
    const res = this.then(
      (d) =>
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        Promise.all(d as Iterable<any>) as Promise<T>
    );
    attachAsyncActions(res);
    return res as ExtendedPromise<T>;
  };

  extendedPromise.forEach = function <U>(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    action: (value: any) => U
  ): ExtendedPromise<U[]> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const res = this.then((d) => Promise.all((d as any[]).map(action)));
    attachAsyncActions(res);
    return res as ExtendedPromise<U[]>;
  };

  return extendedPromise;
}

class ResourceApi {
  private api: ConsoleApi;
  private url: string;

  constructor(api: ConsoleApi, url: string) {
    this.api = api;
    this.url = url;
  }

  single(id: number): ExtendedPromise<Resource> {
    return this.api.fetch(`${this.url}/${id}`);
  }

  all(): ExtendedPromise<Resource[]> {
    return this.api.fetch(this.url);
  }

  filter(pred: (resource: Resource) => boolean): ExtendedPromise<Resource[]> {
    return this.all().filter(pred);
  }

  update(resource: Resource): ExtendedPromise<Resource> {
    return this.api.fetch(`${this.url}/${resource.id}`, {
      method: 'PUT',
      data: resource,
    });
  }

  delete(resource: Resource | number): ExtendedPromise<void> {
    let resourceId: number;

    if (typeof resource === 'object' && resource !== null && resource.id) {
      resourceId = resource.id;
    } else if (typeof resource === 'number') {
      resourceId = resource;
    } else {
      throw Error('Invalid resource');
    }

    if (!resourceId || !Number.isInteger(resourceId)) {
      throw Error('Invalid resource');
    }

    return this.api.fetch(`${this.url}/${resourceId}`, { method: 'DELETE' });
  }

  fetch<T = unknown>(url: string, options?: FetchOptions): ExtendedPromise<T> {
    return this.api.fetch(`${this.url}${url}`, options);
  }
}

class ConsoleApi {
  public game: ResourceApi;

  constructor() {
    this.game = new ResourceApi(this, '/game');
  }

  resource(url: string): ResourceApi {
    return new ResourceApi(this, url);
  }

  fetch<T = unknown>(url: string, options?: FetchOptions): ExtendedPromise<T> {
    checkActivationWarning();

    options = options || {};

    const req: {
      url: string;
      method: string;
      dataType?: string;
      data?: string;
    } = {
      url,
      method: options.method || 'GET',
    };

    if (options.data) {
      req.dataType = 'json';
      req.data = JSON.stringify(options.data);
    }

    const ajaxResult = createAjaxRequest(req);
    const promise = new Promise<T>((resolve, reject) => {
      ajaxResult.request.then(
        (data: T) => resolve(data),
        (xhr: XMLHttpRequest) => {
          console.error(`Failed to fetch ${url}`, xhr);
          reject(xhr);
        }
      );
    });

    return attachAsyncActions(promise);
  }
}

declare global {
  interface Window {
    GamarrApi: ConsoleApi;
  }
}

window.GamarrApi = new ConsoleApi();

export default ConsoleApi;
