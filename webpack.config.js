const path = require("path");
const TsconfigPathsPlugin = require("tsconfig-paths-webpack-plugin");
const AssetsPlugin = require('assets-webpack-plugin');

const PUBLIC_PATH = process.env.PUBLIC_PATH || "http://localhost:8080/";

const baseConfig = {
  entry: ["./CodeSaw.Web/frontend/index.tsx"],
  output: {
    path: path.resolve(__dirname, "CodeSaw.Web/wwwroot"),
    filename: "dist.js",
    publicPath: PUBLIC_PATH
  },
  resolve: {
    extensions: [".tsx", ".ts", ".js", ".jsx"],
    plugins: [
      new TsconfigPathsPlugin({
        extensions: [".tsx", ".ts", ".js", ".jsx"],
        logLevel: "info",
        configFile: path.resolve(__dirname, "tsconfig.json")
      })
    ]
  },
  module: {
    rules: [
      { test: /\.js$/, loader: "babel-loader", exclude: /node_modules/ },
      {
        test: /\.tsx?$/,
        loader: "ts-loader",
        exclude: /node_modules/,
        options: {
          configFile: path.resolve(__dirname, "tsconfig.json")
        }
      },
      {
        test: /\.(css|less)$/,
        use: [
          { loader: "style-loader" },
          { loader: "css-loader" },
          { loader: "less-loader" }
        ]
      },
      {
        test: /\.svg$/,
        loader: "svg-inline-loader"
      },
      {
        test: /\.(png|jpg|gif)$/,
        use: [
          {
            loader: "file-loader",
            options: { name: "[name].[ext]" }
          }
        ]
      }
    ]
  },
  mode: "development",
  devtool: "inline-source-map",
  stats: {
    all: undefined,
    chunks: false,
    chunkModules: false
  },
  plugins: [
    new AssetsPlugin({
      path: path.resolve(__dirname, "CodeSaw.Web/wwwroot"),
      fullPath: false
    })
  ]
};

if (process.env.WEBPACK_SERVE === "true") {
  module.exports.serve = {
    dev: {
      stats: "minimal"
    },
    add: (app, middleware) => {
      app.use((ctx, next) => {
        ctx.set("Access-Control-Allow-Origin", "*");
        next();
      });
      middleware.webpack();
      middleware.content();
    }
  };
}

module.exports = (env, argv) => {
  let config = {...baseConfig};

  if (process.env.WEBPACK_SERVE === "true") {
    config.serve = {
      dev: {
        stats: "minimal"
      },
      add: (app, middleware) => {
        app.use((ctx, next) => {
          ctx.set("Access-Control-Allow-Origin", "*");
          next();
        });
        middleware.webpack();
        middleware.content();
      }
    };
  }

  if(argv && argv.mode == 'production') {
    config.output.filename = '[name].[contenthash].js';
  }

  return config;
}