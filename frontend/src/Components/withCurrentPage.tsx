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

function withCurrentPage<P extends InjectedProps>(
  WrappedComponent: ComponentType<P>
): ComponentType<Omit<P, keyof InjectedProps> & WithCurrentPageProps> {
  function CurrentPage(
    props: Omit<P, keyof InjectedProps> & WithCurrentPageProps
  ) {
    const { history, ...rest } = props;

    return (
      <WrappedComponent
        {...(rest as unknown as P)}
        useCurrentPage={history.action === 'POP'}
      />
    );
  }

  return CurrentPage;
}

export default withCurrentPage;
