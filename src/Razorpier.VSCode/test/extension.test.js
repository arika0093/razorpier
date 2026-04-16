const assert = require('assert');
const fs = require('fs');
const os = require('os');
const path = require('path');
const test = require('node:test');
const { formatTextWithTool, isRazorDocument, resolveToolDll } = require('../razorpier');

const extensionPath = path.resolve(__dirname, '..');

test('resolveToolDll prefers published server output', () => {
  const tempExtension = fs.mkdtempSync(path.join(os.tmpdir(), 'razorpier-vscode-'));
  const serverDir = path.join(tempExtension, 'server');
  fs.mkdirSync(serverDir, { recursive: true });
  const toolPath = path.join(serverDir, 'Razorpier.Tool.dll');
  fs.writeFileSync(toolPath, 'placeholder');

  assert.strictEqual(resolveToolDll(tempExtension), toolPath);
});

test('isRazorDocument recognizes razor language ids and file names', () => {
  assert.strictEqual(isRazorDocument({ languageId: 'razor', fileName: 'Component.txt' }), true);
  assert.strictEqual(isRazorDocument({ languageId: 'plaintext', fileName: 'Component.razor' }), true);
  assert.strictEqual(isRazorDocument({ languageId: 'plaintext', fileName: 'notes.txt' }), false);
});

test('formatTextWithTool formats Razor content through the CLI bridge', async () => {
  const toolPath = resolveToolDll(extensionPath);
  const input = '@inject Svc Service\n<div>\n<p>Hello</p>\n</div>\n@using Zebra\n@page "/"\n';
  const output = await formatTextWithTool(toolPath, input);

  assert.match(output, /@using Zebra/);
  assert.match(output, /@page "\/"/);
  assert.match(output, /<div>\n    <p>Hello<\/p>\n<\/div>/);
});
