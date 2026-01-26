import { ComponentType } from 'react';
import { useNavigationType } from 'react-router-dom';

interface InjectedProps {
  useCurrentPage: boolean;
}

type ExternalProps<P> = Omit<P, keyof InjectedProps>;

function withCurrentPage<P extends InjectedProps>(
  WrappedComponent: ComponentType<P>
): ComponentType<ExternalProps<P>> {
  function CurrentPage(props: ExternalProps<P>) {
    const navigationType = useNavigationType();
    const useCurrentPage = navigationType === 'POP';

    return (
      <WrappedComponent {...(props as P)} useCurrentPage={useCurrentPage} />
    );
  }

  return CurrentPage;
}

export default withCurrentPage;
