const path = require('path');

module.exports = {
    entry: [
        './Web/frontend/index.js'
    ],
    output: {
        path: path.resolve(__dirname, 'Web/wwwroot'),
        filename: 'dist.js'
    }
};
