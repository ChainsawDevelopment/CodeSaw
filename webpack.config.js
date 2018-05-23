const path = require('path');

module.exports = {
    entry: [
        './Web/frontend/index.js'
    ],
    output: {
        path: path.resolve(__dirname, 'Web/wwwroot'),
        filename: 'dist.js'
    },
    mode: 'development',
    stats: {
        all: undefined,
        chunks: false,
        chunkModules: false
    }
};


if (process.env.WEBPACK_SERVE === 'true') {
    module.exports.serve = {
        dev: {
            stats: 'minimal'
        }
    };
}