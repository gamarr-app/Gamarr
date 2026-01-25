import $ from 'jquery';

const absUrlRegex = /^(https?:)?\/\//i;
const apiRoot = window.Gamarr.apiRoot;

export interface AjaxOptions {
  url: string;
  method?: string;
  data?: string | object;
  dataType?: string;
  contentType?: string | null;
  headers?: Record<string, string>;
  traditional?: boolean;
  global?: boolean;
}

type AjaxPromise = ReturnType<typeof $.ajax>;

interface AjaxRequestResult {
  request: AjaxPromise;
  abortRequest: () => void;
}

function isRelative(ajaxOptions: AjaxOptions): boolean {
  return !absUrlRegex.test(ajaxOptions.url);
}

function addRootUrl(ajaxOptions: AjaxOptions): void {
  ajaxOptions.url = apiRoot + ajaxOptions.url;
}

function addApiKey(ajaxOptions: AjaxOptions): void {
  ajaxOptions.headers = ajaxOptions.headers || {};
  ajaxOptions.headers['X-Api-Key'] = window.Gamarr.apiKey;
}

function addContentType(ajaxOptions: AjaxOptions): void {
  if (
    ajaxOptions.contentType == null &&
    ajaxOptions.dataType === 'json' &&
    (ajaxOptions.method === 'PUT' ||
      ajaxOptions.method === 'POST' ||
      ajaxOptions.method === 'DELETE')
  ) {
    ajaxOptions.contentType = 'application/json';
  }
}

export default function createAjaxRequest(
  originalAjaxOptions: AjaxOptions
): AjaxRequestResult {
  const requestXHR = new window.XMLHttpRequest();
  let aborted = false;
  let complete = false;

  function abortRequest(): void {
    if (!complete) {
      aborted = true;
      requestXHR.abort();
    }
  }

  const ajaxOptions = { ...originalAjaxOptions };

  if (isRelative(ajaxOptions)) {
    addRootUrl(ajaxOptions);
    addApiKey(ajaxOptions);
    addContentType(ajaxOptions);
  }

  const request = $.ajax({
    xhr: () => requestXHR,
    ...ajaxOptions,
  })
    .then(
      null,
      (xhr: XMLHttpRequest, textStatus: string, errorThrown: string) => {
        (xhr as XMLHttpRequest & { aborted: boolean }).aborted = aborted;

        return $.Deferred().reject(xhr, textStatus, errorThrown).promise();
      }
    )
    .always(() => {
      complete = true;
    });

  return {
    request,
    abortRequest,
  };
}
