import translate from 'Utilities/String/translate';

const monitorOptions = [
  {
    key: 'gameOnly',
    get value() {
      return translate('GameOnly');
    }
  },
  {
    key: 'gameAndCollection',
    get value() {
      return translate('GameAndCollection');
    }
  },
  {
    key: 'none',
    get value() {
      return translate('None');
    }
  }
];

export default monitorOptions;
