const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

function resolveToolDll(extensionPath) {
  const candidates = [
    path.join(extensionPath, 'server', 'Razorpier.Tool.dll'),
    path.resolve(extensionPath, '..', 'Razorpier.Tool', 'bin', 'Debug', 'net8.0', 'Razorpier.Tool.dll'),
    path.resolve(extensionPath, '..', 'Razorpier.Tool', 'bin', 'Release', 'net8.0', 'Razorpier.Tool.dll')
  ];

  for (const candidate of candidates) {
    if (fs.existsSync(candidate)) {
      return candidate;
    }
  }

  return candidates[0];
}

function isRazorDocument(document) {
  return ['razor', 'aspnetcorerazor'].includes(document.languageId)
    || document.fileName.toLowerCase().endsWith('.razor');
}

function formatTextWithTool(toolPath, input) {
  return new Promise((resolve, reject) => {
    const child = spawn('dotnet', [toolPath, '--stdin'], { stdio: ['pipe', 'pipe', 'pipe'] });
    let stdout = '';
    let stderr = '';

    child.stdout.on('data', (chunk) => {
      stdout += chunk.toString();
    });

    child.stderr.on('data', (chunk) => {
      stderr += chunk.toString();
    });

    child.on('error', reject);
    child.on('close', (code) => {
      if (code === 0) {
        resolve(stdout);
        return;
      }

      reject(new Error(stderr || `Razorpier exited with code ${code}.`));
    });

    child.stdin.end(input);
  });
}

module.exports = {
  formatTextWithTool,
  isRazorDocument,
  resolveToolDll,
};
