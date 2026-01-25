import $ from 'jquery';
import createAjaxRequest from './createAjaxRequest';

interface ProviderDataValue {
  value: unknown;
}

interface ProviderData {
  fields?: unknown;
  [key: string]: ProviderDataValue | unknown;
}

interface RequestActionPayload {
  provider: string;
  action: string;
  providerData: ProviderData;
  queryParams?: Record<string, string | number | boolean>;
}

function flattenProviderData(
  providerData: ProviderData
): Record<string, unknown> {
  return Object.keys(providerData).reduce<Record<string, unknown>>(
    (result, key) => {
      const property = providerData[key];

      if (key === 'fields') {
        result[key] = property;
      } else {
        result[key] = (property as ProviderDataValue).value;
      }

      return result;
    },
    {}
  );
}

function requestAction(
  payload: RequestActionPayload
): ReturnType<typeof $.ajax> {
  const { provider, action, providerData, queryParams } = payload;

  const ajaxOptions = {
    url: `/${provider}/action/${action}`,
    contentType: 'application/json',
    method: 'POST',
    data: JSON.stringify(flattenProviderData(providerData)),
  };

  if (queryParams) {
    ajaxOptions.url += `?${$.param(queryParams, true)}`;
  }

  return createAjaxRequest(ajaxOptions).request;
}

export default requestAction;
