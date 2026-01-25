import Link, { LinkProps } from 'Components/Link/Link';

interface GameTitleLinkProps extends LinkProps {
  titleSlug: string;
  title: string;
  year?: number;
}

function GameTitleLink({
  titleSlug,
  title,
  year = 0,
  ...otherProps
}: GameTitleLinkProps) {
  const link = `/game/${titleSlug}`;

  return (
    <Link to={link} title={title} {...otherProps}>
      {title}
      {year > 0 ? ` (${year})` : ''}
    </Link>
  );
}

export default GameTitleLink;
