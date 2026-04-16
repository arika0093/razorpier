const vscode = require('vscode');
const { formatTextWithTool, isRazorDocument, resolveToolDll } = require('./razorpier');

async function formatDocument(document, extensionPath) {
  const toolPath = resolveToolDll(extensionPath);
  const formatted = await formatTextWithTool(toolPath, document.getText());
  if (formatted === document.getText()) {
    return [];
  }

  const fullRange = new vscode.Range(document.positionAt(0), document.positionAt(document.getText().length));
  return [vscode.TextEdit.replace(fullRange, formatted)];
}

function activate(context) {
  const selector = [
    { language: 'razor', scheme: 'file' },
    { language: 'aspnetcorerazor', scheme: 'file' }
  ];

  context.subscriptions.push(vscode.languages.registerDocumentFormattingEditProvider(selector, {
    provideDocumentFormattingEdits(document) {
      return formatDocument(document, context.extensionPath);
    }
  }));

  context.subscriptions.push(vscode.commands.registerCommand('razorpier.formatDocument', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor || !isRazorDocument(editor.document)) {
      return;
    }

    const edits = await formatDocument(editor.document, context.extensionPath);
    if (edits.length === 0) {
      return;
    }

    await editor.edit((editBuilder) => {
      for (const edit of edits) {
        editBuilder.replace(edit.range, edit.newText);
      }
    });
  }));
}

function deactivate() {
}

module.exports = {
  activate,
  deactivate,
};
