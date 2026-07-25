import { useNavigationType } from 'react-router';

function useCurrentPage() {
  const navigationType = useNavigationType();

  return navigationType === 'POP';
}

export default useCurrentPage;
