import * as showdown from "showdown"

import "./markdownGenerator.less";

export default class MarkdownGenerator {
    private _markdown: showdown.Converter;
    private _classMap = {
        pre: 'cs-preformatted',
        code: 'cs-code',
        table: 'cs-table',
        tr: 'cs-tr',
        td: 'cs-td',
    }

    constructor() {
        const styleAdder = Object.keys(this._classMap)
        .map(key => ({
          type: 'output',
          regex: new RegExp(`<${key}(.*)>`, 'g'),
          replace: `<${key} class="${this._classMap[key]}" $1>`
        }));

        this._markdown = new showdown.Converter({
            simplifiedAutoLink: true,
            literalMidWordUnderscores: true,
            tables: true,
            emoji: true,
            strikethrough: true,
            extensions: [...styleAdder]
        });
    }

    makeHtml(text: string) {
        return this._markdown.makeHtml(text);
    }
}
