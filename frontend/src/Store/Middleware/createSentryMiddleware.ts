import * as sentry from '@sentry/browser';
import * as Integrations from '@sentry/integrations';
import _ from 'lodash';
import { Action, Middleware } from 'redux';
import parseUrl from 'Utilities/String/parseUrl';

interface SentryEvent {
  transaction: string;
  exception?: {
    values: Array<{
      stacktrace?: {
        frames: Array<{
          filename: string;
        }>;
      };
    }>;
  };
  request: {
    url: string;
  };
}

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
  event: sentry.Event,
  hint: sentry.EventHint
): sentry.Event | null {
  const result = _.cloneDeep(event) as SentryEvent;

  const error = hint?.originalException as Error | undefined;

  result.transaction = cleanseUrl(result.transaction);

  if (result.exception) {
    result.exception.values.forEach((exception) => {
      const stacktrace = exception.stacktrace;

      if (stacktrace) {
        stacktrace.frames.forEach((frame) => {
          frame.filename = cleanseUrl(frame.filename);
        });
      }
    });
  }

  if (error && error.message && shouldIgnoreException(error.message)) {
    return null;
  }

  result.request.url = cleanseUrl(result.request.url);

  return result as unknown as sentry.Event;
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
      new Integrations.RewriteFrames({ iteratee: stripUrlBase }),
      new Integrations.Dedupe(),
    ],
  });

  sentry.configureScope((scope) => {
    scope.setUser({ username: userHash });
    scope.setTag('version', version);
    scope.setTag('production', isProduction);
  });

  return createMiddleware();
}
