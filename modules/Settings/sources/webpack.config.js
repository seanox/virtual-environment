const path = require("path")

module.exports = {
    entry: "./settings.js",
    target: "node",
    mode: "production",
    output: {
        filename: "settings.js",
        path: path.resolve(__dirname, "..", "data")
    },
    optimization: {
        minimize: false
    }
}