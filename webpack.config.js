const path = require('path');

module.exports = {
    entry: [
        './Web/frontend/index.tsx'
    ],
    output: {
        path: path.resolve(__dirname, 'Web/wwwroot'),
        filename: 'dist.js'
    },
    module: {
        rules: [
            { 
                test: /\.tsx?$/, 
                loader: "ts-loader",
                options: {
                    configFile: 'Web/frontend/tsconfig.json'
                }
            }
        ]
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