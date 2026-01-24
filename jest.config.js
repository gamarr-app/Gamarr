module.exports = {
  testEnvironment: 'jsdom',
  roots: ['<rootDir>/frontend/src'],
  moduleDirectories: [
    'node_modules',
    'frontend/src',
    'frontend/src/Shims'
  ],
  moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx'],
  moduleNameMapper: {
    '\\.css$': 'identity-obj-proxy'
  },
  transform: {
    '^.+\\.(js|jsx|ts|tsx)$': ['babel-jest', { configFile: './frontend/babel.config.js' }]
  },
  transformIgnorePatterns: [
    '/node_modules/'
  ]
};
