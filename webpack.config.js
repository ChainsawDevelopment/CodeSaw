const path = require('path');

module.exports = {
    entry: [
        './Web/frontend/index.tsx'
    ],
    output: {
        path: path.resolve(__dirname, 'Web/wwwroot'),
        filename: 'dist.js',
        publicPath: 'http://localhost:8080/'
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js', '.jsx']
    },
    module: {
        rules: [
            { test: /\.js$/, loader: 'babel-loader', exclude: /node_modules/ },
            { 
                test: /\.tsx?$/, 
                loader: "ts-loader",
                exclude: /node_modules/,
                options: {
                    configFile: 'Web/frontend/tsconfig.json'
                }
            },
            { test: /\.(css|less)$/, use: [
                { loader: 'style-loader' },
                { loader: 'css-loader' },
                { loader: 'less-loader' }
            ]}
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
        },
        add: (app, middleware) => {
            app.use((ctx, next) => {
                ctx.set('Access-Control-Allow-Origin', '*');
                next();
            });
            middleware.webpack();
            middleware.content();
        },
    };
}