const vscode = require('vscode');

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {
    const keywords = [
        'Component', 'INPUTS', 'OUTPUTS', 'NODES',
        'NOT', 'AND', 'NAND', 'OR', 'NOR', 'XOR', 'XNOR', 
        'ADDER', 'SUBTRACTOR', 'BITSHIFT', 'MULTIPLEXER'
    ];

    const provider = vscode.languages.registerCompletionItemProvider(
        { language: 'vma' }, 
        {
            provideCompletionItems(document, position, token, context) {
                const completionItems = [];

                keywords.forEach(word => {
                    completionItems.push(new vscode.CompletionItem(word, vscode.CompletionItemKind.Keyword));
                });

                const text = document.getText();
                const variableRegex = /\b([a-zA-Z_][a-zA-Z0-9_]*)\b/g;
                const uniqueVariables = new Set();
                let match;

                while ((match = variableRegex.exec(text)) !== null) {
                    const word = match[1];
                    if (!keywords.includes(word) && isNaN(word)) {
                        uniqueVariables.add(word);
                    }
                }

                // 3. Add the discovered variables to the autocomplete menu
                uniqueVariables.forEach(variable => {
                    completionItems.push(new vscode.CompletionItem(variable, vscode.CompletionItemKind.Variable));
                });

                return completionItems;
            }
        }
    );

    context.subscriptions.push(provider);
}

function deactivate() {}

module.exports = {
    activate,
    deactivate
};