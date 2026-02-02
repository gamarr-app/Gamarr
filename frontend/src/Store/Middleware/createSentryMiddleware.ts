import * as sentry from '@sentry/browser';
import _ from 'lodash';
import { Action, Middleware } from 'redux';
import parseUrl from 'Utilities/String/parseUrl';

interface StackFrame {
  filename?: string;
}

const IgnoreErrors: RegExp[] = [
  // Innocuous browser errors
  /ResizeObserver loop limit exceeded/,
  /ResizeObserver loop completed with undelivered notifications/,
];

function cleanseUrl(url: string): string {
  const properties = parseUrl(url);

  return `${properties.pathname}${properties.search}`;
}

function shouldIgnoreException(s: string): RegExp | undefined {
  return s ? IgnoreErrors.find((pattern) => pattern.test(s)) : undefined;
}

function cleanseData(
  event: sentry.ErrorEvent,
  hint: sentry.EventHint
): sentry.ErrorEvent | null {
  const result = _.cloneDeep(event);

  const error = hint?.originalException as Error | undefined;

  if (result.transaction) {
    result.transaction = cleanseUrl(result.transaction);
  }

  if (result.exception?.values) {
    result.exception.values.forEach((exception) => {
      const stacktrace = exception.stacktrace;

      if (stacktrace?.frames) {
        stacktrace.frames.forEach((frame) => {
          if (frame.filename) {
            frame.filename = cleanseUrl(frame.filename);
          }
        });
      }
    });
  }

  if (error && error.message && shouldIgnoreException(error.message)) {
    return null;
  }

  if (result.request?.url) {
    result.request.url = cleanseUrl(result.request.url);
  }

  return result;
}

function identity<T>(stuff: T): T {
  return stuff;
}

function stripUrlBase(frame: StackFrame): StackFrame {
  if (frame.filename && window.Gamarr.urlBase) {
    frame.filename = frame.filename.replace(window.Gamarr.urlBase, '');
  }
  return frame;
}

function createMiddleware(): Middleware {
  return (store) => (next) => (action: Action) => {
    try {
      // Adds a breadcrumb for reporting later (if necessary).
      sentry.addBreadcrumb({
        category: 'redux',
        message: action.type,
      });

      return next(action);
    } catch (err) {
      console.error(`[sentry] Reporting error to Sentry: ${err}`);

      // Send the report including breadcrumbs.
      sentry.captureException(err, {
        extra: {
          action: identity(action),
          state: identity(store.getState()),
        },
      });

      return undefined;
    }
  };
}

export default function createSentryMiddleware(): Middleware | undefined {
  const { analytics, branch, version, release, userHash, isProduction } =
    window.Gamarr;

  if (!analytics) {
    return;
  }

  const dsn = isProduction
    ? 'https://7794f2858478485ea337fb5535624fbd@sentry.servarr.com/12'
    : 'https://da610619280249f891ec3ee306906793@sentry.servarr.com/13';

  sentry.init({
    dsn,
    environment: branch,
    release,
    sendDefaultPii: true,
    beforeSend: cleanseData,
    integrations: [
      sentry.rewriteFramesIntegration({ iteratee: stripUrlBase }),
      sentry.dedupeIntegration(),
    ],
  });

  sentry.setUser({ username: userHash });
  sentry.setTag('version', version);
  sentry.setTag('production', isProduction);

  return createMiddleware();
}
