import { ComponentType } from 'react';

interface HistoryObject {
  action: string;
}

interface WithCurrentPageProps {
  history: HistoryObject;
}

interface InjectedProps {
  useCurrentPage: boolean;
}

type ExternalProps<P> = Omit<P, keyof InjectedProps> & WithCurrentPageProps;

function withCurrentPage<P extends InjectedProps>(
  WrappedComponent: ComponentType<P>
): ComponentType<ExternalProps<P>> {
  function CurrentPage(props: ExternalProps<P>) {
    const { history } = props;
    const useCurrentPage = history.action === 'POP';

    // Build props by omitting 'history' and adding 'useCurrentPage'
    // Type assertion is required because TypeScript cannot statically prove
    // that Omit<ExternalProps<P>, 'history'> & InjectedProps === P
    const wrappedProps: Record<string, unknown> = {};
    for (const key in props) {
      if (key !== 'history') {
        wrappedProps[key] = props[key as keyof typeof props];
      }
    }
    wrappedProps.useCurrentPage = useCurrentPage;

    return <WrappedComponent {...(wrappedProps as P)} />;
  }

  return CurrentPage;
}

export default withCurrentPage;
