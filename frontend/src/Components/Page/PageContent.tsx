import classNames from 'classnames';
import { ReactNode } from 'react';
import { Helmet } from 'react-helmet-async';
import ErrorBoundary from 'Components/Error/ErrorBoundary';
import PageContentError from './PageContentError';
import styles from './PageContent.css';

interface PageContentProps {
  className?: string;
  title?: string;
  children: ReactNode;
}

function PageContent({ className, title, children }: PageContentProps) {
  return (
    <ErrorBoundary errorComponent={PageContentError}>
      <Helmet>
        <title>
          {title
            ? `${title} - ${window.Gamarr.instanceName}`
            : window.Gamarr.instanceName}
        </title>
      </Helmet>
      <div className={classNames(styles.content, className)}>{children}</div>
    </ErrorBoundary>
  );
}

export default PageContent;
