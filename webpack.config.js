const path = require('path');
const TsconfigPathsPlugin = require('tsconfig-paths-webpack-plugin');

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
        extensions: ['.tsx', '.ts', '.js', '.jsx'],
        plugins: [new TsconfigPathsPlugin({
            extensions: ['.tsx', '.ts', '.js', '.jsx'],
            logLevel: 'info',
            configFile: 'tsconfig.json'
        })]
    },
    module: {
        rules: [
            { test: /\.js$/, loader: 'babel-loader', exclude: /node_modules/ },
            {
                test: /\.tsx?$/,
                loader: "ts-loader",
                exclude: /node_modules/,
                options: {
                    configFile: 'tsconfig.json'
                }
            },
            {
                test: /\.(css|less)$/, use: [
                    { loader: 'style-loader' },
                    { loader: 'css-loader' },
                    { loader: 'less-loader' }
                ]
            },
            {
                test: /\.svg$/,
                loader: 'svg-inline-loader'
            },
            {
                test: /\.(png|jpg|gif)$/,
                use: [
                    {
                        loader: 'file-loader',
                        options: {name: '[name].[ext]'}
                    }
                ]
            }
        ]
    },
    mode: 'development',
    devtool: 'inline-source-map',
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